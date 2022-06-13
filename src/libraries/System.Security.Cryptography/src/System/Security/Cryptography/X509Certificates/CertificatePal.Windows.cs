// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using SafePasswordHandle = Microsoft.Win32.SafeHandles.SafePasswordHandle;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class CertificatePal : IDisposable, ICertificatePal
    {
        private SafeCertContextHandle _certContext;

        internal static partial ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            SafeCertContextHandle safeCertContextHandle = Interop.Crypt32.CertDuplicateCertificateContext(handle);
            if (safeCertContextHandle.IsInvalid)
                throw ErrorCode.HRESULT_INVALID_HANDLE.ToCryptographicException();

            int cbData = 0;
            bool deleteKeyContainer = Interop.Crypt32.CertGetCertificateContextProperty(safeCertContextHandle, Interop.Crypt32.CertContextPropId.CERT_CLR_DELETE_KEY_PROP_ID, out Interop.Crypt32.DATA_BLOB _, ref cbData);
            return new CertificatePal(safeCertContextHandle, deleteKeyContainer);
        }

        /// <summary>
        /// Returns the SafeCertContextHandle. Use this instead of FromHandle() when
        /// creating another X509Certificate object based on this one to ensure the underlying
        /// cert context is not released at the wrong time.
        /// </summary>
        internal static partial ICertificatePal FromOtherCert(X509Certificate copyFrom)
        {
            CertificatePal pal = new CertificatePal((CertificatePal)copyFrom.Pal!);
            return pal;
        }

        public IntPtr Handle
        {
            get { return _certContext.DangerousGetHandle(); }
        }

        public string Issuer => GetIssuerOrSubject(issuer: true, reverse: true);

        public string Subject => GetIssuerOrSubject(issuer: false, reverse: true);

        public string LegacyIssuer => GetIssuerOrSubject(issuer: true, reverse: false);

        public string LegacySubject => GetIssuerOrSubject(issuer: false, reverse: false);

        public byte[] Thumbprint
        {
            get
            {
                int cbData = 0;
                if (!Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_SHA1_HASH_PROP_ID, null, ref cbData))
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                byte[] thumbprint = new byte[cbData];
                if (!Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_SHA1_HASH_PROP_ID, thumbprint, ref cbData))
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                return thumbprint;
            }
        }

        public string KeyAlgorithm
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    string keyAlgorithm = Marshal.PtrToStringAnsi(pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId)!;
                    GC.KeepAlive(this);
                    return keyAlgorithm;
                }
            }
        }

        public byte[] KeyAlgorithmParameters
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    string keyAlgorithmOid = Marshal.PtrToStringAnsi(pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.pszObjId)!;

                    int algId;
                    if (keyAlgorithmOid == Oids.Rsa)
                        algId = AlgId.CALG_RSA_KEYX;  // Fast-path for the most common case.
                    else
                        algId = Interop.Crypt32.FindOidInfo(Interop.Crypt32.CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, keyAlgorithmOid, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId;

                    unsafe
                    {
                        byte* NULL_ASN_TAG = (byte*)0x5;

                        byte[] keyAlgorithmParameters;

                        if (algId == AlgId.CALG_DSS_SIGN
                            && pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.cbData == 0
                            && pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.pbData.ToPointer() == NULL_ASN_TAG)
                        {
                            //
                            // DSS certificates may not have the DSS parameters in the certificate. In this case, we try to build
                            // the certificate chain and propagate the parameters down from the certificate chain.
                            //
                            keyAlgorithmParameters = PropagateKeyAlgorithmParametersFromChain();
                        }
                        else
                        {
                            keyAlgorithmParameters = pCertContext->pCertInfo->SubjectPublicKeyInfo.Algorithm.Parameters.ToByteArray();
                        }

                        GC.KeepAlive(this);
                        return keyAlgorithmParameters;
                    }
                }
            }
        }

        private byte[] PropagateKeyAlgorithmParametersFromChain()
        {
            unsafe
            {
                SafeX509ChainHandle? certChainContext = null;
                try
                {
                    int cbData = 0;
                    if (!Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref cbData))
                    {
                        Interop.Crypt32.CERT_CHAIN_PARA chainPara = default;
                        chainPara.cbSize = sizeof(Interop.Crypt32.CERT_CHAIN_PARA);
                        if (!Interop.Crypt32.CertGetCertificateChain((IntPtr)Interop.Crypt32.ChainEngine.HCCE_CURRENT_USER, _certContext, null, SafeCertStoreHandle.InvalidHandle, ref chainPara, Interop.Crypt32.CertChainFlags.None, IntPtr.Zero, out certChainContext))
                            throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                        if (!Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, null, ref cbData))
                            throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                    }

                    byte[] keyAlgorithmParameters = new byte[cbData];
                    if (!Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_PUBKEY_ALG_PARA_PROP_ID, keyAlgorithmParameters, ref cbData))
                        throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                    return keyAlgorithmParameters;
                }
                finally
                {
                    if (certChainContext != null)
                        certChainContext.Dispose();
                }
            }
        }

        public byte[] PublicKeyValue
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    byte[] publicKey = pCertContext->pCertInfo->SubjectPublicKeyInfo.PublicKey.ToByteArray();
                    GC.KeepAlive(this);
                    return publicKey;
                }
            }
        }

        public byte[] SerialNumber
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    byte[] serialNumber = pCertContext->pCertInfo->SerialNumber.ToByteArray();
                    Array.Reverse(serialNumber);
                    GC.KeepAlive(this);
                    return serialNumber;
                }
            }
        }

        public string SignatureAlgorithm
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    string signatureAlgorithm = Marshal.PtrToStringAnsi(pCertContext->pCertInfo->SignatureAlgorithm.pszObjId)!;
                    GC.KeepAlive(this);
                    return signatureAlgorithm;
                }
            }
        }

        public DateTime NotAfter
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    DateTime notAfter = pCertContext->pCertInfo->NotAfter.ToDateTime();
                    GC.KeepAlive(this);
                    return notAfter;
                }
            }
        }

        public DateTime NotBefore
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    DateTime notBefore = pCertContext->pCertInfo->NotBefore.ToDateTime();
                    GC.KeepAlive(this);
                    return notBefore;
                }
            }
        }

        public byte[] RawData
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    byte[] rawData = new Span<byte>(pCertContext->pbCertEncoded, pCertContext->cbCertEncoded).ToArray();
                    GC.KeepAlive(this);
                    return rawData;
                }
            }
        }

        public int Version
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_CONTEXT* pCertContext = _certContext.CertContext;
                    int version = pCertContext->pCertInfo->dwVersion + 1;
                    GC.KeepAlive(this);
                    return version;
                }
            }
        }

        public bool Archived
        {
            get
            {
                int uninteresting = 0;
                bool archivePropertyExists = Interop.Crypt32.CertGetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_ARCHIVED_PROP_ID, null!, ref uninteresting);
                return archivePropertyExists;
            }

            set
            {
                unsafe
                {
                    Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0);
                    Interop.Crypt32.DATA_BLOB* pValue = value ? &blob : (Interop.Crypt32.DATA_BLOB*)null;
                    if (!Interop.Crypt32.CertSetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_ARCHIVED_PROP_ID, Interop.Crypt32.CertSetPropertyFlags.None, pValue))
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                }
            }
        }

        public string FriendlyName
        {
            get
            {
                unsafe
                {
                    uint cbData = 0;
                    if (!Interop.Crypt32.CertGetCertificateContextPropertyString(_certContext, Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, null, ref cbData))
                        return string.Empty;

                    uint spanLength = (cbData + 1) / 2;
                    Span<char> buffer = spanLength <= 256 ?
                        stackalloc char[(int)spanLength] : // Already checked to be a size that won't overflow.
                        new char[spanLength];
                    fixed (char* ptr = &MemoryMarshal.GetReference(buffer))
                    {
                        if (!Interop.Crypt32.CertGetCertificateContextPropertyString(_certContext, Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, (byte*)ptr, ref cbData))
                            return string.Empty;
                    }

                    return new string(buffer.Slice(0, ((int)cbData / 2) - 1));
                }
            }

            set
            {
                string friendlyName = value ?? string.Empty;
                unsafe
                {
                    IntPtr pFriendlyName = Marshal.StringToHGlobalUni(friendlyName);
                    try
                    {
                        Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(pFriendlyName, checked(2 * ((uint)friendlyName.Length + 1)));
                        if (!Interop.Crypt32.CertSetCertificateContextProperty(_certContext, Interop.Crypt32.CertContextPropId.CERT_FRIENDLY_NAME_PROP_ID, Interop.Crypt32.CertSetPropertyFlags.None, &blob))
                            throw Marshal.GetLastWin32Error().ToCryptographicException();
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pFriendlyName);
                    }
                }
            }
        }

        public X500DistinguishedName SubjectName
        {
            get
            {
                unsafe
                {
                    byte[] encodedSubjectName = _certContext.CertContext->pCertInfo->Subject.ToByteArray();
                    X500DistinguishedName subjectName = new X500DistinguishedName(encodedSubjectName);
                    GC.KeepAlive(this);
                    return subjectName;
                }
            }
        }

        public X500DistinguishedName IssuerName
        {
            get
            {
                unsafe
                {
                    byte[] encodedIssuerName = _certContext.CertContext->pCertInfo->Issuer.ToByteArray();
                    X500DistinguishedName issuerName = new X500DistinguishedName(encodedIssuerName);
                    GC.KeepAlive(this);
                    return issuerName;
                }
            }
        }

        public PolicyData GetPolicyData()
        {
            throw new PlatformNotSupportedException();
        }

        public IEnumerable<X509Extension> Extensions
        {
            get
            {
                unsafe
                {
                    Interop.Crypt32.CERT_INFO* pCertInfo = _certContext.CertContext->pCertInfo;
                    int numExtensions = pCertInfo->cExtension;
                    X509Extension[] extensions = new X509Extension[numExtensions];
                    for (int i = 0; i < numExtensions; i++)
                    {
                        Interop.Crypt32.CERT_EXTENSION* pCertExtension = (Interop.Crypt32.CERT_EXTENSION*)pCertInfo->rgExtension.ToPointer() + i;
                        string oidValue = Marshal.PtrToStringAnsi(pCertExtension->pszObjId)!;
                        Oid oid = new Oid(oidValue, friendlyName: null);
                        bool critical = pCertExtension->fCritical != 0;
                        byte[] rawData = pCertExtension->Value.ToByteArray();

                        extensions[i] = new X509Extension(oid, rawData, critical);
                    }
                    GC.KeepAlive(this);
                    return extensions;
                }
            }
        }

        public unsafe string GetNameInfo(X509NameType nameType, bool forIssuer) =>
            Interop.crypt32.CertGetNameString(
                _certContext,
                MapNameType(nameType),
                forIssuer ? Interop.Crypt32.CertNameFlags.CERT_NAME_ISSUER_FLAG : Interop.Crypt32.CertNameFlags.None,
                Interop.Crypt32.CertNameStringType.CERT_X500_NAME_STR | Interop.Crypt32.CertNameStringType.CERT_NAME_STR_REVERSE_FLAG);

        public void AppendPrivateKeyInfo(StringBuilder sb)
        {
            if (!HasPrivateKey)
            {
                return;
            }

            // UWP, Windows CNG persisted, and Windows Ephemeral keys will all acknowledge that
            // a private key exists, but detailed printing is limited to Windows CAPI persisted.
            // (This is the same thing we do in Unix)
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("[Private Key]");

            CspKeyContainerInfo? cspKeyContainerInfo = null;
            try
            {
                CspParameters? parameters = GetPrivateKeyCsp();

                if (parameters != null)
                {
                    cspKeyContainerInfo = new CspKeyContainerInfo(parameters);
                }
            }
            // We could not access the key container. Just return.
            catch (CryptographicException) { }

            // Ephemeral keys will not have container information.
            if (cspKeyContainerInfo == null)
                return;

            sb.AppendLine().Append("  Key Store: ").Append(cspKeyContainerInfo.MachineKeyStore ? "Machine" : "User");
            sb.AppendLine().Append("  Provider Name: ").Append(cspKeyContainerInfo.ProviderName);
            sb.AppendLine().Append("  Provider type: ").Append(cspKeyContainerInfo.ProviderType);
            sb.AppendLine().Append("  Key Spec: ").Append(cspKeyContainerInfo.KeyNumber);
            sb.AppendLine().Append("  Key Container Name: ").Append(cspKeyContainerInfo.KeyContainerName);

            try
            {
                string uniqueKeyContainer = cspKeyContainerInfo.UniqueKeyContainerName;
                sb.AppendLine().Append("  Unique Key Container Name: ").Append(uniqueKeyContainer);
            }
            catch (CryptographicException) { }
            catch (NotSupportedException) { }

            try
            {
                bool b = cspKeyContainerInfo.HardwareDevice;
                sb.AppendLine().Append("  Hardware Device: ").Append(b);
            }
            catch (CryptographicException) { }

            try
            {
                bool b = cspKeyContainerInfo.Removable;
                sb.AppendLine().Append("  Removable: ").Append(b);
            }
            catch (CryptographicException) { }

            try
            {
                bool b = cspKeyContainerInfo.Protected;
                sb.AppendLine().Append("  Protected: ").Append(b);
            }
            catch (CryptographicException) { }
            catch (NotSupportedException) { }
        }

        public void Dispose()
        {
            SafeCertContextHandle certContext = _certContext;
            _certContext = null!;
            if (certContext != null && !certContext.IsInvalid)
            {
                certContext.Dispose();
            }
        }

        internal SafeCertContextHandle CertContext
        {
            get
            {
                SafeCertContextHandle certContext = Interop.Crypt32.CertDuplicateCertificateContext(_certContext.DangerousGetHandle());
                GC.KeepAlive(_certContext);
                return certContext;
            }
        }

        private static Interop.Crypt32.CertNameType MapNameType(X509NameType nameType)
        {
            switch (nameType)
            {
                case X509NameType.SimpleName:
                    return Interop.Crypt32.CertNameType.CERT_NAME_SIMPLE_DISPLAY_TYPE;

                case X509NameType.EmailName:
                    return Interop.Crypt32.CertNameType.CERT_NAME_EMAIL_TYPE;

                case X509NameType.UpnName:
                    return Interop.Crypt32.CertNameType.CERT_NAME_UPN_TYPE;

                case X509NameType.DnsName:
                case X509NameType.DnsFromAlternativeName:
                    return Interop.Crypt32.CertNameType.CERT_NAME_DNS_TYPE;

                case X509NameType.UrlName:
                    return Interop.Crypt32.CertNameType.CERT_NAME_URL_TYPE;

                default:
                    throw new ArgumentException(SR.Argument_InvalidNameType);
            }
        }

        private unsafe string GetIssuerOrSubject(bool issuer, bool reverse) =>
            Interop.crypt32.CertGetNameString(
                _certContext,
                Interop.Crypt32.CertNameType.CERT_NAME_RDN_TYPE,
                issuer ? Interop.Crypt32.CertNameFlags.CERT_NAME_ISSUER_FLAG : Interop.Crypt32.CertNameFlags.None,
                Interop.Crypt32.CertNameStringType.CERT_X500_NAME_STR | (reverse ? Interop.Crypt32.CertNameStringType.CERT_NAME_STR_REVERSE_FLAG : 0));

        private CertificatePal(CertificatePal copyFrom)
        {
            // Use _certContext (instead of CertContext) to keep the original context handle from being
            // finalized until all cert copies are no longer referenced.
            _certContext = new SafeCertContextHandle(copyFrom._certContext);
        }

        private CertificatePal(SafeCertContextHandle certContext, bool deleteKeyContainer)
        {
            if (deleteKeyContainer)
            {
                // We need to delete any associated key container upon disposition. Thus, replace the safehandle we got with a safehandle whose
                // Release() method performs the key container deletion.
                SafeCertContextHandle oldCertContext = certContext;
                certContext = Interop.Crypt32.CertDuplicateCertificateContextWithKeyContainerDeletion(oldCertContext.DangerousGetHandle());
                GC.KeepAlive(oldCertContext);
            }
            _certContext = certContext;
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
    }
}
