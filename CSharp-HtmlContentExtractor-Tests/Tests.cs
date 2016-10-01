using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CSharp_HtmlContentExtractor_Library;
using NUnit.Framework;

namespace CSharp_HtmlContentExtractor_Tests
{
    public class Tests
    {
        [Test]
        public void Extractor()
        {
            string url = "http://www.dobreprogramy.pl/Messenger-jak-Snapchat-Polacy-pierwsi-dostali-niedopracowany-komunikator-Facebooka,News,76571.html";
            string htmlCode = File.ReadAllText(@"Y:\Documents\Visual Studio 2015\Projects\CSharp-HtmlContentExtractor\sample.html");
            /*using (WebClient client = new WebClient()) 
            {
                htmlCode = client.DownloadString(url);
            }*/

            HtmlContentExtractor extractor = new HtmlContentExtractor(htmlCode, url);
            extractor.ExtractContent();

            File.WriteAllText(@"Y:\Documents\Visual Studio 2015\Projects\CSharp-HtmlContentExtractor\result.htm", extractor.Output);
        }
    }
}
