// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.DirectoryServices.Protocols
{
    public partial class LdapSessionOptions
    {
        private static void PALCertFreeCRLContext(IntPtr certPtr) { /* No op */ }


        // Options that are not supported in Linux

        internal bool FQDN
        {
            set { /* no op */ }
        }

        public bool SecureSocketLayer
        {
            get; // no op
            set; // no op
        }

        private static string PtrToString(IntPtr pointer) => Marshal.PtrToStringAnsi(pointer);

        private static IntPtr StringToPtr(string value) => Marshal.StringToHGlobalAnsi(value);
    }
}
