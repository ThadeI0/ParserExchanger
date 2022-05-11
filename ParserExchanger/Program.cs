using System;
using YouTrackHubExchanger;

namespace ParserExchanger
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var EndPoint = "https://192.168.0.1/api";
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(EndPoint) };
            Connector connector = new Connector();
            connector.YouTrackRestParams();
            connector.YouTrackConnect();
            connector.MarkdownDeserializer();
            connector.MarkdownSerializer();
            connector.YoutrackConnectPost();
            Console.WriteLine("Hello World!");
        }
    }
}
