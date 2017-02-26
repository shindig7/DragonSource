using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCL;
using System.IO;

namespace Evaluator
{
    class Program
    {
        static void Main(string[] args)
        {
            const string LEFT_SOURCE = "../../../clean_out/mkv/left/";
            const string LEFT_SOURCE_CMP = "../../../clean_out/mkv/left/take/";
            const string RIGHT_SOURCE = "../../../clean_out/mkv/right/";
            const string RIGHT_SOURCE_CMP = "../../../clean_out/mkv/right/take/";
            const string CENTER_SOURCE = "../../../clean_out/tmkv/";
            const string CTRL_SOURCE = "../../../clean_out/mkv/ctrl/";

            int MAX_NUMF = Math.Min(Directory.GetFiles(LEFT_SOURCE_CMP).Count(), Directory.GetFiles(RIGHT_SOURCE_CMP).Count());
            
            List<string> CSVHeaders = new List<string>(){ "name", "expected" };
            Dictionary<string, List<string>> CSVContent = new Dictionary<string, List<string>>();

            for(var use_source_count = 1; use_source_count <= MAX_NUMF; use_source_count++)
            {
                CSVHeaders.Add(use_source_count + "");
                Dictionary<string, double> SourceFiles = new Dictionary<string, double>();
                int knowFail = 0;
                foreach (var InputBrain in Directory.GetFiles(CENTER_SOURCE).Union(Directory.GetFiles(LEFT_SOURCE)).Union(Directory.GetFiles(RIGHT_SOURCE)))
                {
                    SourceFiles.Clear();
                    foreach (string file in Directory.GetFiles(LEFT_SOURCE_CMP).Where(file=> Path.GetFileName(file) != Path.GetFileName(InputBrain)).Take(use_source_count))
                    {
                        SourceFiles.Add(file, 1);
                    }
                    foreach (string file in Directory.GetFiles(RIGHT_SOURCE_CMP).Where(file => Path.GetFileName(file) != Path.GetFileName(InputBrain)).Take(use_source_count))
                    {
                        SourceFiles.Add(file, -1);
                    }

                    MarkovChain ControlChain = null;
                    MarkovChain[] SubControls = null;
                    foreach(string file in Directory.GetFiles(CTRL_SOURCE))
                    {
                        using (StreamReader reader = new StreamReader(new FileStream(file, FileMode.Open)))
                        {
                            if (ControlChain == null)
                                ControlChain = MarkovChain.ReadFromStream(reader);
                            else
                                ControlChain.Extend(MarkovChain.ReadFromStream(reader));
                        }
                    }
                    if (ControlChain != null)
                        SubControls = ControlChain.BuildSubChains().ToArray();

                    MarkovChain chain = null;
                    using (StreamReader reader = new StreamReader(new FileStream(InputBrain, FileMode.Open)))
                    {
                        chain = MarkovChain.ReadFromStream(reader);
                    }

                    double total = 0;
                    MarkovChain Positive = null;
                    MarkovChain Negitive = null;

                    List<Tuple<double, MarkovChain, string>> PositiveSources = new List<Tuple<double, MarkovChain, string>>();
                    List<Tuple<double, MarkovChain, string>> NegitiveSources = new List<Tuple<double, MarkovChain, string>>();
                    foreach (var kvpair in SourceFiles)
                    {
                        using (StreamReader read = new StreamReader(new FileStream(kvpair.Key, FileMode.Open)))
                        {
                            var learnChain = MarkovChain.ReadFromStream(read);
                            if (kvpair.Value > 0)
                            {
                                if (Positive == null)
                                    Positive = learnChain;
                                else
                                    Positive.Extend(learnChain);
                            }
                            else
                            {
                                if (Negitive == null)
                                    Negitive = learnChain;
                                else
                                    Negitive.Extend(learnChain);
                            }
                        }
                    }
                    PositiveSources.Add(new Tuple<double, MarkovChain, string>(10, /*MarkovChain.Exclusion(Positive, ControlChain)*/Positive, $"POS(MAIN)"));
                    var running = 10 - (10.00 / Positive.MaxChainLength);
                    int sub = 0;
                    foreach (var subchain in Positive.BuildSubChains())
                    {
                        PositiveSources.Add(new Tuple<double, MarkovChain, string>(running, /*MarkovChain.Exclusion(subchain, SubControls[sub])*/subchain, $"POS({sub++})"));
                        running -= (10.00 / Positive.MaxChainLength);
                    }
                    NegitiveSources.Add(new Tuple<double, MarkovChain, string>(10, /*MarkovChain.Exclusion(Negitive, ControlChain)*/Negitive, $"NEG(MAIN)"));
                    running = 10 - (10.00 / Negitive.MaxChainLength);
                    sub = 0;
                    foreach (var subchain in Negitive.BuildSubChains())
                    {
                        NegitiveSources.Add(new Tuple<double, MarkovChain, string>(running, /*MarkovChain.Exclusion(subchain, SubControls[sub])*/subchain, $"NEG({sub++})"));
                        running -= (10.00 / Negitive.MaxChainLength);
                    }
                    /*
                    foreach (var kvpair in SourceFiles)
                    {
                        using (StreamReader read = new StreamReader(new FileStream(kvpair.Key, FileMode.Open)))
                        {
                            if (kvpair.Value > 0)
                            {
                                var learnChain = MarkovChain.ReadFromStream(read);
                                PositiveSources.Add(new Tuple<double, MarkovChain, string>(kvpair.Value, learnChain, kvpair.Key));
                                var running = kvpair.Value - (1.00 / learnChain.MaxChainLength);
                                int sub = 0;
                                foreach(var subchain in learnChain.BuildSubChains())
                                {
                                    PositiveSources.Add(new Tuple<double, MarkovChain, string>(running, learnChain, kvpair.Key + $"(({sub++}))"));
                                    running -= (1.00 / learnChain.MaxChainLength);
                                }
                            }
                            else
                            {
                                var learnChain = MarkovChain.ReadFromStream(read);
                                NegitiveSources.Add(new Tuple<double, MarkovChain, string>(-kvpair.Value, learnChain, kvpair.Key));
                                var running = kvpair.Value + (1.00 / learnChain.MaxChainLength);
                                int sub = 0;
                                foreach (var subchain in learnChain.BuildSubChains())
                                {
                                    NegitiveSources.Add(new Tuple<double, MarkovChain, string>(-running, learnChain, kvpair.Key + $"(({sub++}))"));
                                    running += (1.00 / learnChain.MaxChainLength);
                                }
                            }
                        }
                    }
                    */

                    total += PositiveSources.Sum(SourceTuple =>
                    {
                        var perc = MarkovChain.PercentageMatch(chain, SourceTuple.Item2);
                       // Console.WriteLine(SourceTuple.Item3 + ": " + Math.Round(perc * 100, 2));
                        return SourceTuple.Item1 * perc;
                    }) / PositiveSources.Sum(tuple => tuple.Item1);

                    total -= NegitiveSources.Sum(SourceTuple =>
                    {
                        var perc = MarkovChain.PercentageMatch(chain, SourceTuple.Item2);
                        //Console.WriteLine(SourceTuple.Item3 + ": " + Math.Round(perc * 100, 2));
                        return SourceTuple.Item1 * perc;
                    }) / NegitiveSources.Sum(tuple => tuple.Item1);

                    var name = Path.GetFileNameWithoutExtension(InputBrain);

                    if(CSVContent.ContainsKey(name))
                    {
                        CSVContent[name].Add(total * 100 + "");
                    }
                    else
                    {
                        CSVContent.Add(name, new List<string>() { name, (InputBrain.Contains("left") ? "LEFT" : (InputBrain.Contains("right") ? "RIGHT" : "?")) });
                    }

                    Console.WriteLine((InputBrain.Contains("left") ? "L " : (InputBrain.Contains("right") ? "R " : "  ")) + Path.GetFileName(InputBrain));

                    if ((InputBrain.Contains("left") && total < 0) || (InputBrain.Contains("right") && total > 0))
                        knowFail++;
                    if (Math.Abs(total * 100) < .01)
                    {
                        Console.Out.WriteLine(" TOTAL PERCENTAGE: ~ CENTER");
                    }
                    else
                        Console.Out.WriteLine(" TOTAL PERCENTAGE: %" + Math.Round(Math.Abs(total) * 100, 4) + " " + (total > 0 ? "LEFT" : "RIGHT"));
                }
                using (StreamWriter writer = new StreamWriter(new FileStream("OUT.csv", FileMode.Create)))
                {
                    writer.WriteLine(string.Join(",", CSVHeaders));
                    foreach(var v in CSVContent.Values)
                    {
                        writer.WriteLine(string.Join(",", v));
                    }
                }
                // Trim last comma
                Console.Out.WriteLine("\nFAIL: " + knowFail);
            }
            // Trim last comma
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        public static string PercsToColor(double pos, double neg)
        {
            return "#" + DoubleToColor(pos) + "00" + DoubleToColor(neg);
        }
        public static string DoubleToColor(double val)
        {
            if (val < 0)
                val = -val;
            val *= 255;
            val = Math.Round(val);
            byte b = (byte)val;
            return MapToChar(b / 16) + MapToChar(b % 16);
        }
        public static string MapToChar(int b)
        {
            if (b < 10)
                return "" + b;
            if (b < 16)
                return "" + ((char)('A' + (b - 10)));
            return "?";
        }
    }
}
