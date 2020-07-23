// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Data.Tests
{
    public class DataCommonEventSourceTest
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InvokeCodeThatShouldFirEvents_EnsureEventsFired()
        {
            RemoteExecutor.Invoke(() =>
            {
                using var listener = new TestEventListener("System.Data.DataCommonEventSource", EventLevel.Verbose);
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
                Assert.True(events.Count > 0);
            }).Dispose();
        }
    }
}
