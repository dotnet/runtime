// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        internal unsafe delegate void ToManagedCallback(JSMarshalerArgument* arguments_buffer);

        public sealed class PromiseHolder
        {
            public readonly nint GCHandle; // could be also virtual GCVHandle
            public ToManagedCallback? Callback;
            public JSProxyContext ProxyContext;
            public bool IsDisposed;
            public bool IsCanceling;
#if FEATURE_WASM_MANAGED_THREADS
            public ManualResetEventSlim? CallbackReady;
#endif

            public PromiseHolder(JSProxyContext targetContext)
            {
                GCHandle = (IntPtr)InteropServices.GCHandle.Alloc(this, GCHandleType.Normal);
                ProxyContext = targetContext;
            }

            public PromiseHolder(JSProxyContext targetContext, nint gcvHandle)
            {
                GCHandle = gcvHandle;
                ProxyContext = targetContext;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle methodHandle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }

        // keep in sync with types\internal.ts
        public enum MainThreadingMode : int
        {
            // Running the managed main thread on UI thread.
            // Managed GC and similar scenarios could be blocking the UI.
            // Easy to deadlock. Not recommended for production.
            UIThread = 0,
            // Running the managed main thread on dedicated WebWorker. Marshaling all JavaScript calls to and from the main thread.
            DeputyThread = 1,
        }

        // keep in sync with types\internal.ts
        public enum JSThreadBlockingMode : int
        {
            // throw PlatformNotSupportedException if blocking .Wait is called on threads with JS interop, like JSWebWorker and Main thread.
            // Avoids deadlocks (typically with pending JS promises on the same thread) by throwing exceptions.
            NoBlockingWait = 0,
            // allow .Wait on all threads.
            // Could cause deadlocks with blocking .Wait on a pending JS Task/Promise on the same thread or similar Task/Promise chain.
            AllowBlockingWait = 100,
        }

        // keep in sync with types\internal.ts
        public enum JSThreadInteropMode : int
        {
            // throw PlatformNotSupportedException if synchronous JSImport/JSExport is called on threads with JS interop, like JSWebWorker and Main thread.
            // calling synchronous JSImport on thread pool or new threads is allowed.
            NoSyncJSInterop = 0,
            // allow non-re-entrant synchronous blocking calls to and from JS on JSWebWorker on threads with JS interop, like JSWebWorker and Main thread.
            // calling synchronous JSImport on thread pool or new threads is allowed.
            SimpleSynchronousJSInterop = 1,
        }
    }
}
