// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = unchecked((int)ntStatus) | 0x01000000;
            return new CryptographicException(hr);
        }
    }
}
