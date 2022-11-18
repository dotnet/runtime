// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            CFB = 3,
            CFB8 = 10,
        }

        internal enum PAL_SymmetricOptions
        {
            None = 0,
        }

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorFree")]
        internal static partial void CryptorFree(IntPtr handle);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorCreate")]
        internal static unsafe partial int CryptorCreate(
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

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorUpdate")]
        internal static unsafe partial int CryptorUpdate(
            SafeAppleCryptorHandle cryptor,
            byte* pbData,
            int cbData,
            byte* pbOutput,
            int cbOutput,
            out int cbWritten,
            out int ccStatus);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_CryptorReset")]
        internal static unsafe partial int CryptorReset(SafeAppleCryptorHandle cryptor, byte* pbIv, out int ccStatus);
    }
}

namespace System.Security.Cryptography
{
    internal sealed class SafeAppleCryptorHandle : SafeHandle
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
