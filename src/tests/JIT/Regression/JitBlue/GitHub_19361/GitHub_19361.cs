// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test exposed a bug in CORINFO_HELP_ASSIGN_BYREF GC kill set on Unix x64.
// that caused segfault.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Repro
{
    [Serializable]
    public struct CompositeSource
    {
        public double? Modifier { get; set; }
        public int InstrumentId { get; set; }
        public string Section { get; set; }

        public bool IsCash { get; set; }
        public bool IsHedge { get; set; }

    }

    public class Test
    {
        private static readonly DateTime BaseDate = new DateTime(2012, 1, 1);

        private readonly Random rng;

        public int Count { get; }

        public Test(Random rng, List<CompositeSource> sources)
        {
            this.rng = rng;
            var holdingsAttribution = this.GetNumbers(sources);
            var hedgeAttribution = this.GetNumbers(sources).Where(x => x.Date >= holdingsAttribution.Min(y => y.Date)).ToList();
            this.Count = hedgeAttribution.Count;
        }

        private List<(DateTime Date, CompositeSource Key, decimal Attribution)> GetNumbers(List<CompositeSource> sources)
        {
            var items = new List<(DateTime Date, CompositeSource Key, decimal Attribution)>();

            foreach (var _ in Enumerable.Range(0, rng.Next(50, 100)))
            {
                items.Add((
                    BaseDate.AddDays(rng.Next(1, 100)),
                    sources[rng.Next(0, sources.Count - 1)],
                    Convert.ToDecimal(rng.NextDouble() * rng.Next(1, 10))));
            }

            return items;
        }
    }

    class Program
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        static readonly Random Rng = new Random(Seed);

        public static List<CompositeSource> GetCompositeSources()
        {

            var list = new List<CompositeSource>();
            foreach (var _ in Enumerable.Range(0, 50))
            {
                lock (Rng)
                {
                    list.Add(new CompositeSource
                    {
                        InstrumentId = 1,
                        IsCash = true,
                        IsHedge = true,
                        Modifier = 0.5,
                        Section = "hello"
                    });
                }

            }

            return list;
        }

        static int Main()
        {
            Console.WriteLine("Starting stress loop");
            var compositeSources = GetCompositeSources();
            var res = Parallel.For(0, 5, i =>
            {
                int seed;
                lock (Rng)
                {
                    seed = Rng.Next();
                }
                Console.WriteLine(new Test(new Random(seed), compositeSources).Count);
            });

            Console.WriteLine("Result: {0}", res.IsCompleted ? "Completed Normally" :
                $"Completed to {res.LowestBreakIteration}");
            return res.IsCompleted ? 100 : -1;
        }
    }
}
