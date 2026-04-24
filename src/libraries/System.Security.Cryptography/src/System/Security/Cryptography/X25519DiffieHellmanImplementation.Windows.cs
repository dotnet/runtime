// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        // https://learn.microsoft.com/en-us/windows/win32/seccng/cng-named-elliptic-curves
        private const string BCRYPT_ECC_CURVE_25519 = "curve25519";
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

        // p = 2^255 - 19 in little-endian
        private static ReadOnlySpan<byte> FieldPrime =>
        [
            0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f,
        ];

        private readonly SafeBCryptKeyHandle _key;
        private readonly bool _hasPrivate;
        private readonly byte _privatePreservation;
        private readonly byte[]? _originalPublicKey;

        private X25519DiffieHellmanImplementation(SafeBCryptKeyHandle key, bool hasPrivate, byte privatePreservation, byte[]? originalPublicKey = null)
        {
            _key = key;
            _hasPrivate = hasPrivate;
            _privatePreservation = privatePreservation;
            _originalPublicKey = originalPublicKey;
            Debug.Assert(_hasPrivate || _privatePreservation == 0);
            Debug.Assert(!_hasPrivate || _originalPublicKey is null);
        }

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static new bool IsSupported => s_algHandle is not null;

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();
            int written;

            if (otherParty is X25519DiffieHellmanImplementation x25519impl)
            {
                using (SafeBCryptSecretHandle secret = Interop.BCrypt.BCryptSecretAgreement(_key, x25519impl._key))
                {
                    Interop.BCrypt.BCryptDeriveKey(
                        secret,
                        BCryptNative.KeyDerivationFunction.Raw,
                        in Unsafe.NullRef<Interop.BCrypt.BCryptBufferDesc>(),
                        destination,
                        out written);
                }
            }
            else
            {
                Span<byte> publicKeyBytes = stackalloc byte[PublicKeySizeInBytes];
                otherParty.ExportPublicKey(publicKeyBytes);

                using (X25519DiffieHellmanImplementation otherPartyImplementation = X25519DiffieHellmanImplementation.ImportPublicKeyImpl(publicKeyBytes))
                using (SafeBCryptSecretHandle secret = Interop.BCrypt.BCryptSecretAgreement(_key, otherPartyImplementation._key))
                {
                    Interop.BCrypt.BCryptDeriveKey(
                        secret,
                        BCryptNative.KeyDerivationFunction.Raw,
                        in Unsafe.NullRef<Interop.BCrypt.BCryptBufferDesc>(),
                        destination,
                        out written);
                }
            }

            if (written != SecretAgreementSizeInBytes)
            {
                destination.Clear();
                Debug.Fail($"Unexpected number of bytes written: {written}.");
                throw new CryptographicException();
            }

            // CNG with BCRYPT_NO_KEY_VALIDATION permits low-order public keys, which produce
            // an all-zero shared secret. Other platforms reject these at
            // derive time per RFC 7748 6.1.
            // We still need BCRYPT_NO_KEY_VALIDATION though because there are small subgroup keys that work, which do
            // not produce all zero shared secrets.
            ReadOnlySpan<byte> zeros = [
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            ];

            Debug.Assert(zeros.Length == SecretAgreementSizeInBytes);

            if (CryptographicOperations.FixedTimeEquals(destination, zeros))
            {
                throw new CryptographicException();
            }
            else
            {
                // BCryptDeriveKey exports with the wrong endianness.
                destination.Reverse();
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            ExportKey(true, destination);
            RefixPrivateScalar(destination, _privatePreservation);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            if (_originalPublicKey is not null)
            {
                _originalPublicKey.CopyTo(destination);
            }
            else
            {
                ExportKey(false, destination);
            }
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
                _key.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static X25519DiffieHellmanImplementation GenerateKeyImpl()
        {
            Debug.Assert(IsSupported);
            SafeBCryptKeyHandle key = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, 0);
            Debug.Assert(!key.IsInvalid);

            try
            {
                Interop.BCrypt.BCryptFinalizeKeyPair(key);
                return new X25519DiffieHellmanImplementation(key, hasPrivate: true, privatePreservation: 0);
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            SafeBCryptKeyHandle key = ImportKey(true, source, out byte preservation);
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: true, privatePreservation: preservation);
        }

        internal static X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            // RFC 7748 Section 5: "implementations of X25519 MUST mask the most significant
            // bit in the final byte" and "Implementations MUST accept non-canonical values and
            // process them as if they had been reduced modulo the field prime."
            //
            // CNG rejects non-canonical u-coordinates (values >= p = 2^255 - 19) and does not
            // mask the high bit. We handle both by masking the high bit then if the value is
            // non-canonical, subtract p to reduce it. Since all values are < 2^255 after
            // masking and p = 2^255 - 19, a single subtraction suffices.
            Span<byte> reduced = stackalloc byte[PublicKeySizeInBytes];
            source.CopyTo(reduced);
            reduced[^1] &= 0x7F;

            byte[]? originalPublicKey = null;

            if (IsNonCanonicalPublicKey(reduced))
            {
                originalPublicKey = source.ToArray();
                ReducePublicKey(reduced);
            }
            else if ((source[^1] & 0x80) != 0)
            {
                // The value is canonical but has the high bit set. CNG doesn't mask it,
                // so we need to clear it before import and preserve the original for export.
                originalPublicKey = source.ToArray();
            }

            SafeBCryptKeyHandle key = ImportKey(false, reduced, out _);
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: false, privatePreservation: 0, originalPublicKey);
        }

        private void ExportKey(bool privateKey, Span<byte> destination)
        {
            string blobType = privateKey ?
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

            Interop.BCrypt.KeyBlobMagicNumber expectedMagicNumber = privateKey ?
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC :
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC;

            ArraySegment<byte> key = Interop.BCrypt.BCryptExportKey(_key, blobType);

            try
            {
                unsafe
                {
                    int blobHeaderSize = sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB);
                    ReadOnlySpan<byte> exported = key;

                    fixed (byte* pExportedSpan = exported)
                    {
                        const int ElementSize = 32;
                        Interop.BCrypt.BCRYPT_ECCKEY_BLOB* blob = (Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)pExportedSpan;

                        if (blob->cbKey != ElementSize || blob->Magic != expectedMagicNumber)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                        }

                        // x
                        ReadOnlySpan<byte> y = new(pExportedSpan + blobHeaderSize + ElementSize, ElementSize);
                        // d

                        // y shouldn't have a value.
                        if (y.IndexOfAnyExcept((byte)0) >= 0)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                        }

                        if (privateKey)
                        {
                            ReadOnlySpan<byte> d = new(pExportedSpan + blobHeaderSize + ElementSize * 2, ElementSize);
                            d.CopyTo(destination);
                        }
                        else
                        {
                            ReadOnlySpan<byte> x = new(pExportedSpan + blobHeaderSize, ElementSize);
                            x.CopyTo(destination);
                        }
                    }
                }
            }
            finally
            {
                if (privateKey)
                {
                    CryptoPool.Return(key);
                }
                else
                {
                    CryptoPool.Return(key, clearSize: 0);
                }
            }
        }

        private static SafeBCryptKeyHandle ImportKey(bool privateKey, ReadOnlySpan<byte> key, out byte preservation)
        {
            Debug.Assert(IsSupported);
            string blobType = privateKey ?
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

            Interop.BCrypt.KeyBlobMagicNumber magicNumber = privateKey ?
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC :
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC;

            unsafe
            {
                int blobHeaderSize = sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB);
                const int ElementSize = 32;
                int requiredBufferSize = blobHeaderSize + ElementSize * 2; // blob + X, Y

                if (privateKey)
                {
                    requiredBufferSize += ElementSize; // d
                }

                byte[] rented = CryptoPool.Rent(requiredBufferSize);
                Span<byte> buffer = rented.AsSpan(0, requiredBufferSize);
                buffer.Clear();

                try
                {
                    fixed (byte* pBlobHeader = buffer)
                    {
                        Interop.BCrypt.BCRYPT_ECCKEY_BLOB* blob = (Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)pBlobHeader;
                        blob->Magic = magicNumber;
                        blob->cbKey = ElementSize;
                    }

                    if (privateKey)
                    {
                        Span<byte> destination = buffer.Slice(blobHeaderSize + ElementSize * 2, ElementSize);
                        key.CopyTo(destination);
                        preservation = FixupPrivateScalar(destination);
                    }
                    else
                    {
                        Span<byte> destination = buffer.Slice(blobHeaderSize, ElementSize);
                        key.CopyTo(destination);
                        preservation = 0;
                    }

                    return Interop.BCrypt.BCryptImportKeyPair(
                        s_algHandle,
                        blobType,
                        buffer,
                        Interop.BCrypt.BCryptImportKeyPairFlags.BCRYPT_NO_KEY_VALIDATION);
                }
                finally
                {
                    if (privateKey)
                    {
                        CryptoPool.Return(rented);
                    }
                    else
                    {
                        CryptoPool.Return(rented, clearSize: 0);
                    }
                }
            }
        }

        private static byte FixupPrivateScalar(Span<byte> bytes)
        {
            byte preservation = (byte)(bytes[0] & 0b111 | bytes[^1] & 0b11000000);

            // From RFC 7748:
            // For X25519, in
            // order to decode 32 random bytes as an integer scalar, set the three
            // least significant bits of the first byte and the most significant bit
            // of the last to zero, set the second most significant bit of the last
            // byte to 1 and, finally, decode as little-endian.
            //
            // Most other X25519 implementations do this for you when importing a private key. CNG does not, so we
            // apply the scalar fixup here.
            //
            // If we import a key that requires us to modify it, we store the modified bits in a byte. This byte does
            // not effectively contain any private key material since these bits are always coerced. However we want
            // keys to roundtrip correctly.
            bytes[0] &= 0b11111000;
            bytes[^1] &= 0b01111111;
            bytes[^1] |= 0b01000000;
            return preservation;
        }

        private static void RefixPrivateScalar(Span<byte> bytes, byte preservation)
        {
            bytes[0] = (byte)((preservation & 0b111) | (bytes[0] & 0b11111000));
            bytes[^1] = (byte)((preservation & 0b11000000) | (bytes[^1] & 0b00111111));
        }

        private static bool IsNonCanonicalPublicKey(ReadOnlySpan<byte> key)
        {
            Debug.Assert(key.Length == PublicKeySizeInBytes);
            Debug.Assert((key[^1] & 0x80) == 0);

            // Compare key >= p (little-endian). Since key < 2^255 (high bit masked)
            // and p = 2^255 - 19, a non-canonical value is in [p, 2^255 - 1].
            // Compare from most significant byte to least significant.
            for (int i = PublicKeySizeInBytes - 1; i >= 0; i--)
            {
                if (key[i] > FieldPrime[i])
                    return true;
                if (key[i] < FieldPrime[i])
                    return false;
            }

            // key == p, which is also non-canonical (reduces to 0)
            return true;
        }

        private static void ReducePublicKey(Span<byte> key)
        {
            Debug.Assert(key.Length == PublicKeySizeInBytes);

            // Subtract p from key. Since we only call this when key >= p and key < 2^255,
            // a single subtraction is sufficient: key = key - p.
            int borrow = 0;

            for (int i = 0; i < PublicKeySizeInBytes; i++)
            {
                int diff = key[i] - FieldPrime[i] - borrow;
                key[i] = (byte)diff;
                borrow = (diff < 0) ? 1 : 0;
            }

            Debug.Assert(borrow == 0);
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.ECDH,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }

            unsafe
            {
                fixed (char* pbInput = BCRYPT_ECC_CURVE_25519)
                {
                    status = Interop.BCrypt.BCryptSetProperty(
                        hAlgorithm,
                        KeyPropertyName.ECCCurveName,
                        pbInput,
                        ((uint)BCRYPT_ECC_CURVE_25519.Length + 1) * 2,
                        0);
                }
            }

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }

            return hAlgorithm;
        }

        private void ThrowIfPrivateNeeded()
        {
            if (!_hasPrivate)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
        }
    }
}
