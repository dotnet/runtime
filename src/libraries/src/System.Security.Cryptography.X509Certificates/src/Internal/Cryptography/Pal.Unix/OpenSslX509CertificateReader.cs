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

        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            return new OpenSslX509CertificateReader(Interop.Crypto.X509UpRef(handle));
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            // Ensure private key is copied
            OpenSslX509CertificateReader certPal = (OpenSslX509CertificateReader)cert.Pal;
            return certPal.DuplicateHandles();
        }

        public static ICertificatePal FromBlob(byte[] rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal cert;
            Exception openSslException;

            if (TryReadX509Der(rawData, out cert) ||
                TryReadX509Pem(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs7Der(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs7Pem(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs12(rawData, password, out cert, out openSslException))
            {
                if (cert == null)
                {
                    // Empty collection, most likely.
                    throw new CryptographicException();
                }

                return cert;
            }

            // Unsupported
            Debug.Assert(openSslException != null);
            throw openSslException;
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            // If we can't open the file, fail right away.
            using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(fileBio);

                return FromBio(fileBio, password);
            }
        }

        private static ICertificatePal FromBio(SafeBioHandle bio, SafePasswordHandle password)
        {
            int bioPosition = Interop.Crypto.BioTell(bio);

            Debug.Assert(bioPosition >= 0);

            ICertificatePal certPal;
            if (TryReadX509Pem(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (TryReadX509Der(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Pem(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Der(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            // Capture the exception so in case of failure, the call to BioSeek does not override it.
            Exception openSslException;
            if (PkcsFormatReader.TryReadPkcs12(bio, password, out certPal, out openSslException))
            {
                return certPal;
            }

            // Since we aren't going to finish reading, leaving the buffer where it was when we got
            // it seems better than leaving it in some arbitrary other position.
            // 
            // Use BioSeek directly for the last seek attempt, because any failure here should instead
            // report the already created (but not yet thrown) exception.
            if (Interop.Crypto.BioSeek(bio, bioPosition) < 0)
            {
                Interop.Crypto.ErrClearError();
            }

            Debug.Assert(openSslException != null);
            throw openSslException;
        }

        internal static void RewindBio(SafeBioHandle bio, int bioPosition)
        {
            int ret = Interop.Crypto.BioSeek(bio, bioPosition);

            if (ret < 0)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        internal static bool TryReadX509Der(byte[] rawData, out ICertificatePal certPal)
        {
            SafeX509Handle certHandle = Interop.Crypto.DecodeX509(rawData, rawData.Length);

            if (certHandle.IsInvalid)
            {
                certHandle.Dispose();
                certPal = null;
                Interop.Crypto.ErrClearError();
                return false;
            }

            certPal = new OpenSslX509CertificateReader(certHandle);
            return true;
        }

        internal static bool TryReadX509Pem(SafeBioHandle bio, out ICertificatePal certPal)
        {
            SafeX509Handle cert = Interop.Crypto.PemReadX509FromBioAux(bio);

            if (cert.IsInvalid)
            {
                cert.Dispose();
                certPal = null;
                Interop.Crypto.ErrClearError();
                return false;
            }

            certPal = new OpenSslX509CertificateReader(cert);
            return true;
        }

        internal static bool TryReadX509PemNoAux(SafeBioHandle bio, out ICertificatePal certPal)
        {
            SafeX509Handle cert = Interop.Crypto.PemReadX509FromBio(bio);

            if (cert.IsInvalid)
            {
                cert.Dispose();
                certPal = null;
                Interop.Crypto.ErrClearError();
                return false;
            }

            certPal = new OpenSslX509CertificateReader(cert);
            return true;
        }

        internal static bool TryReadX509Pem(byte[] rawData, out ICertificatePal certPal)
        {
            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                if (Interop.Crypto.BioWrite(bio, rawData, rawData.Length) != rawData.Length)
                {
                    Interop.Crypto.ErrClearError();
                }

                return TryReadX509Pem(bio, out certPal);
            }
        }

        internal static bool TryReadX509Der(SafeBioHandle bio, out ICertificatePal fromBio)
        {
            SafeX509Handle cert = Interop.Crypto.ReadX509AsDerFromBio(bio);

            if (cert.IsInvalid)
            {
                cert.Dispose();
                fromBio = null;
                Interop.Crypto.ErrClearError();
                return false;
            }

            fromBio = new OpenSslX509CertificateReader(cert);
            return true;
        }

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

        public string Issuer => IssuerName.Name;

        public string Subject => SubjectName.Name;

        public string LegacyIssuer => IssuerName.Decode(X500DistinguishedNameFlags.None);

        public string LegacySubject => SubjectName.Decode(X500DistinguishedNameFlags.None);

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
                    return Interop.Crypto.GetAsn1IntegerBytes(serialNumber);
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

        internal static ArraySegment<byte> FindFirstExtension(SafeX509Handle cert, string oidValue)
        {
            int nid = Interop.Crypto.ResolveRequiredNid(oidValue);

            using (SafeSharedAsn1OctetStringHandle data = Interop.Crypto.X509FindExtensionData(cert, nid))
            {
                if (data.IsInvalid)
                {
                    return default;
                }

                return Interop.Crypto.RentAsn1StringBytes(data.DangerousGetHandle());
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

        public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                return storePal.Export (contentType, password);
            }
        }
    }
}
