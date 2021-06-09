// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

        [Fact]
        public void GetValueRefOrNullRefValueType()
        {
            var dict = new Dictionary<int, Struct>
            {
                {  1, default },
                {  2, default }
            };

            Assert.Equal(2, dict.Count);

            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            var itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does not change values in dictionary
            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref var itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 2);

            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new() { Value = 7, Property = 8 };

            Assert.Equal(7, itemRef.Value);
            Assert.Equal(8, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            // Check for null refs

            Assert.True(Unsafe.IsNullRef(ref CollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
            Assert.Throws<NullReferenceException>(() => CollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

            Assert.Equal(2, dict.Count);
        }

        [Fact]
        public void GetValueRefOrNullRefClass()
        {
            var dict = new Dictionary<int, IntAsObject>
            {
                {  1, new() },
                {  2, new() }
            };

            Assert.Equal(2, dict.Count);

            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            var itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does change values in dictionary
            Assert.Equal(1, dict[1].Value);
            Assert.Equal(2, dict[1].Property);

            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref var itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 2);

            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new() { Value = 7, Property = 8 };

            Assert.Equal(7, itemRef.Value);
            Assert.Equal(8, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            // Check for null refs

            Assert.True(Unsafe.IsNullRef(ref CollectionsMarshal.GetValueRefOrNullRef(dict, 3)));
            Assert.Throws<NullReferenceException>(() => CollectionsMarshal.GetValueRefOrNullRef(dict, 3).Value = 9);

            Assert.Equal(2, dict.Count);
        }

        [Fact]
        public void GetValueRefOrNullRefLinkBreaksOnResize()
        {
            var dict = new Dictionary<int, Struct>
            {
                {  1, new() }
            };

            Assert.Equal(1, dict.Count);

            ref var itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 1);

            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 1;
            itemRef.Property = 2;

            Assert.Equal(1, itemRef.Value);
            Assert.Equal(2, itemRef.Property);
            Assert.Equal(dict[1].Value, itemRef.Value);
            Assert.Equal(dict[1].Property, itemRef.Property);

            // Resize
            dict.EnsureCapacity(100);
            for (int i = 2; i <= 50; i++)
            {
                dict.Add(i, new());
            }

            itemRef.Value = 3;
            itemRef.Property = 4;

            Assert.Equal(3, itemRef.Value);
            Assert.Equal(4, itemRef.Property);

            // Check connection broken
            Assert.NotEqual(dict[1].Value, itemRef.Value);
            Assert.NotEqual(dict[1].Property, itemRef.Property);

            Assert.Equal(50, dict.Count);
        }

        private struct Struct
        {
            public int Value;
            public int Property { get; set; }
        }

        private class IntAsObject
        {
            public int Value;
            public int Property { get; set; }
        }
    }
}
