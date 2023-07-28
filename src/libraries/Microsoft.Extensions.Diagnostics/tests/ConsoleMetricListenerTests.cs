// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Tests
{
    public class ConsoleMetricListenerTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerCanBeRegisteredViaDi()
        {
            RemoteExecutor.Invoke(() =>
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
                var meter = factory.Create("TestMeter");
                var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
                counter.Add(4);
                counter.Add(1);

                // The rule doesn't match, we shouldn't get this output.
                var negativeMeter = factory.Create("NegativeMeter");
                counter = negativeMeter.CreateCounter<int>("counter", "blop", "I count blops");
                counter.Add(1);

                sp.Dispose(); // TODO: Why did we have to do this to get the Stopped message? Why doesn't disposing of the meter do it?

                Assert.Equal("TestMeter-counter Started; Description: I count blips." + Environment.NewLine
                    + "TestMeter-counter 4 blip" + Environment.NewLine
                    + "TestMeter-counter 1 blip" + Environment.NewLine
                    + "TestMeter-counter Stopped." + Environment.NewLine, output.ToString());
            }).Dispose();
        }
    }
}
