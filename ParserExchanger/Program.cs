using System;
using YouTrackHubExchanger;

namespace ParserExchanger
{
    class Program
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
