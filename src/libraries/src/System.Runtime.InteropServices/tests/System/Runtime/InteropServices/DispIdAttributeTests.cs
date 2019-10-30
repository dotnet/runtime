// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices
{
    public class DispIdAttributeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public static void Ctor_Value(int dispId)
        {
            var attribute = new DispIdAttribute(dispId);
            Assert.Equal(dispId, attribute.Value);
        }
    }
}
