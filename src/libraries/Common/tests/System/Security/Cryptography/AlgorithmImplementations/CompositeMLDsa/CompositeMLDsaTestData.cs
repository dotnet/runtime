// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static partial class CompositeMLDsaTestData
    {
        public class CompositeMLDsaTestVector
        {
            internal string Id { get; }
            internal CompositeMLDsaAlgorithm Algorithm { get; }

            private readonly byte[] _publicKey;
            private readonly byte[] _certificate;
            private readonly byte[] _secretKey;
            private readonly byte[] _pkcs8;
            private readonly byte[] _message;
            private readonly byte[] _context;
            private readonly byte[] _signature;
            private readonly byte[] _signatureWithContext;
            private byte[] _spki;

            internal byte[] Message => _message.AsSpan().ToArray();
            internal byte[] Context => _context.AsSpan().ToArray();
            internal byte[] PublicKey => _publicKey.AsSpan().ToArray();
            internal byte[] Certificate => _certificate.AsSpan().ToArray();
            internal byte[] SecretKey => _secretKey.AsSpan().ToArray();
            internal byte[] Pkcs8 => _pkcs8.AsSpan().ToArray();
            internal byte[] Signature => _signature.AsSpan().ToArray();
            internal byte[] SignatureWithContext => _signatureWithContext.AsSpan().ToArray();

            internal byte[] Spki => _spki.AsSpan().ToArray();

            internal CompositeMLDsaTestVector(string tcId, CompositeMLDsaAlgorithm algo, string pk, string x5c, string sk, string sk_pkcs8, string m, string ctx, string s, string sWithContext)
            {
                Id = tcId;
                Algorithm = algo;
                _publicKey = Convert.FromBase64String(pk);
                _certificate = Convert.FromBase64String(x5c);
                _secretKey = Convert.FromBase64String(sk);
                _pkcs8 = Convert.FromBase64String(sk_pkcs8);
                _message = Convert.FromBase64String(m);
                _context = Convert.FromBase64String(ctx);
                _signature = Convert.FromBase64String(s);
                _signatureWithContext = Convert.FromBase64String(sWithContext);

                AsnReader reader = new AsnReader(_certificate, AsnEncodingRules.DER);
                AsnReader certificate = reader.ReadSequence();
                AsnReader tbsCertificate = certificate.ReadSequence();

                tbsCertificate.ReadEncodedValue(); // Version
                tbsCertificate.ReadEncodedValue(); // SerialNumber
                tbsCertificate.ReadEncodedValue(); // Signature
                tbsCertificate.ReadEncodedValue(); // Issuer
                tbsCertificate.ReadEncodedValue(); // Validity
                tbsCertificate.ReadEncodedValue(); // Subject

                _spki = tbsCertificate.ReadEncodedValue().ToArray();
            }

            public override string ToString() => Id;
        }

        internal static partial CompositeMLDsaTestVector[] AllIetfVectors { get; }

        public static IEnumerable<object[]> AllIetfVectorsTestData =>
            AllIetfVectors.Select(v => new object[] { v });

        internal static CompositeMLDsaTestVector[] SupportedAlgorithmIetfVectors =>
            field ??= AllIetfVectors.Where(v => CompositeMLDsa.IsAlgorithmSupported(v.Algorithm)).ToArray();

        public static IEnumerable<object[]> SupportedAlgorithmIetfVectorsTestData =>
            SupportedAlgorithmIetfVectors.Select(v => new object[] { v });

        public static IEnumerable<object[]> SupportedECDsaAlgorithmIetfVectorsTestData =>
            SupportedAlgorithmIetfVectors
                .Where(vector => CompositeMLDsa.IsAlgorithmSupported(vector.Algorithm) && CompositeMLDsaTestHelpers.IsECDsa(vector.Algorithm))
                .Select(v => new object[] { v });

        internal static CompositeMLDsaAlgorithm[] AllAlgorithms => field ??=
        [
            CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,
            CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,
            CompositeMLDsaAlgorithm.MLDsa44WithEd25519,
            CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,
            CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,
            CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,
            CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,
            CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,
            CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,
            CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,
            CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,
            CompositeMLDsaAlgorithm.MLDsa65WithEd25519,
            CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,
            CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,
            CompositeMLDsaAlgorithm.MLDsa87WithEd448,
            CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,
            CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,
            CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,
        ];

        public static IEnumerable<object[]> AllAlgorithmsTestData =>
            AllAlgorithms.Select(v => new object[] { v });

        public static IEnumerable<object[]> SupportedAlgorithmsTestData =>
            AllAlgorithms.Where(CompositeMLDsa.IsAlgorithmSupported).Select(v => new object[] { v });

        internal static MLDsaKeyInfo GetMLDsaIetfTestVector(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaAlgorithm mldsaAlgorithm = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm];

            if (mldsaAlgorithm == MLDsaAlgorithm.MLDsa44)
            {
                return MLDsaTestsData.IetfMLDsa44;
            }
            else if (mldsaAlgorithm == MLDsaAlgorithm.MLDsa65)
            {
                return MLDsaTestsData.IetfMLDsa65;
            }
            else if (mldsaAlgorithm == MLDsaAlgorithm.MLDsa87)
            {
                return MLDsaTestsData.IetfMLDsa87;
            }
            else
            {
                throw new XunitException($"Algorithm '{algorithm.Name}' doesn't have ML-DSA component.");
            }
        }

        internal static CompositeMLDsaTestVector GetIetfTestVector(CompositeMLDsaAlgorithm algorithm) =>
            AllIetfVectors.Single(v => v.Algorithm == algorithm);
    }
}
