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
            return View();
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
                xml.WriteStartElement("Consumption");
                xml.WriteElementString("Electric", dobj.EL.ToString());
                xml.WriteElementString("Chilled_Water", dobj.CW.ToString());
                xml.WriteElementString("Steam", dobj.ST.ToString());
                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application/xml");
        }

        public ActionResult Intensity(string building)
        {
            List<DataObject> buildingNames = APIModel.GetCurrentIntensity(building);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter xml = XmlWriter.Create(@"H:\\out.xml", settings))
            {
                xml.WriteStartElement("intensity");

                foreach (DataObject dobj in buildingNames)
                {
                    xml.WriteStartElement("Building");
                    xml.WriteElementString("Name", dobj.Name);
                    xml.WriteElementString("Electric", dobj.EL.ToString());
                    xml.WriteElementString("Chilled_Water", dobj.CW.ToString());
                    xml.WriteElementString("Steam", dobj.ST.ToString());
                    xml.WriteElementString("Total", dobj.Total.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application/xml");
        }

        public ActionResult Trend(string name = "Electric_Power")
        {
            List<TimeValuePair> pair = APIModel.GetTrend(name);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (XmlWriter xml = XmlWriter.Create(@"H:\\out.xml", settings))
            {
                xml.WriteStartElement("Trend");

                foreach (TimeValuePair dobj in pair)
                {
                    xml.WriteStartElement("Data");
                    xml.WriteElementString("Name", name);
                    xml.WriteElementString("Time", dobj.Timestamp.ToString());
                    xml.WriteElementString("Value", dobj.Value.ToString());
                    
                    xml.WriteEndElement();
                }

                xml.WriteEndDocument();
            }

            return File(@"H:\\out.xml", "application/xml");
        }
    }
}