using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication2.Model;
using System.Xml;

namespace WebApplication2.Controllers
{
    public class APIController : Controller
    {
        // GET: API
        public ActionResult Index()
        {
            return Content("hello");
        }

        public ActionResult Consumption()
        {
            DataObject dobj = APIModel.GetCurrentConsumption();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter xml = XmlWriter.Create(@"H:\\out.xml", settings))
            {
                xml.WriteStartElement("consumption");
                xml.WriteElementString("el", dobj.el.ToString());
                xml.WriteElementString("cw", dobj.cw.ToString());
                xml.WriteElementString("st", dobj.st.ToString());
                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application.xml");
        }

        public ActionResult Intensity()
        {
            List<DataObject> buildings = APIModel.GetCurrentIntensity();
            IEnumerable<DataObject> query =
                from obj in buildings
                orderby obj.total descending
                select obj;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter xml = XmlWriter.Create(@"H:\\out.xml", settings))
            {
                xml.WriteStartElement("intensity");

                foreach (DataObject dobj in query)
                {
                    xml.WriteStartElement("building");
                    xml.WriteAttributeString("name", dobj.name);
                    xml.WriteElementString("el", dobj.el.ToString());
                    xml.WriteElementString("cw", dobj.cw.ToString());
                    xml.WriteElementString("st", dobj.st.ToString());
                    xml.WriteElementString("total", dobj.total.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application.xml");
        }

        public ActionResult Trend()
        {
            List<TimeValuePair> pair = APIModel.GetTrend();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter xml = XmlWriter.Create(@"H:\\out.xml", settings))
            {
                xml.WriteStartElement("trend");

                foreach (TimeValuePair dobj in pair)
                {
                    xml.WriteStartElement("data");
                    xml.WriteAttributeString("util", dobj.name);
                    xml.WriteElementString("time", dobj.timestamp.ToString());
                    xml.WriteElementString("value", dobj.value.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application.xml");
        }
    }
}