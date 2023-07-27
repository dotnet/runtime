// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Tests
{
    public class ConsoleMetricListenerTests
    {
        [Fact]
        public void ListenerCanBeRegisteredViaDi()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMetrics(builder =>
            {
                builder.AddDebugConsole();
                builder.EnableMetrics("TestMeter", null, ConsoleMetrics.ListenerName);
            });
            using var sp = services.BuildServiceProvider();
            sp.GetRequiredService<IMetricsSubscriptionManager>().Start();

            var listener = sp.GetRequiredService<IMetricsListener>();
            var consoleListener = Assert.IsType<ConsoleMetricListener>(listener);
            var output = new StringWriter();
            consoleListener._textWriter = output;

            var factory = sp.GetRequiredService<IMeterFactory>();
            var meter = factory.Create("TestMeter", "version", new TagList() { { "key1", "value1" }, { "key2", "value2" } });
            var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
            counter.Add(1);
            sp.Dispose();

            Assert.Equal("TestMeter-counter Started; Description: I count blips." + Environment.NewLine
                + "TestMeter-counter 1 blip" + Environment.NewLine
                + "TestMeter-counter Stopped." + Environment.NewLine, output.ToString());
        }
    }
}
