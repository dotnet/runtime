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
        public void SameResultsRepeatCallsIntQueryPrePend()
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
        public void SourceNull()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Append(1));
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Prepend(1));
        }
    }
}
