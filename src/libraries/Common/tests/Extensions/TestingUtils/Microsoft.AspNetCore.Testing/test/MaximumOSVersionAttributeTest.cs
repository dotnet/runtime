// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class MaximumOSVersionAttributeTest
    {
        [Fact]
        public void Linux_ThrowsNotImplemeneted()
        {
            Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.Linux, "2.5"));
        }

        [Fact]
        public void Mac_ThrowsNotImplemeneted()
        {
            Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.MacOSX, "2.5"));
        }

        [Fact]
        public void WindowsOrLinux_ThrowsNotImplemeneted()
        {
            Assert.Throws<NotImplementedException>(() => new MaximumOSVersionAttribute(OperatingSystems.Linux | OperatingSystems.Windows, "2.5"));
        }

        [Fact]
        public void DoesNotSkip_EarlierVersions()
        {
            var osSkipAttribute = new MaximumOSVersionAttribute(
                OperatingSystems.Windows,
                new Version("2.5"),
                OperatingSystems.Windows,
                new Version("2.0"));

            Assert.True(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_SameVersion()
        {
            var osSkipAttribute = new MaximumOSVersionAttribute(
                OperatingSystems.Windows,
                new Version("2.5"),
                OperatingSystems.Windows,
                new Version("2.5"));

            Assert.True(osSkipAttribute.IsMet);
        }

        [Fact]
        public void Skip_LaterVersion()
        {
            var osSkipAttribute = new MaximumOSVersionAttribute(
                OperatingSystems.Windows,
                new Version("2.5"),
                OperatingSystems.Windows,
                new Version("3.0"));

            Assert.False(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_WhenOnlyVersionsMatch()
        {
            var osSkipAttribute = new MaximumOSVersionAttribute(
                OperatingSystems.Windows,
                new Version("2.5"),
                OperatingSystems.Linux,
                new Version("2.5"));

            Assert.True(osSkipAttribute.IsMet);
        }
    }
}
