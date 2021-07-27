// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class AggregationStoreTests
    {
        [Fact]
        public void GetDefaultAggregator()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            LastValue val = store.GetAggregator();

            Assert.NotNull(val);
            Assert.Equal(val, store.GetAggregator());
        }

        [Fact]
        public void GetNoLabels()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>();
            LastValue val = store.GetAggregator(span);

            Assert.NotNull(val);
            Assert.Equal(val, store.GetAggregator(span));
            Assert.Equal(val, store.GetAggregator());
        }

        [Fact]
        public void GetOneLabel()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "blue") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", 1) };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", "eight") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 1);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 1);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 1);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));
        }

        [Fact]
        public void GetTwoLabel()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "blue"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", 1), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", "eight"), new KeyValuePair<string, object?>("name", "ned") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 2);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 2);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 2);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 2);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));
        }

        [Fact]
        public void GetTwoLabelUnordered()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("name", "ned"), new KeyValuePair<string, object?>("color", "red") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 2);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 2);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, val2);
        }

        [Fact]
        public void GetThreeLabel()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "blue"),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("size", 1),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("size", "eight"),
                new KeyValuePair<string, object?>("name", "ned")
            };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 3);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 3);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 3);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 3);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));
        }

        [Fact]
        public void GetThreeLabelUnordered()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("color", "red")
            };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red")
            };
            KeyValuePair<string, object?>[] labels5 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("alpha", 15)
            };
            KeyValuePair<string, object?>[] labels6 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("alpha", 15)
            };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 3);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 3);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 3);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 3);
            var span5 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels5, 0, 3);
            var span6 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels6, 0, 3);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            LastValue val2 = store.GetAggregator(span2);
            Assert.Equal(val1, val2);
            LastValue val3 = store.GetAggregator(span3);
            Assert.Equal(val1, val3);
            LastValue val4 = store.GetAggregator(span4);
            Assert.Equal(val1, val4);
            LastValue val5 = store.GetAggregator(span5);
            Assert.Equal(val1, val5);
            LastValue val6 = store.GetAggregator(span6);
            Assert.Equal(val1, val6);
        }

        [Fact]
        public void GetFourLabel()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "blue"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("size", 1),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("size", "eight"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 4);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 4);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 4);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 4);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));
        }


        [Fact]
        public void GetFourLabelUnordered()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44),
                new KeyValuePair<string, object?>("color", "red")
            };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("four", 44),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("name", "ned")
            };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44),
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red")
            };
            KeyValuePair<string, object?>[] labels5 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("four", 44)
            };
            KeyValuePair<string, object?>[] labels6 = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("four", 44),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("alpha", 15)
            };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 4);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 4);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 4);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 4);
            var span5 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels5, 0, 4);
            var span6 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels6, 0, 4);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            LastValue val2 = store.GetAggregator(span2);
            Assert.Equal(val1, val2);
            LastValue val3 = store.GetAggregator(span3);
            Assert.Equal(val1, val3);
            LastValue val4 = store.GetAggregator(span4);
            Assert.Equal(val1, val4);
            LastValue val5 = store.GetAggregator(span5);
            Assert.Equal(val1, val5);
            LastValue val6 = store.GetAggregator(span6);
            Assert.Equal(val1, val6);
        }

        [Fact]
        public void GetMultiRank0Start()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "blue"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", 1), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", "eight"), new KeyValuePair<string, object?>("name", "ned") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 2);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 2);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 2);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 2);

            var span = new ReadOnlySpan<KeyValuePair<string, object?>>();
            LastValue val = store.GetAggregator(span);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));

            Assert.Equal(val, store.GetAggregator(span));
            Assert.NotEqual(val, val1);
            Assert.NotEqual(val, val2);
            Assert.NotEqual(val, val3);
            Assert.NotEqual(val, val4);
        }

        [Fact]
        public void GetMultiRank1Start()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "blue"), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", 1), new KeyValuePair<string, object?>("name", "ned") };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("size", "eight"), new KeyValuePair<string, object?>("name", "ned") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 2);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 2);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 2);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 2);

            KeyValuePair<string, object?>[] labels = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red") };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>(labels, 0, 1);
            LastValue val = store.GetAggregator(span);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));

            Assert.Equal(val, store.GetAggregator(span));
            Assert.NotEqual(val, val1);
            Assert.NotEqual(val, val2);
            Assert.NotEqual(val, val3);
            Assert.NotEqual(val, val4);
        }

        [Fact]
        public void GetMultiRank2Start()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "blue") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", 1) };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", "eight") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 1);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 1);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 1);

            KeyValuePair<string, object?>[] labels = new KeyValuePair<string, object?>[]
            { new KeyValuePair<string, object?>("color", "red"), new KeyValuePair<string, object?>("name", "ned") };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>(labels, 0, 2);
            LastValue val = store.GetAggregator(span);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));

            Assert.Equal(val, store.GetAggregator(span));
            Assert.NotEqual(val, val1);
            Assert.NotEqual(val, val2);
            Assert.NotEqual(val, val3);
            Assert.NotEqual(val, val4);
        }

        [Fact]
        public void GetMultiRank3Start()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "blue") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", 1) };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", "eight") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 1);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 1);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 1);

            KeyValuePair<string, object?>[] labels = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned")
            };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>(labels, 0, 3);
            LastValue val = store.GetAggregator(span);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));

            Assert.Equal(val, store.GetAggregator(span));
            Assert.NotEqual(val, val1);
            Assert.NotEqual(val, val2);
            Assert.NotEqual(val, val3);
            Assert.NotEqual(val, val4);
        }

        [Fact]
        public void GetMultiRank4Start()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => new LastValue());
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            KeyValuePair<string, object?>[] labels2 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "blue") };
            KeyValuePair<string, object?>[] labels3 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", 1) };
            KeyValuePair<string, object?>[] labels4 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("size", "eight") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);
            var span2 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels2, 0, 1);
            var span3 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels3, 0, 1);
            var span4 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels4, 0, 1);

            KeyValuePair<string, object?>[] labels = new KeyValuePair<string, object?>[]
            {
                new KeyValuePair<string, object?>("alpha", 15),
                new KeyValuePair<string, object?>("color", "red"),
                new KeyValuePair<string, object?>("name", "ned"),
                new KeyValuePair<string, object?>("four", 44)
            };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>(labels, 0, 4);
            LastValue val = store.GetAggregator(span);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val2 = store.GetAggregator(span2);
            Assert.NotNull(val2);
            Assert.NotEqual(val1, val2);
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val3 = store.GetAggregator(span3);
            Assert.NotNull(val3);
            Assert.NotEqual(val1, val3);
            Assert.NotEqual(val2, val3);
            Assert.Equal(val3, store.GetAggregator(span3));
            LastValue val4 = store.GetAggregator(span4);
            Assert.NotNull(val4);
            Assert.NotEqual(val1, val4);
            Assert.NotEqual(val2, val4);
            Assert.NotEqual(val3, val4);
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val1, store.GetAggregator(span1));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val4, store.GetAggregator(span4));
            Assert.Equal(val2, store.GetAggregator(span2));
            Assert.Equal(val3, store.GetAggregator(span3));
            Assert.Equal(val3, store.GetAggregator(span3));

            Assert.Equal(val, store.GetAggregator(span));
            Assert.NotEqual(val, val1);
            Assert.NotEqual(val, val2);
            Assert.NotEqual(val, val3);
            Assert.NotEqual(val, val4);
        }

        [Fact]
        public void AggregatorLimitReached_NoLabel()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => null);
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>();
            LastValue val = store.GetAggregator(span);

            Assert.Null(val);
            Assert.Equal(val, store.GetAggregator(span));
            Assert.Equal(val, store.GetAggregator());
        }

        [Fact]
        public void AggregatorLimitReached_WithLabels()
        {
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() => null);
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);
            LastValue val = store.GetAggregator(span1);

            Assert.Null(val);
            Assert.Equal(val, store.GetAggregator(span1));
        }

        [Fact]
        public void AggregatorLimitReached_Multisize_NoLabel()
        {
            int count = 1;
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() =>
            {
                if (count > 0)
                {
                    count--;
                    return new LastValue();
                }
                return null;
            });
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>();
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);

            LastValue val1 = store.GetAggregator(span1);
            Assert.NotNull(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            LastValue val0 = store.GetAggregator(span);
            Assert.Null(val0);
            Assert.Equal(val0, store.GetAggregator(span));
        }

        [Fact]
        public void AggregatorLimitReached_Multisize_TwoLabel()
        {
            int count = 1;
            AggregatorStore<LastValue> store = new AggregatorStore<LastValue>(() =>
            {
                if (count > 0)
                {
                    count--;
                    return new LastValue();
                }
                return null;
            });
            KeyValuePair<string, object?>[] labels1 = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("color", "red") };
            var span = new ReadOnlySpan<KeyValuePair<string, object?>>();
            var span1 = new ReadOnlySpan<KeyValuePair<string, object?>>(labels1, 0, 1);

            LastValue val0 = store.GetAggregator(span);
            Assert.NotNull(val0);
            Assert.Equal(val0, store.GetAggregator(span));
            LastValue val1 = store.GetAggregator(span1);
            Assert.Null(val1);
            Assert.Equal(val1, store.GetAggregator(span1));
            
        }
    }
}
