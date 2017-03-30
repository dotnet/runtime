// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ComConversionLoss]
    public class ComConversionLossAttributeTests
    {
        [Fact]
        public void Exists()
        {
            var type = typeof(ComConversionLossAttributeTests);
            var attr = type.GetCustomAttributes(typeof(ComConversionLossAttribute), false).OfType<ComConversionLossAttribute>().SingleOrDefault();
            Assert.NotNull(attr);
        }
    }
}
