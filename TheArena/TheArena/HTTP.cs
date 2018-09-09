using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace TheArena
{
    public class HTTP
    {

        //Look at Request.xml. This is the packet we send to driveright in this function.
        public static HttpStatusCode Service(string xmlPath, out XmlDocument serviceResponse, string country, string plate, string username, string password)
        {
            serviceResponse = null;
            XmlDocument command = new XmlDocument();
            string result;
            try
            {
                command.Load(xmlPath);
                command.InnerXml = command.InnerXml.Replace("countryAAA", country).Replace("plateAAA", plate).Replace("usernameAAA", username).Replace("passwordAAA", password);
                HttpStatusCode status = HTTP_POST(command, out result);
                if (result != null && status != (HttpStatusCode)(-1))
                {
                    serviceResponse = new XmlDocument();
                    serviceResponse.LoadXml(result);
                    return status;
                }
                else
                {
                    serviceResponse = new XmlDocument();
                    serviceResponse.Load("Response.xml");
                    serviceResponse.InnerXml = serviceResponse.InnerXml.Replace("ktypeee", result);

                    return (HttpStatusCode)(-1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                serviceResponse = new XmlDocument();
                serviceResponse.LoadXml(e.Message);
                return (HttpStatusCode)(-1);
            }
        }

        public static string SendHTTP(string country, string plate)
        {
            //Authentication
            string username = "Hunter";
            string password = "MA20c111x";

            //Responses from Camera
            XmlDocument response = null;


            Console.WriteLine("Sending Request...");
            response = null;
            HttpStatusCode code = Service(@"Request.xml", out response, country, plate, username, password);

            if (response != null)
            {
                //We don't want <KType> in our varible. Adding its length (7) will start writing at the actual KType
                int NumberStart = response.InnerXml.ToString().IndexOf(@"<KType>") + 7;
                int NumberEnd = response.InnerXml.ToString().IndexOf(@"</KType>");
                return response.InnerXml.ToString().Substring(NumberStart, NumberEnd - NumberStart);
            }
            return response.InnerXml.ToString();
        }

        protected static HttpStatusCode HTTP_POST(XmlDocument command, out string result)
        {
            result = null;
            //"http://driveright.com/GetGlobalVRM"
            string endpoint = @"http://api.wheelwizards.net/eu/webservice.asmx?op=GetGlobalVRM";
            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(endpoint);
                httpRequest.Method = "POST";
                httpRequest.ContentType = "text/xml";
                httpRequest.Timeout = 7000;
                using (Stream requestStream = httpRequest.GetRequestStream())
                {
                    StreamWriter streamWriter = new StreamWriter(requestStream);
                    streamWriter.Write(command.InnerXml);
                    streamWriter.Close();
                    requestStream.Close();
                    using (HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse())
                    {
                        using (StreamReader os = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            try
                            {
                                result = os.ReadToEnd();
                            }
                            finally
                            {
                                os.Close();
                            }
                            return httpResponse.StatusCode;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                result = e.Message;
                return (HttpStatusCode)(-1);
            }
        }
    }
}
