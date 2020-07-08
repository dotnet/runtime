// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class UnknownWrapperTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData("Value")]
        public void Ctor_Value(object value)
        {
            var wrapper = new UnknownWrapper(value);
            Assert.Equal(value, wrapper.WrappedObject);
        }
    }
}
