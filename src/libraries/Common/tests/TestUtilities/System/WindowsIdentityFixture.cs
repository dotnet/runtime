// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
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

        public WindowsTestAccount(string userName)
        {
            Assert.True(PlatformDetection.IsWindowsAndElevated);

            _userName = userName;
            CreateUser();
        }

        private void CreateUser()
        {
            string testAccountPassword;
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[33];
                rng.GetBytes(randomBytes);

                // Add special chars to ensure it satisfies password requirements.
                testAccountPassword = Convert.ToBase64String(randomBytes) + "_-As@!%*(1)4#2";

                USER_INFO_1 userInfo = new USER_INFO_1
                {
                    usri1_name = _userName,
                    usri1_password = testAccountPassword,
                    usri1_priv = 1
                };

                // Create user and remove/create if already exists
                uint result = NetUserAdd(null, 1, ref userInfo, out uint param_err);

                // error codes https://docs.microsoft.com/en-us/windows/desktop/netmgmt/network-management-error-codes
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

                if (!LogonUser(_userName, ".", testAccountPassword, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out _accountTokenHandle))
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
        }

        [LibraryImport("advapi32.dll", EntryPoint = "LogonUserW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool LogonUser(string userName, string domain, string password, int logonType, int logonProvider, out SafeAccessTokenHandle safeAccessTokenHandle);

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [LibraryImportGenerator] Switch to use LibraryImport once we add support for non-blittable struct marshalling.
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint NetUserAdd([MarshalAs(UnmanagedType.LPWStr)]string servername, uint level, ref USER_INFO_1 buf, out uint parm_err);
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [LibraryImport("netapi32.dll")]
        internal static partial uint NetUserDel([MarshalAs(UnmanagedType.LPWStr)]string servername, [MarshalAs(UnmanagedType.LPWStr)]string username);

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

