// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int HMAC_Init(out HMAC_CTX ctx, byte* key, int key_len, IntPtr md);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int HMAC_Update(ref HMAC_CTX ctx, byte* data, int len);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe int HMAC_Final(ref HMAC_CTX ctx, byte* md, ref uint len);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe void HMAC_CTX_cleanup(ref HMAC_CTX ctx);

        [StructLayout(LayoutKind.Explicit, Size = 512)]
        internal struct HMAC_CTX { }
    }
}
