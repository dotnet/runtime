// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public partial class CheckPlatformTests
    {
        [Fact, PlatformSpecific(TestPlatforms.Linux)]  // Tests RuntimeInformation OS platform
        public void CheckLinux()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("LINUX")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("linux")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NetBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("netbsd")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("ubuntu")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("UNIX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact, PlatformSpecific(TestPlatforms.NetBSD)]  // Tests RuntimeInformation OS platform
        public void CheckNetBSD()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NetBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("netbsd")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("LINUX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("linux")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("ubuntu")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("UNIX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]  // Tests RuntimeInformation OS platform
        public void CheckOSX()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("OSX")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NetBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("netbsd")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("osx")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("mac")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACOSX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact, PlatformSpecific(TestPlatforms.Windows)]  // Tests RuntimeInformation OS platform
        public void CheckWindows()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("WINDOWS")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("NetBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("netbsd")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("windows")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("Windows NT")));
        }

        [Fact]
        public void CheckOSPlatform()
        {
            OSPlatform winObj = OSPlatform.Create("WINDOWS");
            OSPlatform winProp = OSPlatform.Windows;
            OSPlatform randomObj = OSPlatform.Create("random");
            OSPlatform defaultObj = default(OSPlatform);
            OSPlatform conObj = new OSPlatform();
            Assert.Throws<ArgumentNullException>(() => { OSPlatform nullObj = OSPlatform.Create(null); });
            AssertExtensions.Throws<ArgumentException>("osPlatform", () => { OSPlatform emptyObj = OSPlatform.Create(""); });

            Assert.True(winObj == winProp);
            Assert.True(winObj != randomObj);
            Assert.True(defaultObj == conObj);

            Assert.False(winObj == defaultObj);
            Assert.False(winObj == randomObj);
            Assert.False(winObj != winProp);

            Assert.True(winObj.Equals(winProp));
            Assert.True(winObj.Equals((object)winProp));
            Assert.True(conObj.Equals(defaultObj));

            Assert.False(defaultObj.Equals(winProp));
            Assert.False(defaultObj.Equals((object)winProp));
            Assert.False(winObj.Equals(null));
            Assert.False(winObj.Equals("something"));

            Assert.Equal("WINDOWS", winObj.ToString());
            Assert.Equal("WINDOWS", winProp.ToString());
            Assert.Equal("", defaultObj.ToString());
            Assert.Equal("", conObj.ToString());
            Assert.Equal("random", randomObj.ToString());

            Assert.Equal(winObj.GetHashCode(), winProp.GetHashCode());
            Assert.Equal(0, defaultObj.GetHashCode());
            Assert.Equal(defaultObj.GetHashCode(), conObj.GetHashCode());
        }
    }
}
