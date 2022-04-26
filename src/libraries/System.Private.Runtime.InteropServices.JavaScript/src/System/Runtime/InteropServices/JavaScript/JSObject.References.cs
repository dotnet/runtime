// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject : SafeHandleMinusOneIsInvalid
    {
        private GCHandle? InFlight;
        private int InFlightCounter;
        public IntPtr JSHandle => handle;
        public bool IsDisposed { get; private set; }

        public JSObject() : base(true)
        {
            InFlight = null;
            InFlightCounter = 0;

            var jsHandle = Runtime.CreateCSOwnedObject(this, nameof(Object));
            SetHandle(jsHandle);
        }

        public JSObject(string typeName, params object[] _params) : base(true)
        {
            InFlight = null;
            InFlightCounter = 0;

            var jsHandle = Runtime.CreateCSOwnedObject(this, typeName, _params);
            SetHandle(jsHandle);
        }

        internal JSObject(IntPtr jsHandle) : base(true)
        {
            SetHandle(jsHandle);
            InFlight = null;
            InFlightCounter = 0;
        }

        internal void AddInFlight()
        {
            AssertNotDisposed();
            lock (this)
            {
                InFlightCounter++;
                if (InFlightCounter == 1)
                {
                    Debug.Assert(InFlight == null, "InFlight == null");
                    InFlight = GCHandle.Alloc(this, GCHandleType.Normal);
                }
            }
        }

        // Note that we could not use SafeHandle.DangerousAddRef() and DangerousRelease()
        // because we could get to zero InFlightCounter multiple times accross lifetime of the JSObject
        // we only want JSObject to be disposed (from GC finalizer) once there is no in-flight reference and also no natural C# reference
        internal void ReleaseInFlight()
        {
            lock (this)
            {
                Debug.Assert(InFlightCounter != 0, "InFlightCounter != 0");

                InFlightCounter--;
                if (InFlightCounter == 0)
                {
                    Debug.Assert(InFlight.HasValue, "InFlight.HasValue");
                    InFlight.Value.Free();
                    InFlight = null;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
        public void AssertNotDisposed()
#else
        internal void AssertNotDisposed()
#endif
        {
            if (IsDisposed) throw new ObjectDisposedException($"Cannot access a disposed {GetType().Name}.");
        }

#if DEBUG
        public void AssertInFlight(int expectedInFlightCount)
        {
            if (InFlightCounter != expectedInFlightCount) throw new InvalidProgramException($"Invalid InFlightCounter for JSObject {JSHandle}, expected: {expectedInFlightCount}, actual: {InFlightCounter}");
        }
#endif

        protected override bool ReleaseHandle()
        {
            Runtime.ReleaseCSOwnedObject(this);
            SetHandleAsInvalid();
            IsDisposed = true;
            return true;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        public override int GetHashCode() => (int)JSHandle;

        public override string ToString()
        {
            return $"(js-obj js '{JSHandle}')";
        }
    }
}
