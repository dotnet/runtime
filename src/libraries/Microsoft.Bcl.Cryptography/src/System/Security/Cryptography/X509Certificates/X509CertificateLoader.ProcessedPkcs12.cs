// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref Pkcs12Return earlyReturn)
        {
            string hydrated = password.ToString();

            if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) && segment.Offset == 0)
            {
                Debug.Assert(segment.Array is not null);
                earlyReturn = new X509Certificate2(segment.Array, hydrated, keyStorageFlags);
            }
            else
            {
                byte[] rented = CryptoPool.Rent(data.Length);
                data.Span.CopyTo(rented);

                try
                {
                    earlyReturn = new X509Certificate2(rented, hydrated, keyStorageFlags);
                }
                finally
                {
                    CryptoPool.Return(rented, data.Length);
                }
            }
        }

        static partial void LoadPkcs12NoLimits(
            ReadOnlyMemory<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            ref X509Certificate2Collection? earlyReturn)
        {
            string hydrated = password.ToString();
            X509Certificate2Collection coll = new X509Certificate2Collection();

            if (MemoryMarshal.TryGetArray(data, out ArraySegment<byte> segment) && segment.Offset == 0)
            {
                Debug.Assert(segment.Array is not null);
                coll.Import(segment.Array, hydrated, keyStorageFlags);
            }
            else
            {
                byte[] rented = CryptoPool.Rent(data.Length);
                data.Span.CopyTo(rented);

                try
                {
                    coll.Import(rented, hydrated, keyStorageFlags);
                }
                finally
                {
                    CryptoPool.Return(rented, data.Length);
                }
            }

            earlyReturn = coll;
        }

        private static partial Pkcs12Return LoadPkcs12(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            ArraySegment<byte> reassembled = bagState.ToPfx(password);

            try
            {
                Debug.Assert(reassembled.Array is not null);
                Debug.Assert(reassembled.Offset == 0);

                return new X509Certificate2(reassembled.Array, password.ToString(), keyStorageFlags);
            }
            finally
            {
                CryptoPool.Return(reassembled);
            }
        }

        private static partial X509Certificate2Collection LoadPkcs12Collection(
            ref BagState bagState,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags)
        {
            ArraySegment<byte> reassembled = bagState.ToPfx(password);
            X509Certificate2Collection coll = new X509Certificate2Collection();

            try
            {
                Debug.Assert(reassembled.Array is not null);
                Debug.Assert(reassembled.Offset == 0);

                coll.Import(reassembled.Array, password.ToString(), keyStorageFlags);
                return coll;
            }
            finally
            {
                CryptoPool.Return(reassembled);
            }
        }

        private readonly partial struct Pkcs12Return
        {
            private readonly X509Certificate2 _cert;

            internal Pkcs12Return(X509Certificate2 cert)
            {
                _cert = cert;
            }

            internal partial bool HasValue() => _cert is not null;
            internal partial X509Certificate2 ToCertificate() => _cert;

            public static implicit operator Pkcs12Return(X509Certificate2 cert)
            {
                return new Pkcs12Return(cert);
            }
        }
    }
}
