// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace System.Net
{
    internal static partial class CertificateValidation
    {
        internal static SslPolicyErrors BuildChainAndVerifyProperties(X509Chain chain, X509Certificate2 remoteCertificate, bool checkCertName, bool isServer, string? hostName)
        {
            SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;

            bool chainBuildResult = chain.Build(remoteCertificate);
            if (!chainBuildResult       // Build failed on handle or on policy.
                && chain.SafeHandle!.DangerousGetHandle() == IntPtr.Zero)   // Build failed to generate a valid handle.
            {
                throw new CryptographicException(Marshal.GetLastPInvokeError());
            }

            if (checkCertName)
            {
                unsafe
                {
                    uint status = 0;

                    var eppStruct = new Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)sizeof(Interop.Crypt32.SSL_EXTRA_CERT_CHAIN_POLICY_PARA),
                        // Authenticate the remote party: (e.g. when operating in server mode, authenticate the client).
                        dwAuthType = isServer ? Interop.Crypt32.AuthType.AUTHTYPE_CLIENT : Interop.Crypt32.AuthType.AUTHTYPE_SERVER,
                        fdwChecks = 0,
                        pwszServerName = null
                    };

                    var cppStruct = new Interop.Crypt32.CERT_CHAIN_POLICY_PARA()
                    {
                        cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_PARA),
                        dwFlags = 0,
                        pvExtraPolicyPara = &eppStruct
                    };

                    fixed (char* namePtr = hostName)
                    {
                        eppStruct.pwszServerName = namePtr;
                        cppStruct.dwFlags |=
                            (Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_ALL &
                             ~Interop.Crypt32.CertChainPolicyIgnoreFlags.CERT_CHAIN_POLICY_IGNORE_INVALID_NAME_FLAG);

                        SafeX509ChainHandle chainContext = chain.SafeHandle!;
                        status = Verify(chainContext, ref cppStruct);
                        if (status == Interop.Crypt32.CertChainPolicyErrors.CERT_E_CN_NO_MATCH)
                        {
                            sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNameMismatch;
                        }
                    }
                }
            }

            if (!chainBuildResult)
            {
                sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return sslPolicyErrors;
        }

        private static unsafe uint Verify(SafeX509ChainHandle chainContext, ref Interop.Crypt32.CERT_CHAIN_POLICY_PARA cpp)
        {
            Interop.Crypt32.CERT_CHAIN_POLICY_STATUS status = default;
            status.cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);

            bool errorCode =
                Interop.Crypt32.CertVerifyCertificateChainPolicy(
                    (IntPtr)Interop.Crypt32.CertChainPolicy.CERT_CHAIN_POLICY_SSL,
                    chainContext,
                    ref cpp,
                    ref status);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(chainContext, $"CertVerifyCertificateChainPolicy returned: {errorCode}. Status: {status.dwError}");
            return status.dwError;
        }
    }
}
