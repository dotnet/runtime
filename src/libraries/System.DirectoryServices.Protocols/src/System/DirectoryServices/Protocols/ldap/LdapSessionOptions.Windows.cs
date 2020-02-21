// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Security.Authentication;
using System.Runtime.CompilerServices;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) => Wldap32.CertFreeCRLContext(certPtr);

        internal bool FQDN
        {
            set
            {
                // set the value to true
                SetIntValueHelper(LdapOption.LDAP_OPT_AREC_EXCLUSIVE, 1);
            }
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

        private static string PtrToString(IntPtr pointer) => Marshal.PtrToStringUni(pointer);

        private static IntPtr StringToPtr(string value) => Marshal.StringToHGlobalUni(value);
    }
}
