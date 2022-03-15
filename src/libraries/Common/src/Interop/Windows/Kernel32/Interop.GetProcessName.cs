// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool QueryFullProcessImageName(
            SafeHandle hProcess,
            uint dwFlags,
            char* lpBuffer,
            ref uint lpdwSize);

        internal static unsafe string? GetProcessName(uint processId)
        {
            using (SafeProcessHandle handle = OpenProcess(Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false, (int)processId))
            {
                if (handle.IsInvalid) // OpenProcess can fail
                {
                    return null;
                }

                const int StartLength =
#if DEBUG
                    1; // in debug, validate ArrayPool growth
#else
                    Interop.Kernel32.MAX_PATH;
#endif

                Span<char> buffer = stackalloc char[StartLength + 1];
                char[]? rentedArray = null;

                try
                {
                    while (true)
                    {
                        uint length = (uint)buffer.Length;
                        fixed (char* pinnedBuffer = &MemoryMarshal.GetReference(buffer))
                        {
                            if (QueryFullProcessImageName(handle, 0, pinnedBuffer, ref length))
                            {
                                return buffer.Slice(0, (int)length).ToString();
                            }
                            else if (Marshal.GetLastWin32Error() != Errors.ERROR_INSUFFICIENT_BUFFER)
                            {
                                return null;
                            }
                        }

                        char[]? toReturn = rentedArray;
                        buffer = rentedArray = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
                        if (toReturn is not null)
                        {
                            ArrayPool<char>.Shared.Return(toReturn);
                        }
                    }
                }
                finally
                {
                    if (rentedArray is not null)
                    {
                        ArrayPool<char>.Shared.Return(rentedArray);
                    }
                }
            }
        }
    }
}
