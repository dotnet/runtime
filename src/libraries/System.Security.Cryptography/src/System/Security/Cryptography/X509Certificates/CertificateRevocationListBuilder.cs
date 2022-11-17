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
    /// <summary>
    ///   Facilitates building a Certificate Revocation List (CRL).
    /// </summary>
    public sealed partial class CertificateRevocationListBuilder
    {
        private readonly List<RevokedCertificate> _revoked;
        private AsnWriter? _writer;

        /// <summary>
        ///   Initializes a new instance of the <see cref="CertificateRevocationListBuilder" /> class.
        /// </summary>
        public CertificateRevocationListBuilder()
        {
            _revoked = new List<RevokedCertificate>();
        }

        private CertificateRevocationListBuilder(List<RevokedCertificate> revoked)
        {
            Debug.Assert(revoked != null);
            _revoked = revoked;
        }

        /// <summary>
        ///   Adds the specified certificate to the revocation list with an optional revocation time
        ///   and an optional revocation reason.
        /// </summary>
        /// <param name="certificate">
        ///   The certificate to revoke.
        /// </param>
        /// <param name="revocationTime">
        ///   The time the certificate was revoked,
        ///   or <see langword="null" /> to use the current system time.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <param name="reason">
        ///   The reason why the certificate was revoked,
        ///   or <see langword="null" /> to not include a reason.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <remarks>
        ///   This method does not check that the certificate issuer is appropriate for the
        ///   CRL that is being built, the certificate is just used for extracting the serial
        ///   number.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificate"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="reason" /> is not a supported revocation reason.
        /// </exception>
        public void AddEntry(
            X509Certificate2 certificate,
            DateTimeOffset? revocationTime = null,
            X509RevocationReason? reason = null)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            AddEntry(certificate.SerialNumberBytes.Span, revocationTime, reason);
        }

        /// <summary>
        ///   Adds the specified serial number to the revocation list with an optional revocation time
        ///   and an optional revocation reason.
        /// </summary>
        /// <param name="serialNumber">
        ///   The serial number of the certificate to revoke.
        /// </param>
        /// <param name="revocationTime">
        ///   The time the certificate was revoked,
        ///   or <see langword="null" /> to use the current system time.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <param name="reason">
        ///   The reason why the certificate was revoked,
        ///   or <see langword="null" /> to not include a reason.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="serialNumber"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="reason" /> is not a supported revocation reason.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is empty.
        /// </exception>
        public void AddEntry(
            byte[] serialNumber,
            DateTimeOffset? revocationTime = null,
            X509RevocationReason? reason = null)
        {
            ArgumentNullException.ThrowIfNull(serialNumber);

            AddEntry(new ReadOnlySpan<byte>(serialNumber), revocationTime, reason);
        }

        /// <summary>
        ///   Adds the specified serial number to the revocation list with an optional revocation time
        ///   and an optional revocation reason.
        /// </summary>
        /// <param name="serialNumber">
        ///   The serial number of the certificate to revoke.
        /// </param>
        /// <param name="revocationTime">
        ///   The time the certificate was revoked,
        ///   or <see langword="null" /> to use the current system time.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <param name="reason">
        ///   The reason why the certificate was revoked,
        ///   or <see langword="null" /> to not include a reason.
        ///   The default is <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="reason" /> is not a supported revocation reason.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="serialNumber"/> is empty.
        /// </exception>
        public void AddEntry(
            ReadOnlySpan<byte> serialNumber,
            DateTimeOffset? revocationTime = null,
            X509RevocationReason? reason = null)
        {
            if (serialNumber.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(serialNumber));

            if (serialNumber.Length > 1)
            {
                if ((serialNumber[0] == 0x00 && serialNumber[1] < 0x80) ||
                    (serialNumber[0] == 0xFF && serialNumber[1] > 0x7F))
                {
                    throw new ArgumentException(
                        SR.Argument_InvalidSerialNumberBytes,
                        nameof(serialNumber));
                }
            }

            byte[]? extensions = null;

            if (reason.HasValue)
            {
                X509RevocationReason reasonValue = reason.GetValueOrDefault();

                switch (reasonValue)
                {
                    case X509RevocationReason.Unspecified:
                    case X509RevocationReason.KeyCompromise:
                    case X509RevocationReason.CACompromise:
                    case X509RevocationReason.AffiliationChanged:
                    case X509RevocationReason.Superseded:
                    case X509RevocationReason.CessationOfOperation:
                    case X509RevocationReason.CertificateHold:
                    case X509RevocationReason.PrivilegeWithdrawn:
                    case X509RevocationReason.WeakAlgorithmOrKey:
                        break;
                    default:
                        // Includes RemoveFromCrl (no delta CRL support)
                        // Includes AaCompromise (no support for attribute certificates)
                        throw new ArgumentOutOfRangeException(
                            nameof(reason),
                            reasonValue,
                            SR.Cryptography_CRLBuilder_ReasonNotSupported);
                }

                AsnWriter writer = (_writer ??= new AsnWriter(AsnEncodingRules.DER));
                writer.Reset();

                // SEQUENCE OF Extension
                using (writer.PushSequence())
                {
                    // Extension
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(Oids.CrlReasons);

                        using (writer.PushOctetString())
                        {
                            writer.WriteEnumeratedValue(reasonValue);
                        }
                    }
                }

                extensions = writer.Encode();
            }

            _revoked.Add(
                new RevokedCertificate
                {
                    Serial = serialNumber.ToArray(),
                    RevocationTime = (revocationTime ?? DateTimeOffset.UtcNow).ToUniversalTime(),
                    Extensions = extensions,
                });
        }

        /// <summary>
        ///   Removes the specified serial number from the revocation list.
        /// </summary>
        /// <param name="serialNumber">
        ///   The serial number to remove.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the serial number was found in the list and was removed;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///   This method assumes that the same serial number is not present on the list more than once,
        ///   and thus stops at the first match.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="serialNumber"/> is <see langword="null" />.
        /// </exception>
        public bool RemoveEntry(byte[] serialNumber)
        {
            ArgumentNullException.ThrowIfNull(serialNumber);

            return RemoveEntry(new ReadOnlySpan<byte>(serialNumber));
        }

        /// <summary>
        ///   Removes the specified serial number from the revocation list.
        /// </summary>
        /// <param name="serialNumber">
        ///   The serial number to remove.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the serial number was found in the list and was removed;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///   This method assumes that the same serial number is not present on the list more than once,
        ///   and thus stops at the first match.
        /// </remarks>
        public bool RemoveEntry(ReadOnlySpan<byte> serialNumber)
        {
            for (int i = _revoked.Count - 1; i >= 0; i--)
            {
                if (serialNumber.SequenceEqual(_revoked[i].Serial))
                {
                    _revoked.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static DateTimeOffset ReadX509Time(ref AsnValueReader reader)
        {
            if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime))
            {
                return reader.ReadUtcTime();
            }

            return reader.ReadGeneralizedTime();
        }

        private static DateTimeOffset? ReadX509TimeOpt(ref AsnValueReader reader)
        {
            if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime))
            {
                return reader.ReadUtcTime();
            }

            if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.GeneralizedTime))
            {
                return reader.ReadGeneralizedTime();
            }

            return null;
        }

        private static void WriteX509Time(AsnWriter writer, DateTimeOffset time)
        {
            DateTimeOffset timeUtc = time.ToUniversalTime();
            int year = timeUtc.Year;

            if (year >= 1950 && year < 2050)
            {
                writer.WriteUtcTime(timeUtc);
            }
            else
            {
                writer.WriteGeneralizedTime(time, omitFractionalSeconds: true);
            }
        }

        private struct RevokedCertificate
        {
            internal byte[] Serial;
            internal DateTimeOffset RevocationTime;
            internal byte[]? Extensions;

            internal RevokedCertificate(ref AsnValueReader reader, int version)
            {
                AsnValueReader revokedCertificate = reader.ReadSequence();
                Serial = revokedCertificate.ReadIntegerBytes().ToArray();
                RevocationTime = ReadX509Time(ref revokedCertificate);
                Extensions = null;

                if (version > 0 && revokedCertificate.HasData)
                {
                    if (!revokedCertificate.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    Extensions = revokedCertificate.ReadEncodedValue().ToArray();
                }

                revokedCertificate.ThrowIfNotEmpty();
            }
        }
    }
}
