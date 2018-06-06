using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using RestSharp;
using RestSharp.Authenticators;
using AngleSharp.Extensions;
using AngleSharp.Parser.Html;
using Newtonsoft.Json.Linq;


namespace YouTrackHubExchanger
{
    class Connector
    {
        private JObject bufferBody = new JObject();
        private JToken widgetID;
        public dynamic exchangeList = new JArray();
        private dynamic exchangeListout = new JArray();
        private string jsonInput;
        private JObject jInput;
        private RestClient client;

        public string Linemodel(string line)
        {
            string lineA = line.Replace('\\', '_').Replace('/', '_');
            return lineA;
        }

        public string Lineurl(string line)
        {
            string lineA = line.Remove(0, line.IndexOf(' ') + 1);

            return lineA;
        }

        public string ReadHTMLFILE(string model)
        {
            try
            {
                StreamReader file = new StreamReader(Path.Combine(@"data", Linemodel(model) + ".html"));
                string HTML = file.ReadToEnd();

                return HTML;
            }
            catch (FileNotFoundException e)
            {
                Console.Write(e);
                return null;

            }
        }

        public void CreateHTMLFILE(string HTML, string path)
        {
            StreamWriter file = new StreamWriter(path);
            file.WriteLine(HTML);
            file.Close();
        }

        public string DownloadHTML(string url)
        {
            var webClient = new WebClient();
            string HTML = webClient.DownloadString(url);
            return HTML;
        }

        public void YouTrackRestParams()
        {
            try
            {
                jsonInput = File.ReadAllText(@"YouTrackInput.json");
                Console.WriteLine("Params read: done");
            }
            catch (FileNotFoundException e)
            {
                Console.Write(e);
            }
        }

        public void YouTrackConnect()
        {
            jInput = JObject.Parse(jsonInput);

            client = new RestClient((string)jInput["YTurl"] + "/" + (string)jInput["YTdashboard"]);
            client.Authenticator = new JwtAuthenticator((string)jInput["YTtoken"]);
            var request = new RestRequest(Method.GET);
            request.AddHeader("Accept", "application/json");

            IRestResponse response = client.Execute(request);
            var content = response.Content;

            bufferBody = JObject.Parse(content);
            widgetID = bufferBody.SelectToken(string.Format(@"$.data.widgets[?(@.config.id=='{0}')].config.message", (string)jInput["YTwidget"]));
            if (widgetID.ToString().Length == 0) throw new ArgumentException("Parameter cannot be null", "widgetID.ToString().Length");
            Console.WriteLine("YOUTRACK GET: done");
        }

        public void MarkdownDeserializer()
        {

            dynamic tempProduct = null;
            dynamic tempProduct2 = null;
            string widgetMessage = widgetID.ToString(); //
            Regex regex = new Regex(@"### (?<vendor>\w+)\n\n(?<models>(?:^\+.*$\n?)+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(widgetMessage);
            Regex regex2 = new Regex(@"^\+ \[(?<model>\S+)\]\((?<url>\S+)\)(?: - (?<fw>.+))?$", RegexOptions.Multiline);

            foreach (Match m in matches)
            {
                //output
                dynamic products2 = new JObject();
                dynamic modelList2 = new JArray();
                //

                dynamic products = new JObject();
                dynamic modelList = new JArray();

                products.Add("Vendor", m.Groups["vendor"].Value);
                products2.Add("Vendor", m.Groups["vendor"].Value);
                MatchCollection matches2 = regex2.Matches(m.Groups["models"].Value);
                foreach (Match m2 in matches2)
                {
                    tempProduct = new JObject();
                    tempProduct.Model = m2.Groups["model"].ToString();
                    tempProduct.Url = m2.Groups["url"].ToString();
                    tempProduct.FW = m2.Groups["fw"].ToString();
                    modelList.Add(tempProduct);


                    string path = Path.Combine(@"data", Linemodel(m2.Groups["model"].ToString()) + ".html");

                    if (!File.Exists((path)))
                    {
                        
                        Directory.CreateDirectory(@"data");
                        CreateHTMLFILE(DownloadHTML(m2.Groups["url"].ToString()), path);

                    }
                    var htmlText = ReadHTMLFILE(m2.Groups["model"].ToString());


                    var parser = new HtmlParser();
                    var document = parser.Parse(htmlText.ToString());
                    var SelAlla = document.QuerySelectorAll("a[href*='Firmware']");
                    tempProduct2 = new JObject();
                    tempProduct2.Model = m2.Groups["model"].ToString();

                    foreach (var item in SelAlla)
                    {
                        tempProduct2.Url = item.GetAttribute("href");
                        tempProduct2.FW = item.Text();

                        break;
                    }




                    modelList2.Add(tempProduct);
                }
                products.Add("Models", modelList);
                exchangeList.Add(products);
                products2.Add("Models", modelList);
                exchangeListout.Add(products);
            }

            Console.WriteLine("Markdown deserialized: done");
        }
        //
        public void ExchangeCompare()
        {
            JObject.EqualityComparer.Equals(exchangeList, exchangeListout);
        }
        //
        public void MarkdownSerializer()
        {
            StringBuilder markdownContent = new StringBuilder();
            foreach (JObject disassemb0 in exchangeListout)
            {
                markdownContent.Append("### " + disassemb0["Vendor"] + "\n\n");
                foreach (JObject disassemb1 in disassemb0["Models"])
                {
                    markdownContent.Append(string.Format(@"+ [{0}]({1})", disassemb1["Model"], disassemb1["Url"]));
                    if (disassemb1["FW"].ToString() != "") markdownContent.Append(@" - " + disassemb1["FW"] + "\n");
                    else if (!(disassemb1 == disassemb0["Models"].Last)) markdownContent.Append("\n");
                    if ((disassemb1 == disassemb0["Models"].Last) && (!(disassemb0 == exchangeListout.Last))) markdownContent.Append("\n");
                }
                if (!(disassemb0 == exchangeListout.Last)) markdownContent.Append("\n");

            }

            widgetID = markdownContent.ToString();
            
            if (widgetID.ToString().Length == 0) throw new ArgumentException("Parameter cannot be null", "widgetID.ToString().Length");
            Console.WriteLine("Markdown serialized: done");
        }

        public void YoutrackConnectPost()
        {
            bufferBody.SelectToken(string.Format(@"$.data.widgets[?(@.config.id=='{0}')].config.message", (string)jInput["YTwidget"])).Replace(widgetID);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "application/json");
            if (bufferBody.ToString().Length == 0) throw new ArgumentException("Parameter cannot be null", "bufferBody.ToString().Length == 0");
            request.AddParameter("application/json", bufferBody.ToString(), ParameterType.RequestBody);
            var response = client.Execute(request);
            var content = response.Content;
            Console.WriteLine("YOUTRACK POST: Done");
        }
    }
}
