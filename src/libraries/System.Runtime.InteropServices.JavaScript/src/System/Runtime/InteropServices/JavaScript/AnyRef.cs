// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.JavaScript
{
    public abstract class AnyRef : SafeHandleMinusOneIsInvalid
    {
        public int JSHandle
        {
            get => (int)handle;
        }

        private AnyRef() : base(true) { }

        internal GCHandle AnyRefHandle;

        internal AnyRef(int js_handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle((IntPtr)js_handle);
            AnyRefHandle = GCHandle.Alloc(this, ownsHandle ? GCHandleType.Weak : GCHandleType.Normal);
        }

        internal AnyRef(IntPtr js_handle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(js_handle);
            AnyRefHandle = GCHandle.Alloc(this, ownsHandle ? GCHandleType.Weak : GCHandleType.Normal);
        }
        internal int Int32Handle => (int)(IntPtr)AnyRefHandle;

#if DEBUG_HANDLE
        private int _refCount = 0;

        internal void AddRef()
        {
            Interlocked.Increment(ref _refCount);
        }

        internal void Release()
        {
            Debug.Assert(_refCount > 0, "AnyRefSafeHandle: Release() called more times than AddRef");
            Interlocked.Decrement(ref _refCount);
        }

        internal int RefCount => _refCount;
#endif
    }
}
