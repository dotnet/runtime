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
    // TODO: [AndroidCrypto] Rename class to AndroidX509CertificateReader
    internal sealed class OpenSslX509CertificateReader : ICertificatePal
    {
        private SafeX509Handle _cert;
        private SafeEvpPKeyHandle? _privateKey;
        private X500DistinguishedName? _subjectName;
        private X500DistinguishedName? _issuerName;
        private string? _subject;
        private string? _issuer;
        private Interop.AndroidCrypto.X509BasicInformation _basicInformation;

        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            return new OpenSslX509CertificateReader(Interop.Crypto.X509UpRef(handle));
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            // Ensure private key is copied
            OpenSslX509CertificateReader certPal = (OpenSslX509CertificateReader)cert.Pal;
            return certPal.DuplicateHandles();
        }

        public static ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);
            ICertificatePal? cert;
            Exception? exception;
            if (TryReadX509(rawData, out cert)
                || PkcsFormatReader.TryReadPkcs7Der(rawData, out cert)
                || PkcsFormatReader.TryReadPkcs7Pem(rawData, out cert)
                || PkcsFormatReader.TryReadPkcs12(rawData, password, out cert, out exception))
            {
                if (cert == null)
                {
                    // Empty collection, most likely.
                    throw new CryptographicException();
                }

                return cert;
            }

            // Unsupported
            Debug.Assert(exception != null);
            throw exception;
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            return FromBlob(fileBytes, password, keyStorageFlags);
        }

        // Handles both DER and PEM
        internal static bool TryReadX509(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out ICertificatePal? handle)
        {
            handle = null;
            SafeX509Handle certHandle = Interop.AndroidCrypto.DecodeX509(
                ref MemoryMarshal.GetReference(rawData),
                rawData.Length);

            if (certHandle.IsInvalid)
            {
                certHandle.Dispose();
                return false;
            }

            handle = new OpenSslX509CertificateReader(certHandle);
            return true;
        }

        internal static bool TryReadX509Der(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out ICertificatePal? certPal)
        {
            return TryReadX509(rawData, out certPal);
        }

        internal static bool TryReadX509Pem(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out ICertificatePal? certPal)
        {
            return TryReadX509(rawData, out certPal);
        }

        internal static bool TryReadX509Der(SafeBioHandle bio, [NotNullWhen(true)] out ICertificatePal? certPal)
        {
            certPal = null;
            throw new NotImplementedException(nameof(TryReadX509Der));
        }

        internal static bool TryReadX509Pem(SafeBioHandle bio, [NotNullWhen(true)] out ICertificatePal? certPal)
        {
            certPal = null;
            throw new NotImplementedException(nameof(TryReadX509Pem));
        }

        internal static bool TryReadX509PemNoAux(SafeBioHandle bio, [NotNullWhen(true)] out ICertificatePal? certPal)
        {
            certPal = null;
            throw new NotImplementedException(nameof(TryReadX509PemNoAux));
        }

        internal static void RewindBio(SafeBioHandle bio, int bioPosition)
        {
            throw new NotImplementedException(nameof(RewindBio));
        }

        private OpenSslX509CertificateReader(SafeX509Handle handle)
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
                if (_issuer == null)
                {
                    // IssuerName is mutable to callers in X509Certificate. We want to be
                    // able to get the issuer even if IssuerName has been mutated, so we
                    // don't use it here.
                    _issuer = Interop.AndroidCrypto.X509GetIssuerName(_cert).Name;
                }

                return _issuer;
            }
        }

        public string Subject
        {
            get
            {
                if (_subject == null)
                {
                    // SubjectName is mutable to callers in X509Certificate. We want to be
                    // able to get the subject even if SubjectName has been mutated, so we
                    // don't use it here.
                    _subject = Interop.AndroidCrypto.X509GetSubjectName(_cert).Name;
                }

                return _subject;
            }
        }

        public string LegacyIssuer => IssuerName.Decode(X500DistinguishedNameFlags.None);

        public string LegacySubject => SubjectName.Decode(X500DistinguishedNameFlags.None);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required for Compat")]
        public byte[] Thumbprint => SHA1.HashData(RawData);

        public string KeyAlgorithm => new Oid(Interop.AndroidCrypto.X509GetPublicKeyAlgorithm(_cert)).Value!;

        public byte[] KeyAlgorithmParameters => Interop.AndroidCrypto.X509GetPublicKeyParameterBytes(_cert);

        public byte[] PublicKeyValue
        {
            get
            {
                // AndroidCrypto returns the SubjectPublicKeyInfo - extract just the SubjectPublicKey
                byte[] bytes = Interop.AndroidCrypto.X509GetPublicKeyBytes(_cert);
                return SubjectPublicKeyInfoAsn.Decode(bytes, AsnEncodingRules.DER).SubjectPublicKey.ToArray();
            }
        }

        public byte[] SerialNumber => Interop.AndroidCrypto.X509GetSerialNumber(_cert);

        public string SignatureAlgorithm => Interop.AndroidCrypto.X509GetSignatureAlgorithm(_cert);

        public DateTime NotAfter
        {
            get
            {
                EnsureBasicInformation();
                return _basicInformation.NotAfter;
            }
        }

        public DateTime NotBefore
        {
            get
            {
                EnsureBasicInformation();
                return _basicInformation.NotBefore;
            }
        }

        public byte[] RawData => Interop.AndroidCrypto.EncodeX509(_cert);

        public int Version
        {
            get
            {
                EnsureBasicInformation();
                return _basicInformation.Version;
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
                if (_subjectName == null)
                {
                    _subjectName = Interop.AndroidCrypto.X509GetSubjectName(_cert);
                }

                return _subjectName;
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                if (_issuerName == null)
                {
                    _issuerName = Interop.AndroidCrypto.X509GetIssuerName(_cert);
                }

                return _issuerName;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void EnumExtensionsPolicyDataCallback(byte* oid, int oidLen, byte* data, int dataLen, byte isCritical, void* context)
        {
            ref PolicyData policyData = ref Unsafe.As<byte, PolicyData>(ref *(byte*)context);
            string oidStr = Encoding.UTF8.GetString(oid, oidLen);

            ReadOnlySpan<byte> rawData;
            if (!AsnDecoder.TryReadPrimitiveOctetString(new ReadOnlySpan<byte>(data, dataLen), AsnEncodingRules.DER, out rawData, out _))
            {
                Debug.Fail($"{nameof(AsnDecoder.TryReadPrimitiveOctetString)} can't return false for DER inputs");
                return;
            }

            switch (oidStr)
            {
                case Oids.ApplicationCertPolicies:
                    policyData.ApplicationCertPolicies = rawData.ToArray();
                    break;
                case Oids.CertPolicies:
                    policyData.CertPolicies = rawData.ToArray();
                    break;
                case Oids.CertPolicyMappings:
                    policyData.CertPolicyMappings = rawData.ToArray();
                    break;
                case Oids.CertPolicyConstraints:
                    policyData.CertPolicyConstraints = rawData.ToArray();
                    break;
                case Oids.EnhancedKeyUsage:
                    policyData.EnhancedKeyUsage = rawData.ToArray();
                    break;
                case Oids.InhibitAnyPolicyExtension:
                    policyData.InhibitAnyPolicyExtension = rawData.ToArray();
                    break;
            }
        }

        public PolicyData GetPolicyData()
        {
            PolicyData policyData = default;
            unsafe
            {
                Interop.AndroidCrypto.X509EnumExtensions(_cert, &EnumExtensionsPolicyDataCallback, Unsafe.AsPointer(ref policyData));
            }

            return policyData;
        }

        private struct EnumExtensionsContext
        {
            public List<X509Extension> Results;
        }

        [UnmanagedCallersOnly]
        private static unsafe void EnumExtensionsCallback(byte* oid, int oidLen, byte* data, int dataLen, byte isCritical, void* context)
        {
            ref EnumExtensionsContext callbackContext = ref Unsafe.As<byte, EnumExtensionsContext>(ref *(byte*)context);
            string oidStr = Encoding.UTF8.GetString(oid, oidLen);
            byte[] rawData = AsnDecoder.ReadOctetString(new ReadOnlySpan<byte>(data, dataLen), AsnEncodingRules.DER, out _);
            bool critical = isCritical != 0;
            callbackContext.Results.Add(new X509Extension(new Oid(oidStr), rawData, critical));
        }

        // TODO: [AndroidCrypto] This is returning critical exceptions followed by non-critical, rather than in their declared order.
        // Might have to switch to using CertificateAsn for parsing the raw data.
        public IEnumerable<X509Extension> Extensions
        {
            get
            {
                EnumExtensionsContext context = default;
                context.Results = new List<X509Extension>();
                unsafe
                {
                    Interop.AndroidCrypto.X509EnumExtensions(_cert, &EnumExtensionsCallback, Unsafe.AsPointer(ref context));
                }

                return context.Results;
            }
        }

        internal static ArraySegment<byte> FindFirstExtension(SafeX509Handle cert, string oidValue)
        {
            return Interop.AndroidCrypto.X509FindExtensionData(cert, oidValue);
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
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new RSAOpenSsl(_privateKey);
        }

        public DSA? GetDSAPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new DSAOpenSsl(_privateKey);
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
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new ECDsaOpenSsl(_privateKey);
        }

        public ECDiffieHellman? GetECDiffieHellmanPrivateKey()
        {
            if (_privateKey == null || _privateKey.IsInvalid)
            {
                return null;
            }

            return new ECDiffieHellmanOpenSsl(_privateKey);
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
            throw new NotImplementedException(nameof(GetNameInfo));
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

        internal OpenSslX509CertificateReader DuplicateHandles()
        {
            // Add a global reference to the underlying cert object.
            SafeX509Handle certHandle = Interop.Crypto.X509UpRef(_cert);
            OpenSslX509CertificateReader duplicate = new OpenSslX509CertificateReader(certHandle);

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

        internal static DateTime ExtractValidityDateTime(IntPtr validityDatePtr)
        {
            throw new NotImplementedException(nameof(ExtractValidityDateTime));
        }

        private void EnsureBasicInformation()
        {
            if (!_basicInformation.Equals(default(Interop.AndroidCrypto.X509BasicInformation)))
                return;

            _basicInformation = Interop.AndroidCrypto.X509GetBasicInformation(_cert);
        }
    }
}
