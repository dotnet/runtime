// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

internal static partial class Interop
{
    internal const int EFail = unchecked((int)0x80004005);

    public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    public const int STARTF_USESTDHANDLES = 0x00000100;

    public const int STD_INPUT_HANDLE = -10;
    public const int STD_OUTPUT_HANDLE = -11;
    public const int STD_ERROR_HANDLE = -12;

    public const int STILL_ACTIVE = 0x00000103;

    public const int WAIT_TIMEOUT = 0x00000102;

    public const int ERROR_BAD_EXE_FORMAT = 193;
    public const int ERROR_EXE_MACHINE_TYPE_MISMATCH = 216;
    public const int ERROR_NOT_READY = 21;
    public const int ERROR_LOCK_FAILED = 167;
    public const int ERROR_BUSY = 170;
    public const int ERROR_INVALID_HANDLE = 6;
    public const int ERROR_INVALID_PARAMETER = 87;
    public const int ERROR_PARTIAL_COPY = 299;
    public const int ERROR_SUCCESS = 0;

    public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;

    public const int NtPerfCounterSizeLarge = 0x00000100;

    public const int NtQueryProcessBasicInfo = 0;
    public const int NtQuerySystemProcessInformation = 5;

    public const int PROCESS_TERMINATE = 0x0001;
    public const int PROCESS_VM_READ = 0x0010;
    public const int PROCESS_SET_QUOTA = 0x0100;
    public const int PROCESS_SET_INFORMATION = 0x0200;
    public const int PROCESS_QUERY_INFORMATION = 0x0400;
    public const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;

    public const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    public const int SYNCHRONIZE = 0x00100000;

    public const int THREAD_SET_INFORMATION = 0x0020;
    public const int THREAD_QUERY_INFORMATION = 0x0040;

    public const int DUPLICATE_SAME_ACCESS = 2;

    public const int RPC_S_SERVER_UNAVAILABLE = 1722;
    public const int RPC_S_CALL_FAILED = 1726;

    public const int SE_PRIVILEGE_ENABLED = 2;

    public const int TOKEN_ADJUST_PRIVILEGES = 0x20;

    public const int CREATE_NO_WINDOW = 0x08000000;

    public const int SMTO_ABORTIFHUNG = 0x0002;

    public const int GWL_STYLE = (-16);

    public const int WS_DISABLED = 0x08000000;
    public const int WM_NULL = 0x0000;
    public const int WM_CLOSE = 0x0010;


    [StructLayout(LayoutKind.Sequential)]
    internal class STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved = IntPtr.Zero;
        public IntPtr lpDesktop = IntPtr.Zero;
        public IntPtr lpTitle = IntPtr.Zero;
        public int dwX = 0;
        public int dwY = 0;
        public int dwXSize = 0;
        public int dwYSize = 0;
        public int dwXCountChars = 0;
        public int dwYCountChars = 0;
        public int dwFillAttribute = 0;
        public int dwFlags = 0;
        public short wShowWindow = 0;
        public short cbReserved2 = 0;
        public IntPtr lpReserved2 = IntPtr.Zero;
        public SafeFileHandle hStdInput = new SafeFileHandle(IntPtr.Zero, false);
        public SafeFileHandle hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
        public SafeFileHandle hStdError = new SafeFileHandle(IntPtr.Zero, false);

        public STARTUPINFO()
        {
            cb = Marshal.SizeOf(this);
        }

