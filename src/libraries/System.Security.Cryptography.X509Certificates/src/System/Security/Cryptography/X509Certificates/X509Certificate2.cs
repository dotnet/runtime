// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Internal.Cryptography.Pal;
using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;

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

        public X509Certificate2()
            : base()
        {
        }

        public X509Certificate2(byte[] rawData)
            : base(rawData)
        {
        }

        public X509Certificate2(byte[] rawData, string? password)
            : base(rawData, password)
        {
        }

        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(byte[] rawData, SecureString? password)
            : base(rawData, password)
        {
        }

        public X509Certificate2(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
            : base(rawData, password, keyStorageFlags)
        {
        }

        [System.CLSCompliantAttribute(false)]
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
        public X509Certificate2(ReadOnlySpan<byte> rawData, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
            : base(rawData, password, keyStorageFlags)
        {
        }

        public X509Certificate2(IntPtr handle)
            : base(handle)
        {
        }

        internal X509Certificate2(ICertificatePal pal)
            : base(pal)
        {
        }

        public X509Certificate2(string fileName)
            : base(fileName)
        {
        }

        public X509Certificate2(string fileName, string? password)
            : base(fileName, password)
        {
        }

        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, SecureString? password)
            : base(fileName, password)
        {
        }


        public X509Certificate2(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
        {
        }

        [System.CLSCompliantAttribute(false)]
        public X509Certificate2(string fileName, SecureString? password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
        {
        }

        public X509Certificate2(string fileName, ReadOnlySpan<char> password, X509KeyStorageFlags keyStorageFlags = 0)
            : base(fileName, password, keyStorageFlags)
        {
        }

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

        public AsymmetricAlgorithm? PrivateKey
        {
            get
            {
                ThrowIfInvalid();

                if (!HasPrivateKey)
                    return null;

                if (_lazyPrivateKey == null)
                {
                    _lazyPrivateKey = GetKeyAlgorithm() switch
                    {
                        Oids.Rsa => Pal.GetRSAPrivateKey(),
                        Oids.Dsa => Pal.GetDSAPrivateKey(),

                        // This includes ECDSA, because an Oids.EcPublicKey key can be
                        // many different algorithm kinds, not necessarily with mutual exclusion.
                        // Plus, .NET Framework only supports RSA and DSA in this property.
                        _ => throw new NotSupportedException(SR.NotSupported_KeyAlgorithm),
                    };
                }

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

                X500DistinguishedName? issuerName = _lazyIssuerName;
                if (issuerName == null)
                    issuerName = _lazyIssuerName = Pal.IssuerName;
                return issuerName;
            }
        }

        public DateTime NotAfter
        {
            get { return GetNotAfter(); }
        }

        public DateTime NotBefore
        {
            get { return GetNotBefore(); }
        }

        public PublicKey PublicKey
        {
            get
            {
                ThrowIfInvalid();

                PublicKey? publicKey = _lazyPublicKey;
                if (publicKey == null)
                {
                    string keyAlgorithmOid = GetKeyAlgorithm();
                    byte[] parameters = GetKeyAlgorithmParameters();
                    byte[] keyValue = GetPublicKey();
                    Oid oid = new Oid(keyAlgorithmOid);
                    publicKey = _lazyPublicKey = new PublicKey(oid, new AsnEncodedData(oid, parameters), new AsnEncodedData(oid, keyValue));
                }
                return publicKey;
            }
        }

        public byte[] RawData
        {
            get
            {
                ThrowIfInvalid();

                byte[]? rawData = _lazyRawData;
                if (rawData == null)
                {
                    rawData = _lazyRawData = Pal.RawData;
                }
                return rawData.CloneByteArray();
            }
        }

        public string SerialNumber
        {
            get
            {
                return GetSerialNumberString();
            }
        }

        public Oid SignatureAlgorithm
        {
            get
            {
                ThrowIfInvalid();

                Oid? signatureAlgorithm = _lazySignatureAlgorithm;
                if (signatureAlgorithm == null)
                {
                    string oidValue = Pal.SignatureAlgorithm;
                    signatureAlgorithm = _lazySignatureAlgorithm = Oid.FromOidValue(oidValue, OidGroup.SignatureAlgorithm);
                }
                return signatureAlgorithm;
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                ThrowIfInvalid();

                X500DistinguishedName? subjectName = _lazySubjectName;
                if (subjectName == null)
                    subjectName = _lazySubjectName = Pal.SubjectName;
                return subjectName;
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
        public static X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
        {
            if (rawData.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(rawData));

            return X509Pal.Instance.GetCertContentType(rawData);
        }

        public static X509ContentType GetCertContentType(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // .NET Framework compat: The .NET Framework expands the filename to a full path for the purpose of performing a CAS permission check. While CAS is not present here,
            // we still need to call GetFullPath() so we get the same exception behavior if the fileName is bad.
            _ = Path.GetFullPath(fileName);

            return X509Pal.Instance.GetCertContentType(fileName);
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            return Pal.GetNameInfo(nameType, forIssuer);
        }

        public override string ToString()
        {
            return base.ToString(fVerbose: true);
        }

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

        public override void Import(byte[] rawData)
        {
            base.Import(rawData);
        }

        public override void Import(byte[] rawData, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(rawData, password, keyStorageFlags);
        }

        [System.CLSCompliantAttribute(false)]
        public override void Import(byte[] rawData, SecureString? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(rawData, password, keyStorageFlags);
        }

        public override void Import(string fileName)
        {
            base.Import(fileName);
        }

        public override void Import(string fileName, string? password, X509KeyStorageFlags keyStorageFlags)
        {
            base.Import(fileName, password, keyStorageFlags);
        }

        [System.CLSCompliantAttribute(false)]
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
            return this.GetPublicKey<ECDiffieHellman>(cert => HasECDiffieHellmanKeyUsage(cert));
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
            return this.GetPrivateKey<ECDiffieHellman>(cert => HasECDiffieHellmanKeyUsage(cert));
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
            if (privateKey is null)
                throw new ArgumentNullException(nameof(privateKey));

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
        public static X509Certificate2 CreateFromPemFile(string certPemFilePath, string? keyPemFilePath = default)
        {
            if (certPemFilePath is null)
                throw new ArgumentNullException(nameof(certPemFilePath));

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
        public static X509Certificate2 CreateFromEncryptedPemFile(string certPemFilePath, ReadOnlySpan<char> password, string? keyPemFilePath = default)
        {
            if (certPemFilePath is null)
                throw new ArgumentNullException(nameof(certPemFilePath));

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
            oid.Value switch
            {
                Oids.BasicConstraints => X509Pal.Instance.SupportsLegacyBasicConstraintsExtension ? new X509BasicConstraintsExtension() : null,
                Oids.BasicConstraints2 => new X509BasicConstraintsExtension(),
                Oids.KeyUsage => new X509KeyUsageExtension(),
                Oids.EnhancedKeyUsage => new X509EnhancedKeyUsageExtension(),
                Oids.SubjectKeyIdentifier => new X509SubjectKeyIdentifierExtension(),
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
