// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) { /* No op */ }

        public bool SecureSocketLayer { get; set; }

        public int ProtocolVersion
        {
            get => GetPtrValueHelper(LdapOption.LDAP_OPT_VERSION).ToInt32();
            set => SetPtrValueHelper(LdapOption.LDAP_OPT_VERSION, new IntPtr(value));
        }
    }
}
