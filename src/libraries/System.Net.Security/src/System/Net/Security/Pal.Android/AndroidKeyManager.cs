// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using PAL_KeyAlgorithm = Interop.AndroidCrypto.PAL_KeyAlgorithm;

namespace System.Net
{
    internal static class AndroidKeyManager
    {
        internal static IntPtr Create(SslStreamCertificateContext context)
        {
            X509Certificate2 cert = context.TargetCertificate;
            Debug.Assert(cert.HasPrivateKey);

            IntPtr keyManagers;
            if (Interop.AndroidCrypto.IsKeyStorePrivateKeyEntry(cert.Handle))
            {
                keyManagers = Interop.AndroidCrypto.SSLStreamCreateKeyManagersFromKeyStoreEntry(cert.Handle);
            }
            else
            {
                PAL_KeyAlgorithm algorithm;
                byte[] keyBytes;
                using (AsymmetricAlgorithm key = GetPrivateKeyAlgorithm(cert, out algorithm))
                {
                    keyBytes = key.ExportPkcs8PrivateKey();
                }

                try
                {
                    IntPtr[] ptrs = new IntPtr[context.IntermediateCertificates.Count + 1];
                    ptrs[0] = cert.Handle;
                    for (int i = 0; i < context.IntermediateCertificates.Count; i++)
                    {
                        ptrs[i + 1] = context.IntermediateCertificates[i].Handle;
                    }

                    keyManagers = Interop.AndroidCrypto.SSLStreamCreateKeyManagers(keyBytes, algorithm, ptrs);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBytes);
                }
            }

            if (keyManagers == IntPtr.Zero)
            {
                throw new Interop.AndroidCrypto.SslException();
            }

            return keyManagers;
        }

        private static AsymmetricAlgorithm GetPrivateKeyAlgorithm(X509Certificate2 cert, out PAL_KeyAlgorithm algorithm)
        {
            AsymmetricAlgorithm? key = cert.GetRSAPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.RSA;
                return key;
            }

            key = cert.GetECDsaPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.EC;
                return key;
            }

            key = cert.GetDSAPrivateKey();
            if (key != null)
            {
                algorithm = PAL_KeyAlgorithm.DSA;
                return key;
            }

            throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
        }
    }
}
