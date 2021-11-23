// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCreate2")]
        internal static partial SafeEvpCipherCtxHandle EvpCipherCreate(
            IntPtr cipher,
            ref byte key,
            int keyLength,
            ref byte iv,
            int enc);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCreatePartial")]
        internal static partial SafeEvpCipherCtxHandle EvpCipherCreatePartial(
            IntPtr cipher);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetKeyAndIV")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherSetKeyAndIV(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetGcmNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptoNative_EvpCipherSetGcmNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void EvpCipherSetGcmNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!CryptoNative_EvpCipherSetGcmNonceLength(ctx, nonceLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetCcmNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CryptoNative_EvpCipherSetCcmNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void EvpCipherSetCcmNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!CryptoNative_EvpCipherSetCcmNonceLength(ctx, nonceLength))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherDestroy")]
        internal static partial void EvpCipherDestroy(IntPtr ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherReset")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvpCipherReset(SafeEvpCipherCtxHandle ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherCtxSetPadding")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvpCipherCtxSetPadding(SafeEvpCipherCtxHandle x, int padding);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherUpdate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherUpdate(
            SafeEvpCipherCtxHandle ctx,
            ref byte output,
            out int outl,
            ref byte input,
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherFinalEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherFinalEx(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherGetGcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherGetGcmTag(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherGetAeadTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherGetAeadTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherGetAeadTag(SafeEvpCipherCtxHandle ctx, Span<byte> tag)
        {
            if (!EvpCipherGetAeadTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetGcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherSetGcmTag(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetAeadTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherSetAeadTag(
            SafeEvpCipherCtxHandle ctx,
            ref byte tag,
            int tagLength);

        internal static void EvpCipherSetAeadTag(SafeEvpCipherCtxHandle ctx, ReadOnlySpan<byte> tag)
        {
            if (!EvpCipherSetAeadTag(ctx, ref MemoryMarshal.GetReference(tag), tag.Length))
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherGetCcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherGetCcmTag(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpCipherSetCcmTag")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherSetCcmTag(
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

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Ecb")]
        internal static partial IntPtr EvpAes128Ecb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Cbc")]
        internal static partial IntPtr EvpAes128Cbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Gcm")]
        internal static partial IntPtr EvpAes128Gcm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Cfb8")]
        internal static partial IntPtr EvpAes128Cfb8();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Cfb128")]
        internal static partial IntPtr EvpAes128Cfb128();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes128Ccm")]
        internal static partial IntPtr EvpAes128Ccm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Ecb")]
        internal static partial IntPtr EvpAes192Ecb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Cbc")]
        internal static partial IntPtr EvpAes192Cbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Gcm")]
        internal static partial IntPtr EvpAes192Gcm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Cfb8")]
        internal static partial IntPtr EvpAes192Cfb8();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Cfb128")]
        internal static partial IntPtr EvpAes192Cfb128();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes192Ccm")]
        internal static partial IntPtr EvpAes192Ccm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Ecb")]
        internal static partial IntPtr EvpAes256Ecb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Cbc")]
        internal static partial IntPtr EvpAes256Cbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Gcm")]
        internal static partial IntPtr EvpAes256Gcm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Cfb128")]
        internal static partial IntPtr EvpAes256Cfb128();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Cfb8")]
        internal static partial IntPtr EvpAes256Cfb8();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpAes256Ccm")]
        internal static partial IntPtr EvpAes256Ccm();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDesCbc")]
        internal static partial IntPtr EvpDesCbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDesEcb")]
        internal static partial IntPtr EvpDesEcb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDesCfb8")]
        internal static partial IntPtr EvpDesCfb8();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Cbc")]
        internal static partial IntPtr EvpDes3Cbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Ecb")]
        internal static partial IntPtr EvpDes3Ecb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Cfb8")]
        internal static partial IntPtr EvpDes3Cfb8();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDes3Cfb64")]
        internal static partial IntPtr EvpDes3Cfb64();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpRC2Cbc")]
        internal static partial IntPtr EvpRC2Cbc();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpRC2Ecb")]
        internal static partial IntPtr EvpRC2Ecb();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpChaCha20Poly1305")]
        internal static partial IntPtr EvpChaCha20Poly1305();

        internal enum EvpCipherDirection : int
        {
            NoChange = -1,
            Decrypt = 0,
            Encrypt = 1,
        }
    }
}
