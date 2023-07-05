// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class MetricsTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MeterConstructionTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("meter1");
                Assert.Equal("meter1", meter.Name);
                Assert.Null(meter.Version);

                meter = new Meter("meter2", "v1.0");
                Assert.Equal("meter2", meter.Name);
                Assert.Equal("v1.0", meter.Version);

                Assert.Throws<ArgumentNullException>(() => new Meter(name: null));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void InstrumentCreationTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("InstrumentCreationTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter", "seconds", "Seconds Counter");
                ValidateInstrumentInfo(counter, "Counter", "seconds", "Seconds Counter", false, false);

                UpDownCounter<int> upDownCounter = meter.CreateUpDownCounter<int>("UpDownCounter", "seconds", "Seconds UpDownCounter");
                ValidateInstrumentInfo(upDownCounter, "UpDownCounter", "seconds", "Seconds UpDownCounter", false, false);

                Histogram<float> histogram = meter.CreateHistogram<float>("Histogram", "centimeters", "centimeters Histogram");
                ValidateInstrumentInfo(histogram, "Histogram", "centimeters", "centimeters Histogram", false, false);

                ObservableCounter<long> observableCounter = meter.CreateObservableCounter<long>("ObservableCounter", () => 10, "millisecond", "millisecond ObservableCounter");
                ValidateInstrumentInfo(observableCounter, "ObservableCounter", "millisecond", "millisecond ObservableCounter", false, true);

                ObservableUpDownCounter<long> observableUpDownCounter = meter.CreateObservableUpDownCounter<long>("ObservableUpDownCounter", () => -1, "request", "request ObservableUpDownCounter");
                ValidateInstrumentInfo(observableUpDownCounter, "ObservableUpDownCounter", "request", "request ObservableUpDownCounter", false, true);

                ObservableGauge<double> observableGauge = meter.CreateObservableGauge<double>("ObservableGauge", () => 10, "Fahrenheit", "Fahrenheit ObservableGauge");
                ValidateInstrumentInfo(observableGauge, "ObservableGauge", "Fahrenheit", "Fahrenheit ObservableGauge", false, true);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CreateInstrumentParametersTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("CreateInstrumentParametersTest");

                Assert.Throws<ArgumentNullException>(() => meter.CreateCounter<byte>(null, "seconds", "Seconds Counter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateUpDownCounter<float>(null, "items", "Items UpDownCounter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateHistogram<short>(null, "seconds", "Seconds Counter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateObservableCounter<long>(null, () => 0, "seconds", "Seconds ObservableCounter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateObservableUpDownCounter<Decimal>(null, () => 0, "items", "Items ObservableUpDownCounter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateObservableGauge<double>(null, () => 0, "seconds", "Seconds ObservableGauge"));

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SupportedGenericParameterTypesTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("SupportedGenericParameterTypesTest");

                Counter<byte> counter1 = meter.CreateCounter<byte>("Counter1", "seconds", "Seconds Counter");
                Counter<short> counter2 = meter.CreateCounter<short>("Counter2", "seconds", "Seconds Counter");
                Counter<int> counter3 = meter.CreateCounter<int>("Counter3", "seconds", "Seconds Counter");
                Counter<long> counter4 = meter.CreateCounter<long>("Counter4", "seconds", "Seconds Counter");
                Counter<float> counter5 = meter.CreateCounter<float>("Counter5", "seconds", "Seconds Counter");
                Counter<double> counter6 = meter.CreateCounter<double>("Counter6", "seconds", "Seconds Counter");
                Counter<decimal> counter7 = meter.CreateCounter<decimal>("Counter7", "seconds", "Seconds Counter");

                UpDownCounter<byte> upDownCounter1 = meter.CreateUpDownCounter<byte>("UpDownCounter1", "seconds", "Seconds UpDownCounter");
                UpDownCounter<short> upDownCounter2 = meter.CreateUpDownCounter<short>("UpDownCounter2", "seconds", "Seconds Counter");
                UpDownCounter<int> upDownCounter3 = meter.CreateUpDownCounter<int>("UpDownCounter3", "seconds", "Seconds UpDownCounter");
                UpDownCounter<long> upDownCounter4 = meter.CreateUpDownCounter<long>("UpDownCounter4", "seconds", "Seconds UpDownCounter");
                UpDownCounter<float> upDownCounter5 = meter.CreateUpDownCounter<float>("UpDownCounter5", "seconds", "Seconds UpDownCounter");
                UpDownCounter<double> upDownCounter6 = meter.CreateUpDownCounter<double>("UpDownCounter6", "seconds", "Seconds UpDownCounter");
                UpDownCounter<decimal> upDownCounter7 = meter.CreateUpDownCounter<decimal>("UpDownCounter7", "seconds", "Seconds UpDownCounter");

                Histogram<byte> histogram1 = meter.CreateHistogram<byte>("histogram1", "seconds", "Seconds histogram");
                Histogram<short> histogram2 = meter.CreateHistogram<short>("histogram2", "seconds", "Seconds histogram");
                Histogram<int> histogram3 = meter.CreateHistogram<int>("histogram3", "seconds", "Seconds histogram");
                Histogram<long> histogram4 = meter.CreateHistogram<long>("histogram4", "seconds", "Seconds histogram");
                Histogram<float> histogram5 = meter.CreateHistogram<float>("histogram5", "seconds", "Seconds histogram");
                Histogram<double> histogram6 = meter.CreateHistogram<double>("histogram6", "seconds", "Seconds histogram");
                Histogram<decimal> histogram7 = meter.CreateHistogram<decimal>("histogram7", "seconds", "Seconds histogram");

                ObservableCounter<byte> observableCounter1 = meter.CreateObservableCounter<byte>("observableCounter1", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<short> observableCounter2 = meter.CreateObservableCounter<short>("observableCounter2", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<int> observableCounter3 = meter.CreateObservableCounter<int>("observableCounter3", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<long> observableCounter4 = meter.CreateObservableCounter<long>("observableCounter4", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<float> observableCounter5 = meter.CreateObservableCounter<float>("observableCounter5", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<double> observableCounter6 = meter.CreateObservableCounter<double>("observableCounter6", () => 0, "seconds", "Seconds ObservableCounter");
                ObservableCounter<decimal> observableCounter7 = meter.CreateObservableCounter<decimal>("observableCounter7", () => 0, "seconds", "Seconds ObservableCounter");

                ObservableUpDownCounter<byte> observableUpDownCounter1 = meter.CreateObservableUpDownCounter<byte>("observableUpDownCounter1", () => 0, "items", "Items ObservableUpDownCounter");
                ObservableUpDownCounter<short> observableUpDownCounter2 = meter.CreateObservableUpDownCounter<short>("observableUpDownCounter2", () => 0, "items", "Items ObservableCounter");
                ObservableUpDownCounter<int> observableUpDownCounter3 = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter3", () => 0, "items", "Items ObservableUpDownCounter");
                ObservableUpDownCounter<long> observableUpDownCounter4 = meter.CreateObservableUpDownCounter<long>("observableUpDownCounter4", () => 0, "items", "Items ObservableUpDownCounter");
                ObservableUpDownCounter<float> observableUpDownCounter5 = meter.CreateObservableUpDownCounter<float>("observableUpDownCounter5", () => 0, "items", "Items ObservableUpDownCounter");
                ObservableUpDownCounter<double> observableUpDownCounter6 = meter.CreateObservableUpDownCounter<double>("observableUpDownCounter6", () => 0, "items", "Items ObservableUpDownCounter");
                ObservableUpDownCounter<decimal> observableUpDownCounter7 = meter.CreateObservableUpDownCounter<decimal>("observableUpDownCounter7", () => 0, "items", "Items ObservableUpDownCounter");

                ObservableGauge<byte> observableGauge1 = meter.CreateObservableGauge<byte>("observableGauge1", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<short> observableGauge2 = meter.CreateObservableGauge<short>("observableGauge2", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<int> observableGauge3 = meter.CreateObservableGauge<int>("observableGauge3", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<long> observableGauge4 = meter.CreateObservableGauge<long>("observableGauge4", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<float> observableGauge5 = meter.CreateObservableGauge<float>("observableGauge5", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<double> observableGauge6 = meter.CreateObservableGauge<double>("observableGauge6", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<decimal> observableGauge7 = meter.CreateObservableGauge<decimal>("observableGauge7", () => 0, "seconds", "Seconds ObservableGauge");

                Assert.Throws<InvalidOperationException>(() => meter.CreateCounter<uint>("Counter", "seconds", "Seconds Counter"));
                Assert.Throws<InvalidOperationException>(() => meter.CreateUpDownCounter<uint>("UpDownCounter", "items", "Items Counter"));
                Assert.Throws<InvalidOperationException>(() => meter.CreateHistogram<ulong>("histogram1", "seconds", "Seconds histogram"));
                Assert.Throws<InvalidOperationException>(() => meter.CreateObservableCounter<sbyte>("observableCounter3", () => 0, "seconds", "Seconds ObservableCounter"));
                Assert.Throws<InvalidOperationException>(() => meter.CreateObservableGauge<ushort>("observableGauge7", () => 0, "seconds", "Seconds ObservableGauge"));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListeningToInstrumentsPublishingTest()
        {
            RemoteExecutor.Invoke(() => {

                Meter meter = new Meter("ListeningToInstrumentsPublishingTest");

                int instrumentsEncountered = 0;
                using (MeterListener listener = new MeterListener())
                {
                    Counter<long> counter = meter.CreateCounter<long>("Counter4", "seconds", "Seconds Counter");
                    ObservableGauge<byte> observableGauge = meter.CreateObservableGauge<byte>("observableGauge1", () => 0, "seconds", "Seconds ObservableGauge");

                    // Listener is not enabled yet
                    Assert.Equal(0, instrumentsEncountered);

                    listener.InstrumentPublished = (instruments, theListener) => instrumentsEncountered++;

                    // Listener still not started yet
                    Assert.Equal(0, instrumentsEncountered);

                    listener.Start();

                    Assert.Equal(2, instrumentsEncountered);

                    Histogram<byte> histogram = meter.CreateHistogram<byte>("histogram1", "seconds", "Seconds histogram");
                    ObservableCounter<byte> observableCounter = meter.CreateObservableCounter<byte>("observableCounter1", () => 0, "seconds", "Seconds ObservableCounter");
                    UpDownCounter<long> upDownCounter = meter.CreateUpDownCounter<long>("UpDownCounter4", "request", "Requests UpDownCounter");
                    ObservableUpDownCounter<byte> observableUpDownCounter = meter.CreateObservableUpDownCounter<byte>("observableUpDownCounter1", () => 0, "items", "Items ObservableCounter");

                    Assert.Equal(6, instrumentsEncountered);

                    // Enable listening to the 4 instruments

                    listener.EnableMeasurementEvents(counter, counter);
                    listener.EnableMeasurementEvents(observableGauge, observableGauge);
                    listener.EnableMeasurementEvents(histogram, histogram);
                    listener.EnableMeasurementEvents(observableCounter, observableCounter);
                    listener.EnableMeasurementEvents(upDownCounter, upDownCounter);
                    listener.EnableMeasurementEvents(observableUpDownCounter, observableUpDownCounter);

                    // Enable listening to instruments unpublished event
                    listener.MeasurementsCompleted = (instruments, state) => { instrumentsEncountered--; Assert.Same(state, instruments); };

                    // Should fire all MeasurementsCompleted event for all instruments in the Meter
                    meter.Dispose();

                    // MeasurementsCompleted should be called 4 times for every instrument.
                    Assert.Equal(0, instrumentsEncountered);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ThrowingExceptionsFromObservableInstrumentCallbacks()
        {
            RemoteExecutor.Invoke(() => {
                using Meter meter = new Meter("ThrowingExceptionsFromObservableInstrumentCallbacks");

                using (MeterListener listener = new MeterListener())
                {
                    ObservableCounter<int>  counter1 = meter.CreateObservableCounter<int>("observableCounter1", (Func<int>) (() => throw new ArgumentOutOfRangeException()));
                    ObservableGauge<int>    gauge1   = meter.CreateObservableGauge<int>("observableGauge1", (Func<int>)(() => throw new ArgumentException()));
                    ObservableCounter<int>  counter2 = meter.CreateObservableCounter<int>("observableCounter2", (Func<int>)(() => throw new PlatformNotSupportedException()));
                    ObservableGauge<int>    gauge2   = meter.CreateObservableGauge<int>("observableGauge2", (Func<int>)(() => throw new NullReferenceException()));
                    ObservableUpDownCounter<int>  upDownCounterCounter1 = meter.CreateObservableUpDownCounter<int>("upDownCounter1", (Func<int>) (() => throw new ArgumentOutOfRangeException()));
                    ObservableUpDownCounter<int>  upDownCounterCounter2 = meter.CreateObservableUpDownCounter<int>("upDownCounter2", (Func<int>) (() => throw new PlatformNotSupportedException()));
                    ObservableCounter<int>  counter3 = meter.CreateObservableCounter<int>("observableCounter3", () => 5);
                    ObservableGauge<int>    gauge3   = meter.CreateObservableGauge<int>("observableGauge3", () => 7);
                    ObservableUpDownCounter<int>  upDownCounterCounter3 = meter.CreateObservableUpDownCounter<int>("ObservableUpDownCounter3", () => -1);

                    listener.EnableMeasurementEvents(counter1, null);
                    listener.EnableMeasurementEvents(gauge1, null);
                    listener.EnableMeasurementEvents(counter2, null);
                    listener.EnableMeasurementEvents(gauge2, null);
                    listener.EnableMeasurementEvents(upDownCounterCounter1, null);
                    listener.EnableMeasurementEvents(upDownCounterCounter2, null);
                    listener.EnableMeasurementEvents(counter3, null);
                    listener.EnableMeasurementEvents(gauge3, null);
                    listener.EnableMeasurementEvents(upDownCounterCounter3, null);

                    int accumulated = 0;

                    listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => accumulated += measurement);

                    Exception exception = Record.Exception(() => listener.RecordObservableInstruments());
                    Assert.NotNull(exception);
                    Assert.IsType<AggregateException>(exception);
                    AggregateException ae = exception as AggregateException;
                    Assert.Equal(6, ae.InnerExceptions.Count);

                    Assert.IsType<ArgumentOutOfRangeException>(ae.InnerExceptions[0]);
                    Assert.IsType<ArgumentException>(ae.InnerExceptions[1]);
                    Assert.IsType<PlatformNotSupportedException>(ae.InnerExceptions[2]);
                    Assert.IsType<NullReferenceException>(ae.InnerExceptions[3]);
                    Assert.IsType<ArgumentOutOfRangeException>(ae.InnerExceptions[4]);
                    Assert.IsType<PlatformNotSupportedException>(ae.InnerExceptions[5]);

                    // Ensure the instruments which didn't throw reported correct measurements.
                    Assert.Equal(11, accumulated);
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void InstrumentMeasurementTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("InstrumentMeasurementTest");

                Counter<byte> counter = meter.CreateCounter<byte>("byteCounter");
                InstrumentMeasurementAggregationValidation(counter, (value, tags) => { counter.Add(value, tags); } );

                UpDownCounter<byte> upDownCounter = meter.CreateUpDownCounter<byte>("byteUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter, (value, tags) => { upDownCounter.Add(value, tags); });

                Counter<short> counter1 = meter.CreateCounter<short>("shortCounter");
                InstrumentMeasurementAggregationValidation(counter1, (value, tags) => { counter1.Add(value, tags); } );

                UpDownCounter<short> upDownCounter1 = meter.CreateUpDownCounter<short>("shortUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter1, (value, tags) => { upDownCounter1.Add(value, tags); }, true);

                Counter<int> counter2 = meter.CreateCounter<int>("intCounter");
                InstrumentMeasurementAggregationValidation(counter2, (value, tags) => { counter2.Add(value, tags); } );

                UpDownCounter<int> upDownCounter2 = meter.CreateUpDownCounter<int>("intUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter2, (value, tags) => { upDownCounter2.Add(value, tags); }, true);

                Counter<long> counter3 = meter.CreateCounter<long>("longCounter");
                InstrumentMeasurementAggregationValidation(counter3, (value, tags) => { counter3.Add(value, tags); } );

                UpDownCounter<long> upDownCounter3 = meter.CreateUpDownCounter<long>("longUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter3, (value, tags) => { upDownCounter3.Add(value, tags); }, true);

                Counter<float> counter4 = meter.CreateCounter<float>("floatCounter");
                InstrumentMeasurementAggregationValidation(counter4, (value, tags) => { counter4.Add(value, tags); } );

                UpDownCounter<float> upDownCounter4 = meter.CreateUpDownCounter<float>("floatUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter4, (value, tags) => { upDownCounter4.Add(value, tags); }, true);

                Counter<double> counter5 = meter.CreateCounter<double>("doubleCounter");
                InstrumentMeasurementAggregationValidation(counter5, (value, tags) => { counter5.Add(value, tags); } );

                UpDownCounter<double> upDownCounter5 = meter.CreateUpDownCounter<double>("doubleUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter5, (value, tags) => { upDownCounter5.Add(value, tags); }, true);

                Counter<decimal> counter6 = meter.CreateCounter<decimal>("decimalCounter");
                InstrumentMeasurementAggregationValidation(counter6, (value, tags) => { counter6.Add(value, tags); } );

                UpDownCounter<decimal> upDownCounter6 = meter.CreateUpDownCounter<decimal>("decimalUpDownCounter");
                InstrumentMeasurementAggregationValidation(upDownCounter6, (value, tags) => { upDownCounter6.Add(value, tags); }, true);

                Histogram<byte> histogram = meter.CreateHistogram<byte>("byteHistogram");
                InstrumentMeasurementAggregationValidation(histogram, (value, tags) => { histogram.Record(value, tags); } );

                Histogram<short> histogram1 = meter.CreateHistogram<short>("shortHistogram");
                InstrumentMeasurementAggregationValidation(histogram1, (value, tags) => { histogram1.Record(value, tags); } );

                Histogram<int> histogram2 = meter.CreateHistogram<int>("intHistogram");
                InstrumentMeasurementAggregationValidation(histogram2, (value, tags) => { histogram2.Record(value, tags); } );

                Histogram<long> histogram3 = meter.CreateHistogram<long>("longHistogram");
                InstrumentMeasurementAggregationValidation(histogram3, (value, tags) => { histogram3.Record(value, tags); } );

                Histogram<float> histogram4 = meter.CreateHistogram<float>("floatHistogram");
                InstrumentMeasurementAggregationValidation(histogram4, (value, tags) => { histogram4.Record(value, tags); } );

                Histogram<double> histogram5 = meter.CreateHistogram<double>("doubleHistogram");
                InstrumentMeasurementAggregationValidation(histogram5, (value, tags) => { histogram5.Record(value, tags); } );

                Histogram<decimal> histogram6 = meter.CreateHistogram<decimal>("decimalHistogram");
                InstrumentMeasurementAggregationValidation(histogram6, (value, tags) => { histogram6.Record(value, tags); } );

            }).Dispose();
        }


        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ObservableInstrumentMeasurementTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("ObservableInstrumentMeasurementTest");

                //
                // CreateObservableCounter using Func<T>
                //
                ObservableCounter<byte> observableCounter = meter.CreateObservableCounter<byte>("ByteObservableCounter", () => 50);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter, new Measurement<byte>[] { new Measurement<byte>(50)});
                ObservableCounter<short> observableCounter1 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => 30_000);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter1, new Measurement<short>[] { new Measurement<short>(30_000)});
                ObservableCounter<int> observableCounter2 = meter.CreateObservableCounter<int>("IntObservableCounter", () => 1_000_000);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter2, new Measurement<int>[] { new Measurement<int>(1_000_000)});
                ObservableCounter<long> observableCounter3 = meter.CreateObservableCounter<long>("longObservableCounter", () => 1_000_000_000);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter3, new Measurement<long>[] { new Measurement<long>(1_000_000_000)});
                ObservableCounter<float> observableCounter4 = meter.CreateObservableCounter<float>("floatObservableCounter", () => 3.14f);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter4, new Measurement<float>[] { new Measurement<float>(3.14f)});
                ObservableCounter<double> observableCounter5 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => 1e6);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter5, new Measurement<double>[] { new Measurement<double>(1e6)});
                ObservableCounter<decimal> observableCounter6 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => 1.5E6m);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter6, new Measurement<decimal>[] { new Measurement<decimal>(1.5E6m)});

                //
                // CreateObservableUpDownCounter using Func<T>
                //
                ObservableUpDownCounter<byte> observableUpDownCounter = meter.CreateObservableUpDownCounter<byte>("ByteObservableUpDownCounter", () => 10);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter, new Measurement<byte>[] { new Measurement<byte>(10)});
                ObservableUpDownCounter<short> observableUpDownCounter1 = meter.CreateObservableUpDownCounter<short>("shortObservableUpDownCounter", () => -10);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter1, new Measurement<short>[] { new Measurement<short>(-10)});
                ObservableUpDownCounter<int> observableUpDownCounter2 = meter.CreateObservableUpDownCounter<int>("intObservableUpDownCounter", () => -12);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter2, new Measurement<int>[] { new Measurement<int>(-12)});
                ObservableUpDownCounter<long> observableUpDownCounter3 = meter.CreateObservableUpDownCounter<long>("longObservableUpDownCounter", () => -100);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter3, new Measurement<long>[] { new Measurement<long>(-100)});
                ObservableUpDownCounter<float> observableUpDownCounter4 = meter.CreateObservableUpDownCounter<float>("floatObservableUpDownCounter", () => -3.4f);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter4, new Measurement<float>[] { new Measurement<float>(-3.4f)});
                ObservableUpDownCounter<double> observableUpDownCounter5 = meter.CreateObservableUpDownCounter<double>("doubleObservableUpDownCounter", () => -3.14);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter5, new Measurement<double>[] { new Measurement<double>(-3.14)});
                ObservableUpDownCounter<decimal> observableUpDownCounter6 = meter.CreateObservableUpDownCounter<decimal>("doubleObservableUpDownCounter", () => -32222.14m);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter6, new Measurement<decimal>[] { new Measurement<decimal>(-32222.14m)});

                //
                // CreateObservableGauge using Func<T>
                //
                ObservableGauge<byte> observableGauge = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => 100);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge, new Measurement<byte>[] { new Measurement<byte>(100)});
                ObservableGauge<short> observableGauge1 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => 30_123);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge1, new Measurement<short>[] { new Measurement<short>(30_123)});
                ObservableGauge<int> observableGauge2 = meter.CreateObservableGauge<int>("IntObservableGauge", () => 2_123_456);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge2, new Measurement<int>[] { new Measurement<int>(2_123_456)});
                ObservableGauge<long> observableGauge3 = meter.CreateObservableGauge<long>("longObservableGauge", () => 3_123_456_789);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge3, new Measurement<long>[] { new Measurement<long>(3_123_456_789)});
                ObservableGauge<float> observableGauge4 = meter.CreateObservableGauge<float>("floatObservableGauge", () => 1.6f);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge4, new Measurement<float>[] { new Measurement<float>(1.6f)});
                ObservableGauge<double> observableGauge5 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => 1e5);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge5, new Measurement<double>[] { new Measurement<double>(1e5)});
                ObservableGauge<decimal> observableGauge6 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => 2.5E7m);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge6, new Measurement<decimal>[] { new Measurement<decimal>(2.5E7m)});

                //
                // CreateObservableCounter using Func<Measurement<T>>
                //
                Measurement<byte> byteMeasurement = new Measurement<byte>(60, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T1", "V1"), new KeyValuePair<string, object?>("T2", "V2") });
                ObservableCounter<byte> observableCounter7 = meter.CreateObservableCounter<byte>("ByteObservableCounter", () => byteMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter7, new Measurement<byte>[] { byteMeasurement });

                Measurement<short> shortMeasurement = new Measurement<short>(20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T3", "V3"), new KeyValuePair<string, object?>("T4", "V4") });
                ObservableCounter<short> observableCounter8 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => shortMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter8, new Measurement<short>[] { shortMeasurement });

                Measurement<int> intMeasurement = new Measurement<int>(2_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T5", "V5"), new KeyValuePair<string, object?>("T6", "V6") });
                ObservableCounter<int> observableCounter9 = meter.CreateObservableCounter<int>("IntObservableCounter", () => intMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter9, new Measurement<int>[] { intMeasurement });

                Measurement<long> longMeasurement = new Measurement<long>(20_000_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T7", "V7"), new KeyValuePair<string, object?>("T8", "V8") });
                ObservableCounter<long> observableCounter10 = meter.CreateObservableCounter<long>("longObservableCounter", () => longMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter10, new Measurement<long>[] { longMeasurement });

                Measurement<float> floatMeasurement = new Measurement<float>(1e2f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T9", "V10"), new KeyValuePair<string, object?>("T11", "V12") });
                ObservableCounter<float> observableCounter11 = meter.CreateObservableCounter<float>("floatObservableCounter", () => 3.14f);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter11, new Measurement<float>[] { new Measurement<float>(3.14f)});

                Measurement<double> doubleMeasurement = new Measurement<double>(2.5e7, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T13", "V14"), new KeyValuePair<string, object?>("T15", "V16") });
                ObservableCounter<double> observableCounter12 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => doubleMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter12, new Measurement<double>[] { doubleMeasurement });

                Measurement<decimal> decimalMeasurement = new Measurement<decimal>(3.2e20m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T17", "V18"), new KeyValuePair<string, object?>("T19", "V20") });
                ObservableCounter<decimal> observableCounter13 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => decimalMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter13, new Measurement<decimal>[] { decimalMeasurement });

                //
                // CreateObservableUpDownCounter using Func<Measurement<T>>
                //
                Measurement<byte> byteMeasurement1 = new Measurement<byte>(100, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T1", "V1"), new KeyValuePair<string, object?>("T2", "V2") });
                ObservableUpDownCounter<byte> observableUpDownCounter7 = meter.CreateObservableUpDownCounter<byte>("ByteObservableUpDownCounter", () => byteMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter7, new Measurement<byte>[] { byteMeasurement1 });

                Measurement<short> shortMeasurement1 = new Measurement<short>(-20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T3", "V3"), new KeyValuePair<string, object?>("T4", "V4") });
                ObservableUpDownCounter<short> observableUpDownCounter8 = meter.CreateObservableUpDownCounter<short>("ShortObservableUpDownCounter", () => shortMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter8, new Measurement<short>[] { shortMeasurement1 });

                Measurement<int> intMeasurement1 = new Measurement<int>(-2_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T5", "V5"), new KeyValuePair<string, object?>("T6", "V6") });
                ObservableUpDownCounter<int> observableUpDownCounter9 = meter.CreateObservableUpDownCounter<int>("IntObservableUpDownCounter", () => intMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter9, new Measurement<int>[] { intMeasurement1 });

                Measurement<long> longMeasurement1 = new Measurement<long>(-20_000_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T7", "V7"), new KeyValuePair<string, object?>("T8", "V8") });
                ObservableUpDownCounter<long> observableUpDownCounter10 = meter.CreateObservableUpDownCounter<long>("longObservableUpDownCounter", () => longMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter10, new Measurement<long>[] { longMeasurement1 });

                Measurement<float> floatMeasurement1 = new Measurement<float>(-1e2f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T9", "V10"), new KeyValuePair<string, object?>("T11", "V12") });
                ObservableUpDownCounter<float> observableUpDownCounter11 = meter.CreateObservableUpDownCounter<float>("floatObservableUpDownCounter", () => floatMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter11, new Measurement<float>[] { floatMeasurement1 });

                Measurement<double> doubleMeasurement1 = new Measurement<double>(-2.5e7, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T13", "V14"), new KeyValuePair<string, object?>("T15", "V16") });
                ObservableUpDownCounter<double> observableUpDownCounter12 = meter.CreateObservableUpDownCounter<double>("doubleObservableUpDownCounter", () => doubleMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter12, new Measurement<double>[] { doubleMeasurement1 });

                Measurement<decimal> decimalMeasurement1 = new Measurement<decimal>(-3.2e20m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T17", "V18"), new KeyValuePair<string, object?>("T19", "V20") });
                ObservableUpDownCounter<decimal> observableUpDownCounter13 = meter.CreateObservableUpDownCounter<decimal>("decimalObservableUpDownCounter", () => decimalMeasurement1);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter13, new Measurement<decimal>[] { decimalMeasurement1 });

                //
                // CreateObservableGauge using Func<Measurement<T>>
                //
                Measurement<byte> byteGaugeMeasurement = new Measurement<byte>(35, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T21", "V22"), new KeyValuePair<string, object?>("T23", "V24") });
                ObservableGauge<byte> observableGauge7 = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => byteGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge7, new Measurement<byte>[] { byteGaugeMeasurement });

                Measurement<short> shortGaugeMeasurement = new Measurement<short>(23_987, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T25", "V26"), new KeyValuePair<string, object?>("T27", "V28") });
                ObservableGauge<short> observableGauge8 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => shortGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge8, new Measurement<short>[] { shortGaugeMeasurement });

                Measurement<int> intGaugeMeasurement = new Measurement<int>(1_987_765, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T29", "V30"), new KeyValuePair<string, object?>("T31", "V32") });
                ObservableGauge<int> observableGauge9 = meter.CreateObservableGauge<int>("IntObservableGauge", () => intGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge9, new Measurement<int>[] { intGaugeMeasurement });

                Measurement<long> longGaugeMeasurement = new Measurement<long>(10_000_234_343, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T33", "V342"), new KeyValuePair<string, object?>("T35", "V36") });
                ObservableGauge<long> observableGauge10 = meter.CreateObservableGauge<long>("longObservableGauge", () => longGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge10, new Measurement<long>[] { longGaugeMeasurement });

                Measurement<float> floatGaugeMeasurement = new Measurement<float>(2.1f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T37", "V38"), new KeyValuePair<string, object?>("T39", "V40") });
                ObservableGauge<float> observableGauge11 = meter.CreateObservableGauge<float>("floatObservableGauge", () => floatGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge11, new Measurement<float>[] { floatGaugeMeasurement });

                Measurement<double> doubleGaugeMeasurement = new Measurement<double>(1.5e30, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T41", "V42"), new KeyValuePair<string, object?>("T43", "V44") });
                ObservableGauge<double> observableGauge12 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => doubleGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge12, new Measurement<double>[] { doubleGaugeMeasurement });

                Measurement<decimal> decimalGaugeMeasurement = new Measurement<decimal>(2.5e20m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T45", "V46"), new KeyValuePair<string, object?>("T47", "V48") });
                ObservableGauge<decimal> observableGauge13 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => decimalGaugeMeasurement);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge13, new Measurement<decimal>[] { decimalGaugeMeasurement });

                //
                // CreateObservableCounter using Func<Measurement<T>>
                //
                Measurement<byte>[] byteGaugeMeasurementList = new Measurement<byte>[]
                {
                    new Measurement<byte>(0, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T48", "V49"), new KeyValuePair<string, object?>("T50", "V51") }),
                    new Measurement<byte>(1, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T51", "V52"), new KeyValuePair<string, object?>("T53", "V54") }),
                };
                ObservableCounter<byte> observableCounter14 = meter.CreateObservableCounter<byte>("ByteObservableCounter", () => byteGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter14, byteGaugeMeasurementList);

                Measurement<short>[] shortGaugeMeasurementList = new Measurement<short>[]
                {
                    new Measurement<short>(20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T55", "V56"), new KeyValuePair<string, object?>("T57", "V58") }),
                    new Measurement<short>(30_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T59", "V60"), new KeyValuePair<string, object?>("T61", "V62") }),
                };
                ObservableCounter<short> observableCounter15 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => shortGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter15, shortGaugeMeasurementList);

                Measurement<int>[] intGaugeMeasurementList = new Measurement<int>[]
                {
                    new Measurement<int>(1_000_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T63", "V64"), new KeyValuePair<string, object?>("T65", "V66") }),
                    new Measurement<int>(1_000_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T67", "V68"), new KeyValuePair<string, object?>("T69", "V70") }),
                };
                ObservableCounter<int> observableCounter16 = meter.CreateObservableCounter<int>("IntObservableCounter", () => intGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter16, intGaugeMeasurementList);

                Measurement<long>[] longGaugeMeasurementList = new Measurement<long>[]
                {
                    new Measurement<long>(1_000_001_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T71", "V72"), new KeyValuePair<string, object?>("T73", "V74") }),
                    new Measurement<long>(1_000_002_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T75", "V76"), new KeyValuePair<string, object?>("T77", "V78") }),
                };
                ObservableCounter<long> observableCounter17 = meter.CreateObservableCounter<long>("longObservableCounter", () => longGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter17, longGaugeMeasurementList);

                Measurement<float>[] floatGaugeMeasurementList = new Measurement<float>[]
                {
                    new Measurement<float>(68.15e8f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T79", "V80"), new KeyValuePair<string, object?>("T81", "V82") }),
                    new Measurement<float>(68.15e6f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T83", "V84"), new KeyValuePair<string, object?>("T85", "V86") }),
                };
                ObservableCounter<float> observableCounter18 = meter.CreateObservableCounter<float>("floatObservableCounter", () => floatGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter18, floatGaugeMeasurementList);

                Measurement<double>[] doubleGaugeMeasurementList = new Measurement<double>[]
                {
                    new Measurement<double>(68.15e20, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T87", "V88"), new KeyValuePair<string, object?>("T89", "V90") }),
                    new Measurement<double>(68.15e21, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T91", "V92"), new KeyValuePair<string, object?>("T93", "V94") }),
                };
                ObservableCounter<double> observableCounter19 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => doubleGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter19, doubleGaugeMeasurementList);

                Measurement<decimal>[] decimalGaugeMeasurementList = new Measurement<decimal>[]
                {
                    new Measurement<decimal>(68.15e8m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T95", "V96"), new KeyValuePair<string, object?>("T97", "V98") }),
                    new Measurement<decimal>(68.15e6m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T99", "V100"), new KeyValuePair<string, object?>("T101", "V102") }),
                };
                ObservableCounter<decimal> observableCounter20 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => decimalGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableCounter20, decimalGaugeMeasurementList);

                //
                // CreateObservableUpDownCounter using Func<Measurement<T>>
                //
                Measurement<byte>[] byteUpDownCounterMeasurementList = new Measurement<byte>[]
                {
                    new Measurement<byte>(10, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T48", "V49"), new KeyValuePair<string, object?>("T50", "V51") }),
                    new Measurement<byte>(12, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T51", "V52"), new KeyValuePair<string, object?>("T53", "V54") }),
                };
                ObservableUpDownCounter<byte> observableUpDownCounter14 = meter.CreateObservableUpDownCounter<byte>("ByteObservableUpDownCounter", () => byteUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter14, byteUpDownCounterMeasurementList);

                Measurement<short>[] shortUpDownCounterMeasurementList = new Measurement<short>[]
                {
                    new Measurement<short>(-20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T55", "V56"), new KeyValuePair<string, object?>("T57", "V58") }),
                    new Measurement<short>(30_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T59", "V60"), new KeyValuePair<string, object?>("T61", "V62") }),
                };
                ObservableUpDownCounter<short> observableUpDownCounter15 = meter.CreateObservableUpDownCounter<short>("ShortObservableUpDownCounter", () => shortUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter15, shortUpDownCounterMeasurementList);

                Measurement<int>[] intUpDownCounterMeasurementList = new Measurement<int>[]
                {
                    new Measurement<int>(1_000_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T63", "V64"), new KeyValuePair<string, object?>("T65", "V66") }),
                    new Measurement<int>(-1_000_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T67", "V68"), new KeyValuePair<string, object?>("T69", "V70") }),
                };
                ObservableUpDownCounter<int> observableUpDownCounter16 = meter.CreateObservableUpDownCounter<int>("IntObservableUpDownCounter", () => intUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter16, intUpDownCounterMeasurementList);

                Measurement<long>[] longUpDownCounterMeasurementList = new Measurement<long>[]
                {
                    new Measurement<long>(-1_000_001_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T71", "V72"), new KeyValuePair<string, object?>("T73", "V74") }),
                    new Measurement<long>(1_000_002_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T75", "V76"), new KeyValuePair<string, object?>("T77", "V78") }),
                };
                ObservableUpDownCounter<long> observableUpDownCounter17 = meter.CreateObservableUpDownCounter<long>("longObservableUpDownCounter", () => longUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter17, longUpDownCounterMeasurementList);

                Measurement<float>[] floatUpDownCounterMeasurementList = new Measurement<float>[]
                {
                    new Measurement<float>(-68.15e8f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T79", "V80"), new KeyValuePair<string, object?>("T81", "V82") }),
                    new Measurement<float>(-68.15e6f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T83", "V84"), new KeyValuePair<string, object?>("T85", "V86") }),
                };
                ObservableUpDownCounter<float> observableUpDownCounter18 = meter.CreateObservableUpDownCounter<float>("floatObservableUpDownCounter", () => floatUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter18, floatUpDownCounterMeasurementList);

                Measurement<double>[] doubleUpDownCounterMeasurementList = new Measurement<double>[]
                {
                    new Measurement<double>(-68.15e20, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T87", "V88"), new KeyValuePair<string, object?>("T89", "V90") }),
                    new Measurement<double>(68.15e21, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T91", "V92"), new KeyValuePair<string, object?>("T93", "V94") }),
                };
                ObservableUpDownCounter<double> observableUpDownCounter19 = meter.CreateObservableUpDownCounter<double>("doubleObservableUpDownCounter", () => doubleUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter19, doubleUpDownCounterMeasurementList);

                Measurement<decimal>[] decimalUpDownCounterMeasurementList = new Measurement<decimal>[]
                {
                    new Measurement<decimal>(68.15e8m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T95", "V96"), new KeyValuePair<string, object?>("T97", "V98") }),
                    new Measurement<decimal>(-68.15e6m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T99", "V100"), new KeyValuePair<string, object?>("T101", "V102") }),
                };
                ObservableUpDownCounter<decimal> observableUpDownCounter20 = meter.CreateObservableUpDownCounter<decimal>("decimalObservableUpDownCounter", () => decimalUpDownCounterMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableUpDownCounter20, decimalUpDownCounterMeasurementList);

                //
                // CreateObservableGauge using IEnumerable<Measurement<T>>
                //
                ObservableGauge<byte> observableGauge14 = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => byteGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge14, byteGaugeMeasurementList);

                ObservableGauge<short> observableGauge15 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => shortGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge15, shortGaugeMeasurementList);

                ObservableGauge<int> observableGauge16 = meter.CreateObservableGauge<int>("IntObservableGauge", () => intGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge16, intGaugeMeasurementList);

                ObservableGauge<long> observableGauge17 = meter.CreateObservableGauge<long>("longObservableGauge", () => longGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge17, longGaugeMeasurementList);

                ObservableGauge<float> observableGauge18 = meter.CreateObservableGauge<float>("floatObservableGauge", () => floatGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge18, floatGaugeMeasurementList);

                ObservableGauge<double> observableGauge19 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => doubleGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge19, doubleGaugeMeasurementList);

                ObservableGauge<decimal> observableGauge20 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => decimalGaugeMeasurementList);
                ObservableInstrumentMeasurementAggregationValidation(observableGauge20, decimalGaugeMeasurementList);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void PassingVariableTagsParametersTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("PassingVariableTagsParametersTest");

                InstrumentPassingVariableTagsParametersTest<byte>(meter.CreateCounter<byte>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<byte>, value, tags);
                                                            return (byte)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<short>(meter.CreateCounter<short>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<short>, value, tags);
                                                            return (short)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<int>(meter.CreateCounter<int>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<int>, value, tags);
                                                            return (int)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<long>(meter.CreateCounter<long>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<long>, value, tags);
                                                            return (long)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<float>(meter.CreateCounter<float>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<float>, value, tags);
                                                            return (float)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<double>(meter.CreateCounter<double>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<double>, value, tags);
                                                            return (double)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<decimal>(meter.CreateCounter<decimal>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishCounterMeasurement(instrument as Counter<decimal>, value, tags);
                                                            return (decimal)(value * 2);
                                                        });

                InstrumentPassingVariableTagsParametersTest<byte>(meter.CreateUpDownCounter<byte>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<byte>, value, tags);
                                                            return (byte)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<short>(meter.CreateUpDownCounter<short>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<short>, value, tags);
                                                            return (short)(-value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<int>(meter.CreateUpDownCounter<int>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<int>, value, tags);
                                                            return (int)(-value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<long>(meter.CreateUpDownCounter<long>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<long>, value, tags);
                                                            return (long)(-value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<float>(meter.CreateUpDownCounter<float>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<float>, value, tags);
                                                            return (float)(-value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<double>(meter.CreateUpDownCounter<double>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<double>, value, tags);
                                                            return (double)(-value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<decimal>(meter.CreateUpDownCounter<decimal>("Counter"), (instrument, value, tags) =>
                                                        {
                                                            PublishUpDownCounterMeasurement(instrument as UpDownCounter<decimal>, value, tags);
                                                            return (decimal)(-value * 2);
                                                        });

                InstrumentPassingVariableTagsParametersTest<byte>(meter.CreateHistogram<byte>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<byte>, value, tags);
                                                            return (byte)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<short>(meter.CreateHistogram<short>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<short>, value, tags);
                                                            return (short)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<int>(meter.CreateHistogram<int>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<int>, value, tags);
                                                            return (int)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<long>(meter.CreateHistogram<long>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<long>, value, tags);
                                                            return (long)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<float>(meter.CreateHistogram<float>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<float>, value, tags);
                                                            return (float)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<double>(meter.CreateHistogram<double>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<double>, value, tags);
                                                            return (double)(value * 2);
                                                        });
                InstrumentPassingVariableTagsParametersTest<decimal>(meter.CreateHistogram<decimal>("Histogram"), (instrument, value, tags) =>
                                                        {
                                                            PublishHistogramMeasurement(instrument as Histogram<decimal>, value, tags);
                                                            return (decimal)(value * 2);
                                                        });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MeterDisposalsTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter1 = new Meter("MeterDisposalsTest1");
                Meter meter2 = new Meter("MeterDisposalsTest2");
                Meter meter3 = new Meter("MeterDisposalsTest3");
                Meter meter4 = new Meter("MeterDisposalsTest4");
                Meter meter5 = new Meter("MeterDisposalsTest5");
                Meter meter6 = new Meter("MeterDisposalsTest6");

                Counter<int> counter = meter1.CreateCounter<int>("Counter");
                Histogram<double> histogram = meter2.CreateHistogram<double>("Histogram");
                ObservableCounter<long> observableCounter = meter3.CreateObservableCounter<long>("ObservableCounter", () => new Measurement<long>(10, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                ObservableGauge<decimal> observableGauge = meter4.CreateObservableGauge<decimal>("ObservableGauge", () => new Measurement<decimal>(5.7m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                UpDownCounter<short> upDownCounter = meter5.CreateUpDownCounter<short>("UpDownCounter");
                ObservableUpDownCounter<int> observableUpDownCounter = meter6.CreateObservableUpDownCounter<int>("ObservableUpDownCounter", () => new Measurement<int>(-5, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));

                using MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int count = 0;

                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state)     => count++);
                listener.SetMeasurementEventCallback<short>((inst, measurement, tags, state)   => count++);
                listener.SetMeasurementEventCallback<double>((inst, measurement, tags, state)  => count++);
                listener.SetMeasurementEventCallback<long>((inst, measurement, tags, state)    => count++);
                listener.SetMeasurementEventCallback<decimal>((inst, measurement, tags, state) => count++);

                listener.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(1, count);

                upDownCounter.Add(-1);
                Assert.Equal(2, count);

                histogram.Record(1);
                Assert.Equal(3, count);

                listener.RecordObservableInstruments();
                Assert.Equal(6, count);

                meter1.Dispose();
                counter.Add(1);
                Assert.Equal(6, count);

                meter2.Dispose();
                histogram.Record(1);
                Assert.Equal(6, count);

                meter5.Dispose();
                upDownCounter.Add(-10);
                Assert.Equal(6, count);

                listener.RecordObservableInstruments();
                Assert.Equal(9, count);

                meter3.Dispose();
                listener.RecordObservableInstruments();
                Assert.Equal(11, count);

                meter4.Dispose();
                listener.RecordObservableInstruments();
                Assert.Equal(12, count);

                meter6.Dispose();
                listener.RecordObservableInstruments();
                Assert.Equal(12, count);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerDisposalsTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("ListenerDisposalsTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter");
                UpDownCounter<short> upDownCounter = meter.CreateUpDownCounter<short>("upDownCounter");
                Histogram<double> histogram = meter.CreateHistogram<double>("Histogram");
                ObservableCounter<long> observableCounter = meter.CreateObservableCounter<long>("ObservableCounter", () => new Measurement<long>(10, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                ObservableGauge<decimal> observableGauge = meter.CreateObservableGauge<decimal>("ObservableGauge", () => new Measurement<decimal>(5.7m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                ObservableUpDownCounter<float> observableUpDownCounter = meter.CreateObservableUpDownCounter<float>("ObservableUpDownCounter", () => new Measurement<float>(-5.7f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));

                int completedMeasurements = 0;
                MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);
                listener.MeasurementsCompleted = (theInstrument, state) => completedMeasurements++;

                int count = 0;

                listener.SetMeasurementEventCallback<short>((inst, measurement, tags, state)   => count++);
                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state)     => count++);
                listener.SetMeasurementEventCallback<float>((inst, measurement, tags, state)   => count++);
                listener.SetMeasurementEventCallback<double>((inst, measurement, tags, state)  => count++);
                listener.SetMeasurementEventCallback<long>((inst, measurement, tags, state)    => count++);
                listener.SetMeasurementEventCallback<decimal>((inst, measurement, tags, state) => count++);

                listener.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(1, count);

                upDownCounter.Add(-1);
                Assert.Equal(2, count);

                histogram.Record(1);
                Assert.Equal(3, count);

                listener.RecordObservableInstruments();
                Assert.Equal(6, count);

                listener.Dispose();
                Assert.Equal(6, completedMeasurements);

                counter.Add(1);
                Assert.Equal(6, count);

                upDownCounter.Add(-1);
                Assert.Equal(6, count);

                histogram.Record(1);
                Assert.Equal(6, count);

                listener.RecordObservableInstruments();
                Assert.Equal(6, count);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MultipleListenersTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("MultipleListenersTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter");

                MeterListener listener1 = new MeterListener();
                MeterListener listener2 = new MeterListener();
                MeterListener listener3 = new MeterListener();

                listener1.InstrumentPublished = listener2.InstrumentPublished = listener3.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int count = 0;

                listener1.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => count++);
                listener2.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => count++);
                listener3.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => count++);

                listener1.Start();
                listener2.Start();
                listener3.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(3, count);

                counter.Add(1);
                Assert.Equal(6, count);

                counter.Add(1);
                Assert.Equal(9, count);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void NullMeasurementEventCallbackTest()
        {
             RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("NullMeasurementEventCallbackTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter");

                MeterListener listener = new MeterListener();

                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int count = 0;
                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => count++);

                listener.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(1, count);

                listener.SetMeasurementEventCallback<int>(null);
                counter.Add(1);
                Assert.Equal(1, count);

                Assert.Throws<InvalidOperationException>(() => listener.SetMeasurementEventCallback<ulong>(null));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EnableListeningMultipleTimesWithDifferentState()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("EnableListeningMultipleTimesWithDifferentState");

                Counter<int> counter = meter.CreateCounter<int>("Counter");

                MeterListener listener = new MeterListener();

                string lastState = "1";
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, lastState);
                int completedCount = 0;
                listener.MeasurementsCompleted = (theInstrument, state) => { Assert.Equal(lastState, state); completedCount++; };
                listener.Start();

                string newState = "2";
                listener.EnableMeasurementEvents(counter, newState);
                Assert.Equal(1, completedCount);
                lastState = newState;

                newState = "3";
                listener.EnableMeasurementEvents(counter, newState);
                Assert.Equal(2, completedCount);
                lastState = newState;

                newState = null;
                listener.EnableMeasurementEvents(counter, newState);
                Assert.Equal(3, completedCount);
                lastState = newState;

                listener.Dispose();
                Assert.Equal(4, completedCount);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ParallelRunningTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("ParallelRunningTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter");
                UpDownCounter<int> upDownCounter = meter.CreateUpDownCounter<int>("UpDownCounter");
                Histogram<int> histogram = meter.CreateHistogram<int>("Histogram");
                ObservableCounter<int> observableCounter = meter.CreateObservableCounter<int>("ObservableCounter", () => 1);
                ObservableUpDownCounter<int> observableUpDownCounter = meter.CreateObservableUpDownCounter<int>("ObservableUpDownCounter", () => 1);
                ObservableGauge<int> observableGauge = meter.CreateObservableGauge<int>("ObservableGauge", () => 1);

                MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int totalCount = 0;
                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => Interlocked.Add(ref totalCount, measurement));
                listener.Start();

                Task[] taskList = new Task[9];

                int loopLength = 10_000;

                taskList[0] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { counter.Add(1); } });
                taskList[1] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { counter.Add(1); } });
                taskList[2] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { histogram.Record(1); } });
                taskList[3] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { histogram.Record(1); } });
                taskList[4] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { listener.RecordObservableInstruments(); } });
                taskList[5] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { listener.RecordObservableInstruments(); } });
                taskList[6] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { upDownCounter.Add(1); } });
                taskList[7] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { upDownCounter.Add(1); } });
                taskList[8] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { listener.RecordObservableInstruments(); } });

                Task.WaitAll(taskList);

                Assert.Equal(loopLength * 15, totalCount);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SerializedEventsTest()
        {
            RemoteExecutor.Invoke(() => {

                const int MaxMetersCount = 50;

                Meter [] meters = new Meter[MaxMetersCount];
                for (int i = 0; i < MaxMetersCount; i++)
                {
                    meters[i] = new Meter("SerializedEventsTest" + i);
                }

                Dictionary<Instrument, bool> instruments = new Dictionary<Instrument, bool>();

                MeterListener listener = new MeterListener()
                {
                    InstrumentPublished = (instrument, theListener) =>
                    {
                        lock (instruments)
                        {
                            Assert.False(instruments.ContainsKey(instrument), $"{instrument.Name}, {instrument.Meter.Name} is already published before");
                            instruments.Add(instrument, true);
                            theListener.EnableMeasurementEvents(instrument, null);
                        }
                    },

                    MeasurementsCompleted = (instrument, state) =>
                    {
                        lock (instruments)
                        {
                            Assert.True(instruments.Remove(instrument), $"{instrument.Name}, {instrument.Meter.Name} is not published while getting completed results");
                        }
                    }
                };

                listener.Start();

                int counterCounter = 0;
                Random r = new Random();

                Task [] jobs = new Task[Environment.ProcessorCount];
                for (int i = 0; i < jobs.Length; i++)
                {
                    jobs[i] = Task.Run(() => {
                        for (int j = 0; j < 10; j++)
                        {
                            int index = r.Next(MaxMetersCount);

                            for (int k = 0; k < 10; k++)
                            {
                                int c = Interlocked.Increment(ref counterCounter);
                                Counter<int> counter = meters[index].CreateCounter<int>("Counter");
                                counter.Add(1);
                            }

                            meters[index].Dispose();
                        }
                    });
                }

                Task.WaitAll(jobs);
                listener.Dispose();
                Assert.Equal(0, instruments.Count);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestRecordingMeasurementsWithTagList()
        {
            RemoteExecutor.Invoke(() => {

                Meter meter = new Meter("RecordingMeasurementsWithTagList");

                using (MeterListener listener = new MeterListener())
                {
                    Counter<int> counter = meter.CreateCounter<int>("Counter");
                    UpDownCounter<int> upDownCounter = meter.CreateUpDownCounter<int>("UpDownCounter");
                    Histogram<int> histogram = meter.CreateHistogram<int>("histogram");

                    listener.EnableMeasurementEvents(counter, counter);
                    listener.EnableMeasurementEvents(upDownCounter, upDownCounter);
                    listener.EnableMeasurementEvents(histogram, histogram);

                    KeyValuePair<string, object?>[] expectedTags = null;

                    listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => {
                        for (int i = 0; i < expectedTags.Length; i++)
                        {
                            Assert.Equal(expectedTags[i], tags[i]);
                        }
                    });

                    // 0 Tags

                    expectedTags = new KeyValuePair<string, object?>[0];
                    counter.Add(10, new TagList());
                    upDownCounter.Add(-1, new TagList());
                    histogram.Record(10, new TagList());

                    // 1 Tags
                    expectedTags = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key1", "Value1") };
                    counter.Add(10, new TagList() { expectedTags[0] });
                    upDownCounter.Add(-2, new TagList() { expectedTags[0] });
                    histogram.Record(10, new TagList() { new KeyValuePair<string, object?>("Key1", "Value1") });

                    // 2 Tags
                    expectedTags = new List<KeyValuePair<string, object?>>
                    {
                        {"Key1", "Value1"},
                        {"Key2", "Value2"}
                    }.ToArray();

                    counter.Add(10, new TagList() { expectedTags[0], expectedTags[1] });
                    upDownCounter.Add(-3, new TagList() { expectedTags[0], expectedTags[1] });
                    histogram.Record(10, new TagList() { expectedTags[0], expectedTags[1] });

                    // 8 Tags
                    expectedTags = new List<KeyValuePair<string, object?>>
                    {
                        { "Key1", "Value1" },
                        { "Key2", "Value2" },
                        { "Key3", "Value3" },
                        { "Key4", "Value4" },
                        { "Key5", "Value5" },
                        { "Key6", "Value6" },
                        { "Key7", "Value7" },
                        { "Key8", "Value8" },
                    }.ToArray();

                    counter.Add(10, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7] });
                    upDownCounter.Add(-4, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7] });
                    histogram.Record(10, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7] });

                    // 13 Tags
                    expectedTags = new List<KeyValuePair<string, object?>>
                    {
                        { "Key1", "Value1" },
                        { "Key2", "Value2" },
                        { "Key3", "Value3" },
                        { "Key4", "Value4" },
                        { "Key5", "Value5" },
                        { "Key6", "Value6" },
                        { "Key7", "Value7" },
                        { "Key8", "Value8" },
                        { "Key9", "Value9" },
                        { "Key10", "Value10" },
                        { "Key11", "Value11" },
                        { "Key12", "Value12" },
                        { "Key13", "Value13" },
                    }.ToArray();

                    counter.Add(10, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7],
                                                     expectedTags[8], expectedTags[9], expectedTags[10], expectedTags[11], expectedTags[12] });
                    upDownCounter.Add(-5, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7],
                                                     expectedTags[8], expectedTags[9], expectedTags[10], expectedTags[11], expectedTags[12] });
                    histogram.Record(10, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7],
                                                     expectedTags[8], expectedTags[9], expectedTags[10], expectedTags[11], expectedTags[12] });
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestMeterCreationWithOptions()
        {
            RemoteExecutor.Invoke(() =>
            {
                using Meter meter1 = new Meter("TestMeterCreationWithOptions1");
                Assert.Equal("TestMeterCreationWithOptions1", meter1.Name);
                Assert.Null(meter1.Version);
                Assert.Null(meter1.Tags);
                Assert.Null(meter1.Scope);

                using Meter meter2 = new Meter("TestMeterCreationWithOptions2", "2.0", new TagList() { { "Key1", "Value1" } });
                Assert.Equal("TestMeterCreationWithOptions2", meter2.Name);
                Assert.Equal("2.0", meter2.Version);
                Assert.Equal(new[] { new KeyValuePair<string, object?>("Key1", "Value1") }, meter2.Tags);
                Assert.Null(meter2.Scope);

                using Meter meter3 = new Meter("TestMeterCreationWithOptions3", "3.0", new TagList() { { "Key3", "Value3" } }, "Scope");
                Assert.Equal("TestMeterCreationWithOptions3", meter3.Name);
                Assert.Equal("3.0", meter3.Version);
                Assert.Equal(new[] { new KeyValuePair<string, object?>("Key3", "Value3") }, meter3.Tags);
                Assert.Equal("Scope", meter3.Scope);

                Assert.Throws<ArgumentNullException>(() => new MeterOptions(null!));
                Assert.Throws<ArgumentNullException>(() => new MeterOptions("Something").Name = null!);

                using Meter meter4 = new Meter(new MeterOptions("TestMeterCreationWithOptions4"));
                Assert.Equal("TestMeterCreationWithOptions4", meter4.Name);
                Assert.Null(meter4.Version);
                Assert.Null(meter4.Tags);
                Assert.Null(meter4.Scope);

                using Meter meter5 = new Meter(new MeterOptions("TestMeterCreationWithOptions5") { Version = "5.0" });
                Assert.Equal("TestMeterCreationWithOptions5", meter5.Name);
                Assert.Equal("5.0", meter5.Version);
                Assert.Null(meter5.Tags);
                Assert.Null(meter5.Scope);

                using Meter meter6 = new Meter(new MeterOptions("TestMeterCreationWithOptions6") { Tags = new TagList() { { "Key6", "Value6"} } });
                Assert.Equal("TestMeterCreationWithOptions6", meter6.Name);
                Assert.Null(meter6.Version);
                Assert.Equal(new[] { new KeyValuePair<string, object?>("Key6", "Value6") }, meter6.Tags);
                Assert.Null(meter5.Scope);

                using Meter meter7 = new Meter(new MeterOptions("TestMeterCreationWithOptions7") { Scope = "Scope7" });
                Assert.Equal("TestMeterCreationWithOptions7", meter7.Name);
                Assert.Null(meter7.Version);
                Assert.Null(meter7.Tags);
                Assert.Equal("Scope7", meter7.Scope);

                using Meter meter8 = new Meter(new MeterOptions("TestMeterCreationWithOptions8") { Version = "8.0", Tags = new TagList() { { "Key8", "Value8" } }, Scope = "Scope8" });
                Assert.Equal("TestMeterCreationWithOptions8", meter8.Name);
                Assert.Equal("8.0", meter8.Version);
                Assert.Equal(new[] { new KeyValuePair<string, object?>("Key8", "Value8") }, meter8.Tags);
                Assert.Equal("Scope8", meter8.Scope);

                // Test tags sorting order
                TagList l = new TagList() { { "f", "a" }, { "d", "b" }, { "w", "b" }, { "h", new object() }, { "N", null }, { "a", "b" }, { "a", null } };
                using Meter meter9 = new Meter(new MeterOptions("TestMeterCreationWithOptions9") { Version = "8.0", Tags = l, Scope = "Scope8" });
                var insArray = meter9.Tags.ToArray();
                Assert.Equal(l.Count, insArray.Length);
                for (int i = 0; i < insArray.Length - 1; i++)
                {
                    Assert.True(string.Compare(insArray[i].Key, insArray[i + 1].Key, StringComparison.Ordinal) <= 0);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestCachedInstruments()
        {
            RemoteExecutor.Invoke(() =>
            {
                using Meter meter = new Meter("TestCachedInstruments");

                Counter<int> counter1 = meter.CreateCounter<int>("name1");
                Counter<int> counter2 = meter.CreateCounter<int>("name1");

                Assert.True(object.ReferenceEquals(counter1, counter2));

                Counter<int> counter3 = meter.CreateCounter<int>("name1", "unique");
                Assert.False(object.ReferenceEquals(counter1, counter3));

                var list1 = new List<KeyValuePair<string, object?>>
                {
                    new KeyValuePair<string, object?>("key1", "value1"),
                    new KeyValuePair<string, object?>("key2", null)
                };

                Counter<int> counter4 = meter.CreateCounter<int>("name", null, null, list1);
                Counter<int> counter5 = meter.CreateCounter<int>("name", null, null, list1);

                Assert.True(object.ReferenceEquals(counter4, counter5));

                Counter<int> counter6 = meter.CreateCounter<int>("name", "diff", null, list1);

                Assert.False(object.ReferenceEquals(counter4, counter6));

                Counter<long> counter7 = meter.CreateCounter<long>("name", null, null, list1);

                Assert.False(object.ReferenceEquals(counter4, counter7));

                var list2 = new List<KeyValuePair<string, object?>>
                {
                    new KeyValuePair<string, object?>("key1", "value1"),
                    new KeyValuePair<string, object?>("key2", "value2")
                };

                Counter<int> counter8 = meter.CreateCounter<int>("name", null, null, list2);

                Assert.False(object.ReferenceEquals(counter4, counter8));

                Histogram<int> histogram1 = meter.CreateHistogram<int>("name", null, null, list2);

                Assert.False(object.ReferenceEquals(counter8, histogram1));

                Histogram<int> histogram2 = meter.CreateHistogram<int>("name", null, null, list2);

                Assert.True(object.ReferenceEquals(histogram2, histogram1));

                var list3 = new List<KeyValuePair<string, object?>>
                {
                    new KeyValuePair<string, object?>("key1", "value3"),
                    new KeyValuePair<string, object?>("key2", "value2")
                };

                Histogram<int> histogram3 = meter.CreateHistogram<int>("name", null, null, list3);

                Assert.False(object.ReferenceEquals(histogram3, histogram1));

                UpDownCounter<int> upDownCounter1 = meter.CreateUpDownCounter<int>("name", null, null, list2);

                Assert.False(object.ReferenceEquals(counter8, upDownCounter1));

                UpDownCounter<int> upDownCounter2 = meter.CreateUpDownCounter<int>("name", null, null, list2);

                Assert.True(object.ReferenceEquals(upDownCounter2, upDownCounter1));

                UpDownCounter<int> upDownCounter3 = meter.CreateUpDownCounter<int>("name", null, null, list3);

                Assert.False(object.ReferenceEquals(upDownCounter3, upDownCounter1));

                //
                // Test instrument creation with unordered tags
                //

                object o = new object();
                TagList l1 = new TagList() { { "f", "a" }, { "d", "b" }, { "w", "b" }, { "h", o}, { "N", null }, { "a", "b" }, { "a", null } };
                List<KeyValuePair<string, object?>> l2 = new List<KeyValuePair<string, object?>>()
                {
                    new KeyValuePair<string, object?>("w", "b"), new KeyValuePair<string, object?>("h", o), new KeyValuePair<string, object?>("a", null),
                    new KeyValuePair<string, object?>("d", "b"), new KeyValuePair<string, object?>("f", "a"), new KeyValuePair<string, object?>("N", null),
                    new KeyValuePair<string, object?>("a", "b")
                };
                HashSet<KeyValuePair<string, object?>> l3 = new HashSet<KeyValuePair<string, object?>>()
                {
                    new KeyValuePair<string, object?>("d", "b"), new KeyValuePair<string, object?>("f", "a"), new KeyValuePair<string, object?>("a", null),
                    new KeyValuePair<string, object?>("w", "b"), new KeyValuePair<string, object?>("h", o), new KeyValuePair<string, object?>("a", "b"),
                    new KeyValuePair<string, object?>("N", null)
                };

                Counter<int> counter9 = meter.CreateCounter<int>("name9", null, null, l1);
                Counter<int> counter10 = meter.CreateCounter<int>("name9", null, null, l2);
                Counter<int> counter11 = meter.CreateCounter<int>("name9", null, null, l3);
                Assert.Same(counter9, counter10);
                Assert.Same(counter9, counter11);

                KeyValuePair<string, object?>[] t1 = counter9.Tags.ToArray();
                Assert.Equal(l1.Count, t1.Length);
                t1[0] = new KeyValuePair<string, object?>(t1[0].Key, "newValue"); // change value of one item;
                Counter<int> counter12 = meter.CreateCounter<int>("name9", null, null, t1);
                Assert.NotSame(counter9, counter12);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestInstrumentCreationWithTags()
        {
            RemoteExecutor.Invoke(() => {
                using Meter meter = new Meter("TestInstrumentCreationWithTags");

                Instrument ins1 = meter.CreateCounter<int>("counter", null, null, new TagList() { { "c1", "cv-1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("c1", "cv-1") }, ins1.Tags);

                Instrument ins2 = meter.CreateHistogram<double>("histogram", null, null, new TagList() { { "h1", "hv-1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("h1", "hv-1") }, ins2.Tags);

                Instrument ins3 = meter.CreateUpDownCounter<long>("UpDownCounter", null, null, new TagList() { { "udc1", "udc-v1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("udc1", "udc-v1") }, ins3.Tags);

                Instrument ins4 = meter.CreateObservableCounter<short>("ObservableCounter1", () => 1, null, null, new TagList() { { "oc1", "oc-v1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oc1", "oc-v1") }, ins4.Tags);

                Instrument ins5 = meter.CreateObservableCounter<short>("ObservableCounter2", () => new Measurement<short>(2), null, null, new TagList() { { "oc2", "oc-v2" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oc2", "oc-v2") }, ins5.Tags);

                Instrument ins6 = meter.CreateObservableCounter<short>("ObservableCounter3", () => new Measurement<short>[] { new Measurement<short>(3) }, null, null, new TagList() { { "oc3", "oc-v3" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oc3", "oc-v3") }, ins6.Tags);

                Instrument ins7 = meter.CreateObservableGauge<long>("ObservableGauge1", () => 1, null, null, new TagList() { { "og1", "og-v1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("og1", "og-v1") }, ins7.Tags);

                Instrument ins8 = meter.CreateObservableGauge<long>("ObservableGauge2", () => new Measurement<long>(2), null, null, new TagList() { { "og2", "og-v2" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("og2", "og-v2") }, ins8.Tags);

                Instrument ins9 = meter.CreateObservableGauge<long>("ObservableGauge3", () => new Measurement<long>[] { new Measurement<long>(3) }, null, null, new TagList() { { "og3", "og-v3" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("og3", "og-v3") }, ins9.Tags);

                Instrument ins10 = meter.CreateObservableUpDownCounter<float>("ObservableUpDownCounter1", () => 1, null, null, new TagList() { { "oudc1", "oudc-v1" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oudc1", "oudc-v1") }, ins10.Tags);

                Instrument ins11 = meter.CreateObservableGauge<float>("ObservableUpDownCounter2", () => new Measurement<float>(2), null, null, new TagList() { { "oudc2", "oudc-v2" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oudc2", "oudc-v2") }, ins11.Tags);

                Instrument ins12 = meter.CreateObservableGauge<float>("ObservableUpDownCounter3", () => new Measurement<float>[] { new Measurement<float>(3) }, null, null, new TagList() { { "oudc3", "oudc-v3" } });
                Assert.Equal(new[] { new KeyValuePair<string, object?>("oudc3", "oudc-v3") }, ins12.Tags);

                // Test tags sorting order

                TagList l = new TagList() { { "z", "a" }, { "y", "b" }, { "x", "b" }, { "m", new object() }, { "N", null }, { "a", "b" }, { "a", null } };
                Instrument ins13 = meter.CreateCounter<int>("counter", null, null, l);
                var insArray = ins13.Tags.ToArray();
                Assert.Equal(l.Count, insArray.Length);
                for (int i = 0; i < insArray.Length - 1; i++)
                {
                    Assert.True(string.Compare(insArray[i].Key, insArray[i + 1].Key, StringComparison.Ordinal) <= 0);
                }

            }).Dispose();
        }


        private void PublishCounterMeasurement<T>(Counter<T> counter, T value, KeyValuePair<string, object?>[] tags) where T : struct
        {
            switch (tags.Length)
            {
                case 0: counter.Add(value); break;
                case 1: counter.Add(value, tags[0]); break;
                case 2: counter.Add(value, tags[0], tags[1]); break;
                case 3: counter.Add(value, tags[0], tags[1], tags[2]); break;
                case 4: counter.Add(value, tags[0], tags[1], tags[2], tags[3]); break;
                case 5: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4]); break;
                case 6: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5]); break;
                case 7: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6]); break;
                case 8: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7]); break;
                case 9: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7], tags[8]); break;
                default: counter.Add(value, tags); break;
            }
        }

        private void PublishUpDownCounterMeasurement<T>(UpDownCounter<T> counter, T value, KeyValuePair<string, object?>[] tags) where T : struct
        {
            switch (tags.Length)
            {
                case 0: counter.Add(value); break;
                case 1: counter.Add(value, tags[0]); break;
                case 2: counter.Add(value, tags[0], tags[1]); break;
                case 3: counter.Add(value, tags[0], tags[1], tags[2]); break;
                case 4: counter.Add(value, tags[0], tags[1], tags[2], tags[3]); break;
                case 5: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4]); break;
                case 6: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5]); break;
                case 7: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6]); break;
                case 8: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7]); break;
                case 9: counter.Add(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7], tags[8]); break;
                default: counter.Add(value, tags); break;
            }
        }

        private void PublishHistogramMeasurement<T>(Histogram<T> histogram, T value, KeyValuePair<string, object?>[] tags) where T : struct
        {
            switch (tags.Length)
            {
                case 0: histogram.Record(value); break;
                case 1: histogram.Record(value, tags[0]); break;
                case 2: histogram.Record(value, tags[0], tags[1]); break;
                case 3: histogram.Record(value, tags[0], tags[1], tags[2]); break;
                case 4: histogram.Record(value, tags[0], tags[1], tags[2], tags[3]); break;
                case 5: histogram.Record(value, tags[0], tags[1], tags[2], tags[3], tags[4]); break;
                case 6: histogram.Record(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5]); break;
                case 7: histogram.Record(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6]); break;
                case 8: histogram.Record(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7]); break;
                case 9: histogram.Record(value, tags[0], tags[1], tags[2], tags[3], tags[4], tags[5], tags[6], tags[7], tags[8]); break;
                default: histogram.Record(value, tags); break;
            }
        }

        private void ValidateInstrumentInfo(Instrument instrument, string name, string unit, string description, bool isEnabled, bool isObservable)
        {
            Assert.Equal(name, instrument.Name);
            Assert.Equal(unit, instrument.Unit);
            Assert.Equal(description, instrument.Description);
            Assert.Equal(isEnabled, instrument.Enabled);
            Assert.Equal(isObservable, instrument.IsObservable);
        }

        private void InstrumentMeasurementAggregationValidation<T>(Instrument<T> instrument, Action<T, KeyValuePair<string, object?>[]> record, bool allowNegative = false) where T : struct
        {
            using MeterListener listener = new MeterListener();
            listener.InstrumentPublished = (theInstrument, theListener) =>
            {
                if (object.ReferenceEquals(instrument, theInstrument))
                {
                    Assert.Same(listener, theListener);
                    listener.EnableMeasurementEvents(theInstrument, theInstrument);
                }
            };

            List<KeyValuePair<string, object?>> expectedTags = new List<KeyValuePair<string, object?>>();
            T expectedValue = default;
            int counter = 0;

            listener.SetMeasurementEventCallback<T>((inst, measurement, tags, state) =>
            {
                Assert.True(instrument.Enabled);
                Assert.Same(instrument, inst);
                Assert.Same(instrument, state);
                Assert.Equal(expectedValue, measurement);
                Assert.Equal(expectedTags.ToArray(), tags.ToArray());
                counter++;
            });

            listener.Start();

            if (allowNegative && typeof(T) != typeof(Byte))
            {
                for (short i = 0; i < 100; i++)
                {
                    expectedTags.Add(new KeyValuePair<string, object?>(i.ToString(), i.ToString()));
                    expectedValue = ConvertValue<T>((short)(i % 2 == 0 ? 2 : -1));
                    record(expectedValue, expectedTags.ToArray());
                }
            }
            else
            {
                for (short i = 0; i < 100; i++)
                {
                    expectedTags.Add(new KeyValuePair<string, object?>(i.ToString(), i.ToString()));
                    expectedValue = ConvertValue<T>(i);
                    record(expectedValue, expectedTags.ToArray());
                }
            }
            Assert.Equal(100, counter);
        }

        private void ObservableInstrumentMeasurementAggregationValidation<T>(ObservableInstrument<T> instrument, Measurement<T>[] expectedResult) where T : struct
        {
            using MeterListener listener = new MeterListener();
            listener.InstrumentPublished = (theInstrument, theListener) =>
            {
                if (object.ReferenceEquals(instrument, theInstrument))
                {
                    Assert.Same(listener, theListener);
                    listener.EnableMeasurementEvents(theInstrument, theInstrument);
                }
            };

            int index = 0;

            listener.SetMeasurementEventCallback<T>((inst, measurement, tags, state) =>
            {
                Assert.True(instrument.Enabled);
                Assert.Same(instrument, inst);
                Assert.Same(instrument, state);
                Assert.True(index < expectedResult.Length, "We are getting more unexpected results");

                Assert.Equal(expectedResult[index].Value, measurement);
                Assert.Equal(expectedResult[index].Tags.ToArray(), tags.ToArray());
                index++;
            });

            listener.Start();

            listener.RecordObservableInstruments();

            Assert.Equal(expectedResult.Length, index);
        }

        private void InstrumentPassingVariableTagsParametersTest<T>(Instrument<T> instrument, Func<Instrument<T>, T, KeyValuePair<string, object?>[], T> record) where T : struct
        {
            using MeterListener listener = new MeterListener();
            listener.InstrumentPublished = (theInstrument, theListener) =>
            {
                if (object.ReferenceEquals(instrument, theInstrument))
                {
                    Assert.Same(listener, theListener);
                    listener.EnableMeasurementEvents(theInstrument, theInstrument);
                }
            };

            KeyValuePair<string, object?>[] expectedTags = Array.Empty<KeyValuePair<string, object?>>();
            T expectedValue = default;

            listener.SetMeasurementEventCallback<T>((inst, measurement, tags, state) =>
            {
                Assert.True(instrument.Enabled);
                Assert.Same(instrument, inst);
                Assert.Same(instrument, state);
                Assert.Equal(expectedValue, measurement);
                Assert.Equal(expectedTags, tags.ToArray());
            });

            listener.Start();

            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
                { "K5", "V5" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
                { "K5", "V5" },
                { "K6", "V6" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
                { "K5", "V5" },
                { "K6", "V6" },
                { "K7", "V7" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
                { "K5", "V5" },
                { "K6", "V6" },
                { "K7", "V7" },
                { "K8", "V8" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
            expectedTags = new List<KeyValuePair<string, object?>>
            {
                { "K1", "V1" },
                { "K2", "V2" },
                { "K3", "V3" },
                { "K4", "V4" },
                { "K5", "V5" },
                { "K6", "V6" },
                { "K7", "V7" },
                { "K8", "V8" },
                { "K9", "V9" },
            }.ToArray();
            expectedValue = record(instrument, expectedValue, expectedTags);
        }

        private T ConvertValue<T>(short value) where T : struct
        {
            if (typeof(T) == typeof(byte))  { return (T)(object)(byte)value; }
            if (typeof(T) == typeof(short)) { return (T)(object)value; }
            if (typeof(T) == typeof(int)) { return (T)(object)Convert.ToInt32(value); }
            if (typeof(T) == typeof(long)) { return (T)(object)Convert.ToInt64(value); }
            if (typeof(T) == typeof(float)) { return (T)(object)Convert.ToSingle(value); }
            if (typeof(T) == typeof(double)) { return (T)(object)Convert.ToDouble(value); }
            if (typeof(T) == typeof(decimal)) { return (T)(object)Convert.ToDecimal(value);}

            Assert.True(false, "We encountered unsupported type");
            return default;
        }
    }
    public static class DiagnosticsCollectionExtensions
    {
        public static void Add<T1, T2>(this ICollection<KeyValuePair<T1, T2>> collection, T1 item1, T2 item2) => collection?.Add(new KeyValuePair<T1, T2>(item1, item2));
        public static bool Same<T>(this IEnumerable<Measurement<T>> measurements, IEnumerable<Measurement<T>> expected) where T : struct
        {
            IEnumerator<Measurement<T>> enumerator = measurements.GetEnumerator();
            IEnumerator<Measurement<T>> expectedEnumerator = expected.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (!expectedEnumerator.MoveNext())
                {
                    return false;
                }

                Measurement<T> measurement = enumerator.Current;
                Measurement<T> expectedMeasurement = expectedEnumerator.Current;

                if (measurement.Value.Equals(expectedMeasurement.Value) &&
                    measurement.Tags.ToArray().SequenceEqual(expectedMeasurement.Tags.ToArray()))
                {
                    continue;
                }

                return false;
            }

            return !expectedEnumerator.MoveNext();
        }
    }
}
