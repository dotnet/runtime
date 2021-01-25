// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class BStrWrapperTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Ctor_StringValue(string value)
        {
            var wrapper = new BStrWrapper(value);
            Assert.Equal(value, wrapper.WrappedObject);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Ctor_ObjectValue(object value)
        {
            var wrapper = new BStrWrapper(value);
            Assert.Equal(value, wrapper.WrappedObject);
        }

        [Fact]
        public void Ctor_NonStringObjectValue_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new BStrWrapper(1));
        }
    }
}
