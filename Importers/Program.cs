using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importers
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var importer = new InfowarsImporter()) {
                var importTask = importer.GetArticleLinks();

                importTask.Wait();
                var result = importTask.Result;
                
                foreach (string url in result)
                {
                    var datatask = importer.GetArticleData(url);
                    datatask.Wait();
                    var data = datatask.Result;
                    using (StreamWriter write = new StreamWriter(new FileStream("infowars.txt", FileMode.Append)))
                    {
                        write.Write(data);
                    }
                    Console.Out.WriteLine("Processed " + url);
                }

            }
        }
    }
}
