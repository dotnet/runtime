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

namespace Microsoft.Win32 {
    using System;
    using System.Security;
#if FEATURE_IMPERSONATION
    using System.Security.Principal;
#endif
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

    [System.Security.SecurityCritical]
    [SuppressUnmanagedCodeSecurityAttribute()]
    internal static class Win32Native {

        internal const int KEY_QUERY_VALUE        = 0x0001;
        internal const int KEY_SET_VALUE          = 0x0002;
        internal const int KEY_CREATE_SUB_KEY     = 0x0004;
        internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        internal const int KEY_NOTIFY             = 0x0010;
        internal const int KEY_CREATE_LINK        = 0x0020;
        internal const int KEY_READ               =((STANDARD_RIGHTS_READ       |
                                                           KEY_QUERY_VALUE            |
                                                           KEY_ENUMERATE_SUB_KEYS     |
                                                           KEY_NOTIFY)                 
                                                          &                           
                                                          (~SYNCHRONIZE));
    
        internal const int KEY_WRITE              =((STANDARD_RIGHTS_WRITE      |
                                                           KEY_SET_VALUE              |
                                                           KEY_CREATE_SUB_KEY)         
                                                          &                           
                                                          (~SYNCHRONIZE));
        internal const int KEY_WOW64_64KEY        = 0x0100;     //
        internal const int KEY_WOW64_32KEY        = 0x0200;     //
        internal const int REG_OPTION_NON_VOLATILE= 0x0000;     // (default) keys are persisted beyond reboot/unload
        internal const int REG_OPTION_VOLATILE    = 0x0001;     // All keys created by the function are volatile
        internal const int REG_OPTION_CREATE_LINK = 0x0002;     // They key is a symbolic link
        internal const int REG_OPTION_BACKUP_RESTORE = 0x0004;  // Use SE_BACKUP_NAME process special privileges
        internal const int REG_NONE                    = 0;     // No value type
        internal const int REG_SZ                      = 1;     // Unicode nul terminated string
        internal const int REG_EXPAND_SZ               = 2;     // Unicode nul terminated string
        // (with environment variable references)
        internal const int REG_BINARY                  = 3;     // Free form binary
        internal const int REG_DWORD                   = 4;     // 32-bit number
        internal const int REG_DWORD_LITTLE_ENDIAN     = 4;     // 32-bit number (same as REG_DWORD)
        internal const int REG_DWORD_BIG_ENDIAN        = 5;     // 32-bit number
        internal const int REG_LINK                    = 6;     // Symbolic Link (unicode)
        internal const int REG_MULTI_SZ                = 7;     // Multiple Unicode strings
        internal const int REG_RESOURCE_LIST           = 8;     // Resource list in the resource map
        internal const int REG_FULL_RESOURCE_DESCRIPTOR  = 9;   // Resource list in the hardware description
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10; 
        internal const int REG_QWORD                   = 11;    // 64-bit number

        internal const int HWND_BROADCAST              = 0xffff;
        internal const int WM_SETTINGCHANGE            = 0x001A;

        // CryptProtectMemory and CryptUnprotectMemory.
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE    = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS  = 0x00;
        internal const uint CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01;
        internal const uint CRYPTPROTECTMEMORY_SAME_LOGON    = 0x02;

        // Security Quality of Service flags
        internal const int SECURITY_ANONYMOUS       = ((int)SECURITY_IMPERSONATION_LEVEL.Anonymous << 16);
        internal const int SECURITY_SQOS_PRESENT    = 0x00100000;

        // Access Control library.
        internal const string MICROSOFT_KERBEROS_NAME = "Kerberos";
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6;

        internal const int SECURITY_ANONYMOUS_LOGON_RID    = 0x00000007;
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B;
        internal const int SECURITY_LOCAL_SYSTEM_RID       = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID     = 0x00000020;

        internal const uint SE_PRIVILEGE_DISABLED           = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const uint SE_PRIVILEGE_ENABLED            = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS    = 0x80000000;

        internal const uint SE_GROUP_MANDATORY          = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        internal const uint SE_GROUP_ENABLED            = 0x00000004;
        internal const uint SE_GROUP_OWNER              = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY  = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID           = 0xC0000000;
        internal const uint SE_GROUP_RESOURCE           = 0x20000000;

        internal const uint DUPLICATE_CLOSE_SOURCE      = 0x00000001;
        internal const uint DUPLICATE_SAME_ACCESS       = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES   = 0x00000004;

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
        internal struct SystemTime {
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
        internal struct TimeZoneInformation {
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

