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
        #region intentionaly opaque internal structure

#pragma warning disable CS0649 // temporary until we have implementation
        internal unsafe JSBindingHeader* Header;
        internal unsafe JSBindingType* Sigs;// points to first arg, not exception, not result
        internal JSObject? JSFunction;
#pragma warning restore CS0649

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

        #endregion

        /// <summary>
        /// Invokes a previously bound JavaScript function using the provided span to transport argument and return values.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeJS(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Locates and binds a JavaScript function given name and module so that it can later be invoked by managed callers.
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        // JavaScriptExports need to be protected from trimming because they are used from C/JS code which linker can't see
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JavaScriptExports", "System.Runtime.InteropServices.JavaScript")]
        public static JSFunctionBinding BindJSFunction(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Low level implementation of creating and binding JavaScript function, so that managed code could be called from JavaScript.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public static JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            throw new NotImplementedException();
        }
    }
}
