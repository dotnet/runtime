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

        public X509Certificate2(byte[] rawData, String password)
            : base(rawData, password)
        {
        }

        public X509Certificate2(byte[] rawData, String password, X509KeyStorageFlags keyStorageFlags)
            : base(rawData, password, keyStorageFlags)
        {
        }

        public X509Certificate2(IntPtr handle)
            : base(handle)
        {
        }

        public X509Certificate2(String fileName)
            : base(fileName)
        {
        }

        public X509Certificate2(String fileName, String password)
            : base(fileName, password)
        {
        }

        public X509Certificate2(String fileName, String password, X509KeyStorageFlags keyStorageFlags)
            : base(fileName, password, keyStorageFlags)
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

        public String FriendlyName
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

        public AsymmetricAlgorithm PrivateKey
        {
            get
            {
                ThrowIfInvalid();

                if (!this.HasPrivateKey)
                    return null;

                AsymmetricAlgorithm privateKey = _lazyPrivateKey;
                if (privateKey == null)
                    privateKey = _lazyPrivateKey = Pal.PrivateKey;
                return privateKey;
            }

            set
            {
                ThrowIfInvalid();

                AsymmetricAlgorithm publicKey = this.PublicKey.Key;
                Pal.SetPrivateKey(value, publicKey);
                _lazyPrivateKey = value;
            }
        }

        public PublicKey PublicKey
        {
            get
            {
                ThrowIfInvalid();

                PublicKey publicKey = _lazyPublicKey;
                if (publicKey == null)
                {
                    String keyAlgorithmOid = this.GetKeyAlgorithm();
                    byte[] parameters = this.GetKeyAlgorithmParameters();
                    byte[] keyValue = this.GetPublicKey();
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

        public String SerialNumber
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
                    String oidValue = Pal.SignatureAlgorithm;
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

        public String Thumbprint
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
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, "rawData");

            return X509Pal.Instance.GetCertContentType(rawData);
        }

        public static X509ContentType GetCertContentType(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            // Desktop compat: The desktop CLR expands the filename to a full path for the purpose of performing a CAS permission check. While CAS is not present here,
            // we still need to call GetFullPath() so we get the same exception behavior if the fileName is bad.
            String fullPath = Path.GetFullPath(fileName);

            return X509Pal.Instance.GetCertContentType(fileName);
        }

        public String GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            return Pal.GetNameInfo(nameType, forIssuer);
        }

        public override String ToString()
        {
            return base.ToString(fVerbose: true);
        }

        public override String ToString(bool verbose)
        {
            if (verbose == false || Pal == null)
                return ToString();

            StringBuilder sb = new StringBuilder();
            String newLine = Environment.NewLine;
            String newLine2 = newLine + newLine;
            String newLinesp2 = newLine + "  ";

            // Version
            sb.Append("[Version]");
            sb.Append(newLinesp2);
            sb.Append("V" + this.Version);

            // Subject
            sb.Append(newLine2);
            sb.Append("[Subject]");
            sb.Append(newLinesp2);
            sb.Append(this.SubjectName.Name);
            String simpleName = GetNameInfo(X509NameType.SimpleName, false);
            if (simpleName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("Simple Name: ");
                sb.Append(simpleName);
            }
            String emailName = GetNameInfo(X509NameType.EmailName, false);
            if (emailName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("Email Name: ");
                sb.Append(emailName);
            }
            String upnName = GetNameInfo(X509NameType.UpnName, false);
            if (upnName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("UPN Name: ");
                sb.Append(upnName);
            }
            String dnsName = GetNameInfo(X509NameType.DnsName, false);
            if (dnsName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("DNS Name: ");
                sb.Append(dnsName);
            }

            // Issuer
            sb.Append(newLine2);
            sb.Append("[Issuer]");
            sb.Append(newLinesp2);
            sb.Append(this.IssuerName.Name);
            simpleName = GetNameInfo(X509NameType.SimpleName, true);
            if (simpleName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("Simple Name: ");
                sb.Append(simpleName);
            }
            emailName = GetNameInfo(X509NameType.EmailName, true);
            if (emailName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("Email Name: ");
                sb.Append(emailName);
            }
            upnName = GetNameInfo(X509NameType.UpnName, true);
            if (upnName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("UPN Name: ");
                sb.Append(upnName);
            }
            dnsName = GetNameInfo(X509NameType.DnsName, true);
            if (dnsName.Length > 0)
            {
                sb.Append(newLinesp2);
                sb.Append("DNS Name: ");
                sb.Append(dnsName);
            }

            // Serial Number
            sb.Append(newLine2);
            sb.Append("[Serial Number]");
            sb.Append(newLinesp2);
            sb.Append(this.SerialNumber);

            // NotBefore
            sb.Append(newLine2);
            sb.Append("[Not Before]");
            sb.Append(newLinesp2);
            sb.Append(FormatDate(this.NotBefore));

            // NotAfter
            sb.Append(newLine2);
            sb.Append("[Not After]");
            sb.Append(newLinesp2);
            sb.Append(FormatDate(this.NotAfter));

            // Thumbprint
            sb.Append(newLine2);
            sb.Append("[Thumbprint]");
            sb.Append(newLinesp2);
            sb.Append(this.Thumbprint);

            // Signature Algorithm
            sb.Append(newLine2);
            sb.Append("[Signature Algorithm]");
            sb.Append(newLinesp2);
            sb.Append(this.SignatureAlgorithm.FriendlyName + "(" + this.SignatureAlgorithm.Value + ")");

            // Public Key
            sb.Append(newLine2);
            sb.Append("[Public Key]");
            // It could throw if it's some user-defined CryptoServiceProvider
            try
            {
                PublicKey pubKey = this.PublicKey;

                String temp = pubKey.Oid.FriendlyName;
                sb.Append(newLinesp2);
                sb.Append("Algorithm: ");
                sb.Append(temp);
                // So far, we only support RSACryptoServiceProvider & DSACryptoServiceProvider Keys
                try
                {
                    temp = pubKey.Key.KeySize.ToString();
                    sb.Append(newLinesp2);
                    sb.Append("Length: ");
                    sb.Append(temp);
                }
                catch (NotSupportedException)
                {
                }

                temp = pubKey.EncodedKeyValue.Format(true);
                sb.Append(newLinesp2);
                sb.Append("Key Blob: ");
                sb.Append(temp);

                temp = pubKey.EncodedParameters.Format(true);
                sb.Append(newLinesp2);
                sb.Append("Parameters: ");
                sb.Append(temp);
            }
            catch (CryptographicException)
            {
            }

            // Private key
            Pal.AppendPrivateKeyInfo(sb);

            // Extensions
            X509ExtensionCollection extensions = this.Extensions;
            if (extensions.Count > 0)
            {
                sb.Append(newLine2);
                sb.Append("[Extensions]");
                String temp;
                foreach (X509Extension extension in extensions)
                {
                    try
                    {
                        temp = extension.Oid.FriendlyName;
                        sb.Append(newLine);
                        sb.Append("* " + temp);
                        sb.Append("(" + extension.Oid.Value + "):");

                        temp = extension.Format(true);
                        sb.Append(newLinesp2);
                        sb.Append(temp);
                    }
                    catch (CryptographicException)
                    {
                    }
                }
            }

            sb.Append(newLine);
            return sb.ToString();
        }

        private static X509Extension CreateCustomExtensionIfAny(Oid oid)
        {
            String oidValue = oid.Value;
            switch (oidValue)
            {
                case Oids.BasicConstraints:
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

