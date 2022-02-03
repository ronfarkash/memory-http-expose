using MemHTTPExpose.ExposedProc;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MemHTTPExpose.Controllers
{
    [ApiController]
    [Route("[controller]/process")]
    public class MemoryController : Controller
    {
        [Route("{address}/address")]
        [HttpGet]
        public string GetAddress(string address)
        {
            return "hei";
        }

        [Route("{address}/int32")]
        [HttpGet]
        public int GetInt32(string address)
        {
            return 0;
        }

        [Route("{address}/int16")]
        [HttpGet]
        public short GetInt16(string address)
        {
            return 0;
        }

        [Route("{address}/string")]
        [HttpGet]
        public string GetString(string address)
        {
            return "hei";
        }
    }
}
