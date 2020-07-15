// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.Runtime.Versioning.Tests
{
    public class OSPlatformAttributeTests
    {
        [Theory]
        [InlineData("Windows10.0")]
        [InlineData("iOS")]
        [InlineData("")]
        public void TestTargetPlatformAttribute(string platformName)
        {
            var tpa = new TargetPlatformAttribute(platformName);

            Assert.Equal(platformName, tpa.PlatformName);
        }

        [Theory]
        [InlineData("Windows8.0", "Obsolete", "http://test.com/obsoletedInOSPlatform")]
        [InlineData("Linux", "Message", null)]
        [InlineData("iOS13", null, null)]
        [InlineData("", null, "http://test.com/obsoletedInOSPlatform")]
        public void TestObsoletedInOSPlatformAttribute(string platformName, string message, string url)
        {
            var opa = message == null ? new ObsoletedInOSPlatformAttribute(platformName) { Url = url} : new ObsoletedInOSPlatformAttribute(platformName, message) { Url = url };

            Assert.Equal(platformName, opa.PlatformName);
            Assert.Equal(message, opa.Message);
            Assert.Equal(url, opa.Url);
        }

        [Theory]
        [InlineData("Windows8.0")]
        [InlineData("Android4.1")]
        [InlineData("")]
        public void TestRemovedInOSPlatformAttribute(string platformName)
        {
            var tpa = new RemovedInOSPlatformAttribute(platformName);

            Assert.Equal(platformName, tpa.PlatformName);
        }

        [Theory]
        [InlineData("Windows10.0")]
        [InlineData("OSX")]
        [InlineData("")]
        public void TestMinimumOSPlatformAttribute(string platformName)
        {
            var tpa = new MinimumOSPlatformAttribute(platformName);

            Assert.Equal(platformName, tpa.PlatformName);
        }
    }
}
