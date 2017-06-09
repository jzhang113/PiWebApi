using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication2.Model
{
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

        static List<BuildingData> buildingList;
        static BuildingData summaryData;

        static APIModel()
        {
            dynamic root = MakeRequest(rootUrl);
            InitializeDictionary(root);

            string summaryQuery = root.Links.Attributes;
            dynamic summary = MakeRequest(summaryQuery);

            summaryData = new BuildingData
            {
                name = "Summary",
                id = summary.WebId,
                attributes = new Dictionary<string, string>()
            };

            string name = summary.Name;
            string id = summary.WebId;
            summaryData.attributes.Add(name, id);
        }

        static void InitializeDictionary(dynamic root)
        {
            buildingList = new List<BuildingData>();
            string buildingQuery = root.Links.Elements + "?templateName=BuildingTemplate&searchFullHierarchy=true";
            dynamic buildings = MakeRequest(buildingQuery);

            foreach (dynamic b in buildings.Items)
            {
                string path = b.Path;
                if (!path.Contains("Buildings") || path.Contains("Inactive"))
                {
                    continue;
                }

                string attrQuery = b.Links.Attributes + "?selectedFields=Items.Name;Items.WebId";
                dynamic attr = MakeRequest(attrQuery);

                BuildingData item = new BuildingData
                {
                    name = attr.Name,
                    id = attr.WebId,
                    attributes = new Dictionary<string, string>()
                };

                foreach (dynamic d in attr)
                {
                    string name = d.Name;
                    string id = d.WebId;
                    item.attributes.Add(name, id);
                }

                string bName = b.Name;
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
                dynamic dyn = MassRequest(entry.id);

                //decimal currentEL = ValueRequest(entry.Value, "EL_Intensity");
                //decimal currentCW = ValueRequest(entry.Value, "CW_Intensity");
                //decimal currentST = ValueRequest(entry.Value, "ST_Intensity");
                //decimal currentTotal = ValueRequest(entry.Value, "Total_Intensity");

                //info.Add(new DataObject { name = entry.Key,  el = currentEL, cw = currentCW, st = currentST, total = currentTotal });
            }

            return info;
        }

        public static void GetTrend()
        {
            GetRange(summaryData, "Electric_Power");
            GetRange(summaryData, "CW_Power");
            GetRange(summaryData, "Steam_Power");
        }

        static decimal ValueRequest(BuildingData entry, string name)
        {
            string id;

            if (entry.attributes.TryGetValue(name, out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/value", id);
                dynamic response = MakeRequest(query);
                string stringVal = response.Value;
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
            dynamic response = MakeRequest(query);
            return response.Items;
        }

        static void GetRange(BuildingData entry, string name, string startTime = "-1w", string endTime = "*", string interval = "1h")
        {
            string id;

            if (entry.attributes.TryGetValue(name, out id))
            {
                string query = String.Format("https://pi-web-api.facilities.uiowa.edu/piwebapi/streams/{0}/interpolated?startTime={1}&endTime={2}&interval={3}", id, startTime, endTime, interval);
                dynamic response = MakeRequest(query);
                Console.WriteLine(response);
            }
            else
            {
                Console.WriteLine("Failed");
            }
        }

        internal static dynamic MakeRequest(string url) //an internal method that takes a url
        {
            dynamic json = MakeRequestAsync(url).Result; //returns results of MakeRequestAsync
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

        internal static async Task<dynamic> MakeRequestAsync(string url)
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
