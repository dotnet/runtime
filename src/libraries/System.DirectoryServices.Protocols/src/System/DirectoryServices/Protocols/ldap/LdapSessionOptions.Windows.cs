// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) => Interop.Ldap.CertFreeCRLContext(certPtr);

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
    }
}
