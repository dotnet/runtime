// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public partial class CheckPlatformTests
    {
        [Fact, PlatformSpecific(TestPlatforms.FreeBSD)]
        public void CheckFreeBSD()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("LINUX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("linux")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NetBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("ubuntu")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("UNIX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact, PlatformSpecific(~TestPlatforms.FreeBSD)]
        public void CheckFreeBSDOnOtherPlatforms()
        {
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
        }
    }
}
