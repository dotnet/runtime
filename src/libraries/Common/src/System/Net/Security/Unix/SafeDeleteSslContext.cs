// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

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
