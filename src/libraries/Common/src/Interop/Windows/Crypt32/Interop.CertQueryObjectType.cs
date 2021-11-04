// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertQueryObjectType : int
        {
            CERT_QUERY_OBJECT_FILE = 0x00000001,
            CERT_QUERY_OBJECT_BLOB = 0x00000002,
        }
    }
}
