// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal partial class AcceptOptions
        {
            internal const int ACCEPT_PAUSE_CONTINUE = 0x00000002;
            internal const int ACCEPT_SHUTDOWN = 0x00000004;
            internal const int ACCEPT_STOP = 0x00000001;
        }

        internal partial class ControlOptions
        {
            internal const int CONTROL_CONTINUE = 0x00000003;
            internal const int CONTROL_PAUSE = 0x00000002;
            internal const int CONTROL_STOP = 0x00000001;
        }

        internal partial class ServiceOptions
        {
            internal const int SERVICE_QUERY_CONFIG = 0x0001;
            internal const int SERVICE_CHANGE_CONFIG = 0x0002;
            internal const int SERVICE_QUERY_STATUS = 0x0004;
            internal const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
            internal const int SERVICE_START = 0x0010;
            internal const int SERVICE_STOP = 0x0020;
            internal const int SERVICE_PAUSE_CONTINUE = 0x0040;
            internal const int SERVICE_INTERROGATE = 0x0080;
            internal const int SERVICE_USER_DEFINED_CONTROL = 0x0100;

            internal const int SERVICE_ALL_ACCESS =
                STANDARD_RIGHTS_REQUIRED |
                SERVICE_QUERY_CONFIG |
                SERVICE_CHANGE_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_ENUMERATE_DEPENDENTS |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_INTERROGATE |
                SERVICE_USER_DEFINED_CONTROL;

            internal const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        }

        internal partial class ServiceTypeOptions
        {
            internal const int SERVICE_TYPE_ADAPTER = 0x00000004;
            internal const int SERVICE_TYPE_FILE_SYSTEM_DRIVER = 0x00000002;
            internal const int SERVICE_TYPE_INTERACTIVE_PROCESS = 0x00000100;
            internal const int SERVICE_TYPE_KERNEL_DRIVER = 0x00000001;
            internal const int SERVICE_TYPE_RECOGNIZER_DRIVER = 0x00000008;
            internal const int SERVICE_TYPE_WIN32_OWN_PROCESS = 0x00000010;
            internal const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 0x00000020;
            internal const int SERVICE_TYPE_WIN32 =
                SERVICE_TYPE_WIN32_OWN_PROCESS |
                SERVICE_TYPE_WIN32_SHARE_PROCESS;
            internal const int SERVICE_TYPE_DRIVER =
                SERVICE_TYPE_KERNEL_DRIVER |
                SERVICE_TYPE_FILE_SYSTEM_DRIVER |
                SERVICE_TYPE_RECOGNIZER_DRIVER;
            internal const int SERVICE_TYPE_ALL =
                SERVICE_TYPE_WIN32 |
                SERVICE_TYPE_ADAPTER |
                SERVICE_TYPE_DRIVER |
                SERVICE_TYPE_INTERACTIVE_PROCESS;
        }

        internal partial class ServiceStartModes
        {
            internal const int START_TYPE_BOOT = 0x00000000;
            internal const int START_TYPE_SYSTEM = 0x00000001;
            internal const int START_TYPE_AUTO = 0x00000002;
            internal const int START_TYPE_DEMAND = 0x00000003;
            internal const int START_TYPE_DISABLED = 0x00000004;
        }

        internal partial class ServiceState
        {
            internal const int SERVICE_ACTIVE = 1;
            internal const int SERVICE_INACTIVE = 2;
            internal const int SERVICE_STATE_ALL = SERVICE_ACTIVE | SERVICE_INACTIVE;
        }

        internal partial class StatusOptions
        {
            internal const int STATUS_ACTIVE = 0x00000001;
            internal const int STATUS_INACTIVE = 0x00000002;
            internal const int STATUS_ALL = STATUS_ACTIVE | STATUS_INACTIVE;
        }

        internal partial class ServiceControlStatus
        {
            internal const int STATE_CONTINUE_PENDING = 0x00000005;
            internal const int STATE_PAUSED = 0x00000007;
            internal const int STATE_PAUSE_PENDING = 0x00000006;
            internal const int STATE_RUNNING = 0x00000004;
            internal const int STATE_START_PENDING = 0x00000002;
            internal const int STATE_STOPPED = 0x00000001;
            internal const int STATE_STOP_PENDING = 0x00000003;
        }

        internal partial class ServiceControllerOptions
        {
            internal const int SC_MANAGER_CONNECT = 0x0001;
            internal const int SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
            internal const int SC_ENUM_PROCESS_INFO = 0;
        }
    }
}
