// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslPkcs12Reader : IDisposable
    {
        private readonly SafePkcs12Handle _pkcs12Handle;
        private SafeEvpPKeyHandle _evpPkeyHandle;
        private SafeX509Handle _x509Handle;
        private SafeX509StackHandle _caStackHandle;

        private OpenSslPkcs12Reader(SafePkcs12Handle pkcs12Handle)
        {
            _pkcs12Handle = pkcs12Handle;
        }

        public static bool TryRead(byte[] data, out OpenSslPkcs12Reader pkcs12Reader)
        {
            SafePkcs12Handle handle = Interop.Crypto.DecodePkcs12(data, data.Length);

            if (!handle.IsInvalid)
            {
                pkcs12Reader = new OpenSslPkcs12Reader(handle);
                return true;
            }

            handle.Dispose();
            pkcs12Reader = null;
            return false;
        }

        public static bool TryRead(SafeBioHandle fileBio, out OpenSslPkcs12Reader pkcs12Reader)
        {
            SafePkcs12Handle p12 = Interop.Crypto.DecodePkcs12FromBio(fileBio);

            if (!p12.IsInvalid)
            {
                pkcs12Reader = new OpenSslPkcs12Reader(p12);
                return true;
            }

            p12.Dispose();
            pkcs12Reader = null;
            return false;
        }

        public void Dispose()
        {
            if (_caStackHandle != null)
            {
                _caStackHandle.Dispose();
                _caStackHandle = null;
            }

            if (_x509Handle != null)
            {
                _x509Handle.Dispose();
                _x509Handle = null;
            }

            if (_evpPkeyHandle != null)
            {
                _evpPkeyHandle.Dispose();
                _evpPkeyHandle = null;
            }

            if (_pkcs12Handle != null)
            {
                _pkcs12Handle.Dispose();
            }
        }

        public void Decrypt(SafePasswordHandle password)
        {
            bool parsed = Interop.Crypto.Pkcs12Parse(
                _pkcs12Handle,
                password,
                out _evpPkeyHandle,
                out _x509Handle,
                out _caStackHandle);

            if (!parsed)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        public List<OpenSslX509CertificateReader> ReadCertificates()
        {
            var certs = new List<OpenSslX509CertificateReader>();

            if (_caStackHandle != null && !_caStackHandle.IsInvalid)
            {
                int caCertCount = Interop.Crypto.GetX509StackFieldCount(_caStackHandle);

                for (int i = 0; i < caCertCount; i++)
                {
                    IntPtr certPtr = Interop.Crypto.GetX509StackField(_caStackHandle, i);

                    if (certPtr != IntPtr.Zero)
                    {
                        // The STACK_OF(X509) still needs to be cleaned up, so upref the handle out of it.
                        certs.Add(new OpenSslX509CertificateReader(Interop.Crypto.X509UpRef(certPtr)));
                    }
                }
            }

            if (_x509Handle != null && !_x509Handle.IsInvalid)
            {
                // The certificate and (if applicable) private key handles will be given over
                // to the OpenSslX509CertificateReader, and the fields here are thus nulled out to
                // prevent double-Dispose.
                OpenSslX509CertificateReader reader = new OpenSslX509CertificateReader(_x509Handle);
                _x509Handle = null;

                if (_evpPkeyHandle != null && !_evpPkeyHandle.IsInvalid)
                {
                    reader.SetPrivateKey(_evpPkeyHandle);
                    _evpPkeyHandle = null;
                }

                certs.Add(reader);
            }

            return certs;
        }
    }
}
