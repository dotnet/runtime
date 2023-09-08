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

#if FEATURE_WASM_THREADS
        private readonly object _thisLock = new object();
        private SynchronizationContext? m_SynchronizationContext;
#endif

        public SynchronizationContext SynchronizationContext
        {
            get
            {
#if FEATURE_WASM_THREADS
                return m_SynchronizationContext!;
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }

#if FEATURE_WASM_THREADS
        // the JavaScript object could only exist on the single web worker and can't migrate to other workers
        internal int OwnerThreadId;
#endif
#if !DISABLE_LEGACY_JS_INTEROP
        internal GCHandle? InFlight;
        internal int InFlightCounter;
#endif
        private bool _isDisposed;

        internal JSObject(IntPtr jsHandle)
        {
            JSHandle = jsHandle;
#if FEATURE_WASM_THREADS
            OwnerThreadId = Thread.CurrentThread.ManagedThreadId;
            m_SynchronizationContext = JSSynchronizationContext.CurrentJSSynchronizationContext;
            if (m_SynchronizationContext == null)
            {
                throw new InvalidOperationException(); // should not happen
            }
#endif
        }

#if !DISABLE_LEGACY_JS_INTEROP
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

#if FEATURE_WASM_THREADS
        internal static void AssertThreadAffinity(object value)
        {
            if (value == null)
            {
                return;
            }
            if (value is JSObject jsObject)
            {
                if (jsObject.OwnerThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    throw new InvalidOperationException("The JavaScript object can be used only on the thread where it was created.");
                }
            }
            if (value is JSException jsException)
            {
                if (jsException.jsException != null && jsException.jsException.OwnerThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    throw new InvalidOperationException("The JavaScript object can be used only on the thread where it was created.");
                }
            }
            if (value is JSHostImplementation.TaskCallback holder)
            {
                if (holder.OwnerThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    throw new InvalidOperationException("The JavaScript promise can be used only on the thread where it was created.");
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
#if FEATURE_WASM_THREADS
                SynchronizationContext.Send(static (JSObject self) =>
                {
                    lock (self._thisLock)
                    {
                        JSHostImplementation.ReleaseCSOwnedObject(self.JSHandle);
                        self._isDisposed = true;
                        self.JSHandle = IntPtr.Zero;
                        self.m_SynchronizationContext = null;
                    } //lock
                }, this);
#else
                JSHostImplementation.ReleaseCSOwnedObject(JSHandle);
                _isDisposed = true;
                JSHandle = IntPtr.Zero;
#endif
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
