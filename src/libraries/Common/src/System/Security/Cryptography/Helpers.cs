// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
        private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
#endif

        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        internal static bool HasSymmetricEncryption { get; } =
#if NET
            !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#else
            true;
#endif

#if NET
        [UnsupportedOSPlatformGuard("ios")]
        [UnsupportedOSPlatformGuard("tvos")]
        public static bool IsDSASupported => !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS();
#else
        public static bool IsDSASupported => true;
#endif

#if NET
        [UnsupportedOSPlatformGuard("android")]
        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        public static bool IsRC2Supported => !OperatingSystem.IsAndroid() && !OperatingSystem.IsBrowser();
#else
        public static bool IsRC2Supported => true;
#endif

        [UnsupportedOSPlatformGuard("browser")]
        [UnsupportedOSPlatformGuard("wasi")]
        internal static bool HasMD5 { get; } =
#if NET
            !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
#else
            true;
#endif

        [SupportedOSPlatformGuard("windows")]
        internal static bool IsOSPlatformWindows =>
#if NETFRAMEWORK
                true;
#else
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

        [return: NotNullIfNotNull(nameof(src))]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            return src switch
            {
                null => null,
                { Length: 0 } => src,
                _ => (byte[])src.Clone(),
            };
        }

        internal static bool ContainsNull<T>(this ReadOnlySpan<T> span)
        {
            return Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span));
        }

#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
        internal static void RngFill(byte[] destination)
        {
            s_rng.GetBytes(destination);
        }
#endif

        internal static void RngFill(Span<byte> destination)
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            RandomNumberGenerator.Fill(destination);
#else
            byte[] temp = CryptoPool.Rent(destination.Length);
            s_rng.GetBytes(temp, 0, destination.Length);
            temp.AsSpan(0, destination.Length).CopyTo(destination);
            CryptoPool.Return(temp, destination.Length);
#endif
        }

        internal static bool TryCopyToDestination(this ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (source.TryCopyTo(destination))
            {
                bytesWritten = source.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        private static int? TryGetHashOidToByteLength(ReadOnlySpan<char> hashOid)
        {
            return hashOid switch
            {
                Oids.Md5 => 128 / 8,
                Oids.Sha1 => 160 / 8,
                Oids.Sha256 => 256 / 8,
                Oids.Sha384 => 384 / 8,
                Oids.Sha512 => 512 / 8,
                Oids.Sha3_256 => 256 / 8,
                Oids.Sha3_384 => 384 / 8,
                Oids.Sha3_512 => 512 / 8,
                Oids.Shake128 => 256 / 8,
                Oids.Shake256 => 512 / 8,
                _ => null,
            };
        }

        internal static int HashOidToByteLength(string hashOid)
        {
            return TryGetHashOidToByteLength(hashOid) ??
                throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashOid));
        }

#if !BUILDING_PKCS
        /// <summary>
        /// Validates if the hash length matches the expected output size for the given hash algorithm OID.
        /// When the OID is not one of the common hash algorithms, it only validates the format of the OID.
        /// </summary>
        internal static void ValidateHashLength(ReadOnlySpan<byte> hash, ReadOnlySpan<char> hashAlgorithmOid)
        {
            int? outputSize = TryGetHashOidToByteLength(hashAlgorithmOid);

            if (outputSize is not null)
            {
                if (hash.Length != outputSize)
                {
                    throw new CryptographicException(SR.Cryptography_HashLengthMismatch);
                }
            }
            else
            {
                // The OIDs for the algorithms above have max length 11. We'll just round up for a conservative initial estimate.
                const int MaxEncodedOidLengthForCommonHashAlgorithms = 16;
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, MaxEncodedOidLengthForCommonHashAlgorithms);

                try
                {
                    // Only the format of the OID is validated here. The derived classes can decide to do more if they want to.
                    writer.WriteObjectIdentifier(hashAlgorithmOid);
                }
                catch (ArgumentException ae)
                {
                    throw new CryptographicException(SR.Cryptography_HashLengthMismatch, ae);
                }
            }
        }
#endif

        internal static bool HashAlgorithmRequired(string? keyAlgorithm)
        {
            // This list could either be written as "ML-DSA and friends return false",
            // or "RSA and friends return true".
            //
            // The consequences of returning true is that the hashAlgorithm parameter
            // gets pre-validated to not be null or empty, which means false positives
            // impact new ML-DSA-like algorithms.
            //
            // The consequences of returning false is that the hashAlgorithm parameter
            // is not pre-validated.  That just means that in a false negative the user
            // gets probably the same exception, but from a different callstack.
            //
            // False positives or negatives are not possible with the simple Build that takes
            // only an X509Certificate2, as we control the destiny there entirely, it's only
            // for the power user scenario of the X509SignatureGenerator that this is a concern.
            //
            // Since the false-positive is worse than the false-negative, the list is written
            // as explicit-true, implicit-false.
            return keyAlgorithm switch
            {
                Oids.Rsa or
                Oids.RsaPss or
                Oids.EcPublicKey or
                Oids.Dsa => true,
                _ => false,
            };
        }

        internal static IncrementalHash CreateIncrementalHash(HashAlgorithmName hashAlgorithmName)
        {
            try
            {
                return IncrementalHash.CreateHash(hashAlgorithmName);
            }
            catch (PlatformNotSupportedException ex)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName), ex);
            }
        }

        internal static CryptographicException CreateAlgorithmUnknownException(AsnWriter encodedId)
        {
#if NET10_0_OR_GREATER
            return encodedId.Encode(static encoded => CreateAlgorithmUnknownException(Convert.ToHexString(encoded)));
#else
            return CreateAlgorithmUnknownException(HexConverter.ToString(encodedId.Encode(), HexConverter.Casing.Upper));
#endif
        }

        internal static CryptographicException CreateAlgorithmUnknownException(string algorithmId)
        {
            throw new CryptographicException(
                SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, algorithmId));
        }

#if !BUILDING_PKCS
        internal static string EncodeAsnWriterToPem(string label, AsnWriter writer, bool clear = true)
        {
#if NET10_0_OR_GREATER
            return writer.Encode(label, static (label, span) => PemEncoding.WriteString(label, span));
#else
            int length = writer.GetEncodedLength();
            byte[] rent = CryptoPool.Rent(length);

            try
            {
                int written = writer.Encode(rent);
                Debug.Assert(written == length);
                return PemEncoding.WriteString(label, rent.AsSpan(0, written));
            }
            finally
            {
                CryptoPool.Return(rent, clear ? length : 0);
            }
#endif
        }
#endif

        internal static void ThrowIfAsnInvalidLength(ReadOnlySpan<byte> data)
        {
            int bytesRead;

            try
            {
                AsnDecoder.ReadEncodedValue(data, AsnEncodingRules.BER, out _, out _, out bytesRead);
            }
            catch (AsnContentException ace)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, ace);
            }

            if (bytesRead != data.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

#if !BUILDING_PKCS
        internal static void ThrowIfDestinationWrongLength(
            Span<byte> destination,
            int expectedLength,
            [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(destination))] string? paramName = null)
        {
            if (destination.Length != expectedLength)
            {
                throw new ArgumentException(SR.Format(SR.Argument_DestinationImprecise, expectedLength), paramName);
            }
        }
#endif
    }
}
