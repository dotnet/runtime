// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace System.Diagnostics.Tests
{
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

        [DllImport("kernel32.dll")]
        internal static extern int GetConsoleCP();

        [DllImport("kernel32.dll")]
        internal static extern int GetConsoleOutputCP();

        [DllImport("kernel32.dll")]
        internal static extern int SetConsoleCP(int codePage);

        [DllImport("kernel32.dll")]
        internal static extern int SetConsoleOutputCP(int codePage);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint NetUserAdd(string servername, uint level, ref USER_INFO_1 buf, out uint parm_err);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint NetUserDel(string servername, string username);

        [DllImport("advapi32.dll")]
        internal static extern bool OpenProcessToken(SafeProcessHandle ProcessHandle, uint DesiredAccess, out SafeProcessHandle TokenHandle);

        [DllImport("advapi32.dll")]
        internal static extern bool GetTokenInformation(SafeProcessHandle TokenHandle, uint TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, ref int ReturnLength);

        [DllImport("shell32.dll")]
        internal static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        internal static void NetUserAdd(string username, string password)
        {
            USER_INFO_1 userInfo = new USER_INFO_1();
            userInfo.usri1_name = username;
            userInfo.usri1_password = password;
            userInfo.usri1_priv = 1;

            uint parm_err;
            uint result = NetUserAdd(null, 1, ref userInfo, out parm_err);

            if (result != ExitCodes.NERR_Success)
            {
                // most likely result == ERROR_ACCESS_DENIED
                // due to running without elevated privileges
                throw new Win32Exception((int)result);
            }
        }

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
}
