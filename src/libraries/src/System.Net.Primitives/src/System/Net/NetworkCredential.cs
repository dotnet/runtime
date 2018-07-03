// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       Provides credentials for password-based authentication schemes such as basic,
    ///       digest, NTLM and Kerberos.
    ///    </para>
    /// </devdoc>
    public class NetworkCredential : ICredentials, ICredentialsByHost
    {
        private string _domain;
        private string _userName;
        private object _password;

        public NetworkCredential()
            : this(string.Empty, string.Empty, string.Empty)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name and password set as specified.
        ///    </para>
        /// </devdoc>
        public NetworkCredential(string userName, string password)
        : this(userName, password, string.Empty)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Net.NetworkCredential'/>
        ///       class with name, password and domain set as specified.
        ///    </para>
        /// </devdoc>
        public NetworkCredential(string userName, string password, string domain)
        {
            UserName = userName;
            Password = password;
            Domain = domain;
        }

        [CLSCompliant(false)]
        public NetworkCredential(string userName, SecureString password)
        : this(userName, password, string.Empty)
        {
        }

        [CLSCompliant(false)]
        public NetworkCredential(string userName, SecureString password, string domain)
        {
            UserName = userName;
            SecurePassword = password;
            Domain = domain;
        }

        /// <devdoc>
        ///    <para>
        ///       The user name associated with this credential.
        ///    </para>
        /// </devdoc>
        public string UserName
        {
            get { return _userName; }
            set { _userName = value ?? string.Empty; }
        }

        /// <devdoc>
        ///    <para>
        ///       The password for the user name.
        ///    </para>
        /// </devdoc>
        public string Password
        {
            get
            {
                SecureString sstr = _password as SecureString;
                if (sstr != null)
                {
                    return MarshalToString(sstr);
                }
                return (string)_password ?? string.Empty;
            }
            set
            {
                SecureString old = _password as SecureString;
                _password = value;
                old?.Dispose();
            }
        }

        [CLSCompliant(false)]
        public SecureString SecurePassword
        {
            get
            {
                string str = _password as string;
                if (str != null)
                {
                    return MarshalToSecureString(str);
                }
                SecureString sstr = _password as SecureString;
                return sstr != null ? sstr.Copy() : new SecureString();
            } 
            set
            { 
                SecureString old = _password as SecureString;
                _password = value?.Copy();
                old?.Dispose();
            }
        }

        /// <devdoc>
        ///    <para>
        ///       The machine name that verifies
        ///       the credentials. Usually this is the host machine.
        ///    </para>
        /// </devdoc>
        public string Domain
        {
            get { return _domain; }
            set { _domain = value ?? string.Empty; }
        }

        /// <devdoc>
        ///    <para>
        ///       Returns an instance of the NetworkCredential class for a Uri and
        ///       authentication type.
        ///    </para>
        /// </devdoc>
        public NetworkCredential GetCredential(Uri uri, string authenticationType)
        {
            return this;
        }

        public NetworkCredential GetCredential(string host, int port, string authenticationType)
        {
            return this;
        }

        private string MarshalToString(SecureString sstr)
        {
            if (sstr == null || sstr.Length == 0)
            {
                return string.Empty;
            }

            IntPtr ptr = IntPtr.Zero;
            string result = string.Empty;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(sstr);
                result = Marshal.PtrToStringUni(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
            return result;
        }

        private unsafe SecureString MarshalToSecureString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return new SecureString();
            }

            fixed (char* ptr = str)
            {
                return new SecureString(ptr, str.Length);
            }
        }
    }
}
