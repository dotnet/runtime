// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private LDAP_TLS_CONNECT_CB _serverCertificateRoutine;
        private Interop.OpenSsl.VerifyCallback? _openSslVerifyRoutine;

        private void InitializeServerCertificateDelegate()
        {
            _serverCertificateRoutine = new LDAP_TLS_CONNECT_CB(SetOpenSslCallback);
            _openSslVerifyRoutine ??= new Interop.OpenSsl.VerifyCallback(ProcessServerCertificate);
        }

        static partial void PALCertFreeCRLContext(IntPtr certPtr);

        private int SetOpenSslCallback(IntPtr ld, IntPtr ssl, IntPtr ctx, IntPtr arg)
        {
            Interop.OpenSsl.SSL_set_verify(ssl, Interop.OpenSsl.SSL_VERIFY_PEER, _openSslVerifyRoutine);

            return 1; // continue the handshake
        }

        private int ProcessServerCertificate(int preverify_ok, IntPtr x509StoreCtx)
        {
            if (_serverCertificateDelegate == null)
            {
                return preverify_ok;
            }

            int depth = Interop.OpenSsl.X509_STORE_CTX_get_error_depth(x509StoreCtx);
            if (depth != 0)
            {
                return 1;
            }

            X509Certificate? cert = TryGetX509Certificate2FromStoreCtx(x509StoreCtx);
            if (cert == null)
                return 0;

            try
            {
                return _serverCertificateDelegate(_connection, cert) ? 1 : 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
                cert.Dispose();
            }
        }

        private static X509Certificate2? TryGetX509Certificate2FromStoreCtx(IntPtr x509StoreCtx)
        {
            IntPtr x509 = Interop.OpenSsl.X509_STORE_CTX_get_current_cert(x509StoreCtx);
            if (x509 == IntPtr.Zero)
                return null;

            // OpenSSL will allocate a buffer and write its address into pp if pp starts as NULL.
            IntPtr pDer = IntPtr.Zero;
            int len = Interop.OpenSsl.i2d_X509(x509, ref pDer);
            if (len <= 0 || pDer == IntPtr.Zero)
                return null;

            try
            {
                byte[] der = new byte[len];
                Marshal.Copy(pDer, der, 0, len);
                return X509CertificateLoader.LoadCertificate(der);
            }
            finally
            {
                Interop.OpenSsl.CRYPTO_free(pDer, IntPtr.Zero, 0);
            }
        }

        private bool _secureSocketLayer;

        /// <summary>
        /// Specifies the path of the directory containing CA certificates in the PEM format.
        /// Multiple directories may be specified by separating with a semi-colon.
        /// </summary>
        /// <remarks>
        /// The certificate files are looked up by the CA subject name hash value where that hash can be
        /// obtained by using, for example, <code>openssl x509 -hash -noout -in CA.crt</code>.
        /// It is a common practice to have the certificate file be a symbolic link to the actual certificate file
        /// which can be done by using <code>openssl rehash .</code> or <code>c_rehash .</code> in the directory
        /// containing the certificate files.
        /// </remarks>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
        [UnsupportedOSPlatform("windows")]
        public string TrustedCertificatesDirectory
        {
            get => GetStringValueHelper(LdapOption.LDAP_OPT_X_TLS_CACERTDIR, releasePtr: true);

            set
            {
                if (!Directory.Exists(value))
                {
                    throw new DirectoryNotFoundException(SR.Format(SR.DirectoryNotFound, value));
                }

                SetStringOptionHelper(LdapOption.LDAP_OPT_X_TLS_CACERTDIR, value);
            }
        }

        public bool SecureSocketLayer
        {
            get
            {
                if (_connection._disposed) throw new ObjectDisposedException(GetType().Name);
                return _secureSocketLayer;
            }
            set
            {
                if (_connection._disposed) throw new ObjectDisposedException(GetType().Name);
                _secureSocketLayer = value;
            }
        }

        public ReferralChasingOptions ReferralChasing
        {
            get
            {
                return GetBoolValueHelper(LdapOption.LDAP_OPT_REFERRALS) ? ReferralChasingOptions.All : ReferralChasingOptions.None;
            }
            set
            {
                if (((value) & (~ReferralChasingOptions.All)) != 0)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ReferralChasingOptions));
                }
                if (value != ReferralChasingOptions.None && value != ReferralChasingOptions.All)
                {
                    throw new PlatformNotSupportedException(SR.ReferralChasingOptionsNotSupported);
                }

                SetBoolValueHelper(LdapOption.LDAP_OPT_REFERRALS, value == ReferralChasingOptions.All);
            }
        }

        public VerifyServerCertificateCallback? VerifyServerCertificate
        {
            get
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                return _serverCertificateDelegate;
            }
            set
            {
                if (_connection._disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }

                if (value != null)
                {
                    IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(_serverCertificateRoutine);
                    int error = Interop.Ldap.ldap_set_option_ptr_value(_connection._ldapHandle, LdapOption.LDAP_OPT_X_TLS_CONNECT_CB, functionPointer);

                    ErrorChecking.CheckAndSetLdapError(error);
                }

                _serverCertificateDelegate = value;
            }
        }

        /// <summary>
        /// Create a new TLS library context.
        /// Calling this is necessary after setting TLS-based options, such as <c>TrustedCertificatesDirectory</c>.
        /// </summary>
        [UnsupportedOSPlatform("windows")]
        public void StartNewTlsSessionContext()
        {
            SetIntValueHelper(LdapOption.LDAP_OPT_X_TLS_NEWCTX, 0);
        }

        // In practice, this apparently rarely if ever contains useful text
        internal string ServerErrorMessage => GetStringValueHelper(LdapOption.LDAP_OPT_ERROR_STRING, true);

        private bool GetBoolValueHelper(LdapOption option)
        {
            if (_connection._disposed) throw new ObjectDisposedException(GetType().Name);

            bool outValue = false;
            int error = LdapPal.GetBoolOption(_connection._ldapHandle, option, ref outValue);
            ErrorChecking.CheckAndSetLdapError(error);

            return outValue;
        }

        private void SetBoolValueHelper(LdapOption option, bool value)
        {
            if (_connection._disposed) throw new ObjectDisposedException(GetType().Name);

            int error = LdapPal.SetBoolOption(_connection._ldapHandle, option, value);

            ErrorChecking.CheckAndSetLdapError(error);
        }

        private void SetStringOptionHelper(LdapOption option, string value)
        {
            if (_connection._disposed) throw new ObjectDisposedException(GetType().Name);

            int error = LdapPal.SetStringOption(_connection._ldapHandle, option, value);

            ErrorChecking.CheckAndSetLdapError(error);
        }
    }
}
