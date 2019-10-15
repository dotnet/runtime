// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GuidAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Guid")]
        public void Ctor_Guid(string guid)
        {
            var attribute = new GuidAttribute(guid);
            Assert.Equal(guid, attribute.Value);
        }
    }
}
