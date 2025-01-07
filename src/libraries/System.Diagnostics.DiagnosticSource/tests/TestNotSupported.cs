// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Diagnostics.Metrics;
using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class MetricsNotSupportedTest
    {
        /// <summary>
        /// Tests using Metrics when the System.Diagnostics.Metrics.Meter.IsSupported
        /// feature switch is set to disable all metrics operations.
        /// </summary>
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void IsSupportedSwitch(bool value)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Diagnostics.Metrics.Meter.IsSupported", value);

            RemoteExecutor.Invoke((val) =>
            {
                bool isSupported = bool.Parse(val);

                Meter meter = new Meter("IsSupportedTest");
                Counter<long> counter = meter.CreateCounter<long>("counter");
                bool instrumentsPublished = false;
                bool instrumentCompleted = false;
                long counterValue = 100;

                using (MeterListener listener = new MeterListener
                {
                    InstrumentPublished = (instruments, theListener) => instrumentsPublished = true,
                    MeasurementsCompleted = (instruments, state) => instrumentCompleted = true
                })
                {
                    listener.EnableMeasurementEvents(counter, null);
                    listener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) => counterValue = measurement);
                    listener.Start();

                    Assert.Equal(isSupported, counter.Enabled);

                    counter.Add(20);
                }
                meter.Dispose();

                Assert.Equal(isSupported, instrumentsPublished);
                Assert.Equal(isSupported, instrumentCompleted);
                Assert.Equal(isSupported ? 20 : 100, counterValue);
            }, value.ToString(), options).Dispose();
        }
    }
}
