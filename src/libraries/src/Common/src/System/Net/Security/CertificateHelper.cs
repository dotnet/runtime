// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal static partial class CertificateHelper
    {
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";

        internal static X509Certificate2 GetEligibleClientCertificate(X509CertificateCollection candidateCerts)
        {
            if (candidateCerts.Count == 0)
            {
                return null;
            }

            var certs = new X509Certificate2Collection();
            certs.AddRange(candidateCerts);

            return GetEligibleClientCertificate(certs);
        }

        internal static X509Certificate2 GetEligibleClientCertificate(X509Certificate2Collection candidateCerts)
        {
            if (candidateCerts.Count == 0)
            {
                return null;
            }

            // Build a new collection with certs that have a private key. We need to do this manually because there is
            // no X509FindType to match this criteria.
            // Find(...) returns a collection of clones instead of a filtered collection, so do this before calling
            // Find(...) to minimize the number of unnecessary allocations and finalizations.
            var eligibleCerts = new X509Certificate2Collection();
            foreach (X509Certificate2 cert in candidateCerts)
            {
                if (cert.HasPrivateKey)
                {
                    eligibleCerts.Add(cert);
                }
            }

            // Don't call Find(...) if we don't need to.
            if (eligibleCerts.Count == 0)
            {
                return null;
            }

            // Reduce the set of certificates to match the proper 'Client Authentication' criteria.
            // Client EKU is probably more rare than the DigitalSignature KU. Filter by ClientAuthOid first to reduce
            // the candidate space as quickly as possible.
            eligibleCerts = eligibleCerts.Find(X509FindType.FindByApplicationPolicy, ClientAuthenticationOID, false);
            eligibleCerts = eligibleCerts.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);

            if (eligibleCerts.Count > 0)
            {
                return eligibleCerts[0];
            }
            else
            {
                return null;
            }
        }
    }
}
