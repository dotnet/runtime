// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static partial class CompositeMLKemTestData
    {
        internal static partial CompositeMLKemTestVector[] AllIetfVectors { get; }

        public static IEnumerable<object[]> AllIetfVectorsTestData =>
            AllIetfVectors.Select(vector => new object[] { vector });

        public static IEnumerable<object[]> SupportedAlgorithmIetfVectorsTestData =>
            AllIetfVectors
                .Where(vector => CompositeMLKem.IsAlgorithmSupported(vector.Algorithm))
                .Select(vector => new object[] { vector });

        public static IEnumerable<object[]> SupportedXDiffieHellmanIetfVectorsTestData =>
            AllIetfVectors
                .Where(vector =>
                    CompositeMLKem.IsAlgorithmSupported(vector.Algorithm) &&
                    IsXDiffieHellman(vector.Algorithm))
                .Select(vector => new object[] { vector });

        internal static CompositeMLKemAlgorithm[] AllAlgorithms { get; } =
        [
            CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048,
            CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072,
            CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096,
            CompositeMLKemAlgorithm.MLKem768WithX25519,
            CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256,
            CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384,
            CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1,
            CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072,
            CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384,
            CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1,
            CompositeMLKemAlgorithm.MLKem1024WithX448,
            CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521,
        ];

        public static IEnumerable<object[]> AllAlgorithmsTestData =>
            AllAlgorithms.Select(algorithm => new object[] { algorithm });

        public static IEnumerable<object[]> SupportedAlgorithmsTestData =>
            AllAlgorithms.Where(CompositeMLKem.IsAlgorithmSupported).Select(algorithm => new object[] { algorithm });

        public static IEnumerable<object[]> AllAlgorithmsAndDisposalTestData =>
            from algorithm in AllAlgorithms
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        internal sealed class RsaAlgorithm(int keySizeInBits, int maxPublicKeySizeInBytes, int maxPrivateKeySizeInBytes)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;

            internal int MaxPublicKeySizeInBytes { get; } = maxPublicKeySizeInBytes;

            internal int MaxPrivateKeySizeInBytes { get; } = maxPrivateKeySizeInBytes;
        }

        internal sealed class ECDiffieHellmanAlgorithm(int keySizeInBits, int maxPrivateKeySizeInBytes, bool isSecg)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;

            internal int MaxPrivateKeySizeInBytes { get; } = maxPrivateKeySizeInBytes;

            internal bool IsSecg { get; } = isSecg;
        }

        internal sealed class XDiffieHellmanAlgorithm(int keySizeInBits, bool isX25519)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;

            internal bool IsX25519 { get; } = isX25519;
        }

        internal static MLKemAlgorithm GetMLKemAlgorithm(CompositeMLKemAlgorithm algorithm) =>
            algorithm.Name.StartsWith("MLKEM768-", StringComparison.Ordinal) ? MLKemAlgorithm.MLKem768 :
            algorithm.Name.StartsWith("MLKEM1024-", StringComparison.Ordinal) ? MLKemAlgorithm.MLKem1024 :
            throw new XunitException($"Algorithm '{algorithm.Name}' doesn't have an ML-KEM component.");

        internal static T ExecuteComponentFunc<T>(
            CompositeMLKemAlgorithm algorithm,
            Func<RsaAlgorithm, T> rsaFunc,
            Func<ECDiffieHellmanAlgorithm, T> ecdhFunc,
            Func<XDiffieHellmanAlgorithm, T> xdhFunc)
        {
            // Traditional component sizes are derived from the size table in the Composite ML-KEM specification.
            return algorithm.Name switch
            {
                "MLKEM768-RSA2048-SHA3-256" => rsaFunc(new RsaAlgorithm(2048, 300, 1224)),
                "MLKEM768-RSA3072-SHA3-256" or
                "MLKEM1024-RSA3072-SHA3-256" => rsaFunc(new RsaAlgorithm(3072, 428, 1800)),
                "MLKEM768-RSA4096-SHA3-256" => rsaFunc(new RsaAlgorithm(4096, 556, 2381)),
                "MLKEM768-ECDH-P256-SHA3-256" => ecdhFunc(new ECDiffieHellmanAlgorithm(256, 51, isSecg: true)),
                "MLKEM768-ECDH-P384-SHA3-256" or
                "MLKEM1024-ECDH-P384-SHA3-256" => ecdhFunc(new ECDiffieHellmanAlgorithm(384, 64, isSecg: true)),
                "MLKEM1024-ECDH-P521-SHA3-256" => ecdhFunc(new ECDiffieHellmanAlgorithm(521, 82, isSecg: true)),
                "MLKEM768-ECDH-brainpoolP256r1-SHA3-256" => ecdhFunc(new ECDiffieHellmanAlgorithm(256, 52, isSecg: false)),
                "MLKEM1024-ECDH-brainpoolP384r1-SHA3-256" => ecdhFunc(new ECDiffieHellmanAlgorithm(384, 68, isSecg: false)),
                "MLKEM768-X25519-SHA3-256" => xdhFunc(new XDiffieHellmanAlgorithm(32 * 8, isX25519: true)),
                "MLKEM1024-X448-SHA3-256" => xdhFunc(new XDiffieHellmanAlgorithm(56 * 8, isX25519: false)),
                _ => throw new XunitException($"Unsupported algorithm: {algorithm.Name}"),
            };
        }

        internal static int ExpectedEncapsulationKeySizeLowerBound(CompositeMLKemAlgorithm algorithm)
        {
            return GetMLKemAlgorithm(algorithm).EncapsulationKeySizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8, // RSAPublicKey contains at least the modulus
                    ecdh => 1 + 2 * ((ecdh.KeySizeInBits + 7) / 8),
                    xdh => xdh.KeySizeInBits / 8);
        }

        internal static int ExpectedEncapsulationKeySizeUpperBound(CompositeMLKemAlgorithm algorithm)
        {
            return GetMLKemAlgorithm(algorithm).EncapsulationKeySizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.MaxPublicKeySizeInBytes,
                    ecdh => 1 + 2 * ((ecdh.KeySizeInBits + 7) / 8),
                    xdh => xdh.KeySizeInBits / 8);
        }

        internal static int ExpectedDecapsulationKeySizeLowerBound(CompositeMLKemAlgorithm algorithm)
        {
            // The ML-KEM component of a Composite ML-KEM private key is the private seed.
            return GetMLKemAlgorithm(algorithm).PrivateSeedSizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8, // RSAPrivateKey contains at least the modulus
                    ecdh => ecdh.MaxPrivateKeySizeInBytes,
                    xdh => xdh.KeySizeInBits / 8);
        }

        internal static int ExpectedDecapsulationKeySizeUpperBound(CompositeMLKemAlgorithm algorithm)
        {
            return GetMLKemAlgorithm(algorithm).PrivateSeedSizeInBytes +
                ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.MaxPrivateKeySizeInBytes,
                    ecdh => ecdh.MaxPrivateKeySizeInBytes,
                    xdh => xdh.KeySizeInBits / 8);
        }

        internal static bool IsXDiffieHellman(CompositeMLKemAlgorithm algorithm) =>
            ExecuteComponentFunc(algorithm, rsa => false, ecdh => false, xdh => true);
    }
}
