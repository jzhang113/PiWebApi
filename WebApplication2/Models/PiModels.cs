using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace WebApplication2.Model
{
    public class TimeValuePair
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public decimal Value { get; set; }
        public string Units { get; set; }
    }

    public class DataObject
    {
        public string Name { get; set; }
        public decimal EL { get; set; }
        public decimal CW { get; set; }
        public decimal ST { get; set; }
        public decimal Total { get; set; }
    }

    public class BuildingData
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
    }

    public static class APIModel
    {
        static string PiWebApiServer = "pi-web-api.facilities.uiowa.edu";
        static string DatabasePath = @"\\pi-af.facilities.uiowa.edu\PIDB-AF\Location\Main Campus";
        static string UrlFormat = "https://{0}/piwebapi/elements?path={1}";
        static string RootUrl = string.Format(UrlFormat, PiWebApiServer, DatabasePath);

        static string Selector = "?selectedFields=Items.Name;Items.WebId";

        static BuildingData SummaryData;
        static List<BuildingData> BuildingList;

        static APIModel()
        {
            dynamic root = MakeRequest(RootUrl);

            // initialize summaryData
            string summaryQuery = root.Links.Attributes + selector;
            dynamic summary = MakeRequest(summaryQuery).Items;

            SummaryData = new BuildingData
            {
                Name = root.Name,
                Id = root.WebId,
                Attributes = new Dictionary<string, string>()
            };

            foreach (dynamic d in summary)
            {
                string name = d.Name;
                string id = d.WebId;
                SummaryData.Attributes.Add(name.ToLower(), id);
            }

            // initialize buildingList
            BuildingList = new List<BuildingData>();
            string buildingQuery = root.Links.Elements + "?templateName=BuildingTemplate&searchFullHierarchy=true";
            dynamic buildings = MakeRequest(buildingQuery).Items;

            foreach (dynamic b in buildings)
            {
                string path = b.Path;
                if (!path.Contains("Buildings") || path.Contains("Inactive"))
                {
                    continue;
                }

                string attrQuery = b.Links.Attributes + Selector;
                dynamic attr = MakeRequest(attrQuery).Items;

                BuildingData item = new BuildingData
                {
                    Name = b.Name,
                    Id = b.WebId,
                    Attributes = new Dictionary<string, string>()
                };

                foreach (dynamic d in attr)
                {
                    string name = d.Name;
                    string id = d.WebId;
                    item.Attributes.Add(name.ToLower(), id);
                }

                BuildingList.Add(item);
            }
        }

        public static DataObject GetCurrentConsumption()
        {
            decimal currentEL = ValueRequest(SummaryData, "Electric_Power");
            decimal currentCW = ValueRequest(SummaryData, "CW_Power");
            decimal currentST = ValueRequest(SummaryData, "Steam_Power");

            return new DataObject { EL = currentEL, CW = currentCW, ST = currentST };
        }

        public static List<DataObject> GetCurrentIntensity(string building)
        {
            List<DataObject> info = new List<DataObject>();

            if (building != null)
            {
                BuildingData b = BuildingList.Find(x => x.Name == building);

                if (b == null)
                {
                    info.Add(new DataObject { Name = building, EL = -1, CW = -1, ST = -1, Total = -1 });
                }
                else
                {
                    decimal currentEL = ValueRequest(b, "EL_Intensity");
                    decimal currentCW = ValueRequest(b, "CW_Intensity");
                    decimal currentST = ValueRequest(b, "ST_Intensity");
                    decimal currentTotal = ValueRequest(b, "Total_Intensity");

                    info.Add(new DataObject { Name = building, EL = currentEL, CW = currentCW, ST = currentST, Total = currentTotal });
                }

                return info;
            }

            foreach (BuildingData entry in BuildingList)
            {
                List<dynamic> d = MassRequest(entry.Id).ToObject<List<dynamic>>();

                decimal currentEL = -1, currentCW = -1, currentST = -1, currentTotal = -1;

                foreach (dynamic metric in d)
                {
                    if (!(bool)metric.Value.Good)
                    {
                        continue;
                    }

                    string metricName = metric.Name;
                    string value = metric.Value.Value;

                    switch (metricName)
                    {
                        case "EL_Intensity":
                            currentEL = Decimal.TryParse(value, out currentEL) ? currentEL : -1;
                            break;
                        case "CW_Intensity":
                            currentCW = Decimal.TryParse(value, out currentCW) ? currentCW : -1;
                            break;
                        case "ST_Intensity":
                            currentST = Decimal.TryParse(value, out currentST) ? currentST : -1;
                            break;
                        case "Total_Intensity":
                            currentTotal = Decimal.TryParse(value, out currentTotal) ? currentTotal : -1;
                            break;
                    }
                }

                info.Add(new DataObject { Name = entry.Name, EL = currentEL, CW = currentCW, ST = currentST, Total = currentTotal });
            }

            return info;
        }

        public static List<TimeValuePair> GetTrend(string field)
        {
            List<TimeValuePair> info = new List<TimeValuePair>();

            AddValues(ref info, field);

            return info;
        }

        private static decimal ValueRequest(BuildingData entry, string name)
        {
            string id;

            if (entry.Attributes.TryGetValue(name.ToLower(), out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/value", id);
                dynamic response = MakeRequest(query);

                if (!(bool)response.Good)
                {
                    return -1;
                }

                String stringVal = response.Value;
                decimal val;

                if (Decimal.TryParse(stringVal, out val))
                {
                    return val;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        private static dynamic MassRequest(string id)
        {
            string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streamsets/{0}/value", id);
            return MakeRequest(query).Items;
        }

        private static dynamic GetRange(BuildingData entry, string name, string startTime = "-1w", string endTime = "*", string interval = "1h")
        {
            string id;

            if (entry.Attributes.TryGetValue(name.ToLower(), out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/interpolated?startTime={1}&endTime={2}&interval={3}", id, startTime, endTime, interval);
                return MakeRequest(query).Items;
            }
            else
            {
                return null;
            }
        }

        private static void AddValues(ref List<TimeValuePair> list, string dataName)
        {
            dynamic hist = GetRange(SummaryData, dataName);

            if (hist == null)
            {
                return;
            }

            foreach (dynamic d in hist)
            {
                if (!(bool)d.Good)
                {
                    continue;
                }

                string time = d.Timestamp;
                decimal val = d.Value;
                string unitAbbrev = d.UnitsAbbreviation;
                list.Add(new TimeValuePair { Name = dataName, Timestamp = time, Value = val, Units = unitAbbrev });
            }
        }

        internal static dynamic MakeRequest(string url) //an internal method that takes a url
        {
            string json = MakeRequestAsync(url).Result; //returns results of MakeRequestAsync
            dynamic results = JsonConvert.DeserializeObject<dynamic>(json);
            return results;
        }

        // callback used to validate the certificate in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            bool result = false;
            if (cert.Subject.ToUpper().Contains("itsnt1426.iowa.uiowa.edu"))   // if (cert.Subject.ToUpper().Contains("fm-7-eccrack.iowa.uiowa.edu"))
            {
                result = true;
            }

            return result;
        }

        internal static async Task<string> MakeRequestAsync(string url)
        {
            // ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //start
            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            // trust sender
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                            = ((sender, cert, chain, errors) => cert.Subject.Contains("fm-7-eccrack.iowa.uiowa.edu"));

            // validate cert by calling a function
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //end

            WebRequest request = WebRequest.Create(url);

            //commented this 01/05/17
            //UseDefaultCredentials helped fix 400 errors
            //request.UseDefaultCredentials = true; //commented this 01/05/17

            // request.Credentials = new System.Net.NetworkCredential("IOWA\fm-pictoapi", "H1LGvgGnJs!N");
            //ConfigureAwait fixed thread problems I encountered awhile ago (you'll need this)
            // new code to set basic authentication start - Rama 01-05-17
            string userName = @"IOWA\fm-pictoapi";//"iowa\fm-eccserviceacc";
            string password = @"H1LGvgGnJs!N";//"GEL#rt2016";

            string authInfo = userName + ":" + password;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            request.Headers["Authorization"] = "Basic " + authInfo;

            // new code to set basic authentication end - Rama 01-05-17

            WebResponse response = await request.GetResponseAsync().ConfigureAwait(false);

            using (System.IO.StreamReader sw = new System.IO.StreamReader(response.GetResponseStream()))
            {
                return sw.ReadToEnd();
            }
        }
    }
}
