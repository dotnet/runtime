// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;

namespace System.Security.Cryptography.Tests
{
    public sealed class CompositeMLKemTestVector
    {
        private readonly byte[] _encapsulationKey;
        private readonly byte[] _decapsulationKey;
        private readonly byte[] _pkcs8;
        private readonly byte[] _ciphertext;
        private readonly byte[] _sharedSecret;
        private readonly byte[] _spki;

        internal string Id { get; }
        internal CompositeMLKemAlgorithm Algorithm { get; }
        internal ReadOnlySpan<byte> EncapsulationKey => _encapsulationKey;
        internal ReadOnlySpan<byte> DecapsulationKey => _decapsulationKey;
        internal ReadOnlySpan<byte> Pkcs8 => _pkcs8;
        internal ReadOnlySpan<byte> Ciphertext => _ciphertext;
        internal ReadOnlySpan<byte> SharedSecret => _sharedSecret;
        internal ReadOnlySpan<byte> Spki => _spki;

        internal CompositeMLKemTestVector(
            string id,
            CompositeMLKemAlgorithm algorithm,
            string encapsulationKey,
            string certificate,
            string decapsulationKey,
            string pkcs8,
            string ciphertext,
            string sharedSecret)
        {
            Id = id;
            Algorithm = algorithm;
            _encapsulationKey = Convert.FromBase64String(encapsulationKey);
            _decapsulationKey = Convert.FromBase64String(decapsulationKey);
            _pkcs8 = Convert.FromBase64String(pkcs8);
            _ciphertext = Convert.FromBase64String(ciphertext);
            _sharedSecret = Convert.FromBase64String(sharedSecret);

            AsnReader reader = new AsnReader(Convert.FromBase64String(certificate), AsnEncodingRules.DER);
            AsnReader certificateReader = reader.ReadSequence();
            AsnReader tbsCertificate = certificateReader.ReadSequence();
            tbsCertificate.ReadEncodedValue(); // Version
            tbsCertificate.ReadEncodedValue(); // Serial number
            tbsCertificate.ReadEncodedValue(); // Signature
            tbsCertificate.ReadEncodedValue(); // Issuer
            tbsCertificate.ReadEncodedValue(); // Validity
            tbsCertificate.ReadEncodedValue(); // Subject
            _spki = tbsCertificate.ReadEncodedValue().ToArray();
        }

        public override string ToString() => Id;
    }
}
