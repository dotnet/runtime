// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject
    {
        internal nint JSHandle;
        internal JSProxyContext ProxyContext;

        public SynchronizationContext SynchronizationContext
        {
            get
            {
#if FEATURE_WASM_THREADS
                return ProxyContext.SynchronizationContext;
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }

#if !DISABLE_LEGACY_JS_INTEROP
        internal GCHandle? InFlight;
        internal int InFlightCounter;
#endif
        internal bool _isDisposed;

        internal JSObject(IntPtr jsHandle, JSProxyContext ctx)
        {
            ProxyContext = ctx;
            JSHandle = jsHandle;
        }

#if !DISABLE_LEGACY_JS_INTEROP
        internal void AddInFlight()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            lock (ProxyContext)
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
            lock (ProxyContext)
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

#if FEATURE_WASM_THREADS
        internal static void AssertThreadAffinity(object value)
        {
            if (value == null)
            {
                return;
            }

            var currentContext = JSProxyContext.AssertCurrentContext();

            if (value is JSObject jsObject)
            {
                if (jsObject.ProxyContext != currentContext)
                {
                    throw new InvalidOperationException("The JavaScript object can be used only on the thread where it was created.");
                }
            }
            else if (value is JSException jsException)
            {
                if (jsException.jsException != null && jsException.jsException.ProxyContext != currentContext)
                {
                    throw new InvalidOperationException("The JavaScript object can be used only on the thread where it was created.");
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

        internal void DisposeImpl(bool skipJS)
        {
            if (!_isDisposed)
            {
#if FEATURE_WASM_THREADS
                if (ProxyContext == JSProxyContext.CurrentThreadContext)
                {
                    JSProxyContext.ReleaseCSOwnedObject(this, skipJS);
                    return;
                }

                ProxyContext.SynchronizationContext.Post(static (object? s) =>
                {
                    var x = ((JSObject self, bool skipJS))s!;
                    JSProxyContext.ReleaseCSOwnedObject(x.self, x.skipJS);
                }, (this, skipJS));
#else
                JSProxyContext.ReleaseCSOwnedObject(this, skipJS);
                _isDisposed = true;
                JSHandle = IntPtr.Zero;
#endif
            }
        }

        ~JSObject()
        {
            DisposeImpl(false);
        }

        /// <summary>
        /// Releases any resources used by the proxy and discards the reference to its target JavaScript object.
        /// </summary>
        public void Dispose()
        {
            DisposeImpl(false);
        }
    }
}
