// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal static partial class SEPrivileges
        {
            internal const uint SE_PRIVILEGE_DISABLED = 0;
            internal const int SE_PRIVILEGE_ENABLED = 2;
        }

        internal static partial class PerfCounterOptions
        {
            internal const int NtPerfCounterSizeLarge = 0x00000100;
        }

        internal static partial class ProcessOptions
        {
            internal const int PROCESS_TERMINATE = 0x0001;
            internal const int PROCESS_VM_READ = 0x0010;
            internal const int PROCESS_SET_QUOTA = 0x0100;
            internal const int PROCESS_SET_INFORMATION = 0x0200;
            internal const int PROCESS_QUERY_INFORMATION = 0x0400;
            internal const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            internal const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;


            internal const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            internal const int SYNCHRONIZE = 0x00100000;
        }

        internal static partial class RPCStatus
        {
            internal const int RPC_S_SERVER_UNAVAILABLE = 1722;
            internal const int RPC_S_CALL_FAILED = 1726;
        }

        internal static partial class StartupInfoOptions
        {
            internal const int STARTF_USESTDHANDLES = 0x00000100;
            internal const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
            internal const int CREATE_NO_WINDOW = 0x08000000;
        }
    }
}
