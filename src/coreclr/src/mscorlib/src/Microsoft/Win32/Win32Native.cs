// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The CLR wrapper for all Win32 as well as 
**          ROTOR-style Unix PAL, etc. native operations
**
**
===========================================================*/
/**
 * Notes to PInvoke users:  Getting the syntax exactly correct is crucial, and
 * more than a little confusing.  Here's some guidelines.
 *
 * For handles, you should use a SafeHandle subclass specific to your handle
 * type.  For files, we have the following set of interesting definitions:
 *
 *  [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
 *  private static extern SafeFileHandle CreateFile(...);
 *
 *  [DllImport(KERNEL32, SetLastError=true)]
 *  unsafe internal static extern int ReadFile(SafeFileHandle handle, ...);
 *
 *  [DllImport(KERNEL32, SetLastError=true)]
 *  internal static extern bool CloseHandle(IntPtr handle);
 * 
 * P/Invoke will create the SafeFileHandle instance for you and assign the 
 * return value from CreateFile into the handle atomically.  When we call 
 * ReadFile, P/Invoke will increment a ref count, make the call, then decrement
 * it (preventing handle recycling vulnerabilities).  Then SafeFileHandle's
 * ReleaseHandle method will call CloseHandle, passing in the handle field
 * as an IntPtr.
 *
 * If for some reason you cannot use a SafeHandle subclass for your handles,
 * then use IntPtr as the handle type (or possibly HandleRef - understand when
 * to use GC.KeepAlive).  If your code will run in SQL Server (or any other
 * long-running process that can't be recycled easily), use a constrained 
 * execution region to prevent thread aborts while allocating your 
 * handle, and consider making your handle wrapper subclass 
 * CriticalFinalizerObject to ensure you can free the handle.  As you can 
 * probably guess, SafeHandle  will save you a lot of headaches if your code 
 * needs to be robust to thread aborts and OOM.
 *
 *
 * If you have a method that takes a native struct, you have two options for
 * declaring that struct.  You can make it a value type ('struct' in CSharp),
 * or a reference type ('class').  This choice doesn't seem very interesting, 
 * but your function prototype must use different syntax depending on your 
 * choice.  For example, if your native method is prototyped as such:
 *
 *    bool GetVersionEx(OSVERSIONINFO & lposvi);
 *
 *
 * you must use EITHER THIS OR THE NEXT syntax:
 *
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
 *    internal struct OSVERSIONINFO {  ...  }
 *
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)]
 *    internal static extern bool GetVersionEx(ref OSVERSIONINFO lposvi);
 *
 * OR:
 *
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
 *    internal class OSVERSIONINFO {  ...  }
 *
 *    [DllImport(KERNEL32, CharSet=CharSet.Auto)]
 *    internal static extern bool GetVersionEx([In, Out] OSVERSIONINFO lposvi);
 *
 * Note that classes require being marked as [In, Out] while value types must
 * be passed as ref parameters.
 *
 * Also note the CharSet.Auto on GetVersionEx - while it does not take a String
 * as a parameter, the OSVERSIONINFO contains an embedded array of TCHARs, so
 * the size of the struct varies on different platforms, and there's a
 * GetVersionExA & a GetVersionExW.  Also, the OSVERSIONINFO struct has a sizeof
 * field so the OS can ensure you've passed in the correctly-sized copy of an
 * OSVERSIONINFO.  You must explicitly set this using Marshal.SizeOf(Object);
 *
 * For security reasons, if you're making a P/Invoke method to a Win32 method
 * that takes an ANSI String and that String is the name of some resource you've 
 * done a security check on (such as a file name), you want to disable best fit 
 * mapping in WideCharToMultiByte.  Do this by setting BestFitMapping=false 
 * in your DllImportAttribute.
 */

namespace Microsoft.Win32
{
    using System;
    using System.Security;
    using System.Text;
    using System.Configuration.Assemblies;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;

    using BOOL = System.Int32;
    using DWORD = System.UInt32;
    using ULONG = System.UInt32;

