// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class CertificateRevocationListBuilder
    {
        /// <summary>
        ///   Decodes the specified Certificate Revocation List (CRL) and produces
        ///   a <see cref="CertificateRevocationListBuilder" /> with all of the revocation
        ///   entries from the decoded CRL.
        /// </summary>
        /// <param name="currentCrl">
        ///   The DER-encoded CRL to decode.
        /// </param>
        /// <param name="currentCrlNumber">
        ///   When this method returns, contains the CRL sequence number from the decoded CRL.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   A new builder that has the same revocation entries as the decoded CRL.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="currentCrl" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="currentCrl" /> could not be decoded.
        ///   </para>
        ///   <para>- or -</para>
        ///   <para>
        ///     <paramref name="currentCrl" /> decoded successfully, but decoding did not
        ///     need all of the bytes provided in the array.
        ///   </para>
        /// </exception>
        public static CertificateRevocationListBuilder Load(byte[] currentCrl, out BigInteger currentCrlNumber)
        {
            ArgumentNullException.ThrowIfNull(currentCrl);

            CertificateRevocationListBuilder ret = Load(
                new ReadOnlySpan<byte>(currentCrl),
                out BigInteger crlNumber,
                out int bytesConsumed);

            if (bytesConsumed != currentCrl.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            currentCrlNumber = crlNumber;
            return ret;
        }

        /// <summary>
        ///   Decodes the specified Certificate Revocation List (CRL) and produces
        ///   a <see cref="CertificateRevocationListBuilder" /> with all of the revocation
        ///   entries from the decoded CRL.
        /// </summary>
        /// <param name="currentCrl">
        ///   The DER-encoded CRL to decode.
        /// </param>
        /// <param name="currentCrlNumber">
        ///   When this method returns, contains the CRL sequence number from the decoded CRL.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, contains the number of bytes that were read from
        ///   <paramref name="currentCrl"/> while decoding.
        /// </param>
        /// <returns>
        ///   A new builder that has the same revocation entries as the decoded CRL.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <paramref name="currentCrl" /> could not be decoded.
        /// </exception>
        public static CertificateRevocationListBuilder Load(
            ReadOnlySpan<byte> currentCrl,
            out BigInteger currentCrlNumber,
            out int bytesConsumed)
        {
            List<RevokedCertificate> list = new();
            BigInteger crlNumber = 0;
            int payloadLength;

            try
            {
                AsnValueReader reader = new AsnValueReader(currentCrl, AsnEncodingRules.DER);
                payloadLength = reader.PeekEncodedValue().Length;

                AsnValueReader certificateList = reader.ReadSequence();
                AsnValueReader tbsCertList = certificateList.ReadSequence();
                AlgorithmIdentifierAsn.Decode(ref certificateList, ReadOnlyMemory<byte>.Empty, out _);

                if (!certificateList.TryReadPrimitiveBitString(out _, out _))
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                certificateList.ThrowIfNotEmpty();

                int version = 0;

                if (tbsCertList.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
                {
                    // https://datatracker.ietf.org/doc/html/rfc5280#section-5.1 says the only
                    // version values are v1 (0) and v2 (1).
                    //
                    // Since v1 (0) is supposed to not write down the version value, v2 (1) is the
                    // only legal value to read.
                    if (!tbsCertList.TryReadInt32(out version) || version != 1)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }

                AlgorithmIdentifierAsn.Decode(ref tbsCertList, ReadOnlyMemory<byte>.Empty, out _);
                // X500DN
                tbsCertList.ReadSequence();

                // thisUpdate
                ReadX509Time(ref tbsCertList);

                // nextUpdate
                ReadX509TimeOpt(ref tbsCertList);

                AsnValueReader revokedCertificates = default;

                if (tbsCertList.HasData && tbsCertList.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
                {
                    revokedCertificates = tbsCertList.ReadSequence();
                }

                if (version > 0 && tbsCertList.HasData)
                {
                    AsnValueReader crlExtensionsExplicit = tbsCertList.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    AsnValueReader crlExtensions = crlExtensionsExplicit.ReadSequence();
                    crlExtensionsExplicit.ThrowIfNotEmpty();

                    while (crlExtensions.HasData)
                    {
                        AsnValueReader extension = crlExtensions.ReadSequence();
                        Oid? extnOid = Oids.GetSharedOrNullOid(ref extension);

                        if (extnOid is null)
                        {
                            extension.ReadObjectIdentifier();
                        }

                        if (extension.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
                        {
                            extension.ReadBoolean();
                        }

                        if (!extension.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> extnValue))
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        // Since we're only matching against OIDs that come from GetSharedOrNullOid
                        // we can use ReferenceEquals and skip the Value string equality check in
                        // the Oid.ValueEquals extension method (as it will always be preempted by
                        // the ReferenceEquals or will evaulate to false).
                        if (ReferenceEquals(extnOid, Oids.CrlNumberOid))
                        {
                            AsnValueReader crlNumberReader = new AsnValueReader(
                                extnValue,
                                AsnEncodingRules.DER);

                            crlNumber = crlNumberReader.ReadInteger();
                            crlNumberReader.ThrowIfNotEmpty();
                        }
                    }
                }

                tbsCertList.ThrowIfNotEmpty();

                while (revokedCertificates.HasData)
                {
                    RevokedCertificate revokedCertificate = new RevokedCertificate(ref revokedCertificates, version);
                    list.Add(revokedCertificate);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            bytesConsumed = payloadLength;
            currentCrlNumber = crlNumber;
            return new CertificateRevocationListBuilder(list);
        }

        /// <summary>
        ///   Decodes the specified Certificate Revocation List (CRL) and produces
        ///   a <see cref="CertificateRevocationListBuilder" /> with all of the revocation
        ///   entries from the decoded CRL.
        /// </summary>
        /// <param name="currentCrl">
        ///   The PEM-encoded CRL to decode.
        /// </param>
        /// <param name="currentCrlNumber">
        ///   When this method returns, contains the CRL sequence number from the decoded CRL.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   A new builder that has the same revocation entries as the decoded CRL.
        /// </returns>
        /// <remarks>
        ///   This loads the first well-formed PEM found with an <c>X509 CRL</c> label.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="currentCrl" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="currentCrl" /> did not contain a well-formed PEM payload with
        ///     an <c>X509 CRL</c> label.
        ///   </para>
        ///   <para>- or -</para>
        ///   <para>
        ///     <paramref name="currentCrl" /> could not be decoded.
        ///   </para>
        /// </exception>
        public static CertificateRevocationListBuilder LoadPem(string currentCrl, out BigInteger currentCrlNumber)
        {
            ArgumentNullException.ThrowIfNull(currentCrl);

            return LoadPem(currentCrl.AsSpan(), out currentCrlNumber);
        }

        /// <summary>
        ///   Decodes the specified Certificate Revocation List (CRL) and produces
        ///   a <see cref="CertificateRevocationListBuilder" /> with all of the revocation
        ///   entries from the decoded CRL.
        /// </summary>
        /// <param name="currentCrl">
        ///   The PEM-encoded CRL to decode.
        /// </param>
        /// <param name="currentCrlNumber">
        ///   When this method returns, contains the CRL sequence number from the decoded CRL.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   A new builder that has the same revocation entries as the decoded CRL.
        /// </returns>
        /// <remarks>
        ///   This loads the first well-formed PEM found with an <c>X509 CRL</c> label.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="currentCrl" /> did not contain a well-formed PEM payload with
        ///     an <c>X509 CRL</c> label.
        ///   </para>
        ///   <para>- or -</para>
        ///   <para>
        ///     <paramref name="currentCrl" /> could not be decoded.
        ///   </para>
        /// </exception>
        public static CertificateRevocationListBuilder LoadPem(ReadOnlySpan<char> currentCrl, out BigInteger currentCrlNumber)
        {
            foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(currentCrl))
            {
                if (contents[fields.Label].SequenceEqual(PemLabels.X509CertificateRevocationList))
                {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(fields.DecodedDataLength);

                    if (!Convert.TryFromBase64Chars(contents[fields.Base64Data], rented, out int bytesWritten))
                    {
                        Debug.Fail("Base64Decode failed, but PemEncoding said it was legal");
                        throw new UnreachableException();
                    }

                    CertificateRevocationListBuilder ret = Load(
                        rented.AsSpan(0, bytesWritten),
                        out currentCrlNumber,
                        out int bytesConsumed);

                    Debug.Assert(bytesConsumed == bytesWritten);
                    ArrayPool<byte>.Shared.Return(rented);
                    return ret;
                }
            }

            throw new CryptographicException(SR.Cryptography_NoPemOfLabel, PemLabels.X509CertificateRevocationList);
        }
    }
}
