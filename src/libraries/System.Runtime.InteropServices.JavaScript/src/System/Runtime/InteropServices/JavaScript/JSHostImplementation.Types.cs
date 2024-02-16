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
    }
}
