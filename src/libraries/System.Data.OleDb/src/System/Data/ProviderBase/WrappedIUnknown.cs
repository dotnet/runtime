// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.ProviderBase
{
    // We wrap the interface as a native IUnknown IntPtr so that every
    // thread that creates a connection will fake the correct context when
    // in transactions, otherwise everything is marshalled.  We do this
    // for two reasons: first for the connection pooler, this is a significant
    // performance gain, second for the OLE DB provider, it doesn't marshal.

    internal class WrappedIUnknown : SafeHandle
    {
        internal WrappedIUnknown() : base(IntPtr.Zero, true)
        {
        }

        internal WrappedIUnknown(object? unknown) : this()
        {
            if (null != unknown)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    base.handle = Marshal.GetIUnknownForObject(unknown);
                }
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return (IntPtr.Zero == base.handle);
            }
        }

        internal object ComWrapper()
        {
            // NOTE: Method, instead of property, to avoid being evaluated at
            // runtime in the debugger.
            object? value = null;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr handle = DangerousGetHandle();
                value = Marshal.GetObjectForIUnknown(handle);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                Marshal.Release(ptr);
            }
            return true;
        }
    }
}
