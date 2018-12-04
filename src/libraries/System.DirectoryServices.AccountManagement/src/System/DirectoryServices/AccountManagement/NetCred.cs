// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;

namespace System.DirectoryServices.AccountManagement
{
    internal class NetCred
    {
        public NetCred(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public string UserName
        {
            get { return _username; }
        }

        public string Password
        {
            get { return _password; }
        }

        public string ParsedUserName
        {
            get
            {
                if (null == _parsedUserName)
                {
                    SplitUsername(_username, ref _parsedUserName, ref _domainname);
                }

                return _parsedUserName;
            }
        }

        public string Domain
        {
            get
            {
                if (null == _parsedUserName)
                {
                    SplitUsername(_username, ref _parsedUserName, ref _domainname);
                }

                return _domainname;
            }
        }

        private unsafe void SplitUsername(string username, ref string parsedUserName, ref string parsedDomainName)
        {
            // If the user has passed null creds then parsed components should also be null.
            if (username == null)
            {
                parsedDomainName = null;
                parsedUserName = null;
                return;
            }

            // Logon user expects the username in UPN or to have the username and domain split to the separate parameters.
            // It does not work properly with NT4 style name formats.  This function will put the username in the proper format.
            fixed (char* splitUsername = new char[UnsafeNativeMethods.CRED_MAX_USERNAME_LENGTH])
            fixed (char* splitDomain = new char[UnsafeNativeMethods.CRED_MAX_DOMAIN_TARGET_LENGTH])
            {
                int result = UnsafeNativeMethods.CredUIParseUserName(
                                                     username,
                                                     splitUsername,
                                                     UnsafeNativeMethods.CRED_MAX_USERNAME_LENGTH,
                                                     splitDomain,
                                                     UnsafeNativeMethods.CRED_MAX_DOMAIN_TARGET_LENGTH);

                // If CredUiParseUsername fails then username format must have been in a format it does not expect.
                // Just pass then entire username as the user passed it with a null domain string.
                if (result != 0)
                {
                    parsedDomainName = null;
                    parsedUserName = username;
                }
                else
                {
                    parsedDomainName = new string(splitDomain);
                    parsedUserName = new string(splitUsername);
                }
            }
        }

        //
        private string _username = null;
        private string _password = null;
        private string _domainname = null;
        private string _parsedUserName = null;
    }
}

