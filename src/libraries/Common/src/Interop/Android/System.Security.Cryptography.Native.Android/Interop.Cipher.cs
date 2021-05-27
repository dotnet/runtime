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
        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCreate")]
        internal static extern SafeEvpCipherCtxHandle EvpCipherCreate(
            IntPtr cipher,
            ref byte key,
            int keyLength,
            int effectivekeyLength,
            ref byte iv,
            int enc);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCreatePartial")]
        internal static extern SafeEvpCipherCtxHandle EvpCipherCreatePartial(
            IntPtr cipher);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetKeyAndIV")]
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
                throw new CryptographicException();
            }
        }

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetNonceLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AndroidCryptoNative_CipherSetNonceLength(
            SafeEvpCipherCtxHandle ctx, int nonceLength);

        internal static void CipherSetNonceLength(SafeEvpCipherCtxHandle ctx, int nonceLength)
        {
            if (!AndroidCryptoNative_CipherSetNonceLength(ctx, nonceLength))
            {
                throw new CryptographicException();
            }
        }

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherDestroy")]
        internal static extern void EvpCipherDestroy(IntPtr ctx);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherReset")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherReset(SafeEvpCipherCtxHandle ctx);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherCtxSetPadding")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherCtxSetPadding(SafeEvpCipherCtxHandle x, int padding);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherUpdate")]
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

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherUpdateAAD")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CipherUpdateAAD(
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

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherFinalEx")]
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

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherSetTagLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CipherSetTagLength(
            SafeEvpCipherCtxHandle ctx,
            int tagLength);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_CipherIsSupported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CipherIsSupported(IntPtr cipher);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Ecb")]
        internal static extern IntPtr EvpAes128Ecb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cbc")]
        internal static extern IntPtr EvpAes128Cbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Gcm")]
        internal static extern IntPtr EvpAes128Gcm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cfb8")]
        internal static extern IntPtr EvpAes128Cfb8();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Cfb128")]
        internal static extern IntPtr EvpAes128Cfb128();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes128Ccm")]
        internal static extern IntPtr EvpAes128Ccm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Ecb")]
        internal static extern IntPtr EvpAes192Ecb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cbc")]
        internal static extern IntPtr EvpAes192Cbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Gcm")]
        internal static extern IntPtr EvpAes192Gcm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cfb8")]
        internal static extern IntPtr EvpAes192Cfb8();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Cfb128")]
        internal static extern IntPtr EvpAes192Cfb128();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes192Ccm")]
        internal static extern IntPtr EvpAes192Ccm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Ecb")]
        internal static extern IntPtr EvpAes256Ecb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cbc")]
        internal static extern IntPtr EvpAes256Cbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Gcm")]
        internal static extern IntPtr EvpAes256Gcm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cfb128")]
        internal static extern IntPtr EvpAes256Cfb128();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Cfb8")]
        internal static extern IntPtr EvpAes256Cfb8();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Aes256Ccm")]
        internal static extern IntPtr EvpAes256Ccm();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesCbc")]
        internal static extern IntPtr EvpDesCbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesEcb")]
        internal static extern IntPtr EvpDesEcb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DesCfb8")]
        internal static extern IntPtr EvpDesCfb8();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cbc")]
        internal static extern IntPtr EvpDes3Cbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Ecb")]
        internal static extern IntPtr EvpDes3Ecb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cfb8")]
        internal static extern IntPtr EvpDes3Cfb8();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Des3Cfb64")]
        internal static extern IntPtr EvpDes3Cfb64();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RC2Cbc")]
        internal static extern IntPtr EvpRC2Cbc();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_RC2Ecb")]
        internal static extern IntPtr EvpRC2Ecb();

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_ChaCha20Poly1305")]
        internal static extern IntPtr EvpChaCha20Poly1305();

        internal enum EvpCipherDirection : int
        {
            NoChange = -1,
            Decrypt = 0,
            Encrypt = 1,
        }
    }
}
