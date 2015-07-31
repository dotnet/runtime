// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;

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
        private string _password;

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
        ///       class with name and password set as specified.
        ///    </para>
        /// </devdoc>
        public NetworkCredential(string userName, string password, string domain)
        {
            UserName = userName;
            Password = password;
            Domain = domain;
        }

        /// <devdoc>
        ///    <para>
        ///       The user name associated with this credential.
        ///    </para>
        /// </devdoc>
        public string UserName
        {
            get
            {
                return InternalGetUserName();
            }
            set
            {
                _userName = value ?? string.Empty;
            }
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
                return InternalGetPassword();
            }
            set
            {
                _password = value;
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
            get
            {
                return InternalGetDomain();
            }
            set
            {
                _domain = value ?? string.Empty;
            }
        }

        internal string InternalGetUserName()
        {
            return _userName;
        }

        internal string InternalGetPassword()
        {
            return _password;
        }

        internal string InternalGetDomain()
        {
            return _domain;
        }

        internal string InternalGetDomainUserName()
        {
            string domain = InternalGetDomain();
            string userName = InternalGetUserName();
            return domain != "" ? domain + "\\" + userName : userName;
        }

        /// <devdoc>
        ///    <para>
        ///       Returns an instance of the NetworkCredential class for a Uri and
        ///       authentication type.
        ///    </para>
        /// </devdoc>
        public NetworkCredential GetCredential(Uri uri, String authType)
        {
            return this;
        }

        public NetworkCredential GetCredential(string host, int port, String authenticationType)
        {
            return this;
        }

#if DEBUG
        // This method is only called as part of an assert
        internal bool IsEqualTo(object compObject)
        {
            if ((object)compObject == null)
            {
                return false;
            }

            if ((object)this == (object)compObject)
            {
                return true;
            }

            NetworkCredential compCred = compObject as NetworkCredential;
            if ((object)compCred == null)
            {
                return false;
            }

            return (InternalGetUserName() == compCred.InternalGetUserName() &&
                    InternalGetDomain() == compCred.InternalGetDomain() &&
                    string.Equals(_password, compCred._password, StringComparison.Ordinal));
        }
#endif
    }
}
