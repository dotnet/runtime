// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AppendPrependTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQueryAppend()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
                     
            Assert.Equal(q1.Append(42), q1.Append(42));
            Assert.Equal(q1.Append(42), q1.Concat(new int?[] { 42 }));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryPrepend()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;

            Assert.Equal(q1.Prepend(42), q1.Prepend(42));
            Assert.Equal(q1.Prepend(42), (new int?[] { 42 }).Concat(q1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryAppend()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;

            Assert.Equal(q1.Append("hi"), q1.Append("hi"));
            Assert.Equal(q1.Append("hi"), q1.Concat(new string[] { "hi" }));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryPrepend()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;

            Assert.Equal(q1.Prepend("hi"), q1.Prepend("hi"));
            Assert.Equal(q1.Prepend("hi"), (new string[] { "hi" }).Concat(q1));
        }

        [Fact]
        public void EmptyAppend()
        {
            int[] first = { };
            Assert.Single(first.Append(42), 42);
        }

        [Fact]
        public void EmptyPrepend()
        {
            string[] first = { };
            Assert.Single(first.Prepend("aa"), "aa");
        }

        [Fact]
        public void PrependNoIteratingSourceBeforeFirstItem()
        {
            var ie = new List<int>();
            var prepended = (from i in ie select i).Prepend(4);

            ie.Add(42);

            Assert.Equal(prepended, ie.Prepend(4));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumeratePrepend()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Prepend(4);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateAppend()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Append(4);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void SourceNull()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Append(1));
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Prepend(1));
        }

        [Fact]
        public void Combined()
        {
            var v = "foo".Append('1').Append('2').Prepend('3').Concat("qq".Append('Q').Prepend('W'));

            Assert.Equal(v.ToArray(), "3foo12WqqQ".ToArray());

            var v1 = "a".Append('b').Append('c').Append('d');

            Assert.Equal(v1.ToArray(), "abcd".ToArray());

            var v2 = "a".Prepend('b').Prepend('c').Prepend('d');

            Assert.Equal(v2.ToArray(), "dcba".ToArray());
        }
    }
}
