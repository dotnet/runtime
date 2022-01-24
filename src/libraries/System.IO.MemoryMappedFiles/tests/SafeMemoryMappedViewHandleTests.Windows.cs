// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
