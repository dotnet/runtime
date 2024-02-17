// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Contains the storage and type information for an argument or return value on the native stack.
    /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [SupportedOSPlatform("browser")]
    [CLSCompliant(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial struct JSMarshalerArgument
    {
        internal JSMarshalerArgumentImpl slot;

        [StructLayout(LayoutKind.Explicit, Pack = 32, Size = 32)]
        internal struct JSMarshalerArgumentImpl
        {
            [FieldOffset(0)]
            internal bool BooleanValue;
            [FieldOffset(0)]
            internal byte ByteValue;
            [FieldOffset(0)]
            internal char CharValue;
            [FieldOffset(0)]
            internal short Int16Value;
            [FieldOffset(0)]
            internal int Int32Value;
            [FieldOffset(0)]
            internal long Int64Value;// must be aligned to 8 because of HEAPI64 alignment
            [FieldOffset(0)]
            internal float SingleValue;
            [FieldOffset(0)]
            internal double DoubleValue;// must be aligned to 8 because of Module.HEAPF64 view alignment
            [FieldOffset(0)]
            internal IntPtr IntPtrValue;

            [FieldOffset(4)]
            internal IntPtr JSHandle;
            [FieldOffset(4)]
            internal IntPtr GCHandle;

            [FieldOffset(8)]
            internal int Length;

            /// <summary>
            /// Discriminators
            /// </summary>
            [FieldOffset(12)]
            internal MarshalerType Type;
            [FieldOffset(13)]
            internal MarshalerType ElementType;

            [FieldOffset(16)]
            internal IntPtr ContextHandle;

            [FieldOffset(20)]
            internal bool ReceiverShouldFree;
        }

        /// <summary>
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Initialize()
        {
            slot.Type = MarshalerType.None;
#if FEATURE_WASM_MANAGED_THREADS
            // we know that this is at the start of some JSImport call, but we don't know yet what would be the target thread
            // also this is called multiple times
            JSProxyContext.JSImportWithUnknownContext();
            slot.ContextHandle = IntPtr.Zero;
#endif
        }

#if FEATURE_WASM_MANAGED_THREADS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void InitializeWithContext(JSProxyContext knownProxyContext)
        {
            slot.Type = MarshalerType.None;
            slot.ContextHandle = knownProxyContext.ContextHandle;
        }
#endif
        // this is always called from ToManaged() marshaler
#pragma warning disable CA1822 // Mark members as static
        internal JSProxyContext ToManagedContext
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
#if !FEATURE_WASM_MANAGED_THREADS
                return JSProxyContext.MainThreadContext;
#else
                // ContextHandle always has to be set
                // during JSImport, this is marshaling result/exception and it would be set by:
                //    - InvokeJSImport implementation
                //    - ActionJS.InvokeJS
                //    - ResolveVoidPromise/ResolvePromise/RejectPromise
                // during JSExport, this is marshaling parameters and it would be set by:
                //    - alloc_stack_frame
                //    - set_js_handle/set_gc_handle
                var proxyContextGCHandle = (GCHandle)slot.ContextHandle;
                if (proxyContextGCHandle == default)
                {
                    Environment.FailFast($"ContextHandle not set, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                }
                var argumentContext = (JSProxyContext)proxyContextGCHandle.Target!;
                return argumentContext;
#endif
            }
        }

        // this is always called from ToJS() marshaler
#pragma warning disable CA1822 // Mark members as static
        internal JSProxyContext ToJSContext
#pragma warning restore CA1822 // Mark members as static
        {
            get
            {
#if !FEATURE_WASM_MANAGED_THREADS
                return JSProxyContext.MainThreadContext;
#else
                if (JSProxyContext.CapturingState == JSProxyContext.JSImportOperationState.JSImportParams)
                {
                    // we are called from ToJS, during JSImport
                    // we need to check for captured or default context
                    return JSProxyContext.CurrentOperationContext;
                }
                // ContextHandle must be set be set by JS side of JSExport, and we are marshaling result of JSExport
                var proxyContextGCHandle = slot.ContextHandle;
                if (proxyContextGCHandle == IntPtr.Zero)
                {
                    Environment.FailFast($"ContextHandle not set, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
                }
                var argumentContext = (JSProxyContext)((GCHandle)proxyContextGCHandle).Target!;
                return argumentContext;
#endif
            }
        }

        // make sure that we are on a thread with JS interop and that it matches the target of the argument
#pragma warning disable CA1822 // Mark members as static
        internal JSProxyContext AssertCurrentThreadContext()
#pragma warning restore CA1822 // Mark members as static
        {
#if !FEATURE_WASM_MANAGED_THREADS
            return JSProxyContext.MainThreadContext;
#else
            var currentThreadContext = JSProxyContext.CurrentThreadContext;
            if (currentThreadContext == null)
            {
                Environment.FailFast($"Must be called on same thread with JS interop, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }
            if (slot.ContextHandle != currentThreadContext.ContextHandle)
            {
                Environment.FailFast($"Must be called on same thread which created the stack frame, ManagedThreadId: {Environment.CurrentManagedThreadId}. {Environment.NewLine} {Environment.StackTrace}");
            }
            return currentThreadContext;
#endif
        }
    }
}
