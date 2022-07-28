// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Base class for safe handles representing NULL-based pointers.
    /// </summary>
    internal abstract class SafeCrypt32Handle<T> : SafeHandle where T : SafeHandle, new()
    {
        protected SafeCrypt32Handle()
            : base(IntPtr.Zero, true)
        {
        }

        public sealed override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        public static T InvalidHandle
        {
            get { return SafeHandleCache<T>.GetInvalidHandle(() => new T()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<T>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }
    }
}
