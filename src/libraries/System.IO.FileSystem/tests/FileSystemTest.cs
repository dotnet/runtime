// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileSystemTest : FileCleanupTestBase
    {
        public static readonly byte[] TestBuffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };

        protected const PlatformID CaseInsensitivePlatforms = PlatformID.Windows | PlatformID.OSX;
        protected const PlatformID CaseSensitivePlatforms = PlatformID.AnyUnix & ~PlatformID.OSX;

        // In some cases (such as when running without elevated privileges),
        // the symbolic link may fail to create. Only run this test if it creates
        // links successfully.
        protected static bool CanCreateSymbolicLinks
        {
            get
            {
                var path = Path.GetTempFileName();
                var linkPath = path + ".link";
                var ret = MountHelper.CreateSymbolicLink(linkPath, path, isDirectory: false);
                try { File.Delete(path); } catch { }
                try { File.Delete(linkPath); } catch { }
                return ret;
            }
        }

        [DllImport("libc", SetLastError = true)]
        protected static extern int geteuid();

        [DllImport("libc", SetLastError = true)]
        protected static extern int mkfifo(string path, int mode);
    }
}
