// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.Asn1;
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
        {
            if (signerIdentifierAsn.IssuerAndSerialNumber.HasValue)
            {
                ReadOnlySpan<byte> issuerNameSpan = signerIdentifierAsn.IssuerAndSerialNumber.Value.Issuer.Span;
                ReadOnlySpan<byte> serial = signerIdentifierAsn.IssuerAndSerialNumber.Value.SerialNumber.Span;

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
                    DummySignerEncodedValue.AsReadOnlySpan().SequenceEqual(issuerNameSpan))
                {
                    Type = SubjectIdentifierType.NoSignature;
                    Value = null;
                }
                else
                {
                    Type = SubjectIdentifierType.IssuerAndSerialNumber;

                    var name = new X500DistinguishedName(issuerNameSpan.ToArray());
                    Value = new X509IssuerSerial(name.Name, serial.ToSkiString());
                }
            }
            else if (signerIdentifierAsn.SubjectKeyIdentifier != null)
            {
                Type = SubjectIdentifierType.SubjectKeyIdentifier;
                Value = signerIdentifierAsn.SubjectKeyIdentifier.Value.Span.ToSkiString();
            }
            else
            {
                Debug.Fail("Do not know how to decode value");
                throw new CryptographicException();
            }
        }

        public SubjectIdentifierType Type { get; }
        public object Value { get; }
    }
}


