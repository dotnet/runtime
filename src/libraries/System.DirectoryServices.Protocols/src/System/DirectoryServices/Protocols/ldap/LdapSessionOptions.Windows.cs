// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private VERIFYSERVERCERT _serverCertificateRoutine;

        private void InitializeServerCertificateDelegate()
        {
            _serverCertificateRoutine = new VERIFYSERVERCERT(ProcessServerCertificate);
        }

        private static void PALCertFreeCRLContext(IntPtr certPtr) => Interop.Ldap.CertFreeCRLContext(certPtr);

        private Interop.BOOL ProcessServerCertificate(IntPtr connection, IntPtr serverCert)
        {
            // If callback is not specified by user, it means the server certificate is accepted.
            bool value = true;
            if (_serverCertificateDelegate != null)
            {
                IntPtr certPtr = IntPtr.Zero;
                X509Certificate certificate = null;
                try
                {
                    Debug.Assert(serverCert != IntPtr.Zero);
                    certPtr = Marshal.ReadIntPtr(serverCert);
                    certificate = new X509Certificate(certPtr);
                }
                finally
                {
                    PALCertFreeCRLContext(certPtr);
                }

                value = _serverCertificateDelegate(_connection, certificate);
            }

            return value ? Interop.BOOL.TRUE : Interop.BOOL.FALSE;
        }

        [UnsupportedOSPlatform("windows")]
        public string TrustedCertificatesDirectory
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool SecureSocketLayer
        {
            get
            {
                int outValue = GetIntValueHelper(LdapOption.LDAP_OPT_SSL);
                return outValue == 1;
            }
            set
            {
                int temp = value ? 1 : 0;
                SetIntValueHelper(LdapOption.LDAP_OPT_SSL, temp);
            }
        }

        [UnsupportedOSPlatform("windows")]
        public void StartNewTlsSessionContext() => throw new PlatformNotSupportedException();

        public ReferralChasingOptions ReferralChasing
        {
            get
            {
                int result = GetIntValueHelper(LdapOption.LDAP_OPT_REFERRALS);
                return result == 1 ? ReferralChasingOptions.All : (ReferralChasingOptions)result;
            }
            set
            {
                if (((value) & (~ReferralChasingOptions.All)) != 0)
                {
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ReferralChasingOptions));
                }

                SetIntValueHelper(LdapOption.LDAP_OPT_REFERRALS, (int)value);
            }
        }

        public VerifyServerCertificateCallback VerifyServerCertificate
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
                    int error = LdapPal.SetServerCertOption(_connection._ldapHandle, LdapOption.LDAP_OPT_SERVER_CERTIFICATE, _serverCertificateRoutine);
                    ErrorChecking.CheckAndSetLdapError(error);
                }

                _serverCertificateDelegate = value;
            }
        }

        // In practice, this apparently rarely if ever contains useful text
        internal string ServerErrorMessage => GetStringValueHelper(LdapOption.LDAP_OPT_SERVER_ERROR, true);
    }
}