    /**
     * Win32 encapsulation for MSCORLIB.
     */
    // Remove the default demands for all P/Invoke methods with this
    // global declaration on the class.

    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class Win32Native
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
        internal const int KEY_WOW64_64KEY = 0x0100;     //
        internal const int KEY_WOW64_32KEY = 0x0200;     //
        internal const int REG_OPTION_NON_VOLATILE = 0x0000;     // (default) keys are persisted beyond reboot/unload
        internal const int REG_OPTION_VOLATILE = 0x0001;     // All keys created by the function are volatile
        internal const int REG_OPTION_CREATE_LINK = 0x0002;     // They key is a symbolic link
        internal const int REG_OPTION_BACKUP_RESTORE = 0x0004;  // Use SE_BACKUP_NAME process special privileges
        internal const int REG_NONE = 0;     // No value type
        internal const int REG_SZ = 1;     // Unicode nul terminated string
        internal const int REG_EXPAND_SZ = 2;     // Unicode nul terminated string
        // (with environment variable references)
        internal const int REG_BINARY = 3;     // Free form binary
        internal const int REG_DWORD = 4;     // 32-bit number
        internal const int REG_DWORD_LITTLE_ENDIAN = 4;     // 32-bit number (same as REG_DWORD)
        internal const int REG_DWORD_BIG_ENDIAN = 5;     // 32-bit number
        internal const int REG_LINK = 6;     // Symbolic Link (unicode)
        internal const int REG_MULTI_SZ = 7;     // Multiple Unicode strings
        internal const int REG_RESOURCE_LIST = 8;     // Resource list in the resource map
        internal const int REG_FULL_RESOURCE_DESCRIPTOR = 9;   // Resource list in the hardware description
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        internal const int REG_QWORD = 11;    // 64-bit number

        internal const int HWND_BROADCAST = 0xffff;
        internal const int WM_SETTINGCHANGE = 0x001A;

        // TimeZone
        internal const int TIME_ZONE_ID_INVALID = -1;
        internal const int TIME_ZONE_ID_UNKNOWN = 0;
        internal const int TIME_ZONE_ID_STANDARD = 1;
        internal const int TIME_ZONE_ID_DAYLIGHT = 2;
        internal const int MAX_PATH = 260;

        internal const int MUI_LANGUAGE_ID = 0x4;
        internal const int MUI_LANGUAGE_NAME = 0x8;
        internal const int MUI_PREFERRED_UI_LANGUAGES = 0x10;
        internal const int MUI_INSTALLED_LANGUAGES = 0x20;
        internal const int MUI_ALL_LANGUAGES = 0x40;
        internal const int MUI_LANG_NEUTRAL_PE_FILE = 0x100;
        internal const int MUI_NON_LANG_NEUTRAL_FILE = 0x200;

        internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        internal const int LOAD_STRING_MAX_LENGTH = 500;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            public short Year;
            [MarshalAs(UnmanagedType.U2)]
            public short Month;
            [MarshalAs(UnmanagedType.U2)]
            public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)]
            public short Day;
            [MarshalAs(UnmanagedType.U2)]
            public short Hour;
            [MarshalAs(UnmanagedType.U2)]
            public short Minute;
            [MarshalAs(UnmanagedType.U2)]
            public short Second;
            [MarshalAs(UnmanagedType.U2)]
            public short Milliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TimeZoneInformation
        {
            [MarshalAs(UnmanagedType.I4)]
            public Int32 Bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string StandardName;
            public SystemTime StandardDate;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 StandardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DaylightName;
            public SystemTime DaylightDate;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 DaylightBias;

            public TimeZoneInformation(Win32Native.DynamicTimeZoneInformation dtzi)
            {
                Bias = dtzi.Bias;
                StandardName = dtzi.StandardName;
                StandardDate = dtzi.StandardDate;
                StandardBias = dtzi.StandardBias;
                DaylightName = dtzi.DaylightName;
                DaylightDate = dtzi.DaylightDate;
                DaylightBias = dtzi.DaylightBias;
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DynamicTimeZoneInformation
        {
            [MarshalAs(UnmanagedType.I4)]
            public Int32 Bias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string StandardName;
            public SystemTime StandardDate;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 StandardBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DaylightName;
            public SystemTime DaylightDate;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 DaylightBias;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string TimeZoneKeyName;
            [MarshalAs(UnmanagedType.Bool)]
            public bool DynamicDaylightTimeDisabled;
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct RegistryTimeZoneInformation
        {
            [MarshalAs(UnmanagedType.I4)]
            public Int32 Bias;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 StandardBias;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 DaylightBias;
            public SystemTime StandardDate;
            public SystemTime DaylightDate;

            public RegistryTimeZoneInformation(Win32Native.TimeZoneInformation tzi)
            {
                Bias = tzi.Bias;
                StandardDate = tzi.StandardDate;
                StandardBias = tzi.StandardBias;
                DaylightDate = tzi.DaylightDate;
                DaylightBias = tzi.DaylightBias;
            }

            public RegistryTimeZoneInformation(Byte[] bytes)
            {
                //
                // typedef struct _REG_TZI_FORMAT {
                // [00-03]    LONG Bias;
                // [04-07]    LONG StandardBias;
                // [08-11]    LONG DaylightBias;
                // [12-27]    SYSTEMTIME StandardDate;
                // [12-13]        WORD wYear;
                // [14-15]        WORD wMonth;
                // [16-17]        WORD wDayOfWeek;
                // [18-19]        WORD wDay;
                // [20-21]        WORD wHour;
                // [22-23]        WORD wMinute;
                // [24-25]        WORD wSecond;
                // [26-27]        WORD wMilliseconds;
                // [28-43]    SYSTEMTIME DaylightDate;
                // [28-29]        WORD wYear;
                // [30-31]        WORD wMonth;
                // [32-33]        WORD wDayOfWeek;
                // [34-35]        WORD wDay;
                // [36-37]        WORD wHour;
                // [38-39]        WORD wMinute;
                // [40-41]        WORD wSecond;
                // [42-43]        WORD wMilliseconds;
                // } REG_TZI_FORMAT;
                //
                if (bytes == null || bytes.Length != 44)
                {
                    throw new ArgumentException(SR.Argument_InvalidREG_TZI_FORMAT, nameof(bytes));
                }
                Bias = BitConverter.ToInt32(bytes, 0);
                StandardBias = BitConverter.ToInt32(bytes, 4);
                DaylightBias = BitConverter.ToInt32(bytes, 8);

                StandardDate.Year = BitConverter.ToInt16(bytes, 12);
                StandardDate.Month = BitConverter.ToInt16(bytes, 14);
                StandardDate.DayOfWeek = BitConverter.ToInt16(bytes, 16);
                StandardDate.Day = BitConverter.ToInt16(bytes, 18);
                StandardDate.Hour = BitConverter.ToInt16(bytes, 20);
                StandardDate.Minute = BitConverter.ToInt16(bytes, 22);
                StandardDate.Second = BitConverter.ToInt16(bytes, 24);
                StandardDate.Milliseconds = BitConverter.ToInt16(bytes, 26);

                DaylightDate.Year = BitConverter.ToInt16(bytes, 28);
                DaylightDate.Month = BitConverter.ToInt16(bytes, 30);
                DaylightDate.DayOfWeek = BitConverter.ToInt16(bytes, 32);
                DaylightDate.Day = BitConverter.ToInt16(bytes, 34);
                DaylightDate.Hour = BitConverter.ToInt16(bytes, 36);
                DaylightDate.Minute = BitConverter.ToInt16(bytes, 38);
                DaylightDate.Second = BitConverter.ToInt16(bytes, 40);
                DaylightDate.Milliseconds = BitConverter.ToInt16(bytes, 42);
            }
        }

        // end of TimeZone 


        // Win32 ACL-related constants:
        internal const int READ_CONTROL = 0x00020000;
        internal const int SYNCHRONIZE = 0x00100000;

        internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
        internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;

        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // SEMAPHORE_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3) 

        // SEMAPHORE and Event both use 0x0002
        // MUTEX uses 0x001 (MUTANT_QUERY_STATE)

        // Note that you may need to specify the SYNCHRONIZE bit as well
        // to be able to open a synchronization primitive.
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002;
        internal const int EVENT_MODIFY_STATE = 0x00000002;
        internal const int MUTEX_MODIFY_STATE = 0x00000001;
        internal const int MUTEX_ALL_ACCESS = 0x001F0001;


        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR = (LMEM_FIXED | LMEM_ZEROINIT);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVERSIONINFO
        {
            internal OSVERSIONINFO()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal String CSDVersion = null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class OSVERSIONINFOEX
        {
            public OSVERSIONINFOEX()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string CSDVersion = null;
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0;
            internal short SuiteMask = 0;
            internal byte ProductType = 0;
            internal byte Reserved = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            internal int nLength = 0;
            // don't remove null, or this field will disappear in bcl.small
            internal unsafe byte* pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh;
            internal int fileSizeLow;

            internal void PopulateFrom(WIN32_FIND_DATA findData)
            {
                // Copy the information to data
                fileAttributes = findData.dwFileAttributes;
                ftCreationTimeLow = findData.ftCreationTime_dwLowDateTime;
                ftCreationTimeHigh = findData.ftCreationTime_dwHighDateTime;
                ftLastAccessTimeLow = findData.ftLastAccessTime_dwLowDateTime;
                ftLastAccessTimeHigh = findData.ftLastAccessTime_dwHighDateTime;
                ftLastWriteTimeLow = findData.ftLastWriteTime_dwLowDateTime;
                ftLastWriteTimeHigh = findData.ftLastWriteTime_dwHighDateTime;
                fileSizeHigh = findData.nFileSizeHigh;
                fileSizeLow = findData.nFileSizeLow;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            // The length field must be set to the size of this data structure.
            internal int length;
            internal int memoryLoad;
            internal ulong totalPhys;
            internal ulong availPhys;
            internal ulong totalPageFile;
            internal ulong availPageFile;
            internal ulong totalVirtual;
            internal ulong availVirtual;
            internal ulong availExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MEMORY_BASIC_INFORMATION
        {
            internal void* BaseAddress;
            internal void* AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize;
            internal uint State;
            internal uint Protect;
            internal uint Type;
        }

#if !FEATURE_PAL
        internal const String KERNEL32 = "kernel32.dll";
        internal const String USER32 = "user32.dll";
        internal const String OLE32 = "ole32.dll";
        internal const String OLEAUT32 = "oleaut32.dll";
#else //FEATURE_PAL
        internal const String KERNEL32 = "libcoreclr";
        internal const String USER32   = "libcoreclr";
        internal const String OLE32    = "libcoreclr";
        internal const String OLEAUT32 = "libcoreclr";
#endif //FEATURE_PAL         
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String SHELL32 = "shell32.dll";
        internal const String SHIM = "mscoree.dll";
        internal const String CRYPT32 = "crypt32.dll";
        internal const String SECUR32 = "secur32.dll";
        internal const String MSCORWKS = "coreclr.dll";

        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1;

        [DllImport(KERNEL32, CharSet = CharSet.Auto, BestFitMapping = true)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, [Out]StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments);

        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode)
        {
            StringBuilder sb = StringBuilderCache.Acquire(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder.
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else
            {
                StringBuilderCache.Release(sb);
                return SR.Format(SR.UnknownError_Num, errorCode);
            }
        }

        [DllImport(KERNEL32, EntryPoint = "LocalAlloc")]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, UIntPtr sizetdwBytes);

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer)
        {
            buffer.length = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            return GlobalMemoryStatusExNative(ref buffer);
        }

        [DllImport(KERNEL32, SetLastError = true, EntryPoint = "GlobalMemoryStatusEx")]
        private static extern bool GlobalMemoryStatusExNative([In, Out] ref MEMORYSTATUSEX buffer);

        [DllImport(KERNEL32, SetLastError = true)]
        unsafe internal static extern UIntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        // VirtualAlloc should generally be avoided, but is needed in 
        // the MemoryFailPoint implementation (within a CER) to increase the 
        // size of the page file, ignoring any host memory allocators.
        [DllImport(KERNEL32, SetLastError = true)]
        unsafe internal static extern void* VirtualAlloc(void* address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode);

        [DllImport(KERNEL32, SetLastError = true)]
        unsafe internal static extern bool VirtualFree(void* address, UIntPtr numBytes, int pageFreeMode);

        [DllImport(KERNEL32, CharSet = CharSet.Ansi, ExactSpelling = true, EntryPoint = "lstrlenA")]
        internal static extern int lstrlenA(IntPtr ptr);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "lstrlenW")]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(Win32Native.OLEAUT32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SysAllocStringLen(String src, int len);  // BSTR

        [DllImport(Win32Native.OLEAUT32)]
        internal static extern uint SysStringLen(IntPtr bstr);

        [DllImport(Win32Native.OLEAUT32)]
        internal static extern void SysFreeString(IntPtr bstr);

#if FEATURE_COMINTEROP
        [DllImport(Win32Native.OLEAUT32)]
        internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);  // BSTR

        [DllImport(Win32Native.OLEAUT32)]
        internal static extern uint SysStringByteLen(IntPtr bstr);

#endif

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern bool SetEvent(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern bool ResetEvent(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle CreateEvent(SECURITY_ATTRIBUTES lpSecurityAttributes, bool isManualReset, bool initialState, String name);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle OpenEvent(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle CreateMutex(SECURITY_ATTRIBUTES lpSecurityAttributes, bool initialOwner, String name);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle OpenMutex(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern bool ReleaseMutex(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(KERNEL32, SetLastError = true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle CreateSemaphore(SECURITY_ATTRIBUTES lpSecurityAttributes, int initialCount, int maximumCount, String name);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle OpenSemaphore(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        // Will be in winnls.h
        internal const int FIND_STARTSWITH = 0x00100000; // see if value is at the beginning of source
        internal const int FIND_ENDSWITH = 0x00200000; // see if value is at the end of source
        internal const int FIND_FROMSTART = 0x00400000; // look for value in source, starting at the beginning
        internal const int FIND_FROMEND = 0x00800000; // look for value in source, starting at the end

        [StructLayout(LayoutKind.Sequential)]
        internal struct NlsVersionInfoEx
        {
            internal int dwNLSVersionInfoSize;
            internal int dwNLSVersion;
            internal int dwDefinedVersion;
            internal int dwEffectiveId;
            internal Guid guidCustomVersion;
        }

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern int GetSystemDirectory([Out]StringBuilder sb, int length);

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        [DllImport(KERNEL32, SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!

        // From wincon.h
        internal const int CTRL_C_EVENT = 0;
        internal const int CTRL_BREAK_EVENT = 1;
        internal const int CTRL_CLOSE_EVENT = 2;
        internal const int CTRL_LOGOFF_EVENT = 5;
        internal const int CTRL_SHUTDOWN_EVENT = 6;
        internal const short KEY_EVENT = 1;

        // From WinBase.h
        internal const int FILE_TYPE_DISK = 0x0001;
        internal const int FILE_TYPE_CHAR = 0x0002;
        internal const int FILE_TYPE_PIPE = 0x0003;

        internal const int REPLACEFILE_WRITE_THROUGH = 0x1;
        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        internal const uint FILE_MAP_WRITE = 0x0002;
        internal const uint FILE_MAP_READ = 0x0004;

        // Constants from WinNT.h
        internal const int FILE_ATTRIBUTE_READONLY = 0x00000001;
        internal const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        internal const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);

        internal const int PAGE_READWRITE = 0x04;

        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RESERVE = 0x2000;
        internal const int MEM_RELEASE = 0x8000;
        internal const int MEM_FREE = 0x10000;

        // Error codes from WinError.h
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6;
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_DATA = 0xd;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_BROKEN_PIPE = 0x6D;
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
        internal const int ERROR_NO_DATA = 0xE8;
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_DIRECTORY = 0x10B;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
        internal const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation
        internal const int ERROR_NO_TOKEN = 0x3f0;
        internal const int ERROR_DLL_INIT_FAILED = 0x45A;
        internal const int ERROR_NON_ACCOUNT_SID = 0x4E9;
        internal const int ERROR_NOT_ALL_ASSIGNED = 0x514;
        internal const int ERROR_UNKNOWN_REVISION = 0x519;
        internal const int ERROR_INVALID_OWNER = 0x51B;
        internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        internal const int ERROR_NONE_MAPPED = 0x534;
        internal const int ERROR_INVALID_ACL = 0x538;
        internal const int ERROR_INVALID_SID = 0x539;
        internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
        internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
        internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;

        // Error codes from ntstatus.h
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107;
        internal const uint STATUS_NO_MEMORY = 0xC0000017;
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073;
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;

        internal const int INVALID_FILE_SIZE = -1;

        // From WinStatus.h
        internal const int STATUS_ACCOUNT_RESTRICTION = unchecked((int)0xC000006E);

        // Use this to translate error codes like the above into HRESULTs like
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");
            return unchecked(((int)0x80070000) | errorCode);
        }

        // Win32 Structs in N/Direct style
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        [BestFitMapping(false)]
        internal class WIN32_FIND_DATA
        {
            internal int dwFileAttributes = 0;
            // ftCreationTime was a by-value FILETIME structure
            internal uint ftCreationTime_dwLowDateTime = 0;
            internal uint ftCreationTime_dwHighDateTime = 0;
            // ftLastAccessTime was a by-value FILETIME structure
            internal uint ftLastAccessTime_dwLowDateTime = 0;
            internal uint ftLastAccessTime_dwHighDateTime = 0;
            // ftLastWriteTime was a by-value FILETIME structure
            internal uint ftLastWriteTime_dwLowDateTime = 0;
            internal uint ftLastWriteTime_dwHighDateTime = 0;
            internal int nFileSizeHigh = 0;
            internal int nFileSizeLow = 0;
            // If the file attributes' reparse point flag is set, then
            // dwReserved0 is the file tag (aka reparse tag) for the 
            // reparse point.  Use this to figure out whether something is
            // a volume mount point or a symbolic link.
            internal int dwReserved0 = 0;
            internal int dwReserved1 = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal String cFileName = null;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            internal String cAlternateFileName = null;
        }

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Win32Native.WIN32_FIND_DATA data);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern bool FindNextFile(
                    SafeFindHandle hndFindFile,
                    [In, Out, MarshalAs(UnmanagedType.LPStruct)]
                    WIN32_FIND_DATA lpFindFileData);

        [DllImport(KERNEL32)]
        internal static extern bool FindClose(IntPtr handle);

        [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
        internal static extern uint GetCurrentDirectoryW(uint nBufferLength, char[] lpBuffer);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern bool GetFileAttributesEx(String name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern bool SetCurrentDirectory(String path);

        [DllImport(KERNEL32, SetLastError = false, EntryPoint = "SetErrorMode", ExactSpelling = true)]
        private static extern int SetErrorMode_VistaAndOlder(int newMode);

        // RTM versions of Win7 and Windows Server 2008 R2
        private static readonly Version ThreadErrorModeMinOsVersion = new Version(6, 1, 7600);

        // this method uses the thread-safe version of SetErrorMode on Windows 7 / Windows Server 2008 R2 operating systems.
        internal static int SetErrorMode(int newMode)
        {
            return SetErrorMode_VistaAndOlder(newMode);
        }

        internal const int LCID_SUPPORTED = 0x00000002;  // supported locale ids

        [DllImport(KERNEL32)]
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char* pwzSource, int cchSource, byte* pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2);

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern int GetEnvironmentVariable(string lpName, [Out]StringBuilder lpValue, int size);

        [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
        internal static unsafe extern char* GetEnvironmentStrings();

        [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
        internal static unsafe extern bool FreeEnvironmentStrings(char* pStrings);

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(OLE32)]
        internal extern static int CoCreateGuid(out Guid guid);

        [DllImport(OLE32)]
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);

        [DllImport(OLE32)]
        internal static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport(OLE32)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);

#if FEATURE_WIN32_REGISTRY

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal unsafe static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    char[] lpName, ref int lpcbName, int[] lpReserved,
                    [Out]StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal unsafe static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    char[] lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, [Out]StringBuilder lpClass,
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen,
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);
#endif // FEATURE_WIN32_REGISTRY

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern int ExpandEnvironmentStrings(String lpSrc, [Out]StringBuilder lpDst, int nSize);

        [DllImport(KERNEL32)]
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags);

        internal const int SHGFP_TYPE_CURRENT = 0;      // the current (user) folder path setting
        internal const int UOI_FLAGS = 1;
        internal const int WSF_VISIBLE = 1;

        // .NET Framework 4.0 and newer - all versions of windows ||| \public\sdk\inc\shlobj.h
        internal const int CSIDL_FLAG_CREATE = 0x8000; // force folder creation in SHGetFolderPath
        internal const int CSIDL_FLAG_DONT_VERIFY = 0x4000; // return an unverified folder path
        internal const int CSIDL_ADMINTOOLS = 0x0030; // <user name>\Start Menu\Programs\Administrative Tools
        internal const int CSIDL_CDBURN_AREA = 0x003b; // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
        internal const int CSIDL_COMMON_ADMINTOOLS = 0x002f; // All Users\Start Menu\Programs\Administrative Tools
        internal const int CSIDL_COMMON_DOCUMENTS = 0x002e; // All Users\Documents
        internal const int CSIDL_COMMON_MUSIC = 0x0035; // All Users\My Music
        internal const int CSIDL_COMMON_OEM_LINKS = 0x003a; // Links to All Users OEM specific apps
        internal const int CSIDL_COMMON_PICTURES = 0x0036; // All Users\My Pictures
        internal const int CSIDL_COMMON_STARTMENU = 0x0016; // All Users\Start Menu
        internal const int CSIDL_COMMON_PROGRAMS = 0X0017; // All Users\Start Menu\Programs
        internal const int CSIDL_COMMON_STARTUP = 0x0018; // All Users\Startup
        internal const int CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019; // All Users\Desktop
        internal const int CSIDL_COMMON_TEMPLATES = 0x002d; // All Users\Templates
        internal const int CSIDL_COMMON_VIDEO = 0x0037; // All Users\My Video
        internal const int CSIDL_FONTS = 0x0014; // windows\fonts
        internal const int CSIDL_MYVIDEO = 0x000e; // "My Videos" folder
        internal const int CSIDL_NETHOOD = 0x0013; // %APPDATA%\Microsoft\Windows\Network Shortcuts
        internal const int CSIDL_PRINTHOOD = 0x001b; // %APPDATA%\Microsoft\Windows\Printer Shortcuts
        internal const int CSIDL_PROFILE = 0x0028; // %USERPROFILE% (%SystemDrive%\Users\%USERNAME%)
        internal const int CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c; // x86 Program Files\Common on RISC
        internal const int CSIDL_PROGRAM_FILESX86 = 0x002a; // x86 C:\Program Files on RISC
        internal const int CSIDL_RESOURCES = 0x0038; // %windir%\Resources
        internal const int CSIDL_RESOURCES_LOCALIZED = 0x0039; // %windir%\resources\0409 (code page)
        internal const int CSIDL_SYSTEMX86 = 0x0029; // %windir%\system32
        internal const int CSIDL_WINDOWS = 0x0024; // GetWindowsDirectory()

        // .NET Framework 3.5 and earlier - all versions of windows
        internal const int CSIDL_APPDATA = 0x001a;
        internal const int CSIDL_COMMON_APPDATA = 0x0023;
        internal const int CSIDL_LOCAL_APPDATA = 0x001c;
        internal const int CSIDL_COOKIES = 0x0021;
        internal const int CSIDL_FAVORITES = 0x0006;
        internal const int CSIDL_HISTORY = 0x0022;
        internal const int CSIDL_INTERNET_CACHE = 0x0020;
        internal const int CSIDL_PROGRAMS = 0x0002;
        internal const int CSIDL_RECENT = 0x0008;
        internal const int CSIDL_SENDTO = 0x0009;
        internal const int CSIDL_STARTMENU = 0x000b;
        internal const int CSIDL_STARTUP = 0x0007;
        internal const int CSIDL_SYSTEM = 0x0025;
        internal const int CSIDL_TEMPLATES = 0x0015;
        internal const int CSIDL_DESKTOPDIRECTORY = 0x0010;
        internal const int CSIDL_PERSONAL = 0x0005;
        internal const int CSIDL_PROGRAM_FILES = 0x0026;
        internal const int CSIDL_PROGRAM_FILES_COMMON = 0x002b;
        internal const int CSIDL_DESKTOP = 0x0000;
        internal const int CSIDL_DRIVES = 0x0011;
        internal const int CSIDL_MYMUSIC = 0x000d;
        internal const int CSIDL_MYPICTURES = 0x0027;

        internal const int NameSamCompatible = 2;

        [DllImport(USER32, SetLastError = true, BestFitMapping = false)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, String lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

        internal const byte VER_GREATER_EQUAL = 0x3;
        internal const uint VER_MAJORVERSION = 0x0000002;
        internal const uint VER_MINORVERSION = 0x0000001;
        internal const uint VER_SERVICEPACKMAJOR = 0x0000020;
        internal const uint VER_SERVICEPACKMINOR = 0x0000010;
        [DllImport("kernel32.dll")]
        internal static extern bool VerifyVersionInfoW([In, Out] OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);
        [DllImport("kernel32.dll")]
        internal static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);        
    }
}
