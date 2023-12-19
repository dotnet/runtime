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
            public nint GCHandle; // could be also virtual GCVHandle
            public ToManagedCallback? Callback;
#if FEATURE_WASM_THREADS
            // the JavaScript object could only exist on the single web worker and can't migrate to other workers
            internal JSSynchronizationContext SynchronizationContext;
#endif

#if FEATURE_WASM_THREADS
            // TODO possibly unify signature with non-MT and pass null
            public PromiseHolder(JSSynchronizationContext targetContext)
            {
                GCHandle = (IntPtr)InteropServices.GCHandle.Alloc(this, GCHandleType.Normal);
                SynchronizationContext = targetContext;
            }
#else
            public PromiseHolder()
            {
                GCHandle = (IntPtr)InteropServices.GCHandle.Alloc(this, GCHandleType.Normal);
            }
#endif

            public PromiseHolder(nint gcvHandle)
            {
                GCHandle = gcvHandle;
#if FEATURE_WASM_THREADS
                JSSynchronizationContext.AssertWebWorkerContext();
                SynchronizationContext = JSSynchronizationContext.CurrentJSSynchronizationContext!;
#endif
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
