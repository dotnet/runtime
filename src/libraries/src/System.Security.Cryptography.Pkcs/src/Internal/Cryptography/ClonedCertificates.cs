// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography
{
    internal sealed class ClonedCertificates : IDisposable
    {
        /// <summary>
        /// Invokes X509Certificate2Collection.Find() and creates a ClonedCertificate object that will dispose the returned certificates. This 
        /// is a way of coping with the non-obvious fact that X509Certificate2Collection.Find() returns cloned certificates. 
        /// </summary>
        public ClonedCertificates(X509Certificate2Collection certs, X509FindType findType, object findValue)
        {
            _clonedCerts = certs.Find(findType, findValue, validOnly: false);
        }

        /// <summary>
        /// Chains another X509Certificate2Collection.Find() call on the contents of this instance, and returns
        /// a freshly allocated ClonedCertificates object containing the narrowed results.
        /// </summary>
        public ClonedCertificates FindCertificates(X509FindType findType, object findValue)
        {
            return new ClonedCertificates(_clonedCerts, findType, findValue);
        }

        /// <summary>
        /// Returns true if this instance contains at least one certificate.
        /// </summary>
        public bool Any
        {
            get
            {
                return _clonedCerts.Count != 0;
            }
        }

        /// <summary>
        /// Returns one result from the contents of this instance. Whichever one we returned will no longer be disposed
        /// by the ClonedCertificates object. It becomes the caller's responsibility to do so.
        /// </summary>
        public X509Certificate2 ClaimResult()
        {
            Debug.Assert(_clonedCerts.Count != 0);

            X509Certificate2 cert = _clonedCerts[0];
            _clonedCerts.RemoveAt(0);
            return cert;
        }

        public void Dispose()
        {
            if (_clonedCerts != null)
            {
                foreach (X509Certificate2 cert in _clonedCerts)
                {
                    cert.Dispose();
                }
                _clonedCerts = null;
            }
        }

        private X509Certificate2Collection _clonedCerts;
    }

    internal static class ClonedCertificatesFactory
    {
        /// <summary>
        /// Invokes X509Certificate2Collection.Find() and returns a ClonedCertificate object that will dispose the returned certificates. This 
        /// is a way of coping with the non-obvious fact that X509Certificate2Collection.Find() returns cloned certificates. 
        /// </summary>
        public static ClonedCertificates FindCertificates(this X509Certificate2Collection certs, X509FindType findType, object findValue)
        {
            return new ClonedCertificates(certs, findType, findValue);
        }
    }
}
