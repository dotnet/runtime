// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly Interop.NetSecurityNative.PackageType _packageType;
        private readonly string _userName;
        private readonly bool _isDefault;

        public SafeGssCredHandle GssCredential
        {
            get { return _credential; }
        }

        // Property represents which protocol is specified (Negotiate, Ntlm or Kerberos).
        public Interop.NetSecurityNative.PackageType PackageType
        {
            get { return _packageType; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public bool IsDefault
        {
            get { return _isDefault; }
        }

        public SafeFreeNegoCredentials(Interop.NetSecurityNative.PackageType packageType, string username, string password, ReadOnlySpan<char> domain)
            : base(IntPtr.Zero, true)
        {
            Debug.Assert(username != null && password != null, "Username and Password can not be null");

            // any invalid user format will not be manipulated and passed as it is.
            int index = username.IndexOf('\\');
            if (index > 0 && username.IndexOf('\\', index + 1) < 0 && domain.IsEmpty)
            {
                domain = username.AsSpan(0, index);
                username = username.Substring(index + 1);
            }

            // remove any leading and trailing whitespace
            username = username.Trim();
            domain = domain.Trim();
            if (!username.Contains('@') && !domain.IsEmpty)
            {
                username = string.Concat(username, "@", domain);
            }

            bool ignore = false;
            _packageType = packageType;
            _userName = username;
            _isDefault = string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password);
            _credential = SafeGssCredHandle.Create(username, password, packageType);
            _credential.DangerousAddRef(ref ignore);
        }

        public override bool IsInvalid
        {
            get { return (null == _credential); }
        }

        protected override bool ReleaseHandle()
        {
            _credential.DangerousRelease();
            _credential = null!;
            return true;
        }
    }
}
