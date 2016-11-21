// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal partial class RegistryOptions
        {
            internal const int REG_OPTION_NON_VOLATILE = 0x0000;     // (default) keys are persisted beyond reboot/unload
            internal const int REG_OPTION_VOLATILE = 0x0001;        // All keys created by the function are volatile
            internal const int REG_OPTION_CREATE_LINK = 0x0002;     // They key is a symbolic link
            internal const int REG_OPTION_BACKUP_RESTORE = 0x0004;  // Use SE_BACKUP_NAME process special privileges
        }

        internal partial class RegistryView
        {
            internal const int KEY_WOW64_64KEY = 0x0100;
            internal const int KEY_WOW64_32KEY = 0x0200;
        }

        internal partial class RegistryOperations
        {
            internal const int KEY_QUERY_VALUE = 0x0001;
            internal const int KEY_SET_VALUE = 0x0002;
            internal const int KEY_CREATE_SUB_KEY = 0x0004;
            internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
            internal const int KEY_NOTIFY = 0x0010;
            internal const int KEY_CREATE_LINK = 0x0020;
            internal const int KEY_READ = ((STANDARD_RIGHTS_READ |
                                                               KEY_QUERY_VALUE |
                                                               KEY_ENUMERATE_SUB_KEYS |
                                                               KEY_NOTIFY)
                                                              &
                                                              (~SYNCHRONIZE));

            internal const int KEY_WRITE = ((STANDARD_RIGHTS_WRITE |
                                                               KEY_SET_VALUE |
                                                               KEY_CREATE_SUB_KEY)
                                                              &
                                                              (~SYNCHRONIZE));

            internal const int SYNCHRONIZE = 0x00100000;
            internal const int READ_CONTROL = 0x00020000;
            internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
            internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;
        }

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
