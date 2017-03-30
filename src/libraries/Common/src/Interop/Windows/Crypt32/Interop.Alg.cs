// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        // All constants below are from wincrypt.h.

        // Algorithm classes
        public const int ALG_CLASS_ANY = 0;
        public const int ALG_CLASS_SIGNATURE = (1 << 13);
        public const int ALG_CLASS_ENCRYPT = (3 << 13);
        public const int ALG_CLASS_HASH = (4 << 13);
        public const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);

        // Algorithm types
        public const int ALG_TYPE_RSA = (2 << 9);
        public const int ALG_TYPE_BLOCK = (3 << 9);
        public const int ALG_TYPE_STREAM = (4 << 9);
        public const int ALG_TYPE_DH = (5 << 9);

        // Block cipher sub-IDs
        public const int ALG_SID_DES = 1;
        public const int ALG_SID_3DES = 3;
        public const int ALG_SID_AES_128 = 14;
        public const int ALG_SID_AES_192 = 15;
        public const int ALG_SID_AES_256 = 16;
        public const int ALG_SID_AES = 17;

        // RC2 sub-IDs
        public const int ALG_SID_RC2 = 2;

        // Stream cipher sub-IDs
        public const int ALG_SID_RC4 = 1;

        // Diffie-Hellman sub-IDs
        public const int ALG_SID_DH_EPHEM = 2;

        // Hash sub-IDs
        public const int ALG_SID_MD5 = 3;
        public const int ALG_SID_SHA = 4;
        public const int ALG_SID_SHA_256 = 12;
        public const int ALG_SID_SHA_384 = 13;
        public const int ALG_SID_SHA_512 = 14;
    }
}