            public TimeZoneInformation(Win32Native.DynamicTimeZoneInformation dtzi) {
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
        internal struct DynamicTimeZoneInformation {
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
        internal struct RegistryTimeZoneInformation {
            [MarshalAs(UnmanagedType.I4)]
            public Int32 Bias;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 StandardBias;
            [MarshalAs(UnmanagedType.I4)]
            public Int32 DaylightBias;
            public SystemTime StandardDate;
            public SystemTime DaylightDate;

            public RegistryTimeZoneInformation(Win32Native.TimeZoneInformation tzi) {
                Bias = tzi.Bias;
                StandardDate = tzi.StandardDate;
                StandardBias = tzi.StandardBias;
                DaylightDate = tzi.DaylightDate;
                DaylightBias = tzi.DaylightBias;
            }

            public RegistryTimeZoneInformation(Byte[] bytes) {
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
                if (bytes == null || bytes.Length != 44) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidREG_TZI_FORMAT"), "bytes");
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
        internal const int READ_CONTROL                    = 0x00020000;
        internal const int SYNCHRONIZE                     = 0x00100000;

        internal const int STANDARD_RIGHTS_READ            = READ_CONTROL;
        internal const int STANDARD_RIGHTS_WRITE           = READ_CONTROL;
    
        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // SEMAPHORE_ALL_ACCESS          (STANDARD_RIGHTS_REQUIRED|SYNCHRONIZE|0x3) 

        // SEMAPHORE and Event both use 0x0002
        // MUTEX uses 0x001 (MUTANT_QUERY_STATE)

        // Note that you may need to specify the SYNCHRONIZE bit as well
        // to be able to open a synchronization primitive.
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002;
        internal const int EVENT_MODIFY_STATE     = 0x00000002;
        internal const int MUTEX_MODIFY_STATE     = 0x00000001;
        internal const int MUTEX_ALL_ACCESS       = 0x001F0001;


        internal const int LMEM_FIXED    = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR          = (LMEM_FIXED | LMEM_ZEROINIT);

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFO {
            internal OSVERSIONINFO() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            internal String CSDVersion = null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal class OSVERSIONINFOEX {
        
            public OSVERSIONINFOEX() {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=128)]
            internal string CSDVersion = null;
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0;
            internal short SuiteMask = 0;
            internal byte ProductType = 0;
            internal byte Reserved = 0; 
        }

        [StructLayout(LayoutKind.Sequential)]
            internal struct SYSTEM_INFO {  
            internal int dwOemId;    // This is a union of a DWORD and a struct containing 2 WORDs.
            internal int dwPageSize;  
            internal IntPtr lpMinimumApplicationAddress;  
            internal IntPtr lpMaximumApplicationAddress;  
            internal IntPtr dwActiveProcessorMask;  
            internal int dwNumberOfProcessors;  
            internal int dwProcessorType;  
            internal int dwAllocationGranularity;  
            internal short wProcessorLevel;  
            internal short wProcessorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES {
            internal int nLength = 0;
            // don't remove null, or this field will disappear in bcl.small
            internal unsafe byte * pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WIN32_FILE_ATTRIBUTE_DATA {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh;
            internal int fileSizeLow;

            [System.Security.SecurityCritical]
            internal void PopulateFrom(WIN32_FIND_DATA findData) {
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
        internal struct FILE_TIME {
            public FILE_TIME(long fileTime) {
                ftTimeLow = (uint) fileTime;
                ftTimeHigh = (uint) (fileTime >> 32);
            }

            public long ToTicks() {
                return ((long) ftTimeHigh << 32) + ftTimeLow;
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct KERB_S4U_LOGON {
            internal uint                   MessageType;
            internal uint                   Flags;
            internal UNICODE_INTPTR_STRING  ClientUpn;   // REQUIRED: UPN for client
            internal UNICODE_INTPTR_STRING  ClientRealm; // Optional: Client Realm, if known
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_OBJECT_ATTRIBUTES {
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes;
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct UNICODE_STRING {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)] internal string Buffer;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct UNICODE_INTPTR_STRING {
            /// <remarks>
            ///     Note - this constructor extracts the raw pointer from the safe handle, so any
            ///     strings created with this version of the constructor will be unsafe to use after the buffer
            ///     has been freed.
            /// </remarks>
            [System.Security.SecurityCritical]  // auto-generated
            internal UNICODE_INTPTR_STRING (int stringBytes, SafeLocalAllocHandle buffer) {
                BCLDebug.Assert(buffer == null || (stringBytes >= 0 && (ulong)stringBytes <= buffer.ByteLength),
                                "buffer == null || (stringBytes >= 0 && stringBytes <= buffer.ByteLength)");

                this.Length = (ushort) stringBytes;
                this.MaxLength = (ushort) buffer.ByteLength;

                // Marshaling with a SafePointer does not work correctly, so unfortunately we need to extract
                // the raw handle here.
                this.Buffer = buffer.DangerousGetHandle();
            }

            /// <remarks>
            ///     This constructor should be used for constructing UNICODE_STRING structures with pointers
            ///     into a block of memory managed by a SafeHandle or the GC.  It shouldn't be used to own
            ///     any memory on its own.
            /// </remarks>
            internal UNICODE_INTPTR_STRING(int stringBytes, IntPtr buffer) {
                BCLDebug.Assert((stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero),
                                "(stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero)");

                this.Length = (ushort)stringBytes;
                this.MaxLength = (ushort)stringBytes;
                this.Buffer = buffer;
            }

            internal ushort Length;
            internal ushort MaxLength;
            internal IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_NAME {
            internal int Use;
            internal UNICODE_INTPTR_STRING Name;
            internal int DomainIndex;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID {
            internal int Use;
            internal uint Rid;
            internal int DomainIndex;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct LSA_TRANSLATED_SID2 {
            internal int Use;
            internal IntPtr Sid;
            internal int DomainIndex;
            uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_TRUST_INFORMATION {
            internal UNICODE_INTPTR_STRING Name;
            internal IntPtr Sid;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LSA_REFERENCED_DOMAIN_LIST {
            internal int Entries;
            internal IntPtr Domains;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID {
            internal uint LowPart;
            internal uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct LUID_AND_ATTRIBUTES {
            internal LUID Luid;
            internal uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct QUOTA_LIMITS {
            internal IntPtr PagedPoolLimit;
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize;
            internal IntPtr MaximumWorkingSetSize;
            internal IntPtr PagefileLimit;
            internal IntPtr TimeLimit;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct SECURITY_LOGON_SESSION_DATA {
            internal uint       Size;
            internal LUID       LogonId;
            internal UNICODE_INTPTR_STRING UserName;
            internal UNICODE_INTPTR_STRING LogonDomain;
            internal UNICODE_INTPTR_STRING AuthenticationPackage;
            internal uint       LogonType;
            internal uint       Session;
            internal IntPtr     Sid;
            internal long       LogonTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct SID_AND_ATTRIBUTES {
            internal IntPtr Sid;
            internal uint   Attributes;
            internal static readonly long SizeOf = (long)Marshal.SizeOf(typeof(SID_AND_ATTRIBUTES));
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_GROUPS {
            internal uint GroupCount;
            internal SID_AND_ATTRIBUTES Groups; // SID_AND_ATTRIBUTES Groups[ANYSIZE_ARRAY];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct TOKEN_PRIMARY_GROUP
        {
            internal IntPtr PrimaryGroup;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_PRIVILEGE {
            internal uint                PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_SOURCE {
            private const int TOKEN_SOURCE_LENGTH = 8;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst=TOKEN_SOURCE_LENGTH)]
            internal char[] Name;
            internal LUID   SourceIdentifier;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_STATISTICS {
            internal LUID   TokenId;
            internal LUID   AuthenticationId;
            internal long   ExpirationTime;
            internal uint   TokenType;
            internal uint   ImpersonationLevel;
            internal uint   DynamicCharged;
            internal uint   DynamicAvailable;
            internal uint   GroupCount;
            internal uint   PrivilegeCount;
            internal LUID   ModifiedId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        internal struct TOKEN_USER {
            internal SID_AND_ATTRIBUTES User;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX {
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
        internal unsafe struct MEMORY_BASIC_INFORMATION {
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
        internal const String USER32   = "user32.dll";
        internal const String OLE32    = "ole32.dll";
        internal const String OLEAUT32 = "oleaut32.dll";
        internal const String NTDLL    = "ntdll.dll";
#else //FEATURE_PAL
        internal const String KERNEL32 = "libcoreclr";
        internal const String USER32   = "libcoreclr";
        internal const String OLE32    = "libcoreclr";
        internal const String OLEAUT32 = "libcoreclr";
        internal const String NTDLL    = "libcoreclr";
#endif //FEATURE_PAL         
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String SHELL32  = "shell32.dll";
        internal const String SHIM     = "mscoree.dll";
        internal const String CRYPT32  = "crypt32.dll";
        internal const String SECUR32  = "secur32.dll";
#if FEATURE_MAIN_CLR_MODULE_USES_CORE_NAME
        internal const String MSCORWKS = "coreclr.dll";
#else //FEATURE_MAIN_CLR_MODULE_USES_CORE_NAME
        internal const String MSCORWKS = "clr.dll";
#endif //FEATURE_MAIN_CLR_MODULE_USES_CORE_NAME

        // From WinBase.h
        internal const int SEM_FAILCRITICALERRORS = 1;

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=true)]
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
                    int dwMessageId, int dwLanguageId, [Out]StringBuilder lpBuffer,
                    int nSize, IntPtr va_list_arguments);

        // Gets an error message for a Win32 error code.
        internal static String GetMessage(int errorCode) {
            StringBuilder sb = StringBuilderCache.Acquire(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
            if (result != 0) {
                // result is the # of characters copied to the StringBuilder.
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else {
                StringBuilderCache.Release(sb);
                return Environment.GetResourceString("UnknownError_Num", errorCode);
            }
        }

        [DllImport(KERNEL32, EntryPoint="LocalAlloc")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, UIntPtr sizetdwBytes);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        SafeLocalAllocHandle LocalAlloc(
            [In] int uFlags, 
            [In] UIntPtr sizetdwBytes);

        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        // MSDN says the length is a SIZE_T.
        [DllImport(NTDLL, EntryPoint = "RtlZeroMemory")]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void ZeroMemory(IntPtr address, UIntPtr length);

        internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer)
        {
            buffer.length = Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            return GlobalMemoryStatusExNative(ref buffer);
        }

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="GlobalMemoryStatusEx")]
        private static extern bool GlobalMemoryStatusExNative([In, Out] ref MEMORYSTATUSEX buffer);

        [DllImport(KERNEL32, SetLastError=true)]
        unsafe internal static extern UIntPtr VirtualQuery(void* address, ref MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        // VirtualAlloc should generally be avoided, but is needed in 
        // the MemoryFailPoint implementation (within a CER) to increase the 
        // size of the page file, ignoring any host memory allocators.
        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern void * VirtualAlloc(void* address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode);

        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        unsafe internal static extern bool VirtualFree(void* address, UIntPtr numBytes, int pageFreeMode);


         
        // Note - do NOT use this to call methods.  Use P/Invoke, which will
        // do much better things w.r.t. marshaling, pinning memory, security 
        // stuff, better interactions with thread aborts, etc.  This is used
        // solely by DoesWin32MethodExist for avoiding try/catch EntryPointNotFoundException
        // in scenarios where an OS Version check is insufficient
        [DllImport(KERNEL32, CharSet=CharSet.Ansi, BestFitMapping=false, SetLastError=true, ExactSpelling=true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, String methodName);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern IntPtr GetModuleHandle(String moduleName);

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool DoesWin32MethodExist(String moduleName, String methodName)
        {
            // GetModuleHandle does not increment the module's ref count, so we don't need to call FreeLibrary.
            IntPtr hModule = Win32Native.GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero) {
                BCLDebug.Assert(hModule != IntPtr.Zero, "GetModuleHandle failed.  Dll isn't loaded?");
                return false;
            }
            IntPtr functionPointer = Win32Native.GetProcAddress(hModule, methodName);
            return (functionPointer != IntPtr.Zero);       
        }

        // There is no need to call CloseProcess or to use a SafeHandle if you get the handle
        // using GetCurrentProcess as it returns a pseudohandle
        [DllImport(KERNEL32, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(
                   [In]
                   IntPtr hSourceProcessHandle,
                   [Out, MarshalAs(UnmanagedType.Bool)]
                   out bool isWow64);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern uint GetTempPath(int bufferLen, [Out]StringBuilder buffer);

        [DllImport(KERNEL32, CharSet=CharSet.Ansi, ExactSpelling=true, EntryPoint="lstrlenA")]
        internal static extern int lstrlenA(IntPtr ptr);

        [DllImport(KERNEL32, CharSet=CharSet.Unicode, ExactSpelling=true, EntryPoint="lstrlenW")]
        internal static extern int lstrlenW(IntPtr ptr);

        [DllImport(Win32Native.OLEAUT32, CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        internal static extern IntPtr SysAllocStringLen(String src, int len);  // BSTR

        [DllImport(Win32Native.OLEAUT32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SysStringLen(IntPtr bstr);

        [DllImport(Win32Native.OLEAUT32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern void SysFreeString(IntPtr bstr);

#if FEATURE_COMINTEROP
        [DllImport(Win32Native.OLEAUT32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]            
        internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);  // BSTR

        [DllImport(Win32Native.OLEAUT32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SysStringByteLen(IntPtr bstr);

#if FEATURE_LEGACYSURFACE
        [DllImport(Win32Native.OLEAUT32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern uint SysStringLen(SafeBSTRHandle bstr);
#endif

#endif

        [DllImport(KERNEL32)]
        internal static extern int GetACP();

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetEvent(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool ResetEvent(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern SafeWaitHandle CreateEvent(SECURITY_ATTRIBUTES lpSecurityAttributes, bool isManualReset, bool initialState, String name);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern SafeWaitHandle OpenEvent(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]            
        internal static extern SafeWaitHandle CreateMutex(SECURITY_ATTRIBUTES lpSecurityAttributes, bool initialOwner, String name);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern SafeWaitHandle OpenMutex(/* DWORD */ int desiredAccess, bool inheritHandle, String name);
  
        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]            
        internal static extern bool ReleaseMutex(SafeWaitHandle handle);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal unsafe static extern int GetFullPathName(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal unsafe static extern int GetFullPathName(String path, int numBufferChars, [Out]StringBuilder buffer, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal unsafe static extern int GetLongPathName(char* path, char* longPathBuffer, int bufferLength);

        [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
        internal unsafe static extern uint GetFullPathNameW(char* path, uint numBufferChars, SafeHandle buffer, IntPtr mustBeZero);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int GetLongPathName(String path, [Out]StringBuilder longPathBuffer, int bufferLength);

        [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
        internal static extern uint GetLongPathNameW(SafeHandle lpszShortPath, SafeHandle lpszLongPath, uint cchBuffer);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern uint GetLongPathNameW(string lpszShortPath, SafeHandle lpszLongPath, uint cchBuffer);

        // Disallow access to all non-file devices from methods that take
        // a String.  This disallows DOS devices like "con:", "com1:", 
        // "lpt1:", etc.  Use this to avoid security problems, like allowing
        // a web client asking a server for "http://server/com1.aspx" and
        // then causing a worker process to hang.
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle SafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode,
                                securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile );

            if (!handle.IsInvalid)
            {
                int fileType = Win32Native.GetFileType(handle);
                if (fileType != Win32Native.FILE_TYPE_DISK) {
                    handle.Dispose();
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_FileStreamOnNonFiles"));
                }
            }

            return handle;
        }            

        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle UnsafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile( lpFileName, dwDesiredAccess, dwShareMode,
                                securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile );

            return handle;
        }            
    
        // Do not use these directly, use the safe or unsafe versions above.
        // The safe version does not support devices (aka if will only open
        // files on disk), while the unsafe version give you the full semantic
        // of the native version.
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        private static extern SafeFileHandle CreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint fProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, String lpName);

        [DllImport(KERNEL32, SetLastError=true, ExactSpelling=true)]
        internal static extern IntPtr MapViewOfFile(
            SafeFileMappingHandle handle, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumerOfBytesToMap);

        [DllImport(KERNEL32, ExactSpelling=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress );

        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(KERNEL32)]
        internal static extern int GetFileType(SafeFileHandle handle);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetEndOfFile(SafeFileHandle hFile);

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlushFileBuffers(SafeFileHandle hFile);

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="SetFilePointer")]
        private unsafe static extern int SetFilePointerWin32(SafeFileHandle handle, int lo, int * hi, int origin);
        
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static long SetFilePointer(SafeFileHandle handle, long offset, System.IO.SeekOrigin origin, out int hr) {
            hr = 0;
            int lo = (int) offset;
            int hi = (int) (offset >> 32);
            lo = SetFilePointerWin32(handle, lo, &hi, (int) origin);

            if (lo == -1 && ((hr = Marshal.GetLastWin32Error()) != 0))
                return -1;
            return (long) (((ulong) ((uint) hi)) << 32) | ((uint) lo);
        }

        // Note there are two different ReadFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndRead method called by that callback), 
        // and pass in an address for the numBytesRead parameter.  

        [DllImport(KERNEL32, SetLastError=true)]
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);

        [DllImport(KERNEL32, SetLastError=true)]
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);
        
        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.  

        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

        // This is only available on Vista or higher
        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern bool CancelIoEx(SafeFileHandle handle, NativeOverlapped* lpOverlapped);

        // NOTE: The out parameters are PULARGE_INTEGERs and may require
        // some byte munging magic.
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern int GetDriveType(String drive);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool GetVolumeInformation(String drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName);

        // The following 4 methods are used by Microsoft.WlcProfile
        [DllImport(KERNEL32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceCounter(out long value);

        [DllImport(KERNEL32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceFrequency(out long value);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle CreateSemaphore(SECURITY_ATTRIBUTES lpSecurityAttributes, int initialCount, int maximumCount, String name);

        [DllImport(KERNEL32, SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern SafeWaitHandle OpenSemaphore(/* DWORD */ int desiredAccess, bool inheritHandle, String name);

        // Will be in winnls.h
        internal const int FIND_STARTSWITH  = 0x00100000; // see if value is at the beginning of source
        internal const int FIND_ENDSWITH    = 0x00200000; // see if value is at the end of source
        internal const int FIND_FROMSTART   = 0x00400000; // look for value in source, starting at the beginning
        internal const int FIND_FROMEND     = 0x00800000; // look for value in source, starting at the end

#if !FEATURE_CORECLR
        [StructLayout(LayoutKind.Sequential)]
        internal struct NlsVersionInfoEx 
        {
            internal int dwNLSVersionInfoSize;
            internal int dwNLSVersion;
            internal int dwDefinedVersion;
            internal int dwEffectiveId;
            internal Guid guidCustomVersion;
        }
#endif

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern int GetWindowsDirectory([Out]StringBuilder sb, int length);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern int GetSystemDirectory([Out]StringBuilder sb, int length);

        [DllImport(KERNEL32, SetLastError=true)]
        internal unsafe static extern bool SetFileTime(SafeFileHandle hFile, FILE_TIME* creationTime,
                    FILE_TIME* lastAccessTime, FILE_TIME* lastWriteTime);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern int GetFileSize(SafeFileHandle hFile, out int highSize);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
        
        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
  
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        // Note, these are #defines used to extract handles, and are NOT handles.
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;

        [DllImport(KERNEL32, SetLastError=true)]
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
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        internal const uint FILE_MAP_WRITE = 0x0002;
        internal const uint FILE_MAP_READ = 0x0004;

        // Constants from WinNT.h
        internal const int FILE_ATTRIBUTE_READONLY      = 0x00000001;
        internal const int FILE_ATTRIBUTE_DIRECTORY     = 0x00000010;
        internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        internal const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);

        internal const int PAGE_READWRITE = 0x04;

        internal const int MEM_COMMIT  =  0x1000;
        internal const int MEM_RESERVE =  0x2000;
        internal const int MEM_RELEASE =  0x8000;
        internal const int MEM_FREE    = 0x10000;

        // Error codes from WinError.h
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED  = 0x5;
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

        internal const int INVALID_FILE_SIZE     = -1;

        // From WinStatus.h
        internal const int STATUS_ACCOUNT_RESTRICTION = unchecked((int) 0xC000006E);

        // Use this to translate error codes like the above into HRESULTs like
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");
            return unchecked(((int)0x80070000) | errorCode);
        }

        // Win32 Structs in N/Direct style
        [Serializable]
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        [BestFitMapping(false)]
        internal class WIN32_FIND_DATA {
            internal int  dwFileAttributes = 0;
            // ftCreationTime was a by-value FILETIME structure
            internal uint ftCreationTime_dwLowDateTime = 0 ;
            internal uint ftCreationTime_dwHighDateTime = 0;
            // ftLastAccessTime was a by-value FILETIME structure
            internal uint ftLastAccessTime_dwLowDateTime = 0;
            internal uint ftLastAccessTime_dwHighDateTime = 0;
            // ftLastWriteTime was a by-value FILETIME structure
            internal uint ftLastWriteTime_dwLowDateTime = 0;
            internal uint ftLastWriteTime_dwHighDateTime = 0;
            internal int  nFileSizeHigh = 0;
            internal int  nFileSizeLow = 0;
            // If the file attributes' reparse point flag is set, then
            // dwReserved0 is the file tag (aka reparse tag) for the 
            // reparse point.  Use this to figure out whether something is
            // a volume mount point or a symbolic link.
            internal int  dwReserved0 = 0;
            internal int  dwReserved1 = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
            internal String   cFileName = null;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
            internal String   cAlternateFileName = null;
        }

#if FEATURE_CORESYSTEM
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        private static extern bool CopyFileEx(String src,
                                              String dst,
                                              IntPtr progressRoutine,
                                              IntPtr progressData,
                                              ref uint cancel,
                                              uint flags);

        internal static bool CopyFile(String src, String dst, bool failIfExists)
        {
            uint cancel = 0;
            return CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, failIfExists ? 1U : 0U);
        }
#else // FEATURE_CORESYSTEM
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool CopyFile(
                    String src, String dst, bool failIfExists);
#endif // FEATURE_CORESYSTEM

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool CreateDirectory(
                    String path, SECURITY_ATTRIBUTES lpSecurityAttributes);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool DeleteFile(String path);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool ReplaceFile(String replacedFileName, String replacementFileName, String backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool DecryptFile(String path, int reservedMustBeZero);

        [DllImport(ADVAPI32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool EncryptFile(String path);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Win32Native.WIN32_FIND_DATA data);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool FindNextFile(
                    SafeFindHandle hndFindFile,
                    [In, Out, MarshalAs(UnmanagedType.LPStruct)]
                    WIN32_FIND_DATA lpFindFileData);

        [DllImport(KERNEL32)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool FindClose(IntPtr handle);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int GetCurrentDirectory(
                  int nBufferLength,
                  [Out]StringBuilder lpBuffer);

        [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
        internal static extern uint GetCurrentDirectoryW(uint nBufferLength, SafeHandle lpBuffer);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool GetFileAttributesEx(String name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool SetFileAttributes(String name, int attr);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern int GetLogicalDrives();

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern uint GetTempFileName(String tmpPath, String prefix, uint uniqueIdOrZero, [Out]StringBuilder tmpFileName);

#if FEATURE_CORESYSTEM
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        private static extern bool MoveFileEx(String src, String dst, uint flags);

        internal static bool MoveFile(String src, String dst)
        {
            return MoveFileEx(src, dst, 2 /* MOVEFILE_COPY_ALLOWED */);
        }
#else // FEATURE_CORESYSTEM
        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool MoveFile(String src, String dst);
#endif // FEATURE_CORESYSTEM

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool DeleteVolumeMountPoint(String mountPoint);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool RemoveDirectory(String path);

        [DllImport(KERNEL32, SetLastError=true, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern bool SetCurrentDirectory(String path);

        [DllImport(KERNEL32, SetLastError=false, EntryPoint="SetErrorMode", ExactSpelling=true)]
        private static extern int SetErrorMode_VistaAndOlder(int newMode);

        [DllImport(KERNEL32, SetLastError=true, EntryPoint="SetThreadErrorMode")]
        private static extern bool SetErrorMode_Win7AndNewer(int newMode, out int oldMode);

        // RTM versions of Win7 and Windows Server 2008 R2
        private static readonly Version ThreadErrorModeMinOsVersion = new Version(6, 1, 7600);

        // this method uses the thread-safe version of SetErrorMode on Windows 7 / Windows Server 2008 R2 operating systems.
        internal static int SetErrorMode(int newMode)
        {
#if !FEATURE_CORESYSTEM // ARMSTUB
            if (Environment.OSVersion.Version >= ThreadErrorModeMinOsVersion)
            {
                int oldMode;
                SetErrorMode_Win7AndNewer(newMode, out oldMode);
                return oldMode;
            }
#endif
            return SetErrorMode_VistaAndOlder(newMode);
        }

        internal const int LCID_SUPPORTED = 0x00000002;  // supported locale ids

        [DllImport(KERNEL32)]
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char* pwzSource, int cchSource, byte* pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2);

        // A Win32 HandlerRoutine
        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);

        [DllImport(KERNEL32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);
        
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern int GetEnvironmentVariable(string lpName, [Out]StringBuilder lpValue, int size);

        [DllImport(KERNEL32, CharSet=CharSet.Unicode)]
        internal static unsafe extern char * GetEnvironmentStrings();

        [DllImport(KERNEL32, CharSet=CharSet.Unicode)]
        internal static unsafe extern bool FreeEnvironmentStrings(char * pStrings);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern uint GetCurrentProcessId();

        [DllImport(ADVAPI32, CharSet=CharSet.Auto)]
        internal static extern bool GetUserName([Out]StringBuilder lpBuffer, ref int nSize);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal extern static int GetComputerName([Out]StringBuilder nameBuffer, ref int bufferSize);

        [DllImport(OLE32)]
        internal extern static int CoCreateGuid(out Guid guid);

        [DllImport(OLE32)]
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);

        [DllImport(OLE32)]
        internal static extern void CoTaskMemFree(IntPtr ptr);

        [DllImport(OLE32)]
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct COORD
        {
            internal short X;
            internal short Y;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct SMALL_RECT
        {
            internal short Left; 
            internal short Top; 
            internal short Right; 
            internal short Bottom; 
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO 
        {
            internal COORD      dwSize; 
            internal COORD      dwCursorPosition; 
            internal short      wAttributes; 
            internal SMALL_RECT srWindow; 
            internal COORD      dwMaximumWindowSize; 
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        internal struct CONSOLE_CURSOR_INFO 
        {
            internal int dwSize;
            internal bool bVisible;
        }

        // Win32's KEY_EVENT_RECORD
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal struct KeyEventRecord
        {
            internal bool keyDown;
            internal short repeatCount;
            internal short virtualKeyCode;
            internal short virtualScanCode;
            internal char uChar; // Union between WCHAR and ASCII char
            internal int controlKeyState;
        }

        // Really, this is a union of KeyEventRecords and other types.
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        internal struct InputRecord
        {
            internal short eventType;
            internal KeyEventRecord keyEvent;
            // This struct is a union!  Word alighment should take care of padding!
        }

[Serializable]
        [Flags]
        internal enum Color : short
        {
            Black = 0,
            ForegroundBlue = 0x1,
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4,
            ForegroundYellow = 0x6,
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10,
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80,

            ForegroundMask = 0xf,
            BackgroundMask = 0xf0,
            ColorMask = 0xff
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        {
            ushort charData;  // Union between WCHAR and ASCII char
            short attributes;
        }

        internal const int ENABLE_PROCESSED_INPUT  = 0x0001;
        internal const int ENABLE_LINE_INPUT  = 0x0002;
        internal const int ENABLE_ECHO_INPUT  = 0x0004;

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool Beep(int frequency, int duration);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput,
            out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern bool FillConsoleOutputCharacter(IntPtr hConsoleOutput,
            char character, int nLength, COORD dwWriteCoord, out int pNumCharsWritten);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool FillConsoleOutputAttribute(IntPtr hConsoleOutput,
            short wColorAttribute, int numCells, COORD startCoord, out int pNumBytesWritten);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, 
            bool absolute, SMALL_RECT* consoleWindow);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, short attributes);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, 
            COORD cursorPosition);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput, 
            out CONSOLE_CURSOR_INFO cci);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, 
            ref CONSOLE_CURSOR_INFO cci);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=true)]
        internal static extern bool SetConsoleTitle(String title);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion);

        [DllImport(KERNEL32, CharSet=CharSet.Unicode, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool ReadConsoleW(SafeFileHandle hConsoleInput, Byte* lpBuffer, Int32 nNumberOfCharsToRead, out Int32 lpNumberOfCharsRead, IntPtr pInputControl);

        [DllImport(KERNEL32, SetLastError=true)]
        internal static unsafe extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion);

        [DllImport(KERNEL32, CharSet=CharSet.Unicode, SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool WriteConsoleW(SafeFileHandle hConsoleOutput, Byte* lpBuffer, Int32 nNumberOfCharsToWrite, out Int32 lpNumberOfCharsWritten, IntPtr lpReservedMustBeNull);

        [DllImport(USER32)]  // Appears to always succeed
        internal static extern short GetKeyState(int virtualKeyCode);

        [DllImport(KERNEL32, SetLastError=false)]
        internal static extern uint GetConsoleCP();

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleCP(uint codePage);

        [DllImport(KERNEL32, SetLastError=false)]
        internal static extern uint GetConsoleOutputCP();

        [DllImport(KERNEL32, SetLastError=true)]
        internal static extern bool SetConsoleOutputCP(uint codePage);

#if FEATURE_WIN32_REGISTRY
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegConnectRegistry(String machineName,
                    SafeRegistryHandle key, out SafeRegistryHandle result);
    
        // Note: RegCreateKeyEx won't set the last error on failure - it returns
        // an error code if it fails.
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int Reserved, String lpClass, int dwOptions,
                    int samDesired, SECURITY_ATTRIBUTES lpSecurityAttributes,
                    out SafeRegistryHandle hkResult, out int lpdwDisposition);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegDeleteKey(SafeRegistryHandle hKey, String lpSubKey);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegDeleteKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int samDesired, int Reserved);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal unsafe static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex,
                    char *lpName, ref int lpcbName, int[] lpReserved,
                    [Out]StringBuilder lpClass, int[] lpcbClass,
                    long[] lpftLastWriteTime);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal unsafe static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex,
                    char *lpValueName, ref int lpcbValueName,
                    IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData,
                    int[] lpcbData);
    

        [DllImport(ADVAPI32)]
        internal static extern int RegFlushKey(SafeRegistryHandle hKey);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegOpenKeyEx(IntPtr hKey, String lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, [Out]StringBuilder lpClass,
                    int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys,
                    int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen,
                    ref int lpcValues, int[] lpcbMaxValueNameLen,
                    int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor,
                    int[] lpftLastWriteTime);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)] 
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out] byte[] lpData,
                    ref int lpcbData);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref int lpData,
                    ref int lpcbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, ref long lpData,
                    ref int lpcbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                     int[] lpReserved, ref int lpType, [Out] char[] lpData, 
                     ref int lpcbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int[] lpReserved, ref int lpType, [Out]StringBuilder lpData,
                    ref int lpcbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);
    
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName,
                    int Reserved, RegistryValueKind dwType, String lpData, int cbData);
#endif // FEATURE_WIN32_REGISTRY
    
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern int ExpandEnvironmentStrings(String lpSrc, [Out]StringBuilder lpDst, int nSize);

        [DllImport(KERNEL32)]
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags);

        internal const int SHGFP_TYPE_CURRENT               = 0;      // the current (user) folder path setting
        internal const int UOI_FLAGS                        = 1;
        internal const int WSF_VISIBLE                      = 1;

        // .NET Framework 4.0 and newer - all versions of windows ||| \public\sdk\inc\shlobj.h
        internal const int CSIDL_FLAG_CREATE                = 0x8000; // force folder creation in SHGetFolderPath
        internal const int CSIDL_FLAG_DONT_VERIFY           = 0x4000; // return an unverified folder path
        internal const int CSIDL_ADMINTOOLS                 = 0x0030; // <user name>\Start Menu\Programs\Administrative Tools
        internal const int CSIDL_CDBURN_AREA                = 0x003b; // USERPROFILE\Local Settings\Application Data\Microsoft\CD Burning
        internal const int CSIDL_COMMON_ADMINTOOLS          = 0x002f; // All Users\Start Menu\Programs\Administrative Tools
        internal const int CSIDL_COMMON_DOCUMENTS           = 0x002e; // All Users\Documents
        internal const int CSIDL_COMMON_MUSIC               = 0x0035; // All Users\My Music
        internal const int CSIDL_COMMON_OEM_LINKS           = 0x003a; // Links to All Users OEM specific apps
        internal const int CSIDL_COMMON_PICTURES            = 0x0036; // All Users\My Pictures
        internal const int CSIDL_COMMON_STARTMENU           = 0x0016; // All Users\Start Menu
        internal const int CSIDL_COMMON_PROGRAMS            = 0X0017; // All Users\Start Menu\Programs
        internal const int CSIDL_COMMON_STARTUP             = 0x0018; // All Users\Startup
        internal const int CSIDL_COMMON_DESKTOPDIRECTORY    = 0x0019; // All Users\Desktop
        internal const int CSIDL_COMMON_TEMPLATES           = 0x002d; // All Users\Templates
        internal const int CSIDL_COMMON_VIDEO               = 0x0037; // All Users\My Video
        internal const int CSIDL_FONTS                      = 0x0014; // windows\fonts
        internal const int CSIDL_MYVIDEO                    = 0x000e; // "My Videos" folder
        internal const int CSIDL_NETHOOD                    = 0x0013; // %APPDATA%\Microsoft\Windows\Network Shortcuts
        internal const int CSIDL_PRINTHOOD                  = 0x001b; // %APPDATA%\Microsoft\Windows\Printer Shortcuts
        internal const int CSIDL_PROFILE                    = 0x0028; // %USERPROFILE% (%SystemDrive%\Users\%USERNAME%)
        internal const int CSIDL_PROGRAM_FILES_COMMONX86    = 0x002c; // x86 Program Files\Common on RISC
        internal const int CSIDL_PROGRAM_FILESX86           = 0x002a; // x86 C:\Program Files on RISC
        internal const int CSIDL_RESOURCES                  = 0x0038; // %windir%\Resources
        internal const int CSIDL_RESOURCES_LOCALIZED        = 0x0039; // %windir%\resources\0409 (code page)
        internal const int CSIDL_SYSTEMX86                  = 0x0029; // %windir%\system32
        internal const int CSIDL_WINDOWS                    = 0x0024; // GetWindowsDirectory()

        // .NET Framework 3.5 and earlier - all versions of windows
        internal const int CSIDL_APPDATA                    = 0x001a;
        internal const int CSIDL_COMMON_APPDATA             = 0x0023;
        internal const int CSIDL_LOCAL_APPDATA              = 0x001c;
        internal const int CSIDL_COOKIES                    = 0x0021;
        internal const int CSIDL_FAVORITES                  = 0x0006;
        internal const int CSIDL_HISTORY                    = 0x0022;
        internal const int CSIDL_INTERNET_CACHE             = 0x0020;
        internal const int CSIDL_PROGRAMS                   = 0x0002;
        internal const int CSIDL_RECENT                     = 0x0008;
        internal const int CSIDL_SENDTO                     = 0x0009;
        internal const int CSIDL_STARTMENU                  = 0x000b;
        internal const int CSIDL_STARTUP                    = 0x0007;
        internal const int CSIDL_SYSTEM                     = 0x0025;
        internal const int CSIDL_TEMPLATES                  = 0x0015;
        internal const int CSIDL_DESKTOPDIRECTORY           = 0x0010;
        internal const int CSIDL_PERSONAL                   = 0x0005;
        internal const int CSIDL_PROGRAM_FILES              = 0x0026;
        internal const int CSIDL_PROGRAM_FILES_COMMON       = 0x002b;
        internal const int CSIDL_DESKTOP                    = 0x0000;
        internal const int CSIDL_DRIVES                     = 0x0011;
        internal const int CSIDL_MYMUSIC                    = 0x000d;
        internal const int CSIDL_MYPICTURES                 = 0x0027;

        [DllImport(SHELL32, CharSet=CharSet.Auto, BestFitMapping=false)]
        internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, [Out]StringBuilder lpszPath);

        internal const int NameSamCompatible = 2;
        
        [DllImport(SECUR32, CharSet=CharSet.Unicode, SetLastError=true)]     
        // Win32 return type is BOOLEAN (which is 1 byte and not BOOL which is 4bytes)
        internal static extern byte GetUserNameEx(int format, [Out]StringBuilder domainName, ref uint domainNameLen);
        
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true, BestFitMapping=false)]
        internal static extern bool LookupAccountName(string machineName, string accountName, byte[] sid,
                                 ref int sidLen, [Out]StringBuilder domainName, ref uint domainNameLen, out int peUse);

        // Note: This returns a handle, but it shouldn't be closed.  The Avalon
        // team says CloseWindowStation would ignore this handle.  So there
        // isn't a lot of value to switching to SafeHandle here.
        [DllImport(USER32, ExactSpelling=true)]
        internal static extern IntPtr GetProcessWindowStation();

        [DllImport(USER32, SetLastError=true)]
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
            [MarshalAs(UnmanagedType.LPStruct)] USEROBJECTFLAGS pvBuffer, int nLength, ref int lpnLengthNeeded);

        [DllImport(USER32, SetLastError=true, BestFitMapping=false)]
        internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, String lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);

        [StructLayout(LayoutKind.Sequential)]
        internal class USEROBJECTFLAGS {
            internal int fInherit = 0;
            internal int fReserved = 0;
            internal int dwFlags = 0;
        }

        //
        // DPAPI
        //

#if FEATURE_LEGACYSURFACE
        //
        // RtlEncryptMemory and RtlDecryptMemory are declared in the internal header file crypt.h. 
        // They were also recently declared in the public header file ntsecapi.h (in the Platform SDK as well as the current build of Server 2003). 
        // We use them instead of CryptProtectMemory and CryptUnprotectMemory because 
        // they are available in both WinXP and in Windows Server 2003.
        //

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern
        int SystemFunction040 (
            [In,Out] SafeBSTRHandle     pDataIn,
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE
            [In]     uint       dwFlags);

        [DllImport(Win32Native.ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern
        int SystemFunction041 (
            [In,Out] SafeBSTRHandle     pDataIn,
            [In]     uint       cbDataIn,   // multiple of RTL_ENCRYPT_MEMORY_SIZE
            [In]     uint       dwFlags);
#endif // FEATURE_LEGACYSURFACE

#if FEATURE_CORECLR 
        [DllImport(NTDLL, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern
        int RtlNtStatusToDosError (
            [In]    int         status);
#else
        // identical to RtlNtStatusToDosError, but we are in ask mode for desktop CLR
        [DllImport(ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern
        int LsaNtStatusToWinError (
            [In]    int         status);
#endif
        // Get the current FIPS policy setting on Vista and above
        [DllImport("bcrypt.dll")]
        internal static extern uint BCryptGetFipsAlgorithmMode(
                [MarshalAs(UnmanagedType.U1), Out]out bool pfEnabled);

        //
        // Managed ACLs
        //

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern 
        bool AdjustTokenPrivileges (
            [In]     SafeAccessTokenHandle TokenHandle,
            [In]     bool                  DisableAllPrivileges,
            [In]     ref TOKEN_PRIVILEGE   NewState,
            [In]     uint                  BufferLength,
            [In,Out] ref TOKEN_PRIVILEGE   PreviousState,
            [In,Out] ref uint              ReturnLength);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern 
        bool AllocateLocallyUniqueId(
            [In,Out] ref LUID              Luid);

        [DllImport(ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern 
        bool CheckTokenMembership(
            [In]     SafeAccessTokenHandle  TokenHandle,
            [In]     byte[]                 SidToCheck,
            [In,Out] ref bool               IsMember);

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertSecurityDescriptorToStringSecurityDescriptorW",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL ConvertSdToStringSd(
            byte[] securityDescriptor,
            /* DWORD */ uint requestedRevision,
            ULONG securityInformation,
            out IntPtr resultString,
            ref ULONG resultStringLength );

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertStringSecurityDescriptorToSecurityDescriptorW",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL ConvertStringSdToSd(
            string stringSd,
            /* DWORD */ uint stringSdRevision,
            out IntPtr resultSd,
            ref ULONG resultSdLength );

        [DllImport(
             ADVAPI32,
             EntryPoint="ConvertStringSidToSidW",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL ConvertStringSidToSid(
            string stringSid,
            out IntPtr ByteArray
            );

        [DllImport(
           ADVAPI32,
           EntryPoint = "ConvertSidToStringSidW",
           CallingConvention = CallingConvention.Winapi,
           SetLastError = true,
           ExactSpelling = true,
           CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertSidToStringSid(
            IntPtr Sid,
            ref IntPtr StringSid
            );


        [DllImport(
             ADVAPI32,
             EntryPoint="CreateWellKnownSid",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL CreateWellKnownSid(
            int sidType,
            byte[] domainSid,
            [Out] byte[] resultSid,
            ref /*DWORD*/ uint resultSidLength );

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool DuplicateHandle (
            [In]     IntPtr                     hSourceProcessHandle,
            [In]     IntPtr                     hSourceHandle,
            [In]     IntPtr                     hTargetProcessHandle,
            [In,Out] ref SafeAccessTokenHandle  lpTargetHandle,
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle,
            [In]     uint                       dwOptions);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool DuplicateHandle (
            [In]     IntPtr                     hSourceProcessHandle,
            [In]     SafeAccessTokenHandle      hSourceHandle,
            [In]     IntPtr                     hTargetProcessHandle,
            [In,Out] ref SafeAccessTokenHandle  lpTargetHandle,
            [In]     uint                       dwDesiredAccess,
            [In]     bool                       bInheritHandle,
            [In]     uint                       dwOptions);

#if FEATURE_IMPERSONATION
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool DuplicateTokenEx (
            [In]     SafeAccessTokenHandle       ExistingTokenHandle,
            [In]     TokenAccessLevels           DesiredAccess,
            [In]     IntPtr                      TokenAttributes,
            [In]     SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            [In]     System.Security.Principal.TokenType TokenType,
            [In,Out] ref SafeAccessTokenHandle   DuplicateTokenHandle );

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool DuplicateTokenEx (
            [In]     SafeAccessTokenHandle      hExistingToken,
            [In]     uint                       dwDesiredAccess,
            [In]     IntPtr                     lpTokenAttributes,   // LPSECURITY_ATTRIBUTES
            [In]     uint                       ImpersonationLevel,
            [In]     uint                       TokenType,
            [In,Out] ref SafeAccessTokenHandle  phNewToken);
#endif
        [DllImport(
             ADVAPI32,
             EntryPoint="EqualDomainSid",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL IsEqualDomainSid(
            byte[] sid1,
            byte[] sid2,
            out bool result);

        [DllImport(KERNEL32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentThread();

        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityDescriptorLength",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint GetSecurityDescriptorLength(
            IntPtr byteArray );

        [DllImport(
             ADVAPI32,
             EntryPoint="GetSecurityInfo",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint GetSecurityInfoByHandle(
            SafeHandle handle,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor );

        [DllImport(
             ADVAPI32,
             EntryPoint="GetNamedSecurityInfoW",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint GetSecurityInfoByName(
            string name,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor );

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool GetTokenInformation (
            [In]  IntPtr                TokenHandle,
            [In]  uint                  TokenInformationClass,
            [In]  SafeLocalAllocHandle  TokenInformation,
            [In]  uint                  TokenInformationLength,
            [Out] out uint              ReturnLength);

        [DllImport(ADVAPI32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        bool GetTokenInformation (
            [In]  SafeAccessTokenHandle TokenHandle,
            [In]  uint                  TokenInformationClass,
            [In]  SafeLocalAllocHandle  TokenInformation,
            [In]  uint                  TokenInformationLength,
            [Out] out uint              ReturnLength);

        [DllImport(
             ADVAPI32,
             EntryPoint="GetWindowsAccountDomainSid",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL GetWindowsAccountDomainSid(
            byte[] sid,
            [Out] byte[] resultSid,
            ref /*DWORD*/ uint  resultSidLength );

        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            Anonymous = 0,
            Identification = 1,
            Impersonation = 2,
            Delegation = 3,
        }

        // Structures and definitions for Claims that are being introduced in Win8
        // inside the NTTOken - see winnt.h.  They will be surfaced through WindowsIdentity.Claims

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID -> 0x00
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID = 0;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 -> 0x01
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 = 1;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 -> 0x02
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 = 2;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING -> 0x03
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING = 3;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN -> 0x04
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN = 4;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_SID -> 0x05
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_SID = 5;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN -> 0x06
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN = 6;

        // CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING -> 0x10
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING = 16;

        // CLAIM_SECURITY_ATTRIBUTE_NON_INHERITABLE -> 0x0001
        internal const int CLAIM_SECURITY_ATTRIBUTE_NON_INHERITABLE = 1;

        // CLAIM_SECURITY_ATTRIBUTE_VALUE_CASE_SENSITIVE -> 0x0002
        internal const int CLAIM_SECURITY_ATTRIBUTE_VALUE_CASE_SENSITIVE = 2;

        // CLAIM_SECURITY_ATTRIBUTE_USE_FOR_DENY_ONLY -> 0x0004
        internal const int CLAIM_SECURITY_ATTRIBUTE_USE_FOR_DENY_ONLY = 4;

        // CLAIM_SECURITY_ATTRIBUTE_DISABLED_BY_DEFAULT -> 0x0008
        internal const int CLAIM_SECURITY_ATTRIBUTE_DISABLED_BY_DEFAULT = 8;

        // CLAIM_SECURITY_ATTRIBUTE_DISABLED -> 0x0010
        internal const int CLAIM_SECURITY_ATTRIBUTE_DISABLED = 16;

        // CLAIM_SECURITY_ATTRIBUTE_MANDATORY -> 0x0020
        internal const int CLAIM_SECURITY_ATTRIBUTE_MANDATORY = 32;

        internal const int CLAIM_SECURITY_ATTRIBUTE_VALID_FLAGS =
                      CLAIM_SECURITY_ATTRIBUTE_NON_INHERITABLE
                    | CLAIM_SECURITY_ATTRIBUTE_VALUE_CASE_SENSITIVE
                    | CLAIM_SECURITY_ATTRIBUTE_USE_FOR_DENY_ONLY
                    | CLAIM_SECURITY_ATTRIBUTE_DISABLED_BY_DEFAULT
                    | CLAIM_SECURITY_ATTRIBUTE_DISABLED
                    | CLAIM_SECURITY_ATTRIBUTE_MANDATORY;


        [StructLayoutAttribute( LayoutKind.Explicit )]
        internal struct CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1
        {
            // defined as union in CLAIM_SECURITY_ATTRIBUTES_INFORMATION
            [FieldOffsetAttribute( 0 )]
            public IntPtr pAttributeV1;
        }

        [StructLayoutAttribute( LayoutKind.Sequential )]
        internal struct CLAIM_SECURITY_ATTRIBUTES_INFORMATION
        {
            /// WORD->unsigned short
            public ushort Version;

            /// WORD->unsigned short
            public ushort Reserved;

            /// DWORD->unsigned int
            public uint AttributeCount;

            /// CLAIM_SECURITY_ATTRIBUTE_V1
            public CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1 Attribute;
        }

        //
        //  Fully-qualified binary name.
        //
        [StructLayoutAttribute( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
        internal struct CLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE
        {
            // DWORD64->unsigned __int64
            public ulong Version;

            // PWSTR->WCHAR*
            [MarshalAsAttribute( UnmanagedType.LPWStr )]
            public string Name;
        }

        [StructLayoutAttribute( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
        internal struct CLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE
        {
            /// PVOID->void*
            public IntPtr pValue;

            /// DWORD->unsigned int
            public uint ValueLength;
        }

        [StructLayoutAttribute( LayoutKind.Explicit, CharSet = CharSet.Unicode )]
        internal struct CLAIM_VALUES_ATTRIBUTE_V1
        {
            // PLONG64->__int64*
            [FieldOffsetAttribute( 0 )]
            public IntPtr pInt64;

            // PDWORD64->unsigned __int64*
            [FieldOffsetAttribute( 0 )]
            public IntPtr pUint64;

            // PWSTR*
            [FieldOffsetAttribute( 0 )]
            public IntPtr ppString;

            // PCLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE->_CLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE*
            [FieldOffsetAttribute( 0 )]
            public IntPtr pFqbn;

            // PCLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE->_CLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE*
            [FieldOffsetAttribute( 0 )]
            public IntPtr pOctetString;
        }

        [StructLayoutAttribute( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
        internal struct CLAIM_SECURITY_ATTRIBUTE_V1
        {
            // PWSTR->WCHAR*
            [MarshalAsAttribute( UnmanagedType.LPWStr )]
            public string Name;

            // WORD->unsigned short
            public ushort ValueType;

            // WORD->unsigned short
            public ushort Reserved;

            // DWORD->unsigned int
            public uint Flags;

            // DWORD->unsigned int
            public uint ValueCount;

            // struct CLAIM_VALUES - a union of 4 possible values
            public CLAIM_VALUES_ATTRIBUTE_V1 Values;
        }

        [DllImport(
             ADVAPI32,
             EntryPoint="IsWellKnownSid",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern BOOL IsWellKnownSid(
            byte[] sid,
            int type );

        [DllImport(
            ADVAPI32,
            EntryPoint="LsaOpenPolicy",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            ExactSpelling=true,
            CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint LsaOpenPolicy(
            string systemName,
            ref LSA_OBJECT_ATTRIBUTES attributes,
            int accessMask,
            out SafeLsaPolicyHandle handle
            );

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport(
            ADVAPI32,
            EntryPoint="LookupPrivilegeValueW",
            CharSet=CharSet.Auto,
            SetLastError=true,
            ExactSpelling=true,
            BestFitMapping=false)]
        internal static extern 
        bool LookupPrivilegeValue (
            [In]     string             lpSystemName,
            [In]     string             lpName,
            [In,Out] ref LUID           Luid);

        [DllImport(
            ADVAPI32,
            EntryPoint="LsaLookupSids",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            ExactSpelling=true,
            CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint LsaLookupSids(
            SafeLsaPolicyHandle handle,
            int count,
            IntPtr[] sids,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle names
            );

        [DllImport(ADVAPI32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaFreeMemory( IntPtr handle );

        [DllImport(
            ADVAPI32,
            EntryPoint="LsaLookupNames",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            ExactSpelling=true,
            CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint LsaLookupNames(
            SafeLsaPolicyHandle handle,
            int count,
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle sids
            );

        [DllImport(
            ADVAPI32,
            EntryPoint="LsaLookupNames2",
            CallingConvention=CallingConvention.Winapi,
            SetLastError=true,
            ExactSpelling=true,
            CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint LsaLookupNames2(
            SafeLsaPolicyHandle handle,
            int flags,
            int count,
            UNICODE_STRING[] names,
            ref SafeLsaMemoryHandle referencedDomains,
            ref SafeLsaMemoryHandle sids
            );

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        int LsaConnectUntrusted (
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle);

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        int LsaGetLogonSessionData (
            [In]     ref LUID                      LogonId,
            [In,Out] ref SafeLsaReturnBufferHandle ppLogonSessionData);

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        int LsaLogonUser (
            [In]     SafeLsaLogonProcessHandle      LsaHandle,
            [In]     ref UNICODE_INTPTR_STRING      OriginName,
            [In]     uint                           LogonType,
            [In]     uint                           AuthenticationPackage,
            [In]     IntPtr                         AuthenticationInformation,
            [In]     uint                           AuthenticationInformationLength,
            [In]     IntPtr                         LocalGroups,
            [In]     ref TOKEN_SOURCE               SourceContext,
            [In,Out] ref SafeLsaReturnBufferHandle  ProfileBuffer,
            [In,Out] ref uint                       ProfileBufferLength,
            [In,Out] ref LUID                       LogonId,
            [In,Out] ref SafeAccessTokenHandle      Token,
            [In,Out] ref QUOTA_LIMITS               Quotas,
            [In,Out] ref int                        SubStatus);

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        int LsaLookupAuthenticationPackage (
            [In]     SafeLsaLogonProcessHandle LsaHandle,
            [In]     ref UNICODE_INTPTR_STRING PackageName,
            [In,Out] ref uint                  AuthenticationPackage);

        [DllImport(SECUR32, CharSet=CharSet.Auto, SetLastError=true)]
        internal static extern 
        int LsaRegisterLogonProcess (
            [In]     ref UNICODE_INTPTR_STRING     LogonProcessName,
            [In,Out] ref SafeLsaLogonProcessHandle LsaHandle,
            [In,Out] ref IntPtr                    SecurityMode);

        [DllImport(SECUR32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaDeregisterLogonProcess(IntPtr handle);

        [DllImport(ADVAPI32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaClose( IntPtr handle );

        [DllImport(SECUR32, SetLastError=true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static extern int LsaFreeReturnBuffer(IntPtr handle);

#if FEATURE_IMPERSONATION
        [DllImport (ADVAPI32, CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern 
        bool OpenProcessToken (
            [In]     IntPtr                     ProcessToken,
            [In]     TokenAccessLevels          DesiredAccess,
            [Out]    out SafeAccessTokenHandle  TokenHandle);
#endif

        [DllImport(
             ADVAPI32,
             EntryPoint="SetNamedSecurityInfoW",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint SetSecurityInfoByName(
            string name,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            byte[] owner,
            byte[] group,
            byte[] dacl,
            byte[] sacl );

        [DllImport(
             ADVAPI32,
             EntryPoint="SetSecurityInfo",
             CallingConvention=CallingConvention.Winapi,
             SetLastError=true,
             ExactSpelling=true,
             CharSet=CharSet.Unicode)]
        internal static extern /*DWORD*/ uint SetSecurityInfoByHandle(
            SafeHandle handle,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            byte[] owner,
            byte[] group,
            byte[] dacl,
            byte[] sacl );

        // Fusion APIs
#if FEATURE_FUSION
        [DllImport(MSCORWKS, CharSet=CharSet.Unicode)]
        internal static extern int CreateAssemblyNameObject(out IAssemblyName ppEnum, String szAssemblyName, uint dwFlags, IntPtr pvReserved);
    
        [DllImport(MSCORWKS, CharSet=CharSet.Auto)]
        internal static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, IApplicationContext pAppCtx, IAssemblyName pName, uint dwFlags, IntPtr pvReserved);
#endif // FEATURE_FUSION

#if FEATURE_CORECLR
        [DllImport(KERNEL32, CharSet=CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurityAttribute()]
        internal  unsafe static extern int WideCharToMultiByte(
            int     CodePage,
            UInt32    dwFlags,
            char*  lpWideCharStr,
            int      cchWideChar,
            byte*    lpMultiByteStr,
            int      cchMultiByte,
            char*   lpDefaultChar,
            bool*   lpUsedDefaultChar);    

        [DllImport(KERNEL32, CharSet=CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurityAttribute()]
        internal unsafe static extern int MultiByteToWideChar(
            int     CodePage,
            UInt32    dwFlags,
            byte*    lpMultiByteStr,
            int      cchMultiByte,
            char*  lpWideCharStr,
            int      cchWideChar);
#endif  // FEATURE_CORECLR

        [DllImport(KERNEL32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

#if FEATURE_CORECLR
#if FEATURE_PAL
        [DllImport(KERNEL32, EntryPoint = "PAL_Random")]
        internal extern static bool Random(bool bStrong,
                           [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int length);
#else
        private const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

        [DllImport("BCrypt.dll", CharSet = CharSet.Unicode)]
        private static extern uint BCryptGenRandom(IntPtr hAlgorithm, [In, Out] byte[] pbBuffer, int cbBuffer, int dwFlags);

        internal static void Random(bool bStrong, byte[] buffer, int length)
        {
            uint status = BCryptGenRandom(IntPtr.Zero, buffer, length, BCRYPT_USE_SYSTEM_PREFERRED_RNG);
            if (status != STATUS_SUCCESS)
            {
                if (status == STATUS_NO_MEMORY)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }
#endif
#endif
    }
}
