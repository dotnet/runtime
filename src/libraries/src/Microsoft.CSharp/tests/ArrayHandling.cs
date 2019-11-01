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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void SingleRankNonSZArray()
        {
            dynamic d = Array.CreateInstance(typeof(int), new[] { 8 }, new[] { -2 });
            d.SetValue(32, 3);
            d.SetValue(28, -1);
            Assert.Equal(32, d.GetValue(3));
            Assert.Equal(28, d.GetValue(-1));
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void ArrayTypeNames()
        {
            dynamic d;
            RuntimeBinderException ex;

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                d = Array.CreateInstance(typeof(int), new[] { 8 }, new[] { -2 });
                ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
                Assert.Contains("int[*]", ex.Message);
            }

            d = new int[3];
            ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[]", ex.Message);
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void MultiDimArrayTypeNames()
        {
            dynamic d = new int[3, 2, 1];
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
            Assert.Contains("int[,,]", ex.Message);

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                d = Array.CreateInstance(typeof(int), new[] { 3, 2, 1 }, new[] { -2, 2, -0 });
                ex = Assert.Throws<RuntimeBinderException>(() => { string s = d; });
                Assert.Contains("int[,,]", ex.Message);
            }
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void IncorrectNumberOfIndices()
        {
            dynamic d = new int[2, 2, 2];
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => d[1] = 0);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'3'", ex.Message);

            ex = Assert.Throws<RuntimeBinderException>(() => d[1, 2, 3, 4] = 0);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'3'", ex.Message);

            ex = Assert.Throws<RuntimeBinderException>(() => d[1]);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'3'", ex.Message);

            ex = Assert.Throws<RuntimeBinderException>(() => d[1, 2, 3, 4]);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'3'", ex.Message);

            d = new int[2];
            ex = Assert.Throws<RuntimeBinderException>(() => d[1, 2, 3, 4] = 0);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'1'", ex.Message);

            ex = Assert.Throws<RuntimeBinderException>(() => d[1, 2, 3, 4]);
            Assert.Contains("[]", ex.Message);
            Assert.Contains("'1'", ex.Message);
        }

        [Fact]
        public void SZArrayLength()
        {
            dynamic d = new int[23];
            Assert.Equal(23, d.Length);

            d = new[]
            {
                "A", "\"Hello,", "World!\"", "program", "is", "a", "computer", "program", "that", "outputs", "or",
                "displays", "\"Hello,", "World!\"", "to", "the", "user."
            };
            Assert.Equal(17, d.Length);
        }

        [Fact]
        public void MDArrayLength()
        {
            dynamic d = new int[2, 3, 4];
            Assert.Equal(2 * 3 * 4, d.Length);

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                d = Array.CreateInstance(typeof(string), new[] {5, 6, 8}, new[] {-2, 4, 3});
                Assert.Equal(5 * 6 * 8, d.Length);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void NonSZ1RArrayLenght()
        {
            dynamic d = Array.CreateInstance(typeof(int), new[] {23}, new[] {-2});
            Assert.Equal(23, d.Length);
        }
    }
}
