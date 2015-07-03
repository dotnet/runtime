// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = ((int)ntStatus) | 0x01000000;
            return new CryptographicException(hr);
        }
    }
}
