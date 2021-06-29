// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public abstract partial class BaseSymbolicLinks : FileSystemTest
    {
        private const int OPEN_EXISTING = 3;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        // Some Windows versions like Windows Nano Server have the %TEMP% environment variable set to "C:\TEMP" but the
        // actual folder name is "C:\Temp", which prevents asserting path values using Assert.Equal due to case sensitiveness.
        // So instead of using TestDirectory directly, we retrieve the real path with proper casing of the initial folder path.
        private unsafe string GetTestDirectoryActualCasing()
        {
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
