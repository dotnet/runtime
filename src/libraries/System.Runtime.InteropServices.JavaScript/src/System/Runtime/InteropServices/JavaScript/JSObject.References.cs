﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject
    {
        internal nint JSHandle;

#if ENABLE_LEGACY_JS_INTEROP
        internal GCHandle? InFlight;
        internal int InFlightCounter;
#endif
        private bool _isDisposed;

        internal JSObject(IntPtr jsHandle)
        {
            JSHandle = jsHandle;
        }

#if ENABLE_LEGACY_JS_INTEROP
        internal void AddInFlight()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
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
        // because we could get to zero InFlightCounter multiple times across lifetime of the JSObject
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
#endif

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        /// <inheritdoc />
        public override int GetHashCode() => (int)JSHandle;

        /// <inheritdoc />
        public override string ToString() => $"(js-obj js '{JSHandle}')";

        private void DisposeThis()
        {
            if (!_isDisposed)
            {
                JSHostImplementation.ReleaseCSOwnedObject(JSHandle);
                _isDisposed = true;
                JSHandle = IntPtr.Zero;
            }
        }

        ~JSObject()
        {
            DisposeThis();
        }

        /// <summary>
        /// Releases any resources used by the proxy and discards the reference to its target JavaScript object.
        /// </summary>
        public void Dispose()
        {
            DisposeThis();
            GC.SuppressFinalize(this);
        }
    }
}
