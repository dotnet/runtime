// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents a bound imported or exported JavaScript function and contains information necessary to invoke it.
    /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [CLSCompliant(false)]
    [SupportedOSPlatform("browser")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JSFunctionBinding
    {
        /// <summary>
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        internal JSFunctionBinding() { }

        #region intentionally opaque internal structure

        internal unsafe JSBindingHeader* Header;
        internal unsafe JSBindingType* Sigs;// points to first arg, not exception, not result
        internal static volatile uint nextImportHandle = 1;
        internal int ImportHandle;
        internal bool IsAsync;
        internal bool IsDiscardNoWait;
#if DEBUG
        internal string? FunctionName;
#endif

        // keep in sync with JSBindingHeaderOffsets in marshal.ts
        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        internal struct JSBindingHeader
        {
            internal const int JSMarshalerSignatureHeaderSize = 4 * 8; // without Exception and Result

            [FieldOffset(0)]
            public int Version;
            [FieldOffset(4)]
            public int ArgumentCount;
            [FieldOffset(8)]
            public int ImportHandle;
            [FieldOffset(16)]
            public int FunctionNameOffset;
            [FieldOffset(20)]
            public int FunctionNameLength;
            [FieldOffset(24)]
            public int ModuleNameOffset;
            [FieldOffset(28)]
            public int ModuleNameLength;
            [FieldOffset(32)]
            public JSBindingType Exception;
            [FieldOffset(64)]
            public JSBindingType Result;
        }

        // keep in sync with JSBindingTypeOffsets in marshal.ts
        [StructLayout(LayoutKind.Explicit, Pack = 4, Size = 32)]
        internal struct JSBindingType
        {
            [FieldOffset(0)]
            internal MarshalerType Type;
            [FieldOffset(16)]
            internal MarshalerType ResultMarshalerType;
            [FieldOffset(20)]
            internal MarshalerType Arg1MarshalerType;
            [FieldOffset(24)]
            internal MarshalerType Arg2MarshalerType;
            [FieldOffset(28)]
            internal MarshalerType Arg3MarshalerType;
        }

        internal unsafe int ArgumentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].ArgumentCount;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].ArgumentCount = value;
            }
        }

        internal unsafe int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].Version;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].Version = value;
            }
        }

        internal unsafe JSBindingType Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].Result;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].Result = value;
            }
        }

        internal unsafe JSBindingType Exception
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Header[0].Exception;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Header[0].Exception = value;
            }
        }

        // one based position of args, not exception, not result
        internal unsafe JSBindingType this[int position]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Sigs[position - 1];
            }
        }

        #endregion

        /// <summary>
        /// Invokes a previously bound JavaScript function using the provided span to transport argument and return values.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="signature">Generated metadata about the method signature used for marshaling.</param>
        /// <param name="arguments">The intermediate buffer with marshalled arguments.</param>
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void InvokeJS(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
            InvokeJSImportImpl(signature, arguments);
        }

        /// <summary>
        /// Locates and binds a JavaScript function given name and module so that it can later be invoked by managed callers.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="functionName">The name of the exported JavaScript function.</param>
        /// <param name="moduleName">The name of the ES6 module.</param>
        /// <param name="signatures">The metadata about the signature of the marshaled parameters.</param>
        /// <returns>The method metadata.</returns>
        /// <exception cref="PlatformNotSupportedException">The method is executed on an architecture other than WebAssembly.</exception>
        // JavaScriptExports need to be protected from trimming because they are used from C/JS code which IL linker can't see
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JavaScriptExports", "System.Runtime.InteropServices.JavaScript")]
        public static JSFunctionBinding BindJSFunction(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Wasm)
                throw new PlatformNotSupportedException();

            return BindJSImportImpl(functionName, moduleName, signatures);
        }

        /// <summary>
        /// Binds a specific managed function wrapper so that it can later be invoked by JavaScript callers.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="fullyQualifiedName">The fully qualified name of the exported method.</param>
        /// <param name="signatureHash">The hash of the signature metadata.</param>
        /// <param name="signatures">The metadata about the signature of the marshaled parameters.</param>
        /// <returns>The method metadata.</returns>
        /// <exception cref="PlatformNotSupportedException">The method is executed on architecture other than WebAssembly.</exception>
        public static JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Wasm)
                throw new PlatformNotSupportedException();

            // this could be called by assembly module initializer from Net7 code-gen
            // on wrong thread, in which case we will bind it to UI thread

            return JSHostImplementation.BindManagedFunction(fullyQualifiedName, signatureHash, signatures);
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void InvokeJSFunction(JSObject jsFunction, Span<JSMarshalerArgument> arguments)
        {
            jsFunction.AssertNotDisposed();

#if FEATURE_WASM_MANAGED_THREADS
            ref JSMarshalerArgument exc = ref arguments[0];
            exc.slot.CallerNativeTID = jsFunction.ProxyContext.NativeTID;

            // if we are on correct thread already, just call it
            if (jsFunction.ProxyContext.IsCurrentThread())
            {
                InvokeJSFunctionCurrent(jsFunction, arguments);
            }
            else
            {
                DispatchJSFunctionSync(jsFunction, arguments);
            }
            // async functions are not implemented
#else
            InvokeJSFunctionCurrent(jsFunction, arguments);
#endif
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void InvokeJSFunctionCurrent(JSObject jsFunction, Span<JSMarshalerArgument> arguments)
        {
#if FEATURE_WASM_MANAGED_THREADS
            if (JSProxyContext.ThreadInteropMode == JSHostImplementation.JSThreadInteropMode.NoSyncJSInterop)
            {
                throw new PlatformNotSupportedException("Cannot call synchronous JS functions.");
            }
            else if (jsFunction.ProxyContext.IsPendingSynchronousCall)
            {
                throw new PlatformNotSupportedException("Cannot call synchronous JS function from inside a synchronous call to a C# method.");
            }
#endif

            var functionHandle = (int)jsFunction.JSHandle;
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.InvokeJSFunction(functionHandle, (nint)ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
        }


#if FEATURE_WASM_MANAGED_THREADS
#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void DispatchJSFunctionSync(JSObject jsFunction, Span<JSMarshalerArgument> arguments)
        {
#if FEATURE_WASM_MANAGED_THREADS
            if (JSProxyContext.ThreadInteropMode == JSHostImplementation.JSThreadInteropMode.NoSyncJSInterop)
            {
                throw new PlatformNotSupportedException("Cannot call synchronous JS functions.");
            }
            else if (jsFunction.ProxyContext.IsPendingSynchronousCall)
            {
                throw new PlatformNotSupportedException("Cannot call synchronous JS function from inside a synchronous call to a C# method.");
            }
#endif
            var args = (nint)Unsafe.AsPointer(ref arguments[0]);
            var functionHandle = jsFunction.JSHandle;

            // we already know that we are not on the right thread
            // this will be blocking until resolved by that thread
            // we don't have to disable ThrowOnBlockingWaitOnJSInteropThread, because this is lock in native code
            // we also don't throw PNSE here, because we know that the target has JS interop installed and that it could not block
            // so it could take some time, while target is CPU busy, but not forever
            // see also https://github.com/dotnet/runtime/issues/76958#issuecomment-1921418290
            Interop.Runtime.InvokeJSFunctionSend(jsFunction.ProxyContext.JSNativeTID, functionHandle, args);

            ref JSMarshalerArgument exceptionArg = ref arguments[0];
            if (exceptionArg.slot.Type != MarshalerType.None)
            {
                JSHostImplementation.ThrowException(ref exceptionArg);
            }
        }
#endif

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void InvokeJSImportImpl(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
            ref JSMarshalerArgument exc = ref arguments[0];
            ref JSMarshalerArgument res = ref arguments[1];
#if FEATURE_WASM_MANAGED_THREADS
            var targetContext = JSProxyContext.SealJSImportCapturing();
            exc.slot.CallerNativeTID = targetContext.NativeTID;
            exc.slot.ContextHandle = targetContext.ContextHandle;
            res.slot.ContextHandle = targetContext.ContextHandle;
#else
            var targetContext = JSProxyContext.MainThreadContext;
#endif

            if (signature.IsAsync)
            {
                // pre-allocate the result handle and Task
                var holder = targetContext.CreatePromiseHolder();
                res.slot.Type = MarshalerType.TaskPreCreated;
                res.slot.GCHandle = holder.GCHandle;
            }
#if FEATURE_WASM_MANAGED_THREADS
            else
            {
                if (JSProxyContext.ThreadInteropMode == JSHostImplementation.JSThreadInteropMode.NoSyncJSInterop)
                {
                    throw new PlatformNotSupportedException("Cannot call synchronous JS functions.");
                }
                else if (targetContext.IsPendingSynchronousCall)
                {
                    throw new PlatformNotSupportedException("Cannot call synchronous JS function from inside a synchronous call to a C# method.");
                }
            }
#endif

            if (signature.IsDiscardNoWait)
            {
                arguments[1].slot.Type = MarshalerType.DiscardNoWait;
            }

#if FEATURE_WASM_MANAGED_THREADS
            // if we are on correct thread already or this is synchronous call, just call it
            if (targetContext.IsCurrentThread())
            {
                InvokeJSImportCurrent(signature, arguments);

#if DEBUG
                if (signature.IsAsync && arguments[1].slot.Type == MarshalerType.None)
                {
                    throw new InvalidOperationException("null Task/Promise return is not supported");
                }
#endif

            }
            else if (signature.IsAsync || signature.IsDiscardNoWait)
            {
                //async
                DispatchJSImportAsyncPost(signature, targetContext, arguments);
            }
            else
            {
                //sync
                DispatchJSImportSyncSend(signature, targetContext, arguments);
            }
#else
            InvokeJSImportCurrent(signature, arguments);

            if (signature.IsAsync)
            {
                // if js synchronously returned null
                if (arguments[1].slot.Type == MarshalerType.None)
                {
                    var holderHandle = (GCHandle)arguments[1].slot.GCHandle;
                    holderHandle.Free();
                }
            }
#endif
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void InvokeJSImportCurrent(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
            fixed (JSMarshalerArgument* args = arguments)
            {
#if FEATURE_WASM_MANAGED_THREADS
                Interop.Runtime.InvokeJSImportSync((nint)signature.Header, (nint)args);
#else
                Interop.Runtime.InvokeJSImportST(signature.ImportHandle, (nint)args);
#endif
            }

            ref JSMarshalerArgument exceptionArg = ref arguments[0];
            if (exceptionArg.slot.Type != MarshalerType.None)
            {
                JSHostImplementation.ThrowException(ref exceptionArg);
            }
        }

#if FEATURE_WASM_MANAGED_THREADS

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void DispatchJSImportSyncSend(JSFunctionBinding signature, JSProxyContext targetContext, Span<JSMarshalerArgument> arguments)
        {
            var args = (nint)Unsafe.AsPointer(ref arguments[0]);
            var sig = (nint)signature.Header;

            ref JSMarshalerArgument exc = ref arguments[0];

            // we already know that we are not on the right thread
            // this will be blocking until resolved by that thread
            // we don't have to disable ThrowOnBlockingWaitOnJSInteropThread, because this is lock in native code
            // we also don't throw PNSE here, because we know that the target has JS interop installed and that it could not block
            // so it could take some time, while target is CPU busy, but not forever
            // see also https://github.com/dotnet/runtime/issues/76958#issuecomment-1921418290
            Interop.Runtime.InvokeJSImportSyncSend(targetContext.JSNativeTID, sig, args);

            if (exc.slot.Type != MarshalerType.None)
            {
                JSHostImplementation.ThrowException(ref exc);
            }
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void DispatchJSImportAsyncPost(JSFunctionBinding signature, JSProxyContext targetContext, Span<JSMarshalerArgument> arguments)
        {
            // meaning JS side needs to dispose it
            ref JSMarshalerArgument exc = ref arguments[0];
            exc.slot.ReceiverShouldFree = true;

            var bytes = sizeof(JSMarshalerArgument) * arguments.Length;
            void* cpy = (void*)Marshal.AllocHGlobal(bytes);
            void* src = Unsafe.AsPointer(ref arguments[0]);
            Unsafe.CopyBlock(cpy, src, (uint)bytes);
            var sig = (nint)signature.Header;

            // we already know that we are not on the right thread
            // this will return quickly after sending the message
            // async
            Interop.Runtime.InvokeJSImportAsyncPost(targetContext.JSNativeTID, sig, (nint)cpy);

        }

#endif

        internal static unsafe JSFunctionBinding BindJSImportImpl(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var signature = JSHostImplementation.GetMethodSignature(signatures, functionName, moduleName);

#if !FEATURE_WASM_MANAGED_THREADS

            Interop.Runtime.BindJSImport(signature.Header, out int isException, out object exceptionMessage);
            if (isException != 0)
                throw new JSException((string)exceptionMessage);

            JSHostImplementation.FreeMethodSignatureBuffer(signature);

#endif

            return signature;
        }

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static unsafe void ResolveOrRejectPromise(JSProxyContext targetContext, Span<JSMarshalerArgument> arguments)
        {
            ref JSMarshalerArgument exc = ref arguments[0];
#if FEATURE_WASM_MANAGED_THREADS
            exc.slot.CallerNativeTID = targetContext.NativeTID;

            if (targetContext.IsCurrentThread())
#endif
            {
                fixed (JSMarshalerArgument* ptr = arguments)
                {
                    Interop.Runtime.ResolveOrRejectPromise((nint)ptr);
                    if (exc.slot.Type != MarshalerType.None)
                    {
                        JSHostImplementation.ThrowException(ref exc);
                    }
                }
            }
#if FEATURE_WASM_MANAGED_THREADS
            else
            {
                // meaning JS side needs to dispose it
                exc.slot.ReceiverShouldFree = true;

                // this copy is freed in mono_wasm_resolve_or_reject_promise
                var bytes = sizeof(JSMarshalerArgument) * arguments.Length;
                void* cpy = (void*)Marshal.AllocHGlobal(bytes);
                void* src = Unsafe.AsPointer(ref arguments[0]);
                Unsafe.CopyBlock(cpy, src, (uint)bytes);

                // async
                Interop.Runtime.ResolveOrRejectPromisePost(targetContext.JSNativeTID, (nint)cpy);

                // this never throws directly
            }
#endif
        }
    }
}
