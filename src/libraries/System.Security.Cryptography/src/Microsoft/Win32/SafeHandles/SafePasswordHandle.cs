// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Wrap a string- or SecureString-based object. A null value indicates IntPtr.Zero should be used.
    /// </summary>
    internal sealed partial class SafePasswordHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal int Length { get; private set; }

        /// <summary>
        /// This is used to track if a password was explicitly provided.
        /// A null/empty password is a valid password.
        /// </summary>
        internal bool PasswordProvided { get; }

        public SafePasswordHandle(string? password, bool passwordProvided)
            : base(ownsHandle: true)
        {
            if (password != null)
            {
                handle = Marshal.StringToHGlobalUni(password);
                Length = password.Length;
            }

            PasswordProvided = passwordProvided;
        }

        public SafePasswordHandle(ReadOnlySpan<char> password, bool passwordProvided)
            : base(ownsHandle: true)
        {
            // "".AsSpan() is not null ref, so this is compat for "null tries NULL first".
            if (!Unsafe.IsNullRef(ref MemoryMarshal.GetReference(password)))
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

            PasswordProvided = passwordProvided;
        }

        public SafePasswordHandle(SecureString? password, bool passwordProvided)
            : base(ownsHandle: true)
        {
            if (password != null)
            {
                handle = Marshal.SecureStringToGlobalAllocUnicode(password);
                Length = password.Length;
            }

            PasswordProvided = passwordProvided;
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
                    var handle = new SafePasswordHandle((string?)null, false);
                    handle.handle = (IntPtr)(-1);
                    return handle;
                });
    }
}
