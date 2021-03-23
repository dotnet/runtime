// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class AndroidCertificatePal : ICertificatePal
    {
        private SafeX509Handle _cert;
        private SafeEvpPKeyHandle? _privateKey;

        private CertificateData _certData;

        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            var newHandle = new SafeX509Handle(Interop.JObjectLifetime.NewGlobalReference(handle));
            return new AndroidCertificatePal(newHandle);
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            // Ensure private key is copied
            AndroidCertificatePal certPal = (AndroidCertificatePal)cert.Pal;
            return certPal.DuplicateHandles();
        }

        public static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            // TODO: [AndroidCrypto] Handle PKCS#12
            Debug.Assert(password != null);
            ICertificatePal? cert;
            if (TryReadX509(rawData, out cert))
            {
                if (cert == null)
                {
                    // Empty collection, most likely.
                    throw new CryptographicException();
                }

                return cert;
            }

            // Unsupported
            throw new CryptographicException();
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        // Handles both DER and PEM
        private static bool TryReadX509(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out ICertificatePal? handle)
        {
            handle = null;
            SafeX509Handle certHandle = Interop.AndroidCrypto.X509Decode(
                ref MemoryMarshal.GetReference(rawData),
                rawData.Length);

            if (certHandle.IsInvalid)
            {
                certHandle.Dispose();
                return false;
            }

            handle = new AndroidCertificatePal(certHandle);
            return true;
        }

        private AndroidCertificatePal(SafeX509Handle handle)
        {
            _cert = handle;
        }

        public bool HasPrivateKey => _privateKey != null;

        public IntPtr Handle => _cert == null ? IntPtr.Zero : _cert.DangerousGetHandle();

        internal SafeX509Handle SafeHandle => _cert;

        public string Issuer
        {
            get
            {
                EnsureCertificateData();
                return _certData.IssuerName;
            }
        }

        public string Subject
        {
            get
            {
                EnsureCertificateData();
                return _certData.SubjectName;
            }
        }

        public string LegacyIssuer => IssuerName.Decode(X500DistinguishedNameFlags.None);

        public string LegacySubject => SubjectName.Decode(X500DistinguishedNameFlags.None);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required for Compat")]
        public byte[] Thumbprint
        {
            get
            {
                EnsureCertificateData();
                return SHA1.HashData(_certData.RawData);
            }
        }

        public string KeyAlgorithm
        {
            get
            {
                EnsureCertificateData();
                return _certData.PublicKeyAlgorithm.AlgorithmId!;
            }
        }

        public byte[] KeyAlgorithmParameters
        {
            get
            {
                EnsureCertificateData();
                return _certData.PublicKeyAlgorithm.Parameters;
            }
        }

        public byte[] PublicKeyValue
        {
            get
            {
                EnsureCertificateData();
                return _certData.PublicKey;
            }
        }

        public byte[] SubjectPublicKeyInfo
        {
            get
            {
                EnsureCertificateData();
                return _certData.SubjectPublicKeyInfo;
            }
        }

        public byte[] SerialNumber
        {
            get
            {
                EnsureCertificateData();
                return _certData.SerialNumber;
            }
        }

        public string SignatureAlgorithm
        {
            get
            {
                EnsureCertificateData();
                return _certData.SignatureAlgorithm.AlgorithmId!;
            }
        }

        public DateTime NotAfter
        {
            get
            {
                EnsureCertificateData();
                return _certData.NotAfter.ToLocalTime();
            }
        }

        public DateTime NotBefore
        {
            get
            {
                EnsureCertificateData();
                return _certData.NotBefore.ToLocalTime();
            }
        }

        public byte[] RawData => Interop.AndroidCrypto.X509Encode(_cert);

        public int Version
        {
            get
            {
                EnsureCertificateData();
                return _certData.Version + 1;
            }
        }

        public bool Archived
        {
            get { return false; }
            set
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_Unix_X509_PropertyNotSettable, nameof(Archived)));
            }
        }

        public string FriendlyName
        {
            get { return ""; }
            set
            {
                throw new PlatformNotSupportedException(
                  SR.Format(SR.Cryptography_Unix_X509_PropertyNotSettable, nameof(FriendlyName)));
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                EnsureCertificateData();
                return _certData.Subject;
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                EnsureCertificateData();
                return _certData.Issuer;
            }
        }

        public PolicyData GetPolicyData()
        {
            EnsureCertificateData();
            PolicyData policyData = default;
            foreach (X509Extension extension in _certData.Extensions)
            {
                switch (extension.Oid!.Value)
                {
                    case Oids.ApplicationCertPolicies:
                        policyData.ApplicationCertPolicies = extension.RawData;
                        break;
                    case Oids.CertPolicies:
                        policyData.CertPolicies = extension.RawData;
                        break;
                    case Oids.CertPolicyMappings:
                        policyData.CertPolicyMappings = extension.RawData;
                        break;
                    case Oids.CertPolicyConstraints:
                        policyData.CertPolicyConstraints = extension.RawData;
                        break;
                    case Oids.EnhancedKeyUsage:
                        policyData.EnhancedKeyUsage = extension.RawData;
                        break;
                    case Oids.InhibitAnyPolicyExtension:
                        policyData.InhibitAnyPolicyExtension = extension.RawData;
                        break;
                }
            }

            return policyData;
        }

        public IEnumerable<X509Extension> Extensions
        {
            get
            {
                EnsureCertificateData();
                return _certData.Extensions;
            }
        }

        internal void SetPrivateKey(SafeEvpPKeyHandle privateKey)
        {
            _privateKey = privateKey;
        }

        internal SafeEvpPKeyHandle? PrivateKeyHandle
        {
            get { return _privateKey; }
        }

        public RSA? GetRSAPrivateKey()
        {
            throw new NotImplementedException(nameof(GetRSAPrivateKey));
        }

        public DSA? GetDSAPrivateKey()
        {
            throw new NotImplementedException(nameof(GetDSAPrivateKey));
        }

        public ECDsa GetECDsaPublicKey()
        {
            throw new NotImplementedException(nameof(GetECDsaPublicKey));
        }

        public ECDiffieHellman GetECDiffieHellmanPublicKey()
        {
            throw new NotImplementedException(nameof(GetECDiffieHellmanPublicKey));
        }

        public ECDsa? GetECDsaPrivateKey()
        {
            throw new NotImplementedException(nameof(GetECDsaPrivateKey));
        }

        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            throw new NotImplementedException(nameof(GetECDiffieHellmanPrivateKey));
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            throw new NotImplementedException($"{nameof(CopyWithPrivateKey)}(DSA)");
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            throw new NotImplementedException($"{nameof(CopyWithPrivateKey)}(ECDsa)");
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            throw new NotImplementedException($"{nameof(CopyWithPrivateKey)}(ECDiffieHellman)");
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            throw new NotImplementedException($"{nameof(CopyWithPrivateKey)}(RSA)");
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            EnsureCertificateData();
            return _certData.GetNameInfo(nameType, forIssuer);
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
                _cert = null!;
            }
        }

        internal AndroidCertificatePal DuplicateHandles()
        {
            // Add a global reference to the underlying cert object.
            SafeX509Handle duplicateHandle = new SafeX509Handle(Interop.JObjectLifetime.NewGlobalReference(Handle));
            AndroidCertificatePal duplicate = new AndroidCertificatePal(duplicateHandle);

            if (_privateKey != null)
            {
                SafeEvpPKeyHandle keyHandle = _privateKey.DuplicateHandle();
                duplicate.SetPrivateKey(keyHandle);
            }

            return duplicate;
        }

        public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                byte[]? exported = storePal.Export(contentType, password);
                Debug.Assert(exported != null);
                return exported;
            }
        }

        private void EnsureCertificateData()
        {
            if (!_certData.Equals(default(CertificateData)))
                return;

            Debug.Assert(!_cert.IsInvalid);
            _certData = new CertificateData(RawData);
        }
    }
}
