// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        private static readonly Lazy<SafeX25519PublicKeyHandle> s_basePointHandle = new(static () =>
        {
            ReadOnlySpan<byte> basePoint =
            [
                9, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            ];

            return ImportPublicKeyAsHandle(basePoint);
        });

        private readonly SafeX25519PublicKeyHandle _publicKey;
        private readonly SafeX25519PrivateKeyHandle? _privateKey;

        internal static new bool IsSupported { get; } = Interop.AndroidCrypto.X25519IsSupported();

        private X25519DiffieHellmanImplementation(
            SafeX25519PublicKeyHandle publicKey,
            SafeX25519PrivateKeyHandle? privateKey)
        {
            _publicKey = publicKey;
            _privateKey = privateKey;
        }

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();

            if (otherParty is X25519DiffieHellmanImplementation otherImpl)
            {
                DeriveRawSecretAgreementCore(_privateKey, otherImpl._publicKey, destination);
            }
            else
            {
                unsafe
                {
                    Span<byte> otherPublicKey = stackalloc byte[PublicKeySizeInBytes];
                    otherParty.ExportPublicKey(otherPublicKey);
                    DeriveRawSecretAgreementCore(otherPublicKey, destination);
                }
            }
        }

        protected override void DeriveRawSecretAgreementCore(ReadOnlySpan<byte> otherPartyPublicKey, Span<byte> destination)
        {
            Debug.Assert(otherPartyPublicKey.Length == PublicKeySizeInBytes);
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();

            using (SafeX25519PublicKeyHandle importedPublicKey = ImportPublicKeyAsHandle(otherPartyPublicKey))
            {
                DeriveRawSecretAgreementCore(_privateKey, importedPublicKey, destination);
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);
            ThrowIfPrivateNeeded();

            byte[] pkcs8Buffer = new byte[128];

            if (!Interop.AndroidCrypto.X25519TryExportPkcs8PrivateKey(_privateKey, pkcs8Buffer, out int written))
            {
                Debug.Fail($"X25519 PKCS#8 PrivateKeyInfo did not fit in {pkcs8Buffer.Length} bytes.");
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            try
            {
                ExtractPrivateKeyFromPkcs8(pkcs8Buffer.AsSpan(0, written)).CopyTo(destination);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs8Buffer);
            }
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);

            byte[] spkiBuffer = new byte[64];

            if (!Interop.AndroidCrypto.X25519TryExportSubjectPublicKeyInfo(_publicKey, spkiBuffer, out int written))
            {
                Debug.Fail($"X25519 SubjectPublicKeyInfo did not fit in {spkiBuffer.Length} bytes.");
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            ExtractPublicKeyFromSubjectPublicKeyInfo(spkiBuffer.AsSpan(0, written)).CopyTo(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfPrivateNeeded();
            return TryExportPkcs8PrivateKeyImpl(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _publicKey.Dispose();
                _privateKey?.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static X25519DiffieHellmanImplementation GenerateKeyImpl()
        {
            Interop.AndroidCrypto.X25519GenerateKey(
                out SafeX25519PublicKeyHandle publicKey,
                out SafeX25519PrivateKeyHandle privateKey);

            return new X25519DiffieHellmanImplementation(publicKey, privateKey);
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == PrivateKeySizeInBytes);

            unsafe
            {
                Span<byte> pkcs8 = stackalloc byte[48];
                bool encoded = TryEncodePrivateKey(source, pkcs8, out int written);
                Debug.Assert(encoded);

                SafeX25519PrivateKeyHandle privateKey = Interop.AndroidCrypto.X25519ImportPkcs8PrivateKey(pkcs8.Slice(0, written));

                try
                {
                    Span<byte> publicKeyBytes = stackalloc byte[PublicKeySizeInBytes];
                    DeriveRawSecretAgreementCore(privateKey, s_basePointHandle.Value, publicKeyBytes);
                    SafeX25519PublicKeyHandle publicKey = ImportPublicKeyAsHandle(publicKeyBytes);
                    return new X25519DiffieHellmanImplementation(publicKey, privateKey);
                }
                catch
                {
                    privateKey.Dispose();
                    throw;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(pkcs8);
                }
            }
        }

        internal static X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == PublicKeySizeInBytes);
            return new X25519DiffieHellmanImplementation(ImportPublicKeyAsHandle(source), privateKey: null);
        }

        [MemberNotNull(nameof(_privateKey))]
        private void ThrowIfPrivateNeeded()
        {
            if (_privateKey is null)
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }
        }

        private static void DeriveRawSecretAgreementCore(
            SafeX25519PrivateKeyHandle currentParty,
            SafeX25519PublicKeyHandle otherParty,
            Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            Interop.AndroidCrypto.X25519DeriveSecret(currentParty, otherParty, destination);
        }

        private static SafeX25519PublicKeyHandle ImportPublicKeyAsHandle(ReadOnlySpan<byte> source)
        {
            unsafe
            {
                Span<byte> spki = stackalloc byte[44];
                bool encoded = TryEncodePublicKey(source, spki, out int written);
                Debug.Assert(encoded);

                return Interop.AndroidCrypto.X25519ImportSubjectPublicKeyInfo(spki.Slice(0, written));
            }
        }

        private static ReadOnlySpan<byte> ExtractPrivateKeyFromPkcs8(ReadOnlySpan<byte> pkcs8)
        {
            ReadOnlySpan<byte> pkcs8Preamble =
            [
                0x30, 0x2e,                         // SEQUENCE (46 bytes)
                0x02, 0x01, 0x00,                   // INTEGER 0
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x04, 0x22,                         // OCTET STRING (34 bytes)
                0x04, 0x20,                         // OCTET STRING (32 bytes)
            ];

            if (pkcs8.Length != pkcs8Preamble.Length + PrivateKeySizeInBytes ||
                !pkcs8.StartsWith(pkcs8Preamble))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return pkcs8.Slice(pkcs8Preamble.Length);
        }

        private static ReadOnlySpan<byte> ExtractPublicKeyFromSubjectPublicKeyInfo(ReadOnlySpan<byte> spki)
        {
            ReadOnlySpan<byte> spkiPreamble =
            [
                0x30, 0x2a, // SEQUENCE (42 bytes)
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x03, 0x21, 0x00, // BIT STRING (33 bytes, 0 unused bits)
            ];

            if (spki.Length != spkiPreamble.Length + PublicKeySizeInBytes ||
                !spki.StartsWith(spkiPreamble))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return spki.Slice(spkiPreamble.Length);
        }

        private static bool TryEncodePrivateKey(ReadOnlySpan<byte> privateKey, Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(privateKey.Length == PrivateKeySizeInBytes);

            ReadOnlySpan<byte> pkcs8Preamble =
            [
                0x30, 0x2e,                         // SEQUENCE (46 bytes)
                0x02, 0x01, 0x00,                   // INTEGER 0
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x04, 0x22,                         // OCTET STRING (34 bytes)
                0x04, 0x20,                         // OCTET STRING (32 bytes)
            ];

            int pkcs8Size = pkcs8Preamble.Length + privateKey.Length;

            if (destination.Length < pkcs8Size)
            {
                bytesWritten = 0;
                return false;
            }

            pkcs8Preamble.CopyTo(destination);
            privateKey.CopyTo(destination.Slice(pkcs8Preamble.Length));
            bytesWritten = pkcs8Size;
            return true;
        }

        private static bool TryEncodePublicKey(ReadOnlySpan<byte> publicKey, Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(publicKey.Length == PublicKeySizeInBytes);

            ReadOnlySpan<byte> spkiPreamble =
            [
                0x30, 0x2a, // SEQUENCE (42 bytes)
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x03, 0x21, 0x00, // BIT STRING (33 bytes, 0 unused bits)
            ];

            int spkiSize = spkiPreamble.Length + publicKey.Length;

            if (destination.Length < spkiSize)
            {
                bytesWritten = 0;
                return false;
            }

            spkiPreamble.CopyTo(destination);
            publicKey.CopyTo(destination.Slice(spkiPreamble.Length));
            bytesWritten = spkiSize;
            return true;
        }
    }
}
