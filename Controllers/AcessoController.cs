using CAST.Business.Component.Security;
using CAST.Business.Entities;
using Newtonsoft.Json;
//using CAST.Business.Application.Interfaces;
using CAST.Business.Component;
//using CAST.Business.Component.Common.Helpers;
using CAST.Business.Component.Security;
using CAST.Business.Entities;
//using CAST.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
//using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CAST.Controllers
{
    public class AcessoController : Controller
    {
        //
        // GET: /Acesso/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Ajuda()
        {
            return View();
        }
    }
}