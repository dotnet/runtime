// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Interop.Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_SSLStreamRelease")]
        internal static extern void SSLStreamRelease(IntPtr ptr);

        internal class SslException : Exception
        {
            internal SslException()
            {
            }

            internal SslException(int errorCode)
            {
                HResult = errorCode;
            }
        }
    }
}

namespace System.Net
{
    internal sealed class SafeSslHandle : SafeHandle
    {
        public SafeSslHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.SSLStreamRelease(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
