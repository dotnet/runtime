// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    /// <summary>
    /// Tests for SafeMemoryMappedViewHandle
    /// </summary>
    public class SafeMemoryMappedViewHandleTests : MemoryMappedFilesTestBase
    {
        /// <summary>
        /// Tests that external code can use SafeMemoryMappedViewHandle as the result of a P/Invoke on Windows.
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SafeMemoryMappedViewHandle_CanUseInPInvoke_Windows()
        {
            const int BUF_SIZE = 256;

            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default;
            using SafeMemoryMappedFileHandle fileHandle = Interop.Kernel32.CreateFileMapping(
                new IntPtr(-1),
                ref secAttrs,
                Interop.Kernel32.PageOptions.PAGE_EXECUTE_READWRITE,
                0,
                BUF_SIZE,
                CreateUniqueMapName());

            using SafeMemoryMappedViewHandle handle = Interop.Kernel32.MapViewOfFile(
                fileHandle,
                Interop.Kernel32.FileMapOptions.FILE_MAP_READ,
                0,
                0,
                (UIntPtr)BUF_SIZE);

            Assert.NotNull(handle);
        }

        /// <summary>
        /// Tests that external code can use SafeMemoryMappedViewHandle as the result of a P/Invoke on Unix.
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        public void SafeMemoryMappedViewHandle_CanUseInPInvoke_Unix()
        {
            const int MAP_PRIVATE = 0x02;
            const int MAP_ANONYMOUS = 0x10;

            const int PROT_READ = 0x1;
            const int PROT_WRITE = 0x2;

            // The handle returned may be invalid, but this is testing that the
            // SafeHandle object can successfully be created in a P/Invoke
            using SafeMemoryMappedViewHandle handle = mmap(
                IntPtr.Zero,
                1,
                PROT_READ | PROT_WRITE,
                MAP_PRIVATE | MAP_ANONYMOUS,
                -1,
                0);

            Assert.NotNull(handle);
        }

        [DllImport("libc")]
        private static unsafe extern SafeMemoryMappedViewHandle mmap(IntPtr addr, nint lengthint, int prot, int flags, int fd, nuint offset);
    }
}
