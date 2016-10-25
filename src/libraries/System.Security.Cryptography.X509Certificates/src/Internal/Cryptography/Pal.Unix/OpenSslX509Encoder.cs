// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslX509Encoder : IX509Pal
    {
        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal)
        {
            switch (oid.Value)
            {
                case Oids.RsaRsa:
                    return BuildRsaPublicKey(encodedKeyValue);
                case Oids.Ecc:
                    return ((OpenSslX509CertificateReader)certificatePal).GetECDsaPublicKey();
            }

            // NotSupportedException is what desktop and CoreFx-Windows throw in this situation.
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flags)
        {
            return X500NameEncoder.X500DistinguishedNameDecode(encodedDistinguishedName, true, flags);
        }

        public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
        {
            return X500NameEncoder.X500DistinguishedNameEncode(distinguishedName, flag);
        }

        public string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
        {
            return X500NameEncoder.X500DistinguishedNameDecode(
                encodedDistinguishedName,
                true,
                multiLine ? X500DistinguishedNameFlags.UseNewLines : X500DistinguishedNameFlags.None,
                multiLine);
        }

        public X509ContentType GetCertContentType(byte[] rawData)
        {
            {
                ICertificatePal certPal;

                if (CertificatePal.TryReadX509Der(rawData, out certPal) ||
                    CertificatePal.TryReadX509Pem(rawData, out certPal))
                {
                    certPal.Dispose();

                    return X509ContentType.Cert;
                }
            }

            if (PkcsFormatReader.IsPkcs7(rawData))
            {
                return X509ContentType.Pkcs7;
            }

            {
                OpenSslPkcs12Reader pfx;

                if (OpenSslPkcs12Reader.TryRead(rawData, out pfx))
                {
                    pfx.Dispose();
                    return X509ContentType.Pkcs12;
                }
            }

            // Unsupported format.
            // Windows throws new CryptographicException(CRYPT_E_NO_MATCH)
            throw new CryptographicException();
        }

        public X509ContentType GetCertContentType(string fileName)
        {
            // If we can't open the file, fail right away.
            using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(fileBio);

                int bioPosition = Interop.Crypto.BioTell(fileBio);
                Debug.Assert(bioPosition >= 0);

                // X509ContentType.Cert
                {
                    ICertificatePal certPal;

                    if (CertificatePal.TryReadX509Der(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);

                    if (CertificatePal.TryReadX509Pem(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
                }

                // X509ContentType.Pkcs7
                {
                    if (PkcsFormatReader.IsPkcs7Der(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);

                    if (PkcsFormatReader.IsPkcs7Pem(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
                }

                // X509ContentType.Pkcs12 (aka PFX)
                {
                    OpenSslPkcs12Reader pkcs12Reader;

                    if (OpenSslPkcs12Reader.TryRead(fileBio, out pkcs12Reader))
                    {
                        pkcs12Reader.Dispose();

                        return X509ContentType.Pkcs12;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
                }
            }

            // Unsupported format.
            // Windows throws new CryptographicException(CRYPT_E_NO_MATCH)
            throw new CryptographicException();
        }

        public byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
        {
            // The numeric values of X509KeyUsageFlags mean that if we interpret it as a little-endian
            // ushort it will line up with the flags in the spec.
            ushort ushortValue = unchecked((ushort)(int)keyUsages);
            byte[] data = BitConverter.GetBytes(ushortValue);

            // RFC 3280 section 4.2.1.3 (https://tools.ietf.org/html/rfc3280#section-4.2.1.3) defines
            // digitalSignature (0) through decipherOnly (8), making 9 named bits.
            const int namedBitsCount = 9;

            // The expected output of this method isn't the SEQUENCE value, but just the payload bytes.
            byte[][] segments = DerEncoder.SegmentedEncodeNamedBitList(data, namedBitsCount);
            Debug.Assert(segments.Length == 3);
            return ConcatenateArrays(segments);
        }

        public void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
        {
            using (SafeAsn1BitStringHandle bitString = Interop.Crypto.DecodeAsn1BitString(encoded, encoded.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bitString);

                byte[] decoded = Interop.Crypto.GetAsn1StringBytes(bitString.DangerousGetHandle());

                // Only 9 bits are defined.
                if (decoded.Length > 2)
                {
                    throw new CryptographicException();
                }

                // DER encodings of BIT_STRING values number the bits as
                // 01234567 89 (big endian), plus a number saying how many bits of the last byte were padding.
                //
                // So digitalSignature (0) doesn't mean 2^0 (0x01), it means the most significant bit
                // is set in this byte stream.
                //
                // BIT_STRING values are compact.  So a value of cRLSign (6) | keyEncipherment (2), which
                // is 0b0010001 => 0b0010 0010 (1 bit padding) => 0x22 encoded is therefore
                // 0x02 (length remaining) 0x01 (1 bit padding) 0x22.
                //
                // OpenSSL's d2i_ASN1_BIT_STRING is going to take that, and return 0x22.  0x22 lines up
                // exactly with X509KeyUsageFlags.CrlSign (0x20) | X509KeyUsageFlags.KeyEncipherment (0x02)
                //
                // Once the decipherOnly (8) bit is added to the mix, the values become:
                // 0b001000101 => 0b0010 0010 1000 0000 (7 bits padding)
                // { 0x03 0x07 0x22 0x80 }
                // And OpenSSL returns new byte[] { 0x22 0x80 }
                //
                // The value of X509KeyUsageFlags.DecipherOnly is 0x8000.  0x8000 in a little endian
                // representation is { 0x00 0x80 }.  This means that the DER storage mechanism has effectively
                // ended up being little-endian for BIT_STRING values.  Untwist the bytes, and now the bits all
                // line up with the existing X509KeyUsageFlags.

                int value = 0;

                if (decoded.Length > 0)
                {
                    value = decoded[0];
                }

                if (decoded.Length > 1)
                {
                    value |= decoded[1] << 8;
                }

                keyUsages = (X509KeyUsageFlags)value;
            }
        }

        public bool SupportsLegacyBasicConstraintsExtension
        {
            get { return false; }
        }

        public byte[] EncodeX509BasicConstraints2Extension(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint)
        {
            //BasicConstraintsSyntax::= SEQUENCE {
            //    cA BOOLEAN DEFAULT FALSE,
            //    pathLenConstraint INTEGER(0..MAX) OPTIONAL,
            //    ... }

            List<byte[][]> segments = new List<byte[][]>(2);

            if (certificateAuthority)
            {
                segments.Add(DerEncoder.SegmentedEncodeBoolean(true));
            }

            if (hasPathLengthConstraint)
            {
                byte[] pathLengthBytes = BitConverter.GetBytes(pathLengthConstraint);
                // Little-Endian => Big-Endian
                Array.Reverse(pathLengthBytes);
                segments.Add(DerEncoder.SegmentedEncodeUnsignedInteger(pathLengthBytes));
            }

            return DerEncoder.ConstructSequence(segments);
        }

        public void DecodeX509BasicConstraintsExtension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            // No RFC nor ITU document describes the layout of the 2.5.29.10 structure,
            // and OpenSSL doesn't have a decoder for it, either.
            //
            // Since it was never published as a standard (2.5.29.19 replaced it before publication)
            // there shouldn't be too many people upset that we can't decode it for them on Unix.
            throw new PlatformNotSupportedException(SR.NotSupported_LegacyBasicConstraints);
        }

        public void DecodeX509BasicConstraints2Extension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            if (!Interop.Crypto.DecodeX509BasicConstraints2Extension(
                encoded,
                encoded.Length,
                out certificateAuthority,
                out hasPathLengthConstraint,
                out pathLengthConstraint))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        public byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages)
        {
            //extKeyUsage EXTENSION ::= {
            //    SYNTAX SEQUENCE SIZE(1..MAX) OF KeyPurposeId
            //    IDENTIFIED BY id - ce - extKeyUsage }
            //
            //KeyPurposeId::= OBJECT IDENTIFIER

            List<byte[][]> segments = new List<byte[][]>(usages.Count);

            foreach (Oid usage in usages)
            {
                segments.Add(DerEncoder.SegmentedEncodeOid(usage));
            }

            return DerEncoder.ConstructSequence(segments);
        }

        public void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
        {
            OidCollection oids = new OidCollection();

            using (SafeEkuExtensionHandle eku = Interop.Crypto.DecodeExtendedKeyUsage(encoded, encoded.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(eku);

                int count = Interop.Crypto.GetX509EkuFieldCount(eku);

                for (int i = 0; i < count; i++)
                {
                    IntPtr oidPtr = Interop.Crypto.GetX509EkuField(eku, i);

                    if (oidPtr == IntPtr.Zero)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    string oidValue = Interop.Crypto.GetOidValue(oidPtr);

                    oids.Add(new Oid(oidValue));
                }
            }

            usages = oids;
        }

        public byte[] EncodeX509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier)
        {
            //subjectKeyIdentifier EXTENSION ::= {
            //    SYNTAX SubjectKeyIdentifier
            //    IDENTIFIED BY id - ce - subjectKeyIdentifier }
            //
            //SubjectKeyIdentifier::= KeyIdentifier
            //
            //KeyIdentifier ::= OCTET STRING

            byte[][] segments = DerEncoder.SegmentedEncodeOctetString(subjectKeyIdentifier);

            // The extension is not a sequence, just the octet string
            return ConcatenateArrays(segments);
        }

        public void DecodeX509SubjectKeyIdentifierExtension(byte[] encoded, out byte[] subjectKeyIdentifier)
        {
            subjectKeyIdentifier = DecodeX509SubjectKeyIdentifierExtension(encoded);
        }

        internal static byte[] DecodeX509SubjectKeyIdentifierExtension(byte[] encoded)
        {
            DerSequenceReader reader = DerSequenceReader.CreateForPayload(encoded);
            return reader.ReadOctetString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required for Compat")]
        public byte[] ComputeCapiSha1OfPublicKey(PublicKey key)
        {
            // The CapiSha1 value is the SHA-1 of the SubjectPublicKeyInfo field, inclusive
            // of the DER structural bytes.

            //SubjectPublicKeyInfo::= SEQUENCE {
            //    algorithm AlgorithmIdentifier{ { SupportedAlgorithms} },
            //    subjectPublicKey BIT STRING,
            //    ... }
            //
            //AlgorithmIdentifier{ ALGORITHM: SupportedAlgorithms} ::= SEQUENCE {
            //    algorithm ALGORITHM.&id({ SupportedAlgorithms}),
            //    parameters ALGORITHM.&Type({ SupportedAlgorithms}
            //    { @algorithm}) OPTIONAL,
            //    ... }
            //
            //ALGORITHM::= CLASS {
            //    &Type OPTIONAL,
            //    &id OBJECT IDENTIFIER UNIQUE }
            //WITH SYNTAX {
            //    [&Type]
            //IDENTIFIED BY &id }

            // key.EncodedKeyValue corresponds to SubjectPublicKeyInfo.subjectPublicKey, except it
            // has had the BIT STRING envelope removed.
            //
            // key.EncodedParameters corresponds to AlgorithmIdentifier.Parameters precisely
            // (DER NULL for RSA, DER Constructed SEQUENCE for DSA)

            byte[] empty = Array.Empty<byte>();
            byte[][] algorithmOid = DerEncoder.SegmentedEncodeOid(key.Oid);
            // Because ConstructSegmentedSequence doesn't look to see that it really is tag+length+value (but does check
            // that the array has length 3), just hide the joined TLV triplet in the last element.
            byte[][] segmentedParameters = { empty, empty, key.EncodedParameters.RawData };
            byte[][] algorithmIdentifier = DerEncoder.ConstructSegmentedSequence(algorithmOid, segmentedParameters);
            byte[][] subjectPublicKey = DerEncoder.SegmentedEncodeBitString(key.EncodedKeyValue.RawData);

            using (SHA1 hash = SHA1.Create())
            {
                return hash.ComputeHash(
                    DerEncoder.ConstructSequence(
                        algorithmIdentifier,
                        subjectPublicKey));
            }
        }

        private static RSA BuildRsaPublicKey(byte[] encodedData)
        {
            using (SafeRsaHandle rsaHandle = Interop.Crypto.DecodeRsaPublicKey(encodedData, encodedData.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(rsaHandle);

                RSAParameters rsaParameters = Interop.Crypto.ExportRsaParameters(rsaHandle, false);
                RSA rsa = new RSAOpenSsl();
                rsa.ImportParameters(rsaParameters);
                return rsa;
            }
        }

        private static byte[] ConcatenateArrays(byte[][] segments)
        {
            int length = 0;

            foreach (byte[] segment in segments)
            {
                length += segment.Length;
            }

            byte[] concatenated = new byte[length];

            int offset = 0;

            foreach (byte[] segment in segments)
            {
                Buffer.BlockCopy(segment, 0, concatenated, offset, segment.Length);
                offset += segment.Length;
            }

            return concatenated;
        }
    }
}
