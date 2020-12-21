// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CollectionsMarshalTests
    {
        [Fact]
        public unsafe void NullListAsSpanValueType()
        {
            List<int> list = null;
            Span<int> span = CollectionsMarshal.AsSpan(list);

            Assert.Equal(0, span.Length);

            fixed (int* pSpan = span)
            {
                Assert.True(pSpan == null);
            }
        }

        [Fact]
        public unsafe void NullListAsSpanClass()
        {
            List<object> list = null;
            Span<object> span = CollectionsMarshal.AsSpan(list);

            Assert.Equal(0, span.Length);
        }

        [Fact]
        public void ListAsSpanValueType()
        {
            var list = new List<int>();
            foreach (int length in Enumerable.Range(0, 36))
            {
                list.Clear();
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                for (int i = 0; i < length; i++)
                {
                    list.Add(i);
                }
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                list.TrimExcess();
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                list.Add(length + 1);
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));
            }

            static void ValidateContentEquality(List<int> list, Span<int> span)
            {
                Assert.Equal(list.Count, span.Length);

                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(list[i], span[i]);
                }
            }
        }

        [Fact]
        public void ListAsSpanClass()
        {
            var list = new List<IntAsObject>();
            foreach (int length in Enumerable.Range(0, 36))
            {
                list.Clear();
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                for (var i = 0; i < length; i++)
                {
                    list.Add(new IntAsObject { Value = i });
                }
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                list.TrimExcess();
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));

                list.Add(new IntAsObject { Value = length + 1 });
                ValidateContentEquality(list, CollectionsMarshal.AsSpan(list));
            }

            static void ValidateContentEquality(List<IntAsObject> list, Span<IntAsObject> span)
            {
                Assert.Equal(list.Count, span.Length);

                for (int i = 0; i < span.Length; i++)
                {
                    Assert.Equal(list[i].Value, span[i].Value);
                }
            }
        }

        [Fact]
        public void ListAsSpanLinkBreaksOnResize()
        {
            var list = new List<int>(capacity: 10);

            for (int i = 0; i < 10; i++)
            {
                list.Add(i);
            }
            list.TrimExcess();
            Span<int> span = CollectionsMarshal.AsSpan(list);

            int startCapacity = list.Capacity;
            int startCount = list.Count;
            Assert.Equal(startCount, startCapacity);
            Assert.Equal(startCount, span.Length);

            for (int i = 0; i < span.Length; i++)
            {
                span[i]++;
                Assert.Equal(list[i], span[i]);

                list[i]++;
                Assert.Equal(list[i], span[i]);
            }

            // Resize to break link between Span and List
            list.Add(11);

            Assert.NotEqual(startCapacity, list.Capacity);
            Assert.NotEqual(startCount, list.Count);
            Assert.Equal(startCount, span.Length);

            for (int i = 0; i < span.Length; i++)
            {
                span[i] += 2;
                Assert.NotEqual(list[i], span[i]);

                list[i] += 3;
                Assert.NotEqual(list[i], span[i]);
            }
        }

        private class IntAsObject
        {
            public int Value;
        }
    }
}
