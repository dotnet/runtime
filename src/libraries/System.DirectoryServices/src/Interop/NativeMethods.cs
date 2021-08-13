// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.DirectoryServices.Interop
{
    internal static class NativeMethods
    {
        public enum AuthenticationModes
        {
            SecureAuthentication = 0x1,
            UseEncryption = 0x2,
            UseSSL = 0x2,
            ReadonlyServer = 0x4,
            NoAuthentication = 0x10,
            FastBind = 0x20,
            UseSigning = 0x40,
            UseSealing = 0x80,
            UseDelegation = 0x100,
            UseServerBinding = 0x200
        }
    }
}
