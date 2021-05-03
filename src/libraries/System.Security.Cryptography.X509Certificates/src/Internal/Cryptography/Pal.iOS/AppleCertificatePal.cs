// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private SafeSecIdentityHandle? _identityHandle;
        private SafeSecCertificateHandle _certHandle;
        private CertificateData _certData;
        private bool _readCertData;

        public static ICertificatePal? FromHandle(IntPtr handle)
        {
            return FromHandle(handle, true);
        }

        internal static ICertificatePal? FromHandle(IntPtr handle, bool throwOnFail)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            SafeSecCertificateHandle certHandle;
            SafeSecIdentityHandle identityHandle;

            if (Interop.AppleCrypto.X509DemuxAndRetainHandle(handle, out certHandle, out identityHandle))
            {
                Debug.Assert(
                    certHandle.IsInvalid != identityHandle.IsInvalid,
                    $"certHandle.IsInvalid ({certHandle.IsInvalid}) should differ from identityHandle.IsInvalid ({identityHandle.IsInvalid})");

                if (certHandle.IsInvalid)
                {
                    certHandle.Dispose();
                    return new AppleCertificatePal(identityHandle);
                }

                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            certHandle.Dispose();
            identityHandle.Dispose();

            if (throwOnFail)
            {
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));
            }

            return null;
        }

        public static ICertificatePal? FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            ICertificatePal? pal = FromHandle(cert.Handle);
            GC.KeepAlive(cert); // ensure cert's safe handle isn't finalized while raw handle is in use
            return pal;
        }

        private static bool IsPkcs12(ReadOnlySpan<byte> rawData)
        {
            try
            {
                unsafe
                {
                    fixed (byte* pin = rawData)
                    {
                        using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                        {
                            PfxAsn.Decode(manager.Memory, AsnEncodingRules.BER);
                        }

                        return true;
                    }
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        private static bool IsPkcs7Signed(ReadOnlySpan<byte> rawData)
        {
            try
            {
                unsafe
                {
                    fixed (byte* pin = rawData)
                    {
                        using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                        {
                            AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.BER);

                            ContentInfoAsn.Decode(ref reader, manager.Memory, out ContentInfoAsn contentInfo);

                            switch (contentInfo.ContentType)
                            {
                                case Oids.Pkcs7Signed:
                                case Oids.Pkcs7SignedEnveloped:
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        internal static X509ContentType GetDerCertContentType(ReadOnlySpan<byte> rawData)
        {
            X509ContentType contentType = Interop.AppleCrypto.X509GetContentType(rawData);

            if (contentType == X509ContentType.Unknown)
            {
                if (IsPkcs12(rawData))
                {
                    return X509ContentType.Pkcs12;
                }

                if (IsPkcs7Signed(rawData))
                {
                    return X509ContentType.Pkcs7;
                }
            }

            return contentType;
        }

        private static ICertificatePal FromDerBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = GetDerCertContentType(rawData);

            if (contentType == X509ContentType.Pkcs7)
            {
                throw new CryptographicException(
                    SR.Cryptography_X509_PKCS7_Unsupported,
                    new PlatformNotSupportedException(SR.Cryptography_X509_PKCS7_Unsupported));
            }

            if (contentType == X509ContentType.Pkcs12)
            {
                // TODO: keyStorageFlags
                return ImportPkcs12(rawData, password);
            }

            SafeSecIdentityHandle identityHandle;
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                rawData,
                contentType,
                password,
                out identityHandle);

            if (identityHandle.IsInvalid)
            {
                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            Debug.Fail("Non-PKCS12 import produced an identity handle");

            identityHandle.Dispose();
            certHandle.Dispose();
            throw new CryptographicException();
        }

        public static ICertificatePal FromBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            ICertificatePal? result = null;
            TryDecodePem(
                rawData,
                derData =>
                {
                    result = FromDerBlob(derData, password, keyStorageFlags);
                    return false;
                });

            return result ?? FromDerBlob(rawData, password, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        internal AppleCertificatePal(SafeSecCertificateHandle certHandle)
        {
            Debug.Assert(!certHandle.IsInvalid);

            _certHandle = certHandle;
        }

        internal AppleCertificatePal(SafeSecIdentityHandle identityHandle)
        {
            Debug.Assert(!identityHandle.IsInvalid);

            _identityHandle = identityHandle;
            _certHandle = Interop.AppleCrypto.X509GetCertFromIdentity(identityHandle);
        }

        public void Dispose()
        {
            _certHandle?.Dispose();
            _identityHandle?.Dispose();

            _certHandle = null!;
            _identityHandle = null;
        }

        internal SafeSecCertificateHandle CertificateHandle => _certHandle;
        internal SafeSecIdentityHandle? IdentityHandle => _identityHandle;

        public bool HasPrivateKey => !(_identityHandle?.IsInvalid ?? true);

        public IntPtr Handle
        {
            get
            {
                if (HasPrivateKey)
                {
                    return _identityHandle!.DangerousGetHandle();
                }

                return _certHandle?.DangerousGetHandle() ?? IntPtr.Zero;
            }
        }

        public string Issuer
        {
            get
            {
                EnsureCertData();
                return _certData.IssuerName;
            }
        }

        public string Subject
        {
            get
            {
                EnsureCertData();
                return _certData.SubjectName;
            }
        }

        public string LegacyIssuer => IssuerName.Decode(X500DistinguishedNameFlags.None);

        public string LegacySubject => SubjectName.Decode(X500DistinguishedNameFlags.None);

        public string KeyAlgorithm
        {
            get
            {
                EnsureCertData();
                return _certData.PublicKeyAlgorithm.AlgorithmId!;
            }
        }

        public byte[] KeyAlgorithmParameters
        {
            get
            {
                EnsureCertData();
                return _certData.PublicKeyAlgorithm.Parameters;
            }
        }

        public byte[] PublicKeyValue
        {
            get
            {
                EnsureCertData();
                return _certData.PublicKey;
            }
        }

        public byte[] SerialNumber
        {
            get
            {
                EnsureCertData();
                return _certData.SerialNumber;
            }
        }

        public string SignatureAlgorithm
        {
            get
            {
                EnsureCertData();
                return _certData.SignatureAlgorithm.AlgorithmId!;
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

        public int Version
        {
            get
            {
                EnsureCertData();
                return _certData.Version + 1;
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                EnsureCertData();
                return _certData.Subject;
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                EnsureCertData();
                return _certData.Issuer;
            }
        }

        public PolicyData GetPolicyData()
        {
            PolicyData policyData = default;
            EnsureCertData();

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

        public IEnumerable<X509Extension> Extensions {
            get
            {
                EnsureCertData();
                return _certData.Extensions;
            }
        }

        public byte[] RawData
        {
            get
            {
                EnsureCertData();
                return _certData.RawData.CloneByteArray();
            }
        }

        public DateTime NotAfter
        {
            get
            {
                EnsureCertData();
                return _certData.NotAfter.ToLocalTime();
            }
        }

        public DateTime NotBefore
        {
            get
            {
                EnsureCertData();
                return _certData.NotBefore.ToLocalTime();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required for Compat")]
        public byte[] Thumbprint
        {
            get
            {
                EnsureCertData();
                return SHA1.HashData(_certData.RawData);
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

        public byte[] SubjectPublicKeyInfo
        {
            get
            {
                EnsureCertData();

                return _certData.SubjectPublicKeyInfo;
            }
        }

        public RSA? GetRSAPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            Debug.Assert(!_identityHandle.IsInvalid);
            SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.X509GetPublicKey(_certHandle);
            SafeSecKeyRefHandle privateKey = Interop.AppleCrypto.X509GetPrivateKeyFromIdentity(_identityHandle);
            Debug.Assert(!publicKey.IsInvalid);

            return new RSAImplementation.RSASecurityTransforms(publicKey, privateKey);
        }

        public DSA? GetDSAPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            throw new PlatformNotSupportedException();
        }

        public ECDsa? GetECDsaPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            Debug.Assert(!_identityHandle.IsInvalid);
            SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.X509GetPublicKey(_certHandle);
            SafeSecKeyRefHandle privateKey = Interop.AppleCrypto.X509GetPrivateKeyFromIdentity(_identityHandle);
            Debug.Assert(!publicKey.IsInvalid);

            return new ECDsaImplementation.ECDsaSecurityTransforms(publicKey, privateKey);
        }

        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            Debug.Assert(!_identityHandle.IsInvalid);
            SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.X509GetPublicKey(_certHandle);
            SafeSecKeyRefHandle privateKey = Interop.AppleCrypto.X509GetPrivateKeyFromIdentity(_identityHandle);
            Debug.Assert(!publicKey.IsInvalid);

            return new ECDiffieHellmanImplementation.ECDiffieHellmanSecurityTransforms(publicKey, privateKey);
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            //return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
            throw new PlatformNotSupportedException("TODO CopyWithPrivateKey");
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }

        public string GetNameInfo(X509NameType nameType, bool forIssuer)
        {
            EnsureCertData();
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

        public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                byte[]? exported = storePal.Export(contentType, password);
                Debug.Assert(exported != null);
                return exported;
            }
        }

        private void EnsureCertData()
        {
            if (_readCertData)
                return;

            Debug.Assert(!_certHandle.IsInvalid);
            _certData = new CertificateData(Interop.AppleCrypto.X509GetRawData(_certHandle));
            _readCertData = true;
        }
    }
}
