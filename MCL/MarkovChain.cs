using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCL
{
    public class MarkovChain
    {
        public int MaxChainLength { get; private set; } = 1;
        static string[] RESET_ON = new string[] { ".", "?", "!" };

        public Dictionary<MarkovPrefix, MarkovChainSegment> Segments { get; private set; }

        public MarkovChain(int chainLength = 2)
        {
            MaxChainLength = chainLength;
            Segments = new Dictionary<MarkovPrefix, MarkovChainSegment>();
        }

        public string Generate(int len)
        {
            string result = "";
            List<string> prefix = new List<string>(MaxChainLength);
            for (var i = 0; i < MaxChainLength; i++)
                prefix.Add(null);
            for(var i = 0; i < len; i++)
            {
                var cmpPrefix = new MarkovPrefix(prefix);
                if(Segments.ContainsKey(cmpPrefix))
                {
                    var match = Segments[cmpPrefix];
                    var str = match.GetRandomPostfix();
                    prefix.RemoveAt(0);
                    prefix.Add(str);
                    result += str + " ";

                    if (RESET_ON.Contains(str))
                    {
                        for (var reset = 0; reset < MaxChainLength; reset++)
                            prefix[reset] = (null);
                    }
                }
                else
                {
                    Console.Out.WriteLine("NULL M:: " + string.Join(",", prefix.Select(prf => @"""" + prf + @"""")));
                }
            }
            return result;
        }

        public void Learn(string[] Tokens)
        {
            var startIndex = 0;
            for(var i = 0; i < Tokens.Length; i++)
            {
                var maxprefix = Tokens.Skip(startIndex).Take(i - startIndex).ToList();
                while(maxprefix.Count() < MaxChainLength)
                {
                    maxprefix.Insert(0, null);
                }
                AddSegment(maxprefix.Reverse<string>().Take(MaxChainLength).Reverse(), Tokens[i]);
                if (RESET_ON.Contains(Tokens[i]))
                {
                    // RESET
                    startIndex = i + 1;
                }

                if(i % 1000 == 0)
                {
                    Console.WriteLine("Progress: " + i);
                }
            }

            AddSegment(Tokens.Reverse().Take(MaxChainLength).Reverse(), RESET_ON[0]);
        }

        public void AddSegment(IEnumerable<string> prefix, string postfix)
        {
            MarkovPrefix prfx = new MarkovPrefix(prefix);
            if (Segments.ContainsKey(prfx))
            {
                Segments[prfx].Add(postfix);
            }
            else
            {
                Segments.Add(prfx, new MarkovChainSegment(postfix));
            }
        }
        
        public void Extend(MarkovChain otherChain)
        {
            foreach(var seg in otherChain.Segments)
            {
                if(Segments.ContainsKey(seg.Key))
                {
                    MarkovChainSegment.Merge(Segments[seg.Key], seg.Value);
                }
                else
                {
                    Segments.Add(seg.Key, seg.Value);
                }
            }
        }

        public IEnumerable<MarkovChain> BuildSubChains()
        {
            var skip = 1;
            for(var length = this.MaxChainLength - 1; length >= 1; length--)
            {
                MarkovChain tempChain = new MarkovChain(length);
                var grp = this.Segments.GroupBy((kv) => new MarkovPrefix(kv.Key.Prefix.Skip(skip)));
                foreach(var SegmentGroup in grp)
                {
                    var prefix = new MarkovPrefix(SegmentGroup.First().Key.Prefix.Skip(skip));
                    MarkovChainSegment newSegment = new MarkovChainSegment("");
                    newSegment.Postfixes.Clear();
                    foreach(var Segment in SegmentGroup)
                    {
                        newSegment = MarkovChainSegment.Merge(newSegment, Segment.Value);
                    }
                    tempChain.Segments.Add(prefix, newSegment);
                }
                yield return tempChain;
                skip++;
            }
        }

        public static void SaveToStream(StreamWriter writer, MarkovChain chain)
        {
            foreach(var segment in chain.Segments)
            {
                writer.WriteLine(string.Join(",", segment.Key.Prefix.Select(x => x == null ? "null" : "\"" + x + "\"")));
                foreach(var postfix in segment.Value.Postfixes)
                {
                    if (postfix.Key.Contains("\n"))
                    {
                        writer.WriteLine(postfix.Value + " .");
                    }
                    else
                    {
                        writer.WriteLine(postfix.Value + " " + postfix.Key);
                    }
                }
                writer.WriteLine("-");
            }
        }
        public MarkovChain Clone()
        {
            MarkovChain tmp = new MarkovChain(MaxChainLength);
            foreach(var seg in Segments)
            {
                tmp.Segments.Add(new MarkovPrefix(seg.Key.Prefix), seg.Value.Clone());
            }
            return tmp;
        }
        public static MarkovChain Exclusion(MarkovChain source, MarkovChain exclude)
        {
            var work = source.Clone();
            foreach(var seg in exclude.Segments)
            {
                if(work.Segments.ContainsKey(seg.Key))
                {
                    var workseg = work.Segments[seg.Key];
                    var excl = seg.Value;

                    foreach(var postfix in excl.Postfixes)
                    {
                        if(workseg.Postfixes.ContainsKey(postfix.Key))
                        {
                            workseg.Postfixes[postfix.Key] -= postfix.Value;
                            if(workseg.Postfixes[postfix.Key] <=  0)
                            {
                                workseg.Postfixes.Remove(postfix.Key);
                            }
                        }
                    }
                    if(workseg.Postfixes.Count == 0)
                    {
                        work.Segments.Remove(seg.Key);
                    }
                }
            }
            return work;
        }
        public static MarkovChain ReadFromStream(StreamReader reader)
        {
            MarkovChain chain = null;
            while(!reader.EndOfStream)
            {
                var prefixline = reader.ReadLine();
                var prefixes = prefixline.Split(',');
                if (chain == null)
                    chain = new MarkovChain(prefixes.Length);
                var prefix = new MarkovPrefix(prefixes.Select(x=>x[0] == '"' ? x.Substring(1, x.Length - 2) : null));

                List<string> Postfixes = new List<string>();
                var nextLine = reader.ReadLine();
                while(nextLine != "-")
                {
                    Postfixes.Add(nextLine);
                    nextLine = reader.ReadLine();
                }
                chain.Segments.Add(prefix, MarkovChainSegment.GenerateSegment(Postfixes));
            }
            return chain;
        }

        public static double PercentageMatch(MarkovChain test, MarkovChain source)
        {
            int totalSegments = test.Segments.Count;
            double perSegment = 1.0 / totalSegments;
            var total = 0.0;
            foreach(var segment in test.Segments)
            {
                var prefix = segment.Key;
                if(source.Segments.ContainsKey(prefix))
                {
                    var sourceSegment = source.Segments[prefix];
                    total += perSegment * MarkovChainSegment.ComparePercentage(sourceSegment, source.Segments[prefix]);
                }
            }
            return total;
        }

        public static IEnumerable<MarkovPrefix> Subprefixes(MarkovPrefix prfx)
        {
            for(var i = 1; i < prfx.Prefix.Length; i++)
            {
                yield return new MarkovPrefix(prfx.Prefix.Skip(i));
            }
        }

        public static double PercentageMatch(MarkovChain test, MarkovChain positive, MarkovChain negitive)
        {
            int totalSegments = test.Segments.Count;
            double perSegment = 1.0 / totalSegments;
            var total = 0.0;
            foreach (var segment in test.Segments)
            {
                var prefix = segment.Key;
                if (positive.Segments.ContainsKey(prefix))
                {
                    var sourceSegment = positive.Segments[prefix];
                    total += perSegment * MarkovChainSegment.ComparePercentage(sourceSegment, positive.Segments[prefix]);
                }
                if (negitive.Segments.ContainsKey(prefix))
                {
                    var sourceSegment = negitive.Segments[prefix];
                    total -= perSegment * MarkovChainSegment.ComparePercentage(sourceSegment, negitive.Segments[prefix]);
                }
            }
            return total;
        }
    }

    public class MarkovPrefix
    {
        public string[] Prefix { get; private set; }

        public MarkovPrefix(IEnumerable<string> prfx)
        {
            Prefix = prfx.ToArray();
            if (Prefix.Length == 0)
                throw new Exception("Don't like that. No sir.");
        }

        public bool PartialMatch(MarkovPrefix oprefix)
        {
            for (var i = 0; i < oprefix.Prefix.Length; i++)
            {
                string left = oprefix.Prefix[i];
                string right = Prefix[i + (Prefix.Length - oprefix.Prefix.Length)];
                if ((left == null || right == null) && !(left == null && right == null))
                {
                    return false;
                }
                else if (!(left == null && right == null) && !left.Equals(right))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashes = this.Prefix.Select(x => (x?.GetHashCode() ?? 0));
            var hash = hashes.First();
            foreach(var nexthash in hashes.Skip(1))
            {
                hash ^= (nexthash);
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            if(obj is MarkovPrefix)
            {
                var oprefix = (MarkovPrefix)obj;
                if (oprefix.Prefix.Length != Prefix.Length)
                    return false;
                for(var i = 0; i < this.Prefix.Length; i++)
                {
                    string left = oprefix.Prefix[i];
                    string right = Prefix[i];
                    if((left == null || right == null) && !(left == null && right == null))
                    {
                        return false;
                    }
                    else if(!(left == null && right == null) && !left.Equals(right))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class MarkovChainSegment
    {
        public static Random random;
        static MarkovChainSegment()
        {
            random = new Random();
        }
        
        public Dictionary<string, int> Postfixes { get; private set; }

        public MarkovChainSegment(string postfix)
        {
            Postfixes = new Dictionary<string, int>();
            Postfixes.Add(postfix, 1);
        }

        public MarkovChainSegment Clone()
        {
            MarkovChainSegment tmp = new MarkovChainSegment("");
            tmp.Postfixes.Clear();
            foreach(var Postfix in Postfixes)
            {
                tmp.Postfixes.Add(Postfix.Key, Postfix.Value);
            }
            return tmp;
        }

        public string GetRandomPostfix()
        {
            int totalWeight = Postfixes.Sum(x => x.Value);
            var i = random.Next(totalWeight);
            foreach(var pair in Postfixes)
            {
                if(i < pair.Value)
                {
                    return pair.Key;
                }
                i -= pair.Value;
            }
            return "GET_RANDOM_ERROR_VALUE_OOR";
        }
        

        public void Add(string postfix)
        {
            if (Postfixes.ContainsKey(postfix))
                Postfixes[postfix]++;
            else
                Postfixes.Add(postfix, 1);
        }

        public static MarkovChainSegment GenerateSegment(List<string> postfixes)
        {
            var tuples = postfixes.Select(x => new Tuple<int, string>(int.Parse(x.Substring(0, x.IndexOf(' '))), x.Substring(x.IndexOf(' ') + 1))).ToArray();
            MarkovChainSegment segment = new MarkovChainSegment(tuples[0].Item2);
            segment.Postfixes.Clear();
            foreach(var tup in tuples)
            {
                segment.Postfixes.Add(tup.Item2, tup.Item1);
            }
            return segment;
        }

        public static double ComparePercentage(MarkovChainSegment test, MarkovChainSegment source)
        {
            var perTest = 1.0 / test.Postfixes.Sum(x => x.Value);

            var total = 0;
            foreach(string postfix in test.Postfixes.Keys)
            {
                if(source.Postfixes.ContainsKey(postfix))
                {
                    total += test.Postfixes[postfix];
                }
            }
            return perTest * total;
        }

        public static MarkovChainSegment Merge(MarkovChainSegment mergeInto, MarkovChainSegment data)
        {
            foreach(var seg in data.Postfixes)
            {
                if(mergeInto.Postfixes.ContainsKey(seg.Key))
                {
                    mergeInto.Postfixes[seg.Key] += seg.Value;
                }
                else
                {
                    mergeInto.Postfixes.Add(seg.Key, seg.Value);
                }
            }
            return mergeInto;
        }
    }
}
