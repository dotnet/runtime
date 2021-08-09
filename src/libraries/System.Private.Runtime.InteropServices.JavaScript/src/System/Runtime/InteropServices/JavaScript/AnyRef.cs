// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject : SafeHandleMinusOneIsInvalid
    {
        private GCHandle? InFlight;
        private int InFlightCounter;
        private GCHandle AnyRefHandle;
        public int JSHandle => (int)handle;
        internal int GCHandleValue => (int)(IntPtr)AnyRefHandle;
        public bool IsDisposed { get; private set; }

        public JSObject() : this(Interop.Runtime.New<object>())
        {
            object result = Interop.Runtime.BindCoreObject(JSHandle, GCHandleValue, out int exception);
            if (exception != 0)
                throw new JSException(SR.Format(SR.JSObjectErrorBinding, result));
        }

        internal JSObject(IntPtr jsHandle) : base(true)
        {
            SetHandle(jsHandle);
            AnyRefHandle = GCHandle.Alloc(this, GCHandleType.Weak);
            InFlight = null;
            InFlightCounter = 0;
        }


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

        protected override bool ReleaseHandle()
        {
            Runtime.ReleaseJSObject(this);
            SetHandleAsInvalid();
            IsDisposed = true;
            AnyRefHandle.Free();
            return true;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        public override int GetHashCode() => JSHandle;

        public override string ToString()
        {
            return $"(js-obj js '{GCHandleValue}')";
        }
    }
}
