// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.JavaScript
{
    public abstract class AnyRef : SafeHandleMinusOneIsInvalid
    {
        private GCHandle? InFlight;
        private int InFlightCounter;
        private GCHandle AnyRefHandle;
        public int JSHandle => (int)handle;

        internal AnyRef(IntPtr jsHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(jsHandle);
            AnyRefHandle = GCHandle.Alloc(this, ownsHandle ? GCHandleType.Weak : GCHandleType.Normal);
            InFlight = null;
            InFlightCounter = 0;
        }
        internal int GCHandleValue => (int)(IntPtr)AnyRefHandle;

        internal void AddInFlight()
        {
            lock (this)
            {
                InFlightCounter++;
                if (InFlightCounter == 1)
                {
                    Debug.Assert(InFlight == null);
                    InFlight = GCHandle.Alloc(this, GCHandleType.Normal);
                }
            }
        }

        internal void ReleaseInFlight()
        {
            lock (this)
            {
                Debug.Assert(InFlightCounter != 0);

                InFlightCounter--;
                if (InFlightCounter == 0)
                {
                    Debug.Assert(InFlight.HasValue);
                    InFlight.Value.Free();
                    InFlight = null;
                }
            }
        }


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
