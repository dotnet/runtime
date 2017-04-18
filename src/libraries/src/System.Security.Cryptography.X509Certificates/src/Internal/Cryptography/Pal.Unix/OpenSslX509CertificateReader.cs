// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslX509CertificateReader : ICertificatePal
    {
        private static DateTimeFormatInfo s_validityDateTimeFormatInfo;

        private SafeX509Handle _cert;
        private SafeEvpPKeyHandle _privateKey;
        private X500DistinguishedName _subjectName;
        private X500DistinguishedName _issuerName;

        internal OpenSslX509CertificateReader(SafeX509Handle handle)
        {
            // X509_check_purpose has the effect of populating the sha1_hash value,
            // and other "initialize" type things.
            bool init = Interop.Crypto.X509CheckPurpose(handle, -1, 0);

            if (!init)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            _cert = handle;
        }

        public bool HasPrivateKey
        {
            get { return _privateKey != null; }
        }

        public IntPtr Handle
        {
            get { return _cert == null ? IntPtr.Zero : _cert.DangerousGetHandle(); }
        }

        internal SafeX509Handle SafeHandle
        {
            get { return _cert; }
        }

        public string Issuer
        {
            get { return IssuerName.Name; }
        }

        public string Subject
        {
            get { return SubjectName.Name; }
        }

        public byte[] Thumbprint
        {
            get
            {
                return Interop.Crypto.GetX509Thumbprint(_cert);
            }
        }

        public string KeyAlgorithm
        {
            get
            {
                IntPtr oidPtr = Interop.Crypto.GetX509PublicKeyAlgorithm(_cert);
                return Interop.Crypto.GetOidValue(oidPtr);
            }
        }

        public byte[] KeyAlgorithmParameters
        {
            get
            {
                return Interop.Crypto.GetX509PublicKeyParameterBytes(_cert);
            }
        }

        public byte[] PublicKeyValue
        {
            get
            {
                IntPtr keyBytesPtr = Interop.Crypto.GetX509PublicKeyBytes(_cert);
                return Interop.Crypto.GetAsn1StringBytes(keyBytesPtr);
            }
        }

        public byte[] SerialNumber
        {
            get
            {
                using (SafeSharedAsn1IntegerHandle serialNumber = Interop.Crypto.X509GetSerialNumber(_cert))
                {
                    byte[] serial = Interop.Crypto.GetAsn1IntegerBytes(serialNumber);

                    // Windows returns this in BigInteger Little-Endian,
                    // OpenSSL returns this in BigInteger Big-Endian.
                    Array.Reverse(serial);
                    return serial;
                }
            }
        }

        public string SignatureAlgorithm
        {
            get
            {
                IntPtr oidPtr = Interop.Crypto.GetX509SignatureAlgorithm(_cert);
                return Interop.Crypto.GetOidValue(oidPtr);
            }
        }

        public DateTime NotAfter
        {
            get
            {
                return ExtractValidityDateTime(Interop.Crypto.GetX509NotAfter(_cert));
            }
        }

        public DateTime NotBefore
        {
            get
            {
                return ExtractValidityDateTime(Interop.Crypto.GetX509NotBefore(_cert));
            }
        }

        public byte[] RawData
        {
            get
            {
                return Interop.Crypto.OpenSslEncode(
                    x => Interop.Crypto.GetX509DerSize(x),
                    (x, buf) => Interop.Crypto.EncodeX509(x, buf),
                    _cert);
            }
        }

        public int Version
        {
            get
            {
                int version = Interop.Crypto.GetX509Version(_cert);

                if (version < 0)
                {
                    throw new CryptographicException();
                }

                // The file encoding is v1(0), v2(1), v3(2).
                // The .NET answers are 1, 2, 3.
                return version + 1;
            }
        }

        public bool Archived
        {
            get { return false; }
            set
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_Unix_X509_PropertyNotSettable, "Archived"));
            }
        }

        public string FriendlyName
        {
            get { return ""; }
            set
            {
                throw new PlatformNotSupportedException(
                  SR.Format(SR.Cryptography_Unix_X509_PropertyNotSettable, "FriendlyName"));
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                if (_subjectName == null)
                {
                    _subjectName = Interop.Crypto.LoadX500Name(Interop.Crypto.X509GetSubjectName(_cert));
                }

                return _subjectName;
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                if (_issuerName == null)
                {
                    _issuerName = Interop.Crypto.LoadX500Name(Interop.Crypto.X509GetIssuerName(_cert));
                }

                return _issuerName;
            }
        }

        public IEnumerable<X509Extension> Extensions
        {
            get
            {
                int extensionCount = Interop.Crypto.X509GetExtCount(_cert);
                X509Extension[] extensions = new X509Extension[extensionCount];

                for (int i = 0; i < extensionCount; i++)
                {
                    IntPtr ext = Interop.Crypto.X509GetExt(_cert, i);

                    Interop.Crypto.CheckValidOpenSslHandle(ext);

                    IntPtr oidPtr = Interop.Crypto.X509ExtensionGetOid(ext);

                    Interop.Crypto.CheckValidOpenSslHandle(oidPtr);

                    string oidValue = Interop.Crypto.GetOidValue(oidPtr);
                    Oid oid = new Oid(oidValue);

                    IntPtr dataPtr = Interop.Crypto.X509ExtensionGetData(ext);

                    Interop.Crypto.CheckValidOpenSslHandle(dataPtr);

                    byte[] extData = Interop.Crypto.GetAsn1StringBytes(dataPtr);
                    bool critical = Interop.Crypto.X509ExtensionGetCritical(ext);

                    extensions[i] = new X509Extension(oid, extData, critical);
                }

                return extensions;
            }
        }

        internal void SetPrivateKey(SafeEvpPKeyHandle privateKey)
        {
            _privateKey = privateKey;
        }

        internal SafeEvpPKeyHandle PrivateKeyHandle
        {
            get { return _privateKey; }
        }

        public RSA GetRSAPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new RSAOpenSsl(_privateKey);
        }

        public DSA GetDSAPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new DSAOpenSsl(_privateKey);
        }

        public ECDsa GetECDsaPublicKey()
        {
            using (SafeEvpPKeyHandle publicKeyHandle = Interop.Crypto.GetX509EvpPublicKey(_cert))
            {
                Interop.Crypto.CheckValidOpenSslHandle(publicKeyHandle);

                return new ECDsaOpenSsl(publicKeyHandle);
            }
        }

        public ECDsa GetECDsaPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new ECDsaOpenSsl(_privateKey);
        }

        private ICertificatePal CopyWithPrivateKey(SafeEvpPKeyHandle privateKey)
        {
            // This could be X509Duplicate for a full clone, but since OpenSSL certificates
            // are functionally immutable (unlike Windows ones) an UpRef is sufficient.
            SafeX509Handle certHandle = Interop.Crypto.X509UpRef(_cert);
            OpenSslX509CertificateReader duplicate = new OpenSslX509CertificateReader(certHandle);

            duplicate.SetPrivateKey(privateKey);
            return duplicate;
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            DSAOpenSsl typedKey = privateKey as DSAOpenSsl;

            if (typedKey != null)
            {
                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }

            DSAParameters dsaParameters = privateKey.ExportParameters(true);

            using (PinAndClear.Track(dsaParameters.X))
            using (typedKey = new DSAOpenSsl(dsaParameters))
            {
                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            ECDsaOpenSsl typedKey = privateKey as ECDsaOpenSsl;

            if (typedKey != null)
            {
                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }

            ECParameters ecParameters = privateKey.ExportParameters(true);

            using (PinAndClear.Track(ecParameters.D))
            using (typedKey = new ECDsaOpenSsl())
            {
                typedKey.ImportParameters(ecParameters);

                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            RSAOpenSsl typedKey = privateKey as RSAOpenSsl;

            if (typedKey != null)
            {
                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }

            RSAParameters rsaParameters = privateKey.ExportParameters(true);

            using (PinAndClear.Track(rsaParameters.D))
            using (PinAndClear.Track(rsaParameters.P))
            using (PinAndClear.Track(rsaParameters.Q))
            using (PinAndClear.Track(rsaParameters.DP))
            using (PinAndClear.Track(rsaParameters.DQ))
            using (PinAndClear.Track(rsaParameters.InverseQ))
            using (typedKey = new RSAOpenSsl(rsaParameters))
            {
                return CopyWithPrivateKey((SafeEvpPKeyHandle)typedKey.DuplicateKeyHandle());
            }
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            using (SafeBioHandle bioHandle = Interop.Crypto.GetX509NameInfo(_cert, (int)nameType, forIssuer))
            {
                if (bioHandle.IsInvalid)
                {
                    return "";
                }

                int bioSize = Interop.Crypto.GetMemoryBioSize(bioHandle);
                // Ensure space for the trailing \0
                var buf = new byte[bioSize + 1];
                int read = Interop.Crypto.BioGets(bioHandle, buf, buf.Length);

                if (read < 0)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return Encoding.UTF8.GetString(buf, 0, read);
            }
        }

        public void AppendPrivateKeyInfo(StringBuilder sb)
        {
            if (!HasPrivateKey)
            {
                return;
            }

            // There's nothing really to say about the key, just acknowledge there is one.
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("[Private Key]");
        }

        public void Dispose()
        {
            if (_privateKey != null)
            {
                _privateKey.Dispose();
                _privateKey = null;
            }

            if (_cert != null)
            {
                _cert.Dispose();
                _cert = null;
            }
        }

        internal OpenSslX509CertificateReader DuplicateHandles()
        {
            SafeX509Handle certHandle = Interop.Crypto.X509UpRef(_cert);
            OpenSslX509CertificateReader duplicate = new OpenSslX509CertificateReader(certHandle);

            if (_privateKey != null)
            {
                SafeEvpPKeyHandle keyHandle = _privateKey.DuplicateHandle();
                duplicate.SetPrivateKey(keyHandle);
            }

            return duplicate;
        }

        internal static DateTime ExtractValidityDateTime(IntPtr validityDatePtr)
        {
            byte[] bytes = Interop.Crypto.GetAsn1StringBytes(validityDatePtr);

            // RFC 5280 (X509v3 - https://tools.ietf.org/html/rfc5280)
            // states that the validity times are either UTCTime:YYMMDDHHMMSSZ (13 bytes)
            // or GeneralizedTime:YYYYMMDDHHMMSSZ (15 bytes).
            // Technically, both UTCTime and GeneralizedTime can have more complicated
            // representations, but X509 restricts them to only the one form each.
            //
            // Furthermore, the UTCTime year values are to be interpreted as 1950-2049.
            //
            // No mention is made in RFC 5280 of different rules for v1 or v2 certificates.

            Debug.Assert(bytes != null);
            Debug.Assert(
                bytes.Length == 13 || bytes.Length == 15,
                "DateTime value should be UTCTime (13 bytes) or GeneralizedTime (15 bytes)");

            Debug.Assert(
                bytes[bytes.Length - 1] == 'Z',
                "DateTime value should end with Z marker");

            if (bytes == null || bytes.Length < 1 || bytes[bytes.Length - 1] != 'Z')
            {
                throw new CryptographicException();
            }

            string dateString = Encoding.ASCII.GetString(bytes);

            if (s_validityDateTimeFormatInfo == null)
            {
                DateTimeFormatInfo validityFormatInfo =
                    (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();

                // Two-digit years are 1950-2049
                validityFormatInfo.Calendar.TwoDigitYearMax = 2049;

                s_validityDateTimeFormatInfo = validityFormatInfo;
            }

            if (bytes.Length == 13)
            {
                DateTime utcTime;

                if (!DateTime.TryParseExact(
                    dateString,
                    "yyMMddHHmmss'Z'",
                    s_validityDateTimeFormatInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out utcTime))
                {
                    throw new CryptographicException();
                }

                return utcTime.ToLocalTime();
            }

            if (bytes.Length == 15)
            {
                DateTime generalizedTime;

                if (!DateTime.TryParseExact(
                    dateString,
                    "yyyyMMddHHmmss'Z'",
                    s_validityDateTimeFormatInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out generalizedTime))
                {
                    throw new CryptographicException();
                }

                return generalizedTime.ToLocalTime();
            }

            throw new CryptographicException();
        }
    }
}
