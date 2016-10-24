// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ProgId("pizza")]
    public class ProgIdAttributeTests
    {
        [Fact]
        public void Exists()
        {
            var type = typeof(ProgIdAttributeTests);
            var attr = type.GetCustomAttributes(typeof(ProgIdAttribute), false).OfType<ProgIdAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
            Assert.Equal("pizza", attr.Value);
        }
    }
}
