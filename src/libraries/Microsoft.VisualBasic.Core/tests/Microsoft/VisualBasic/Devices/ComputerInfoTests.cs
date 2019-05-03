// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Devices.Tests
{
    public class ComputerInfoTests
    {
        [Fact]
        public void Properties()
        {
            var info = new ComputerInfo();
            Assert.Equal(System.Globalization.CultureInfo.InstalledUICulture, info.InstalledUICulture);
            Assert.Equal(System.Environment.OSVersion.Platform.ToString(), info.OSPlatform);
            Assert.Equal(System.Environment.OSVersion.Version.ToString(), info.OSVersion);
        }

        [Fact]
        public void Memory()
        {
            var info = new ComputerInfo();
            if (PlatformDetection.IsWindows)
            {
                Assert.NotEqual(0u, info.AvailablePhysicalMemory);
                Assert.NotEqual(0u, info.AvailableVirtualMemory);
                Assert.NotEqual(0u, info.TotalPhysicalMemory);
                Assert.NotEqual(0u, info.TotalVirtualMemory);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => info.AvailablePhysicalMemory);
                Assert.Throws<PlatformNotSupportedException>(() => info.AvailableVirtualMemory);
                Assert.Throws<PlatformNotSupportedException>(() => info.TotalPhysicalMemory);
                Assert.Throws<PlatformNotSupportedException>(() => info.TotalVirtualMemory);
            }
        }
    }
}
