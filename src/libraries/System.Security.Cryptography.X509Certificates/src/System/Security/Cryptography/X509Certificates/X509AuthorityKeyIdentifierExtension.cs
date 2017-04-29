// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class X509AuthorityKeyIdentifierExtension : X509Extension
    {
        private string _keyIdentifier;
        private bool _decoded;
        private X500DistinguishedName _firstIssuerName;
        private byte[] _serialNumber;

        internal X509AuthorityKeyIdentifierExtension(X509Extension untypedAkid)
            : base(untypedAkid, untypedAkid.Critical)
        {
            _decoded = false;
        }

        // This might not be suitable for public API, Enumerable<GeneralName> is probably
        // better.
        internal X500DistinguishedName FirstIssuerName
        {
            get
            {
                if (!_decoded)
                    Decode();

                return _firstIssuerName;
            }
        }

        internal string KeyIdentifier
        {
            get
            {
                if (!_decoded)
                    Decode();

                return _keyIdentifier;
            }
        }

        internal byte[] SerialNumberNoCopy
        {
            get
            {
                if (!_decoded)
                    Decode();

                return _serialNumber;
            }
        }

        private void Decode()
        {
            if (_decoded)
                return;

            // AuthorityKeyIdentifier ::= SEQUENCE {
            //   keyIdentifier             [0] KeyIdentifier           OPTIONAL,
            //   authorityCertIssuer       [1] GeneralNames            OPTIONAL,
            //   authorityCertSerialNumber [2] CertificateSerialNumber OPTIONAL  }
            //
            // KeyIdentifier ::= OCTET STRING

            string keyId = null;
            X500DistinguishedName firstIssuerName = null;
            DerSequenceReader reader = new DerSequenceReader(RawData);
            byte[] serialNumber = null;

            // Primitive Context 0
            const byte KeyIdTag = DerSequenceReader.ContextSpecificTagFlag | 0;
            // Constructed Context 1
            const byte CertIssuerTag = DerSequenceReader.ContextSpecificConstructedTag1;
            // Primitive Context 2
            const byte CertSerialTag = DerSequenceReader.ContextSpecificTagFlag | 2;

            if (reader.HasTag(KeyIdTag))
            {
                keyId = reader.ReadOctetString().ToHexStringUpper();
            }

            if (reader.HasTag(CertIssuerTag))
            {
                DerSequenceReader generalNames = reader.ReadSequence();

                while (generalNames.HasData)
                {
                    const byte DirectoryNameTag = DerSequenceReader.ConstructedFlag |
                        (byte)GeneralNameEncoder.GeneralNameTag.DirectoryName;

                    if (firstIssuerName == null && generalNames.HasTag(DirectoryNameTag))
                    {
                        firstIssuerName = new X500DistinguishedName(generalNames.ReadNextEncodedValue());
                    }

                    reader.ValidateAndSkipDerValue();
                }
            }

            if (reader.HasTag(CertSerialTag))
            {
                serialNumber = reader.ReadOctetString();
            }

            if (reader.HasData)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            _firstIssuerName = firstIssuerName;
            _serialNumber = serialNumber;
            _keyIdentifier = keyId;
            _decoded = true;
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _firstIssuerName = null;
            _serialNumber = null;
            _keyIdentifier = null;
            _decoded = false;
        }
    }
}
