// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.JavaScript
{
    internal static class Constants
    {
        public const string JSMarshalAsAttribute = "System.Runtime.InteropServices.JavaScript.JSMarshalAsAttribute`1";
        public const string MarshalUsingAttribute = "System.Runtime.InteropServices.Marshalling.MarshalUsingAttribute";
        public const string JSImportAttribute = "System.Runtime.InteropServices.JavaScript.JSImportAttribute";
        public const string JSExportAttribute = "System.Runtime.InteropServices.JavaScript.JSExportAttribute";
        public const string JavaScriptMarshal = "System.Runtime.InteropServices.JavaScript.JavaScriptMarshal";

        public const string JSFunctionSignatureGlobal = "global::System.Runtime.InteropServices.JavaScript.JSFunctionBinding";
        public const string JSMarshalerArgumentGlobal = "global::System.Runtime.InteropServices.JavaScript.JSMarshalerArgument";
        public const string ModuleInitializerAttributeGlobal = "global::System.Runtime.CompilerServices.ModuleInitializerAttribute";
        public const string DynamicDependencyAttributeGlobal = "global::System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute";
        public const string ThreadStaticGlobal = "global::System.ThreadStaticAttribute";
        public const string TaskGlobal = "global::System.Threading.Tasks.Task";
        public const string SpanGlobal = "global::System.Span";
        public const string ArraySegmentGlobal = "global::System.ArraySegment";
        public const string FuncGlobal = "global::System.Func";
        public const string ActionGlobal = "global::System.Action";
        public const string ExceptionGlobal = "global::System.Exception";
        public const string OSArchitectureGlobal = "global::System.Runtime.InteropServices.RuntimeInformation.OSArchitecture";
        public const string ArchitectureWasmGlobal = "global::System.Runtime.InteropServices.Architecture.Wasm";
        public const string ArgumentsBuffer = "__arguments_buffer";
        public const string ArgumentException = "__arg_exception";
        public const string ArgumentReturn = "__arg_return";
        public const string ToJSMethod = "ToJS";
        public const string ToJSBigMethod = "ToJSBig";
        public const string ToManagedMethod = "ToManaged";
        public const string ToManagedBigMethod = "ToManagedBig";
        public const string BindJSFunctionMethod = "BindJSFunction";
        public const string BindCSFunctionMethod = "BindManagedFunction";
        public const string JSMarshalerTypeGlobal = "global::System.Runtime.InteropServices.JavaScript.JSMarshalerType";
        public const string JSMarshalerTypeGlobalDot = "global::System.Runtime.InteropServices.JavaScript.JSMarshalerType.";
    }
}
