// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal
{
    internal sealed class PinnedObject : CriticalDisposableObject
    {
        // can't be read-only since GCHandle is a mutable struct
        private GCHandle _handle;

        // non-zero indicates a valid handle
        private int _isValid;

        public PinnedObject(object obj)
        {
#if FEATURE_CER
            // Make sure the current thread isn't aborted in between allocating the handle and storing it.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { /* intentionally left blank */ }
            finally
#endif
            {
                _handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
                _isValid = 1;
            }
        }

        protected override void Release()
        {
#if FEATURE_CER
            // Make sure the current thread isn't aborted in between zeroing the handle and freeing it.
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try
            {
            }
            finally
            {
                if (Interlocked.Exchange(ref _isValid, 0) != 0)
                {
                    _handle.Free();
                }
            }
        }

        public unsafe byte* Pointer => (byte*)_handle.AddrOfPinnedObject();
    }
}
