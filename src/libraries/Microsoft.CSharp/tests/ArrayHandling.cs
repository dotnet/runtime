// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class ArrayHandling
    {
        [Fact]
        public void SingleRankNonSZArray()
        {
            dynamic d = Array.CreateInstance(typeof(int), new[] { 8 }, new[] { -2 });
            d.SetValue(32, 3);
            d.SetValue(28, -1);
            Assert.Equal(32, d.GetValue(3));
            Assert.Equal(28, d.GetValue(-1));
        }

        [Fact]
        public void SingleRankNonSZArrayIndexed()
        {
            dynamic d = Array.CreateInstance(typeof(int), new[] { 8 }, new[] { -2 });
            d[3] = 32;
            d[-1] = 28;
            Assert.Equal(32, d[3]);
            Assert.Equal(28, d[-1]);
        }

        [Fact]
        public void ArrayTypeNames()
        {
            dynamic d = Array.CreateInstance(typeof(int), new[] { 8 }, new[] { -2 });
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[*]", ex.Message);

            d = new int[3];
            ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[]", ex.Message);

            d = new int[3, 2, 1];
            ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[,,]", ex.Message);

            d = Array.CreateInstance(typeof(int), new[] { 3, 2, 1 }, new[] { -2, 2, -0 });
            ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[,,]", ex.Message);

        }
    }
}
