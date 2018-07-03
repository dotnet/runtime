// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // The private key field is just an integer, but we need the raw bytes.
    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct DsaPrivateKeyAsn
    {
        [Integer]
        public ReadOnlyMemory<byte>? X;
    }
}
