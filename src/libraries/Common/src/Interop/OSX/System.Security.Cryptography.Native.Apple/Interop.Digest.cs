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

        internal static unsafe int DigestUpdate(SafeDigestCtxHandle ctx, ReadOnlySpan<byte> pbData, int cbData)
        {
            fixed (byte* pbDataPtr = &pbData.DangerousGetPinnableReference())
            {
                return DigestUpdate(ctx, pbDataPtr, cbData);
            }
        }

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestUpdate")]
        private static extern unsafe int DigestUpdate(SafeDigestCtxHandle ctx, byte* pbData, int cbData);

        internal static unsafe int DigestFinal(SafeDigestCtxHandle ctx, Span<byte> pbOutput, int cbOutput)
        {
            fixed (byte* pbOutputPtr = &pbOutput.DangerousGetPinnableReference())
            {
                return DigestFinal(ctx, pbOutputPtr, cbOutput);
            }
        }

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_DigestFinal")]
        private static extern unsafe int DigestFinal(SafeDigestCtxHandle ctx, byte* pbOutput, int cbOutput);
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
