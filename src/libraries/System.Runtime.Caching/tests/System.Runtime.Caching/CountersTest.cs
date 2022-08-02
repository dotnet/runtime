// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Xunit;
using MonoTests.Common;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Diagnostics;

namespace MonoTests.System.Runtime.Caching
{
    public struct MemoryCacheCounterValues
    {
        public int Entries;
        public int Hits;
        public double HitRatio;
        public int Misses;
        public int Trims;
        public int TurnoverRate;
    }

    public class CountersTest
    {
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Wasm is single-threaded, which makes TestEventListener ineffective.")]
        public async void Basic_Counters()
        {
            string cacheName = "Basic_Counters_Test";
            var cip = new CacheItemPolicy() { /* _absExpiry = ObjectCache.InfiniteAbsoluteExpiration */ };

            using (var poker = new PokerMemoryCache(cacheName))
            {
                poker.Add("name1", "val1", cip);
                poker.Add("name2", "val2", cip);
                poker.Add("name3", "val3", cip);
                poker.Add("name4", "val4", cip);

                poker["name1"] = "first_value";

                var thing1 = poker["name3"];
                var thing2 = poker["name2"];
                var nothing = poker["anything"];

                var counters = await PollCounters(cacheName);

                Assert.Equal(4, counters.Entries);
                Assert.Equal(2, counters.Hits);
                Assert.True(counters.HitRatio > 28);    // 2/(2+5)
                Assert.True(counters.HitRatio < 29);
                Assert.Equal(5, counters.Misses);
                Assert.Equal(0, counters.Trims);
            }
        }

        //
        // NOTE: This polling method only works in this perfcounter-like style
        //  because we are either reading FullFx perfcounters, or because the
        //  counters we emit in .Net Core are PollingCounters. For regular event
        //  counters, we'd have to have a listener running during the test instead
        //  of polling afterwards. Also, the incrementing counters (that give an
        //  increment or 'rate' on each reading) should also have a listener running
        //  during the test, or else the counter will never increment once we start
        //  listening, so we will have a 0 increment/rate for each reading.
        //
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<MemoryCacheCounterValues> PollCounters(string cacheName)
#pragma warning restore CS1998 // This runs synchronously on 48 and that's ok.
        {
            MemoryCacheCounterValues counters = new MemoryCacheCounterValues();

#if NETCOREAPP
            var events = new ConcurrentQueue<EventWrittenEventArgs>();
            using (var listener = new TestEventListener("System.Runtime.Caching." + cacheName, EventLevel.Verbose, eventCounterInterval: 0.1d))
            {
                // Following the example from System.Net.Http's telemetry test where they are also
                // trying to "poll" some PollingCounters.
                await listener.RunWithCallbackAsync(events.Enqueue, async () => await WaitForEventCountersAsync(events));
            }

            Dictionary<string, double[]> eventCounters = events
                .Where(e => e.EventName == "EventCounters")
                .Select(e => (IDictionary<string, object>)e.Payload.Single())
                .GroupBy(d => (string)d["Name"], d => (double)(d.ContainsKey("Mean") ? d["Mean"] : d["Increment"]))
                .ToDictionary(p => p.Key, p => p.ToArray());

            // Take the first value since this is an explicit "poll" method. We want to know
            // what the values are when we poll. (Also, 'incrementing' counters like turnover rate
            // will only return something meaninful in the first position.)
            Assert.True(eventCounters.TryGetValue("entries", out double[] entries));
            counters.Entries = (int)entries[0];
            Assert.True(eventCounters.TryGetValue("hits", out double[] hits));
            counters.Hits = (int)hits[0];
            Assert.True(eventCounters.TryGetValue("hit-ratio", out double[] hitratio));
            counters.HitRatio = hitratio[0];
            Assert.True(eventCounters.TryGetValue("misses", out double[] misses));
            counters.Misses = (int)misses[0];
            Assert.True(eventCounters.TryGetValue("trims", out double[] trims));
            counters.Trims = (int)trims[0];
            Assert.True(eventCounters.TryGetValue("turnover", out double[] turnover));
            counters.TurnoverRate = (int)turnover[0];

#elif NETFRAMEWORK
            string instanceName = null;
            var pcc = new PerformanceCounterCategory(".NET Memory Cache 4.0");
            foreach (string name in pcc.GetInstanceNames())
            {
                if (name.EndsWith(":" + cacheName, StringComparison.OrdinalIgnoreCase))
                {
                    instanceName = name;
                    break;
                }
            }

            PerformanceCounter pc = new PerformanceCounter(".NET Memory Cache 4.0", "Cache Entries", instanceName);
            counters.Entries = (int)pc.NextValue();
            pc.CounterName = "Cache Hits";
            counters.Hits = (int)pc.NextValue();
            pc.CounterName = "Cache Misses";
            counters.Misses = (int)pc.NextValue();
            pc.CounterName = "Cache Hit Ratio";
            counters.HitRatio = pc.NextValue();
            pc.CounterName = "Cache Trims";
            counters.Trims = (int)pc.NextValue();
            pc.CounterName = "Cache Turnover Rate";
            counters.TurnoverRate = (int)pc.NextValue();
            pc.Dispose();
#endif

            return counters;
        }

#if NETCOREAPP
        private static async Task WaitForEventCountersAsync(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            DateTime startTime = DateTime.UtcNow;
            int startCount = events.Count;
            int numberOfDistinctCounters = 6;

            while (events.Skip(startCount).Where(e => e.EventName == "EventCounters").Select(e => GetCounterName(e)).Distinct().Count() != numberOfDistinctCounters)
            {
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException($"Timed out waiting for EventCounters");

                await Task.Delay(100);
            }

            static string GetCounterName(EventWrittenEventArgs e)
            {
                var dictionary = (IDictionary<string, object>)e.Payload.Single();

                return (string)dictionary["Name"];
            }
        }
#endif
    }
}
