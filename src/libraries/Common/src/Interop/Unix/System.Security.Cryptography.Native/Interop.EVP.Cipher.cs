// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCreate2")]
        internal static extern SafeEvpCipherCtxHandle EvpCipherCreate(
            IntPtr cipher,
            ref byte key,
            int keyLength,
            int effectivekeyLength,
            ref byte iv,
            int enc);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCreatePartial")]
        internal static extern SafeEvpCipherCtxHandle EvpCipherCreatePartial(
            IntPtr cipher);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetKeyAndIV")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherSetKeyAndIV(
            SafeEvpCipherCtxHandle ctx,
            ref byte key,
            ref byte iv,
            EvpCipherDirection direction);

        internal static void EvpCipherSetKeyAndIV(
            SafeEvpCipherCtxHandle ctx,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            EvpCipherDirection direction)
        {
            if (!EvpCipherSetKeyAndIV(
                ctx,
                ref MemoryMarshal.GetReference(key),
                ref MemoryMarshal.GetReference(iv),
                direction))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetGcmNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptoNative_EvpCipherSetGcmNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void EvpCipherSetGcmNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!CryptoNative_EvpCipherSetGcmNonceLength(ctx, nonceLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetCcmNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptoNative_EvpCipherSetCcmNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void EvpCipherSetCcmNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!CryptoNative_EvpCipherSetCcmNonceLength(ctx, nonceLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherDestroy")]
        internal static extern void EvpCipherDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherReset")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherReset(SafeEvpCipherCtxHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCtxSetPadding")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherCtxSetPadding(SafeEvpCipherCtxHandle x, int padding);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherUpdate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherUpdate(
            SafeEvpCipherCtxHandle ctx,
            ref byte @out,
            out int outl,
            ref byte @in,
            int inl);

        internal static bool EvpCipherUpdate(
            SafeEvpCipherCtxHandle ctx,
            Span<byte> output,
            out int bytesWritten,
            ReadOnlySpan<byte> input)
        {
            return EvpCipherUpdate(
                ctx,
                ref MemoryMarshal.GetReference(output),
                out bytesWritten,
                ref MemoryMarshal.GetReference(input),
                input.Length);
        }

        internal static void EvpCipherSetInputLength(SafeEvpCipherCtxHandle ctx, int inputLength)
        {
            ref byte nullRef = ref MemoryMarshal.GetReference(Span<byte>.Empty);
            if (!EvpCipherUpdate(ctx, ref nullRef, out _, ref nullRef, inputLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherFinalEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherFinalEx(
            SafeEvpCipherCtxHandle ctx,
            ref byte outm,
            out int outl);

        internal static bool EvpCipherFinalEx(
            SafeEvpCipherCtxHandle ctx,
            Span<byte> output,
            out int bytesWritten)
        {
            return EvpCipherFinalEx(ctx, ref MemoryMarshal.GetReference(output), out bytesWritten);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherGetGcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherGetGcmTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherGetGcmTag(SafeEvpCipherCtxHandle ctx, Span<byte> tag)
        {
            if (!EvpCipherGetGcmTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetGcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherSetGcmTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherSetGcmTag(SafeEvpCipherCtxHandle ctx, ReadOnlySpan<byte> tag)
        {
            if (!EvpCipherSetGcmTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpCipherSetGcmTagLength(SafeEvpCipherCtxHandle ctx, int tagLength)
        {
            ref byte nullRef = ref MemoryMarshal.GetReference(Span<byte>.Empty);
            if (!EvpCipherSetGcmTag(ctx, ref nullRef, tagLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherGetCcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherGetCcmTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherGetCcmTag(SafeEvpCipherCtxHandle ctx, Span<byte> tag)
        {
            if (!EvpCipherGetCcmTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetCcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvpCipherSetCcmTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherSetCcmTag(SafeEvpCipherCtxHandle ctx, ReadOnlySpan<byte> tag)
        {
            if (!EvpCipherSetCcmTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        internal static void EvpCipherSetCcmTagLength(SafeEvpCipherCtxHandle ctx, int tagLength)
        {
            ref byte nullRef = ref MemoryMarshal.GetReference(Span<byte>.Empty);
            if (!EvpCipherSetCcmTag(ctx, ref nullRef, tagLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Ecb")]
        internal static extern IntPtr EvpAes128Ecb();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Cbc")]
        internal static extern IntPtr EvpAes128Cbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Gcm")]
        internal static extern IntPtr EvpAes128Gcm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Ccm")]
        internal static extern IntPtr EvpAes128Ccm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Ecb")]
        internal static extern IntPtr EvpAes192Ecb();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Cbc")]
        internal static extern IntPtr EvpAes192Cbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Gcm")]
        internal static extern IntPtr EvpAes192Gcm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Ccm")]
        internal static extern IntPtr EvpAes192Ccm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Ecb")]
        internal static extern IntPtr EvpAes256Ecb();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Cbc")]
        internal static extern IntPtr EvpAes256Cbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Gcm")]
        internal static extern IntPtr EvpAes256Gcm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Ccm")]
        internal static extern IntPtr EvpAes256Ccm();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDesCbc")]
        internal static extern IntPtr EvpDesCbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDesEcb")]
        internal static extern IntPtr EvpDesEcb();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Cbc")]
        internal static extern IntPtr EvpDes3Cbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Ecb")]
        internal static extern IntPtr EvpDes3Ecb();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpRC2Cbc")]
        internal static extern IntPtr EvpRC2Cbc();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpRC2Ecb")]
        internal static extern IntPtr EvpRC2Ecb();

        internal enum EvpCipherDirection : int
        {
            NoChange = -1,
            Decrypt = 0,
            Encrypt = 1,
        }
    }
}
