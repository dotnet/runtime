// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal static class EndpointChannelBindingToken
    {
        internal static ChannelBinding? Build(SafeDeleteContext securityContext)
        {
            using (X509Certificate2? cert = CertificateValidationPal.GetRemoteCertificate(securityContext))
            {
                if (cert == null)
                    return null;

                SafeChannelBindingHandle bindingHandle = new SafeChannelBindingHandle(ChannelBindingKind.Endpoint);

                byte[] bindingHash = GetHashForChannelBinding(cert);
                bindingHandle.SetCertHash(bindingHash);

                return bindingHandle;
            }
        }

        private static byte[] GetHashForChannelBinding(X509Certificate2 cert)
        {
            Oid signatureAlgorithm = cert.SignatureAlgorithm;
            switch (signatureAlgorithm.Value)
            {
                // RFC 5929 4.1 says that MD5 and SHA1 both upgrade to SHA256 for cbt calculation
                case "1.2.840.113549.2.5": // MD5
                case "1.2.840.113549.1.1.4": // MD5RSA
                case "1.3.14.3.2.26": // SHA1
                case "1.2.840.10040.4.3": // SHA1DSA
                case "1.2.840.10045.4.1": // SHA1ECDSA
                case "1.2.840.113549.1.1.5": // SHA1RSA
                case "2.16.840.1.101.3.4.2.1": // SHA256
                case "1.2.840.10045.4.3.2": // SHA256ECDSA
                case "1.2.840.113549.1.1.11": // SHA256RSA
                    return SHA256.HashData(cert.RawDataMemory.Span);

                case "2.16.840.1.101.3.4.2.2": // SHA384
                case "1.2.840.10045.4.3.3": // SHA384ECDSA
                case "1.2.840.113549.1.1.12": // SHA384RSA
                    return SHA384.HashData(cert.RawDataMemory.Span);

                case "2.16.840.1.101.3.4.2.3": // SHA512
                case "1.2.840.10045.4.3.4": // SHA512ECDSA
                case "1.2.840.113549.1.1.13": // SHA512RSA
                    return SHA512.HashData(cert.RawDataMemory.Span);

                default:
                    throw new ArgumentException(signatureAlgorithm.Value);
            }
        }
    }
}
