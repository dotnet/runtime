// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509Certificate2 : X509Certificate
    {
        private volatile byte[]? _lazyRawData;
        private volatile Oid? _lazySignatureAlgorithm;
        private volatile int _lazyVersion;
        private volatile X500DistinguishedName? _lazySubjectName;
        private volatile X500DistinguishedName? _lazyIssuerName;
        private volatile PublicKey? _lazyPublicKey;
        private volatile AsymmetricAlgorithm? _lazyPrivateKey;
        private volatile X509ExtensionCollection? _lazyExtensions;
        private static readonly string[] s_EcPublicKeyPrivateKeyLabels = { PemLabels.EcPrivateKey, PemLabels.Pkcs8PrivateKey };
        private static readonly string[] s_RsaPublicKeyPrivateKeyLabels = { PemLabels.RsaPrivateKey, PemLabels.Pkcs8PrivateKey };
        private static readonly string[] s_DsaPublicKeyPrivateKeyLabels = { PemLabels.Pkcs8PrivateKey };

        public override void Reset()
        {
            _lazyRawData = null;
            _lazySignatureAlgorithm = null;
            _lazyVersion = 0;
            _lazySubjectName = null;
            _lazyIssuerName = null;
            _lazyPublicKey = null;
            _lazyPrivateKey = null;
            _lazyExtensions = null;

            base.Reset();
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [UnsupportedOSPlatform("browser")]
        public X509Certificate2()
            : base()
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(byte[] rawData)
            : base(rawData)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(byte[] rawData, string? password)
            : base(rawData, password)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, SecureString? password)
            : base(rawData, password)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
            : base(rawData, password, keyStorageFlags)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
            : base(rawData, password, keyStorageFlags)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509Certificate2"/> class from certificate data.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to process.
        /// </param>
        /// <exception cref="CryptographicException">An error with the certificate occurs.</exception>
        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(ReadOnlySpan<byte> rawData)
            : base(rawData)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="X509Certificate2"/> class from certificate data,
        ///   a password, and key storage flags.
        /// </summary>
        /// <param name="rawData">
        ///   The certificate data to process.
        /// </param>
        /// <param name="password">
        ///   The password required to access the certificate data.
        /// </param>
        /// <param name="keyStorageFlags">
        ///   A bitwise combination of the enumeration values that control where and how to import the certificate.
        /// </param>
        /// <exception cref="CryptographicException">An error with the certificate occurs.</exception>
        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
            : base(rawData, password, keyStorageFlags)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(IntPtr handle)
            : base(handle)
        {
        }

        internal X509Certificate2(ICertificatePal pal)
            : base(pal)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(string fileName)
            : base(fileName)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(string fileName, string? password)
            : base(fileName, password)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, SecureString? password)
            : base(fileName, password)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
        {
        }

        [UnsupportedOSPlatform("browser")]
        [CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
            : base(fileName, password, keyStorageFlags)
        {
        }

        [UnsupportedOSPlatform("browser")]
        public X509Certificate2(X509Certificate certificate)
            : base(certificate)
        {
        }

        protected X509Certificate2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            throw new PlatformNotSupportedException();
        }

        internal new ICertificatePal Pal => (ICertificatePal)base.Pal!; // called base ctors guaranteed to initialize

        public bool Archived
        {
            get
            {
                ThrowIfInvalid();

                return Pal.Archived;
            }

            [SupportedOSPlatform("windows")]
            set
            {
                ThrowIfInvalid();

                Pal.Archived = value;
            }
        }

        public X509ExtensionCollection Extensions
        {
            get
            {
                ThrowIfInvalid();

                X509ExtensionCollection? extensions = _lazyExtensions;
                if (extensions == null)
                {
                    extensions = new X509ExtensionCollection();
                    foreach (X509Extension extension in Pal.Extensions)
                    {
                        X509Extension? customExtension = CreateCustomExtensionIfAny(extension.Oid!);
                        if (customExtension == null)
                        {
                            extensions.Add(extension);
                        }
                        else
                        {
                            customExtension.CopyFrom(extension);
                            extensions.Add(customExtension);
                        }
                    }
                    _lazyExtensions = extensions;
                }
                return extensions;
            }
        }

        public string FriendlyName
        {
            get
            {
                ThrowIfInvalid();

                return Pal.FriendlyName;
            }

            [SupportedOSPlatform("windows")]
            set
            {
                ThrowIfInvalid();

                Pal.FriendlyName = value;
            }
        }

        public bool HasPrivateKey
        {
            get
            {
                ThrowIfInvalid();

                return Pal.HasPrivateKey;
            }
        }

        [Obsolete(Obsoletions.X509CertificatePrivateKeyMessage, DiagnosticId = Obsoletions.X509CertificatePrivateKeyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AsymmetricAlgorithm? PrivateKey
        {
            get
            {
                ThrowIfInvalid();

                if (!HasPrivateKey)
                    return null;

                _lazyPrivateKey ??= GetKeyAlgorithm() switch
                {
                    Oids.Rsa => Pal.GetRSAPrivateKey(),
                    Oids.Dsa => Pal.GetDSAPrivateKey(),

                    // This includes ECDSA, because an Oids.EcPublicKey key can be
                    // many different algorithm kinds, not necessarily with mutual exclusion.
                    // Plus, .NET Framework only supports RSA and DSA in this property.
                    _ => throw new NotSupportedException(SR.NotSupported_KeyAlgorithm),
                };

                return _lazyPrivateKey;
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                ThrowIfInvalid();

                return _lazyIssuerName ??= Pal.IssuerName;
            }
        }

        public DateTime NotAfter => GetNotAfter();
        public DateTime NotBefore => GetNotBefore();

        public PublicKey PublicKey
        {
            get
            {
                ThrowIfInvalid();

                PublicKey? publicKey = _lazyPublicKey;

                if (publicKey == null)
                {
                    string keyAlgorithmOid = GetKeyAlgorithm();
                    byte[] parameters = Pal.KeyAlgorithmParameters;
                    byte[] keyValue = Pal.PublicKeyValue;
                    Oid oid = new Oid(keyAlgorithmOid);
                    publicKey = _lazyPublicKey = new PublicKey(oid, new AsnEncodedData(oid, parameters), new AsnEncodedData(oid, keyValue));
                }

                return publicKey;
            }
        }

        public byte[] RawData => RawDataMemory.ToArray();

        /// <summary>
        /// Gets the raw data of a certificate.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="RawData" />, this does not create a fresh copy of the data
        /// every time.
        /// </remarks>
        public ReadOnlyMemory<byte> RawDataMemory
        {
            get
            {
                ThrowIfInvalid();

                return _lazyRawData ??= Pal.RawData;
            }
        }

        public string SerialNumber => GetSerialNumberString();

        public Oid SignatureAlgorithm
        {
            get
            {
                ThrowIfInvalid();

                return _lazySignatureAlgorithm ??= new Oid(Pal.SignatureAlgorithm, null);
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                ThrowIfInvalid();

                return _lazySubjectName ??= Pal.SubjectName;
            }
        }

        public string Thumbprint
        {
            get
            {
                return GetCertHashString();
            }
        }

        public int Version
        {
            get
            {
                ThrowIfInvalid();

                int version = _lazyVersion;
                if (version == 0)
                    version = _lazyVersion = Pal.Version;
                return version;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public static X509ContentType GetCertContentType(byte[] rawData)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            return X509Pal.Instance.GetCertContentType(rawData);
        }

        /// <summary>
        ///   Indicates the type of certificate contained in the provided data.
        /// </summary>
        /// <param name="rawData">
        ///   The data to identify.
        /// </param>
        /// <returns>
        ///   One of the enumeration values that indicate the content type of the provided data.
        /// </returns>
        [UnsupportedOSPlatform("browser")]
        public static X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
        {
            if (rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            return X509Pal.Instance.GetCertContentType(rawData);
        }

        [UnsupportedOSPlatform("browser")]
        public static X509ContentType GetCertContentType(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            // .NET Framework compat: The .NET Framework expands the filename to a full path for the purpose of performing a CAS permission check. While CAS is not present here,
            // we still need to call GetFullPath() so we get the same exception behavior if the fileName is bad.
            _ = Path.GetFullPath(fileName);

            return X509Pal.Instance.GetCertContentType(fileName);
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            return Pal.GetNameInfo(nameType, forIssuer);
        }

        public override string ToString() => base.ToString(fVerbose: true);

        public override string ToString(bool verbose)
        {
            if (verbose == false || Pal == null)
                return ToString();

            StringBuilder sb = new StringBuilder();

            // Version
            sb.AppendLine("[Version]");
            sb.Append("  V");
            sb.Append(Version);

            // Subject
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("[Subject]");
            sb.Append("  ");
            sb.Append(SubjectName.Name);
            string simpleName = GetNameInfo(X509NameType.SimpleName, false);
            if (simpleName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Simple Name: ");
                sb.Append(simpleName);
            }
            string emailName = GetNameInfo(X509NameType.EmailName, false);
            if (emailName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Email Name: ");
                sb.Append(emailName);
            }
            string upnName = GetNameInfo(X509NameType.UpnName, false);
            if (upnName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("UPN Name: ");
                sb.Append(upnName);
            }
            string dnsName = GetNameInfo(X509NameType.DnsName, false);
            if (dnsName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("DNS Name: ");
                sb.Append(dnsName);
            }

            // Issuer
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("[Issuer]");
            sb.Append("  ");
            sb.Append(IssuerName.Name);
            simpleName = GetNameInfo(X509NameType.SimpleName, true);
            if (simpleName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Simple Name: ");
                sb.Append(simpleName);
            }
            emailName = GetNameInfo(X509NameType.EmailName, true);
            if (emailName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Email Name: ");
                sb.Append(emailName);
            }
            upnName = GetNameInfo(X509NameType.UpnName, true);
            if (upnName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("UPN Name: ");
                sb.Append(upnName);
            }
            dnsName = GetNameInfo(X509NameType.DnsName, true);
            if (dnsName.Length > 0)
            {
                sb.AppendLine();
                sb.Append("  ");
                sb.Append("DNS Name: ");
                sb.Append(dnsName);
            }

            // Serial Number
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("[Serial Number]");
            sb.Append("  ");
            sb.AppendLine(SerialNumber);

            // NotBefore
            sb.AppendLine();
            sb.AppendLine("[Not Before]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(NotBefore));

            // NotAfter
            sb.AppendLine();
            sb.AppendLine("[Not After]");
            sb.Append("  ");
            sb.AppendLine(FormatDate(NotAfter));

            // Thumbprint
            sb.AppendLine();
            sb.AppendLine("[Thumbprint]");
            sb.Append("  ");
            sb.AppendLine(Thumbprint);

            // Signature Algorithm
            sb.AppendLine();
            sb.AppendLine("[Signature Algorithm]");
            sb.Append("  ");
            sb.Append(SignatureAlgorithm.FriendlyName);
            sb.Append('(');
            sb.Append(SignatureAlgorithm.Value);
            sb.AppendLine(")");

            // Public Key
            sb.AppendLine();
            sb.Append("[Public Key]");
            // It could throw if it's some user-defined CryptoServiceProvider
            try
            {
                PublicKey pubKey = PublicKey;

                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Algorithm: ");
                sb.Append(pubKey.Oid.FriendlyName);
                // So far, we only support RSACryptoServiceProvider & DSACryptoServiceProvider Keys
                try
                {
                    sb.AppendLine();
                    sb.Append("  ");
                    sb.Append("Length: ");

                    using (RSA? pubRsa = this.GetRSAPublicKey())
                    {
                        if (pubRsa != null)
                        {
                            sb.Append(pubRsa.KeySize);
                        }
                    }
                }
                catch (NotSupportedException)
                {
                }

                sb.AppendLine();
                sb.Append("  ");
                sb.Append("Key Blob: ");
                sb.AppendLine(pubKey.EncodedKeyValue.Format(true));

                sb.Append("  ");
                sb.Append("Parameters: ");
                sb.Append(pubKey.EncodedParameters.Format(true));
            }
            catch (CryptographicException)
            {
            }

            // Private key
            Pal.AppendPrivateKeyInfo(sb);

            // Extensions
            X509ExtensionCollection extensions = Extensions;
            if (extensions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append("[Extensions]");
                foreach (X509Extension extension in extensions)
                {
                    try
                    {
                        sb.AppendLine();
                        sb.Append("* ");
                        sb.Append(extension.Oid!.FriendlyName);
                        sb.Append('(');
                        sb.Append(extension.Oid.Value);
                        sb.Append("):");

                        sb.AppendLine();
                        sb.Append("  ");
                        sb.Append(extension.Format(true));
                    }
                    catch (CryptographicException)
                    {
                    }
                }
            }

            sb.AppendLine();
            return sb.ToString();
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(byte[] rawData)
        {
            base.Import(rawData);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(rawData, password, keyStorageFlags);
        }

        [System.CLSCompliantAttribute(false)]
        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(rawData, password, keyStorageFlags);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(string fileName)
        {
            base.Import(fileName);
        }

        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(fileName, password, keyStorageFlags);
        }

        [System.CLSCompliantAttribute(false)]
        [Obsolete(Obsoletions.X509CertificateImmutableMessage, DiagnosticId = Obsoletions.X509CertificateImmutableDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override void Import(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(fileName, password, keyStorageFlags);
        }

        public bool Verify()
        {
            ThrowIfInvalid();

            using (var chain = new X509Chain())
            {
                // Use the default vales of chain.ChainPolicy including:
                //  RevocationMode = X509RevocationMode.Online
                //  RevocationFlag = X509RevocationFlag.ExcludeRoot
                //  VerificationFlags = X509VerificationFlags.NoFlag
                //  VerificationTime = DateTime.Now
                //  UrlRetrievalTimeout = new TimeSpan(0, 0, 0)

                bool verified = chain.Build(this, throwOnException: false);

                for (int i = 0; i < chain.ChainElements.Count; i++)
                {
                    chain.ChainElements[i].Certificate.Dispose();
                }

                return verified;
            }
        }

        /// <summary>
        /// Gets the <see cref="ECDiffieHellman" /> public key from this certificate.
        /// </summary>
        /// <returns>
        /// The public key, or <see langword="null" /> if this certificate does not have
        /// an ECDiffieHellman public key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The handle is invalid.
        /// </exception>
        public ECDiffieHellman? GetECDiffieHellmanPublicKey()
        {
            return this.GetPublicKey<ECDiffieHellman>(HasECDiffieHellmanKeyUsage);
        }

        /// <summary>
        /// Gets the <see cref="ECDiffieHellman" /> private key from this certificate.
        /// </summary>
        /// <returns>
        /// The private key, or <see langword="null" /> if this certificate does not have
        /// an ECDiffieHellman private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The handle is invalid.
        /// </exception>
        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            return this.GetPrivateKey<ECDiffieHellman>(HasECDiffieHellmanKeyUsage);
        }

        /// <summary>
        /// Combines a private key with the public key of an <see cref="ECDiffieHellman" />
        /// certificate to generate a new ECDiffieHellman certificate.
        /// </summary>
        /// <param name="privateKey">The private ECDiffieHellman key.</param>
        /// <returns>
        /// A new ECDiffieHellman certificate with the <see cref="HasPrivateKey" /> property set to <see langword="true"/>.
        /// The current certificate isn't modified.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="privateKey" /> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The certificate already has an associated private key.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   The certificate doesn't have a public key.
        /// </para>
        /// <para> -or- </para>
        /// <para>
        ///   The specified private key doesn't match the public key for this certificate.
        /// </para>
        /// </exception>
        public X509Certificate2 CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            ArgumentNullException.ThrowIfNull(privateKey);

            if (HasPrivateKey)
                throw new InvalidOperationException(SR.Cryptography_Cert_AlreadyHasPrivateKey);

            using (ECDiffieHellman? publicKey = GetECDiffieHellmanPublicKey())
            {
                if (publicKey is null)
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_WrongAlgorithm);
                }

                if (!Helpers.AreSamePublicECParameters(publicKey.ExportParameters(false), privateKey.ExportParameters(false)))
                {
                    throw new ArgumentException(SR.Cryptography_PrivateKey_DoesNotMatch, nameof(privateKey));
                }
            }

            ICertificatePal pal = Pal.CopyWithPrivateKey(privateKey);
            return new X509Certificate2(pal);
        }

        /// <summary>
        /// Creates a new X509 certificate from the file contents of an RFC 7468 PEM-encoded
        /// certificate and private key.
        /// </summary>
        /// <param name="certPemFilePath">The path for the PEM-encoded X509 certificate.</param>
        /// <param name="keyPemFilePath">
        /// If specified, the path for the PEM-encoded private key.
        /// If unspecified, the <paramref name="certPemFilePath" /> file will be used to load
        /// the private key.
        /// </param>
        /// <returns>A new certificate with the private key.</returns>
        /// <exception cref="CryptographicException">
        /// <para>
        ///   The contents of the file path in <paramref name="certPemFilePath" /> do not contain
        ///   a PEM-encoded certificate, or it is malformed.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   The contents of the file path in <paramref name="keyPemFilePath" /> do not contain a
        ///   PEM-encoded private key, or it is malformed.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   The contents of the file path in <paramref name="keyPemFilePath" /> contains
        ///   a key that does not match the public key in the certificate.
        /// </para>
        /// <para>-or-</para>
        /// <para>The certificate uses an unknown public key algorithm.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="certPemFilePath" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// See <see cref="System.IO.File.ReadAllText(string)" /> for additional documentation about
        /// exceptions that can be thrown.
        /// </para>
        /// <para>
        ///   The SubjectPublicKeyInfo from the certificate determines what PEM labels are accepted for the private key.
        ///   For RSA certificates, accepted private key PEM labels are "RSA PRIVATE KEY" and "PRIVATE KEY".
        ///   For ECDSA certificates, accepted private key PEM labels are "EC PRIVATE KEY" and "PRIVATE KEY".
        ///   For DSA certificates, the accepted private key PEM label is "PRIVATE KEY".
        /// </para>
        /// <para>PEM-encoded items that have a different label are ignored.</para>
        /// <para>
        ///   Combined PEM-encoded certificates and keys do not require a specific order. For the certificate, the
        ///   the first certificate with a CERTIFICATE label is loaded. For the private key, the first private
        ///   key with an acceptable label is loaded. More advanced scenarios for loading certificates and
        ///   private keys can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        ///   PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// <para>
        /// For password protected PEM-encoded keys, use <see cref="CreateFromEncryptedPemFile" /> to specify a password.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        {
            ArgumentNullException.ThrowIfNull(certPemFilePath);

            ReadOnlySpan<char> certContents = File.ReadAllText(certPemFilePath);
            ReadOnlySpan<char> keyContents = keyPemFilePath is null ? certContents : File.ReadAllText(keyPemFilePath);

            return CreateFromPem(certContents, keyContents);
        }

        /// <summary>
        /// Creates a new X509 certificate from the file contents of an RFC 7468 PEM-encoded
        /// certificate and password protected private key.
        /// </summary>
        /// <param name="certPemFilePath">The path for the PEM-encoded X509 certificate.</param>
        /// <param name="keyPemFilePath">
        /// If specified, the path for the password protected PEM-encoded private key.
        /// If unspecified, the <paramref name="certPemFilePath" /> file will be used to load
        /// the private key.
        /// </param>
        /// <param name="password">The password for the encrypted PEM.</param>
        /// <returns>A new certificate with the private key.</returns>
        /// <exception cref="CryptographicException">
        /// <para>
        ///   The contents of the file path in <paramref name="certPemFilePath" /> do not contain
        ///   a PEM-encoded certificate, or it is malformed.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   The contents of the file path in <paramref name="keyPemFilePath" /> do not contain a
        ///   password protected PEM-encoded private key, or it is malformed.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   The contents of the file path in <paramref name="keyPemFilePath" /> contains
        ///   a key that does not match the public key in the certificate.
        /// </para>
        /// <para>-or-</para>
        /// <para>The certificate uses an unknown public key algorithm.</para>
        /// <para>-or-</para>
        /// <para>The password specified for the private key is incorrect.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="certPemFilePath" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// See <see cref="System.IO.File.ReadAllText(string)" /> for additional documentation about
        /// exceptions that can be thrown.
        /// </para>
        /// <para>
        /// Password protected PEM-encoded keys are always expected to have the PEM label "ENCRYPTED PRIVATE KEY".
        /// </para>
        /// <para>PEM-encoded items that have a different label are ignored.</para>
        /// <para>
        ///   Combined PEM-encoded certificates and keys do not require a specific order. For the certificate, the
        ///   the first certificate with a CERTIFICATE label is loaded. For the private key, the first private
        ///   key with the label "ENCRYPTED PRIVATE KEY" is loaded. More advanced scenarios for loading certificates and
        ///   private keys can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        ///   PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// <para>
        /// For PEM-encoded keys without a password, use <see cref="CreateFromPemFile" />.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, ReadOnlySpan<char> password, string? keyPemFilePath = default)
        {
            ArgumentNullException.ThrowIfNull(certPemFilePath);

            ReadOnlySpan<char> certContents = File.ReadAllText(certPemFilePath);
            ReadOnlySpan<char> keyContents = keyPemFilePath is null ? certContents : File.ReadAllText(keyPemFilePath);

            return CreateFromEncryptedPem(certContents, keyContents, password);
        }

        /// <summary>
        /// Creates a new X509 certificate from the contents of an RFC 7468 PEM-encoded certificate and private key.
        /// </summary>
        /// <param name="certPem">The text of the PEM-encoded X509 certificate.</param>
        /// <param name="keyPem">The text of the PEM-encoded private key.</param>
        /// <returns>A new certificate with the private key.</returns>
        /// <exception cref="CryptographicException">
        /// <para>The contents of <paramref name="certPem" /> do not contain a PEM-encoded certificate, or it is malformed.</para>
        /// <para>-or-</para>
        /// <para>The contents of <paramref name="keyPem" /> do not contain a PEM-encoded private key, or it is malformed.</para>
        /// <para>-or-</para>
        /// <para>The contents of <paramref name="keyPem" /> contains a key that does not match the public key in the certificate.</para>
        /// <para>-or-</para>
        /// <para>The certificate uses an unknown public key algorithm.</para>
        /// </exception>
        /// <remarks>
        /// <para>
        ///   The SubjectPublicKeyInfo from the certificate determines what PEM labels are accepted for the private key.
        ///   For RSA certificates, accepted private key PEM labels are "RSA PRIVATE KEY" and "PRIVATE KEY".
        ///   For ECDSA and ECDH certificates, accepted private key PEM labels are "EC PRIVATE KEY" and "PRIVATE KEY".
        ///   For DSA certificates, the accepted private key PEM label is "PRIVATE KEY".
        /// </para>
        /// <para>PEM-encoded items that have a different label are ignored.</para>
        /// <para>
        ///   If the PEM-encoded certificate and private key are in the same text, use the same
        ///   string for both <paramref name="certPem" /> and <paramref name="keyPem" />, such as:
        ///   <code>
        ///     CreateFromPem(combinedCertAndKey, combinedCertAndKey);
        ///   </code>
        ///   Combined PEM-encoded certificates and keys do not require a specific order. For the certificate, the
        ///   the first certificate with a CERTIFICATE label is loaded. For the private key, the first private
        ///   key with an acceptable label is loaded. More advanced scenarios for loading certificates and
        ///   private keys can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        ///   PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// <para>
        /// For password protected PEM-encoded keys, use <see cref="CreateFromEncryptedPem" /> to specify a password.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static X509Certificate2 CreateFromPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem)
        {
            using (X509Certificate2 certificate = CreateFromPem(certPem))
            {
                string keyAlgorithm = certificate.GetKeyAlgorithm();

                return keyAlgorithm switch
                {
                    Oids.Rsa => ExtractKeyFromPem<RSA>(keyPem, s_RsaPublicKeyPrivateKeyLabels, RSA.Create, certificate.CopyWithPrivateKey),
                    Oids.Dsa when Helpers.IsDSASupported => ExtractKeyFromPem<DSA>(keyPem, s_DsaPublicKeyPrivateKeyLabels, DSA.Create, certificate.CopyWithPrivateKey),
                    Oids.EcPublicKey when IsECDsa(certificate) =>
                        ExtractKeyFromPem<ECDsa>(
                            keyPem,
                            s_EcPublicKeyPrivateKeyLabels,
                            ECDsa.Create,
                            certificate.CopyWithPrivateKey),
                    Oids.EcPublicKey when IsECDiffieHellman(certificate) =>
                        ExtractKeyFromPem<ECDiffieHellman>(
                            keyPem,
                            s_EcPublicKeyPrivateKeyLabels,
                            ECDiffieHellman.Create,
                            certificate.CopyWithPrivateKey),
                    _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm)),
                };
            }
        }

        /// <summary>
        /// Creates a new X509 certificate from the contents of an RFC 7468 PEM-encoded
        /// certificate and password protected private key.
        /// </summary>
        /// <param name="certPem">The text of the PEM-encoded X509 certificate.</param>
        /// <param name="keyPem">The text of the password protected PEM-encoded private key.</param>
        /// <param name="password">The password for the encrypted PEM.</param>
        /// <returns>A new certificate with the private key.</returns>
        /// <exception cref="CryptographicException">
        /// <para>The contents of <paramref name="certPem" /> do not contain a PEM-encoded certificate, or it is malformed.</para>
        /// <para>-or-</para>
        /// <para>
        ///   The contents of <paramref name="keyPem" /> do not contain a password protected PEM-encoded private key,
        ///   or it is malformed.
        /// </para>
        /// <para>-or-</para>
        /// <para>The contents of <paramref name="keyPem" /> contains a key that does not match the public key in the certificate.</para>
        /// <para>-or-</para>
        /// <para>The certificate uses an unknown public key algorithm.</para>
        /// <para>-or-</para>
        /// <para>The password specified for the private key is incorrect.</para>
        /// </exception>
        /// <remarks>
        /// <para>
        /// Password protected PEM-encoded keys are always expected to have the PEM label "ENCRYPTED PRIVATE KEY".
        /// </para>
        /// <para>PEM-encoded items that have a different label are ignored.</para>
        /// <para>
        ///   If the PEM-encoded certificate and private key are in the same text, use the same
        ///   string for both <paramref name="certPem" /> and <paramref name="keyPem" />, such as:
        ///   <code>
        ///     CreateFromEncryptedPem(combinedCertAndKey, combinedCertAndKey, theKeyPassword);
        ///   </code>
        ///   Combined PEM-encoded certificates and keys do not require a specific order. For the certificate, the
        ///   the first certificate with a CERTIFICATE label is loaded. For the private key, the first private
        ///   key with the label "ENCRYPTED PRIVATE KEY" is loaded. More advanced scenarios for loading certificates and
        ///   private keys can leverage <see cref="System.Security.Cryptography.PemEncoding" /> to enumerate
        ///   PEM-encoded values and apply any custom loading behavior.
        /// </para>
        /// <para>
        /// For PEM-encoded keys without a password, use <see cref="CreateFromPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static X509Certificate2 CreateFromEncryptedPem(ReadOnlySpan<char> certPem, ReadOnlySpan<char> keyPem, ReadOnlySpan<char> password)
        {
            using (X509Certificate2 certificate = CreateFromPem(certPem))
            {
                string keyAlgorithm = certificate.GetKeyAlgorithm();

                return keyAlgorithm switch
                {
                    Oids.Rsa => ExtractKeyFromEncryptedPem<RSA>(keyPem, password, RSA.Create, certificate.CopyWithPrivateKey),
                    Oids.Dsa when Helpers.IsDSASupported => ExtractKeyFromEncryptedPem<DSA>(keyPem, password, DSA.Create, certificate.CopyWithPrivateKey),
                    Oids.EcPublicKey when IsECDsa(certificate) =>
                        ExtractKeyFromEncryptedPem<ECDsa>(
                            keyPem,
                            password,
                            ECDsa.Create,
                            certificate.CopyWithPrivateKey),
                    Oids.EcPublicKey when IsECDiffieHellman(certificate) =>
                        ExtractKeyFromEncryptedPem<ECDiffieHellman>(
                            keyPem,
                            password,
                            ECDiffieHellman.Create,
                            certificate.CopyWithPrivateKey),
                    _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownKeyAlgorithm, keyAlgorithm)),
                };
            }
        }

        private static bool IsECDsa(X509Certificate2 certificate)
        {
            using (ECDsa? ecdsa = certificate.GetECDsaPublicKey())
            {
                return ecdsa is not null;
            }
        }

        private static bool IsECDiffieHellman(X509Certificate2 certificate)
        {
            using (ECDiffieHellman? ecdh = certificate.GetECDiffieHellmanPublicKey())
            {
                return ecdh is not null;
            }
        }

        /// <summary>
        /// Creates a new X509 certificate from the contents of an RFC 7468 PEM-encoded
        /// certificate.
        /// </summary>
        /// <param name="certPem">The text of the PEM-encoded X509 certificate.</param>
        /// <returns>A new X509 certificate.</returns>
        /// <exception cref="CryptographicException">
        /// The contents of <paramref name="certPem" /> do not contain a PEM-encoded certificate, or it is malformed.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This loads the first well-formed PEM found with a CERTIFICATE label.
        /// </para>
        /// <para>
        /// For PEM-encoded certificates with a private key, use
        /// <see cref="CreateFromPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />.
        /// </para>
        /// <para>
        /// For PEM-encoded certificates in a file, use <see cref="X509Certificate2(string)" />.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public static X509Certificate2 CreateFromPem(ReadOnlySpan<char> certPem)
        {
            foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(certPem))
            {
                ReadOnlySpan<char> label = contents[fields.Label];

                if (label.SequenceEqual(PemLabels.X509Certificate))
                {
                    byte[] certBytes = CryptoPool.Rent(fields.DecodedDataLength);

                    if (!Convert.TryFromBase64Chars(contents[fields.Base64Data], certBytes, out int bytesWritten)
                        || bytesWritten != fields.DecodedDataLength)
                    {
                        Debug.Fail("The contents should have already been validated by the PEM reader.");
                        throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
                    }

                    ReadOnlyMemory<byte> certData = new ReadOnlyMemory<byte>(certBytes, 0, bytesWritten);

                    try
                    {
                        // Check that the contents are actually an X509 DER encoded
                        // certificate, not something else that the constructor will
                        // will otherwise be able to figure out.
                        CertificateAsn.Decode(certData, AsnEncodingRules.DER);
                    }
                    catch (CryptographicException)
                    {
                        throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
                    }

                    X509Certificate2 ret = new X509Certificate2(certData.Span);
                    // Certs are public data, no need to clear.
                    CryptoPool.Return(certBytes, clearSize: 0);
                    return ret;
                }
            }

            throw new CryptographicException(SR.Cryptography_X509_NoPemCertificate);
        }

        /// <summary>
        /// Exports the public X.509 certificate, encoded as PEM.
        /// </summary>
        /// <returns>
        /// The PEM encoding of the certificate.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 certificate will begin with <c>-----BEGIN CERTIFICATE-----</c>
        ///   and end with <c>-----END CERTIFICATE-----</c>, with the base64 encoded DER
        ///   contents of the certificate between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The certificate is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public string ExportCertificatePem()
        {
            return PemEncoding.WriteString(PemLabels.X509Certificate, RawDataMemory.Span);
        }

        /// <summary>
        /// Attempts to export the public X.509 certificate, encoded as PEM.
        /// </summary>
        /// <param name="destination">The buffer to receive the PEM encoded certificate.</param>
        /// <param name="charsWritten">When this method returns, the total number of characters written to <paramref name="destination" />.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> was large enough to receive the encoded PEM;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The certificate is corrupt, in an invalid state, or could not be exported
        /// to PEM.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 certificate will begin with <c>-----BEGIN CERTIFICATE-----</c>
        ///   and end with <c>-----END CERTIFICATE-----</c>, with the base64 encoded DER
        ///   contents of the certificate between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The certificate is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportCertificatePem(Span<char> destination, out int charsWritten)
        {
            return PemEncoding.TryWrite(PemLabels.X509Certificate, RawDataMemory.Span, destination, out charsWritten);
        }

        /// <summary>
        ///   Checks to see if the certificate matches the provided hostname.
        /// </summary>
        /// <param name="hostname">The host name to match against.</param>
        /// <param name="allowWildcards">
        ///   <see langword="true"/> to allow wildcard matching for <c>dNSName</c> values in the
        ///   Subject Alternative Name extension; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="allowCommonName">
        ///   <see langword="true"/> to allow matching against the subject Common Name value;
        ///   otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the certificate is a match for the requested hostname;
        ///   otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method is a platform neutral implementation of IETF RFC 6125 host matching logic.
        ///     The SslStream class uses the hostname validator from the operating system, which may
        ///     result in different values from this implementation.
        ///   </para>
        ///   <para>
        ///     The logical flow of this method is:
        ///     <list type="bullet">
        ///       <item>
        ///         If the hostname parses as an <see cref="IPAddress"/> then IPAddress matching is done;
        ///         otherwise, DNS Name matching is done.
        ///       </item>
        ///       <item>
        ///         For IPAddress matching, the value must be an exact match against an <c>iPAddress</c> value in an
        ///         entry of the Subject Alternative Name extension.
        ///       </item>
        ///       <item>
        ///         For DNS Name matching, the value must be an exact match against a <c>dNSName</c> value in an
        ///         entry of the Subject Alternative Name extension, or a wildcard match against the same.
        ///       </item>
        ///       <item>
        ///         For wildcard matching, the wildcard must be the first character in the <c>dNSName</c> entry,
        ///         the second character must be a period (.), and the entry must have a length greater than two.
        ///         The wildcard will only match the <paramref name="hostname"/> value up to the first period (.),
        ///         remaining characters must be an exact match.
        ///       </item>
        ///       <item>
        ///         If there is no Subject Alternative Name extension, or the extension does not have any entries
        ///         of the appropriate type, then Common Name matching is used as a fallback.
        ///       </item>
        ///       <item>
        ///         For Common Name matching, if the Subject Name contains a single Common Name, and that attribute
        ///         is not defined as part of a multi-valued Relative Distinguished Name, then the hostname is matched
        ///         against the Common Name attribute's value.
        ///         Note that wildcards are not used in Common Name matching.
        ///       </item>
        ///     </list>
        ///   </para>
        ///   <para>
        ///     This implementation considers <c>SRV-ID</c> values or <c>URI-ID</c> values as out-of-scope,
        ///     and will not use their presence as a reason to stop the fallback from <c>DNS-ID</c> matching
        ///     to the <c>CN-ID</c>.
        ///   </para>
        ///   <para>
        ///     This method does not convert non-ASCII hostnames to the IDNA representation. For Unicode domains,
        ///     the caller must make use of <see cref="System.Globalization.IdnMapping"/> or an equivalent IDNA mapper.
        ///   </para>
        ///   <para>
        ///     The "exact" matches performed by this routine are <see cref="StringComparison.OrdinalIgnoreCase"/>,
        ///     as domain names are not case-sensitive.
        ///   </para>
        ///   <para>
        ///     This method does not determine if the hostname is authorized by a trusted authority.  A trust
        ///     decision cannot be made without additionally checking for trust via <see cref="X509Chain"/>.
        ///   </para>
        ///   <para>
        ///     This method does not check that the certificate has an <c>id-kp-serverAuth</c> (1.3.6.1.5.5.7.3.1)
        ///     extended key usage.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="hostname"/> parameter is not a valid DNS hostname or IP address.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The certificate contains multiple Subject Alternative Name extensions.</para>
        ///   <para>- or -</para>
        ///   <para>The Subject Alternative Name extension or Subject Name could not be decoded.</para>
        /// </exception>
        /// <seealso cref="IPAddress.TryParse(string, out IPAddress)"/>
        /// <seealso cref="Uri.CheckHostName"/>
        public bool MatchesHostname(string hostname, bool allowWildcards = true, bool allowCommonName = true)
        {
            ArgumentNullException.ThrowIfNull(hostname);
            IPAddress? ipAddress;

            if (!IPAddress.TryParse(hostname, out ipAddress))
            {
                UriHostNameType kind = Uri.CheckHostName(hostname);

                if (kind != UriHostNameType.Dns)
                {
                    throw new ArgumentException(
                        SR.Argument_InvalidHostnameOrIPAddress,
                        nameof(hostname));
                }
            }

            X509Extension? rawSAN = null;

            foreach (X509Extension extension in Pal.Extensions)
            {
                if (extension.Oid!.Value == Oids.SubjectAltName)
                {
                    if (rawSAN is null)
                    {
                        rawSAN = extension;
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_X509_TooManySANs);
                    }
                }
            }

            if (rawSAN is not null)
            {
                Debug.Assert(rawSAN.GetType() == typeof(X509Extension));

                var san = new X509SubjectAlternativeNameExtension();
                san.CopyFrom(rawSAN);

                bool hadAny = false;

                if (ipAddress is not null)
                {
                    foreach (IPAddress sanEntry in san.EnumerateIPAddresses())
                    {
                        if (sanEntry.Equals(ipAddress))
                        {
                            return true;
                        }

                        hadAny = true;
                    }
                }
                else
                {
                    ReadOnlySpan<char> match = hostname;

                    // Treat "something.example.org." as "something.example.org"
                    if (hostname.EndsWith('.'))
                    {
                        match = match.Slice(0, match.Length - 1);

                        if (match.IsEmpty)
                        {
                            return false;
                        }
                    }

                    ReadOnlySpan<char> afterFirstDot = default;
                    int firstDot = match.IndexOf('.');
                    Debug.Assert(firstDot != 0, "Leading periods should have been rejected.");

                    if (firstDot > 0)
                    {
                        afterFirstDot = match.Slice(firstDot + 1);
                    }

                    foreach (string embedded in san.EnumerateDnsNames())
                    {
                        hadAny = true;

                        if (embedded.Length == 0)
                        {
                            continue;
                        }

                        ReadOnlySpan<char> embeddedSpan = embedded;

                        // Convert embedded "something.example.org." to "something.example.org"
                        if (embedded.EndsWith('.'))
                        {
                            embeddedSpan = embeddedSpan.Slice(0, embeddedSpan.Length - 1);
                        }

                        if (allowWildcards && embeddedSpan.StartsWith("*.") && embeddedSpan.Length > 2)
                        {
                            if (embeddedSpan.Slice(2).Equals(afterFirstDot, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        else if (embeddedSpan.Equals(match, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                if (hadAny)
                {
                    return false;
                }
            }

            if (allowCommonName)
            {
                X500RelativeDistinguishedName? cn = null;

                foreach (X500RelativeDistinguishedName rdn in SubjectName.EnumerateRelativeDistinguishedNames())
                {
                    if (rdn.HasMultipleElements)
                    {
                        AsnValueReader reader = new AsnValueReader(rdn.RawData.Span, AsnEncodingRules.DER);
                        // Be lax with the sort order because Windows is
                        AsnValueReader set = reader.ReadSetOf(skipSortOrderValidation: true);

                        while (set.HasData)
                        {
                            // We're not concerned with the possibility that the attribute structure
                            // is malformed here, because X500RelativeDistinguishedName already ensures it.
                            // So we don't bother checking that there's a value after the OID and then nothing
                            // after that.
                            AsnValueReader attributeTypeAndValue = set.ReadSequence();
                            Oid? type = Oids.GetSharedOrNullOid(ref attributeTypeAndValue);

                            if (Oids.CommonNameOid.ValueEquals(type))
                            {
                                return false;
                            }
                        }
                    }
                    else if (Oids.CommonNameOid.ValueEquals(rdn.GetSingleElementType()))
                    {
                        if (cn is null)
                        {
                            cn = rdn;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                if (cn is not null)
                {
                    return hostname.Equals(cn.GetSingleElementValue(), StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static X509Certificate2 ExtractKeyFromPem<TAlg>(
            ReadOnlySpan<char> keyPem,
            string[] labels,
            Func<TAlg> factory,
            Func<TAlg, X509Certificate2> import) where TAlg : AsymmetricAlgorithm
        {
            foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(keyPem))
            {
                ReadOnlySpan<char> label = contents[fields.Label];

                foreach (string eligibleLabel in labels)
                {
                    if (label.SequenceEqual(eligibleLabel))
                    {
                        TAlg key = factory();
                        key.ImportFromPem(contents[fields.Location]);

                        try
                        {
                            return import(key);
                        }
                        catch (ArgumentException ae)
                        {
                            throw new CryptographicException(SR.Cryptography_X509_NoOrMismatchedPemKey, ae);
                        }
                    }
                }
            }

            throw new CryptographicException(SR.Cryptography_X509_NoOrMismatchedPemKey);
        }

        private static X509Certificate2 ExtractKeyFromEncryptedPem<TAlg>(
            ReadOnlySpan<char> keyPem,
            ReadOnlySpan<char> password,
            Func<TAlg> factory,
            Func<TAlg, X509Certificate2> import) where TAlg : AsymmetricAlgorithm
        {
            foreach ((ReadOnlySpan<char> contents, PemFields fields) in new PemEnumerator(keyPem))
            {
                ReadOnlySpan<char> label = contents[fields.Label];

                if (label.SequenceEqual(PemLabels.EncryptedPkcs8PrivateKey))
                {
                    TAlg key = factory();
                    key.ImportFromEncryptedPem(contents[fields.Location], password);

                    try
                    {
                        return import(key);
                    }
                    catch (ArgumentException ae)
                    {
                        throw new CryptographicException(SR.Cryptography_X509_NoOrMismatchedPemKey, ae);
                    }

                }
            }

            throw new CryptographicException(SR.Cryptography_X509_NoOrMismatchedPemKey);
        }

        private static X509Extension? CreateCustomExtensionIfAny(Oid oid) =>
            CreateCustomExtensionIfAny(oid.Value);

        internal static X509Extension? CreateCustomExtensionIfAny(string? oidValue) =>
            oidValue switch
            {
                Oids.BasicConstraints => X509Pal.Instance.SupportsLegacyBasicConstraintsExtension ? new X509BasicConstraintsExtension() : null,
                Oids.BasicConstraints2 => new X509BasicConstraintsExtension(),
                Oids.KeyUsage => new X509KeyUsageExtension(),
                Oids.EnhancedKeyUsage => new X509EnhancedKeyUsageExtension(),
                Oids.SubjectKeyIdentifier => new X509SubjectKeyIdentifierExtension(),
                Oids.AuthorityKeyIdentifier => new X509AuthorityKeyIdentifierExtension(),
                Oids.AuthorityInformationAccess => new X509AuthorityInformationAccessExtension(),
                Oids.SubjectAltName => new X509SubjectAlternativeNameExtension(),
                _ => null,
            };

        private static bool HasECDiffieHellmanKeyUsage(X509Certificate2 certificate)
        {
            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension.Oid?.Value == Oids.KeyUsage && extension is X509KeyUsageExtension ext)
                {
                    // keyAgreement is mandatory for id-ecPublicKey certificates
                    // when used with ECDH.
                    return ((ext.KeyUsages & X509KeyUsageFlags.KeyAgreement) != 0);
                }
            }

            // If the key usage extension is not present in the certificate it is
            // considered valid for all usages, so we can use it for ECDH.
            return true;
        }
    }
}
