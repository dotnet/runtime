// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Certificate : IDisposable, IDeserializationCallback, ISerializable
    {
        private volatile byte[]? _lazyCertHash;
        private volatile string? _lazyIssuer;
        private volatile string? _lazySubject;
        private volatile byte[]? _lazySerialNumber;
        private volatile string? _lazyKeyAlgorithm;
        private volatile byte[]? _lazyKeyAlgorithmParameters;
        private volatile byte[]? _lazyPublicKey;
        private DateTime _lazyNotBefore = DateTime.MinValue;
        private DateTime _lazyNotAfter = DateTime.MinValue;

        public virtual void Reset()
        {
            _lazyCertHash = null;
            _lazyIssuer = null;
            _lazySubject = null;
            _lazySerialNumber = null;
            _lazyKeyAlgorithm = null;
            _lazyKeyAlgorithmParameters = null;
            _lazyPublicKey = null;
            _lazyNotBefore = DateTime.MinValue;
            _lazyNotAfter = DateTime.MinValue;

            ICertificatePalCore? pal = Pal;
            if (pal != null)
            {
                Pal = null;
                pal.Dispose();
            }
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [UnsupportedOSPlatform("browser")]
        public X509Certificate()
        {
        }

        // Null turns into the empty span here, which is correct for compat.
        [UnsupportedOSPlatform("browser")]
        public X509Certificate(byte[] data)
            : this(new ReadOnlySpan<byte>(data))
        {
        }

        private protected X509Certificate(ReadOnlySpan<byte> data)
        {
            if (!data.IsEmpty)
            {
                // For compat reasons, this constructor treats passing a null or empty data set as the same as calling the nullary constructor.
                using (var safePasswordHandle = new SafePasswordHandle((string?)null))
                {
                    Pal = CertificatePal.FromBlob(data, safePasswordHandle, X509KeyStorageFlags.DefaultKeySet);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(byte[] rawData, string? password)
            : this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, SecureString? password)
            : this(rawData, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags);
            }
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags);
            }
        }

        private protected X509Certificate(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromBlob(rawData, safePasswordHandle, keyStorageFlags);
            }
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(IntPtr handle)
        {
            Pal = CertificatePal.FromHandle(handle);
        }

        internal X509Certificate(ICertificatePalCore pal)
        {
            Debug.Assert(pal != null);
            Pal = pal;
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(string fileName)
            : this(fileName, (string?)null, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(string fileName, string? password)
            : this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate(string fileName, SecureString? password)
            : this(fileName, password, X509KeyStorageFlags.DefaultKeySet)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromFile(fileName, safePasswordHandle, keyStorageFlags);
            }
        }

        private protected X509Certificate(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromFile(fileName, safePasswordHandle, keyStorageFlags);
            }
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
#pragma warning disable SYSLIB0026
        public X509Certificate(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags) : this()
#pragma warning restore SYSLIB0026
        {
            ArgumentNullException.ThrowIfNull(fileName);

            ValidateKeyStorageFlags(keyStorageFlags);

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                Pal = CertificatePal.FromFile(fileName, safePasswordHandle, keyStorageFlags);
            }
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate(X509Certificate cert)
        {
            ArgumentNullException.ThrowIfNull(cert);

            if (cert.Pal != null)
            {
                Pal = CertificatePal.FromOtherCert(cert);
            }
        }

#pragma warning disable SYSLIB0026
        public X509Certificate(SerializationInfo info, StreamingContext context) : this()
