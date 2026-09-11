// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeX509Handle : SafeHandle
    {
#if DEBUG
        private static readonly bool s_captureTrace =
            Environment.GetEnvironmentVariable("DEBUG_SAFEX509HANDLE_FINALIZATION") != null;

        // Using reflection to avoid a hard dependency on System.Diagnostics.StackTrace, which prevents
        // System.IO.Compression from referencing this assembly.
        private readonly object? _stacktrace =
            s_captureTrace ? Activator.CreateInstance(Type.GetType("System.Diagnostics.StackTrace")!, true) : null;

        ~SafeX509Handle()
        {
            if (s_captureTrace)
            {
                Console.WriteLine($"0x{handle.ToInt64():x} {_stacktrace?.ToString() ?? "no stacktrace..."}");
            }
        }
#endif

        internal static readonly SafeX509Handle InvalidHandle = new SafeX509Handle();

        public SafeX509Handle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.X509Destroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeX509CrlHandle : SafeHandle
    {
        public SafeX509CrlHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.X509CrlDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal sealed class SafeX509StoreHandle : SafeHandle
    {
        public SafeX509StoreHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.X509StoreDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
