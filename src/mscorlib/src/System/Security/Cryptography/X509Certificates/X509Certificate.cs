// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.X509Certificates {
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Text;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509ContentType {
        Unknown         = 0x00,
        Cert            = 0x01,
        SerializedCert  = 0x02,
        Pfx             = 0x03,
        Pkcs12          = Pfx,
        SerializedStore = 0x04, 
        Pkcs7           = 0x05,
        Authenticode    = 0x06
    }

    // DefaultKeySet, UserKeySet and MachineKeySet are mutually exclusive
[Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum X509KeyStorageFlags {
        DefaultKeySet = 0x00,
        UserKeySet    = 0x01,
        MachineKeySet = 0x02,
        Exportable    = 0x04,
        UserProtected = 0x08,
        PersistKeySet = 0x10
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class X509Certificate :
        IDisposable,
        IDeserializationCallback,
        ISerializable {
        private const string m_format = "X509";
        private string m_subjectName;
        private string m_issuerName;
        private byte[] m_serialNumber;
        private byte[] m_publicKeyParameters;
        private byte[] m_publicKeyValue;
        private string m_publicKeyOid;
        private byte[] m_rawData;
        private byte[] m_thumbprint;
        private DateTime m_notBefore;
        private DateTime m_notAfter;
        [System.Security.SecurityCritical] // auto-generated
        private SafeCertContextHandle m_safeCertContext;
        private bool m_certContextCloned = false;

        //
        // public constructors
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        private void Init()
        {
            m_safeCertContext = SafeCertContextHandle.InvalidHandle;
        }
        public X509Certificate () 
        {
            Init();
        }

        public X509Certificate (byte[] data):this() {
            if ((data != null) && (data.Length != 0))
                LoadCertificateFromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate (byte[] rawData, string password):this() {
#if FEATURE_LEGACYNETCF
            if ((rawData != null) && (rawData.Length != 0)) {
#endif
                LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
#if FEATURE_LEGACYNETCF
            }
#endif
        }

#if FEATURE_X509_SECURESTRINGS
        public X509Certificate (byte[] rawData, SecureString password):this() {
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
        }
#endif // FEATURE_X509_SECURESTRINGS

        public X509Certificate (byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags):this() {
#if FEATURE_LEGACYNETCF
            if ((rawData != null) && (rawData.Length != 0)) {
#endif
                LoadCertificateFromBlob(rawData, password, keyStorageFlags);
#if FEATURE_LEGACYNETCF
            }
#endif
        }

#if FEATURE_X509_SECURESTRINGS
        public X509Certificate (byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags):this() {
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }
#endif // FEATURE_X509_SECURESTRINGS

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public X509Certificate (string fileName):this() {
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public X509Certificate (string fileName, string password):this() {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }

#if FEATURE_X509_SECURESTRINGS
        [System.Security.SecuritySafeCritical]  // auto-generated
        public X509Certificate (string fileName, SecureString password):this() {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }
#endif // FEATURE_X509_SECURESTRINGS

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public X509Certificate (string fileName, string password, X509KeyStorageFlags keyStorageFlags):this() {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

#if FEATURE_X509_SECURESTRINGS
        [System.Security.SecuritySafeCritical]  // auto-generated
        public X509Certificate (string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags):this() {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }
#endif // FEATURE_X509_SECURESTRINGS

        // Package protected constructor for creating a certificate from a PCCERT_CONTEXT
        [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
        [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
        public X509Certificate (IntPtr handle):this() {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHandle"), "handle");
            Contract.EndContractBlock();

            X509Utils._DuplicateCertContext(handle, ref m_safeCertContext);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public X509Certificate (X509Certificate cert):this() {
            if (cert == null)
                throw new ArgumentNullException("cert");
            Contract.EndContractBlock();

            if (cert.m_safeCertContext.pCertContext != IntPtr.Zero) {
                m_safeCertContext = cert.GetCertContextForCloning();
                m_certContextCloned = true;
            }
        }

        public X509Certificate (SerializationInfo info, StreamingContext context):this() {
            byte[] rawData = (byte[]) info.GetValue("RawData", typeof(byte[]));
            if (rawData != null)
                LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public static X509Certificate CreateFromCertFile (string filename) {
            return new X509Certificate(filename);
        }

        public static X509Certificate CreateFromSignedFile (string filename) {
            return new X509Certificate(filename);
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public IntPtr Handle {
            [System.Security.SecurityCritical]  // auto-generated_required
#if !FEATURE_CORECLR
            [SecurityPermissionAttribute(SecurityAction.InheritanceDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
            get {
                return m_safeCertContext.pCertContext;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method has been deprecated.  Please use the Subject property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetName() {
            ThrowIfContextInvalid();

            return X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method has been deprecated.  Please use the Issuer property instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual string GetIssuerName() {
            ThrowIfContextInvalid();

            return X509Utils._GetIssuerName(m_safeCertContext, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual byte[] GetSerialNumber() {
            ThrowIfContextInvalid();

            if (m_serialNumber == null)
                m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
            return (byte[]) m_serialNumber.Clone();
        }

        public virtual string GetSerialNumberString() {
            return SerialNumber;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual byte[] GetKeyAlgorithmParameters() {
            ThrowIfContextInvalid();

            if (m_publicKeyParameters == null)
                m_publicKeyParameters = X509Utils._GetPublicKeyParameters(m_safeCertContext);

            return (byte[]) m_publicKeyParameters.Clone();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual string GetKeyAlgorithmParametersString() {
            ThrowIfContextInvalid();

            return Hex.EncodeHexString(GetKeyAlgorithmParameters());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual string GetKeyAlgorithm() {
            ThrowIfContextInvalid();

            if (m_publicKeyOid == null)
                m_publicKeyOid = X509Utils._GetPublicKeyOid(m_safeCertContext);

            return m_publicKeyOid;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual byte[] GetPublicKey() {
            ThrowIfContextInvalid();

            if (m_publicKeyValue == null)
                m_publicKeyValue = X509Utils._GetPublicKeyValue(m_safeCertContext);

            return (byte[]) m_publicKeyValue.Clone();
        }

        public virtual string GetPublicKeyString() {
            return Hex.EncodeHexString(GetPublicKey());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual byte[] GetRawCertData() {
            return RawData;
        }

        public virtual string GetRawCertDataString() {
            return Hex.EncodeHexString(GetRawCertData());
        }

        public virtual byte[] GetCertHash() {
            SetThumbprint();
            return (byte[]) m_thumbprint.Clone();
        }

        public virtual string GetCertHashString() {
            SetThumbprint();
            return Hex.EncodeHexString(m_thumbprint);
        }

        public virtual string GetEffectiveDateString() {
            return NotBefore.ToString();
        }

        public virtual string GetExpirationDateString() {
            return NotAfter.ToString();
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals (Object obj) {
            if (!(obj is X509Certificate)) return false;
            X509Certificate other = (X509Certificate) obj;
            return this.Equals(other);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual bool Equals (X509Certificate other) {
            if (other == null)
                return false;

            if (m_safeCertContext.IsInvalid)
                return other.m_safeCertContext.IsInvalid;

            if (!this.Issuer.Equals(other.Issuer))
                return false;

            if (!this.SerialNumber.Equals(other.SerialNumber))
                return false;

            return true;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int GetHashCode() {
            if (m_safeCertContext.IsInvalid)
                return 0;

            SetThumbprint();
            int value = 0;
            for (int i = 0; i < m_thumbprint.Length && i < 4; ++i) {
                value = value << 8 | m_thumbprint[i];
            }
            return value;
        }

        public override string ToString() {
            return ToString(false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual string ToString (bool fVerbose) {
            if (fVerbose == false || m_safeCertContext.IsInvalid)
                return GetType().FullName;

            StringBuilder sb = new StringBuilder();

            // Subject
            sb.Append("[Subject]" + Environment.NewLine + "  ");
            sb.Append(this.Subject);

            // Issuer
            sb.Append(Environment.NewLine + Environment.NewLine + "[Issuer]" + Environment.NewLine + "  ");
            sb.Append(this.Issuer);

            // Serial Number
            sb.Append(Environment.NewLine + Environment.NewLine + "[Serial Number]" + Environment.NewLine + "  ");
            sb.Append(this.SerialNumber);

            // NotBefore
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not Before]" + Environment.NewLine + "  ");
            sb.Append(FormatDate(this.NotBefore));

            // NotAfter
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not After]" + Environment.NewLine + "  ");
            sb.Append(FormatDate(this.NotAfter));

            // Thumbprint
            sb.Append(Environment.NewLine + Environment.NewLine + "[Thumbprint]" + Environment.NewLine + "  ");
            sb.Append(this.GetCertHashString());

            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        /// <summary>
        ///     Convert a date to a string.
        /// 
        ///     Some cultures, specifically using the Um-AlQura calendar cannot convert dates far into
        ///     the future into strings.  If the expiration date of an X.509 certificate is beyond the range
        ///     of one of these these cases, we need to fall back to a calendar which can express the dates
        /// </summary>
        protected static string FormatDate(DateTime date) {
            CultureInfo culture = CultureInfo.CurrentCulture;

            if (!culture.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0)) {
                // The most common case of culture failing to work is in the Um-AlQuara calendar. In this case,
                // we can fall back to the Hijri calendar, otherwise fall back to the invariant culture.
                if (culture.DateTimeFormat.Calendar is UmAlQuraCalendar) {
                    culture = culture.Clone() as CultureInfo;
                    culture.DateTimeFormat.Calendar = new HijriCalendar();
                }
                else
                {
                    culture = CultureInfo.InvariantCulture;
                }
            }

            return date.ToString(culture);
        }

        public virtual string GetFormat() {
            return m_format;
        }

        public string Issuer {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_issuerName == null)
                    m_issuerName = X509Utils._GetIssuerName(m_safeCertContext, false);
                return m_issuerName;
            }
        }

        public string Subject {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_subjectName == null)
                    m_subjectName = X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, false);
                return m_subjectName;
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #else
        [System.Security.SecurityCritical]
        #endif
          // auto-generated_required
        [System.Runtime.InteropServices.ComVisible(false)]
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(byte[] rawData) {
            Reset();
            LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        #if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] // auto-generated
        #else
        [System.Security.SecurityCritical]
        #endif
          // auto-generated_required
        [System.Runtime.InteropServices.ComVisible(false)]
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags) {
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }

#if FEATURE_X509_SECURESTRINGS
        [System.Security.SecurityCritical]  // auto-generated_required
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }
#endif // FEATURE_X509_SECURESTRINGS

        [System.Security.SecurityCritical]  // auto-generated_required
        [System.Runtime.InteropServices.ComVisible(false)]
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(string fileName) {
            Reset();
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [System.Runtime.InteropServices.ComVisible(false)]
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags) {
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

#if FEATURE_X509_SECURESTRINGS
        [System.Security.SecurityCritical]  // auto-generated_required
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Import(string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags) {
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }
#endif // FEATURE_X509_SECURESTRINGS

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType) {
            return ExportHelper(contentType, null);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(false)]
        public virtual byte[] Export(X509ContentType contentType, string password) {
            return ExportHelper(contentType, password);
        }

#if FEATURE_X509_SECURESTRINGS
        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual byte[] Export(X509ContentType contentType, SecureString password) {
            return ExportHelper(contentType, password);
        }
#endif // FEATURE_X509_SECURESTRINGS

        [System.Security.SecurityCritical]  // auto-generated_required
        [System.Runtime.InteropServices.ComVisible(false)]
#pragma warning disable 618
        [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted=true)]
#pragma warning restore 618
        public virtual void Reset () {
            m_subjectName = null;
            m_issuerName = null;
            m_serialNumber = null;
            m_publicKeyParameters = null;
            m_publicKeyValue = null;
            m_publicKeyOid = null;
            m_rawData = null;
            m_thumbprint = null;
            m_notBefore = DateTime.MinValue;
            m_notAfter = DateTime.MinValue;
            if (!m_safeCertContext.IsInvalid) {
                // Free the current certificate handle
                if (!m_certContextCloned) {
                    m_safeCertContext.Dispose();
                }
                m_safeCertContext = SafeCertContextHandle.InvalidHandle;
            }
            m_certContextCloned = false;
        }
   
        public void Dispose() {
            Dispose(true);
        }

        [System.Security.SecuritySafeCritical] 
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                Reset();
            }            
        }

#if FEATURE_SERIALIZATION
        /// <internalonly/>
        [System.Security.SecurityCritical]  // auto-generated_required
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) {
            if (m_safeCertContext.IsInvalid)
                info.AddValue("RawData", null);
            else
                info.AddValue("RawData", this.RawData);
        }

        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization(Object sender) {}
#endif

        //
        // internal.
        //

        internal SafeCertContextHandle CertContext {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                return m_safeCertContext;
            }
        }

        /// <summary>
        /// Returns the SafeCertContextHandle. Use this instead of the CertContext property when
        /// creating another X509Certificate object based on this one to ensure the underlying
        /// cert context is not released at the wrong time.
        /// </summary>
        [System.Security.SecurityCritical]
        internal SafeCertContextHandle GetCertContextForCloning() {
            m_certContextCloned = true;
            return m_safeCertContext;
        }

        //
        // private methods.
        //

        [System.Security.SecurityCritical]  // auto-generated
        private void ThrowIfContextInvalid() {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
        }

        private DateTime NotAfter {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_notAfter == DateTime.MinValue) {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotAfter(m_safeCertContext, ref fileTime);
                    m_notAfter = DateTime.FromFileTime(fileTime.ToTicks());
                }
                return m_notAfter;
            }
        }

        private DateTime NotBefore {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_notBefore == DateTime.MinValue) {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotBefore(m_safeCertContext, ref fileTime);
                    m_notBefore = DateTime.FromFileTime(fileTime.ToTicks());
                }
                return m_notBefore;
            }
        }

        private byte[] RawData {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_rawData == null)
                    m_rawData = X509Utils._GetCertRawData(m_safeCertContext);
                return (byte[]) m_rawData.Clone(); 
            }
        }

        private string SerialNumber {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                ThrowIfContextInvalid();

                if (m_serialNumber == null)
                    m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
                return Hex.EncodeHexStringFromInt(m_serialNumber);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void SetThumbprint () {
            ThrowIfContextInvalid();

            if (m_thumbprint == null)
                m_thumbprint = X509Utils._GetThumbprint(m_safeCertContext);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private byte[] ExportHelper (X509ContentType contentType, object password) {
            switch(contentType) {
            case X509ContentType.Cert:
                break;
#if FEATURE_CORECLR
            case (X509ContentType)0x02 /* X509ContentType.SerializedCert */:
            case (X509ContentType)0x03 /* X509ContentType.Pkcs12 */:
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType"),
                    new NotSupportedException());
#else // FEATURE_CORECLR
            case X509ContentType.SerializedCert:
                break;
            case X509ContentType.Pkcs12:
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Open | KeyContainerPermissionFlags.Export);
                kp.Demand();
                break;
#endif // FEATURE_CORECLR else
            default:
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType"));
            }

#if !FEATURE_CORECLR
            IntPtr szPassword = IntPtr.Zero;
            byte[] encodedRawData = null;
            SafeCertStoreHandle safeCertStoreHandle = X509Utils.ExportCertToMemoryStore(this);

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                szPassword = X509Utils.PasswordToHGlobalUni(password);
                encodedRawData = X509Utils._ExportCertificatesToBlob(safeCertStoreHandle, contentType, szPassword);
            }
            finally {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(szPassword);
                safeCertStoreHandle.Dispose();
            }
            if (encodedRawData == null)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_ExportFailed"));
            return encodedRawData;
#else // !FEATURE_CORECLR
            return RawData;
#endif // !FEATURE_CORECLR
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void LoadCertificateFromBlob (byte[] rawData, object password, X509KeyStorageFlags keyStorageFlags) {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullArray"), "rawData");
            Contract.EndContractBlock();

            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertBlobType(rawData));
#if !FEATURE_CORECLR
            if (contentType == X509ContentType.Pkcs12 &&
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create);
                kp.Demand();
            }
#endif // !FEATURE_CORECLR
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                szPassword = X509Utils.PasswordToHGlobalUni(password);
                X509Utils._LoadCertFromBlob(rawData,
                                            szPassword,
                                            dwFlags,
#if FEATURE_CORECLR
                                            false,
#else // FEATURE_CORECLR
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true,
#endif // FEATURE_CORECLR else
                                            ref m_safeCertContext);
            }
            finally {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(szPassword);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void LoadCertificateFromFile (string fileName, object password, X509KeyStorageFlags keyStorageFlags) {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            Contract.EndContractBlock();

            string fullPath = Path.GetFullPathInternal(fileName);
            new FileIOPermission (FileIOPermissionAccess.Read, fullPath).Demand();
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertFileType(fileName));
#if !FEATURE_CORECLR
            if (contentType == X509ContentType.Pkcs12 &&
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.Create);
                kp.Demand();
            }
#endif // !FEATURE_CORECLR
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                szPassword = X509Utils.PasswordToHGlobalUni(password);
                X509Utils._LoadCertFromFile(fileName,
                                            szPassword,
                                            dwFlags,
#if FEATURE_CORECLR
                                            false,
#else // FEATURE_CORECLR
                                            (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == 0 ? false : true,
#endif // FEATURE_CORECLR else
                                            ref m_safeCertContext);
            }
            finally {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(szPassword);
            }
        }

#if FEATURE_LEGACYNETCF
        protected internal String CreateHexString(byte[] sArray) {
            return Hex.EncodeHexString(sArray);
        }
#endif

    }
}
