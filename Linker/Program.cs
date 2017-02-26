using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCL;

namespace Linker
{
    // LINKER
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                MarkovChain chain;
                Console.WriteLine("Loading " + File.ReadLines(args[0]).Count() + " lines...");
                using (StreamReader reader = new StreamReader(new FileStream(args[0], FileMode.Open)))
                {
                    chain = MarkovChain.ReadFromStream(reader);
                }
                Console.WriteLine("Done");


                using (StreamWriter outwrite = new StreamWriter(new FileStream("GEN.txt", FileMode.Create)))
                {
                    outwrite.WriteLine(chain.Generate(10000));
                }
            }
            else
            {
                string fileLocation = "../../../clean_out/";

                foreach (var fileloc in Directory.GetFiles(fileLocation))
                {
                    if (Path.GetExtension(fileloc) == ".out")
                    {
                        var chain = new MarkovChain(5);
                        Console.WriteLine(fileloc);
                        using (StreamReader reader = new StreamReader(new FileStream(fileloc, FileMode.Open)))
                        {
                            reader.ReadLine();
                            var tokens = reader.ReadToEnd().Split(' ');
                            tokens = tokens.Where(tok => !tok.Contains('\n')).ToArray();
                            Console.WriteLine("Tokens: " + tokens.Length);
                            chain.Learn(tokens);
                        }
                        Console.WriteLine("Loaded!");
                        
                        using (StreamWriter outwrite = new StreamWriter(new FileStream(Path.Combine(Path.GetDirectoryName(fileloc), Path.GetFileNameWithoutExtension(fileloc) + ".mkv"), FileMode.Create)))
                        {
                            MarkovChain.SaveToStream(outwrite, chain);
                        }
                        /*
                        int sub = 0;
                        foreach(MarkovChain subChain in chain.BuildSubChains())
                        {
                            using (StreamWriter outwrite = new StreamWriter(new FileStream(Path.Combine(Path.GetDirectoryName(fileloc), Path.GetFileNameWithoutExtension(fileloc) +".sub" + (sub++) + ".mkv"), FileMode.Create)))
                            {
                                MarkovChain.SaveToStream(outwrite, subChain);
                            }
                        }
                        */
                    }
                }


                /*
                
                using (StreamWriter outwrite = new StreamWriter(new FileStream("../../../clean_out/save.mkv", FileMode.Create)))
                {
                    MarkovChain.SaveToStream(outwrite, chain);
                }
                

                Console.WriteLine(chain.Generate(100));
                */
            }
            Console.ReadLine();
        }
    }
}
