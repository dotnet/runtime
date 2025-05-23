// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using SafeJObjectHandle = Interop.JObjectLifetime.SafeJObjectHandle;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class AndroidCertificatePal : ICertificatePal
    {
        private SafeX509Handle _cert;
        private SafeKeyHandle? _privateKey;
        private SafeJObjectHandle? _keyStorePrivateKeyEntry;

        private CertificateData _certData;

        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            if (Interop.AndroidCrypto.IsKeyStorePrivateKeyEntry(handle))
            {
                SafeJObjectHandle newPrivateKeyEntryHandle = SafeJObjectHandle.CreateGlobalReferenceFromHandle(handle);
                return new AndroidCertificatePal(newPrivateKeyEntryHandle);
            }

            var newHandle = new SafeX509Handle();
            Marshal.InitHandle(newHandle, Interop.JObjectLifetime.NewGlobalReference(handle));
            return new AndroidCertificatePal(newHandle);
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            AndroidCertificatePal certPal = (AndroidCertificatePal)cert.Pal;

            if (certPal._keyStorePrivateKeyEntry is SafeJObjectHandle privateKeyEntry)
            {
                bool addedRef = false;
                try
                {
                    privateKeyEntry.DangerousAddRef(ref addedRef);
                    SafeJObjectHandle newSafeHandle = SafeJObjectHandle.CreateGlobalReferenceFromHandle(privateKeyEntry.DangerousGetHandle());
                    return new AndroidCertificatePal(newSafeHandle);
                }
                finally
                {
                    if (addedRef)
                    {
                        privateKeyEntry.DangerousRelease();
                    }
                }
            }

            // Ensure private key is copied
            if (certPal.PrivateKeyHandle != null)
            {
                return certPal.CopyWithPrivateKeyHandle(certPal.PrivateKeyHandle.DuplicateHandle());
            }

            var handle = new SafeX509Handle();
            Marshal.InitHandle(handle, Interop.JObjectLifetime.NewGlobalReference(certPal.Handle));
            return new AndroidCertificatePal(handle);
        }

        private static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, bool readingFromFile, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);

            switch (contentType)
            {
                case X509ContentType.Pkcs7:
                    // In single mode for a PKCS#7 signed or signed-and-enveloped file we're supposed to return
                    // the certificate which signed the PKCS#7 file.
                    // We don't support determining this on Android right now, so we throw.
                    throw new CryptographicException(SR.Cryptography_X509_PKCS7_NoSigner);
                case X509ContentType.Pkcs12:
                    try
                    {
                        return X509CertificateLoader.LoadPkcs12Pal(
                            rawData,
                            password.DangerousGetSpan(),
                            keyStorageFlags,
                            X509Certificate.GetPkcs12Limits(readingFromFile, password));
                    }
                    catch (Pkcs12LoadLimitExceededException e)
                    {
                        throw new CryptographicException(
                            SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded,
                            e);
                    }
                case X509ContentType.Cert:
                default:
                {
                    ICertificatePal? cert;
                    if (TryReadX509(rawData, out cert))
                    {
                        return cert;
                    }

                    break;
                }
            }

            // Unsupported
            throw new CryptographicException();
        }

        public static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlob(rawData, password, readingFromFile: false, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, readingFromFile: true, keyStorageFlags);
        }

        // Handles both DER and PEM
        internal static unsafe bool TryReadX509(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out ICertificatePal? handle)
        {
            if (rawData.IsEmpty)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Prevent Android PKCS7 content sniffing
            if (rawData[0] == 0x30)
            {
                fixed (byte* rawDataPtr = rawData)
                {
                    using (PointerMemoryManager<byte> manager = new(rawDataPtr, rawData.Length))
                    {
                        AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.DER);
                        CertificateAsn.Decode(ref reader, manager.Memory, out _);
                    }
                }
            }

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

        internal AndroidCertificatePal(SafeJObjectHandle handle)
        {
            _cert = Interop.AndroidCrypto.GetPrivateKeyEntryCertificate(handle);
            _keyStorePrivateKeyEntry = handle;
        }

        internal AndroidCertificatePal(SafeX509Handle handle)
        {
            _cert = handle;
        }

        internal AndroidCertificatePal(SafeX509Handle handle, SafeKeyHandle privateKey)
        {
            _cert = handle;
            _privateKey = privateKey;
        }

        public bool HasPrivateKey => _privateKey is not null || _keyStorePrivateKeyEntry is not null;

        public IntPtr Handle => _keyStorePrivateKeyEntry?.DangerousGetHandle()
            ?? _cert?.DangerousGetHandle()
            ?? IntPtr.Zero;

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

        public byte[]? KeyAlgorithmParameters
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

        internal void SetPrivateKey(SafeKeyHandle privateKey)
        {
            Debug.Assert(_privateKey == null);
            _privateKey = privateKey;
        }

        internal SafeKeyHandle? PrivateKeyHandle
        {
            get { return _privateKey; }
        }

        public RSA? GetRSAPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
                return null;

            SafeRsaHandle? rsaKey = _privateKey as SafeRsaHandle;
            if (rsaKey == null)
                throw new CryptographicException();

            return new RSAImplementation.RSAAndroid(rsaKey);
        }

        public DSA? GetDSAPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
                return null;

            SafeDsaHandle? dsaKey = _privateKey as SafeDsaHandle;
            if (dsaKey == null)
                throw new CryptographicException();

            return new DSAImplementation.DSAAndroid(dsaKey);
        }

        public ECDsa? GetECDsaPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
                return null;

            SafeEcKeyHandle? ecKey = _privateKey as SafeEcKeyHandle;
            if (ecKey == null)
                throw new CryptographicException();

            return new ECDsaImplementation.ECDsaAndroid(ecKey);
        }

        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
                return null;

            SafeEcKeyHandle? ecKey = _privateKey as SafeEcKeyHandle;
            if (ecKey == null)
                throw new CryptographicException();

            return new ECDiffieHellmanImplementation.ECDiffieHellmanAndroid(ecKey);
        }

        public MLDsa? GetMLDsaPrivateKey()
        {
            // MLDsa is not supported on Android
            return null;
        }

        public MLKem? GetMLKemPrivateKey()
        {
            // MLKem is not supported on Android
            return null;
        }

        public SlhDsa? GetSlhDsaPrivateKey()
        {
            // SlhDsa is not supported on Android
            return null;
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            DSAImplementation.DSAAndroid? typedKey = privateKey as DSAImplementation.DSAAndroid;
            if (typedKey != null)
            {
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }

            DSAParameters dsaParameters = privateKey.ExportParameters(true);
            using (PinAndClear.Track(dsaParameters.X!))
            using (typedKey = new DSAImplementation.DSAAndroid())
            {
                typedKey.ImportParameters(dsaParameters);
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            ECDsaImplementation.ECDsaAndroid? typedKey = privateKey as ECDsaImplementation.ECDsaAndroid;
            if (typedKey != null)
            {
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }

            ECParameters ecParameters = privateKey.ExportParameters(true);
            using (PinAndClear.Track(ecParameters.D!))
            using (typedKey = new ECDsaImplementation.ECDsaAndroid())
            {
                typedKey.ImportParameters(ecParameters);
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            ECDiffieHellmanImplementation.ECDiffieHellmanAndroid? typedKey = privateKey as ECDiffieHellmanImplementation.ECDiffieHellmanAndroid;
            if (typedKey != null)
            {
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }

            ECParameters ecParameters = privateKey.ExportParameters(true);
            using (PinAndClear.Track(ecParameters.D!))
            using (typedKey = new ECDiffieHellmanImplementation.ECDiffieHellmanAndroid())
            {
                typedKey.ImportParameters(ecParameters);
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }
        }

        public ICertificatePal CopyWithPrivateKey(MLDsa privateKey)
        {
            throw new PlatformNotSupportedException(
                SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLDsa)));
        }

        public ICertificatePal CopyWithPrivateKey(MLKem privateKey)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
        }

        public ICertificatePal CopyWithPrivateKey(SlhDsa privateKey)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            RSAImplementation.RSAAndroid? typedKey = privateKey as RSAImplementation.RSAAndroid;
            if (typedKey != null)
            {
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }

            RSAParameters rsaParameters = privateKey.ExportParameters(true);
            using (PinAndClear.Track(rsaParameters.D!))
            using (PinAndClear.Track(rsaParameters.P!))
            using (PinAndClear.Track(rsaParameters.Q!))
            using (PinAndClear.Track(rsaParameters.DP!))
            using (PinAndClear.Track(rsaParameters.DQ!))
            using (PinAndClear.Track(rsaParameters.InverseQ!))
            using (typedKey = new RSAImplementation.RSAAndroid())
            {
                typedKey.ImportParameters(rsaParameters);
                return CopyWithPrivateKeyHandle(typedKey.DuplicateKeyHandle());
            }
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

        public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                byte[]? exported = storePal.Export(contentType, password);
                Debug.Assert(exported != null);
                return exported;
            }
        }

        public byte[] ExportPkcs12(Pkcs12ExportPbeParameters exportParameters, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                return storePal.ExportPkcs12(exportParameters, password);
            }
        }

        public byte[] ExportPkcs12(PbeParameters exportParameters, SafePasswordHandle password)
        {
            using (IExportPal storePal = StorePal.FromCertificate(this))
            {
                return storePal.ExportPkcs12(exportParameters, password);
            }
        }

        private void EnsureCertificateData()
        {
            if (!_certData.Equals(default(CertificateData)))
                return;

            Debug.Assert(!_cert.IsInvalid);
            _certData = new CertificateData(RawData);
        }

        private AndroidCertificatePal CopyWithPrivateKeyHandle(SafeKeyHandle privateKey)
        {
            // Add a global reference to the underlying cert object.
            var handle = new SafeX509Handle();
            Marshal.InitHandle(handle, Interop.JObjectLifetime.NewGlobalReference(Handle));
            return new AndroidCertificatePal(handle, privateKey);
        }
    }
}
