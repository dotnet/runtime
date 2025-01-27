// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        static partial void PALCertFreeCRLContext(IntPtr certPtr);

        private bool _secureSocketLayer;

        /// <summary>
        /// Specifies the path of the directory containing CA certificates.
        /// </summary>
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
        public string CertificateDirectory
        {
            get => GetStringValueHelper(LdapOption.LDAP_OPT_X_TLS_CACERTDIR, releasePtr: true);
            set => SetStringOptionHelper(LdapOption.LDAP_OPT_X_TLS_CACERTDIR, value);
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

        public int ProtocolVersion
        {
            get => GetPtrValueHelper(LdapOption.LDAP_OPT_VERSION).ToInt32();
            set => SetPtrValueHelper(LdapOption.LDAP_OPT_VERSION, new IntPtr(value));
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
        /// </summary>
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("windows")]
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
