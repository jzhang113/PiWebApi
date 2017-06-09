using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Model;

namespace WebApplication2.Controllers
{
    public class APIController : Controller
    {
        // GET: API
        public ActionResult Index()
        {
            return Content("hello");
        }

        public ActionResult CurrentConsumption()
        {
            DataObject result = APIModel.GetCurrentConsumption();
            return Content(result.el + "\n" + result.cw + "\n" + result.st);
        }

        public ActionResult CurrentIntensity()
        {
            List<DataObject> buildings = APIModel.GetCurrentIntensity();
            IEnumerable<DataObject> query =
                from obj in buildings
                orderby obj.total descending
                select obj;

            string s = "";

            foreach (DataObject dobj in query)
            {
                s += dobj.el + "\t" + dobj.cw + "\t" + dobj.st + "\t" + dobj.total + "\n\n";
            }
            return Content(s);
        }

        public ActionResult EnergyTrend()
        {
            return Content("trend");
        }
    }
}