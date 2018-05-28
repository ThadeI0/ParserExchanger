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
            
            Console.WriteLine("Hello World!");
        }
    }
}
