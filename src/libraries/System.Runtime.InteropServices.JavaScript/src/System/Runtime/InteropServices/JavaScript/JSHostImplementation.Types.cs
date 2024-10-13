// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        internal unsafe delegate void ToManagedCallback(JSMarshalerArgument* arguments_buffer);

        public sealed unsafe class PromiseHolder
        {
            public bool IsDisposed;
            public readonly nint GCHandle; // could be also virtual GCVHandle
            public ToManagedCallback? Callback;
            public JSProxyContext ProxyContext;
#if FEATURE_WASM_MANAGED_THREADS
            public ManualResetEventSlim? CallbackReady;
            public PromiseHolderState* State;
#endif

            public PromiseHolder(JSProxyContext targetContext)
            {
                GCHandle = (IntPtr)InteropServices.GCHandle.Alloc(this, GCHandleType.Normal);
                ProxyContext = targetContext;
#if FEATURE_WASM_MANAGED_THREADS
                State = (PromiseHolderState*)Marshal.AllocHGlobal(sizeof(PromiseHolderState));
                Interlocked.Exchange(ref (*State).IsResolving, 0);
#endif
            }

            public PromiseHolder(JSProxyContext targetContext, nint gcvHandle)
            {
                GCHandle = gcvHandle;
                ProxyContext = targetContext;
#if FEATURE_WASM_MANAGED_THREADS
                State = (PromiseHolderState*)Marshal.AllocHGlobal(sizeof(PromiseHolderState));
                Interlocked.Exchange(ref (*State).IsResolving, 0);
#endif
            }
        }

        // NOTE: layout has to match PromiseHolderState in marshal-to-cs.ts
        [StructLayout(LayoutKind.Explicit)]
        public struct PromiseHolderState
        {
            [FieldOffset(0)]
            public volatile int IsResolving;
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
        public enum JSThreadBlockingMode : int
        {
            PreventSynchronousJSExport = 0,
            ThrowWhenBlockingWait = 1,
            WarnWhenBlockingWait = 2,
            DangerousAllowBlockingWait = 100,
        }
    }
}
