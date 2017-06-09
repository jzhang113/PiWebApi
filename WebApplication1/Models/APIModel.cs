using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

//start

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;


//end



namespace WebApplication1.Models
{
    public class APIData
    {

        //defining the server that contains the instance of PI Web API that you want to use
        public static string piWebApiServer = "pi-web-api.facilities.uiowa.edu/piwebapi/";//"itsnt1008.iowa.uiowa.edu/piwebapi/";
        //public static string piWebApiServer = "localhost/piwebapi/";
        //pi-web-api.facilities.uiowa.edu    https://itsnt1008.iowa.uiowa.edu/piwebapi/    fm-7-eccrack.iowa.uiowa.edu/piwebapi/
        static string submeterURL = "https://{0}/attributes?path=\\\\\\pi-af.facilities.uiowa.edu\\PIDB-AF\\Location\\Main%20Campus\\Buildings\\{1}\\EnergyMeters\\{2}|{3}";
        static string buildingURLEnergyMeter = "https://{0}/attributes?path=\\\\\\pi-af.facilities.uiowa.edu\\PIDB-AF\\Location\\Main%20Campus\\Buildings\\{1}\\EnergyMeters|{2}";
        static string buildingURLRoot = "https://{0}/attributes?path=\\\\\\pi-af.facilities.uiowa.edu\\PIDB-AF\\Location\\Main%20Campus\\Buildings\\{1}|{2}";

        //gets the building numbers in
        static StreamReader reader = new StreamReader(System.Web.HttpContext.Current.Request.MapPath("~/Content/BuildingNumbers.json"));
        public static dynamic buildingNumbersJsonArray = JsonConvert.DeserializeObject(reader.ReadToEnd());
        //public static dynamic cleanedBuildingNumbersJsonArray = buildingNumbersJsonArray.replace(/&quot;/g,'"');

