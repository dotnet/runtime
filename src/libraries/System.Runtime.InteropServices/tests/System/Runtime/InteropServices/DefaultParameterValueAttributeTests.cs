// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices
{
    public class DefaultParameterValueAttributeTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData((byte)255)]
        [InlineData(5.0)]
        [InlineData(5.0f)]
        [InlineData("ExpectedValue")]
        [InlineData(null)]
        public static void Ctor_Value(object value)
        {
            var attribute = new DefaultParameterValueAttribute(value);
            Assert.Equal(value, attribute.Value);
        }
    }
}
