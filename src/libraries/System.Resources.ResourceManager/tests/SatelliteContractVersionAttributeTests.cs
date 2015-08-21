// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Resources.Tests
{
    public static class SatelliteContractVersionAttributeTests
    {
        [Theory]
        [InlineData("1.0.0.0")]
        [InlineData("999.999.999")]
        [InlineData("-1")]
        [InlineData("")]
        public static void ConstructorBasic(string version)
        {
            SatelliteContractVersionAttribute scva = new SatelliteContractVersionAttribute(version);
            Assert.Equal(version, scva.Version);
        }

        public static void ConstructorArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SatelliteContractVersionAttribute(null));
        }
    }
}
