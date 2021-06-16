// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static class SafeBufferUtil
    {
        /// <summary>
        /// Creates an unmanaged buffer of the specified length.
        /// </summary>
        public static SafeBuffer CreateSafeBuffer(nuint byteLength)
        {
            return new AllocHGlobalSafeHandle(byteLength);
        }

        private sealed class AllocHGlobalSafeHandle : SafeBuffer
        {
            public AllocHGlobalSafeHandle(nuint cb) : base(ownsHandle: true)
            {
#if !NETCOREAPP
                RuntimeHelpers.PrepareConstrainedRegions();
#endif
                try
                {
                    // intentionally empty to avoid ThreadAbortException in netfx runtimes
                }
                finally
                {
                    SetHandle(Marshal.AllocHGlobal((nint)cb));
                }

                Initialize(cb);
            }

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }
    }
}
