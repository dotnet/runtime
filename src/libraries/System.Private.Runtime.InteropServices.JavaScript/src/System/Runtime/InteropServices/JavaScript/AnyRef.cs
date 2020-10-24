// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.JavaScript
{
    public abstract class AnyRef : SafeHandleMinusOneIsInvalid
    {
        private GCHandle AnyRefHandle;
        public int JSHandle => (int)handle;

        internal AnyRef(int jsHandle, bool ownsHandle) : this((IntPtr)jsHandle, ownsHandle)
        { }

        internal AnyRef(IntPtr jsHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(jsHandle);
            AnyRefHandle = GCHandle.Alloc(this, ownsHandle ? GCHandleType.Weak : GCHandleType.Normal);
        }
        internal int Int32Handle => (int)(IntPtr)AnyRefHandle;

        protected void FreeGCHandle()
        {
            AnyRefHandle.Free();
        }
#if DEBUG_HANDLE
        private int _refCount;

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
