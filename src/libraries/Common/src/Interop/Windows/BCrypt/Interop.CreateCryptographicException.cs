// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Cryptography;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = unchecked((int)ntStatus) | 0x01000000;
            return hr.ToCryptographicException();
        }
    }
}
