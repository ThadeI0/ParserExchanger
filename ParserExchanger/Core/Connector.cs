using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json.Linq;

namespace YouTrackHubExchanger
{
    class Connector
    {
        private JObject bufferBody = new JObject();
        private JToken widgetID;
        public dynamic exchangeList = new JArray();
        private dynamic exchangeListIn;
        private string jsonInput;
        private JObject jInput; 
        private RestClient client;

        public void YouTrackRestParams()
        {
            try
            {
                jsonInput = File.ReadAllText(@"..\..\..\Inputdata\YouTrackInput.json");
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
            Console.WriteLine("YOUTRACK GET: done");
        }

        public void MarkdownDeserializer()
        {
            
            dynamic tempProduct = null; 
            string widgetMessage = widgetID.ToString(); //
            Regex regex = new Regex(@"### (?<vendor>\w+)\n\n(?<models>(?:^\+.*$\n?)+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(widgetMessage);
            Regex regex2 = new Regex(@"^\+ \[(?<model>\S+)\]\((?<url>\S+)\)(?: - (?<fw>.+))?$", RegexOptions.Multiline);

            foreach (Match m in matches)
            {
                dynamic products = new JObject();
                dynamic modelList = new JArray();
                products.Add("Vendor", m.Groups["vendor"].Value);
                MatchCollection matches2 = regex2.Matches(m.Groups["models"].Value);
                foreach (Match m2 in matches2)
                {
                    tempProduct = new JObject();
                    tempProduct.Model = m2.Groups["model"].ToString();
                    tempProduct.Url = m2.Groups["url"].ToString();
                    tempProduct.FW = m2.Groups["fw"].ToString();
                    modelList.Add(tempProduct);
                }
                products.Add("Models", modelList);
                exchangeList.Add(products);
            }
            exchangeListIn = exchangeList;
            Console.WriteLine("Markdown deserialized: done");
        }
        //
        public void ExchangeCompare()
        {
            JObject.EqualityComparer.Equals(exchangeList, exchangeListIn);
        }
        //
        public void MarkdownSerializer()
        {
            StringBuilder markdownContent = new StringBuilder();
            foreach (JObject disassemb0 in exchangeList)
            {
                markdownContent.AppendLine("### " + disassemb0["Vendor"] + "\n");
                foreach (JObject disassemb1 in disassemb0["Models"])
                {
                    markdownContent.Append(string.Format(@"+ [{0}]({1})", disassemb1["Model"], disassemb1["Url"]));
                    if (disassemb1["FW"].ToString() != "") markdownContent.AppendLine(@" - " + disassemb1["FW"]);
                    else if (!(disassemb1 == disassemb0["Models"].Last)) markdownContent.AppendLine();
                    if ((disassemb1 == disassemb0["Models"].Last) && (!(disassemb0 == exchangeList.Last))) markdownContent.AppendLine();     
                }
                if (!(disassemb0 == exchangeList.Last)) markdownContent.AppendLine();
                
            }
            //markdownContent.AppendLine("Здесь был Жура"); - проверка работоспособности post запроса убрать в релизе
            widgetID = markdownContent.ToString();
            Console.WriteLine("Markdown serialized: done");
        }

        public void YoutrackConnectPost()
        {
            bufferBody.SelectToken(string.Format(@"$.data.widgets[?(@.config.id=='{0}')].config.message", (string)jInput["YTwidget"])).Replace(widgetID);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "application/json");           
            request.AddParameter("application/json", bufferBody.ToString(), ParameterType.RequestBody);
            var response = client.Execute(request);
            var content = response.Content;
            Console.WriteLine("YOUTRACK POST: Done");
        }
    }
}
