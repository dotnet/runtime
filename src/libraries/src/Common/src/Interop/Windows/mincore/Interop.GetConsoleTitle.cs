// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Console_L1, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetConsoleTitleW")]
        private static extern int GetConsoleTitle([Out]StringBuilder title, int nSize);

        private const int InitialBufferSizeInChars = 256;

        // Although msdn states that the max allowed limit is 65K,
        // desktop limits this to 24500 as buffer sizes greater than it
        // throw.
        private const int MaxAllowedBufferSizeInChars = 24500;

        // 1. We first try to call the GetConsoleTitle with an InitialBufferSizeInChars of 256.
        // 2. Then based on the return value either increase the capacity or return failure.
        internal static int GetConsoleTitle(out string title, out int titleLength)
        {
            int lastError = 0;
            StringBuilder sb = new StringBuilder(InitialBufferSizeInChars);
            int len = GetConsoleTitle(sb, InitialBufferSizeInChars);

            if (len <= 0)
            {
                lastError = Marshal.GetLastWin32Error();
            }
            else if (len > MaxAllowedBufferSizeInChars)
            {
                // Title is greater than the allowed buffer so we do not read the title and only pass the length to the caller.
                title = string.Empty;
                titleLength = len;
                return 0;
            }
            else
            {
                if (len > InitialBufferSizeInChars)
                {
                    // We need to increase the sb capacity and retry.
                    sb.Capacity = len + 1;
                    len = GetConsoleTitle(sb, sb.Capacity);
                    if (len <= 0)
                    {
                        lastError = Marshal.GetLastWin32Error();
                    }
                }
            }

            // If the call succeeds the size must be less than or equal to sb capacity as retrieved from the first call.
            Debug.Assert(lastError != 0 || len + 1 <= sb.Capacity);
            title = len > 0 ? sb.ToString() : string.Empty;
            titleLength = title.Length;
            return lastError;
        }
    }
}