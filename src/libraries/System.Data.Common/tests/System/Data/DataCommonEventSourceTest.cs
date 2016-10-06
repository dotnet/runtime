// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Xunit;

namespace System.Data.Tests
{
    public class DataCommonEventSourceTest
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InvokeCodeThatShouldFirEvents_EnsureEventsFired()
        {
            using (var listener = new TestEventListener("System.Data.DataCommonEventSource", EventLevel.Verbose))
            {
                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                listener.RunWithCallback(events.Enqueue, () =>
                {
                    var dt = new DataTable("Players");
                    dt.Columns.Add(new DataColumn("Name", typeof(string)));
                    dt.Columns.Add(new DataColumn("Weight", typeof(int)));

                    var ds = new DataSet();
                    ds.Tables.Add(dt);

                    dt.Rows.Add("John", 150);
                    dt.Rows.Add("Jane", 120);

                    DataRow[] results = dt.Select("Weight < 140");
                    Assert.Equal(1, results.Length);
                });
                Assert.InRange(events.Count, 1, int.MaxValue);
            }
        }
    }
}
