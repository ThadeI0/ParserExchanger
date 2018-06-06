using System;
using YouTrackHubExchanger;

namespace ParserExchanger
{
    class Program
    { 
        static void Main(string[] args)
        {           
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
