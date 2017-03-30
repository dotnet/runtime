// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal enum PAL_SymmetricAlgorithm
        {
            AES = 0,
            DES = 1,
            TripleDES = 2,
            RC2 = 5,
        }

        internal enum PAL_SymmetricOperation
        {
            Encrypt = 0,
            Decrypt = 1,
        }

        internal enum PAL_PaddingMode
        {
            None = 0,
            Pkcs7 = 1,
        }

        internal enum PAL_ChainingMode
        {
            ECB = 1,
            CBC = 2,
        }

        internal enum PAL_SymmetricOptions
        {
            None = 0,
        }

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorFree")]
        internal static extern void CryptorFree(IntPtr handle);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorCreate")]
        internal static extern unsafe int CryptorCreate(
            PAL_SymmetricOperation operation,
            PAL_SymmetricAlgorithm algorithm,
            PAL_ChainingMode chainingMode,
            PAL_PaddingMode paddingMode,
            byte* pbKey,
            int cbKey,
            byte* pbIv,
            PAL_SymmetricOptions options,
            out SafeAppleCryptorHandle cryptor,
            out int ccStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorUpdate")]
        internal static extern unsafe int CryptorUpdate(
            SafeAppleCryptorHandle cryptor,
            byte* pbData,
            int cbData,
            byte* pbOutput,
            int cbOutput,
            out int cbWritten,
            out int ccStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorFinal")]
        internal static extern unsafe int CryptorFinal(
            SafeAppleCryptorHandle cryptor,
            byte* pbOutput,
            int cbOutput,
            out int cbWritten,
            out int ccStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorReset")]
        internal static extern unsafe int CryptorReset(SafeAppleCryptorHandle cryptor, byte* pbIv, out int ccStatus);
    }
}

namespace System.Security.Cryptography
{
    internal class SafeAppleCryptorHandle : SafeHandle
    {
        public SafeAppleCryptorHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AppleCrypto.CryptorFree(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
