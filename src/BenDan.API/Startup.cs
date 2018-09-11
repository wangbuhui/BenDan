using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using BenDan.API.AuthHelper;
using BenDan.Repository.sugar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;

namespace BenDan.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
    
            var basePath = AppContext.BaseDirectory;

            #region CORS
            services.AddCors(c =>
            {
                //↓↓↓↓↓↓↓注意正式环境不要使用这种全开放的处理↓↓↓↓↓↓↓↓↓↓
                c.AddPolicy("AllRequests", policy =>
                {
                    policy
                    .AllowAnyOrigin()//允许任何源
                    .AllowAnyMethod()//允许任何方式
                    .AllowAnyHeader()//允许任何头
                    .AllowCredentials();//允许cookie
                });
                //↑↑↑↑↑↑↑注意正式环境不要使用这种全开放的处理↑↑↑↑↑↑↑↑↑↑


                //一般采用这种方法
                c.AddPolicy("LimitRequests", policy =>
                {
                    policy
                    .WithOrigins("http://localhost:8020", "http://blog.core.xxx.com", "")//支持多个域名端口
                    .WithMethods("GET", "POST", "PUT", "DELETE")//请求方法添加到策略
                    .WithHeaders("authorization");//标头添加到策略
                });

            });
            #endregion

            #region swaggerui
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = "BenDan API",
                        Version = "v1",
                        Description = "笨蛋博客API该版本为V1，还有很多不足之处，如有问题可以通过邮箱联系本人。",
                        License = new License
                        {
                            Name = "MIT",
                            Url = "https://mit-license.org/"
                        },
                        Contact = new Contact
                        {
                            Name = "抓住那只羊驼",
                            Email = "admin@3gjn.com"
                        },

                    }
                 );
              
                var filePath = Path.Combine(basePath, "BenDan.API.xml");
                var modelPath = Path.Combine(basePath, "BenDan.Model.xml");
                c.IncludeXmlComments(filePath, true);//添加控制器层注释（true表示显示控制器注释）
                c.IncludeXmlComments(modelPath, true);

                #region Token绑定到ConfigureServices
                //添加header验证信息
                //c.OperationFilter<SwaggerHeader>();
                var security = new Dictionary<string, IEnumerable<string>> { { "BenDan.API", new string[] { } }, };
                c.AddSecurityRequirement(security);
                //方案名称“BenDan.API 可自定义，上下一致即可
                c.AddSecurityDefinition("BenDan.API", new ApiKeyScheme
                {
                    Description = "JWT授权(数据将在请求头中进行传输) 直接在下框中输入{token}\"",
                    Name = "Authorization",//jwt默认的参数名称
                    In = "header",//jwt默认存放Authorization信息的位置(请求头中)
                    Type = "apiKey"
                });
                #endregion
            });

            #region Token服务注册
            services.AddSingleton<IMemoryCache>(factory =>
            {
                var cache = new MemoryCache(new MemoryCacheOptions());
                return cache;
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Client", policy => policy.RequireRole("Client").Build());
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                options.AddPolicy("AdminOrClient", policy => policy.RequireRole("Admin,Client").Build());
            });
            #endregion
            #endregion

            #region 数据库配置
            BaseDBConfig.ConnectionString = Configuration.GetSection("AppSettings:SqlServerConnection").Value;
            #endregion

            #region AutoFac
            //实例化 AutoFac  容器   
            var builder = new ContainerBuilder();
            //注册要通过反射创建的组件
            //builder.RegisterType<AdvertisementServices>().As<IAdvertisementServices>();


            var servicesDllFile = Path.Combine(basePath, "BenDan.Services.dll");//获取项目绝对路径
            var assemblysServices = Assembly.LoadFile(servicesDllFile);//直接采用加载文件的方法

            //builder.RegisterAssemblyTypes(assemblysServices).AsImplementedInterfaces();//指定已扫描程序集中的类型注册为提供所有其实现的接口。

            var repositoryDllFile = Path.Combine(basePath, "BenDan.Repository.dll");
            var assemblysRepository = Assembly.LoadFile(repositoryDllFile);
            builder.RegisterAssemblyTypes(assemblysRepository).AsImplementedInterfaces();

            //将services填充到Autofac容器生成器中
            builder.Populate(services);

            //使用已进行的组件登记创建新容器
            var ApplicationContainer = builder.Build();
            #endregion

            return new AutofacServiceProvider(ApplicationContainer);//第三方IOC接管 core内置DI容器
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

         

            #region SwaggerUI

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api-docs/{documentName}/bendan.json";
            });


            app.UseSwaggerUI(c =>
            {
                c.IndexStream = () => GetType().GetTypeInfo().Assembly
                   .GetManifestResourceStream("BenDan.API.index.html");

                c.SwaggerEndpoint("/api-docs/v1/bendan.json", "BenDan API V1");
                c.RoutePrefix = "";
                c.DocumentTitle = "BenDan API";
            });

            app.UseMiddleware<JwtTokenAuth>();

            app.UseCors("LimitRequests");//将 CORS 中间件添加到 web 应用程序管线中, 以允许跨域请求。有的不加也是可以的，最好是加上吧

            app.UseStaticFiles();
            app.UseAuthentication();

            #endregion

            app.UseMvc();
        }
    }
}
