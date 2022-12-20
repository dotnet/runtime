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

            Struct itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does not change values in dictionary
            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref Struct itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 2);

            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new Struct() { Value = 7, Property = 8 };

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
                {  1, new IntAsObject() },
                {  2, new IntAsObject() }
            };

            Assert.Equal(2, dict.Count);

            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            IntAsObject itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does change values in dictionary
            Assert.Equal(1, dict[1].Value);
            Assert.Equal(2, dict[1].Property);

            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Value = 3;
            CollectionsMarshal.GetValueRefOrNullRef(dict, 1).Property = 4;

            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref IntAsObject itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 2);

            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new IntAsObject() { Value = 7, Property = 8 };

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
                {  1, new Struct() }
            };

            Assert.Equal(1, dict.Count);

            ref Struct itemRef = ref CollectionsMarshal.GetValueRefOrNullRef(dict, 1);

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
                dict.Add(i, new Struct());
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

        [Fact]
        public void GetValueRefOrAddDefaultValueType()
        {
            // This test is the same as the one for GetValueRefOrNullRef, but it uses
            // GetValueRefOrAddDefault instead, and also checks for incorrect additions.
            // The two APIs should behave the same when values already exist.
            var dict = new Dictionary<int, Struct>
            {
                {  1, default },
                {  2, default }
            };

            Assert.Equal(2, dict.Count);

            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            Struct itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does not change values in dictionary
            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            CollectionsMarshal.GetValueRefOrAddDefault(dict, 1, out bool exists).Value = 3;

            Assert.True(exists);
            Assert.Equal(2, dict.Count);

            CollectionsMarshal.GetValueRefOrAddDefault(dict, 1, out exists).Property = 4;

            Assert.True(exists);
            Assert.Equal(2, dict.Count);
            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref Struct itemRef = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, 2, out exists);

            Assert.True(exists);
            Assert.Equal(2, dict.Count);
            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new Struct() { Value = 7, Property = 8 };

            Assert.Equal(7, itemRef.Value);
            Assert.Equal(8, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            // Check for correct additions

            ref Struct entry3Ref = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, 3, out exists);

            Assert.False(exists);
            Assert.Equal(3, dict.Count);
            Assert.False(Unsafe.IsNullRef(ref entry3Ref));
            Assert.True(EqualityComparer<Struct>.Default.Equals(entry3Ref, default));

            entry3Ref.Property = 42;
            entry3Ref.Value = 12345;

            Struct value3 = dict[3];

            Assert.Equal(42, value3.Property);
            Assert.Equal(12345, value3.Value);
        }

        [Fact]
        public void GetValueRefOrAddDefaultClass()
        {
            var dict = new Dictionary<int, IntAsObject>
            {
                {  1, new IntAsObject() },
                {  2, new IntAsObject() }
            };

            Assert.Equal(2, dict.Count);

            Assert.Equal(0, dict[1].Value);
            Assert.Equal(0, dict[1].Property);

            IntAsObject itemVal = dict[1];
            itemVal.Value = 1;
            itemVal.Property = 2;

            // Does change values in dictionary
            Assert.Equal(1, dict[1].Value);
            Assert.Equal(2, dict[1].Property);

            CollectionsMarshal.GetValueRefOrAddDefault(dict, 1, out bool exists).Value = 3;

            Assert.True(exists);
            Assert.Equal(2, dict.Count);

            CollectionsMarshal.GetValueRefOrAddDefault(dict, 1, out exists).Property = 4;

            Assert.True(exists);
            Assert.Equal(2, dict.Count);
            Assert.Equal(3, dict[1].Value);
            Assert.Equal(4, dict[1].Property);

            ref IntAsObject itemRef = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, 2, out exists);

            Assert.True(exists);
            Assert.Equal(2, dict.Count);
            Assert.Equal(0, itemRef.Value);
            Assert.Equal(0, itemRef.Property);

            itemRef.Value = 5;
            itemRef.Property = 6;

            Assert.Equal(5, itemRef.Value);
            Assert.Equal(6, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            itemRef = new IntAsObject() { Value = 7, Property = 8 };

            Assert.Equal(7, itemRef.Value);
            Assert.Equal(8, itemRef.Property);
            Assert.Equal(dict[2].Value, itemRef.Value);
            Assert.Equal(dict[2].Property, itemRef.Property);

            // Check for correct additions

            ref IntAsObject entry3Ref = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, 3, out exists);

            Assert.False(exists);
            Assert.Equal(3, dict.Count);
            Assert.False(Unsafe.IsNullRef(ref entry3Ref));
            Assert.Null(entry3Ref);

            entry3Ref = new IntAsObject() { Value = 12345, Property = 42 };

            IntAsObject value3 = dict[3];

            Assert.Equal(42, value3.Property);
            Assert.Equal(12345, value3.Value);
        }

        [Fact]
        public void GetValueRefOrAddDefaultLinkBreaksOnResize()
        {
            var dict = new Dictionary<int, Struct>
            {
                {  1, new Struct() }
            };

            Assert.Equal(1, dict.Count);

            ref Struct itemRef = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, 1, out bool exists);

            Assert.True(exists);
            Assert.Equal(1, dict.Count);
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
                dict.Add(i, new Struct());
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