        public void Dispose()
        {
            // close the handles created for child process
            if (hStdInput != null && !hStdInput.IsInvalid)
            {
                hStdInput.Dispose();
                hStdInput = null;
            }

            if (hStdOutput != null && !hStdOutput.IsInvalid)
            {
                hStdOutput.Dispose();
                hStdOutput = null;
            }

            if (hStdError != null && !hStdError.IsInvalid)
            {
                hStdError.Dispose();
                hStdError = null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PERF_COUNTER_BLOCK
    {
        public int ByteLength = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PERF_COUNTER_DEFINITION
    {
        public int ByteLength = 0;
        public int CounterNameTitleIndex = 0;

        // this one is kind of weird. It is defined as in SDK:
        // #ifdef _WIN64
        //  DWORD           CounterNameTitle;
        // #else
        //  LPWSTR          CounterNameTitle;
        // #endif
        // so we can't use IntPtr here.

        public int CounterNameTitlePtr = 0;
        public int CounterHelpTitleIndex = 0;
        public int CounterHelpTitlePtr = 0;
        public int DefaultScale = 0;
        public int DetailLevel = 0;
        public int CounterType = 0;
        public int CounterSize = 0;
        public int CounterOffset = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PERF_DATA_BLOCK
    {
        public int Signature1 = 0;
        public int Signature2 = 0;
        public int LittleEndian = 0;
        public int Version = 0;
        public int Revision = 0;
        public int TotalByteLength = 0;
        public int HeaderLength = 0;
        public int NumObjectTypes = 0;
        public int DefaultObject = 0;
        public SYSTEMTIME SystemTime = null;
        public int pad1 = 0;  // Need to pad the struct to get quadword alignment for the 'long' after SystemTime
        public long PerfTime = 0;
        public long PerfFreq = 0;
        public long PerfTime100nSec = 0;
        public int SystemNameLength = 0;
        public int SystemNameOffset = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PERF_INSTANCE_DEFINITION
    {
        public int ByteLength = 0;
        public int ParentObjectTitleIndex = 0;
        public int ParentObjectInstance = 0;
        public int UniqueID = 0;
        public int NameOffset = 0;
        public int NameLength = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PERF_OBJECT_TYPE
    {
        public int TotalByteLength = 0;
        public int DefinitionLength = 0;
        public int HeaderLength = 0;
        public int ObjectNameTitleIndex = 0;
        public int ObjectNameTitlePtr = 0;
        public int ObjectHelpTitleIndex = 0;
        public int ObjectHelpTitlePtr = 0;
        public int DetailLevel = 0;
        public int NumCounters = 0;
        public int DefaultCounter = 0;
        public int NumInstances = 0;
        public int CodePage = 0;
        public long PerfTime = 0;
        public long PerfFreq = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class NtProcessBasicInfo
    {
        public int ExitStatus = 0;
        public IntPtr PebBaseAddress = (IntPtr)0;
        public IntPtr AffinityMask = (IntPtr)0;
        public int BasePriority = 0;
        public IntPtr UniqueProcessId = (IntPtr)0;
        public IntPtr InheritedFromUniqueProcessId = (IntPtr)0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class NtModuleInfo
    {
        public IntPtr BaseOfDll = (IntPtr)0;
        public int SizeOfImage = 0;
        public IntPtr EntryPoint = (IntPtr)0;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    [StructLayout(LayoutKind.Sequential)]
    internal class SYSTEMTIME
    {
        public short wYear;
        public short wMonth;
        public short wDayOfWeek;
        public short wDay;
        public short wHour;
        public short wMinute;
        public short wSecond;
        public short wMilliseconds;

        public override string ToString()
        {
            return "[SYSTEMTIME: "
            + wDay.ToString(CultureInfo.CurrentCulture) + "/" + wMonth.ToString(CultureInfo.CurrentCulture) + "/" + wYear.ToString(CultureInfo.CurrentCulture)
            + " " + wHour.ToString(CultureInfo.CurrentCulture) + ":" + wMinute.ToString(CultureInfo.CurrentCulture) + ":" + wSecond.ToString(CultureInfo.CurrentCulture)
            + "]";
        }
    }

    internal static partial class mincore
    {
        [DllImport("api-ms-win-core-wow64-l1-1-0", SetLastError = true)]
        public static extern bool IsWow64Process(SafeProcessHandle hProcess, ref bool Wow64Process);

        [SecurityCritical]
        [DllImport("api-ms-win-core-handle-l1-1-0", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public static string GetComputerName()
        {
            char[] buffer = new char[256];
            uint length = (uint)buffer.Length;

            Interop.mincore.GetComputerName(buffer, ref length);
            return new string(buffer, 0, (int)length);
        }

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out int exitCode);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetProcessTimes(SafeProcessHandle handle, out long creation, out long exit, out long kernel, out long user);

        [DllImport("api-ms-win-core-processthreads-l1-1-1", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetThreadTimes(SafeThreadHandle handle, out long creation, out long exit, out long kernel, out long user);

        [DllImport("api-ms-win-core-processenvironment-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetStdHandle(int whichHandle);

        [DllImport("api-ms-win-core-namedpipe-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateProcessW")]
        public static extern bool CreateProcess(
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpApplicationName,                   // LPCTSTR
            StringBuilder lpCommandLine,                // LPTSTR - note: CreateProcess might insert a null somewhere in this string
            SECURITY_ATTRIBUTES procSecAttrs,          // LPSECURITY_ATTRIBUTES
            SECURITY_ATTRIBUTES threadSecAttrs,        // LPSECURITY_ATTRIBUTES
            bool bInheritHandles,                        // BOOL
            int dwCreationFlags,                        // DWORD
            IntPtr lpEnvironment,                       // LPVOID
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpCurrentDirectory,                  // LPCTSTR
            STARTUPINFO lpStartupInfo,                  // LPSTARTUPINFO
            Interop.PROCESS_INFORMATION lpProcessInformation    // LPPROCESS_INFORMATION
        );

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool TerminateProcess(SafeProcessHandle processHandle, int exitCode);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true)]
        public static extern SafeProcessHandle GetCurrentProcess();

        [DllImport("api-ms-win-core-processthreads-l1-1-1", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern SafeProcessHandle OpenProcess(int access, bool inherit, int processId);

        [DllImport("api-ms-win-core-psapi-obsolete-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "K32EnumProcessModules")]
        public static extern bool EnumProcessModules(SafeProcessHandle handle, IntPtr modules, int size, ref int needed);

        [DllImport("api-ms-win-core-psapi-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "K32EnumProcesses")]
        public static extern bool EnumProcesses(int[] processIds, int size, out int needed);

        [DllImport("api-ms-win-core-psapi-obsolete-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleInformation")]
        public static extern bool GetModuleInformation(SafeProcessHandle processHandle, IntPtr moduleHandle, NtModuleInfo ntModuleInfo, int size);

        [DllImport("api-ms-win-core-psapi-obsolete-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "K32GetModuleBaseNameW")]
        public static extern int GetModuleBaseName(SafeProcessHandle processHandle, IntPtr moduleHandle, StringBuilder baseName, int size);

        [DllImport("api-ms-win-core-psapi-obsolete-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "K32GetModuleFileNameExW")]
        public static extern int GetModuleFileNameEx(SafeProcessHandle processHandle, IntPtr moduleHandle, StringBuilder baseName, int size);

        [DllImport("api-ms-win-core-memory-l1-1-1", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetProcessWorkingSetSizeEx(SafeProcessHandle handle, IntPtr min, IntPtr max, int flags);

        [DllImport("api-ms-win-core-memory-l1-1-1", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetProcessWorkingSetSizeEx(SafeProcessHandle handle, out IntPtr min, out IntPtr max, out int flags);

        // These APIs are in obsolete lib however even the mincore_fw.lib has these APIs, so it should be ok to expose it.
        [DllImport("api-ms-win-core-processtopology-obsolete-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetProcessAffinityMask(SafeProcessHandle handle, IntPtr mask);
        [DllImport("api-ms-win-core-processtopology-obsolete-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetProcessAffinityMask(SafeProcessHandle handle, out IntPtr processMask, out IntPtr systemMask);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetThreadPriorityBoost(SafeThreadHandle handle, out bool disabled);
        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetThreadPriorityBoost(SafeThreadHandle handle, bool disabled);

        [DllImport("api-ms-win-core-processthreads-l1-1-2", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool GetProcessPriorityBoost(SafeProcessHandle handle, out bool disabled);

        [DllImport("api-ms-win-core-processthreads-l1-1-2", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetProcessPriorityBoost(SafeProcessHandle handle, bool disabled);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern SafeThreadHandle OpenThread(int access, bool inherit, int threadId);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetThreadPriority(SafeThreadHandle handle, int priority);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int GetThreadPriority(SafeThreadHandle handle);

        [DllImport("api-ms-win-core-processtopology-obsolete-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetThreadAffinityMask(SafeThreadHandle handle, IntPtr mask);

        [DllImport("api-ms-win-core-kernel32-legacy-l1-1-1", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int SetThreadIdealProcessor(SafeThreadHandle handle, int processor);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern int GetPriorityClass(SafeProcessHandle handle);

        [DllImport("api-ms-win-core-processthreads-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool SetPriorityClass(SafeProcessHandle handle, int priorityClass);

        [DllImport("ntdll.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int NtQueryInformationProcess(SafeProcessHandle processHandle, int query, NtProcessBasicInfo info, int size, int[] returnedSize);

        [DllImport("ntdll.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int NtQuerySystemInformation(int query, IntPtr dataPtr, int size, out int returnedSize);

        [DllImport("api-ms-win-core-handle-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
        public static extern bool DuplicateHandle(
            SafeProcessHandle hSourceProcessHandle,
            SafeHandle hSourceHandle,
            SafeProcessHandle hTargetProcess,
            out SafeFileHandle targetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

        [DllImport("api-ms-win-core-handle-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
        public static extern bool DuplicateHandle(
            SafeProcessHandle hSourceProcessHandle,
            SafeHandle hSourceHandle,
            SafeProcessHandle hTargetProcess,
            out SafeWaitHandle targetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

        [DllImport("api-ms-win-core-processsecurity-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool OpenProcessToken(SafeProcessHandle ProcessHandle, int DesiredAccess, out SafeTokenHandle TokenHandle);

        [DllImport("api-ms-win-security-lsalookup-l2-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        public static extern bool LookupPrivilegeValue([MarshalAs(UnmanagedType.LPTStr)] string lpSystemName, [MarshalAs(UnmanagedType.LPTStr)] string lpName, out LUID lpLuid);

        [DllImport("api-ms-win-security-base-l1-1-0", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(
            SafeTokenHandle TokenHandle,
            bool DisableAllPrivileges,
            TokenPrivileges NewState,
            int BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength
        );

        [DllImport("api-ms-win-core-kernel32-legacy-l1-1-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "GetComputerNameW")]
        internal extern static int GetComputerName(
                    char[] lpBuffer,
                    ref uint nSize);

        [DllImport("api-ms-win-core-processthreads-l1-1-0.dll")]
        internal extern static uint GetCurrentProcessId();

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static uint GetConsoleCP();

        [DllImport("api-ms-win-core-console-l1-1-0.dll")]
        internal extern static uint GetConsoleOutputCP();

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, EntryPoint = "FormatMessageW")]
        internal extern static uint FormatMessage(
                    uint dwFlags,
                    IntPtr lpSource,
                    uint dwMessageId,
                    uint dwLanguageId,
                    char[] lpBuffer,
                    uint nSize,
                    IntPtr Arguments);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class PROCESS_INFORMATION
    {
        public IntPtr hProcess = IntPtr.Zero;
        public IntPtr hThread = IntPtr.Zero;
        public int dwProcessId = 0;
        public int dwThreadId = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SECURITY_ATTRIBUTES
    {
        public int nLength = 12;
        public IntPtr lpSecurityDescriptor = IntPtr.Zero;
        public bool bInheritHandle = false;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class TokenPrivileges
    {
        public int PrivilegeCount = 1;
        public LUID Luid;
        public int Attributes = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LUID
    {
        public int LowPart;
        public int HighPart;
    }
}
