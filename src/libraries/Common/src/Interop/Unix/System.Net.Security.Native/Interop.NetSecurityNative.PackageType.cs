// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        internal enum PackageType : uint
        {
            Negotiate = 0,
            NTLM = 1,
            Kerberos = 2,
        }
    }
}
