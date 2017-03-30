// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        public static Version OSXKernelVersion { get; } = GetOSXKernelVersion();

        private static Version GetOSXKernelVersion()
        {
            if (IsOSX)
            {
                byte[] bytes = new byte[256];
                IntPtr bytesLength = new IntPtr(bytes.Length);
                Assert.Equal(0, sysctlbyname("kern.osrelease", bytes, ref bytesLength, null, IntPtr.Zero));
                string versionString = Encoding.UTF8.GetString(bytes);
                return Version.Parse(versionString);
            }

            return new Version(0, 0, 0);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int sysctlbyname(string ctlName, byte[] oldp, ref IntPtr oldpLen, byte[] newp, IntPtr newpLen);
    }
}
