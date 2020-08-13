// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            int pageSize;
            SYSTEM_INFO info;
            GetSystemInfo(out info);
            pageSize = (int)info.dwPageSize;
            Assert.InRange(pageSize, 1, int.MaxValue);
            return pageSize;
        });

        [DllImport("kernel32.dll")]
        private static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);

        private const uint HANDLE_FLAG_INHERIT = 0x00000001;

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO input);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            internal uint dwOemId;
            internal uint dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal uint dwNumberOfProcessors;
            internal uint dwProcessorType;
            internal uint dwAllocationGranularity;
            internal short wProcessorLevel;
            internal short wProcessorRevision;
        }

        protected static int geteuid()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Asserts that the handle's inheritability matches the specified value.</summary>
        protected static void AssertInheritability(SafeHandle handle, HandleInheritability inheritability)
        {
            if (OperatingSystem.IsWindows())
            {
                uint flags;
                Assert.True(GetHandleInformation(handle.DangerousGetHandle(), out flags));
                Assert.Equal(inheritability == HandleInheritability.Inheritable, (flags & HANDLE_FLAG_INHERIT) != 0);
            }
        }
    }
}
