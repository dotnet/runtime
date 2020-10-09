// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    internal enum OpenSslInitFlags : long
    {
        LoadSslStrings = 0x00200000L,
        LoadCryptoStrings = 0x00000002L
    }
}
