// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        private static partial IntPtr GetExtraHandle(SafeEvpPKeyHandle handle)
        {
            _ = handle;
            return IntPtr.Zero;
        }
    }
}
