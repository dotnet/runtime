// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class ConstantExpectedAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            var attr = new ConstantExpectedAttribute();

            Assert.Null(attr.Min);
            Assert.Null(attr.Max);
        }

        [Theory]
        [InlineData("https://dot.net")]
        [InlineData("")]
        [InlineData(10000)]
        [InlineData(0.5f)]
        [InlineData(null)]
        public void TestSetMin(object min)
        {
            var attr = new ConstantExpectedAttribute
            {
                Min = min
            };

            Assert.Equal(min, attr.Min);
        }

        [Theory]
        [InlineData("https://dot.net")]
        [InlineData("")]
        [InlineData(10000)]
        [InlineData(0.5f)]
        [InlineData(null)]
        public void TestSetMax(object max)
        {
            var attr = new ConstantExpectedAttribute
            {
                Max = max
            };

            Assert.Equal(max, attr.Max);
        }

        [Theory]
        [InlineData("", "https://dot.net")]
        [InlineData(10000, 20000)]
        [InlineData(0.5f, 2.0f)]
        [InlineData(null, null)]
        public void TestSetMinAndMax(object min, object max)
        {
            var attr = new ConstantExpectedAttribute
            {
                Min = min,
                Max = max
            };

            Assert.Equal(min, attr.Min);
            Assert.Equal(max, attr.Max);
        }
    }
}
