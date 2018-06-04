using System;
using YouTrackHubExchanger;
using SwitchParser;

namespace ParserExchanger
{
    class Program
    { 
        static void Main(string[] args)
        {
            
            Connector connector = new Connector();  
            Parser switchParser = new Parser();

            connector.YouTrackRestParams();
            connector.YouTrackConnect();
            connector.MarkdownDeserializer();

            switchParser.HTMLcreator(connector.exchangeList);
            connector.MarkdownSerializer();
            connector.YoutrackConnectPost();
            Console.WriteLine("Hello World!");
        }
    }
}
