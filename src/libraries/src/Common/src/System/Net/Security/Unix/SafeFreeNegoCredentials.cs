// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal sealed class SafeFreeNegoCredentials : SafeFreeCredentials
    {
        private SafeGssCredHandle _credential;
        private readonly bool _isNtlmOnly;
        private readonly string _userName;
        private readonly bool _isDefault;

        public SafeGssCredHandle GssCredential
        {
            get { return _credential; }
        }

        // Property represents if Ntlm Protocol is specfied or not.
        public bool IsNtlmOnly
        {
            get { return _isNtlmOnly; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
        }

        public SafeFreeNegoCredentials(bool isNtlmOnly, string username, string password, string domain)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert(username != null && password != null, "Username and Password can not be null");
            const char At = '@';
            const char Backwhack = '\\';

            // any invalid user format will not be mnipulated and passed as it is.
            int index = username.IndexOf(Backwhack);
            if (index > 0 && username.IndexOf(Backwhack, index + 1) < 0 && string.IsNullOrEmpty(domain))
            {
                domain = username.Substring(0, index);
                username = username.Substring(index + 1);
            }

            // remove any leading and trailing whitespace
            if (domain != null)
            {
                domain = domain.Trim();
            }

            username = username.Trim();

            if ((username.IndexOf(At) < 0) && !string.IsNullOrEmpty(domain))
            {
                username += At + domain;
            }

            bool ignore = false;
            _isNtlmOnly = isNtlmOnly;
            _userName = username;
            _isDefault = string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password);
            _credential = SafeGssCredHandle.Create(username, password, isNtlmOnly);
            _credential.DangerousAddRef(ref ignore);
        }

        public override bool IsInvalid
        {
            get { return (null == _credential); }
        }

        protected override bool ReleaseHandle()
        {
            _credential.DangerousRelease();
            _credential = null;
            return true;
        }
    }
}
