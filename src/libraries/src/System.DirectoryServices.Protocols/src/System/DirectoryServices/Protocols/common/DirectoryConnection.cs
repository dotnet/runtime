// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.Protocols
{
    public abstract class DirectoryConnection
    {
        internal NetworkCredential _directoryCredential;
        private X509CertificateCollection _certificatesCollection;
        internal TimeSpan _connectionTimeOut = new TimeSpan(0, 0, 30);
        internal DirectoryIdentifier _directoryIdentifier;

        protected DirectoryConnection() => _certificatesCollection = new X509CertificateCollection();

        public virtual DirectoryIdentifier Directory => _directoryIdentifier;

        public X509CertificateCollection ClientCertificates => _certificatesCollection;

        public virtual TimeSpan Timeout
        {
            get => _connectionTimeOut;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.NoNegativeTimeLimit, nameof(value));
                }

                _connectionTimeOut = value;
            }
        }

        public virtual NetworkCredential Credential
        {
            set
            {
                _directoryCredential = (value != null) ? new NetworkCredential(value.UserName, value.Password, value.Domain) : null;
            }
        }

        public abstract DirectoryResponse SendRequest(DirectoryRequest request);

        internal NetworkCredential GetCredential() => _directoryCredential;
    }
}
