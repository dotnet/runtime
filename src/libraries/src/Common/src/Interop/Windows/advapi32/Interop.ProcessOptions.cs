// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal partial class SEPrivileges
        {
            internal const uint SE_PRIVILEGE_DISABLED = 0;
            internal const int SE_PRIVILEGE_ENABLED = 2;
        }

        internal partial class PerfCounterOptions
        {
            internal const int NtPerfCounterSizeLarge = 0x00000100;
        }

        internal partial class ProcessOptions
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

        internal partial class RPCStatus
        {
            internal const int RPC_S_SERVER_UNAVAILABLE = 1722;
            internal const int RPC_S_CALL_FAILED = 1726;
        }

        internal partial class WaitOptions
        {
            internal const int WAIT_TIMEOUT = 0x00000102;
        }

        internal partial class StartupInfoOptions
        {
            internal const int STARTF_USESTDHANDLES = 0x00000100;
            internal const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
            internal const int CREATE_NO_WINDOW = 0x08000000;
            internal const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        }
    }
}
