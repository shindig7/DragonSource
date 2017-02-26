using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCL;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileloc = "../../../clean_out/infowars.mkv";
            using (StreamReader reader = new StreamReader(new FileStream(fileloc, FileMode.Open)))
            {
                MarkovChain chain = MarkovChain.ReadFromStream(reader);
                using (StreamWriter output = new StreamWriter(new FileStream("outfile.txt", FileMode.Create)))
                    output.Write(chain.Generate(1000));
            }
        }
    }
}
