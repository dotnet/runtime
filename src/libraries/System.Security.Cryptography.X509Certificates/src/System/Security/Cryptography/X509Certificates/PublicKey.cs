// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicKey" /> class
        /// using SubjectPublicKeyInfo from an <see cref="AsymmetricAlgorithm" />.
        /// </summary>
        /// <param name="key">
        /// An asymmetric algorithm to obtain the SubjectPublicKeyInfo from.
        /// </param>
        /// <exception cref="CryptographicException">
        /// The SubjectPublicKeyInfo could not be decoded. The
        /// <see cref="AsymmetricAlgorithm.ExportSubjectPublicKeyInfo" /> must return a
        /// valid ASN.1-DER encoded X.509 SubjectPublicKeyInfo.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// <see cref="AsymmetricAlgorithm.ExportSubjectPublicKeyInfo" /> has not been overridden
        /// in a derived class.
        /// </exception>
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

        [Obsolete(Obsoletions.PublicKeyPropertyMessage, DiagnosticId = Obsoletions.PublicKeyPropertyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
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

        /// <summary>
        /// Attempts to export the current key in the X.509 SubjectPublicKeyInfo format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The byte span to receive the X.509 SubjectPublicKeyInfo data.
        /// </param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value that indicates the number of bytes written to
        /// <paramref name="destination" />. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> is big enough to receive the output;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            EncodeSubjectPublicKeyInfo().TryEncode(destination, out bytesWritten);

        /// <summary>
        /// Exports the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        /// A byte array containing the X.509 SubjectPublicKeyInfo representation of this key.
        /// </returns>
        public byte[] ExportSubjectPublicKeyInfo() =>
            EncodeSubjectPublicKeyInfo().Encode();

        /// <summary>
        /// Creates a new instance of <see cref="PublicKey" /> from a X.509 SubjectPublicKeyInfo.
        /// </summary>
        /// <param name="source">
        /// The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.
        /// </param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number of bytes read from
        /// <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>A public key representing the SubjectPublicKeyInfo.</returns>
        /// <exception cref="CryptographicException">
        /// The SubjectPublicKeyInfo could not be decoded.
        /// </exception>
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

        /// <summary>
        /// Gets the <see cref="RSA" /> public key, or <see langword="null" /> if the key is not an RSA key.
        /// </summary>
        /// <returns>
        /// The public key, or <see langword="null" /> if the key is not an RSA key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key contents are corrupt or could not be read successfully.
        /// </exception>
        public RSA? GetRSAPublicKey()
        {
            if (_oid.Value != Oids.Rsa)
                return null;

            RSA rsa = RSA.Create();

            try
            {
                rsa.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out _);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the <see cref="DSA" /> public key, or <see langword="null" /> if the key is not an DSA key.
        /// </summary>
        /// <returns>
        /// The public key, or <see langword="null" /> if the key is not an DSA key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key contents are corrupt or could not be read successfully.
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public DSA? GetDSAPublicKey()
        {
            if (_oid.Value != Oids.Dsa)
                return null;

            DSA dsa = DSA.Create();

            try
            {
                dsa.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out _);
                return dsa;
            }
            catch
            {
                dsa.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the <see cref="ECDsa" /> public key, or <see langword="null" /> if the key is not an ECDsa key.
        /// </summary>
        /// <returns>
        /// The public key, or <see langword="null" /> if the key is not an ECDsa key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key contents are corrupt or could not be read successfully.
        /// </exception>
        public ECDsa? GetECDsaPublicKey()
        {
            if (_oid.Value != Oids.EcPublicKey)
                return null;

            ECDsa ecdsa = ECDsa.Create();

            try
            {
                ecdsa.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out _);
                return ecdsa;
            }
            catch
            {
                ecdsa.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the <see cref="ECDiffieHellman" /> public key, or <see langword="null" />
        /// if the key is not an ECDiffieHellman key.
        /// </summary>
        /// <returns>
        /// The public key, or <see langword="null" /> if the key is not an ECDiffieHellman key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key contents are corrupt or could not be read successfully.
        /// </exception>
        public ECDiffieHellman? GetECDiffieHellmanPublicKey()
        {
            if (_oid.Value != Oids.EcPublicKey)
                return null;

            ECDiffieHellman ecdh = ECDiffieHellman.Create();

            try
            {
                ecdh.ImportSubjectPublicKeyInfo(ExportSubjectPublicKeyInfo(), out _);
                return ecdh;
            }
            catch
            {
                ecdh.Dispose();
                throw;
            }
        }

        private AsnWriter EncodeSubjectPublicKeyInfo()
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = _oid.Value ?? string.Empty,
                    Parameters = EncodedParameters.RawData,
                },
                SubjectPublicKey = EncodedKeyValue.RawData,
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

                oid = new Oid(spki.Algorithm.Algorithm, null);
                parameters = new AsnEncodedData(spki.Algorithm.Parameters?.ToArray() ?? Array.Empty<byte>());
                keyValue = new AsnEncodedData(spki.SubjectPublicKey.ToArray());
                return read;
            }
        }
    }
}
