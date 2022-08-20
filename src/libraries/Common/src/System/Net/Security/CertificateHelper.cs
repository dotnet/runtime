// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal static partial class CertificateHelper
    {
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";

        internal static X509Certificate2? GetEligibleClientCertificate(X509CertificateCollection? candidateCerts)
        {
            if (candidateCerts == null || candidateCerts.Count == 0)
            {
                return null;
            }

            var certs = new X509Certificate2Collection();
            certs.AddRange(candidateCerts);

            return GetEligibleClientCertificate(certs);
        }

        internal static X509Certificate2? GetEligibleClientCertificate(X509Certificate2Collection? candidateCerts)
        {
            if (candidateCerts == null || candidateCerts.Count == 0)
            {
                return null;
            }

            foreach (X509Certificate2 cert in candidateCerts)
            {
                if (!cert.HasPrivateKey)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(candidateCerts, $"Skipping current X509Certificate2 {cert.GetHashCode()} since it doesn't have private key. Certificate Subject: {cert.Subject}, Thumbprint: {cert.Thumbprint}.");
                    }
                    continue;
                }

                if (IsValidClientCertificate(cert))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(candidateCerts, $"Choosing X509Certificate2 {cert.GetHashCode()} as the Client Certificate. Certificate Subject: {cert.Subject}, Thumbprint: {cert.Thumbprint}.");
                    }
                    return cert;
                }
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(candidateCerts, "No eligible client certificate found.");
            }
            return null;
        }

        private static bool IsValidClientCertificate(X509Certificate2 cert)
        {
            foreach (X509Extension extension in cert.Extensions)
            {
                if ((extension is X509EnhancedKeyUsageExtension eku) && !IsValidForClientAuthenticationEKU(eku))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(cert, $"For Certificate {cert.GetHashCode()} - current X509EnhancedKeyUsageExtension {eku.GetHashCode()} is not valid for Client Authentication.");
                    }
                    return false;
                }
                else if ((extension is X509KeyUsageExtension ku) && !IsValidForDigitalSignatureUsage(ku))
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(cert, $"For Certificate {cert.GetHashCode()} - current X509KeyUsageExtension {ku.GetHashCode()} is not valid for Digital Signature.");
                    }
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidForClientAuthenticationEKU(X509EnhancedKeyUsageExtension eku)
        {
            foreach (Oid oid in eku.EnhancedKeyUsages)
            {
                if (oid.Value == ClientAuthenticationOID)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidForDigitalSignatureUsage(X509KeyUsageExtension ku)
        {
            const X509KeyUsageFlags RequiredUsages = X509KeyUsageFlags.DigitalSignature;
            return (ku.KeyUsages & RequiredUsages) == RequiredUsages;
        }
    }
}
