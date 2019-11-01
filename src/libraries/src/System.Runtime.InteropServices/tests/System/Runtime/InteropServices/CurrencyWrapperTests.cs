// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // CurrencyWrapper is marked as Obsolete.
    public class CurrencyWrapperTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_DecimalValue(double value)
        {
            var wrapper = new CurrencyWrapper((decimal)value);
            Assert.Equal((decimal)value, wrapper.WrappedObject);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_ObjectValue(double value)
        {
            var wrapper = new CurrencyWrapper((object)(decimal)value);
            Assert.Equal((decimal)value, wrapper.WrappedObject);
        }

        [Fact]
        public void Ctor_NonDecimalObjectValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("obj", () => new CurrencyWrapper("1"));
        }
    }
#pragma warning restore 0618
}
