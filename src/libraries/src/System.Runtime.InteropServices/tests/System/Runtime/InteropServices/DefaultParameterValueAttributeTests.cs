// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public static void Constructor(object expected)
        {
            DefaultParameterValueAttribute attribute = new DefaultParameterValueAttribute(expected);
            Assert.Equal(expected, attribute.Value);
        }
    }
}
