// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class ExponentialHistogramTests
    {
        [Fact]
        public void HappyPath()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5, 0.95);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(3, stats.Quantiles[0].Value);
            Assert.Equal(0.95, stats.Quantiles[1].Quantile);
            Assert.Equal(5, stats.Quantiles[1].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void MinMax()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.0, 1.0);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(0.0, stats.Quantiles[0].Quantile);
            Assert.Equal(1, stats.Quantiles[0].Value);
            Assert.Equal(1.0, stats.Quantiles[1].Quantile);
            Assert.Equal(5, stats.Quantiles[1].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void NoQuantiles()
        {
            QuantileAggregation quantiles = new QuantileAggregation();
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();
            Assert.Equal(0, stats.Quantiles.Length);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void OutOfBoundsQuantiles()
        {
            QuantileAggregation quantiles = new QuantileAggregation(-3, 100);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(-3, stats.Quantiles[0].Quantile);
            Assert.Equal(1, stats.Quantiles[0].Value);
            Assert.Equal(100, stats.Quantiles[1].Quantile);
            Assert.Equal(5, stats.Quantiles[1].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void UnorderedQuantiles()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.9, 0.1);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(0.1, stats.Quantiles[0].Quantile);
            Assert.Equal(1, stats.Quantiles[0].Value);
            Assert.Equal(0.9, stats.Quantiles[1].Quantile);
            Assert.Equal(5, stats.Quantiles[1].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void DifferencesLessThanErrorBound()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(90.01);
            aggregator.Update(90.01);
            aggregator.Update(90.02);
            aggregator.Update(90.03);
            aggregator.Update(90.04);
            aggregator.Update(100.01);
            aggregator.Update(100.01);
            aggregator.Update(100.02);
            aggregator.Update(100.03);
            aggregator.Update(100.04);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(100, stats.Quantiles[0].Value);
            Assert.True(Math.Abs(100.01 - stats.Quantiles[0].Value) <= 100.01 * quantiles.MaxRelativeError);
            Assert.Equal(10, stats.Count);
            Assert.Equal(950.22, stats.Sum);
        }

        [Fact]
        public void DifferencesGreaterThanErrorBound()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            quantiles.MaxRelativeError = 0.0001;
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(90.01);
            aggregator.Update(90.01);
            aggregator.Update(90.02);
            aggregator.Update(90.03);
            aggregator.Update(90.04);
            aggregator.Update(100.01);
            aggregator.Update(100.01);
            aggregator.Update(100.02);
            aggregator.Update(100.03);
            aggregator.Update(100.04);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.Equal(0.5, stats.Quantiles[0].Quantile);

            //At default error of 0.001 result of 100 would be acceptable, but with higher precision it is not
            Assert.True(100 < stats.Quantiles[0].Value);
            Assert.True(Math.Abs(100.01 - stats.Quantiles[0].Value) <= 100.01 * quantiles.MaxRelativeError);
            Assert.Equal(10, stats.Count);
            Assert.Equal(950.22, stats.Sum);
        }

        [Fact]
        public void NoUpdates()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);
        }

        [Fact]
        public void OneUpdate()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(99);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(99, stats.Quantiles[0].Value);
            Assert.Equal(1, stats.Count);
            Assert.Equal(99, stats.Sum);
        }

        [Fact]
        public void NoUpdatesInSomeIntervals()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();
            stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);
        }

        [Fact]
        public void UpdatesAfterNoUpdates()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            var stats = (HistogramStatistics)aggregator.Collect();
            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);

            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(3, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);
        }

        [Fact]
        public void IterateCollect()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(1);
            aggregator.Update(2);
            aggregator.Update(3);
            aggregator.Update(4);
            aggregator.Update(5);
            var stats = (HistogramStatistics)aggregator.Collect();
            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(3, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(15, stats.Sum);

            aggregator.Update(9);
            aggregator.Update(8);
            aggregator.Update(7);
            aggregator.Update(6);
            aggregator.Update(5);
            stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(7, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(35, stats.Sum);
        }

        [Fact]
        public void NegativeValues()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(-1);
            aggregator.Update(-2);
            aggregator.Update(-3);
            aggregator.Update(-4);
            aggregator.Update(-5);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(-3, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(-15, stats.Sum);
        }

        [Fact]
        public void ZeroValues()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(0);
            aggregator.Update(0);
            aggregator.Update(0);
            aggregator.Update(0);
            aggregator.Update(0);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(0, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(0, stats.Sum);
        }

        [Fact]
        public void MixedValues()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(19);
            aggregator.Update(-4);
            aggregator.Update(100_000);
            aggregator.Update(-0.5);
            aggregator.Update(-189.3231);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(1, stats.Quantiles.Length);
            Assert.Equal(0.5, stats.Quantiles[0].Quantile);
            Assert.Equal(-0.5, stats.Quantiles[0].Value);
            Assert.Equal(5, stats.Count);
            Assert.Equal(99825.1769, stats.Sum);
        }

        [Fact]
        public void FilterNaNAndInfinities()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0,1);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(double.NaN);
            aggregator.Update(-double.NaN);
            aggregator.Update(double.PositiveInfinity);
            aggregator.Update(double.NegativeInfinity);
            aggregator.Update(100);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(2, stats.Quantiles.Length);
            Assert.Equal(0, stats.Quantiles[0].Quantile);
            Assert.Equal(100, stats.Quantiles[0].Value);
            Assert.Equal(1, stats.Quantiles[1].Quantile);
            Assert.Equal(100, stats.Quantiles[1].Value);
            Assert.Equal(1, stats.Count);
            Assert.Equal(100, stats.Sum);
        }

        [Fact]
        public void FilterOnlyNaNAndInfinities()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0, 1);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);

            aggregator.Update(double.NaN);
            aggregator.Update(-double.NaN);
            aggregator.Update(double.PositiveInfinity);
            aggregator.Update(double.NegativeInfinity);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(32)]
        public void ConcurrentUpdatesAndCollectsPreserveEveryMeasurement(int producerCount)
        {
            const int Iterations = 20_000;
            ExponentialHistogramAggregator aggregator = new(new QuantileAggregation(0, 0.5, 1));
            using Barrier barrier = new(producerCount + 1);
            int producersRemaining = producerCount;
            int collectedCount = 0;
            double collectedSum = 0;
            Exception? collectorException = null;
            Exception? producerException = null;

            Thread collector = new(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    while (Volatile.Read(ref producersRemaining) != 0)
                    {
                        Collect();
                        Thread.Yield();
                    }

                    Collect();
                }
                catch (Exception e)
                {
                    collectorException = e;
                }

                void Collect()
                {
                    HistogramStatistics stats = (HistogramStatistics)aggregator.Collect();
                    if (stats.Count != 0)
                    {
                        Assert.All(stats.Quantiles, quantile => Assert.Equal(1, quantile.Value));
                        Assert.Equal(stats.Count, stats.Sum);
                    }

                    collectedCount += stats.Count;
                    collectedSum += stats.Sum;
                }
            });

            Thread[] producers = new Thread[producerCount];
            for (int i = 0; i < producers.Length; i++)
            {
                producers[i] = new(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int j = 0; j < Iterations; j++)
                        {
                            aggregator.Update(1);
                        }

                        aggregator.Update(double.NaN);
                        aggregator.Update(double.PositiveInfinity);
                        aggregator.Update(double.NegativeInfinity);
                    }
                    catch (Exception e)
                    {
                        Interlocked.CompareExchange(ref producerException, e, null);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref producersRemaining);
                    }
                });
                producers[i].Start();
            }

            collector.Start();
            foreach (Thread producer in producers)
            {
                producer.Join();
            }
            collector.Join();

            Assert.Null(collectorException);
            Assert.Null(producerException);
            Assert.Equal(producerCount * Iterations, collectedCount);
            Assert.Equal(collectedCount, collectedSum);
        }

        [Fact]
        public void ConcurrentUpdatesThenIdleCollectionsAndReuse()
        {
            const int ProducerCount = 32;
            const int Iterations = 10_000;
            ExponentialHistogramAggregator aggregator = new(new QuantileAggregation(0.5));
            using Barrier barrier = new(ProducerCount + 1);
            Exception? producerException = null;

            Thread[] producers = new Thread[ProducerCount];
            for (int i = 0; i < producers.Length; i++)
            {
                producers[i] = new(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int j = 0; j < Iterations; j++)
                        {
                            aggregator.Update(1);
                        }
                    }
                    catch (Exception e)
                    {
                        Interlocked.CompareExchange(ref producerException, e, null);
                    }
                });
                producers[i].Start();
            }

            barrier.SignalAndWait();
            foreach (Thread producer in producers)
            {
                producer.Join();
            }

            Assert.Null(producerException);
            Assert.True(GetUseStripes(aggregator));

            HistogramStatistics stats = (HistogramStatistics)aggregator.Collect();
            Assert.Equal(ProducerCount * Iterations, stats.Count);
            Assert.Equal(stats.Count, stats.Sum);
            Assert.Single(stats.Quantiles);
            Assert.Equal(1, stats.Quantiles[0].Value);

            object?[] retainedBuckets = GetStripeFields(aggregator, "Buckets");
            Assert.Contains(retainedBuckets, bucket => bucket is not null);

            aggregator.Update(2);
            object?[] dirtyFlags = GetStripeFields(aggregator, "Dirty");
            int dirtyStripe = Array.FindIndex(dirtyFlags, dirty => (bool)dirty!);
            Assert.NotEqual(-1, dirtyStripe);
            object?[] reusedBuckets = GetStripeFields(aggregator, "Buckets");
            Assert.Same(retainedBuckets[dirtyStripe], reusedBuckets[dirtyStripe]);

            stats = (HistogramStatistics)aggregator.Collect();
            Assert.Equal(1, stats.Count);
            Assert.Equal(2, stats.Sum);
            Assert.Single(stats.Quantiles);
            Assert.Equal(2, stats.Quantiles[0].Value);

            Assert.Contains(GetStripeFields(aggregator, "Buckets"), bucket => bucket is not null);
            stats = (HistogramStatistics)aggregator.Collect();
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);
            Assert.Empty(stats.Quantiles);
            Assert.All(GetStripeFields(aggregator, "Buckets"), Assert.Null);

            static bool GetUseStripes(ExponentialHistogramAggregator aggregator) =>
                (bool)typeof(ExponentialHistogramAggregator)
                    .GetField("_useStripes", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(aggregator)!;

            static object?[] GetStripeFields(ExponentialHistogramAggregator aggregator, string fieldName)
            {
                Array stripes = (Array)typeof(ExponentialHistogramAggregator)
                    .GetField("_stripes", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(aggregator)!;
                object?[] values = new object?[stripes.Length];
                for (int i = 0; i < stripes.Length; i++)
                {
                    object stripe = stripes.GetValue(i)!;
                    values[i] = stripe.GetType()
                        .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)!
                        .GetValue(stripe);
                }

                return values;
            }
        }
    }
}
