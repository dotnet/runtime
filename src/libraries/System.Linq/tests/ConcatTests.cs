// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;
            var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                     select x2;
                     
            Assert.Equal(q1.Concat(q2), q1.Concat(q2));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;
            var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                     select x2;

            Assert.Equal(q1.Concat(q2), q1.Concat(q2));
        }

        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            int[] expected = { };

            Assert.Equal(expected, first.Concat(second));
        }

        [Fact]
        public void EmptyAndNonEmpty()
        {
            int[] first = { };
            int[] second = { 2, 6, 4, 6, 2 };
            int[] expected = { 2, 6, 4, 6, 2 };

            Assert.Equal(expected, first.Concat(second));
        }

        [Fact]
        public void NonEmptyAndEmpty()
        {
            int[] first = { 2, 6, 4, 6, 2 };
            int[] second = { };
            int[] expected = { 2, 6, 4, 6, 2 };

            Assert.Equal(expected, first.Concat(second));
        }

        [Fact]
        public void NonEmptyAndNonEmpty()
        {
            int?[] first = { 2, null, 3, 5, 9 };
            int?[] second = { null, 8, 10 };
            int?[] expected = { 2, null, 3, 5, 9, null, 8, 10 };

            Assert.Equal(expected, first.Concat(second));
        }
    }
}
