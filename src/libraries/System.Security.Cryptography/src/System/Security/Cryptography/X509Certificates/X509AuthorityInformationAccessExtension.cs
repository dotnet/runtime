// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    ///   Represents the Authority Information Access X.509 Extension (1.3.6.1.5.5.7.1.1).
    /// </summary>
    public sealed class X509AuthorityInformationAccessExtension : X509Extension
    {
        private AccessDescriptionAsn[]? _decoded;

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityInformationAccessExtension" />
        ///   class.
        /// </summary>
        public X509AuthorityInformationAccessExtension()
            : base(Oids.AuthorityInformationAccessOid)
        {
            _decoded = Array.Empty<AccessDescriptionAsn>();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityInformationAccessExtension" />
        ///   class from an encoded representation of the extension and an optional critical marker.
        /// </summary>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rawData" /> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <paramref name="rawData" /> did not decode as an Authority Information Access extension.
        /// </exception>
        public X509AuthorityInformationAccessExtension(byte[] rawData, bool critical = false)
            : base(Oids.AuthorityInformationAccessOid, rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityInformationAccessExtension" />
        ///   class from an encoded representation of the extension and an optional critical marker.
        /// </summary>
        /// <param name="rawData">
        ///   The encoded data used to create the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   <paramref name="rawData" /> did not decode as an Authority Information Access extension.
        /// </exception>
        public X509AuthorityInformationAccessExtension(ReadOnlySpan<byte> rawData, bool critical = false)
            : base(Oids.AuthorityInformationAccessOid, rawData, critical)
        {
            _decoded = Decode(RawData);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509AuthorityInformationAccessExtension" />
        ///   class from a collection of OCSP and CAIssuer values.
        /// </summary>
        /// <param name="ocspUris">
        ///   A collection of OCSP URI values to embed in the extension.
        /// </param>
        /// <param name="caIssuersUris">
        ///   A collection of CAIssuers URI values to embed in the extension.
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> if the extension is critical;
        ///   otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   Both <paramref name="ocspUris"/> and <paramref name="caIssuersUris"/> are
        ///   either <see langword="null" /> or empty.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   One of the values in <paramref name="ocspUris"/> or <paramref name="caIssuersUris"/>
        ///   contains characters outside of the International Alphabet 5 (IA5) character space
        ///   (which is equivalent to 7-bit US-ASCII).
        /// </exception>
        public X509AuthorityInformationAccessExtension(
            IEnumerable<string>? ocspUris,
            IEnumerable<string>? caIssuersUris,
            bool critical = false)
            : base(Oids.AuthorityInformationAccessOid, Encode(ocspUris, caIssuersUris), critical, skipCopy: true)
        {
            _decoded = Decode(RawData);
        }

        /// <inheritdoc />
        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = null;
        }

        /// <summary>
        ///   Enumerates the AccessDescription values described in this extension,
        ///   filtering the results to include only items using the specified access method
        ///   and having a content data type of URI.
        /// </summary>
        /// <param name="accessMethodOid">
        ///   The dotted-decimal form of the access method for filtering.
        /// </param>
        /// <remarks>
        ///   This method does not validate or ensure that the produced values are valid URIs,
        ///   merely that they were encoded as a URI.
        /// </remarks>
        /// <returns>
        ///   The URI-typed access location values that correspond to the requested access method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="accessMethodOid"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The contents of the extension could not be decoded successfully.
        /// </exception>
        /// <seealso cref="EnumerateCAIssuersUris"/>
        /// <seealso cref="EnumerateOcspUris"/>
        public IEnumerable<string> EnumerateUris(string accessMethodOid)
        {
            ArgumentNullException.ThrowIfNull(accessMethodOid);

            _decoded ??= Decode(RawData);

            return EnumerateUrisCore(accessMethodOid);
        }

        /// <summary>
        ///   Enumerates the AccessDescription values described in this extension,
        ///   filtering the results to include only items using the specified access method
        ///   and having a content data type of URI.
        /// </summary>
        /// <param name="accessMethodOid">
        ///   The object identifier representing the access method for filtering.
        /// </param>
        /// <remarks>
        ///   This method does not validate or ensure that the produced values are valid URIs,
        ///   merely that they were encoded as a URI.
        /// </remarks>
        /// <returns>
        ///   The URI-typed access location values that correspond to the requested access method.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="accessMethodOid"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The <see cref="Oid.Value"/> property of the <paramref name="accessMethodOid"/> parameter is
        ///   <see langword="null" /> or the empty string.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The contents of the extension could not be decoded successfully.
        /// </exception>
        /// <seealso cref="EnumerateCAIssuersUris"/>
        /// <seealso cref="EnumerateOcspUris"/>
        public IEnumerable<string> EnumerateUris(Oid accessMethodOid)
        {
            ArgumentNullException.ThrowIfNull(accessMethodOid);
            ArgumentException.ThrowIfNullOrEmpty(accessMethodOid.Value);

            return EnumerateUris(accessMethodOid.Value);
        }

        private IEnumerable<string> EnumerateUrisCore(string accessMethodOid)
        {
            Debug.Assert(_decoded is not null);

            for (int i = 0; i < _decoded.Length; i++)
            {
                string? uri = GetUri(accessMethodOid, ref _decoded[i]);

                if (uri is not null)
                {
                    yield return uri;
                }
            }

            static string? GetUri(string accessMethodOid, ref AccessDescriptionAsn desc)
            {
                if (string.Equals(accessMethodOid, desc.AccessMethod))
                {
                    return desc.AccessLocation.Uri;
                }

                return null;
            }
        }

        /// <summary>
        ///   Enumerates the AccessDescription values whose AccessMethod is CAIssuers
        ///   (1.3.6.1.5.5.7.48.2) and content data type is URI.
        /// </summary>
        /// <returns>
        ///   The URIs corresponding to the locations for the issuing Certificate Authority
        ///   certificate.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The contents of the extension could not be decoded successfully.
        /// </exception>
        /// <seealso cref="EnumerateUris(string)" />
        public IEnumerable<string> EnumerateCAIssuersUris()
        {
            return EnumerateUris(Oids.CertificateAuthorityIssuers);
        }

        /// <summary>
        ///   Enumerates the AccessDescription values whose AccessMethod is OCSP
        ///   (1.3.6.1.5.5.7.48.1) and content data type is URI.
        /// </summary>
        /// <returns>
        ///   The URIs corresponding to the locations for the issuing Certificate Authority
        ///   certificate.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The contents of the extension could not be decoded successfully.
        /// </exception>
        /// <seealso cref="EnumerateUris(string)" />
        public IEnumerable<string> EnumerateOcspUris()
        {
            return EnumerateUris(Oids.OcspEndpoint);
        }

        private static AccessDescriptionAsn[] Decode(byte[] authorityInfoAccessSyntax)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(authorityInfoAccessSyntax, AsnEncodingRules.DER);
                AsnValueReader descriptions = reader.ReadSequence();
                reader.ThrowIfNotEmpty();

                int count = 0;
                AsnValueReader counter = descriptions;

                while (counter.HasData)
                {
                    count++;
                    counter.ReadEncodedValue();
                }

                AccessDescriptionAsn[] decoded = new AccessDescriptionAsn[count];
                count = 0;

                while (descriptions.HasData)
                {
                    AccessDescriptionAsn.Decode(
                        ref descriptions,
                        authorityInfoAccessSyntax,
                        out decoded[count]);

                    count++;
                }

                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static byte[] Encode(
            IEnumerable<string>? ocspUris,
            IEnumerable<string>? caIssuersUris)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            bool empty = true;

            static void WriteAccessMethod(AsnWriter writer, string oid, string value)
            {
                if (value is null)
                {
                    throw new ArgumentException(SR.Cryptography_X509_AIA_NullValue);
                }

                writer.PushSequence();
                writer.WriteObjectIdentifier(oid);

                try
                {
                    writer.WriteCharacterString(
                        UniversalTagNumber.IA5String,
                        value,
                        new Asn1Tag(TagClass.ContextSpecific, 6));
                }
                catch (System.Text.EncoderFallbackException e)
                {
                    throw new CryptographicException(SR.Cryptography_Invalid_IA5String, e);
                }

                writer.PopSequence();
            }

            writer.PushSequence();

            if (ocspUris is not null)
            {
                foreach (string uri in ocspUris)
                {
                    WriteAccessMethod(writer, Oids.OcspEndpoint, uri);
                    empty = false;
                }
            }

            if (caIssuersUris is not null)
            {
                foreach (string uri in caIssuersUris)
                {
                    WriteAccessMethod(writer, Oids.CertificateAuthorityIssuers, uri);
                    empty = false;
                }
            }

            writer.PopSequence();

            if (empty)
            {
                throw new ArgumentException(SR.Cryptography_X509_AIA_MustNotBuildEmpty);
            }

            return writer.Encode();
        }
    }
}
