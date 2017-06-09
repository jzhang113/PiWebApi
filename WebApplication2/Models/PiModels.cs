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
        public string name { get; set; }
        public string timestamp { get; set; }
        public decimal value { get; set; }
    }

    public class DataObject
    {
        public string name { get; set; }
        public decimal el { get; set; }
        public decimal cw { get; set; }
        public decimal st { get; set; }
        public decimal total { get; set; }
    }

    public class BuildingData
    {
        public string name { get; set; }
        public string id { get; set; }
        public Dictionary<string, string> attributes { get; set; }
    }

    public static class APIModel
    {
        static string piWebApiServer = "pi-web-api.facilities.uiowa.edu";
        static string databasePath = @"\\pi-af.facilities.uiowa.edu\PIDB-AF\Location\Main Campus";
        static string urlFormat = "https://{0}/piwebapi/elements?path={1}";
        static string rootUrl = string.Format(urlFormat, piWebApiServer, databasePath);

        static string selector = "?selectedFields=Items.Name;Items.WebId";

        static BuildingData summaryData;
        static List<BuildingData> buildingList;

        static APIModel()
        {
            dynamic root = MakeRequest(rootUrl);

            // initialize summaryData
            string summaryQuery = root.Links.Attributes + selector;
            dynamic summary = MakeRequest(summaryQuery).Items;

            summaryData = new BuildingData
            {
                name = root.Name,
                id = root.WebId,
                attributes = new Dictionary<string, string>()
            };

            foreach (dynamic d in summary)
            {
                string name = d.Name;
                string id = d.WebId;
                summaryData.attributes.Add(name, id);
            }

            // initialize buildingList
            buildingList = new List<BuildingData>();
            string buildingQuery = root.Links.Elements + "?templateName=BuildingTemplate&searchFullHierarchy=true";
            dynamic buildings = MakeRequest(buildingQuery).Items;

            foreach (dynamic b in buildings)
            {
                string path = b.Path;
                if (!path.Contains("Buildings") || path.Contains("Inactive"))
                {
                    continue;
                }

                string attrQuery = b.Links.Attributes + selector;
                dynamic attr = MakeRequest(attrQuery).Items;

                BuildingData item = new BuildingData
                {
                    name = b.Name,
                    id = b.WebId,
                    attributes = new Dictionary<string, string>()
                };

                foreach (dynamic d in attr)
                {
                    string name = d.Name;
                    string id = d.WebId;
                    item.attributes.Add(name, id);
                }

                buildingList.Add(item);
            }
        }

        public static DataObject GetCurrentConsumption()
        {
            decimal currentEL = ValueRequest(summaryData, "Electric_Power");
            decimal currentCW = ValueRequest(summaryData, "CW_Power");
            decimal currentST = ValueRequest(summaryData, "Steam_Power");

            return new DataObject { el = currentEL, cw = currentCW, st = currentST, total = -1 };
        }

        public static List<DataObject> GetCurrentIntensity()
        {
            List<DataObject> info = new List<DataObject>();

            foreach (BuildingData entry in buildingList)
            {
                List<dynamic> d = MassRequest(entry.id).ToObject<List<dynamic>>();

                decimal currentEL = -1, currentCW = -1, currentST = -1, currentTotal = -1;

                foreach (dynamic metric in d)
                {
                    string metricName = metric.Name;
                    //error handling here too
                    Type t = metric.Value.Value.GetType();

                    if (t == typeof(JObject))
                        continue;

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
                //decimal currentEL = ValueRequest(entry, "EL_Intensity");
                //decimal currentCW = ValueRequest(entry, "CW_Intensity");
                //decimal currentST = ValueRequest(entry, "ST_Intensity");
                //decimal currentTotal = ValueRequest(entry, "Total_Intensity");

                info.Add(new DataObject { name = entry.name, el = currentEL, cw = currentCW, st = currentST, total = currentTotal });
            }

            return info;
        }

        public static List<TimeValuePair> GetTrend()
        {
            List<TimeValuePair> info = new List<TimeValuePair>();

            dynamic elTrends = GetRange(summaryData, "Electric_Power");

            foreach (dynamic d in elTrends)
            {
                if (!(bool)d.Good)
                    continue;

                info.Add(new TimeValuePair { name = "el", timestamp = (string)d.Timestamp, value = (decimal)d.Value });
            }

            dynamic cwTrends = GetRange(summaryData, "CW_Power");

            foreach (dynamic d in cwTrends)
            {
                Type t = d.Value.GetType();

                if (t == typeof(JObject))
                    continue;

                string time = d.Timestamp;
                decimal val = d.Value;
                info.Add(new TimeValuePair { name = "cw", timestamp = time, value = val });
            }

            dynamic stTrends = GetRange(summaryData, "Steam_Power");
            foreach (dynamic d in stTrends)
            {
                Type t = d.Value.GetType();

                if (t == typeof(JObject))
                    continue;

                string time = d.Timestamp;
                decimal val = d.Value;
                info.Add(new TimeValuePair { name = "st", timestamp = time, value = val });
            }

            return info;
        }

        static decimal ValueRequest(BuildingData entry, string name)
        {
            string id;

            if (entry.attributes.TryGetValue(name, out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/value", id);
                dynamic response = MakeRequest(query);

                // TODO more robust error handling here
                // if value of the response is an error message, it returns a JObject instead of a JValue
                Type t = response.Value.GetType();

                if (t == typeof(JObject))
                    return -1;

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

        static dynamic MassRequest(string id)
        {
            string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streamsets/{0}/value", id);
            return MakeRequest(query).Items;
        }

        static dynamic GetRange(BuildingData entry, string name, string startTime = "-1w", string endTime = "*", string interval = "1h")
        {
            string id;

            if (entry.attributes.TryGetValue(name, out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/interpolated?startTime={1}&endTime={2}&interval={3}", id, startTime, endTime, interval);
                return MakeRequest(query).Items;
            }
            else
            {
                Console.WriteLine("Failed");
                return null;
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
