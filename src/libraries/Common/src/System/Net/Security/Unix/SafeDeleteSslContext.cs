// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal class SafeDeleteSslContext : SafeDeleteContext
    {
        public SafeDeleteSslContext(IntPtr handle) : base(handle, true)
        {
        }

        public SafeDeleteSslContext(IntPtr handle, bool ownsHandle) : base(handle, ownsHandle)
        {
        }
    }
}
