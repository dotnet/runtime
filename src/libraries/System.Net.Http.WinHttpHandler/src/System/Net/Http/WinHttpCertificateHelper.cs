// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    internal static class WinHttpCertificateHelper
    {
        private const string ClientAuthenticationOID = "1.3.6.1.5.5.7.3.2";
        private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");
        
        // TODO: Issue #2165. Merge with similar code used in System.Net.Security move to Common/src//System/Net.
        public static void BuildChain(
            X509Certificate2 certificate,
            string hostName,
            bool checkCertificateRevocationList,
            out X509Chain chain,
            out SslPolicyErrors sslPolicyErrors)
        {
            chain = null;
            sslPolicyErrors = SslPolicyErrors.None;

            // Build the chain.
            chain = new X509Chain();
            chain.ChainPolicy.RevocationMode =
                checkCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            // Authenticate the remote party: (e.g. when operating in client mode, authenticate the server).
            chain.ChainPolicy.ApplicationPolicy.Add(s_serverAuthOid);

            if (!chain.Build(certificate))
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            // Verify the hostName matches the certificate.
            unsafe
            {
                var cppStruct = new Interop.Crypt32.CERT_CHAIN_POLICY_PARA();
                cppStruct.cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_POLICY_PARA>();
                cppStruct.dwFlags = 0;

                var eppStruct = new Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA();
                eppStruct.cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA>();
                eppStruct.dwAuthType = Interop.Crypt32.AuthType.AUTHTYPE_SERVER;
                
                cppStruct.pvExtraPolicyPara = &eppStruct;

                fixed (char* namePtr = hostName)
                {
                    eppStruct.pwszServerName = namePtr;
                    cppStruct.dwFlags = 
                        Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_ALL &
                        ~Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_INVALID_NAME_FLAG;
                        
                    var status = new Interop.Crypt32.CERT_CHAIN_POLICY_STATUS();
                    status.cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);
                    if (Interop.Crypt32.CertVerifyCertificateChainPolicy(
                            (IntPtr)Interop.Crypt32.CertChainPolicy.CERT_CHAIN_POLICY_SSL,
                            chain.SafeHandle,
                            ref cppStruct,
                            ref status))
                    {
                        if (status.dwError == Interop.Crypt32.CertChainPolicyErrors.CERT_E_CN_NO_MATCH)
                        {
                            sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                        }
                    }
                    else
                    {
                        // Failure checking the policy. This is a rare error. We will assume the name check failed.
                        // TODO: Issue #2165. Log this error or perhaps throw an exception instead.
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                    }
                }
            }
        }

        public static X509Certificate2 GetEligibleClientCertificate()
        {
            // Get initial list of client certificates from the MY store.
            X509Certificate2Collection candidateCerts;
            using (var myStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                myStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                candidateCerts = myStore.Certificates;
            }
            
            return GetEligibleClientCertificate(candidateCerts);
        }
        
        // TODO: Issue #3891. Get the Trusted Issuers List from WinHTTP and use that to help narrow down
        // the list of eligible client certificates.
        public static X509Certificate2 GetEligibleClientCertificate(X509Certificate2Collection candidateCerts)
        {
            if (candidateCerts.Count == 0)
            {
                return null;
            }

            // Reduce the set of certificates to match the proper 'Client Authentication' criteria.
            candidateCerts = candidateCerts.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);
            candidateCerts = candidateCerts.Find(X509FindType.FindByApplicationPolicy, ClientAuthenticationOID, false);

            // Build a new collection with certs that have a private key. Need to do this
            // manually because there is no X509FindType to match this criteria.
            var eligibleCerts = new X509Certificate2Collection();
            foreach (X509Certificate2 cert in candidateCerts)
            {
                if (cert.HasPrivateKey)
                {
                    eligibleCerts.Add(cert);
                }
            }
            
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
