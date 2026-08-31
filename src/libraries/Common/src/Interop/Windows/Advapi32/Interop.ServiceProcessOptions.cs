// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal static partial class AcceptOptions
        {
            internal const int ACCEPT_POWEREVENT = 0x00000040;
            internal const int ACCEPT_PAUSE_CONTINUE = 0x00000002;
            internal const int ACCEPT_SESSIONCHANGE = 0x00000080;
            internal const int ACCEPT_SHUTDOWN = 0x00000004;
            internal const int ACCEPT_STOP = 0x00000001;
        }

        internal static partial class ControlOptions
        {
            internal const int CONTROL_CONTINUE = 0x00000003;
            internal const int CONTROL_INTERROGATE = 0x00000004;
            internal const int CONTROL_PAUSE = 0x00000002;
            internal const int CONTROL_POWEREVENT = 0x0000000D;
            internal const int CONTROL_SESSIONCHANGE = 0x0000000E;
            internal const int CONTROL_SHUTDOWN = 0x00000005;
            internal const int CONTROL_STOP = 0x00000001;
        }

        internal static partial class ServiceConfigOptions
        {
            internal const int SERVICE_CONFIG_DESCRIPTION = 0x00000001;
            internal const int SERVICE_CONFIG_FAILURE_ACTIONS = 0x00000002;
            internal const int SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 0x00000003;
        }

        internal static partial class ServiceOptions
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

            internal const int STANDARD_RIGHTS_DELETE = 0x00010000;
            internal const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        }

        internal static partial class ServiceTypeOptions
        {
            internal const int SERVICE_KERNEL_DRIVER = 0x00000001;
            internal const int SERVICE_FILE_SYSTEM_DRIVER = 0x00000002;
            internal const int SERVICE_ADAPTER = 0x00000004;
            internal const int SERVICE_RECOGNIZER_DRIVER = 0x00000008;

            internal const int SERVICE_DRIVER =
                SERVICE_KERNEL_DRIVER |
                SERVICE_FILE_SYSTEM_DRIVER |
                SERVICE_RECOGNIZER_DRIVER;

            internal const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
            internal const int SERVICE_WIN32_SHARE_PROCESS = 0x00000020;

            internal const int SERVICE_WIN32 =
                SERVICE_WIN32_OWN_PROCESS |
                SERVICE_WIN32_SHARE_PROCESS;

            internal const int SERVICE_INTERACTIVE_PROCESS = 0x00000100;
        }

        internal static partial class ServiceAccessOptions
        {
            internal const int ACCESS_TYPE_CHANGE_CONFIG = 0x0002;
            internal const int ACCESS_TYPE_ENUMERATE_DEPENDENTS = 0x0008;
            internal const int ACCESS_TYPE_INTERROGATE = 0x0080;
            internal const int ACCESS_TYPE_PAUSE_CONTINUE = 0x0040;
            internal const int ACCESS_TYPE_QUERY_CONFIG = 0x0001;
            internal const int ACCESS_TYPE_QUERY_STATUS = 0x0004;
            internal const int ACCESS_TYPE_START = 0x0010;
            internal const int ACCESS_TYPE_STOP = 0x0020;
            internal const int ACCESS_TYPE_USER_DEFINED_CONTROL = 0x0100;
            internal const int ACCESS_TYPE_ALL =
                ServiceOptions.STANDARD_RIGHTS_REQUIRED |
                ACCESS_TYPE_QUERY_CONFIG |
                ACCESS_TYPE_CHANGE_CONFIG |
                ACCESS_TYPE_QUERY_STATUS |
                ACCESS_TYPE_ENUMERATE_DEPENDENTS |
                ACCESS_TYPE_START |
                ACCESS_TYPE_STOP |
                ACCESS_TYPE_PAUSE_CONTINUE |
                ACCESS_TYPE_INTERROGATE |
                ACCESS_TYPE_USER_DEFINED_CONTROL;
        }

        internal static partial class ServiceStartModes
        {
            internal const int START_TYPE_BOOT = 0x00000000;
            internal const int START_TYPE_SYSTEM = 0x00000001;
            internal const int START_TYPE_AUTO = 0x00000002;
            internal const int START_TYPE_DEMAND = 0x00000003;
            internal const int START_TYPE_DISABLED = 0x00000004;
        }

        internal static partial class ServiceState
        {
            internal const int SERVICE_ACTIVE = 1;
            internal const int SERVICE_INACTIVE = 2;
            internal const int SERVICE_STATE_ALL = SERVICE_ACTIVE | SERVICE_INACTIVE;
        }

        internal static partial class StatusOptions
        {
            internal const int STATUS_ACTIVE = 0x00000001;
            internal const int STATUS_INACTIVE = 0x00000002;
            internal const int STATUS_ALL = STATUS_ACTIVE | STATUS_INACTIVE;
        }

        internal static partial class ServiceControlStatus
        {
            internal const int STATE_CONTINUE_PENDING = 0x00000005;
            internal const int STATE_PAUSED = 0x00000007;
            internal const int STATE_PAUSE_PENDING = 0x00000006;
            internal const int STATE_RUNNING = 0x00000004;
            internal const int STATE_START_PENDING = 0x00000002;
            internal const int STATE_STOPPED = 0x00000001;
            internal const int STATE_STOP_PENDING = 0x00000003;
            internal const int ERROR_EXCEPTION_IN_SERVICE = 0x00000428;
        }

        internal static partial class ServiceStartErrorModes
        {
            internal const int ERROR_CONTROL_CRITICAL = 0x00000003;
            internal const int ERROR_CONTROL_IGNORE = 0x00000000;
            internal const int ERROR_CONTROL_NORMAL = 0x00000001;
            internal const int ERROR_CONTROL_SEVERE = 0x00000002;
        }

        internal static partial class ServiceControllerOptions
        {
            internal const int SC_ENUM_PROCESS_INFO = 0;
            internal const int SC_MANAGER_CONNECT = 0x0001;
            internal const int SC_MANAGER_CREATE_SERVICE = 0x0002;
            internal const int SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
            internal const int SC_MANAGER_LOCK = 0x0008;
            internal const int SC_MANAGER_MODIFY_BOOT_CONFIG = 0x0020;
            internal const int SC_MANAGER_QUERY_LOCK_STATUS = 0x0010;
            internal const int SC_MANAGER_ALL =
                ServiceOptions.STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG;
        }

        internal static partial class PowerBroadcastStatus
        {
            internal const int PBT_APMBATTERYLOW = 0x0009;
            internal const int PBT_APMOEMEVENT = 0x000B;
            internal const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
            internal const int PBT_APMQUERYSUSPEND = 0x0000;
            internal const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
            internal const int PBT_APMRESUMEAUTOMATIC = 0x0012;
            internal const int PBT_APMRESUMECRITICAL = 0x0006;
            internal const int PBT_APMRESUMESUSPEND = 0x0007;
            internal const int PBT_APMSUSPEND = 0x0004;
        }

        internal static partial class SessionStateChange
        {
            internal const int WTS_CONSOLE_CONNECT = 0x1;
            internal const int WTS_CONSOLE_DISCONNECT = 0x2;
            internal const int WTS_REMOTE_CONNECT = 0x3;
            internal const int WTS_REMOTE_DISCONNECT = 0x4;
            internal const int WTS_SESSION_LOGON = 0x5;
            internal const int WTS_SESSION_LOGOFF = 0x6;
            internal const int WTS_SESSION_LOCK = 0x7;
            internal const int WTS_SESSION_UNLOCK = 0x8;
            internal const int WTS_SESSION_REMOTE_CONTROL = 0x9;
        }
    }
}
