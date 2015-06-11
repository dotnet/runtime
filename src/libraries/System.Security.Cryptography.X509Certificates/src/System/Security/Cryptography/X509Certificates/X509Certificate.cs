// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Certificate : IDisposable
    {
        public X509Certificate()
        {
            return;
        }

        public X509Certificate(byte[] data)
        {
            if (data != null && data.Length != 0)  // For compat reasons, this constructor treats passing a null or empty data set as the same as calling the nullary constructor.
                Pal = CertificatePal.FromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(byte[] rawData, String password)
            : this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(byte[] rawData, String password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, "rawData");
            if ((keyStorageFlags & ~KeyStorageFlagsAll) != 0)
                throw new ArgumentException(SR.Argument_InvalidFlag, "keyStorageFlags");

            Pal = CertificatePal.FromBlob(rawData, password, keyStorageFlags);
        }

        public X509Certificate(IntPtr handle)
        {
            Pal = CertificatePal.FromHandle(handle);
        }

        public X509Certificate(String fileName)
            : this(fileName, null, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(String fileName, String password)
            : this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(String fileName, String password, X509KeyStorageFlags keyStorageFlags)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if ((keyStorageFlags & ~KeyStorageFlagsAll) != 0)
                throw new ArgumentException(SR.Argument_InvalidFlag, "keyStorageFlags");

            Pal = CertificatePal.FromFile(fileName, password, keyStorageFlags);
        }

        public IntPtr Handle
        {
            get
            {
                if (Pal == null)
                    return IntPtr.Zero;
                else
                    return Pal.Handle;
            }
        }

        public String Issuer
        {
            get
            {
                ThrowIfInvalid();

                String issuer = _lazyIssuer;
                if (issuer == null)
                    issuer = _lazyIssuer = Pal.Issuer;
                return issuer;
            }
        }

        public String Subject
        {
            get
            {
                ThrowIfInvalid();

                String subject = _lazySubject;
                if (subject == null)
                    subject = _lazySubject = Pal.Subject;
                return subject;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ICertificatePal pal = Pal;
                Pal = null;
                if (pal != null)
                    pal.Dispose();
            }
        }

        public override bool Equals(Object obj)
        {
            X509Certificate other = obj as X509Certificate;
            if (other == null)
                return false;
            return this.Equals(other);
        }

        public virtual bool Equals(X509Certificate other)
        {
            if (other == null)
                return false;

            if (this.Pal == null)
                return other.Pal == null;

            if (!this.Issuer.Equals(other.Issuer))
                return false;

            byte[] thisSerialNumber = this.GetSerialNumber();
            byte[] otherSerialNumber = other.GetSerialNumber();
            if (thisSerialNumber.Length != otherSerialNumber.Length)
                return false;
            for (int i = 0; i < thisSerialNumber.Length; i++)
            {
                if (thisSerialNumber[i] != otherSerialNumber[i])
                    return false;
            }

            return true;
        }

        public virtual byte[] Export(X509ContentType contentType)
        {
            return Export(contentType, null);
        }

        public virtual byte[] Export(X509ContentType contentType, String password)
        {
            if (!(contentType == X509ContentType.Cert || contentType == X509ContentType.SerializedCert || contentType == X509ContentType.Pkcs12))
                throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);

            if (Pal == null)
                throw new CryptographicException(ErrorCode.E_POINTER);  // Not the greatest error, but needed for backward compat.

            using (IStorePal storePal = StorePal.FromCertificate(Pal))
            {
                return storePal.Export(contentType, password);
            }
        }

        public virtual byte[] GetCertHash()
        {
            ThrowIfInvalid();

            byte[] certHash = _lazyCertHash;
            if (certHash == null)
                _lazyCertHash = certHash = Pal.Thumbprint;

            return certHash.CloneByteArray();
        }

        public virtual String GetFormat()
        {
            return "X509";
        }

        public override int GetHashCode()
        {
            if (Pal == null)
                return 0;

            byte[] thumbPrint = GetCertHash();
            int value = 0;
            for (int i = 0; i < thumbPrint.Length && i < 4; ++i)
            {
                value = value << 8 | thumbPrint[i];
            }
            return value;
        }

        public virtual String GetKeyAlgorithm()
        {
            ThrowIfInvalid();

            String keyAlgorithm = _lazyKeyAlgorithm;
            if (keyAlgorithm == null)
                keyAlgorithm = _lazyKeyAlgorithm = Pal.KeyAlgorithm;
            return keyAlgorithm;
        }

        public virtual byte[] GetKeyAlgorithmParameters()
        {
            ThrowIfInvalid();

            byte[] keyAlgorithmParameters = _lazyKeyAlgorithmParameters;
            if (keyAlgorithmParameters == null)
                keyAlgorithmParameters = _lazyKeyAlgorithmParameters = Pal.KeyAlgorithmParameters;
            return keyAlgorithmParameters.CloneByteArray();
        }

        public virtual String GetKeyAlgorithmParametersString()
        {
            ThrowIfInvalid();

            byte[] keyAlgorithmParameters = GetKeyAlgorithmParameters();
            return keyAlgorithmParameters.ToHexStringUpper();
        }

        public virtual byte[] GetPublicKey()
        {
            ThrowIfInvalid();

            byte[] publicKey = _lazyPublicKey;
            if (publicKey == null)
                publicKey = _lazyPublicKey = Pal.PublicKeyValue;
            return publicKey.CloneByteArray();
        }

        public virtual byte[] GetSerialNumber()
        {
            ThrowIfInvalid();

            byte[] serialNumber = _lazySerialNumber;
            if (serialNumber == null)
                serialNumber = _lazySerialNumber = Pal.SerialNumber;
            return serialNumber.CloneByteArray();
        }

        public override String ToString()
        {
            return ToString(fVerbose: false);
        }

        public virtual String ToString(bool fVerbose)
        {
            if (fVerbose == false || Pal == null)
                return GetType().ToString();

            StringBuilder sb = new StringBuilder();

            // Subject
            sb.AppendLine("[Subject]");
            sb.Append("  ");
            sb.AppendLine(this.Subject);

            // Issuer
            sb.AppendLine();
            sb.AppendLine("[Issuer]");
            sb.Append("  ");
            sb.AppendLine(this.Issuer);

            // Serial Number
            sb.AppendLine();
            sb.AppendLine("[Serial Number]");
            sb.Append("  ");
            byte[] serialNumber = this.GetSerialNumber();
            Array.Reverse(serialNumber);
            sb.AppendLine(serialNumber.ToHexStringUpper());

            // NotBefore
            sb.AppendLine();
            sb.AppendLine("[Not Before]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(this.GetNotBefore()));

            // NotAfter
            sb.AppendLine();
            sb.AppendLine("[Not After]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(this.GetNotAfter()));

            // Thumbprint
            sb.AppendLine();
            sb.AppendLine("[Thumbprint]");
            sb.Append("  ");
            sb.AppendLine(this.GetCertHash().ToHexStringUpper());

            return sb.ToString();
        }

        internal ICertificatePal Pal { get; private set; }

        internal DateTime GetNotAfter()
        {
            ThrowIfInvalid();

            DateTime notAfter = _lazyNotAfter;
            if (notAfter == DateTime.MinValue)
                notAfter = _lazyNotAfter = Pal.NotAfter;
            return notAfter;
        }

        internal DateTime GetNotBefore()
        {
            ThrowIfInvalid();

            DateTime notBefore = _lazyNotBefore;
            if (notBefore == DateTime.MinValue)
                notBefore = _lazyNotBefore = Pal.NotBefore;
            return notBefore;
        }

        internal void ThrowIfInvalid()
        {
            if (Pal == null)
                throw new CryptographicException(SR.Format(SR.Cryptography_InvalidHandle, "m_safeCertContext")); // Keeping "m_safeCertContext" string for backward compat sake.
            return;
        }

        /// <summary>
        ///     Convert a date to a string.
        /// 
        ///     Some cultures, specifically using the Um-AlQura calendar cannot convert dates far into
        ///     the future into strings.  If the expiration date of an X.509 certificate is beyond the range
        ///     of one of these these cases, we need to fall back to a calendar which can express the dates
        /// </summary>
        internal static String FormatDate(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            if (!culture.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0))
            {
                // The most common case of culture failing to work is in the Um-AlQuara calendar. In this case,
                // we can fall back to the Hijri calendar, otherwise fall back to the invariant culture.
                if (culture.DateTimeFormat.Calendar is UmAlQuraCalendar)
                {
                    culture = culture.Clone() as CultureInfo;
                    culture.DateTimeFormat.Calendar = new HijriCalendar();
                }
                else
                {
                    culture = CultureInfo.InvariantCulture;
                }
            }

            return date.ToString(culture);
        }

        private volatile byte[] _lazyCertHash;
        private volatile String _lazyIssuer;
        private volatile String _lazySubject;
        private volatile byte[] _lazySerialNumber;
        private volatile String _lazyKeyAlgorithm;
        private volatile byte[] _lazyKeyAlgorithmParameters;
        private volatile byte[] _lazyPublicKey;
        private DateTime _lazyNotBefore = DateTime.MinValue;
        private DateTime _lazyNotAfter = DateTime.MinValue;

        private const X509KeyStorageFlags KeyStorageFlagsAll = (X509KeyStorageFlags)0x1f;
    }
}
