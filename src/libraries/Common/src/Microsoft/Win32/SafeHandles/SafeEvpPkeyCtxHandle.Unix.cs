// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEvpPKeyCtxHandle : SafeHandle
    {
        public SafeEvpPKeyCtxHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public SafeEvpPKeyCtxHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.EvpPKeyCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        internal static SafeEvpPKeyCtxHandle CreateFromEvpPkey(SafeEvpPKeyHandle evpPkey)
        {
            return Interop.Crypto.EvpPKeyCtxCreate(evpPkey);
        }

        internal void ConfigureForRSASign(HashAlgorithmName hashAlgorithm, RSASignaturePaddingMode padding)
        {
            Interop.Crypto.CryptoNative_ConfigureForRsaSign(this, padding, hashAlgorithm);
        }

        internal void ConfigureForRSAVerify(HashAlgorithmName hashAlgorithm, RSASignaturePaddingMode padding)
        {
            Interop.Crypto.CryptoNative_ConfigureForRsaVerify(this, padding, hashAlgorithm);
        }

        internal void ConfigureForECDSASign()
        {
            Interop.Crypto.EvpPKeyCtxConfigureForECDSASign(this);
        }

        internal void ConfigureForECDSAVerify()
        {
            Interop.Crypto.EvpPKeyCtxConfigureForECDSAVerify(this);
        }

        internal bool TryGetSufficientSignatureSizeInBytesCore(
            ReadOnlySpan<byte> hash, out int bytesWritten)
        {
            return Interop.Crypto.TryEvpPKeyCtxSignatureSize(this, hash, out bytesWritten);
        }

        internal bool TrySignHashCore(
            ReadOnlySpan<byte> hash,
            Span<byte> outputSignature,
            out int bytesWritten)
        {
            return Interop.Crypto.TryEvpPKeyCtxSignHash(this, hash, outputSignature, out bytesWritten);
        }

        internal bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature)
        {
            return Interop.Crypto.EvpPKeyCtxVerifyHash(this, hash, signature);
        }
    }
}
