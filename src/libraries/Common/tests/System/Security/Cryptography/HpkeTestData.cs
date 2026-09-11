// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Security.Cryptography.Tests
{
    public static partial class HpkeTestData
    {
        // A test-data portability bound, not a limit of the HPKE API.
        internal const int MaxExporterContextLength = 1024;

        // Representative coverage, not a Cartesian product. RFC cases include different KEM and outer HKDF hashes.
        // Generated cases add empty inputs/nonce carry and SHAKE PSK mode at the length-prefix boundaries.
        public static IReadOnlyList<HpkeTestVector> Vectors { get; } = Array.AsReadOnly(
            Rfc9180Vectors().Concat(PqDraftVectors()).Concat(GeneratedVectors()).ToArray());

        public static IEnumerable<object[]> VectorNames =>
            Vectors.Select(vector => new object[] { vector.Name });

        public static IEnumerable<object[]> ExportLimits =>
        [
            [HpkeKdf.HKDF_SHA256, 8160],
            [HpkeKdf.HKDF_SHA384, 12240],
            [HpkeKdf.HKDF_SHA512, 16320],
            [HpkeKdf.SHAKE128, 65535],
            [HpkeKdf.SHAKE256, 65535],
        ];

        public static IEnumerable<object[]> RepresentativeSuites
        {
            get
            {
                HashSet<(HpkeKem, HpkeKdf, HpkeAead)> seen = new();

                foreach (HpkeTestVector vector in Vectors)
                {
                    if (seen.Add((vector.Kem, vector.Kdf, vector.Aead)))
                    {
                        yield return new object[] { vector.Kem, vector.Kdf, vector.Aead };
                    }
                }
            }
        }

        public static HpkeTestVector GetVector(string name) => Vectors.Single(vector => vector.Name == name);
    }

    // Binary values are hex strings so consumers can obtain independent mutable buffers when needed.
    // KeyMaterial is the recipient's DeriveKey input; Messages are consecutive, starting at sequence zero.
    public sealed record class HpkeTestVector(
        string Name,
        string Source,
        HpkeKem Kem,
        HpkeKdf Kdf,
        HpkeAead Aead,
        bool UsePsk,
        string KeyMaterial,
        string DecapsulationKey,
        string EncapsulationKey,
        string EncapsulatedSecret,
        string Info,
        string Psk,
        string PskId,
        string SharedSecret,
        string AeadKey,
        string BaseNonce,
        string ExporterSecret,
        IReadOnlyList<HpkeMessageVector> Messages,
        IReadOnlyList<HpkeExportVector> Exports)
    {
        public override string ToString() => Name;
    }

    public sealed record class HpkeMessageVector(string Plaintext, string AssociatedData, string Ciphertext);

    public sealed record class HpkeExportVector(string Context, int Length, string ExportedValue);
}
