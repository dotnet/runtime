// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFree")]
        internal static extern void DigestFree(IntPtr handle);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestCreate")]
        internal static extern SafeDigestCtxHandle DigestCreate(PAL_HashAlgorithm algorithm, out int cbDigest);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestUpdate")]
        internal static extern unsafe int DigestUpdate(SafeDigestCtxHandle ctx, byte* pbData, int cbData);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFinal")]
        internal static extern unsafe int DigestFinal(SafeDigestCtxHandle ctx, byte* pbOutput, int cbOutput);
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeDigestCtxHandle : SafeHandle
    {
        internal SafeDigestCtxHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AppleCrypto.DigestFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
