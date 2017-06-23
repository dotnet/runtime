// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class BestFitMappingAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_BestFitMapping(bool bestFitMapping)
        {
            var attribute = new BestFitMappingAttribute(bestFitMapping);
            Assert.Equal(bestFitMapping, attribute.BestFitMapping);
        }
    }
}
