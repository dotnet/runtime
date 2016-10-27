// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Internal.Cryptography.Pal;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
    [Serializable]
    public class X509Certificate2 : X509Certificate
    {
        public X509Certificate2()
            : base()
        {
        }

        public X509Certificate2(byte[] rawData)
            : base(rawData)
        {
        }

        public X509Certificate2(byte[] rawData, string password)
            : base(rawData, password)
        {
        }

        public X509Certificate2(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
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

        public X509Certificate2(string fileName, string password)
            : base(fileName, password)
        {
        }

        public X509Certificate2(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
        {
        }

        public X509Certificate2(X509Certificate cert)
            : base(cert)
        {
        }

        protected X509Certificate2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

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

                X509ExtensionCollection extensions = _lazyExtensions;
                if (extensions == null)
                {
                    extensions = new X509ExtensionCollection();
                    foreach (X509Extension extension in Pal.Extensions)
                    {
                        X509Extension customExtension = CreateCustomExtensionIfAny(extension.Oid);
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

        public AsymmetricAlgorithm PrivateKey
        {
            get
            {
                if (_lazyPrivateKey == null)
                {
                    _lazyPrivateKey = Pal.GetPrivateKey();
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

                X500DistinguishedName issuerName = _lazyIssuer;
                if (issuerName == null)
                    issuerName = _lazyIssuer = Pal.IssuerName;
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

                PublicKey publicKey = _lazyPublicKey;
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

                byte[] rawData = _lazyRawData;
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
                byte[] serialNumber = GetSerialNumber();
                Array.Reverse(serialNumber);
                return serialNumber.ToHexStringUpper();
            }
        }

        public Oid SignatureAlgorithm
        {
            get
            {
                ThrowIfInvalid();

                Oid signatureAlgorithm = _lazySignatureAlgorithm;
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

                X500DistinguishedName subjectName = _lazySubjectName;
                if (subjectName == null)
                    subjectName = _lazySubjectName = Pal.SubjectName;
                return subjectName;
            }
        }

        public string Thumbprint
        {
            get
            {
                byte[] thumbPrint = GetCertHash();
                return thumbPrint.ToHexStringUpper();
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

        public static X509ContentType GetCertContentType(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Desktop compat: The desktop CLR expands the filename to a full path for the purpose of performing a CAS permission check. While CAS is not present here,
            // we still need to call GetFullPath() so we get the same exception behavior if the fileName is bad.
            string fullPath = Path.GetFullPath(fileName);

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

                    using (RSA pubRsa = this.GetRSAPublicKey())
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
                        sb.Append(extension.Oid.FriendlyName);
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

        private static X509Extension CreateCustomExtensionIfAny(Oid oid)
        {
            string oidValue = oid.Value;
            switch (oidValue)
            {
                case Oids.BasicConstraints:
                    return X509Pal.Instance.SupportsLegacyBasicConstraintsExtension ?
                        new X509BasicConstraintsExtension() :
                        null;

                case Oids.BasicConstraints2:
                    return new X509BasicConstraintsExtension();

                case Oids.KeyUsage:
                    return new X509KeyUsageExtension();

                case Oids.EnhancedKeyUsage:
                    return new X509EnhancedKeyUsageExtension();

                case Oids.SubjectKeyIdentifier:
                    return new X509SubjectKeyIdentifierExtension();

                default:
                    return null;
            }
        }

        private volatile byte[] _lazyRawData;
        private volatile Oid _lazySignatureAlgorithm;
        private volatile int _lazyVersion;
        private volatile X500DistinguishedName _lazySubjectName;
        private volatile X500DistinguishedName _lazyIssuer;
        private volatile PublicKey _lazyPublicKey;
        private volatile AsymmetricAlgorithm _lazyPrivateKey;
        private volatile X509ExtensionCollection _lazyExtensions;
    }
}
