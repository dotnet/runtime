// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // BStrWrapper is marked as Obsolete.
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
#pragma warning restore 0618
}
