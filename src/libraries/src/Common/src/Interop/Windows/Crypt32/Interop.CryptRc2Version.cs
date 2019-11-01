// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CryptRc2Version : int
        {
            CRYPT_RC2_40BIT_VERSION = 160,
            CRYPT_RC2_56BIT_VERSION = 52,
            CRYPT_RC2_64BIT_VERSION = 120,
            CRYPT_RC2_128BIT_VERSION = 58,
        }
    }
}
