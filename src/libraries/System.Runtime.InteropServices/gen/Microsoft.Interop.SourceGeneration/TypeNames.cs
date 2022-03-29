// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    public static class TypeNames
    {
        public const string DllImportAttribute = "System.Runtime.InteropServices.DllImportAttribute";
        public const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
        public const string StringMarshalling = "System.Runtime.InteropServices.StringMarshalling";

        public const string GeneratedMarshallingAttribute = "System.Runtime.InteropServices.GeneratedMarshallingAttribute";

        public const string NativeMarshallingAttribute = "System.Runtime.InteropServices.NativeMarshallingAttribute";

        public const string MarshalUsingAttribute = "System.Runtime.InteropServices.MarshalUsingAttribute";

        public const string CustomTypeMarshallerAttribute = "System.Runtime.InteropServices.CustomTypeMarshallerAttribute";

        public const string CustomTypeMarshallerAttributeGenericPlaceholder = "System.Runtime.InteropServices.CustomTypeMarshallerAttribute.GenericPlaceholder";

        public const string LCIDConversionAttribute = "System.Runtime.InteropServices.LCIDConversionAttribute";

        public const string SuppressGCTransitionAttribute = "System.Runtime.InteropServices.SuppressGCTransitionAttribute";

        public const string UnmanagedCallConvAttribute = "System.Runtime.InteropServices.UnmanagedCallConvAttribute";
        public const string System_Span_Metadata = "System.Span`1";
        public const string System_Span = "System.Span";
        public const string System_ReadOnlySpan_Metadata = "System.ReadOnlySpan`1";
        public const string System_ReadOnlySpan = "System.ReadOnlySpan";

        public const string System_IntPtr = "System.IntPtr";

        public const string System_Activator = "System.Activator";

        public const string System_Type = "System.Type";

        public const string System_Runtime_InteropServices_StructLayoutAttribute = "System.Runtime.InteropServices.StructLayoutAttribute";

        public const string System_Runtime_InteropServices_MarshalAsAttribute = "System.Runtime.InteropServices.MarshalAsAttribute";

        public const string System_Runtime_InteropServices_Marshal = "System.Runtime.InteropServices.Marshal";

        private const string System_Runtime_InteropServices_MarshalEx = "System.Runtime.InteropServices.MarshalEx";

        public static string MarshalEx(InteropGenerationOptions options)
        {
            return options.UseMarshalType ? System_Runtime_InteropServices_Marshal : System_Runtime_InteropServices_MarshalEx;
        }

        public const string System_Runtime_InteropServices_UnmanagedType = "System.Runtime.InteropServices.UnmanagedType";

        public const string System_Runtime_InteropServices_MemoryMarshal = "System.Runtime.InteropServices.MemoryMarshal";

        public const string System_Runtime_InteropServices_GeneratedMarshalling_ArrayMarshaller_Metadata = "System.Runtime.InteropServices.GeneratedMarshalling.ArrayMarshaller`1";

        public const string System_Runtime_InteropServices_GeneratedMarshalling_PtrArrayMarshaller_Metadata = "System.Runtime.InteropServices.GeneratedMarshalling.PtrArrayMarshaller`1";

        public const string System_Runtime_InteropServices_SafeHandle = "System.Runtime.InteropServices.SafeHandle";

        public const string System_Runtime_InteropServices_OutAttribute = "System.Runtime.InteropServices.OutAttribute";

        public const string System_Runtime_InteropServices_InAttribute = "System.Runtime.InteropServices.InAttribute";

        public const string System_Runtime_CompilerServices_SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

        public const string System_Runtime_CompilerServices_Unsafe = "System.Runtime.CompilerServices.Unsafe";

        public const string System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute = "System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute";

        public const string DefaultDllImportSearchPathsAttribute = "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute";

        public const string DllImportSearchPath = "System.Runtime.InteropServices.DllImportSearchPath";

        public const string System_CodeDom_Compiler_GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
    }
}
