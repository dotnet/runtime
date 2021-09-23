// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;

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

                Assert.Throws<ArgumentNullException>(() => new Meter(null));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void InstrumentCreationTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("InstrumentCreationTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter", "seconds", "Seconds Counter");
                ValidateInstrumentInfo(counter, "Counter", "seconds", "Seconds Counter", false, false);

                Histogram<float> histogram = meter.CreateHistogram<float>("Histogram", "centimeters", "centimeters Histogram");
                ValidateInstrumentInfo(histogram, "Histogram", "centimeters", "centimeters Histogram", false, false);

                ObservableCounter<long> observableCounter = meter.CreateObservableCounter<long>("ObservableCounter", () => 10, "millisecond", "millisecond ObservableCounter");
                ValidateInstrumentInfo(observableCounter, "ObservableCounter", "millisecond", "millisecond ObservableCounter", false, true);

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
                Assert.Throws<ArgumentNullException>(() => meter.CreateHistogram<short>(null, "seconds", "Seconds Counter"));
                Assert.Throws<ArgumentNullException>(() => meter.CreateObservableCounter<long>(null, () => 0, "seconds", "Seconds ObservableCounter"));
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

                ObservableGauge<byte> observableGauge1 = meter.CreateObservableGauge<byte>("observableGauge1", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<short> observableGauge2 = meter.CreateObservableGauge<short>("observableGauge2", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<int> observableGauge3 = meter.CreateObservableGauge<int>("observableGauge3", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<long> observableGauge4 = meter.CreateObservableGauge<long>("observableGauge4", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<float> observableGauge5 = meter.CreateObservableGauge<float>("observableGauge5", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<double> observableGauge6 = meter.CreateObservableGauge<double>("observableGauge6", () => 0, "seconds", "Seconds ObservableGauge");
                ObservableGauge<decimal> observableGauge7 = meter.CreateObservableGauge<decimal>("observableGauge7", () => 0, "seconds", "Seconds ObservableGauge");

                Assert.Throws<InvalidOperationException>(() => meter.CreateCounter<uint>("Counter", "seconds", "Seconds Counter"));
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

                    Assert.Equal(4, instrumentsEncountered);

                    // Enable listening to the 4 instruments

                    listener.EnableMeasurementEvents(counter, counter);
                    listener.EnableMeasurementEvents(observableGauge, observableGauge);
                    listener.EnableMeasurementEvents(histogram, histogram);
                    listener.EnableMeasurementEvents(observableCounter, observableCounter);

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
                    ObservableCounter<int>  counter3 = meter.CreateObservableCounter<int>("observableCounter3", () => 5);
                    ObservableGauge<int>    gauge3   = meter.CreateObservableGauge<int>("observableGauge3", () => 7);

                    listener.EnableMeasurementEvents(counter1, null);
                    listener.EnableMeasurementEvents(gauge1, null);
                    listener.EnableMeasurementEvents(counter2, null);
                    listener.EnableMeasurementEvents(gauge2, null);
                    listener.EnableMeasurementEvents(counter3, null);
                    listener.EnableMeasurementEvents(gauge3, null);

                    int accumulated = 0;

                    listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => accumulated += measurement);

                    Exception exception = Record.Exception(() => listener.RecordObservableInstruments());
                    Assert.NotNull(exception);
                    Assert.IsType<AggregateException>(exception);
                    AggregateException ae = exception as AggregateException;
                    Assert.Equal(4, ae.InnerExceptions.Count);

                    Assert.IsType<ArgumentOutOfRangeException>(ae.InnerExceptions[0]);
                    Assert.IsType<ArgumentException>(ae.InnerExceptions[1]);
                    Assert.IsType<PlatformNotSupportedException>(ae.InnerExceptions[2]);
                    Assert.IsType<NullReferenceException>(ae.InnerExceptions[3]);

                    // Ensure the instruments which didn't throw reported correct measurements.
                    Assert.Equal(12, accumulated);
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void InstrumentMeasurementTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("InstrumentMeasurementTest");

                Counter<byte> counter = meter.CreateCounter<byte>("byteCounter");
                InstruementMeasurementAggregationValidation(counter, (value, tags) => { counter.Add(value, tags); } );

                Counter<short> counter1 = meter.CreateCounter<short>("shortCounter");
                InstruementMeasurementAggregationValidation(counter1, (value, tags) => { counter1.Add(value, tags); } );

                Counter<int> counter2 = meter.CreateCounter<int>("intCounter");
                InstruementMeasurementAggregationValidation(counter2, (value, tags) => { counter2.Add(value, tags); } );

                Counter<long> counter3 = meter.CreateCounter<long>("longCounter");
                InstruementMeasurementAggregationValidation(counter3, (value, tags) => { counter3.Add(value, tags); } );

                Counter<float> counter4 = meter.CreateCounter<float>("floatCounter");
                InstruementMeasurementAggregationValidation(counter4, (value, tags) => { counter4.Add(value, tags); } );

                Counter<double> counter5 = meter.CreateCounter<double>("doubleCounter");
                InstruementMeasurementAggregationValidation(counter5, (value, tags) => { counter5.Add(value, tags); } );

                Counter<decimal> counter6 = meter.CreateCounter<decimal>("decimalCounter");
                InstruementMeasurementAggregationValidation(counter6, (value, tags) => { counter6.Add(value, tags); } );

                Histogram<byte> histogram = meter.CreateHistogram<byte>("byteHistogram");
                InstruementMeasurementAggregationValidation(histogram, (value, tags) => { histogram.Record(value, tags); } );

                Histogram<short> histogram1 = meter.CreateHistogram<short>("shortHistogram");
                InstruementMeasurementAggregationValidation(histogram1, (value, tags) => { histogram1.Record(value, tags); } );

                Histogram<int> histogram2 = meter.CreateHistogram<int>("intHistogram");
                InstruementMeasurementAggregationValidation(histogram2, (value, tags) => { histogram2.Record(value, tags); } );

                Histogram<long> histogram3 = meter.CreateHistogram<long>("longHistogram");
                InstruementMeasurementAggregationValidation(histogram3, (value, tags) => { histogram3.Record(value, tags); } );

                Histogram<float> histogram4 = meter.CreateHistogram<float>("floatHistogram");
                InstruementMeasurementAggregationValidation(histogram4, (value, tags) => { histogram4.Record(value, tags); } );

                Histogram<double> histogram5 = meter.CreateHistogram<double>("doubleHistogram");
                InstruementMeasurementAggregationValidation(histogram5, (value, tags) => { histogram5.Record(value, tags); } );

                Histogram<decimal> histogram6 = meter.CreateHistogram<decimal>("decimalHistogram");
                InstruementMeasurementAggregationValidation(histogram6, (value, tags) => { histogram6.Record(value, tags); } );

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
                ObservableInstruementMeasurementAggregationValidation(observableCounter, new Measurement<byte>[] { new Measurement<byte>(50)});
                ObservableCounter<short> observableCounter1 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => 30_000);
                ObservableInstruementMeasurementAggregationValidation(observableCounter1, new Measurement<short>[] { new Measurement<short>(30_000)});
                ObservableCounter<int> observableCounter2 = meter.CreateObservableCounter<int>("IntObservableCounter", () => 1_000_000);
                ObservableInstruementMeasurementAggregationValidation(observableCounter2, new Measurement<int>[] { new Measurement<int>(1_000_000)});
                ObservableCounter<long> observableCounter3 = meter.CreateObservableCounter<long>("longObservableCounter", () => 1_000_000_000);
                ObservableInstruementMeasurementAggregationValidation(observableCounter3, new Measurement<long>[] { new Measurement<long>(1_000_000_000)});
                ObservableCounter<float> observableCounter4 = meter.CreateObservableCounter<float>("floatObservableCounter", () => 3.14f);
                ObservableInstruementMeasurementAggregationValidation(observableCounter4, new Measurement<float>[] { new Measurement<float>(3.14f)});
                ObservableCounter<double> observableCounter5 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => 1e6);
                ObservableInstruementMeasurementAggregationValidation(observableCounter5, new Measurement<double>[] { new Measurement<double>(1e6)});
                ObservableCounter<decimal> observableCounter6 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => 1.5E6m);
                ObservableInstruementMeasurementAggregationValidation(observableCounter6, new Measurement<decimal>[] { new Measurement<decimal>(1.5E6m)});

                //
                // CreateObservableGauge using Func<T>
                //
                ObservableGauge<byte> observableGauge = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => 100);
                ObservableInstruementMeasurementAggregationValidation(observableGauge, new Measurement<byte>[] { new Measurement<byte>(100)});
                ObservableGauge<short> observableGauge1 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => 30_123);
                ObservableInstruementMeasurementAggregationValidation(observableGauge1, new Measurement<short>[] { new Measurement<short>(30_123)});
                ObservableGauge<int> observableGauge2 = meter.CreateObservableGauge<int>("IntObservableGauge", () => 2_123_456);
                ObservableInstruementMeasurementAggregationValidation(observableGauge2, new Measurement<int>[] { new Measurement<int>(2_123_456)});
                ObservableGauge<long> observableGauge3 = meter.CreateObservableGauge<long>("longObservableGauge", () => 3_123_456_789);
                ObservableInstruementMeasurementAggregationValidation(observableGauge3, new Measurement<long>[] { new Measurement<long>(3_123_456_789)});
                ObservableGauge<float> observableGauge4 = meter.CreateObservableGauge<float>("floatObservableGauge", () => 1.6f);
                ObservableInstruementMeasurementAggregationValidation(observableGauge4, new Measurement<float>[] { new Measurement<float>(1.6f)});
                ObservableGauge<double> observableGauge5 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => 1e5);
                ObservableInstruementMeasurementAggregationValidation(observableGauge5, new Measurement<double>[] { new Measurement<double>(1e5)});
                ObservableGauge<decimal> observableGauge6 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => 2.5E7m);
                ObservableInstruementMeasurementAggregationValidation(observableGauge6, new Measurement<decimal>[] { new Measurement<decimal>(2.5E7m)});

                //
                // CreateObservableCounter using Func<Measurement<T>>
                //
                Measurement<byte> byteMeasurement = new Measurement<byte>(60, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T1", "V1"), new KeyValuePair<string, object?>("T2", "V2") });
                ObservableCounter<byte> observableCounter7 = meter.CreateObservableCounter<byte>("ByteObservableCounter", () => byteMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter7, new Measurement<byte>[] { byteMeasurement });

                Measurement<short> shortMeasurement = new Measurement<short>(20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T3", "V3"), new KeyValuePair<string, object?>("T4", "V4") });
                ObservableCounter<short> observableCounter8 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => shortMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter8, new Measurement<short>[] { shortMeasurement });

                Measurement<int> intMeasurement = new Measurement<int>(2_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T5", "V5"), new KeyValuePair<string, object?>("T6", "V6") });
                ObservableCounter<int> observableCounter9 = meter.CreateObservableCounter<int>("IntObservableCounter", () => intMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter9, new Measurement<int>[] { intMeasurement });

                Measurement<long> longMeasurement = new Measurement<long>(20_000_000_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T7", "V7"), new KeyValuePair<string, object?>("T8", "V8") });
                ObservableCounter<long> observableCounter10 = meter.CreateObservableCounter<long>("longObservableCounter", () => longMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter10, new Measurement<long>[] { longMeasurement });

                Measurement<float> floatMeasurement = new Measurement<float>(1e2f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T9", "V10"), new KeyValuePair<string, object?>("T11", "V12") });
                ObservableCounter<float> observableCounter11 = meter.CreateObservableCounter<float>("floatObservableCounter", () => 3.14f);
                ObservableInstruementMeasurementAggregationValidation(observableCounter11, new Measurement<float>[] { new Measurement<float>(3.14f)});

                Measurement<double> doubleMeasurement = new Measurement<double>(2.5e7, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T13", "V14"), new KeyValuePair<string, object?>("T15", "V16") });
                ObservableCounter<double> observableCounter12 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => doubleMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter12, new Measurement<double>[] { doubleMeasurement });

                Measurement<decimal> decimalMeasurement = new Measurement<decimal>(3.2e20m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T17", "V18"), new KeyValuePair<string, object?>("T19", "V20") });
                ObservableCounter<decimal> observableCounter13 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => decimalMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableCounter13, new Measurement<decimal>[] { decimalMeasurement });

                //
                // CreateObservableGauge using Func<Measurement<T>>
                //
                Measurement<byte> byteGaugeMeasurement = new Measurement<byte>(35, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T21", "V22"), new KeyValuePair<string, object?>("T23", "V24") });
                ObservableGauge<byte> observableGauge7 = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => byteGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge7, new Measurement<byte>[] { byteGaugeMeasurement });

                Measurement<short> shortGaugeMeasurement = new Measurement<short>(23_987, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T25", "V26"), new KeyValuePair<string, object?>("T27", "V28") });
                ObservableGauge<short> observableGauge8 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => shortGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge8, new Measurement<short>[] { shortGaugeMeasurement });

                Measurement<int> intGaugeMeasurement = new Measurement<int>(1_987_765, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T29", "V30"), new KeyValuePair<string, object?>("T31", "V32") });
                ObservableGauge<int> observableGauge9 = meter.CreateObservableGauge<int>("IntObservableGauge", () => intGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge9, new Measurement<int>[] { intGaugeMeasurement });

                Measurement<long> longGaugeMeasurement = new Measurement<long>(10_000_234_343, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T33", "V342"), new KeyValuePair<string, object?>("T35", "V36") });
                ObservableGauge<long> observableGauge10 = meter.CreateObservableGauge<long>("longObservableGauge", () => longGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge10, new Measurement<long>[] { longGaugeMeasurement });

                Measurement<float> floatGaugeMeasurement = new Measurement<float>(2.1f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T37", "V38"), new KeyValuePair<string, object?>("T39", "V40") });
                ObservableGauge<float> observableGauge11 = meter.CreateObservableGauge<float>("floatObservableGauge", () => floatGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge11, new Measurement<float>[] { floatGaugeMeasurement });

                Measurement<double> doubleGaugeMeasurement = new Measurement<double>(1.5e30, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T41", "V42"), new KeyValuePair<string, object?>("T43", "V44") });
                ObservableGauge<double> observableGauge12 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => doubleGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge12, new Measurement<double>[] { doubleGaugeMeasurement });

                Measurement<decimal> decimalGaugeMeasurement = new Measurement<decimal>(2.5e20m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T45", "V46"), new KeyValuePair<string, object?>("T47", "V48") });
                ObservableGauge<decimal> observableGauge13 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => decimalGaugeMeasurement);
                ObservableInstruementMeasurementAggregationValidation(observableGauge13, new Measurement<decimal>[] { decimalGaugeMeasurement });

                //
                // CreateObservableCounter using Func<Measurement<T>>
                //
                Measurement<byte>[] byteGaugeMeasurementList = new Measurement<byte>[]
                {
                    new Measurement<byte>(0, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T48", "V49"), new KeyValuePair<string, object?>("T50", "V51") }),
                    new Measurement<byte>(1, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T51", "V52"), new KeyValuePair<string, object?>("T53", "V54") }),
                };
                ObservableCounter<byte> observableCounter14 = meter.CreateObservableCounter<byte>("ByteObservableCounter", () => byteGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter14, byteGaugeMeasurementList);

                Measurement<short>[] shortGaugeMeasurementList = new Measurement<short>[]
                {
                    new Measurement<short>(20_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T55", "V56"), new KeyValuePair<string, object?>("T57", "V58") }),
                    new Measurement<short>(30_000, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T59", "V60"), new KeyValuePair<string, object?>("T61", "V62") }),
                };
                ObservableCounter<short> observableCounter15 = meter.CreateObservableCounter<short>("ShortObservableCounter", () => shortGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter15, shortGaugeMeasurementList);

                Measurement<int>[] intGaugeMeasurementList = new Measurement<int>[]
                {
                    new Measurement<int>(1_000_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T63", "V64"), new KeyValuePair<string, object?>("T65", "V66") }),
                    new Measurement<int>(1_000_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T67", "V68"), new KeyValuePair<string, object?>("T69", "V70") }),
                };
                ObservableCounter<int> observableCounter16 = meter.CreateObservableCounter<int>("IntObservableCounter", () => intGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter16, intGaugeMeasurementList);

                Measurement<long>[] longGaugeMeasurementList = new Measurement<long>[]
                {
                    new Measurement<long>(1_000_001_001, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T71", "V72"), new KeyValuePair<string, object?>("T73", "V74") }),
                    new Measurement<long>(1_000_002_002, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T75", "V76"), new KeyValuePair<string, object?>("T77", "V78") }),
                };
                ObservableCounter<long> observableCounter17 = meter.CreateObservableCounter<long>("longObservableCounter", () => longGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter17, longGaugeMeasurementList);

                Measurement<float>[] floatGaugeMeasurementList = new Measurement<float>[]
                {
                    new Measurement<float>(68.15e8f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T79", "V80"), new KeyValuePair<string, object?>("T81", "V82") }),
                    new Measurement<float>(68.15e6f, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T83", "V84"), new KeyValuePair<string, object?>("T85", "V86") }),
                };
                ObservableCounter<float> observableCounter18 = meter.CreateObservableCounter<float>("floatObservableCounter", () => floatGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter18, floatGaugeMeasurementList);

                Measurement<double>[] doubleGaugeMeasurementList = new Measurement<double>[]
                {
                    new Measurement<double>(68.15e20, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T87", "V88"), new KeyValuePair<string, object?>("T89", "V90") }),
                    new Measurement<double>(68.15e21, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T91", "V92"), new KeyValuePair<string, object?>("T93", "V94") }),
                };
                ObservableCounter<double> observableCounter19 = meter.CreateObservableCounter<double>("doubleObservableCounter", () => doubleGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter19, doubleGaugeMeasurementList);

                Measurement<decimal>[] decimalGaugeMeasurementList = new Measurement<decimal>[]
                {
                    new Measurement<decimal>(68.15e8m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T95", "V96"), new KeyValuePair<string, object?>("T97", "V98") }),
                    new Measurement<decimal>(68.15e6m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("T99", "V100"), new KeyValuePair<string, object?>("T101", "V102") }),
                };
                ObservableCounter<decimal> observableCounter20 = meter.CreateObservableCounter<decimal>("decimalObservableCounter", () => decimalGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableCounter20, decimalGaugeMeasurementList);

                //
                // CreateObservableGauge using IEnumerable<Measurement<T>>
                //
                ObservableGauge<byte> observableGauge14 = meter.CreateObservableGauge<byte>("ByteObservableGauge", () => byteGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge14, byteGaugeMeasurementList);

                ObservableGauge<short> observableGauge15 = meter.CreateObservableGauge<short>("ShortObservableGauge", () => shortGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge15, shortGaugeMeasurementList);

                ObservableGauge<int> observableGauge16 = meter.CreateObservableGauge<int>("IntObservableGauge", () => intGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge16, intGaugeMeasurementList);

                ObservableGauge<long> observableGauge17 = meter.CreateObservableGauge<long>("longObservableGauge", () => longGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge17, longGaugeMeasurementList);

                ObservableGauge<float> observableGauge18 = meter.CreateObservableGauge<float>("floatObservableGauge", () => floatGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge18, floatGaugeMeasurementList);

                ObservableGauge<double> observableGauge19 = meter.CreateObservableGauge<double>("doubleObservableGauge", () => doubleGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge19, doubleGaugeMeasurementList);

                ObservableGauge<decimal> observableGauge20 = meter.CreateObservableGauge<decimal>("decimalObservableGauge", () => decimalGaugeMeasurementList);
                ObservableInstruementMeasurementAggregationValidation(observableGauge20, decimalGaugeMeasurementList);

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

                Counter<int> counter = meter1.CreateCounter<int>("Counter");
                Histogram<double> histogram = meter2.CreateHistogram<double>("Histogram");
                ObservableCounter<long> observableCounter = meter3.CreateObservableCounter<long>("ObservableCounter", () => new Measurement<long>(10, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                ObservableGauge<decimal> observableGauge = meter4.CreateObservableGauge<decimal>("ObservableGauge", () => new Measurement<decimal>(5.7m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));

                using MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int count = 0;

                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state)     => count++);
                listener.SetMeasurementEventCallback<double>((inst, measurement, tags, state)  => count++);
                listener.SetMeasurementEventCallback<long>((inst, measurement, tags, state)    => count++);
                listener.SetMeasurementEventCallback<decimal>((inst, measurement, tags, state) => count++);

                listener.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(1, count);

                histogram.Record(1);
                Assert.Equal(2, count);

                listener.RecordObservableInstruments();
                Assert.Equal(4, count);

                meter1.Dispose();
                counter.Add(1);
                Assert.Equal(4, count);

                meter2.Dispose();
                histogram.Record(1);
                Assert.Equal(4, count);

                listener.RecordObservableInstruments();
                Assert.Equal(6, count);

                meter3.Dispose();
                listener.RecordObservableInstruments();
                Assert.Equal(7, count);

                meter4.Dispose();
                listener.RecordObservableInstruments();
                Assert.Equal(7, count);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ListenerDisposalsTest()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("ListenerDisposalsTest");

                Counter<int> counter = meter.CreateCounter<int>("Counter");
                Histogram<double> histogram = meter.CreateHistogram<double>("Histogram");
                ObservableCounter<long> observableCounter = meter.CreateObservableCounter<long>("ObservableCounter", () => new Measurement<long>(10, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));
                ObservableGauge<decimal> observableGauge = meter.CreateObservableGauge<decimal>("ObservableGauge", () => new Measurement<decimal>(5.7m, new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key", "value")}));

                int completedMeasurements = 0;
                MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);
                listener.MeasurementsCompleted = (theInstrument, state) => completedMeasurements++;

                int count = 0;

                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state)     => count++);
                listener.SetMeasurementEventCallback<double>((inst, measurement, tags, state)  => count++);
                listener.SetMeasurementEventCallback<long>((inst, measurement, tags, state)    => count++);
                listener.SetMeasurementEventCallback<decimal>((inst, measurement, tags, state) => count++);

                listener.Start();

                Assert.Equal(0, count);

                counter.Add(1);
                Assert.Equal(1, count);

                histogram.Record(1);
                Assert.Equal(2, count);

                listener.RecordObservableInstruments();
                Assert.Equal(4, count);

                listener.Dispose();
                Assert.Equal(4, completedMeasurements);

                counter.Add(1);
                Assert.Equal(4, count);

                histogram.Record(1);
                Assert.Equal(4, count);

                listener.RecordObservableInstruments();
                Assert.Equal(4, count);
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
        public void EnableListeneingMultipleTimesWithDifferentState()
        {
            RemoteExecutor.Invoke(() => {
                Meter meter = new Meter("EnableListeneingMultipleTimesWithDifferentState");

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
                Histogram<int> histogram = meter.CreateHistogram<int>("Histogram");
                ObservableCounter<int> observableCounter = meter.CreateObservableCounter<int>("ObservableCounter", () => 1);
                ObservableGauge<int> observableGauge = meter.CreateObservableGauge<int>("ObservableGauge", () => 1);

                MeterListener listener = new MeterListener();
                listener.InstrumentPublished = (theInstrument, theListener) => theListener.EnableMeasurementEvents(theInstrument, theInstrument);

                int totalCount = 0;
                listener.SetMeasurementEventCallback<int>((inst, measurement, tags, state) => Interlocked.Add(ref totalCount, measurement));
                listener.Start();

                Task[] taskList = new Task[6];

                int loopLength = 10_000;

                taskList[0] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { counter.Add(1); } });
                taskList[1] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { counter.Add(1); } });
                taskList[2] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { histogram.Record(1); } });
                taskList[3] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { histogram.Record(1); } });
                taskList[4] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { listener.RecordObservableInstruments(); } });
                taskList[5] = Task.Factory.StartNew(() => { for (int i = 0; i < loopLength; i++) { listener.RecordObservableInstruments(); } });

                Task.WaitAll(taskList);

                Assert.Equal(loopLength * 8, totalCount);
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
                    Histogram<int> histogram = meter.CreateHistogram<int>("histogram");

                    listener.EnableMeasurementEvents(counter, counter);
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
                    histogram.Record(10, new TagList());

                    // 1 Tags
                    expectedTags = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("Key1", "Value1") };
                    counter.Add(10, new TagList() { expectedTags[0] });
                    histogram.Record(10, new TagList() { new KeyValuePair<string, object?>("Key1", "Value1") });

                    // 2 Tags
                    expectedTags = new List<KeyValuePair<string, object?>>
                    {
                        {"Key1", "Value1"},
                        {"Key2", "Value2"}
                    }.ToArray();

                    counter.Add(10, new TagList() { expectedTags[0], expectedTags[1] });
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
                    histogram.Record(10, new TagList() { expectedTags[0], expectedTags[1], expectedTags[2], expectedTags[3], expectedTags[4], expectedTags[5], expectedTags[6], expectedTags[7],
                                                     expectedTags[8], expectedTags[9], expectedTags[10], expectedTags[11], expectedTags[12] });
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

        private void InstruementMeasurementAggregationValidation<T>(Instrument<T> instrument, Action<T, KeyValuePair<string, object?>[]> record) where T : struct
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

            for (byte i = 0; i < 100; i++)
            {
                expectedTags.Add(new KeyValuePair<string, object?>(i.ToString(), i.ToString()));
                expectedValue = ConvertValue<T>(i);
                record(expectedValue, expectedTags.ToArray());
            }

            Assert.Equal(100, counter);
        }

        private void ObservableInstruementMeasurementAggregationValidation<T>(ObservableInstrument<T> instrument, Measurement<T>[] expectedResult) where T : struct
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

        private T ConvertValue<T>(byte value) where T : struct
        {
            if (typeof(T) == typeof(byte))  { return (T)(object)value;}
            if (typeof(T) == typeof(short)) { return (T)(object)Convert.ToInt16(value); }
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
    }
}
