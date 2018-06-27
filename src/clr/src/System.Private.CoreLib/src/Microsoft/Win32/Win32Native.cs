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
 *  [DllImport(Interop.Libraries.Kernel32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
 *  private static extern SafeFileHandle CreateFile(...);
 *
 *  [DllImport(Interop.Libraries.Kernel32, SetLastError=true)]
 *  internal static extern unsafe int ReadFile(SafeFileHandle handle, ...);
 *
 *  [DllImport(Interop.Libraries.Kernel32, SetLastError=true)]
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
 *    [DllImport(Interop.Libraries.Kernel32, CharSet=CharSet.Auto)]
 *    internal static extern bool GetVersionEx(ref OSVERSIONINFO lposvi);
 *
 * OR:
 *
 *    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
 *    internal class OSVERSIONINFO {  ...  }
 *
 *    [DllImport(Interop.Libraries.Kernel32, CharSet=CharSet.Auto)]
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

        // Win32 ACL-related constants:
        internal const int READ_CONTROL = 0x00020000;
        internal const int SYNCHRONIZE = 0x00100000;

        internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
        internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;

        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR = (LMEM_FIXED | LMEM_ZEROINIT);

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

        internal const string ADVAPI32 = "advapi32.dll";
        internal const string SHELL32 = "shell32.dll";
        internal const string SHIM = "mscoree.dll";
        internal const string CRYPT32 = "crypt32.dll";
        internal const string SECUR32 = "secur32.dll";
        internal const string MSCORWKS = "coreclr.dll";

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "LocalAlloc")]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, UIntPtr sizetdwBytes);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer)
        {
            buffer.length = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            return GlobalMemoryStatusExNative(ref buffer);
        }

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true, EntryPoint = "GlobalMemoryStatusEx")]
        private static extern bool GlobalMemoryStatusExNative([In, Out] ref MEMORYSTATUSEX buffer);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe UIntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        // VirtualAlloc should generally be avoided, but is needed in 
        // the MemoryFailPoint implementation (within a CER) to increase the 
        // size of the page file, ignoring any host memory allocators.
        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe void* VirtualAlloc(void* address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe bool VirtualFree(void* address, UIntPtr numBytes, int pageFreeMode);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Ansi, ExactSpelling = true, EntryPoint = "lstrlenA")]
        internal static extern int lstrlenA(IntPtr ptr);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "lstrlenW")]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SysAllocStringLen(string src, int len);  // BSTR

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern uint SysStringLen(IntPtr bstr);

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern void SysFreeString(IntPtr bstr);

#if FEATURE_COMINTEROP
        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);  // BSTR

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern uint SysStringByteLen(IntPtr bstr);

#endif

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!

        internal const int PAGE_READWRITE = 0x04;

        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RESERVE = 0x2000;
        internal const int MEM_RELEASE = 0x8000;
        internal const int MEM_FREE = 0x10000;

        [DllImport(Interop.Libraries.Kernel32)]
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char* pwzSource, int cchSource, byte* pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        private static extern unsafe int GetEnvironmentVariable(string lpName, char* lpValue, int size);

        internal static unsafe int GetEnvironmentVariable(string lpName, Span<char> lpValue)
        {
            fixed (char* lpValuePtr = &MemoryMarshal.GetReference(lpValue))
            {
                return GetEnvironmentVariable(lpName, lpValuePtr, lpValue.Length);
            }
        }

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode)]
        internal static extern unsafe char* GetEnvironmentStrings();

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode)]
        internal static extern unsafe bool FreeEnvironmentStrings(char* pStrings);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);

#if FEATURE_WIN32_REGISTRY

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, string lpValueName);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern unsafe int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    char[] lpName, ref int lpcbName, int[] lpReserved,
                    [Out]StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern unsafe int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    char[] lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, string lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName,
                     int[] lpReserved, ref int lpType, [Out] char[] lpData,
                     ref int lpcbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);

        [DllImport(ADVAPI32, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName,
                    int Reserved, RegistryValueKind dwType, string lpData, int cbData);
#endif // FEATURE_WIN32_REGISTRY

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
        internal static extern int ExpandEnvironmentStrings(string lpSrc, [Out]StringBuilder lpDst, int nSize);

        [DllImport(Interop.Libraries.Kernel32)]
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

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
