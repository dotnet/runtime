// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class PublicKey
    {
        private readonly Oid _oid;
        private AsymmetricAlgorithm? _key;

        public PublicKey(Oid oid, AsnEncodedData parameters, AsnEncodedData keyValue)
        {
            _oid = oid;
            EncodedParameters = new AsnEncodedData(parameters);
            EncodedKeyValue = new AsnEncodedData(keyValue);
        }

        public PublicKey(AsymmetricAlgorithm key)
        {
            byte[] subjectPublicKey = key.ExportSubjectPublicKeyInfo();
            DecodeSubjectPublicKeyInfo(
                subjectPublicKey,
                out Oid localOid,
                out AsnEncodedData localParameters,
                out AsnEncodedData localKeyValue);

            _oid = localOid;
            EncodedParameters = localParameters;
            EncodedKeyValue = localKeyValue;

            // Do not assign _key = key. Otherwise, the public Key property
            // will start returning non Rsa / Dsa types.
        }

        public AsnEncodedData EncodedKeyValue { get; private set; }

        public AsnEncodedData EncodedParameters { get; private set; }

        public AsymmetricAlgorithm Key
        {
            get
            {
                if (_key == null)
                {
                    switch (_oid.Value)
                    {
                        case Oids.Rsa:
                        case Oids.Dsa:
                            _key = X509Pal.Instance.DecodePublicKey(_oid, EncodedKeyValue.RawData, EncodedParameters.RawData, null);
                            break;

                        default:
                            // This includes ECDSA, because an Oids.EcPublicKey key can be
                            // many different algorithm kinds, not necessarily with mutual exclusion.
                            //
                            // Plus, .NET Framework only supports RSA and DSA in this property.
                            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
                    }
                }

                return _key;
            }
        }

        public Oid Oid => _oid;

        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            EncodeSubjectPublicKeyInfo().TryEncode(destination, out bytesWritten);

        public byte[] ExportSubjectPublicKeyInfo() =>
            EncodeSubjectPublicKeyInfo().Encode();

        public static PublicKey CreateFromSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
        {
            int read = DecodeSubjectPublicKeyInfo(
                source,
                out Oid localOid,
                out AsnEncodedData localParameters,
                out AsnEncodedData localKeyValue);

            bytesRead = read;
            return new PublicKey(localOid, localParameters, localKeyValue);
        }

        private AsnWriter EncodeSubjectPublicKeyInfo()
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn {
                Algorithm = new AlgorithmIdentifierAsn {
                    Algorithm = _oid.Value ?? string.Empty,
                    Parameters = EncodedParameters.RawData
                },
                SubjectPublicKey = EncodedKeyValue.RawData
            };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            spki.Encode(writer);
            return writer;
        }

        private static unsafe int DecodeSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out Oid oid,
            out AsnEncodedData parameters,
            out AsnEncodedData keyValue)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
            {
                AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);

                int read;
                SubjectPublicKeyInfoAsn spki;

                try
                {
                    read = reader.PeekEncodedValue().Length;
                    SubjectPublicKeyInfoAsn.Decode(ref reader, manager.Memory, out spki);
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                oid = new Oid(spki.Algorithm.Algorithm);
                parameters = new AsnEncodedData(spki.Algorithm.Parameters?.ToArray() ?? Array.Empty<byte>());
                keyValue = new AsnEncodedData(spki.SubjectPublicKey.ToArray());
                return read;
            }
        }
    }
}
