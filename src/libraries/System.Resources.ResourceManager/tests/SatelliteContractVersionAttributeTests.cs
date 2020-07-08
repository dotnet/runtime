// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Fact]
        public static void ConstructorArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => new SatelliteContractVersionAttribute(null));
        }
    }
}
