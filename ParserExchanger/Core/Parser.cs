using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using AngleSharp.Parser.Html;
using AngleSharp.Extensions;
using YouTrackHubExchanger;
using Newtonsoft.Json.Linq;

namespace SwitchParser
{
    class Parser
    {
        public void HTMLcreator(JArray exchangeList)
        {
            foreach (JObject disassemb0 in exchangeList)
            {
                foreach (JObject disassemb1 in disassemb0["Models"])
                {


                }
                break;
            }
        }
    }
}
