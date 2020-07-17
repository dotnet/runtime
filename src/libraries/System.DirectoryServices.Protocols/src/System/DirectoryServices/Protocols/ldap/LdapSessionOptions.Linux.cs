// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) { /* No op */ }

        public bool SecureSocketLayer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
    }
}
