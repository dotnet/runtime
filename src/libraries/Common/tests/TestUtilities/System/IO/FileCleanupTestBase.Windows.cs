// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO
{
    /// <summary>Base class for test classes the use temporary files that need to be cleaned up.</summary>
    public abstract partial class FileCleanupTestBase : IDisposable
    {
        private const int OPEN_EXISTING = 3;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        // Some Windows versions like Windows Nano Server have the %TEMP% environment variable set to "C:\TEMP" but the
        // actual folder name is "C:\Temp", which prevents asserting path values using Assert.Equal due to case sensitiveness.
        // So instead of using TestDirectory directly, we retrieve the real path with proper casing of the initial folder path.
        private unsafe string GetTestDirectoryActualCasing()
        {
            if (!PlatformDetection.IsWindows)
                return TestDirectory;

            try
            {
                using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                            TestDirectory,
                            dwDesiredAccess: 0,
                            dwShareMode: FileShare.ReadWrite | FileShare.Delete,
                            dwCreationDisposition: FileMode.Open,
                            dwFlagsAndAttributes:
                                OPEN_EXISTING |
                                FILE_FLAG_BACKUP_SEMANTICS // Necessary to obtain a handle to a directory
                            );

                if (!handle.IsInvalid)
                {
                    const int InitialBufferSize = 4096;
                    char[]? buffer = ArrayPool<char>.Shared.Rent(InitialBufferSize);
                    uint result = GetFinalPathNameByHandle(handle, buffer);

                    // Remove extended prefix
                    int skip = PathInternal.IsExtended(buffer) ? 4 : 0;

                    return new string(
                        buffer,
                        skip,
                        (int)result - skip);
                }
            }
            catch { }

            return TestDirectory;
        }

        private unsafe uint GetFinalPathNameByHandle(SafeFileHandle handle, char[] buffer)
        {
            fixed (char* bufPtr = buffer)
            {
                return Interop.Kernel32.GetFinalPathNameByHandle(handle, bufPtr, (uint)buffer.Length, Interop.Kernel32.FILE_NAME_NORMALIZED);
            }
        }
    }
}
