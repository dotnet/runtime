// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCreate")]
        internal static partial SafeEvpCipherCtxHandle EvpCipherCreate(
            IntPtr cipher,
            ref byte key,
            int keyLength,
            ref byte iv,
            int enc);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCreatePartial")]
        internal static partial SafeEvpCipherCtxHandle EvpCipherCreatePartial(
            IntPtr cipher);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetKeyAndIV")]
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
                throw new CryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool AndroidCryptoNative_CipherSetNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void CipherSetNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!AndroidCryptoNative_CipherSetNonceLength(ctx, nonceLength))
            {
                throw new CryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherDestroy")]
        internal static partial void EvpCipherDestroy(IntPtr ctx);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherReset")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvpCipherReset(SafeEvpCipherCtxHandle ctx);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCtxSetPadding")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvpCipherCtxSetPadding(SafeEvpCipherCtxHandle x, int padding);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherUpdate")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EvpCipherUpdate(
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

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherUpdateAAD")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CipherUpdateAAD(
            SafeEvpCipherCtxHandle ctx,
            ref byte @in,
            int inl);

        internal static void CipherUpdateAAD(
            SafeEvpCipherCtxHandle ctx,
            ReadOnlySpan<byte> input)
        {
            if (!CipherUpdateAAD(
                ctx,
                ref MemoryMarshal.GetReference(input),
                input.Length))
            {
                throw new CryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherFinalEx")]
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

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetTagLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CipherSetTagLength(
            SafeEvpCipherCtxHandle ctx,
            int tagLength);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherIsSupported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CipherIsSupported(IntPtr cipher);

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Ecb")]
        internal static partial IntPtr EvpAes128Ecb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cbc")]
        internal static partial IntPtr EvpAes128Cbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Gcm")]
        internal static partial IntPtr EvpAes128Gcm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cfb8")]
        internal static partial IntPtr EvpAes128Cfb8();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cfb128")]
        internal static partial IntPtr EvpAes128Cfb128();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Ccm")]
        internal static partial IntPtr EvpAes128Ccm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Ecb")]
        internal static partial IntPtr EvpAes192Ecb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cbc")]
        internal static partial IntPtr EvpAes192Cbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Gcm")]
        internal static partial IntPtr EvpAes192Gcm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cfb8")]
        internal static partial IntPtr EvpAes192Cfb8();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cfb128")]
        internal static partial IntPtr EvpAes192Cfb128();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Ccm")]
        internal static partial IntPtr EvpAes192Ccm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Ecb")]
        internal static partial IntPtr EvpAes256Ecb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cbc")]
        internal static partial IntPtr EvpAes256Cbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Gcm")]
        internal static partial IntPtr EvpAes256Gcm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cfb128")]
        internal static partial IntPtr EvpAes256Cfb128();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cfb8")]
        internal static partial IntPtr EvpAes256Cfb8();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Ccm")]
        internal static partial IntPtr EvpAes256Ccm();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesCbc")]
        internal static partial IntPtr EvpDesCbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesEcb")]
        internal static partial IntPtr EvpDesEcb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesCfb8")]
        internal static partial IntPtr EvpDesCfb8();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cbc")]
        internal static partial IntPtr EvpDes3Cbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Ecb")]
        internal static partial IntPtr EvpDes3Ecb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cfb8")]
        internal static partial IntPtr EvpDes3Cfb8();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cfb64")]
        internal static partial IntPtr EvpDes3Cfb64();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RC2Cbc")]
        internal static partial IntPtr EvpRC2Cbc();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RC2Ecb")]
        internal static partial IntPtr EvpRC2Ecb();

        [GeneratedDllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_ChaCha20Poly1305")]
        internal static partial IntPtr EvpChaCha20Poly1305();

        internal enum EvpCipherDirection : int
        {
            NoChange = -1,
            Decrypt = 0,
            Encrypt = 1,
        }
    }
}
