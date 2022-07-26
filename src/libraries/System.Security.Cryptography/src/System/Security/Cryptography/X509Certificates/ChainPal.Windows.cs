// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class ChainPal : IDisposable, IChainPal
    {
        private SafeX509ChainHandle _chain;

        private ChainPal(SafeX509ChainHandle chain)
        {
            _chain = chain;
        }

        internal static partial IChainPal FromHandle(IntPtr chainContext)
        {
            if (chainContext == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(chainContext));
            }

            SafeX509ChainHandle certChainHandle = Interop.Crypt32.CertDuplicateCertificateChain(chainContext);
            if (certChainHandle == null || certChainHandle.IsInvalid)
            {
                certChainHandle?.Dispose();
                throw new CryptographicException(SR.Cryptography_InvalidContextHandle, nameof(chainContext));
            }

            var pal = new ChainPal(certChainHandle);
            return pal;
        }

        /// <summary>
        /// Does not throw on api error. Returns default(bool?) and sets "exception" instead.
        /// </summary>
        public bool? Verify(X509VerificationFlags flags, out Exception? exception)
        {
            exception = null;

            unsafe
            {
                Interop.Crypt32.CERT_CHAIN_POLICY_PARA para = default;
                para.cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_PARA);
                para.dwFlags = (uint)flags;

                Interop.Crypt32.CERT_CHAIN_POLICY_STATUS status = default;
                status.cbSize = (uint)sizeof(Interop.Crypt32.CERT_CHAIN_POLICY_STATUS);

                if (!Interop.crypt32.CertVerifyCertificateChainPolicy(ChainPolicy.CERT_CHAIN_POLICY_BASE, _chain, ref para, ref status))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    exception = errorCode.ToCryptographicException();
                    return default(bool?);
                }
                return status.dwError == 0;
            }
        }

        public X509ChainElement[] ChainElements
        {
            get
            {
                unsafe
                {
                    CERT_CHAIN_CONTEXT* pCertChainContext = (CERT_CHAIN_CONTEXT*)(_chain.DangerousGetHandle());
                    CERT_SIMPLE_CHAIN* pCertSimpleChain = pCertChainContext->rgpChain[0];

                    X509ChainElement[] chainElements = new X509ChainElement[pCertSimpleChain->cElement];
                    for (int i = 0; i < pCertSimpleChain->cElement; i++)
                    {
                        CERT_CHAIN_ELEMENT* pChainElement = pCertSimpleChain->rgpElement[i];

                        X509Certificate2 certificate = new X509Certificate2((IntPtr)(pChainElement->pCertContext));
                        X509ChainStatus[] chainElementStatus = GetChainStatusInformation(pChainElement->TrustStatus.dwErrorStatus);
                        string information = Marshal.PtrToStringUni(pChainElement->pwszExtendedErrorInfo)!;

                        X509ChainElement chainElement = new X509ChainElement(certificate, chainElementStatus, information);
                        chainElements[i] = chainElement;
                    }

                    GC.KeepAlive(this);
                    return chainElements;
                }
            }
        }

        public X509ChainStatus[] ChainStatus
        {
            get
            {
                unsafe
                {
                    CERT_CHAIN_CONTEXT* pCertChainContext = (CERT_CHAIN_CONTEXT*)(_chain.DangerousGetHandle());
                    X509ChainStatus[] chainStatus = GetChainStatusInformation(pCertChainContext->TrustStatus.dwErrorStatus);
                    GC.KeepAlive(this);
                    return chainStatus;
                }
            }
        }

        public SafeX509ChainHandle SafeHandle
        {
            get
            {
                return _chain;
            }
        }

        internal static partial bool ReleaseSafeX509ChainHandle(IntPtr handle)
        {
            Interop.Crypt32.CertFreeCertificateChain(handle);
            return true;
        }

        public void Dispose()
        {
            SafeX509ChainHandle? chain = _chain;
            if (chain != null)
            {
                _chain = null!;
                chain.Dispose();
            }
        }
    }
}
