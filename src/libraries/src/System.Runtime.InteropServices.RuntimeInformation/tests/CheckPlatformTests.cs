// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.InteropServices.RuntimeInformationTests
{
    public class CheckPlatformTests
    {
        [Fact, PlatformSpecific(PlatformID.Linux)]
        public void CheckLinux()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("LINUX")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("linux")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("UNIX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("ubuntu")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        }

        [Fact, PlatformSpecific(PlatformID.OSX)]
        public void CheckOSX()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("OSX")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("osx")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("mac")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("DARWIN")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACOSX")));
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact, PlatformSpecific(PlatformID.Windows)]
        public void CheckWindows()
        {
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Create("WINDOWS")));

            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")));
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
            Assert.Throws<ArgumentException>(() => { OSPlatform emptyObj = OSPlatform.Create(""); });

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
