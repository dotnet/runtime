// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: CoreCLR wrapper for Win32, either the native
**          operations or the Unix PAL implementation of them.
**
**
===========================================================*/
/*
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
 *    bool GetVersionEx(OSVERSIONINFO &amp; lposvi);
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
 * GetVersionExA &amp; a GetVersionExW.  Also, the OSVERSIONINFO struct has a sizeof
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
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    /**
     * Win32 encapsulation for System.Private.CoreLib.
     */
    internal static class Win32Native
    {
        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR = (LMEM_FIXED | LMEM_ZEROINIT);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal unsafe struct OSVERSIONINFOEX
        {
            internal int dwOSVersionInfoSize;
            internal int dwMajorVersion;
            internal int dwMinorVersion;
            internal int dwBuildNumber;
            internal int dwPlatformId;
            internal fixed char szCSDVersion[128];
            internal ushort wServicePackMajor;
            internal ushort wServicePackMinor;
            internal short wSuiteMask;
            internal byte wProductType;
            internal byte wReserved;
        }

        internal const string ADVAPI32 = "advapi32.dll";

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "LocalAlloc")]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, UIntPtr sizetdwBytes);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        [DllImport(Interop.Libraries.Kernel32)]
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags);

        [DllImport(Interop.Libraries.OleAut32, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SysAllocStringLen(string src, int len);  // BSTR

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern uint SysStringLen(IntPtr bstr);

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern void SysFreeString(IntPtr bstr);

        [DllImport(Interop.Libraries.OleAut32)]
        internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);  // BSTR

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);  // param is NOT a handle, but it returns one!

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

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto)]
        internal static extern int GetCurrentThreadId();

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Auto)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport(Interop.Libraries.Ole32)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);

        [DllImport(Interop.Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern uint ExpandEnvironmentStringsW(string lpSrc, ref char lpDst, uint nSize);

        [DllImport(Interop.Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

        internal const byte VER_GREATER_EQUAL = 0x3;
        internal const uint VER_MAJORVERSION = 0x0000002;
        internal const uint VER_MINORVERSION = 0x0000001;
        internal const uint VER_SERVICEPACKMAJOR = 0x0000020;
        internal const uint VER_SERVICEPACKMINOR = 0x0000010;
        [DllImport("kernel32.dll")]
        internal static extern bool VerifyVersionInfoW(ref OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);
        [DllImport("kernel32.dll")]
        internal static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);
    }
}
