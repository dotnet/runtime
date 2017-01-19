// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class AsEnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.AsEnumerable(), q.AsEnumerable());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.AsEnumerable(), q.AsEnumerable());
        }

        [Fact]
        public void NullSourceAllowed()
        {
            int[] source = null;

            Assert.Null(source.AsEnumerable());
        }

        [Fact]
        public void OneElement()
        {
            int[] source = { 2 };

            Assert.Equal(source, source.AsEnumerable());
        }

        [Fact]
        public void SomeElements()
        {
            int?[] source = { -5, 0, 1, -4, 3, null, 10 };

            Assert.Equal(source, source.AsEnumerable());
        }

        [Fact]
        public void SomeElementsRunOnce()
        {
            int?[] source = { -5, 0, 1, -4, 3, null, 10 };

            Assert.Equal(source, source.RunOnce().AsEnumerable());
        }
    }
}
