using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenDan.IServices;
using BenDan.Model.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BenDan.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
       // [HttpGet("{id}", Name = "Get")]
        //public List<Advertisement> Get(int id)
        //{
        //    //IAdvertisementServices advertisementServices = new AdvertisementServices();

        //    //return advertisementServices.Query(d => d.Id == id);
        //}

    }
}