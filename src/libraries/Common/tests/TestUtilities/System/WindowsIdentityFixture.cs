// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System
{
    public class WindowsIdentityFixture : IDisposable
    {
        public WindowsTestAccount TestAccount { get; private set; }

        public WindowsIdentityFixture()
        {
            TestAccount = new WindowsTestAccount("CorFxTstWiIde01kiu");
        }

        public void Dispose()
        {
            TestAccount.Dispose();
        }
    }

    public sealed partial class WindowsTestAccount : IDisposable
    {
        private readonly string _userName;
        private SafeAccessTokenHandle _accountTokenHandle;
        public SafeAccessTokenHandle AccountTokenHandle => _accountTokenHandle;
        public string AccountName { get; private set; }
        public string Password { get; }

        public WindowsTestAccount(string userName)
        {
            Assert.True(PlatformDetection.IsWindows);
            Assert.True(PlatformDetection.IsPrivilegedProcess);

            _userName = userName;
            Password = GeneratePassword();
            CreateUser();
        }

        private static string GeneratePassword()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[33];
                rng.GetBytes(randomBytes);

                // Add special chars to ensure it satisfies password requirements.
                return Convert.ToBase64String(randomBytes) + "_-As@!%*(1)4#2";
            }
        }

        private void CreateUser()
        {
            USER_INFO_1 userInfo = new USER_INFO_1
            {
                usri1_name = _userName,
                usri1_password = Password,
                usri1_priv = 1
            };

            // Create user and remove/create if already exists
            uint result = NetUserAdd(null, 1, ref userInfo, out uint param_err);

            // error codes https://learn.microsoft.com/windows/desktop/netmgmt/network-management-error-codes
            // 0 == NERR_Success
            if (result == 2224) // NERR_UserExists
            {
                result = NetUserDel(null, userInfo.usri1_name);
                if (result != 0)
                {
                    throw new Win32Exception((int)result);
                }
                result = NetUserAdd(null, 1, ref userInfo, out param_err);
                if (result != 0)
                {
                    throw new Win32Exception((int)result);
                }
            }
            else if (result != 0)
            {
                throw new Win32Exception((int)result);
            }

            const int LOGON32_PROVIDER_DEFAULT = 0;
            const int LOGON32_LOGON_INTERACTIVE = 2;

            if (!LogonUser(_userName, ".", Password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out _accountTokenHandle))
            {
                _accountTokenHandle = null;
                throw new Exception($"Failed to get SafeAccessTokenHandle for test account {_userName}", new Win32Exception());
            }

            bool gotRef = false;
            try
            {
                _accountTokenHandle.DangerousAddRef(ref gotRef);
                IntPtr logonToken = _accountTokenHandle.DangerousGetHandle();
                AccountName = new WindowsIdentity(logonToken).Name;
            }
            finally
            {
                if (gotRef)
                    _accountTokenHandle.DangerousRelease();
            }
        }

        [LibraryImport("advapi32.dll", EntryPoint = "LogonUserW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider, out SafeAccessTokenHandle safeAccessTokenHandle);

        [LibraryImport("netapi32.dll", SetLastError = true)]
        internal static partial uint NetUserAdd([MarshalAs(UnmanagedType.LPWStr)] string servername, uint level, ref USER_INFO_1 buf, out uint parm_err);

        [LibraryImport("netapi32.dll")]
        internal static partial uint NetUserDel([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string username);

#if NET
        [NativeMarshalling(typeof(USER_INFO_1.Marshaller))]
#endif
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

#if NET
            [CustomMarshaller(typeof(USER_INFO_1), MarshalMode.Default, typeof(Marshaller))]
            public static class Marshaller
            {
                public static USER_INFO_1Native ConvertToUnmanaged(USER_INFO_1 managed) => new(managed);
                public static USER_INFO_1 ConvertToManaged(USER_INFO_1Native native) => native.ToManaged();
                public static void Free(USER_INFO_1Native native) => native.FreeNative();

                [StructLayout(LayoutKind.Sequential)]
                public struct USER_INFO_1Native
                {
                    private IntPtr usri1_name;
                    private IntPtr usri1_password;
                    private uint usri1_password_age;
                    private uint usri1_priv;
                    private IntPtr usri1_home_dir;
                    private IntPtr usri1_comment;
                    private uint usri1_flags;
                    private IntPtr usri1_script_path;

                    public USER_INFO_1Native(USER_INFO_1 managed)
                    {
                        usri1_name = Marshal.StringToCoTaskMemUni(managed.usri1_name);
                        usri1_password = Marshal.StringToCoTaskMemUni(managed.usri1_password);
                        usri1_password_age = managed.usri1_password_age;
                        usri1_priv = managed.usri1_priv;
                        usri1_home_dir = Marshal.StringToCoTaskMemUni(managed.usri1_home_dir);
                        usri1_comment = Marshal.StringToCoTaskMemUni(managed.usri1_comment);
                        usri1_flags = managed.usri1_flags;
                        usri1_script_path = Marshal.StringToCoTaskMemUni(managed.usri1_script_path);
                    }

                    public USER_INFO_1 ToManaged()
                    {
                        return new USER_INFO_1
                        {
                            usri1_name = Marshal.PtrToStringUni(usri1_name),
                            usri1_password = Marshal.PtrToStringUni(usri1_password),
                            usri1_password_age = usri1_password_age,
                            usri1_priv = usri1_priv,
                            usri1_home_dir = Marshal.PtrToStringUni(usri1_home_dir),
                            usri1_comment = Marshal.PtrToStringUni(usri1_comment),
                            usri1_flags = usri1_flags,
                            usri1_script_path = Marshal.PtrToStringUni(usri1_script_path)
                        };
                    }

                    public void FreeNative()
                    {
                        Marshal.FreeCoTaskMem(usri1_name);
                        Marshal.FreeCoTaskMem(usri1_password);
                        Marshal.FreeCoTaskMem(usri1_home_dir);
                        Marshal.FreeCoTaskMem(usri1_comment);
                        Marshal.FreeCoTaskMem(usri1_script_path);
                    }
                }
            }
#endif
        }

        public void Dispose()
        {
            _accountTokenHandle?.Dispose();

            uint result = NetUserDel(null, _userName);

            // 2221= NERR_UserNotFound
            if (result != 0 && result != 2221)
            {
                throw new Win32Exception((int)result);
            }
        }
    }
}

