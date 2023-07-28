// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class CertificateRevocationListBuilder
    {
        /// <summary>
        ///   Builds a CRL Distribution Point Extension with the specified retrieval URIs.
        /// </summary>
        /// <param name="uris">
        ///   The URIs to include as distribution points for the relevant Certificate
        ///   Revocation List (CRL).
        /// </param>
        /// <param name="critical">
        ///   <see langword="true" /> to mark the extension as critical;
        ///   otherwise, <see langword="false" />.
        ///   The default is <see langword="false" />.
        /// </param>
        /// <returns>
        ///   An object suitable for use as a CRL Distribution Point Extension.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uris"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="uris"/> contains a <see langword="null" /> value.
        ///   </para>
        ///   <para>- or -</para>
        ///   <para>
        ///     <paramref name="uris"/> is empty.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   One of the values in <paramref name="uris"/>
        ///   contains characters outside of the International Alphabet 5 (IA5) character space
        ///   (which is equivalent to 7-bit US-ASCII).
        /// </exception>
        public static X509Extension BuildCrlDistributionPointExtension(
            IEnumerable<string> uris,
            bool critical = false)
        {
            ArgumentNullException.ThrowIfNull(uris);

            // CRLDistributionPoints ::= SEQUENCE SIZE (1..MAX) OF DistributionPoint
            //
            // DistributionPoint::= SEQUENCE {
            //    distributionPoint[0]     DistributionPointName OPTIONAL,
            //    reasons[1]     ReasonFlags OPTIONAL,
            //    cRLIssuer[2]     GeneralNames OPTIONAL }

            // DistributionPointName::= CHOICE {
            //    fullName[0]     GeneralNames,
            //    nameRelativeToCRLIssuer[1]     RelativeDistinguishedName }

            AsnWriter? writer = null;

            foreach (string uri in uris)
            {
                if (uri is null)
                {
                    throw new ArgumentException(SR.Cryptography_X509_CDP_NullValue, nameof(uris));
                }

                if (writer is null)
                {
                    writer = new AsnWriter(AsnEncodingRules.DER);
                    // CRLDistributionPoints
                    writer.PushSequence();
                }

                // DistributionPoint
                using (writer.PushSequence())
                {
                    // DistributionPoint/DistributionPointName EXPLICIT [0]
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        // DistributionPointName/GeneralName
                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                        {
                            // GeneralName/Uri
                            try
                            {
                                writer.WriteCharacterString(
                                    UniversalTagNumber.IA5String,
                                    uri,
                                    new Asn1Tag(TagClass.ContextSpecific, 6));
                            }
                            catch (System.Text.EncoderFallbackException e)
                            {
                                throw new CryptographicException(SR.Cryptography_Invalid_IA5String, e);
                            }
                        }
                    }
                }
            }

            if (writer is null)
            {
                throw new ArgumentException(SR.Cryptography_X509_CDP_MustNotBuildEmpty, nameof(uris));
            }

            // CRLDistributionPoints
            writer.PopSequence();

            byte[] encoded = writer.Encode();
            return new X509Extension(Oids.CrlDistributionPointsOid, encoded, critical);
        }
    }
}
