// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;
using SafeNwHandle = Interop.SafeNwHandle;

namespace System.Net
{
    internal static partial class CertificateValidationPal
    {
        internal static SslPolicyErrors VerifyCertificateProperties(
            SafeDeleteContext securityContext,
            X509Chain chain,
            X509Certificate2? remoteCertificate,
            bool checkCertName,
            bool isServer,
            string? hostName)
        {
            SslPolicyErrors errors = SslPolicyErrors.None;

            if (remoteCertificate == null)
            {
                errors |= SslPolicyErrors.RemoteCertificateNotAvailable;
            }
            else
            {
                if (!chain.Build(remoteCertificate))
                {
                    errors |= SslPolicyErrors.RemoteCertificateChainErrors;
                }

                if (!isServer && checkCertName)
                {
                    switch (securityContext)
                    {
                        case SafeDeleteSslContext sslContext:
                            if (!Interop.AppleCrypto.SslCheckHostnameMatch(sslContext.SslContext, hostName!, remoteCertificate.NotBefore, out int osStatus))
                            {
                                errors |= SslPolicyErrors.RemoteCertificateNameMismatch;

                                if (NetEventSource.Log.IsEnabled())
                                    NetEventSource.Error(sslContext, $"Cert name validation for '{hostName}' failed with status '{osStatus}'");
                            }
                            break;

                        case SafeDeleteNwContext nwContext:
                            if (!ValidateHostname(hostName!, remoteCertificate))
                            {
                                errors |= SslPolicyErrors.RemoteCertificateNameMismatch;

                                if (NetEventSource.Log.IsEnabled())
                                    NetEventSource.Error(nwContext, $"Hostname validation for '{hostName}' failed - certificate does not match hostname");
                            }
                            break;

                        default:
                            throw new ArgumentException("Invalid context type", nameof(securityContext));
                    }
                }
            }

            return errors;
        }

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteContext? securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain,
            X509ChainPolicy? chainPolicy)
        {
            if (securityContext == null)
            {
                return null;
            }

            return securityContext switch
            {
                SafeDeleteNwContext nwContext => GetRemoteCertificate(nwContext, retrieveChainCertificates, ref chain, chainPolicy),
                SafeDeleteSslContext sslContext => GetRemoteCertificate(sslContext, retrieveChainCertificates, ref chain, chainPolicy),
                _ => throw new ArgumentException("Invalid context type", nameof(securityContext))
            };
        }

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteSslContext securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain,
            X509ChainPolicy? chainPolicy)
        {
            SafeSslHandle sslContext = securityContext.SslContext;

            if (sslContext == null)
            {
                return null;
            }

            X509Certificate2? result = null;

            using (SafeX509ChainHandle chainHandle = Interop.AppleCrypto.SslCopyCertChain(sslContext))
            {
                long chainSize = Interop.AppleCrypto.X509ChainGetChainSize(chainHandle);

                if (retrieveChainCertificates && chainSize > 1)
                {
                    chain ??= new X509Chain();
                    if (chainPolicy != null)
                    {
                        chain.ChainPolicy = chainPolicy;
                    }

                    // First certificate is peer's certificate.
                    // Any any additional intermediate CAs to ExtraStore.
                    for (int i = 1; i < chainSize; i++)
                    {
                        IntPtr certHandle = Interop.AppleCrypto.X509ChainGetCertificateAtIndex(chainHandle, i);
                        chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(certHandle));
                    }
                }

                // This will be a distinct object than remoteCertificateStore[0] (if applicable),
                // to match what the Windows and Unix PALs do.
                if (chainSize > 0)
                {
                    IntPtr certHandle = Interop.AppleCrypto.X509ChainGetCertificateAtIndex(chainHandle, 0);
                    result = new X509Certificate2(certHandle);
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.RemoteCertificate(result);

            return result;
        }

        private static X509Certificate2? GetRemoteCertificate(
            SafeDeleteNwContext securityContext,
            bool retrieveChainCertificates,
            ref X509Chain? chain,
            X509ChainPolicy? chainPolicy)
        {
            SafeNwHandle nwContext = securityContext.ConnectionHandle;

            if (nwContext == null)
            {
                return null;
            }

            SafeCFArrayHandle certificates;
            X509Certificate2? result = null;

            Interop.NetworkFramework.Tls.CopyCertChain(nwContext, out certificates, out int chainSize);

            using SafeCFArrayHandle _ = certificates;

            if (retrieveChainCertificates && chainSize > 1)
            {
                chain ??= new X509Chain();
                if (chainPolicy != null)
                {
                    chain.ChainPolicy = chainPolicy;
                }

                // First certificate is peer's certificate.
                // Any any additional intermediate CAs to ExtraStore.
                for (int i = 1; i < chainSize; i++)
                {
                    IntPtr certHandle = Interop.CoreFoundation.CFArrayGetValueAtIndex(certificates, i);
                    chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(certHandle));
                }
            }

            if (chainSize > 0)
            {
                IntPtr certHandle = Interop.CoreFoundation.CFArrayGetValueAtIndex(certificates, 0);
                result = new X509Certificate2(certHandle);
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Log.RemoteCertificate(result);
            }

            return result;
        }

        // This is only called when we selected local client certificate.
        // We need to check if the server actually requested it during handshake.
        internal static bool IsLocalCertificateUsed(SafeFreeCredentials? _, SafeDeleteContext? context)
        {
            return context switch
            {
                SafeDeleteNwContext nwContext => IsLocalCertificateUsed(nwContext),
                SafeDeleteSslContext => true,
                _ => true
            };
        }

        private static bool IsLocalCertificateUsed(SafeDeleteNwContext nwContext)
        {
            // For Network Framework, we need to check if the server actually requested
            // a client certificate during the handshake.
            return nwContext.ClientCertificateRequested;
        }

        //
        // Used only by client SSL code, never returns null.
        //
        internal static string[] GetRequestCertificateAuthorities(SafeDeleteContext securityContext)
        {
            return securityContext switch
            {
                SafeDeleteNwContext nwContext => GetRequestCertificateAuthorities(nwContext),
                SafeDeleteSslContext sslContext => GetRequestCertificateAuthorities(sslContext),
                _ => throw new ArgumentException("Invalid context type", nameof(securityContext))
            };
        }

        private static string[] GetRequestCertificateAuthorities(SafeDeleteSslContext securityContext)
        {
            SafeSslHandle sslContext = securityContext.SslContext;

            if (sslContext == null)
            {
                return Array.Empty<string>();
            }

            using (SafeCFArrayHandle dnArray = Interop.AppleCrypto.SslCopyCADistinguishedNames(sslContext))
            {
                if (dnArray.IsInvalid)
                {
                    return Array.Empty<string>();
                }

                long size = Interop.CoreFoundation.CFArrayGetCount(dnArray);

                if (size == 0)
                {
                    return Array.Empty<string>();
                }

                string[] distinguishedNames = new string[size];

                for (int i = 0; i < size; i++)
                {
                    IntPtr element = Interop.CoreFoundation.CFArrayGetValueAtIndex(dnArray, i);

                    using (SafeCFDataHandle cfData = new SafeCFDataHandle(element, ownsHandle: false))
                    {
                        byte[] dnData = Interop.CoreFoundation.CFGetData(cfData);
                        X500DistinguishedName dn = new X500DistinguishedName(dnData);
                        distinguishedNames[i] = dn.Name;
                    }
                }

                return distinguishedNames;
            }
        }

        private static string[] GetRequestCertificateAuthorities(SafeDeleteNwContext securityContext)
        {
            // Network Framework doesn't expose CA distinguished names in the same way
            // This functionality is typically handled during the handshake process
            // Return empty array for now, but this could be extended if Network Framework
            // provides access to this information in the future
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(securityContext, "GetRequestCertificateAuthorities not implemented for Network Framework");

            return Array.Empty<string>();
        }

        private static X509Store OpenStore(StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            return store;
        }

        // TODO: replace by Interop.AppleCrypto.SslCheckHostnameMatch
        private static bool ValidateHostname(string hostName, X509Certificate2 certificate)
        {
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, $"ValidateHostname: checking '{hostName}' against certificate");

            // Check Subject Alternative Names (SAN) first - preferred method
            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension.Oid!.Value == "2.5.29.17") // Subject Alternative Name OID
                {
                    var san = extension as X509SubjectAlternativeNameExtension;
                    if (san != null)
                    {
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(null, "Found Subject Alternative Name extension, checking DNS names");

                        foreach (string dnsName in san.EnumerateDnsNames())
                        {
                            if (NetEventSource.Log.IsEnabled())
                                NetEventSource.Info(null, $"Checking hostname '{hostName}' against SAN DNS name '{dnsName}'");

                            if (MatchHostname(hostName, dnsName))
                            {
                                if (NetEventSource.Log.IsEnabled())
                                    NetEventSource.Info(null, $"Hostname '{hostName}' matches SAN DNS name '{dnsName}'");
                                return true;
                            }
                        }
                        // If SAN is present, don't fall back to CN (RFC 6125)
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(null, $"Hostname '{hostName}' does not match any SAN DNS names, not falling back to CN per RFC 6125");
                        return false;
                    }
                }
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, "No Subject Alternative Name extension found, checking Common Name");

            // Fall back to Common Name (CN) if no SAN extension
            string? commonName = GetCommonName(certificate.Subject);
            if (commonName != null)
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(null, $"Checking hostname '{hostName}' against Common Name '{commonName}'");

                bool matches = MatchHostname(hostName, commonName);
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(null, $"Hostname '{hostName}' {(matches ? "matches" : "does not match")} Common Name '{commonName}'");

                return matches;
            }
            else
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Error(null, "No Common Name found in certificate subject");
                return false;
            }
        }

        private static bool MatchHostname(string hostName, string certificateName)
        {
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, $"MatchHostname: comparing '{hostName}' with '{certificateName}'");

            // Exact match (case-insensitive)
            if (string.Equals(hostName, certificateName, StringComparison.OrdinalIgnoreCase))
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(null, $"Exact match found: '{hostName}' == '{certificateName}'");
                return true;
            }

            // Wildcard match (*.example.com matches foo.example.com but not foo.bar.example.com)
            if (certificateName.StartsWith("*.", StringComparison.OrdinalIgnoreCase))
            {
                if (NetEventSource.Log.IsEnabled())
                    NetEventSource.Info(null, $"Checking wildcard match for '{certificateName}'");

                string pattern = certificateName.Substring(2);
                int dotIndex = hostName.IndexOf('.');
                if (dotIndex > 0 && dotIndex < hostName.Length - 1)
                {
                    string hostDomain = hostName.Substring(dotIndex + 1);
                    bool wildcardMatch = string.Equals(hostDomain, pattern, StringComparison.OrdinalIgnoreCase);

                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Info(null, $"Wildcard check: '{hostDomain}' {(wildcardMatch ? "matches" : "does not match")} pattern '{pattern}'");

                    return wildcardMatch;
                }
                else if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(null, $"Hostname '{hostName}' cannot match wildcard '{certificateName}' - no domain part found");
                }
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, $"No match found between '{hostName}' and '{certificateName}'");

            return false;
        }

        private static string? GetCommonName(string subject)
        {
            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, $"GetCommonName: parsing subject '{subject}'");

            // Parse CN from subject string (e.g., "CN=example.com, O=Example Corp")
            string[] parts = subject.Split(',');
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                {
                    string commonName = trimmed.Substring(3);
                    if (NetEventSource.Log.IsEnabled())
                        NetEventSource.Info(null, $"Found Common Name: '{commonName}'");
                    return commonName;
                }
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Info(null, "No Common Name found in subject");
            return null;
        }
    }
}
