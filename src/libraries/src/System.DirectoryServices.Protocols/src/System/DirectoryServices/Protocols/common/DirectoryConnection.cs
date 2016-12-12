// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;

    public abstract class DirectoryConnection
    {
        //
        // Private/protected
        //
        internal NetworkCredential directoryCredential = null;
        internal X509CertificateCollection certificatesCollection = null;
        internal TimeSpan connectionTimeOut = new TimeSpan(0, 0, 30);
        internal DirectoryIdentifier directoryIdentifier = null;

        protected DirectoryConnection()
        {
            Utility.CheckOSVersion();

            certificatesCollection = new X509CertificateCollection();
        }

        public virtual DirectoryIdentifier Directory
        {
            get
            {
                return directoryIdentifier;
            }
        }

        public X509CertificateCollection ClientCertificates
        {
            get
            {
                return certificatesCollection;
            }
        }

        public virtual TimeSpan Timeout
        {
            get
            {
                return connectionTimeOut;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(Res.GetString(Res.NoNegativeTime), "value");
                }

                connectionTimeOut = value;
            }
        }

        public virtual NetworkCredential Credential
        {
            [
                DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true),
                EnvironmentPermission(SecurityAction.Assert, Unrestricted = true),
                SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)
            ]
            set
            {
                directoryCredential = (value != null) ? new NetworkCredential(value.UserName, value.Password, value.Domain) : null;
            }
        }

        [
            DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public abstract DirectoryResponse SendRequest(DirectoryRequest request);

        internal NetworkCredential GetCredential()
        {
            return directoryCredential;
        }
    }
}
