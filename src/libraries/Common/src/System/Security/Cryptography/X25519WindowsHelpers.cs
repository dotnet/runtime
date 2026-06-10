// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static class X25519WindowsHelpers
    {
        // https://learn.microsoft.com/en-us/windows/win32/seccng/cng-named-elliptic-curves
        internal const string BCRYPT_ECC_CURVE_25519 = "curve25519";
        private const int PublicKeySizeInBytes = X25519DiffieHellman.PublicKeySizeInBytes;
        private const int ElementSize = 32;

        // p = 2^255 - 19 in little-endian
        private static ReadOnlySpan<byte> FieldPrime =>
        [
            0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
            0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f,
        ];

        internal static void ExportKey(ReadOnlySpan<byte> exported, bool privateKey, Span<byte> destination)
        {
            Interop.BCrypt.KeyBlobMagicNumber expectedMagicNumber = privateKey ?
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC :
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC;

            unsafe
            {
                int blobHeaderSize = sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB);

                // For private key we expect three parameters (x, y, d) and for public keys we expect two (x, y).
                if (exported.Length < blobHeaderSize + ElementSize * (privateKey ? 3 : 2))
                {
                    throw new CryptographicException();
                }

                fixed (byte* pExportedSpan = exported)
                {
                    Interop.BCrypt.BCRYPT_ECCKEY_BLOB* blob = (Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)pExportedSpan;

                    if (blob->cbKey != ElementSize || blob->Magic != expectedMagicNumber)
                    {
                        throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                    }

                    // The key material after the blob is { x || y }, and optionally d if there is a private key.
                    // y should always be zero as it is not used for Curve25519 keys.

                    // Check y is zero, skip over the blob header and x.
                    ReadOnlySpan<byte> y = new(pExportedSpan + blobHeaderSize + ElementSize, ElementSize);

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

        internal static CryptoPoolLease CreateCngBlob(ReadOnlySpan<byte> key, bool privateKey, out byte preservation)
        {
            Interop.BCrypt.KeyBlobMagicNumber magicNumber = privateKey ?
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PRIVATE_GENERIC_MAGIC :
                Interop.BCrypt.KeyBlobMagicNumber.BCRYPT_ECDH_PUBLIC_GENERIC_MAGIC;

            unsafe
            {
                int blobHeaderSize = sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB);
                int requiredBufferSize = blobHeaderSize + ElementSize * 2; // blob + X, Y

                if (privateKey)
                {
                    requiredBufferSize += ElementSize; // d
                }

                CryptoPoolLease lease = CryptoPoolLease.Rent(requiredBufferSize, skipClear: !privateKey);
                lease.Span.Clear();

                fixed (byte* pBlobHeader = lease.Span)
                {
                    Interop.BCrypt.BCRYPT_ECCKEY_BLOB* blob = (Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)pBlobHeader;
                    blob->Magic = magicNumber;
                    blob->cbKey = ElementSize;
                }

                if (privateKey)
                {
                    // This builds a blob of { x || y || d }. x is the public key, and we leave it as all zeros
                    // and CNG will reconstruct the public key from the private key.
                    // y is not used for Curve25519 so we leave it as zeros.
                    // d follows y. Since we zeroed the whole blob, skip over the header, x, and y and write d.
                    Span<byte> destination = lease.Span.Slice(blobHeaderSize + ElementSize * 2, ElementSize);
                    key.CopyTo(destination);

                    // Any fixup of the key is done in-place in the rented blob, which gets zeroed later.
                    preservation = FixupPrivateScalar(destination);
                }
                else
                {
                    // Otherwise if we are importing the public key, write x after the header.
                    Span<byte> destination = lease.Span.Slice(blobHeaderSize, ElementSize);
                    key.CopyTo(destination);
                    preservation = 0;
                }

                return lease;
            }
        }

        internal static byte FixupPrivateScalar(Span<byte> bytes)
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

        internal static void RefixPrivateScalar(Span<byte> bytes, byte preservation)
        {
            bytes[0] = (byte)((preservation & 0b111) | (bytes[0] & 0b11111000));
            bytes[^1] = (byte)((preservation & 0b11000000) | (bytes[^1] & 0b00111111));
        }

        internal static bool ReducePublicKey(ReadOnlySpan<byte> publicKey, Span<byte> reduced)
        {
            Debug.Assert(publicKey.Length == PublicKeySizeInBytes);
            Debug.Assert(reduced.Length == PublicKeySizeInBytes);

            // RFC 7748 Section 5: "implementations of X25519 MUST mask the most significant
            // bit in the final byte" and "Implementations MUST accept non-canonical values and
            // process them as if they had been reduced modulo the field prime."
            //
            // CNG rejects non-canonical u-coordinates (values >= p = 2^255 - 19) and does not
            // mask the high bit. We handle both by masking the high bit then if the value is
            // non-canonical, subtract p to reduce it. Since all values are < 2^255 after
            // masking and p = 2^255 - 19, a single subtraction suffices.
            publicKey.CopyTo(reduced);
            reduced[^1] &= 0x7F;

            bool requiredReduction = false;

            if (IsNonCanonicalPublicKey(reduced))
            {
                requiredReduction = true;
                ReducePublicKey(reduced);
            }
            else if ((publicKey[^1] & 0x80) != 0)
            {
                requiredReduction = true;
            }

            return requiredReduction;
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
    }
}
