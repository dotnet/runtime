// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.Cryptography.Pal;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    [Serializable]
    public class X509Certificate : IDisposable, IDeserializationCallback, ISerializable
    {
        public X509Certificate()
        {
        }

        public X509Certificate(byte[] data)
        {
            if (data != null && data.Length != 0)  // For compat reasons, this constructor treats passing a null or empty data set as the same as calling the nullary constructor.
                Pal = CertificatePal.FromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(byte[] rawData, string password)
            : this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));
 
            ValidateKeyStorageFlags(keyStorageFlags);

            Pal = CertificatePal.FromBlob(rawData, password, keyStorageFlags);
        }

        public X509Certificate(IntPtr handle)
        {
            Pal = CertificatePal.FromHandle(handle);
        }

        internal X509Certificate(ICertificatePal pal)
        {
            Debug.Assert(pal != null);
            Pal = pal;
        }

        public X509Certificate(string fileName)
            : this(fileName, null, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(string fileName, string password)
            : this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        public X509Certificate(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            
            ValidateKeyStorageFlags(keyStorageFlags);

            Pal = CertificatePal.FromFile(fileName, password, keyStorageFlags);
        }

        public X509Certificate(X509Certificate cert)
        {
            if (cert == null)
                throw new ArgumentNullException(nameof(cert));

            if (cert.Pal != null)
            {
                Pal = CertificatePal.FromOtherCert(cert);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification = "Public API has already shipped.")]
        public X509Certificate(SerializationInfo info, StreamingContext context) : this()
        {
            byte[] rawData = (byte[])info.GetValue("RawData", typeof(byte[]));
            if (rawData != null)
            {
                Pal = CertificatePal.FromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RawData", Pal?.RawData);
        }

        void IDeserializationCallback.OnDeserialization(object sender) { }

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

        public string Issuer
        {
            get
            {
                ThrowIfInvalid();

                string issuer = _lazyIssuer;
                if (issuer == null)
                    issuer = _lazyIssuer = Pal.Issuer;
                return issuer;
            }
        }

        public string Subject
        {
            get
            {
                ThrowIfInvalid();

                string subject = _lazySubject;
                if (subject == null)
                    subject = _lazySubject = Pal.Subject;
                return subject;
            }
        }

        public void Dispose()
        {
            Dispose(true);
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

        public override bool Equals(object obj)
        {
            X509Certificate other = obj as X509Certificate;
            if (other == null)
                return false;
            return Equals(other);
        }

        public virtual bool Equals(X509Certificate other)
        {
            if (other == null)
                return false;

            if (Pal == null)
                return other.Pal == null;

            if (!Issuer.Equals(other.Issuer))
                return false;

            byte[] thisSerialNumber = GetRawSerialNumber();
            byte[] otherSerialNumber = other.GetRawSerialNumber();

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

        public virtual byte[] Export(X509ContentType contentType, string password)
        {
            if (!(contentType == X509ContentType.Cert || contentType == X509ContentType.SerializedCert || contentType == X509ContentType.Pkcs12))
                throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);

            if (Pal == null)
                throw new CryptographicException(ErrorCode.E_POINTER);  // Not the greatest error, but needed for backward compat.

            using (IExportPal storePal = StorePal.FromCertificate(Pal))
            {
                return storePal.Export(contentType, password);
            }
        }

        public virtual string GetRawCertDataString()
        {
            ThrowIfInvalid();
            return GetRawCertData().ToHexStringUpper();
        }

        public virtual byte[] GetCertHash()
        {
            ThrowIfInvalid();
            return GetRawCertHash().CloneByteArray();
        }

        public virtual string GetCertHashString()
        {
            ThrowIfInvalid();
            return GetRawCertHash().ToHexStringUpper();
        }

        // Only use for internal purposes when the returned byte[] will not be mutated
        private byte[] GetRawCertHash()
        {
            return _lazyCertHash ?? (_lazyCertHash = Pal.Thumbprint);
        }

        public virtual string GetEffectiveDateString()
        {
            return GetNotBefore().ToString();
        }

        public virtual string GetExpirationDateString()
        {
            return GetNotAfter().ToString();
        }

        public virtual string GetFormat()
        {
            return "X509";
        }

        public virtual string GetPublicKeyString()
        {
            return GetPublicKey().ToHexStringUpper();
        }

        public virtual byte[] GetRawCertData()
        {
            ThrowIfInvalid();

            return Pal.RawData.CloneByteArray();
        }

        public override int GetHashCode()
        {
            if (Pal == null)
                return 0;

            byte[] thumbPrint = GetRawCertHash();
            int value = 0;
            for (int i = 0; i < thumbPrint.Length && i < 4; ++i)
            {
                value = value << 8 | thumbPrint[i];
            }
            return value;
        }

        public virtual string GetKeyAlgorithm()
        {
            ThrowIfInvalid();

            string keyAlgorithm = _lazyKeyAlgorithm;
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

        public virtual string GetKeyAlgorithmParametersString()
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

            return GetRawSerialNumber().CloneByteArray();
        }

        public virtual string GetSerialNumberString()
        {
            ThrowIfInvalid();

            return GetRawSerialNumber().ToHexStringUpper();
        }

        // Only use for internal purposes when the returned byte[] will not be mutated
        private byte[] GetRawSerialNumber()
        {
            return _lazySerialNumber ?? (_lazySerialNumber = Pal.SerialNumber);
        }

        [Obsolete("This method has been deprecated.  Please use the Subject property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName()
        {
            return Subject;
        }

        [Obsolete("This method has been deprecated.  Please use the Issuer property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetIssuerName()
        {
            return Issuer;
        }

        public override string ToString()
        {
            return ToString(fVerbose: false);
        }

        public virtual string ToString(bool fVerbose)
        {
            if (fVerbose == false || Pal == null)
                return GetType().ToString();

            StringBuilder sb = new StringBuilder();

            // Subject
            sb.AppendLine("[Subject]");
            sb.Append("  ");
            sb.AppendLine(Subject);

            // Issuer
            sb.AppendLine();
            sb.AppendLine("[Issuer]");
            sb.Append("  ");
            sb.AppendLine(Issuer);

            // Serial Number
            sb.AppendLine();
            sb.AppendLine("[Serial Number]");
            sb.Append("  ");
            byte[] serialNumber = GetSerialNumber();
            Array.Reverse(serialNumber);
            sb.Append(serialNumber.ToHexArrayUpper());
            sb.AppendLine();

            // NotBefore
            sb.AppendLine();
            sb.AppendLine("[Not Before]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(GetNotBefore()));

            // NotAfter
            sb.AppendLine();
            sb.AppendLine("[Not After]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(GetNotAfter()));

            // Thumbprint
            sb.AppendLine();
            sb.AppendLine("[Thumbprint]");
            sb.Append("  ");
            sb.Append(GetRawCertHash().ToHexArrayUpper());
            sb.AppendLine();

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
        }

        /// <summary>
        ///     Convert a date to a string.
        /// 
        ///     Some cultures, specifically using the Um-AlQura calendar cannot convert dates far into
        ///     the future into strings.  If the expiration date of an X.509 certificate is beyond the range
        ///     of one of these cases, we need to fall back to a calendar which can express the dates
        /// </summary>
        internal static string FormatDate(DateTime date)
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

        internal static void ValidateKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            if ((keyStorageFlags & ~KeyStorageFlagsAll) != 0)
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(keyStorageFlags));

            const X509KeyStorageFlags EphemeralPersist =
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.PersistKeySet;

            X509KeyStorageFlags persistenceFlags = keyStorageFlags & EphemeralPersist;

            if (persistenceFlags == EphemeralPersist)
            {
                throw new ArgumentException(
                    SR.Format(SR.Cryptography_X509_InvalidFlagCombination, persistenceFlags),
                    nameof(keyStorageFlags));
            }
        }

        private volatile byte[] _lazyCertHash;
        private volatile string _lazyIssuer;
        private volatile string _lazySubject;
        private volatile byte[] _lazySerialNumber;
        private volatile string _lazyKeyAlgorithm;
        private volatile byte[] _lazyKeyAlgorithmParameters;
        private volatile byte[] _lazyPublicKey;
        private DateTime _lazyNotBefore = DateTime.MinValue;
        private DateTime _lazyNotAfter = DateTime.MinValue;

        internal const X509KeyStorageFlags KeyStorageFlagsAll =
            X509KeyStorageFlags.UserKeySet |
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.Exportable |
            X509KeyStorageFlags.UserProtected |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.EphemeralKeySet;
    }
}
