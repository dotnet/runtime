// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        static partial void PALCertFreeCRLContext(IntPtr certPtr);

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

        /// <summary>
        /// Create a new TLS library context.
        /// Calling this is necessary after setting TLS-based options, such as <c>TrustedCertificatesDirectory</c>.
        /// </summary>
        [UnsupportedOSPlatform("windows")]
        public void StartNewTlsSessionContext()
        {
            SetIntValueHelper(LdapOption.LDAP_OPT_X_TLS_NEWCTX, 0);
        }

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
