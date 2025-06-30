// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a composite ML-DSA algorithm identifier, combining ML-DSA with a traditional algorithm.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class CompositeMLDsaAlgorithm : IEquatable<CompositeMLDsaAlgorithm>
    {
        /// <summary>
        ///   Gets the name of the algorithm.
        /// </summary>
        /// <value>
        ///   An a string representing the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///   Gets the maximum signature size in bytes for the composite algorithm.
        /// </summary>
        public int MaxSignatureSizeInBytes { get; }

        internal MLDsaAlgorithm MLDsaAlgorithm { get; }

        internal object TraditionalAlgorithm { get; }

        internal string Oid { get; }

        internal byte[] DomainSeparator { get; }

        internal HashAlgorithmName HashAlgorithmName { get; }

        private CompositeMLDsaAlgorithm(
            string name,
            MLDsaAlgorithm mlDsaAlgorithm,
            object traditionalAlgorithm,
            int maxTraditionalSignatureSize,
            string oid,
            byte[] domainSeparator,
            HashAlgorithmName hashAlgorithmName)
        {
            Name = name;
            MLDsaAlgorithm = mlDsaAlgorithm;
            TraditionalAlgorithm = traditionalAlgorithm;
            Oid = oid;
            DomainSeparator = domainSeparator;
            HashAlgorithmName = hashAlgorithmName;

            // 32 bytes for randomizer
            MaxSignatureSizeInBytes = 32 + MLDsaAlgorithm.SignatureSizeInBytes + maxTraditionalSignatureSize;
        }

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PSS with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PSS with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pss { get; } =
            new("MLDSA44-RSA2048-PSS-SHA256",
                MLDsaAlgorithm.MLDsa44,
                new RsaAlgorithm(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
                2048 / 8,
                Oids.MLDsa44WithRSA2048PssPreHashSha256,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x00],
                HashAlgorithmName.SHA256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PKCS1-v1_5 with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PKCS1-v1_5 with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pkcs15 { get; } =
            new("MLDSA44-RSA2048-PKCS15-SHA256",
                MLDsaAlgorithm.MLDsa44,
                new RsaAlgorithm(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                2048 / 8,
                Oids.MLDsa44WithRSA2048Pkcs15PreHashSha256,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x01],
                HashAlgorithmName.SHA256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and Ed25519 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and Ed25519 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithEd25519 { get; } =
            new("MLDSA44-Ed25519-SHA512",
                MLDsaAlgorithm.MLDsa44,
                new object(),
                64,
                Oids.MLDsa44WithEd25519PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x02],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and ECDSA P-256 with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and ECDSA P-256 with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithECDsaP256 { get; } =
            new("MLDSA44-ECDSA-P256-SHA256",
                MLDsaAlgorithm.MLDsa44,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(256),
                Oids.MLDsa44WithECDsaP256PreHashSha256,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x03],
                HashAlgorithmName.SHA256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pss { get; } =
            new("MLDSA65-RSA3072-PSS-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new RsaAlgorithm(3072, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                3072 / 8,
                Oids.MLDsa65WithRSA3072PssPreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x05],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pkcs15 { get; } =
            new("MLDSA65-RSA3072-PKCS15-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new RsaAlgorithm(3072, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                3072 / 8,
                Oids.MLDsa65WithRSA3072Pkcs15PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x05],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pss { get; } =
            new("MLDSA65-RSA4096-PSS-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new RsaAlgorithm(4096, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),
                4096 / 8,
                Oids.MLDsa65WithRSA4096PssPreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x06],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pkcs15 { get; } =
            new("MLDSA65-RSA4096-PKCS15-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new RsaAlgorithm(4096, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                4096 / 8,
                Oids.MLDsa65WithRSA4096Pkcs15PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x07],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-256 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-256 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP256 { get; } =
            new("MLDSA65-ECDSA-P256-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(256),
                Oids.MLDsa65WithECDsaP256PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x08],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-384 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-384 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP384 { get; } =
            new("MLDSA65-ECDSA-P384-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(384),
                Oids.MLDsa65WithECDsaP384PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x09],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA BrainpoolP256r1 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA BrainpoolP256r1 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaBrainpoolP256r1 { get; } =
            new("MLDSA65-ECDSA-brainpoolP256r1-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(256),
                Oids.MLDsa65WithECDsaBrainpoolP256r1PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0A],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and Ed25519 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and Ed25519 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithEd25519 { get; } =
            new("MLDSA65-Ed25519-SHA512",
                MLDsaAlgorithm.MLDsa65,
                new EdDsaAlgorithm(),
                64,
                Oids.MLDsa65WithEd25519PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0B],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-384 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-384 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP384 { get; } =
            new("MLDSA87-ECDSA-P384-SHA512",
                MLDsaAlgorithm.MLDsa87,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(384),
                Oids.MLDsa87WithECDsaP384PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0C],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA BrainpoolP384r1 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA BrainpoolP384r1 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaBrainpoolP384r1 { get; } =
            new("MLDSA87-ECDSA-brainpoolP384r1-SHA512",
                MLDsaAlgorithm.MLDsa87,
                new ECDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(384),
                Oids.MLDsa87WithECDsaBrainpoolP384r1PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0D],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and Ed448 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and Ed448 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithEd448 { get; } =
            new("MLDSA87-Ed448-SHAKE256",
                MLDsaAlgorithm.MLDsa87,
                new EdDsaAlgorithm(),
                114,
                Oids.MLDsa87WithEd448PreHashShake256_512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0E],
                new HashAlgorithmName("SHAKE256"));

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithRSA3072Pss { get; } =
            new("MLDSA87-RSA3072-PSS-SHA512",
                MLDsaAlgorithm.MLDsa87,
                new RsaAlgorithm(3072, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                3072 / 8,
                Oids.MLDsa87WithRSA3072PssPreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x0F],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithRSA4096Pss { get; } =
            new("MLDSA87-RSA4096-PSS-SHA512",
                MLDsaAlgorithm.MLDsa87,
                new RsaAlgorithm(4096, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
                4096 / 8,
                Oids.MLDsa87WithRSA4096PssPreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x10],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-521 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-521 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP521 { get; } =
            new("MLDSA87-ECDSA-P521-SHA512",
                MLDsaAlgorithm.MLDsa87,
                new EdDsaAlgorithm(),
                AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(521),
                Oids.MLDsa87WithECDsaP521PreHashSha512,
                [0x06, 0x0B, 0x60, 0x86, 0x48, 0x01, 0x86, 0xFA, 0x6B, 0x50, 0x09, 0x01, 0x11],
                HashAlgorithmName.SHA512);

        /// <summary>
        ///   Compares two <see cref="CompositeMLDsaAlgorithm" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="CompositeMLDsaAlgorithm"/> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        // This is a closed type, so all we need to compare are the names.
        public bool Equals([NotNullWhen(true)] CompositeMLDsaAlgorithm? other) => other is not null && other.Name == Name;

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CompositeMLDsaAlgorithm alg && alg.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLDsaAlgorithm" /> objects specify the same algorithm name.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies an algorithm name.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLDsaAlgorithm" /> objects do not specify the same algorithm name.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies an algorithm name.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are not considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right)
        {
            return !(left == right);
        }

        internal static CompositeMLDsaAlgorithm? GetAlgorithmFromOid(string? oid)
        {
            return oid switch
            {
                Oids.MLDsa44WithRSA2048PssPreHashSha256 =>              MLDsa44WithRSA2048Pss,
                Oids.MLDsa44WithRSA2048Pkcs15PreHashSha256 =>           MLDsa44WithRSA2048Pkcs15,
                Oids.MLDsa44WithEd25519PreHashSha512 =>                 MLDsa44WithEd25519,
                Oids.MLDsa44WithECDsaP256PreHashSha256 =>               MLDsa44WithECDsaP256,
                Oids.MLDsa65WithRSA3072PssPreHashSha512 =>              MLDsa65WithRSA3072Pss,
                Oids.MLDsa65WithRSA3072Pkcs15PreHashSha512 =>           MLDsa65WithRSA3072Pkcs15,
                Oids.MLDsa65WithRSA4096PssPreHashSha512 =>              MLDsa65WithRSA4096Pss,
                Oids.MLDsa65WithRSA4096Pkcs15PreHashSha512 =>           MLDsa65WithRSA4096Pkcs15,
                Oids.MLDsa65WithECDsaP256PreHashSha512 =>               MLDsa65WithECDsaP256,
                Oids.MLDsa65WithECDsaP384PreHashSha512 =>               MLDsa65WithECDsaP384,
                Oids.MLDsa65WithECDsaBrainpoolP256r1PreHashSha512 =>    MLDsa65WithECDsaBrainpoolP256r1,
                Oids.MLDsa65WithEd25519PreHashSha512 =>                 MLDsa65WithEd25519,
                Oids.MLDsa87WithECDsaP384PreHashSha512 =>               MLDsa87WithECDsaP384,
                Oids.MLDsa87WithECDsaBrainpoolP384r1PreHashSha512 =>    MLDsa87WithECDsaBrainpoolP384r1,
                Oids.MLDsa87WithEd448PreHashShake256_512 =>             MLDsa87WithEd448,
                Oids.MLDsa87WithRSA3072PssPreHashSha512 =>              MLDsa87WithRSA3072Pss,
                Oids.MLDsa87WithRSA4096PssPreHashSha512 =>              MLDsa87WithRSA4096Pss,
                Oids.MLDsa87WithECDsaP521PreHashSha512 =>               MLDsa87WithECDsaP521,

                _ => null,
            };
        }

        internal sealed class RsaAlgorithm(int keySizeInBits, HashAlgorithmName hashAlgorithmName, RSASignaturePadding padding)
        {
            internal int KeySizeInBits { get; } = keySizeInBits;
            internal HashAlgorithmName HashAlgorithmName { get; } = hashAlgorithmName;
            internal RSASignaturePadding Padding { get; } = padding;
        }

        internal sealed class ECDsaAlgorithm
        {
        }

        internal sealed class EdDsaAlgorithm
        {
        }
    }
}
