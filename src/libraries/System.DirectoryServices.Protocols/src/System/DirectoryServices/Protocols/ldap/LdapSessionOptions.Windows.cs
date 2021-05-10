// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) => Interop.Ldap.CertFreeCRLContext(certPtr);

        [SupportedOSPlatform("windows")]
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
    }
}
