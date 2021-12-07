// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW", ExactSpelling = true)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            uint dwFlags,
            ref char lpBuffer,
            ref uint lpdwSize);

        private const int MAX_PROCESSNAME_LENGTH = 1024;

        internal static string? GetProcessName(uint processId)
        {
            using (SafeProcessHandle h = Interop.Kernel32.OpenProcess(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false, (int)processId))
            {
                Span<char> buffer = stackalloc char[MAX_PROCESSNAME_LENGTH + 1];
                uint length = (uint)buffer.Length;

                bool queried = QueryFullProcessImageName(
                    h.DangerousGetHandle(),
                    0,
                    ref MemoryMarshal.GetReference(buffer),
                    ref length);

                return queried ? buffer.Slice(0, (int)length).ToString() : null;
            }
        }
    }
}
