using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Importers
{
    class InfowarsImporter : Importer
    {
        Regex ArticleGetter;
        Regex LinkFinder;
        Regex PFinder;
        public InfowarsImporter()
        {
            ArticleGetter = new Regex("<article>(.*?)</article>", RegexOptions.Compiled | RegexOptions.Singleline);
            PFinder = new Regex("<p( class=.*?)?>(<strong>)?(?'p'.*?)(</strong>)?</p>", RegexOptions.Compiled | RegexOptions.Singleline);
            LinkFinder = new Regex("<h3><a href=\"([^\"]+)\">", RegexOptions.Compiled | RegexOptions.Singleline);
        }
        public async override Task<string> GetArticleData(string url)
        {
            var data = await (await this.LaunchRequest(url)).Content.ReadAsStringAsync();
            var articleData = ArticleGetter.Match(data).Groups[1].Value;

            var paragraphs = PFinder.Matches(articleData);
            string result = "";
            foreach(Match m in paragraphs)
            {
                result += m.Groups["p"].Value + " ";
            }
            return result;
        }

        public async override Task<IEnumerable<string>> GetArticleLinks()
        {
            List<string> responseData = new List<string>();
            for(var i = 0; i < 5; i++)
            {
                Console.Out.WriteLine(i + " launch");
                var response = await this.LaunchRequest("http://www.infowars.com/home-page-featured/?id=" + i);
                if (response.IsSuccessStatusCode)
                {
                    string raw_response = await response.Content.ReadAsStringAsync();
                    Regex link_find = LinkFinder;

                    var find = link_find.Matches(raw_response);

                    foreach(Match m in find)
                    {
                        responseData.Add(m.Groups[1].Value);
                    }

                    Console.Out.WriteLine(i + " done");
                }
                else
                {
                    responseData.Add("ERROR -- " + response.ReasonPhrase + " " + response.StatusCode);
                    Console.Out.WriteLine(i + " done");
                }
            }
            return responseData;
        }
    }
}
