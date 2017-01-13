// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Interop
{
#pragma warning disable BCL0015 // CoreFXPort
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;
    using System.Collections;
    using System.IO;
    using System.Text;

    internal class NativeMethods
    {
        public enum AuthenticationModes
        {
            SecureAuthentication = 0x1,
            UseEncryption = 0x2,
            UseSSL = 0x2,
            ReadonlyServer = 0x4,
            // PromptCredentials     = 0x8,   // Deprecated by ADSI
            NoAuthentication = 0x10,
            FastBind = 0x20,
            UseSigning = 0x40,
            UseSealing = 0x80,
            UseDelegation = 0x100,
            UseServerBinding = 0x200
        }
    }
}

