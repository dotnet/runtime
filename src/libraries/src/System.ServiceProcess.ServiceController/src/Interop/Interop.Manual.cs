// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    public const int ERROR_MORE_DATA = 234;
    public const int ERROR_INSUFFICIENT_BUFFER = 122;

    public const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;

    public const int ACCEPT_PAUSE_CONTINUE = 0x00000002;
    public const int ACCEPT_SHUTDOWN = 0x00000004;
    public const int ACCEPT_STOP = 0x00000001;

    public const int CONTROL_CONTINUE = 0x00000003;
    public const int CONTROL_PAUSE = 0x00000002;
    public const int CONTROL_STOP = 0x00000001;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class ENUM_SERVICE_STATUS
    {
        public string serviceName = null;
        public string displayName = null;
        public int serviceType = 0;
        public int currentState = 0;
        public int controlsAccepted = 0;
        public int win32ExitCode = 0;
        public int serviceSpecificExitCode = 0;
        public int checkPoint = 0;
        public int waitHint = 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class ENUM_SERVICE_STATUS_PROCESS
    {
        public string serviceName = null;
        public string displayName = null;
        public int serviceType = 0;
        public int currentState = 0;
        public int controlsAccepted = 0;
        public int win32ExitCode = 0;
        public int serviceSpecificExitCode = 0;
        public int checkPoint = 0;
        public int waitHint = 0;
        public int processID = 0;
        public int serviceFlags = 0;
    }

    public const int SC_MANAGER_CONNECT = 0x0001;
    public const int SC_MANAGER_ENUMERATE_SERVICE = 0x0004;

    public const int SC_ENUM_PROCESS_INFO = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct SERVICE_STATUS
    {
        public int serviceType;
        public int currentState;
        public int controlsAccepted;
        public int win32ExitCode;
        public int serviceSpecificExitCode;
        public int checkPoint;
        public int waitHint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe class QUERY_SERVICE_CONFIG
    {
        public int dwServiceType;
        public int dwStartType;
        public int dwErrorControl;
        public char* lpBinaryPathName;
        public char* lpLoadOrderGroup;
        public int dwTagId;
        public char* lpDependencies;
        public char* lpServiceStartName;
        public char* lpDisplayName;
    }

    public const int SERVICE_QUERY_CONFIG = 0x0001;
    public const int SERVICE_CHANGE_CONFIG = 0x0002;
    public const int SERVICE_QUERY_STATUS = 0x0004;
    public const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
    public const int SERVICE_START = 0x0010;
    public const int SERVICE_STOP = 0x0020;
    public const int SERVICE_PAUSE_CONTINUE = 0x0040;
    public const int SERVICE_INTERROGATE = 0x0080;
    public const int SERVICE_USER_DEFINED_CONTROL = 0x0100;

    public const int SERVICE_ALL_ACCESS =
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

    public const int SERVICE_TYPE_ADAPTER = 0x00000004;
    public const int SERVICE_TYPE_FILE_SYSTEM_DRIVER = 0x00000002;
    public const int SERVICE_TYPE_INTERACTIVE_PROCESS = 0x00000100;
    public const int SERVICE_TYPE_KERNEL_DRIVER = 0x00000001;
    public const int SERVICE_TYPE_RECOGNIZER_DRIVER = 0x00000008;
    public const int SERVICE_TYPE_WIN32_OWN_PROCESS = 0x00000010;
    public const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 0x00000020;
    public const int SERVICE_TYPE_WIN32 =
        SERVICE_TYPE_WIN32_OWN_PROCESS |
        SERVICE_TYPE_WIN32_SHARE_PROCESS;
    public const int SERVICE_TYPE_DRIVER =
        SERVICE_TYPE_KERNEL_DRIVER |
        SERVICE_TYPE_FILE_SYSTEM_DRIVER |
        SERVICE_TYPE_RECOGNIZER_DRIVER;
    public const int SERVICE_TYPE_ALL =
        SERVICE_TYPE_WIN32 |
        SERVICE_TYPE_ADAPTER |
        SERVICE_TYPE_DRIVER |
        SERVICE_TYPE_INTERACTIVE_PROCESS;

    public const int START_TYPE_AUTO = 0x00000002;
    public const int START_TYPE_DEMAND = 0x00000003;
    public const int START_TYPE_DISABLED = 0x00000004;

    public const int SERVICE_ACTIVE = 1;
    public const int SERVICE_INACTIVE = 2;
    public const int SERVICE_STATE_ALL = SERVICE_ACTIVE | SERVICE_INACTIVE;

    public const int STATE_CONTINUE_PENDING = 0x00000005;
    public const int STATE_PAUSED = 0x00000007;
    public const int STATE_PAUSE_PENDING = 0x00000006;
    public const int STATE_RUNNING = 0x00000004;
    public const int STATE_START_PENDING = 0x00000002;
    public const int STATE_STOPPED = 0x00000001;
    public const int STATE_STOP_PENDING = 0x00000003;

    public const int STATUS_ACTIVE = 0x00000001;
    public const int STATUS_INACTIVE = 0x00000002;
    public const int STATUS_ALL = STATUS_ACTIVE | STATUS_INACTIVE;

    public static partial class mincore
    {
        [DllImport("api-ms-win-service-management-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool CloseServiceHandle(IntPtr handle);

        [DllImport("api-ms-win-service-winsvc-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe extern static bool ControlService(IntPtr serviceHandle, int control, SERVICE_STATUS* pStatus);

        [DllImport("api-ms-win-service-core-l1-1-1.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool EnumDependentServices(IntPtr serviceHandle, int serviceState, IntPtr bufferOfENUM_SERVICE_STATUS,
            int bufSize, ref int bytesNeeded, ref int numEnumerated);

        [DllImport("api-ms-win-service-core-l1-1-1.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool EnumServicesStatusEx(IntPtr databaseHandle, int infolevel, int serviceType, int serviceState,
            IntPtr status, int size, out int bytesNeeded, out int servicesReturned, ref int resumeHandle, string group);

        [DllImport("api-ms-win-service-management-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static IntPtr OpenSCManager(string machineName, string databaseName, int access);

        [DllImport("api-ms-win-service-management-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static IntPtr OpenService(IntPtr databaseHandle, string serviceName, int access);

        [DllImport("api-ms-win-service-management-l2-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool QueryServiceConfig(IntPtr serviceHandle, IntPtr query_service_config_ptr, int bufferSize, out int bytesNeeded);

        [DllImport("api-ms-win-service-winsvc-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool QueryServiceStatus(IntPtr serviceHandle, SERVICE_STATUS* pStatus);

        [DllImport("api-ms-win-service-management-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool StartService(IntPtr serviceHandle, int argNum, IntPtr argPtrs);
    }
}