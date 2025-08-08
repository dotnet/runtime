// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Security.Cryptography.Rsa.Tests;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static class CompositeMLDsaTestHelpers
    {
        internal static readonly Dictionary<CompositeMLDsaAlgorithm, MLDsaAlgorithm> MLDsaAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519,               MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             MLDsaAlgorithm.MLDsa44 },

            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519,               MLDsaAlgorithm.MLDsa65 },

            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448,                 MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             MLDsaAlgorithm.MLDsa87 },
        };

        internal static void AssertImportPublicKey(Action<Func<CompositeMLDsa>> action, CompositeMLDsaAlgorithm algorithm, byte[] publicKey)
        {
            action(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, publicKey));

            if (publicKey?.Length == 0)
            {
                action(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, Array.Empty<byte>().AsSpan()));
                action(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                action(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(algorithm, publicKey.AsSpan()));
            }
        }

        internal static void AssertImportPrivateKey(Action<Func<CompositeMLDsa>> action, CompositeMLDsaAlgorithm algorithm, byte[] privateKey)
        {
            action(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, privateKey));

            if (privateKey?.Length == 0)
            {
                action(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, Array.Empty<byte>().AsSpan()));
                action(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                action(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(algorithm, privateKey.AsSpan()));
            }
        }

        internal class RsaAlgorithm(int keySizeInBits)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
        }

        internal class ECDsaAlgorithm(int keySizeInBits, bool isSec)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
            internal bool IsSec { get; } = isSec;
        }

        internal class EdDsaAlgorithm(int keySizeInBits)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
        }

        internal static void ExecuteComponentAction(
            CompositeMLDsaAlgorithm algo,
            Action<RsaAlgorithm> rsaFunc,
            Action<ECDsaAlgorithm> ecdsaFunc,
            Action<EdDsaAlgorithm> eddsaFunc)
        {
            ExecuteComponentFunc(
                algo,
                info => { rsaFunc(info); return true; },
                info => { ecdsaFunc(info); return true; },
                info => { eddsaFunc(info); return true; });
        }

        internal static T ExecuteComponentFunc<T>(
            CompositeMLDsaAlgorithm algo,
            Func<RsaAlgorithm, T> rsaFunc,
            Func<ECDsaAlgorithm, T> ecdsaFunc,
            Func<EdDsaAlgorithm, T> eddsaFunc)
        {
            if (algo == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15 ||
                algo == CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss)
            {
                return rsaFunc(new RsaAlgorithm(2048));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss)
            {
                return rsaFunc(new RsaAlgorithm(3072));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss)
            {
                return rsaFunc(new RsaAlgorithm(4096));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256)
            {
                return ecdsaFunc(new ECDsaAlgorithm(256, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1)
            {
                return ecdsaFunc(new ECDsaAlgorithm(256, isSec: false));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384)
            {
                return ecdsaFunc(new ECDsaAlgorithm(384, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1)
            {
                return ecdsaFunc(new ECDsaAlgorithm(384, isSec: false));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521)
            {
                return ecdsaFunc(new ECDsaAlgorithm(521, isSec: true));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa44WithEd25519 ||
                     algo == CompositeMLDsaAlgorithm.MLDsa65WithEd25519)
            {
                return eddsaFunc(new EdDsaAlgorithm(256));
            }
            else if (algo == CompositeMLDsaAlgorithm.MLDsa87WithEd448)
            {
                return eddsaFunc(new EdDsaAlgorithm(456));
            }
            else
            {
                throw new XunitException($"Unsupported algorithm: {algo}");
            }
        }

        internal static bool IsECDsa(CompositeMLDsaAlgorithm algorithm) => ExecuteComponentFunc(algorithm, rsa => false, ecdsa => true, eddsa => false);

        internal static void AssertExportPublicKey(Action<Func<CompositeMLDsa, byte[]>> callback)
        {
            callback(dsa => dsa.ExportCompositeMLDsaPublicKey());
            callback(dsa => DoTryUntilDone(dsa.TryExportCompositeMLDsaPublicKey));
        }

        internal static void AssertExportPrivateKey(Action<Func<CompositeMLDsa, byte[]>> callback)
        {
            callback(dsa => dsa.ExportCompositeMLDsaPrivateKey());
            callback(dsa => DoTryUntilDone(dsa.TryExportCompositeMLDsaPrivateKey));
        }

        internal static void WithDispose<T>(T disposable, Action<T> callback)
            where T : IDisposable
        {
            using (disposable)
            {
                callback(disposable);
            }
        }

        internal static void AssertPublicKeyEquals(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            AssertExtensions.SequenceEqual(expected, actual);
        }

        internal static void AssertPrivateKeyEquals(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
        {
            ReadOnlySpan<byte> expectedMLDsaKey = expected.Slice(0, MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes);
            ReadOnlySpan<byte> actualMLDsaKey = actual.Slice(0, MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes);

            AssertExtensions.SequenceEqual(expectedMLDsaKey, actualMLDsaKey);

            byte[] expectedTradKey = expected.Slice(expectedMLDsaKey.Length).ToArray();
            byte[] actualTradKey = actual.Slice(actualMLDsaKey.Length).ToArray();

            ExecuteComponentAction(
                algorithm,
                _ =>
                {
                    RSAParameters expectedRsaParameters = RSAParametersFromRawPrivateKey(expectedTradKey);
                    RSAParameters actualRsaParameters = RSAParametersFromRawPrivateKey(actualTradKey);

                    RSATestHelpers.AssertKeyEquals(expectedRsaParameters, actualRsaParameters);
                },
                _ => Assert.Equal(expectedTradKey, actualTradKey),
                _ => Assert.Equal(expectedTradKey, actualTradKey));
        }

        private static RSAParameters RSAParametersFromRawPrivateKey(ReadOnlySpan<byte> key)
        {
            RSAParameters parameters = default;

            AsnValueReader reader = new AsnValueReader(key, AsnEncodingRules.BER);
            AsnValueReader sequenceReader = reader.ReadSequence(Asn1Tag.Sequence);

            if (!sequenceReader.TryReadInt32(out int version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            const int MaxSupportedVersion = 0;

            if (version > MaxSupportedVersion)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_RSAPrivateKey_VersionTooNew,
                        version,
                        MaxSupportedVersion));
            }

            parameters.Modulus = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

            int modulusLength = parameters.Modulus.Length;
            int halfModulusLength = modulusLength / 2;

            if (parameters.Modulus.Length != modulusLength)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
            }

            parameters.Exponent = sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes();

            // We're not pinning and clearing the arrays here because this is a test helper.
            // In production code, you should always pin and clear sensitive data.
            parameters.D = new byte[modulusLength];
            parameters.P = new byte[halfModulusLength];
            parameters.Q = new byte[halfModulusLength];
            parameters.DP = new byte[halfModulusLength];
            parameters.DQ = new byte[halfModulusLength];
            parameters.InverseQ = new byte[halfModulusLength];

            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.D);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.P);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.Q);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DP);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.DQ);
            sequenceReader.ReadIntegerBytes().ToUnsignedIntegerBytes(parameters.InverseQ);

            sequenceReader.ThrowIfNotEmpty();
            reader.ThrowIfNotEmpty();

            return parameters;
        }

        private static byte[] ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span)
        {
            if (span.Length > 1 && span[0] == 0)
            {
                return span.Slice(1).ToArray();
            }

            return span.ToArray();
        }

        private static void ToUnsignedIntegerBytes(this ReadOnlySpan<byte> span, Span<byte> destination)
        {
            int length = destination.Length;

            if (span.Length == length)
            {
                span.CopyTo(destination);
                return;
            }

            if (span.Length == length + 1)
            {
                if (span[0] == 0)
                {
                    span.Slice(1).CopyTo(destination);
                    return;
                }
            }

            if (span.Length > length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            destination.Slice(0, destination.Length - span.Length).Clear();
            span.CopyTo(destination.Slice(length - span.Length));
        }

        internal static void VerifyDisposed(CompositeMLDsa dsa)
        {
            // A signature-sized buffer can be reused for keys as well
            byte[] tempBuffer = new byte[dsa.Algorithm.MaxSignatureSizeInBytes];

            Assert.Throws<ObjectDisposedException>(() => dsa.SignData([], tempBuffer, []));
            Assert.Throws<ObjectDisposedException>(() => dsa.SignData([]));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifyData(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => dsa.VerifyData(Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()));

            Assert.Throws<ObjectDisposedException>(() => dsa.TryExportCompositeMLDsaPrivateKey([], out _));
            Assert.Throws<ObjectDisposedException>(() => dsa.ExportCompositeMLDsaPrivateKey());
            Assert.Throws<ObjectDisposedException>(() => dsa.TryExportCompositeMLDsaPublicKey([], out _));
            Assert.Throws<ObjectDisposedException>(() => dsa.ExportCompositeMLDsaPublicKey());
        }

        private delegate bool TryExportFunc(Span<byte> destination, out int bytesWritten);
        private static byte[] DoTryUntilDone(TryExportFunc func)
        {
            byte[] buffer = new byte[512];
            int written;

            while (!func(buffer, out written))
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            return buffer.AsSpan(0, written).ToArray();
        }
    }
}
