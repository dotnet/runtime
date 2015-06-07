// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;

internal partial class Interop
{
    internal partial class BCrypt
    {
        private enum NTSTATUS : uint
        {
            STATUS_SUCCESS = 0x0,
            STATUS_NOT_FOUND = 0xc0000225,
            STATUS_INVALID_PARAMETER = 0xc000000d,
            STATUS_NO_MEMORY = 0xc0000017,
        }

        private static Exception CreateCryptographicException(NTSTATUS ntStatus)
        {
            int hr = ((int)ntStatus) | 0x01000000;
            return new CryptographicException(hr);
        }
    }
}
