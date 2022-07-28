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
        }

        [Fact]
        public void NoUpdates()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            var stats = (HistogramStatistics)aggregator.Collect();

            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);
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
        }

        [Fact]
        public void UpdatesAfterNoUpdates()
        {
            QuantileAggregation quantiles = new QuantileAggregation(0.5);
            ExponentialHistogramAggregator aggregator = new ExponentialHistogramAggregator(quantiles);
            var stats = (HistogramStatistics)aggregator.Collect();
            Assert.NotNull(stats);
            Assert.Equal(0, stats.Quantiles.Length);

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
        }
    }
}
