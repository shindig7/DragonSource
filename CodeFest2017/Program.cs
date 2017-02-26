using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeFest2017
{
    // CLEANER
    class Program
    {
        static void Main(string[] args)
        {
            string fileLocation = "../../../clean_out/";
            foreach (var fileloc in Directory.GetFiles(fileLocation))
            {
                if (Path.GetExtension(fileloc) != ".out")
                {
                    var title = "";
                    var content = "";
                    using (StreamReader reader = new StreamReader(new FileStream(fileloc, FileMode.Open)))
                    {
                        var firstLine = reader.ReadLine();
                        Console.WriteLine("Title: " + firstLine);

                        var fileContent = reader.ReadToEnd();

                        fileContent = RemoveTag(fileContent, "script");
                        fileContent = RemoveTag(fileContent, "style");
                        fileContent = RemoveTag(fileContent, "meta");

                        var reg = new Regex("</?[^>]*?>");
                        fileContent = reg.Replace(fileContent, "");

                        title = firstLine;
                        content = fileContent;
                    }

                    content = content.Replace("(", "").Replace(")", "").Replace(",", "").Replace(".", " . ").Replace("?", " ? ").Replace("!", " ! ");

                    Regex quotFinder = new Regex("\"([^\"]+?)[.,?!]?\"");

                    List<string> QuotDictionary = new List<string>();
                    Match match;
                    while ((match = quotFinder.Match(content)).Success)
                    {
                        QuotDictionary.Add(match.Groups[1].Value);
                        content = content.Substring(0, match.Index) + $"[[Q-{QuotDictionary.Count}]]" + content.Substring(match.Index + match.Length);
                    }
                    var outfile = Path.Combine(Path.GetDirectoryName(fileloc), Path.GetFileNameWithoutExtension(fileloc) + ".out");
                    content = new Regex(@"\s+").Replace(content.Replace("\r", " ").Replace("\n", " "), " ");
                    using (StreamWriter writer = new StreamWriter(new FileStream(outfile, FileMode.Create)))
                    {
                        writer.WriteLine(title);
                        writer.WriteLine(content);
                        Console.WriteLine(content);
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("File written to " + outfile);
                }
            }
            Console.ReadLine();
        }

        static string RemoveTag(string content, string tagname)
        {
            var searchStart = "<" + tagname;
            var searchEnd = "</" + tagname;

            while(content.Contains(searchStart))
            {
                var startIndex = content.IndexOf(searchStart);
                var endOfStartTag = content.IndexOf('>', startIndex);
                if(endOfStartTag == -1)
                {
                    return content.Substring(0, startIndex);
                }
                if(content[endOfStartTag - 1] == '/')
                {
                    return content.Substring(0, startIndex) + content.Substring(endOfStartTag + 1);
                }

                var endIndex = content.IndexOf(searchEnd);
                var endOfEndTag = content.IndexOf('>', endIndex);
                return content.Substring(0, startIndex) + content.Substring(endOfEndTag + 1);
            }
            return content;
        }
    }
}
