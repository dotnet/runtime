// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents a bound imported or exported JavaScript function and contains information necessary to invoke it.
    /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [CLSCompliant(false)]
    [SupportedOSPlatform("browser")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed partial class JSFunctionBinding
    {
        #region intentionally opaque internal structure
        internal unsafe JSBindingHeader* Header;
        internal unsafe JSBindingType* Sigs;// points to first arg, not exception, not result
        internal JSObject? JSFunction;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct JSBindingHeader
        {
            internal const int JSMarshalerSignatureHeaderSize = 4 + 4; // without Exception and Result

            public int Version;
            public int ArgumentCount;
            public JSBindingType Exception;
            public JSBindingType Result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 32)]
        internal struct JSBindingType
        {
            internal MarshalerType Type;
            internal IntPtr __Reserved;
            internal IntPtr JSCustomMarshallerCode;
            internal int JSCustomMarshallerCodeLength;
            internal MarshalerType ResultMarshalerType;
            internal MarshalerType Arg1MarshalerType;
            internal MarshalerType Arg2MarshalerType;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeJS(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
            InvokeJSImpl(signature.JSFunction!, arguments);
        }

        /// <summary>
        /// Locates and binds a JavaScript function given name and module so that it can later be invoked by managed callers.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        // JavaScriptExports need to be protected from trimming because they are used from C/JS code which IL linker can't see
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JavaScriptExports", "System.Runtime.InteropServices.JavaScript")]
        // TODO make this DynamicDependency conditional
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.LegacyExports", "System.Runtime.InteropServices.JavaScript")]
        public static JSFunctionBinding BindJSFunction(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Wasm)
                throw new PlatformNotSupportedException();

            return BindJSFunctionImpl(functionName, moduleName, signatures);
        }

        /// <summary>
        /// Binds a specific managed function wrapper so that it can later be invoked by JavaScript callers.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Wasm)
                throw new PlatformNotSupportedException();

            return BindManagedFunctionImpl(fullyQualifiedName, signatureHash, signatures);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void InvokeJSImpl(JSObject jsFunction, Span<JSMarshalerArgument> arguments)
        {
            IntPtr functionJSHandle = jsFunction.JSHandle;
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.InvokeJSFunction(functionJSHandle, ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe JSFunctionBinding BindJSFunctionImpl(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var signature = JSHostImplementation.GetMethodSignature(signatures);

            Interop.Runtime.BindJSFunction(functionName, moduleName, signature.Header, out IntPtr jsFunctionHandle, out int isException, out object exceptionMessage);
            if (isException != 0)
                throw new JSException((string)exceptionMessage);

            signature.JSFunction = JSHostImplementation.CreateCSOwnedProxy(jsFunctionHandle);

            return signature;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe JSFunctionBinding BindManagedFunctionImpl(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var signature = JSHostImplementation.GetMethodSignature(signatures);

            Interop.Runtime.BindCSFunction(fullyQualifiedName, signatureHash, signature.Header, out int isException, out object exceptionMessage);
            if (isException != 0)
            {
                throw new JSException((string)exceptionMessage);
            }
            return signature;
        }
    }
}
