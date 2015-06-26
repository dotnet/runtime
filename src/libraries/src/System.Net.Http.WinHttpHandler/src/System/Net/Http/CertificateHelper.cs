// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    // TODO: Move/rename to Common/System/Net as part of System.Private.Networking.dll refactor.
    internal static class CertificateHelper
    {
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
            chain.Build(certificate);
            if (chain.ChainStatus != null && chain.ChainStatus.Length != 0)
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
                eppStruct.dwAuthType = Interop.Crypt32.AuthType.AUTHTYPE_CLIENT;
                
                cppStruct.pvExtraPolicyPara = &eppStruct;

                fixed (char* namePtr = hostName)
                {
                    eppStruct.pwszServerName = namePtr;
                    cppStruct.dwFlags = 
                        Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_ALL &
                        ~Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_INVALID_NAME_FLAG;
                        
                    var status = new Interop.Crypt32.CERT_CHAIN_POLICY_STATUS();
                    status.cbSize = (uint)Marshal.SizeOf<Interop.Crypt32.CERT_CHAIN_POLICY_STATUS>();
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
                        // TODO: Log this error.
                        sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                    }
                }
            }
        }
    }
}
