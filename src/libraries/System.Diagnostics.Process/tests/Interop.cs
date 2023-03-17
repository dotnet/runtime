// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential, Size = 40)]
    public struct PROCESS_MEMORY_COUNTERS
    {
        public uint cb;
        public uint PageFaultCount;
        public uint PeakWorkingSetSize;
        public uint WorkingSetSize;
        public uint QuotaPeakPagedPoolUsage;
        public uint QuotaPagedPoolUsage;
        public uint QuotaPeakNonPagedPoolUsage;
        public uint QuotaNonPagedPoolUsage;
        public uint PagefileUsage;
        public uint PeakPagefileUsage;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct USER_INFO_1
    {
        public string usri1_name;
        public string usri1_password;
        public uint usri1_password_age;
        public uint usri1_priv;
        public string usri1_home_dir;
        public string usri1_comment;
        public uint usri1_flags;
        public string usri1_script_path;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES User;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public int Attributes;
    }

    [DllImport("kernel32.dll")]
    public static extern bool GetProcessWorkingSetSizeEx(SafeProcessHandle hProcess, out IntPtr lpMinimumWorkingSetSize, out IntPtr lpMaximumWorkingSetSize, out uint flags);

    [DllImport("kernel32.dll")]
    internal static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

    [DllImport("kernel32.dll")]
    public static extern int GetProcessId(SafeProcessHandle nativeHandle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern void GetStartupInfoW(out STARTUPINFO lpStartupInfo);

    [DllImport("kernel32.dll")]
    internal static extern int GetConsoleCP();

    [DllImport("kernel32.dll")]
    internal static extern int GetConsoleOutputCP();

    [DllImport("kernel32.dll")]
    internal static extern int SetConsoleCP(int codePage);

    [DllImport("kernel32.dll")]
    internal static extern int SetConsoleOutputCP(int codePage);

    [DllImport("advapi32.dll")]
    internal static extern bool OpenProcessToken(SafeProcessHandle ProcessHandle, uint DesiredAccess, out SafeProcessHandle TokenHandle);

    [DllImport("advapi32.dll")]
    internal static extern bool GetTokenInformation(SafeProcessHandle TokenHandle, uint TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, ref int ReturnLength);

    [DllImport("shell32.dll")]
    internal static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

    internal static bool ProcessTokenToSid(SafeProcessHandle token, out SecurityIdentifier sid)
    {
        bool ret = false;
        sid = null;
        IntPtr tu = IntPtr.Zero;
        try
        {
            TOKEN_USER tokUser;
            const int bufLength = 256;

            tu = Marshal.AllocHGlobal(bufLength);
            int cb = bufLength;
            ret = GetTokenInformation(token, 1, tu, cb, ref cb);
            if (ret)
            {
                tokUser = Marshal.PtrToStructure<TOKEN_USER>(tu);
                sid = new SecurityIdentifier(tokUser.User.Sid);
            }
            return ret;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (tu != IntPtr.Zero)
                Marshal.FreeHGlobal(tu);
        }
    }

    internal static class ExitCodes
    {
        internal const uint NERR_Success = 0;
        internal const uint NERR_UserNotFound = 2221;
    }
}
