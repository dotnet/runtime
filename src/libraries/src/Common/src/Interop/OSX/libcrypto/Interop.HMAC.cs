// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        private const int Success = 1;

        // On OSX, these functions are void returning (and the man pages give no indication of what would happen if they
        // fail), but upstack code is written against surface area where they return 1 on success and 0 on error.
        // These routines call the underlying APIs and then just return success

        internal static unsafe int HMAC_Init(out HMAC_CTX ctx, byte* key, int key_len, IntPtr md)
        {
            HMAC_InitNative(out ctx, key, key_len, md);
            return Success;
        }

        internal static unsafe int HMAC_Update(ref HMAC_CTX ctx, byte* data, int len)
        {
            HMAC_UpdateNative(ref ctx, data, len);
            return Success;
        }

        internal static unsafe int HMAC_Final(ref HMAC_CTX ctx, byte* md, ref uint len)
        {
            HMAC_FinalNative(ref ctx, md, ref len);
            return Success;
        }

        [DllImport(Libraries.LibCrypto, EntryPoint = "HMAC_Init", ExactSpelling = true)]
        private extern static unsafe int HMAC_InitNative(out HMAC_CTX ctx, byte* key, int key_len, IntPtr md);

        [DllImport(Libraries.LibCrypto, EntryPoint = "HMAC_Update", ExactSpelling = true)]
        private extern static unsafe int HMAC_UpdateNative(ref HMAC_CTX ctx, byte* data, int len);

        [DllImport(Libraries.LibCrypto, EntryPoint = "HMAC_Final", ExactSpelling = true)]
        private extern static unsafe int HMAC_FinalNative(ref HMAC_CTX ctx, byte* md, ref uint len);

        [DllImport(Libraries.LibCrypto)]
        internal extern static unsafe void HMAC_CTX_cleanup(ref HMAC_CTX ctx);

        [StructLayout(LayoutKind.Explicit, Size = 512)]
        internal struct HMAC_CTX { }
    }
}
