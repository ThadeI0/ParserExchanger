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
        private DateTime dt = DateTime.Now;

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

        public string HeaderRequest(string uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "HEAD";
                HttpWebResponse responseEltex = (HttpWebResponse)request.GetResponse();
                string responseout = responseEltex.Headers["Last-Modified"].ToString();
                responseEltex.Close();
                return responseout;
            }
            catch(WebException e)
            {
                Console.WriteLine("\nWebExpetion: {0}", e);
                Environment.Exit(0);
                return "";
            }
            catch(Exception e)
            {
                Console.WriteLine("\nException: {0}", e);
                Environment.Exit(0);
                return "";
            }
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
            try
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
            catch (WebException e)
            {
                Console.WriteLine("WebException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }

        public void MarkdownDeserializer()
        {

            dynamic tempProduct = null;
            dynamic tempProduct2 = null;
            string widgetMessage = widgetID.ToString(); //
            Regex regex = new Regex(@"### (?<vendor>\w+)\n\n(?<models>(?:^\+.*$\n?)+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(widgetMessage);
            Regex regex2 = new Regex(@"^\+ \[(?<model>\S+)\]\((?<url>\S+)\)(?: - (?<fw>.+)?)?$", RegexOptions.Multiline);
            Regex preregex3 = new Regex(@"^Программное обеспечение версии |для ревизии ");
            Regex regex3 = new Regex(@"(?<ver>\d+\.\d+\.[BR]\d+)(?: ?(от (?<date>\d+\.\d+\.\d+))?(?: \(?\W+(?<rev>[ABCD]\d(?:\/[ABCD]\d)?)\)?)?)?$", RegexOptions.Multiline);

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

                    string htmlText = DownloadHTML(m2.Groups["url"].ToString());

                    if (products2["Vendor"] == "DLINK")
                    {


                        var parser = new HtmlParser();
                        var document = parser.Parse(htmlText.ToString());
                        var SelAlla = document.QuerySelectorAll("a[href*='Firmware']a:not([href$='.doc'])a:not([href$='.pdf'])");
                        tempProduct2 = new JObject();
                        tempProduct2.Model = m2.Groups["model"].ToString();
                        tempProduct2.Url = m2.Groups["url"].ToString();
                        string fwBody = "";
                        if (SelAlla.Length == 0)
                            fwBody = m2.Groups["fw"].ToString();
                        else
                        {
                            int counter = (SelAlla.Length > 3) ? 4 : SelAlla.Length;

                            foreach (var item in SelAlla.Even())
                            {
                                if (counter == 0) break;
                                string result = preregex3.Replace(item.Text(), "");
                                MatchCollection matches3 = regex3.Matches(result);
                                if (matches3.Count != 0)
                                {

                                    foreach (Match m3 in matches3)
                                    {
                                        if (m3.Groups["rev"].ToString() != "")
                                        {
                                            fwBody = fwBody + string.Format("[{0} {1}]({2} \"{3}\")", m3.Groups["ver"].ToString(), m3.Groups["rev"].ToString(), item.GetAttribute("href"), m3.Groups["date"]);
                                        }
                                        else
                                        {
                                            fwBody = fwBody + string.Format("[{0}]({1} \"{2}\")", m3.Groups["ver"].ToString(), item.GetAttribute("href"), m3.Groups["date"]);
                                        }
                                        if (counter > 1)
                                        {
                                            fwBody = fwBody + ", ";
                                        }
                                    }

                                }
                                else if ((matches3.Count == 0) && !(SelAlla.Length == 0))
                                {
                                    fwBody = fwBody + string.Format("[{0}]({1})", result, item.GetAttribute("href"));
                                    if (counter > 1) fwBody = fwBody + ", ";
                                }
                                counter--;

                            }
                        }
                        tempProduct2.FW = fwBody;
                    }
                    else
                    {
                        Regex regex4 = new Regex("(?>href=\")(?:https?://eltex-co.ru)?(/upload/iblock/[a-zA-Z0-9./-]+)+(mes[a-zA-Z0-9./]+-|smg2016_firmware_)([a-zA-Z0-9./-]+)(.zip|.bin)", RegexOptions.Singleline);
                        MatchCollection matches4 = regex4.Matches(htmlText.ToString());
                        tempProduct2 = new JObject();
                        tempProduct2.Model = m2.Groups["model"].ToString();
                        tempProduct2.Url = m2.Groups["url"].ToString();
                        if (matches4.Count != 0)
                        {
                            string hrefEltex = string.Format("https://eltex-co.ru" + matches4[0].Groups[1].Value.ToString() + matches4[0].Groups[2].Value.ToString() + matches4[0].Groups[3].Value.ToString() + matches4[0].Groups[4].Value.ToString());
                            string fwBody = string.Format("[{0}]({1} \"{2}\")", matches4[0].Groups[3].Value.ToString(), hrefEltex, HeaderRequest(hrefEltex));
                            tempProduct2.FW = fwBody;
                        }
                        else
                        {
                            string fwBody = string.Format("[{0}]({1} \"{2}\")", "", "", "");
                            tempProduct2.FW = fwBody;
                        }
                      
                    }

                    modelList.Add(tempProduct);
                    modelList2.Add(tempProduct2);
                }
                products.Add("Models", modelList);
                exchangeList.Add(products);
                products2.Add("Models", modelList2);
                exchangeListout.Add(products2);
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
                    if (disassemb1["FW"] == null) markdownContent.Append(@" - " + "[свежих прошивок нет](#\"\")" + "\n");
                    else if (disassemb1["FW"].ToString() != "") markdownContent.Append(@" - " + disassemb1["FW"] + "\n");
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
            try
            {
                bufferBody.SelectToken(string.Format(@"$.data.widgets[?(@.config.id=='{0}')].config.message", (string)jInput["YTwidget"])).Replace(widgetID);
                bufferBody.SelectToken(string.Format(@"$.data.widgets[?(@.config.id=='{0}')].config.name", (string)jInput["YTwidget"])).Replace(dt.ToString());
                var request = new RestRequest(Method.POST);
                request.AddHeader("Accept", "application/json");
                if (bufferBody.ToString().Length == 0) throw new ArgumentException("Parameter cannot be null", "bufferBody.ToString().Length == 0");
                request.AddParameter("application/json", bufferBody.ToString(), ParameterType.RequestBody);
                var response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK) throw new WebException(response.Content);
                var content = response.Content;
                
                Console.WriteLine("YOUTRACK POST: Done");
            }
            catch(WebException e)
            {
                Console.WriteLine("WebException: {0}", e);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
                Environment.Exit(0);
            }
        }
    }
}
