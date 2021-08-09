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
        public int JSHandle => (int)handle;
        public bool IsDisposed { get; private set; }

        public JSObject() : base(true)
        {
            InFlight = null;
            InFlightCounter = 0;

            var jsHandle = Runtime.CreateCsOwnedObject(this, nameof(Object));
            SetHandle(jsHandle);
        }

        protected JSObject(string typeName, object[] _params) : base(true)
        {
            InFlight = null;
            InFlightCounter = 0;

            var jsHandle = Runtime.CreateCsOwnedObject(this, typeName, _params);
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
            Runtime.ReleaseCsOwnedObject(this);
            SetHandleAsInvalid();
            IsDisposed = true;
            return true;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        public override int GetHashCode() => JSHandle;

        public override string ToString()
        {
            return $"(js-obj js '{JSHandle}')";
        }
    }
}
