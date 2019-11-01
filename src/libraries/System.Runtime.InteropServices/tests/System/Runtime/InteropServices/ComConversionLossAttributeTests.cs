// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ComConversionLoss]
    public class ComConversionLossAttributeTests
    {
        [Fact]
        public void Exists()
        {
            Type type = typeof(ComConversionLossAttributeTests);
            ComConversionLossAttribute attribute = Assert.IsType<ComConversionLossAttribute>(Assert.Single(type.GetCustomAttributes(typeof(ComConversionLossAttribute), inherit: false)));
            Assert.NotNull(attribute);
        }
    }
}
