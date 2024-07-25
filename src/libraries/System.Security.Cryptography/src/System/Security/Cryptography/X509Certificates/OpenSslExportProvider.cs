// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslExportProvider : UnixExportProvider
    {
        internal OpenSslExportProvider(ICertificatePalCore singleCertPal)
            : base(singleCertPal)
        {
        }

        internal OpenSslExportProvider(X509Certificate2Collection certs)
            : base(certs)
        {
        }

        protected override byte[] ExportPkcs8(
            ICertificatePalCore certificatePal,
            ReadOnlySpan<char> password)
        {
            SafeEvpPKeyHandle? privateKey = ((OpenSslX509CertificateReader)certificatePal).PrivateKeyHandle;

            if (privateKey == null)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }

            Interop.Crypto.EvpAlgorithmId evpAlgId = Interop.Crypto.EvpPKeyType(privateKey);

            AsymmetricAlgorithm alg = evpAlgId switch
            {
                Interop.Crypto.EvpAlgorithmId.RSA => new RSAOpenSsl(privateKey),
                Interop.Crypto.EvpAlgorithmId.ECC => new ECDsaOpenSsl(privateKey),
                Interop.Crypto.EvpAlgorithmId.DSA => new DSAOpenSsl(privateKey),
                _ => throw new CryptographicException(SR.Cryptography_InvalidHandle),
            };

            return alg.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
        }

        private static void PushHandle(IntPtr certPtr, SafeX509StackHandle publicCerts)
        {
            using (SafeX509Handle certHandle = Interop.Crypto.X509UpRef(certPtr))
            {
                if (!Interop.Crypto.PushX509StackField(publicCerts, certHandle))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // The handle ownership has been transferred into the STACK_OF(X509).
                certHandle.SetHandleAsInvalid();
            }
        }

        protected override byte[] ExportPkcs7()
        {
            // Pack all of the certificates into a new PKCS7*, export it to a byte[],
            // then free the PKCS7*, since we don't need it any more.
            using (SafeX509StackHandle certs = Interop.Crypto.NewX509Stack())
            {
                foreach (X509Certificate2 cert in _certs!)
                {
                    PushHandle(cert.Handle, certs);
                    GC.KeepAlive(cert); // ensure cert's safe handle isn't finalized while raw handle is in use
                }

                using (SafePkcs7Handle pkcs7 = Interop.Crypto.Pkcs7CreateCertificateCollection(certs))
                {
                    Interop.Crypto.CheckValidOpenSslHandle(pkcs7);
                    return Interop.Crypto.OpenSslEncode(
                        Interop.Crypto.GetPkcs7DerSize,
                        Interop.Crypto.EncodePkcs7,
                        pkcs7);
                }
            }
        }
    }
}
