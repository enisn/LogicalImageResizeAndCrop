using Components.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Components.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
            return View();
        }
        

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file,int width, int height)
        {

            var imageUrl  = ImageUpload.SaveImage(file, width, height);
            return Redirect(imageUrl);
        }
    }
}