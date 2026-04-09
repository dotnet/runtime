// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Tests
{
    public class DebugConsoleMetricListenerTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerCanResolveLocalMetrics()
        {
            RemoteExecutor.Invoke(() =>
            {
                var services = new ServiceCollection();
                services.AddMetrics(builder =>
                {
                    builder.AddListener<DebugConsoleMetricListener>();
                    builder.EnableMetrics("TestMeter", scopes: MeterScope.Local, listenerName: ConsoleMetrics.DebugListenerName);
                });
                using var sp = services.BuildServiceProvider();
                // Make sure the subscription manager is started.
                sp.GetRequiredService<IStartupValidator>().Validate();

                var listener = sp.GetRequiredService<IMetricsListener>();
                var consoleListener = Assert.IsType<DebugConsoleMetricListener>(listener);
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

                // Meters from the factory can't be disposed, you have to dispose the whole factory.
                sp.Dispose();

                Assert.Equal("TestMeter-counter Started; Description: I count blips." + Environment.NewLine
                    + "TestMeter-counter 4 blip" + Environment.NewLine
                    + "TestMeter-counter 1 blip" + Environment.NewLine
                    + "TestMeter-counter Stopped." + Environment.NewLine, output.ToString());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerCanResolveGlobalMetrics()
        {
            RemoteExecutor.Invoke(() =>
            {
                ServiceCollection services = new ServiceCollection();
                services.AddMetrics(builder =>
                {
                    builder.AddListener<DebugConsoleMetricListener>();
                    builder.EnableMetrics("TestMeter", scopes: MeterScope.Global, listenerName: ConsoleMetrics.DebugListenerName);
                });
                using var sp = services.BuildServiceProvider();
                // Make sure the subscription manager is started.
                sp.GetRequiredService<IStartupValidator>().Validate();

                var listener = sp.GetRequiredService<IMetricsListener>();
                var consoleListener = Assert.IsType<DebugConsoleMetricListener>(listener);
                var output = new StringWriter();
                consoleListener._textWriter = output;

                var meter = new Meter("TestMeter");
                var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
                counter.Add(4);
                counter.Add(1);

                // The rule doesn't match, we shouldn't get this output.
                var negativeMeter = new Meter("NegativeMeter");
                counter = negativeMeter.CreateCounter<int>("counter", "blop", "I count blops");
                counter.Add(1);

                meter.Dispose();

                Assert.Equal("TestMeter-counter Started; Description: I count blips." + Environment.NewLine
                    + "TestMeter-counter 4 blip" + Environment.NewLine
                    + "TestMeter-counter 1 blip" + Environment.NewLine
                    + "TestMeter-counter Stopped." + Environment.NewLine, output.ToString());
            }).Dispose();
        }
    }
}
