// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

using Internal.Cryptography;

using X509IssuerSerial = System.Security.Cryptography.Xml.X509IssuerSerial;

namespace System.Security.Cryptography.Pkcs
{
    public sealed class SubjectIdentifier
    {
        private const string DummySignerSubjectName = "CN=Dummy Signer";
        internal static readonly byte[] DummySignerEncodedValue =
            new X500DistinguishedName(DummySignerSubjectName).RawData;

        internal SubjectIdentifier(SubjectIdentifierType type, object value)
        {
            Debug.Assert(value != null);
#if DEBUG
            switch (type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    Debug.Assert(value is X509IssuerSerial);
                    break;

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    Debug.Assert(value is string);
                    break;

                default:
                    Debug.Fail($"Illegal type passed to SubjectIdentifier: {type}");
                    break;
            }
#endif //DEBUG
            Type = type;
            Value = value;
        }

        internal SubjectIdentifier(SignerIdentifierAsn signerIdentifierAsn)
            : this(signerIdentifierAsn.IssuerAndSerialNumber, signerIdentifierAsn.SubjectKeyIdentifier)
        {
            
        }

        internal SubjectIdentifier(
            IssuerAndSerialNumberAsn? issuerAndSerialNumber,
            ReadOnlyMemory<byte>? subjectKeyIdentifier)
        {
            if (issuerAndSerialNumber.HasValue)
            {
                ReadOnlySpan<byte> issuerNameSpan = issuerAndSerialNumber.Value.Issuer.Span;
                ReadOnlySpan<byte> serial = issuerAndSerialNumber.Value.SerialNumber.Span;

                bool nonZero = false;

                for (int i = 0; i < serial.Length; i++)
                {
                    if (serial[i] != 0)
                    {
                        nonZero = true;
                        break;
                    }
                }

                // If the serial number is zero and the subject is exactly "CN=Dummy Signer"
                // then this is the special "NoSignature" signer.
                if (!nonZero &&
                    DummySignerEncodedValue.AsSpan().SequenceEqual(issuerNameSpan))
                {
                    Type = SubjectIdentifierType.NoSignature;
                    Value = null;
                }
                else
                {
                    Type = SubjectIdentifierType.IssuerAndSerialNumber;

                    var name = new X500DistinguishedName(issuerNameSpan.ToArray());
                    Value = new X509IssuerSerial(name.Name, serial.ToBigEndianHex());
                }
            }
            else if (subjectKeyIdentifier != null)
            {
                Type = SubjectIdentifierType.SubjectKeyIdentifier;
                Value = subjectKeyIdentifier.Value.Span.ToBigEndianHex();
            }
            else
            {
                Debug.Fail("Do not know how to decode value");
                throw new CryptographicException();
            }
        }

        public SubjectIdentifierType Type { get; }
        public object Value { get; }

        public bool MatchesCertificate(X509Certificate2 certificate)
        {
            switch (Type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    {
                        X509IssuerSerial issuerSerial = (X509IssuerSerial)Value;
                        byte[] serialNumber = issuerSerial.SerialNumber.ToSerialBytes();
                        string issuer = issuerSerial.IssuerName;
                        byte[] certSerialNumber = certificate.GetSerialNumber();

                        return PkcsHelpers.AreByteArraysEqual(certSerialNumber, serialNumber) && certificate.Issuer == issuer;
                    }

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    {
                        string skiString = (string)Value;
                        byte[] ski = skiString.ToSkiBytes();
                        byte[] candidateSki = PkcsPal.Instance.GetSubjectKeyIdentifier(certificate);

                        return PkcsHelpers.AreByteArraysEqual(ski, candidateSki);
                    }

                default:
                    // SubjectIdentifier can only be created by this package so if this an invalid type, it's the package's fault.
                    Debug.Fail($"Invalid SubjectIdentifierType: {Type}");
                    throw new CryptographicException();
            }
        }
    }
}


