// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Wrap a string- or SecureString-based object. A null value indicates IntPtr.Zero should be used.
    /// </summary>
    internal sealed partial class SafePasswordHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal int Length { get; private set; }

        public SafePasswordHandle(string? password)
            : base(ownsHandle: true)
        {
            if (password != null)
            {
                handle = Marshal.StringToHGlobalUni(password);
                Length = password.Length;
            }
        }

        public SafePasswordHandle(ReadOnlySpan<char> password)
            : base(ownsHandle: true)
        {
            // "".AsSpan() is not default, so this is compat for "null tries NULL first".
            if (password != default)
            {
                int spanLen;

                checked
                {
                    spanLen = password.Length + 1;
                    handle = Marshal.AllocHGlobal(spanLen * sizeof(char));
                }

                unsafe
                {
                    Span<char> dest = new Span<char>((void*)handle, spanLen);
                    password.CopyTo(dest);
                    dest[password.Length] = '\0';
                }

                Length = password.Length;
            }
        }

        public SafePasswordHandle(SecureString? password)
            : base(ownsHandle: true)
        {
            if (password != null)
            {
                handle = Marshal.SecureStringToGlobalAllocUnicode(password);
                Length = password.Length;
            }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.ZeroFreeGlobalAllocUnicode(handle);
            SetHandle((IntPtr)(-1));
            Length = 0;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && SafeHandleCache<SafePasswordHandle>.IsCachedInvalidHandle(this))
            {
                return;
            }

            base.Dispose(disposing);
        }

        internal ReadOnlySpan<char> DangerousGetSpan()
        {
            if (IsInvalid)
            {
                return default;
            }

            unsafe
            {
                return new ReadOnlySpan<char>((char*)handle, Length);
            }
        }

        public static SafePasswordHandle InvalidHandle =>
            SafeHandleCache<SafePasswordHandle>.GetInvalidHandle(
                () =>
                {
                    var handle = new SafePasswordHandle((string?)null);
                    handle.handle = (IntPtr)(-1);
                    return handle;
                });
    }
}
