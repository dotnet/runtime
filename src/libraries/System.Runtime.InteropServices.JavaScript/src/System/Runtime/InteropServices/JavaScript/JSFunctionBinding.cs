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

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct JSBindingHeader
        {
            internal const int JSMarshalerSignatureHeaderSize = 4 * 8; // without Exception and Result

            public int Version;
            public int ArgumentCount;
            public int ImportHandle;
            public int _Reserved;
            public int FunctionNameOffset;
            public int FunctionNameLength;
            public int ModuleNameOffset;
            public int ModuleNameLength;
            public JSBindingType Exception;
            public JSBindingType Result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
        internal struct JSBindingType
        {
            internal MarshalerType Type;
            internal MarshalerType __ReservedB1;
            internal MarshalerType __ReservedB2;
            internal MarshalerType __ReservedB3;
            internal IntPtr __Reserved;
            internal IntPtr JSCustomMarshallerCode;
            internal int JSCustomMarshallerCodeLength;
            internal MarshalerType ResultMarshalerType;
            internal MarshalerType __ReservedB4;
            internal MarshalerType __ReservedB5;
            internal MarshalerType __ReservedB6;
            internal MarshalerType Arg1MarshalerType;
            internal MarshalerType __ReservedB7;
            internal MarshalerType __ReservedB8;
            internal MarshalerType __ReservedB9;
            internal MarshalerType Arg2MarshalerType;
            internal MarshalerType __ReservedB10;
            internal MarshalerType __ReservedB11;
            internal MarshalerType __ReservedB12;
            internal MarshalerType Arg3MarshalerType;
            internal MarshalerType __ReservedB13;
            internal MarshalerType __ReservedB14;
            internal MarshalerType __ReservedB15;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        // Same for legacy, but the type could be explicitly trimmed by setting WasmEnableLegacyJsInterop=false which would use ILLink.Descriptors.LegacyJsInterop.xml
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.LegacyExportsTrimmingRoot", "System.Runtime.InteropServices.JavaScript")]
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

            return BindManagedFunctionImpl(fullyQualifiedName, signatureHash, signatures);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InvokeJSFunction(JSObject jsFunction, Span<JSMarshalerArgument> arguments)
        {
            ObjectDisposedException.ThrowIf(jsFunction.IsDisposed, jsFunction);
#if FEATURE_WASM_THREADS
            JSObject.AssertThreadAffinity(jsFunction);
#endif

            var functionHandle = (int)jsFunction.JSHandle;
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.InvokeJSFunction(functionHandle, ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InvokeJSImportImpl(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
#if FEATURE_WASM_THREADS
            var targetContext = JSProxyContext.SealJSImportCapturing();
            JSProxyContext.AssertIsInteropThread();
            arguments[0].slot.ContextHandle = targetContext.ContextHandle;
            arguments[1].slot.ContextHandle = targetContext.ContextHandle;
#else
            var targetContext = JSProxyContext.MainThreadContext;
#endif

            if (signature.IsAsync)
            {
                // pre-allocate the result handle and Task
                var holder = new JSHostImplementation.PromiseHolder(targetContext);
                arguments[1].slot.Type = MarshalerType.TaskPreCreated;
                arguments[1].slot.GCHandle = holder.GCHandle;
            }

            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.InvokeJSImport(signature.ImportHandle, ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
            if (signature.IsAsync)
            {
                // if js synchronously returned null
                if (arguments[1].slot.Type == MarshalerType.None)
                {
                    var holderHandle = (GCHandle)arguments[1].slot.GCHandle;
                    holderHandle.Free();
                }
            }
        }

        internal static unsafe JSFunctionBinding BindJSImportImpl(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
#if FEATURE_WASM_THREADS
            JSProxyContext.AssertIsInteropThread();
#endif

            var signature = JSHostImplementation.GetMethodSignature(signatures, functionName, moduleName);

            Interop.Runtime.BindJSImport(signature.Header, out int isException, out object exceptionMessage);
            if (isException != 0)
                throw new JSException((string)exceptionMessage);

            JSHostImplementation.FreeMethodSignatureBuffer(signature);

            return signature;
        }

        internal static unsafe JSFunctionBinding BindManagedFunctionImpl(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var signature = JSHostImplementation.GetMethodSignature(signatures, null, null);

            Interop.Runtime.BindCSFunction(fullyQualifiedName, signatureHash, signature.Header, out int isException, out object exceptionMessage);
            if (isException != 0)
            {
                throw new JSException((string)exceptionMessage);
            }

            JSHostImplementation.FreeMethodSignatureBuffer(signature);

            return signature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void ResolveOrRejectPromise(Span<JSMarshalerArgument> arguments)
        {
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.ResolveOrRejectPromise(ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
        }
    }
}