#pragma warning restore SYSLIB0026
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("browser")]
        public static X509Certificate CreateFromCertFile(string filename)
        {
            return new X509Certificate(filename);
        }

        [UnsupportedOSPlatform("browser")]
        public static X509Certificate CreateFromSignedFile(string filename)
        {
            return new X509Certificate(filename);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            throw new PlatformNotSupportedException();
        }

        public IntPtr Handle => Pal is null ? IntPtr.Zero : Pal.Handle;

        public string Issuer
        {
            get
            {
                ThrowIfInvalid();

                return _lazyIssuer ??= Pal.Issuer;
            }
        }

        public string Subject
        {
            get
            {
                ThrowIfInvalid();

                return _lazySubject ??= Pal.Subject;
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
                Reset();
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is X509Certificate other && Equals(other);

        public virtual bool Equals([NotNullWhen(true)] X509Certificate? other)
        {
            if (other is null)
                return false;

            if (Pal is null)
                return other.Pal is null;

            if (!Issuer.Equals(other.Issuer))
                return false;

            ReadOnlySpan<byte> thisSerialNumber = GetRawSerialNumber();
            ReadOnlySpan<byte> otherSerialNumber = other.GetRawSerialNumber();

            return thisSerialNumber.SequenceEqual(otherSerialNumber);
        }

        public virtual byte[] Export(X509ContentType contentType)
        {
            return Export(contentType, (string?)null);
        }

        public virtual byte[] Export(X509ContentType contentType, string? password)
        {
            VerifyContentType(contentType);

            if (Pal == null)
                throw new CryptographicException(ErrorCode.E_POINTER);  // Not the greatest error, but needed for backward compat.

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                return Pal.Export(contentType, safePasswordHandle);
            }
        }

        [System.CLSCompliantAttribute(false)]
        public virtual byte[] Export(X509ContentType contentType, SecureString? password)
        {
            VerifyContentType(contentType);

            if (Pal == null)
                throw new CryptographicException(ErrorCode.E_POINTER);  // Not the greatest error, but needed for backward compat.

            using (var safePasswordHandle = new SafePasswordHandle(password))
            {
                return Pal.Export(contentType, safePasswordHandle);
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

        public virtual byte[] GetCertHash(HashAlgorithmName hashAlgorithm)
        {
            ThrowIfInvalid();
            return GetCertHash(hashAlgorithm, Pal);
        }

        private static byte[] GetCertHash(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
        {
            return HashOneShotHelpers.HashData(hashAlgorithm, certPal.RawData);
        }

        public virtual bool TryGetCertHash(
            HashAlgorithmName hashAlgorithm,
            Span<byte> destination,
            out int bytesWritten)
        {
            ThrowIfInvalid();

            return HashOneShotHelpers.TryHashData(hashAlgorithm, Pal.RawData, destination, out bytesWritten);
        }

        public virtual string GetCertHashString()
        {
            ThrowIfInvalid();
            return GetRawCertHash().ToHexStringUpper();
        }

        public virtual string GetCertHashString(HashAlgorithmName hashAlgorithm)
        {
            ThrowIfInvalid();

            return GetCertHashString(hashAlgorithm, Pal);
        }

        internal static string GetCertHashString(HashAlgorithmName hashAlgorithm, ICertificatePalCore certPal)
        {
            return GetCertHash(hashAlgorithm, certPal).ToHexStringUpper();
        }

        // Only use for internal purposes when the returned byte[] will not be mutated
        private byte[] GetRawCertHash()
        {
            return _lazyCertHash ??= Pal!.Thumbprint;
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

            return _lazyKeyAlgorithm ??= Pal.KeyAlgorithm;
        }

        public virtual byte[] GetKeyAlgorithmParameters()
        {
            ThrowIfInvalid();

            byte[] keyAlgorithmParameters = _lazyKeyAlgorithmParameters ??= Pal.KeyAlgorithmParameters;
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

            byte[] publicKey = _lazyPublicKey ??= Pal.PublicKeyValue;
            return publicKey.CloneByteArray();
        }

        public virtual byte[] GetSerialNumber()
        {
            ThrowIfInvalid();
            byte[] serialNumber = GetRawSerialNumber().CloneByteArray();
            // PAL always returns big-endian, GetSerialNumber returns little-endian
            Array.Reverse(serialNumber);
            return serialNumber;
        }

        public virtual string GetSerialNumberString()
        {
            ThrowIfInvalid();
            // PAL always returns big-endian, GetSerialNumberString returns big-endian too
            return GetRawSerialNumber().ToHexStringUpper();
        }

        // Only use for internal purposes when the returned byte[] will not be mutated
        private byte[] GetRawSerialNumber() => _lazySerialNumber ??= Pal!.SerialNumber;

        [Obsolete("X509Certificate.GetName has been deprecated. Use the Subject property instead.")]
        public virtual string GetName()
        {
            ThrowIfInvalid();
            return Pal.LegacySubject;
        }

        [Obsolete("X509Certificate.GetIssuerName has been deprecated. Use the Issuer property instead.")]
        public virtual string GetIssuerName()
        {
            ThrowIfInvalid();
            return Pal.LegacyIssuer;
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

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(byte[] rawData)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        [System.CLSCompliantAttribute(false)]
        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(string fileName)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        [System.CLSCompliantAttribute(false)]
        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public virtual void Import(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            throw new PlatformNotSupportedException(SR.NotSupported_ImmutableX509Certificate);
        }

        internal ICertificatePalCore? Pal { get; private set; }

        internal DateTime GetNotAfter()
        {
            ThrowIfInvalid();

            DateTime notAfter = _lazyNotAfter;

            if (notAfter == DateTime.MinValue)
            {
                notAfter = _lazyNotAfter = Pal.NotAfter;
            }

            return notAfter;
        }

        internal DateTime GetNotBefore()
        {
            ThrowIfInvalid();

            DateTime notBefore = _lazyNotBefore;

            if (notBefore == DateTime.MinValue)
            {
                notBefore = _lazyNotBefore = Pal.NotBefore;
            }
            return notBefore;
        }

        [MemberNotNull(nameof(Pal))]
        internal void ThrowIfInvalid()
        {
            if (Pal is null)
                throw new CryptographicException(SR.Format(SR.Cryptography_InvalidHandle, "m_safeCertContext")); // Keeping "m_safeCertContext" string for backward compat sake.
        }

        /// <summary>
        ///     Convert a date to a string.
        ///
        ///     Some cultures, specifically using the Um-AlQura calendar cannot convert dates far into
        ///     the future into strings.  If the expiration date of an X.509 certificate is beyond the range
        ///     of one of these cases, we need to fall back to a calendar which can express the dates
        /// </summary>
        protected static string FormatDate(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            if (!culture.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0))
            {
                // The most common case of culture failing to work is in the Um-AlQuara calendar. In this case,
                // we can fall back to the Hijri calendar, otherwise fall back to the invariant culture.
                if (culture.DateTimeFormat.Calendar is UmAlQuraCalendar)
                {
                    culture = (culture.Clone() as CultureInfo)!;
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

        private static void VerifyContentType(X509ContentType contentType)
        {
            if (!(contentType == X509ContentType.Cert || contentType == X509ContentType.SerializedCert || contentType == X509ContentType.Pkcs12))
                throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
        }

        internal const X509KeyStorageFlags KeyStorageFlagsAll =
            X509KeyStorageFlags.UserKeySet |
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.Exportable |
            X509KeyStorageFlags.UserProtected |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.EphemeralKeySet;
    }
}
