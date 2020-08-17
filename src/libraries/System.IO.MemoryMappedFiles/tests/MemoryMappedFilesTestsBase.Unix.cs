// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>Base class from which all of the memory mapped files test classes derive.</summary>
    public abstract partial class MemoryMappedFilesTestBase : FileCleanupTestBase
    {
        /// <summary>Gets the system's page size.</summary>
        protected static Lazy<int> s_pageSize = new Lazy<int>(() =>
        {
            if (OperatingSystem.IsBrowser())
                return Environment.SystemPageSize;

            int pageSize;
            const int _SC_PAGESIZE_FreeBSD = 47;
            const int _SC_PAGESIZE_Linux = 30;
            const int _SC_PAGESIZE_NetBSD = 28;
            const int _SC_PAGESIZE_OSX = 29;
            pageSize = sysconf(
                OperatingSystem.IsMacOS() ? _SC_PAGESIZE_OSX :
                OperatingSystem.IsFreeBSD() ? _SC_PAGESIZE_FreeBSD :
                RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")) ? _SC_PAGESIZE_NetBSD :
                _SC_PAGESIZE_Linux);
            Assert.InRange(pageSize, 1, int.MaxValue);
            return pageSize;
        });

        [DllImport("libc", SetLastError = true)]
        private static extern int sysconf(int name);

        /// <summary>Asserts that the handle's inheritability matches the specified value.</summary>
        protected static void AssertInheritability(SafeHandle handle, HandleInheritability inheritability)
        {
            //intentional noop
        }
    }
}
