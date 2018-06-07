// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class RegistryValues
        {
            internal const int REG_NONE = 0;                // No value type
            internal const int REG_SZ = 1;                  // Unicode nul terminated string
            internal const int REG_EXPAND_SZ = 2;           // Unicode nul terminated string
            // (with environment variable references)
            internal const int REG_BINARY = 3;              // Free form binary
            internal const int REG_DWORD = 4;               // 32-bit number
            internal const int REG_DWORD_LITTLE_ENDIAN = 4; // 32-bit number (same as REG_DWORD)
            internal const int REG_DWORD_BIG_ENDIAN = 5;    // 32-bit number
            internal const int REG_LINK = 6;                // Symbolic Link (Unicode)
            internal const int REG_MULTI_SZ = 7;            // Multiple Unicode strings
            internal const int REG_QWORD = 11;             // 64-bit number
        }
    }
}