        //Tests to see if the meter type exists.  Takes in "CW", "EL", "ST" for energyType
        public static bool hasMeterType(string buildingName, string energyType)
        {
            string buildingUrl = string.Format(submeterURL, piWebApiServer, buildingName, energyType, "Power");
            buildingUrl = buildingUrl.Replace(@"\\", @"\");


            dynamic powerAttributes = MakeRequest(buildingUrl);

            //This checks if the submeter exists. In the web api the "interpolated data" value doesn't exist if the pi tag isn't set up
            if (powerAttributes.Links["InterpolatedData"] == null)
            {
                return false;
            }
            return true;

        }

        //Give it the building name and the Energy type and it will return the submeter snapshot for "Power"
        public static double SubMeterSnapshotByPath(string buildingName, string energyType)
        {

            string buildingUrl = string.Format(submeterURL, piWebApiServer, buildingName, energyType, "Power");
            buildingUrl = buildingUrl.Replace(@"\\", @"\");
            dynamic powerAttributes = MakeRequest(buildingUrl);

            string snapshotObjectUrl = powerAttributes.Links["Value"];
            dynamic snapshotObject = MakeRequest(snapshotObjectUrl);

            //This is a pretty bad way to handle the errors, need to fix this to do something better
            //Basically if a meter is offline or returning bad data then the revenue meter will read something like {value: {name:bad} {value: 307}}
            try
            {
                double value = snapshotObject.Value;
                // DateTime timestamp = snapshotObject.Timestamp;
                return value;
            }
            catch (Exception)
            {

                return -12345;  //Error code so I know it's broken
            }
        }

        //Give it the building name and the Energy type and it will return the submeter snapshot for "Predicted_Power"
        public static dynamic SubMeterPredictionByPath(string buildingName, string energyType)
        {


            string buildingUrl = string.Format(submeterURL, piWebApiServer, buildingName, energyType, "Predicted_Power");
            buildingUrl = buildingUrl.Replace(@"\\", @"\");
            dynamic powerAttributes = MakeRequest(buildingUrl);

            string snapshotObjectUrl = powerAttributes.Links["Value"];
            dynamic snapshotObject = MakeRequest(snapshotObjectUrl);

            //This is a pretty bad way to handle the errors, need to fix this to do something better
            //Basically if a meter is offline or returning bad data then the revenue meter will read something like {value: {name:bad} {value: 307}}
            try
            {
                double value = snapshotObject.Value;
                // DateTime timestamp = snapshotObject.Timestamp;
                return value;
            }
            catch (Exception)
            {

                return -1;
            }

        }


        public static dynamic SubMeterCostByPath(string buildingName, string energyType)
        {
            string powerCost = "";
            if (energyType == "cw")
            {
                powerCost = "Power_Billed_Cost";
            }
            else
            {
                powerCost = "Power_Cost";
            }


            string buildingUrl = string.Format(submeterURL, piWebApiServer, buildingName, energyType, powerCost);
            buildingUrl = buildingUrl.Replace(@"\\", @"\");
            dynamic powerAttributes = MakeRequest(buildingUrl);

            string snapshotObjectUrl = powerAttributes.Links["Value"];
            dynamic snapshotObject = MakeRequest(snapshotObjectUrl);

            //This is a pretty bad way to handle the errors, need to fix this to do something better
            //Basically if a meter is offline or returning bad data then the revenue meter will read something like {value: {name:bad} {value: 307}}
            try
            {
                decimal value = snapshotObject.Value;
                // DateTime timestamp = snapshotObject.Timestamp;
                return value;
            }
            catch (Exception)
            {

                return -1;
            }
        }

        public static double BuildingPowerSnapshotByPath(string buildingName)
        {
            string fullBuildingURL = string.Format(buildingURLEnergyMeter, piWebApiServer, buildingName, "Power");
            fullBuildingURL = fullBuildingURL.Replace(@"\\", @"\");
            dynamic powerAttributes = MakeRequest(fullBuildingURL);

            string snapshotObjectUrl = powerAttributes.Links["Value"];
            dynamic snapshotObject = MakeRequest(snapshotObjectUrl);

            //This is a pretty bad way to handle the errors, need to fix this to do something better
            //Basically if a meter is offline or returning bad data then the revenue meter will read something like {value: {name:bad} {value: 307}}
            try
            {
                double value = snapshotObject.Value;
                // DateTime timestamp = snapshotObject.Timestamp;
                return value;
            }
            catch (Exception)
            {

                return -1;
            }

        }

        //Returns just the building Number if given the building Name
        public static string BuildingNumberByName(string buildingName)
        {
            //I have everything I need to build an entire URL to get the BLDG number. so it looks something like
            // https://pi-web-api.facilities.uiowa.edu/piwebapi/attributes?path=\\\\pi-af.facilities.uiowa.edu\\PIDB-AF\\Location\\Main%20Campus\\Buildings\\Adler%20Journalism%20and%20Mass%20Communication%20Building|BldgNumber


            string fullBuildingURL = string.Format(buildingURLRoot, piWebApiServer, buildingName, "BldgNumber");
            fullBuildingURL = fullBuildingURL.Replace(@"\\", @"\");
            dynamic Attributes = MakeRequest(fullBuildingURL);

            string snapshotObjectUrl = Attributes.Links["Value"];
            dynamic snapshotObject = MakeRequest(snapshotObjectUrl);

            //This is a pretty bad way to handle the errors, need to fix this to do something better
            //Basically if the building doesn't have the building number it will return none
            try
            {
                string value = snapshotObject.Value;
                // DateTime timestamp = snapshotObject.Timestamp;
                return value;
            }
            catch (Exception)
            {

                return "None";
            }
        }

        public static string ReturnBuildingNameByNumber(string buildingNumber)
        {
            string buildingName;
            string defaultBuilding = "University Services Building";

            foreach (var item in buildingNumbersJsonArray["Document"]["Buildings"])
            {
                dynamic item2 = (string)item["BuildingNumber"];

                if (buildingNumber == (string)item["BuildingNumber"])
                {
                    buildingName = (string)item["BuildingName"];
                    return buildingName;
                }

            }
            return defaultBuilding;
        }

        public static dynamic SubMeterOneWeekData(string buildingName, string energyType, string startTime, string endTime)
        {

            string buildingUrl = string.Format(submeterURL, piWebApiServer, buildingName, energyType, "Power");
            buildingUrl = buildingUrl.Replace(@"\\", @"\");


            dynamic powerAttributes = MakeRequest(buildingUrl);

            //This checks if the submeter exists. In the web api the "interpolated data" value doesn't exist if the pi tag isn't set up
            if (powerAttributes.Links["InterpolatedData"] == null)
            {
                return null;
            }


            string interpolatedObjectUrl = powerAttributes.Links["InterpolatedData"];

            interpolatedObjectUrl = interpolatedObjectUrl + "?startTime=" + startTime + "&endTime=" + endTime + "&interval=1h";

            dynamic interpolatedObject = MakeRequest(interpolatedObjectUrl);

            List<dynamic> arrayOfValues = new List<dynamic>();

            foreach (var item in interpolatedObject.Items)
            {
                List<dynamic> dataSet = new List<dynamic>();
                //actual timestamp
                DateTime newDate = item.Timestamp;
                //need to know where UTC is to subtract the two
                DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

                var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow); //should give you -6 so you need to add it, not subtract it

                TimeSpan diff = newDate.ToUniversalTime() - origin + offset;
                double dateUTC = Math.Floor(diff.TotalSeconds);

                //insert a try catch block here

                dataSet.Add(dateUTC * 1000);
                try
                {
                    dataSet.Add(Math.Round((decimal)item.Value, 4));
                    arrayOfValues.Add(dataSet);
                }
                catch (Exception)
                {

                }



            }

            string json = JsonConvert.SerializeObject(arrayOfValues);
            return json;

        }

        //I need this dataset class to be able to build a collection of all my submeter data in the AllSubMeterOneWeekData
        private class DataSet
        {
            public string name { get; set; }
            public dynamic data { get; set; }

        }

        public static dynamic AllSubMeterOneWeekData(string buildingName, string startTime, string endTime)
        {

            if (startTime == null)
            {
                startTime = "*-1w";
            }
            else
            {
            }
            if (endTime == null)
            {
                endTime = "*";
            }
            else
            {
            }

            dynamic elData = SubMeterOneWeekData(buildingName, "el", startTime, endTime);
            dynamic stData = SubMeterOneWeekData(buildingName, "st", startTime, endTime);
            dynamic cwData = SubMeterOneWeekData(buildingName, "cw", startTime, endTime);

            // The method above will return a 0 if the tag doesn't exist.
            if (elData == null)
            {
                elData = 0;
            }
            else
            {
                elData = JsonConvert.DeserializeObject(elData);
            }

            if (stData == null)
            {
                stData = 0;
            }
            else
            {
                stData = JsonConvert.DeserializeObject(stData);
            }

            if (cwData == null)
            {
                cwData = 0;
            }
            else
            {
                cwData = JsonConvert.DeserializeObject(cwData);
            }
            // End section to check if tag exists and deserialize data

            DataSet elDataSet = new DataSet
            {
                name = "Electric",
                data = elData

            };
            DataSet stDataSet = new DataSet
            {
                name = "Steam",
                data = stData

            };
            DataSet cwDataSet = new DataSet
            {
                name = "Chilled Water",
                data = cwData

            };

            List<DataSet> datasets = new List<DataSet>();
            datasets.Add(elDataSet);
            datasets.Add(stDataSet);
            datasets.Add(cwDataSet);

            string json = JsonConvert.SerializeObject(datasets, Formatting.Indented);
            return json;

        }



        //I got these next two methods from OSIsoft's vcampus page
        internal static dynamic MakeRequest(string url) //an internal method that takes a url
        {
            return MakeRequestAsync(url).Result; //returns results of MakeRequestAsync
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




            WebRequest request = WebRequest.Create(url);//commented this 01/05/17
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
                using (JsonTextReader reader = new JsonTextReader(sw))
                {
                    return JObject.ReadFrom(reader);
                }
            }
        }


        //Takes in the attribute name and returns the offset where it is in the list
        //internal static int FindOffsetByName(string attributeName, dynamic elementArray)
        //{
        //    int i=0;
        //    foreach (var item in elementArray)
        //    {
        //        if (item["Name"] == attributeName )
        //        {
        //            break;
        //        }
        //        i++;
        //    }
        //    return i;
        //}


        //I Don't use this yet but in the future I'd like to create an admin panel that allows the user to update a building list and this would get you that list
        internal static List<String> BuildingList()
        {
            //dynamic database = MakeRequest(databaseUrl);
            //string elementsRoute = database.Links["Elements"]; //https://itsnt1426.iowa.uiowa.edu/piwebapi/

            //string buildingURLPath = "https://localhost/piwebapi/elements/E0Y0VcER_XyEiQaeoz7poiygydTtxwX74xGHHwBQVo4vDQUEktQUYuRkFDSUxJVElFUy5VSU9XQS5FRFVcUElEQi1BRlxMT0NBVElPTlxNQUlOIENBTVBVU1xCVUlMRElOR1M/elements";
            //string buildingURLPath = "https://itsnt1008.iowa.uiowa.edu/piwebapi/elements/E0Y0VcER_XyEiQaeoz7poiygydTtxwX74xGHHwBQVo4vDQUEktQUYuRkFDSUxJVElFUy5VSU9XQS5FRFVcUElEQi1BRlxMT0NBVElPTlxNQUlOIENBTVBVU1xCVUlMRElOR1M/elements";
            //string buildingURLPath = "https://fm-7-eccrack.iowa.uiowa.edu/piwebapi/elements/E0Y0VcER_XyEiQaeoz7poiygydTtxwX74xGHHwBQVo4vDQUEktQUYuRkFDSUxJVElFUy5VSU9XQS5FRFVcUElEQi1BRlxMT0NBVElPTlxNQUlOIENBTVBVU1xCVUlMRElOR1M/elements";
            string buildingURLPath = "https://pi-web-api.facilities.uiowa.edu/piwebapi/elements/E0Y0VcER_XyEiQaeoz7poiygydTtxwX74xGHHwBQVo4vDQUEktQUYuRkFDSUxJVElFUy5VSU9XQS5FRFVcUElEQi1BRlxMT0NBVElPTlxNQUlOIENBTVBVU1xCVUlMRElOR1M/elements";

            //This takes me down into the actual building, the first on the list
            dynamic elements = MakeRequest(buildingURLPath); //returns the JSON element with all the arrays of data
            //string elementsSubRoute4 = elements4.Items[0].Links["Elements"]; //sets the URL to be the Elements Link in the first item, this takes me into: \\pi-af.facilities.uiowa.edu\\PIDB-AF\\Location\\Main Campus\\Buildings\\Adler Journalism and Mass Communication Building\\EnergyMeters


            dynamic buildingArray = elements.Items;  //elements5 turns out to be the full array of Items

            List<string> listRange = new List<string>();

            // string csvBuildingNames = "";

            foreach (var building in buildingArray)
            {
                listRange.Add(building.Name.ToString());
                //csvBuildingNames = csvBuildingNames + "," + building.Name.ToString();
                //dynamic test1 = building.Name;
                //string test2 = test1.ToString();
                //Console.WriteLine(test2);
            }

            return listRange;

        }
    }
}

