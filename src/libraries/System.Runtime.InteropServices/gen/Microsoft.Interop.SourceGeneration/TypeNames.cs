// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    public static class TypeNames
    {
        public const string GlobalAlias = "global::";

        public const string DllImportAttribute = "System.Runtime.InteropServices.DllImportAttribute";
        public const string ErrorHandlerAttribute = "System.Runtime.InteropServices.ErrorHandlerAttribute";
        public const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
        public const string LibraryImportAttribute_ShortName = "LibraryImportAttribute";
        public const string StringMarshalling = "System.Runtime.InteropServices.StringMarshalling";

        public const string NativeMarshallingAttribute = "System.Runtime.InteropServices.Marshalling.NativeMarshallingAttribute";

        public const string MarshalUsingAttribute = "System.Runtime.InteropServices.Marshalling.MarshalUsingAttribute";

        public const string CustomMarshallerAttribute = "System.Runtime.InteropServices.Marshalling.CustomMarshallerAttribute";
        public const string CustomMarshallerAttributeGenericPlaceholder = CustomMarshallerAttribute + ".GenericPlaceholder";
        public const string ContiguousCollectionMarshallerAttribute = "System.Runtime.InteropServices.Marshalling.ContiguousCollectionMarshallerAttribute";

        public const string AnsiStringMarshaller = "System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller";
        public const string BStrStringMarshaller = "System.Runtime.InteropServices.Marshalling.BStrStringMarshaller";
        public const string Utf16StringMarshaller = "System.Runtime.InteropServices.Marshalling.Utf16StringMarshaller";
        public const string Utf8StringMarshaller = "System.Runtime.InteropServices.Marshalling.Utf8StringMarshaller";
        public const string ExceptionAsVoidMarshaller = "System.Runtime.InteropServices.Marshalling.ExceptionAsVoidMarshaller";
        public const string ExceptionAsHResultMarshaller = "System.Runtime.InteropServices.Marshalling.ExceptionAsHResultMarshaller";
        public const string ExceptionAsNaNMarshaller = "System.Runtime.InteropServices.Marshalling.ExceptionAsNaNMarshaller";
        public const string ExceptionAsDefaultMarshaller = "System.Runtime.InteropServices.Marshalling.ExceptionAsDefaultMarshaller";

        public const string LCIDConversionAttribute = "System.Runtime.InteropServices.LCIDConversionAttribute";

        public const string SuppressGCTransitionAttribute = "System.Runtime.InteropServices.SuppressGCTransitionAttribute";

        public const string UnmanagedCallConvAttribute = "System.Runtime.InteropServices.UnmanagedCallConvAttribute";

        public const string UnmanagedCallersOnlyAttribute = "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute";

        public const string System_Runtime_InteropServices_ComImportAttribute = "System.Runtime.InteropServices.ComImportAttribute";
        public const string System_Runtime_InteropServices_ComVisibleAttribute = "System.Runtime.InteropServices.ComVisibleAttribute";

        public const string VirtualMethodIndexAttribute = "System.Runtime.InteropServices.Marshalling.VirtualMethodIndexAttribute";
        public const string VirtualMethodIndexAttribute_ShortName = "VirtualMethodIndexAttribute";

        public const string IUnmanagedVirtualMethodTableProvider = "System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider";

        public const string IUnmanagedInterfaceType_Metadata = "System.Runtime.InteropServices.Marshalling.IUnmanagedInterfaceType";

        public const string System_Span_Metadata = "System.Span`1";
        public const string System_Span = GlobalAlias + "System.Span";
        public const string System_ReadOnlySpan_Metadata = "System.ReadOnlySpan`1";
        public const string System_ReadOnlySpan = GlobalAlias + "System.ReadOnlySpan";

        public const string System_IntPtr = "System.IntPtr";

        public const string System_Exception = "System.Exception";

        public const string System_GC = "System.GC";

        public const string System_Type = "System.Type";

        public const string System_Int16 = "System.Int16";
        public const string @short = "short";

        public const string System_Runtime_InteropServices_MarshalAsAttribute = "System.Runtime.InteropServices.MarshalAsAttribute";

        public const string System_Runtime_InteropServices_Marshal = "System.Runtime.InteropServices.Marshal";

        private const string System_Runtime_InteropServices_MarshalEx = "System.Runtime.InteropServices.MarshalEx";

        public const string System_Diagnostics_StackTraceHiddenAttribute = "System.Diagnostics.StackTraceHiddenAttribute";

        public const string System_Diagnostics_DebuggerHiddenAttribute = "System.Diagnostics.DebuggerHiddenAttribute";

        public static string MarshalEx(InteropGenerationOptions options)
        {
            return options.UseMarshalType ? System_Runtime_InteropServices_Marshal : System_Runtime_InteropServices_MarshalEx;
        }

        public const string System_Runtime_InteropServices_UnmanagedType = "System.Runtime.InteropServices.UnmanagedType";

        public const string System_Runtime_InteropServices_MemoryMarshal = "System.Runtime.InteropServices.MemoryMarshal";

        public const string System_Runtime_InteropServices_ArrayMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.ArrayMarshaller`2";

        public const string System_Runtime_InteropServices_PointerArrayMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller`2";

        public const string System_Runtime_InteropServices_SafeHandle = "System.Runtime.InteropServices.SafeHandle";

        public const string System_Runtime_InteropServices_OutAttribute = "System.Runtime.InteropServices.OutAttribute";

        public const string System_Runtime_InteropServices_InAttribute = "System.Runtime.InteropServices.InAttribute";

        public const string System_Runtime_CompilerServices_SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

        public const string System_Runtime_CompilerServices_Unsafe = "System.Runtime.CompilerServices.Unsafe";

        public const string System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute = "System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute";

        public const string DefaultDllImportSearchPathsAttribute = "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute";

        public const string DllImportSearchPath = "System.Runtime.InteropServices.DllImportSearchPath";

        public const string System_CodeDom_Compiler_GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";

        public const string System_Guid = "System.Guid";

        public const string GeneratedComInterfaceAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute";
        public const string GeneratedComInterfaceAttribute_ShortName = "GeneratedComInterfaceAttribute";

        public const string InterfaceTypeAttribute = "System.Runtime.InteropServices.InterfaceTypeAttribute";

        public const string ComInterfaceType = "System.Runtime.InteropServices.ComInterfaceType";

        public const string System_Runtime_InteropServices_GuidAttribute = "System.Runtime.InteropServices.GuidAttribute";

        public const string System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch = "System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch";

        public const string UnmanagedObjectUnwrapperAttribute = "System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapperAttribute`1";

        public const string UnmanagedObjectUnwrapper = "System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapper";

        public const string GeneratedComClassAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute";
        public const string System_Runtime_InteropServices_Marshalling_SafeHandleMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller`1";

        public const string System_Runtime_InteropServices_Marshalling_ComInterfaceMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller`1";

        public const string System_Runtime_InteropServices_Marshalling_ComObject = "System.Runtime.InteropServices.Marshalling.ComObject";

        public const string System_Runtime_InteropServices_BestFitMappingAttribute = "System.Runtime.InteropServices.BestFitMappingAttribute";

        public const string System_Runtime_InteropServices_CLong = "System.Runtime.InteropServices.CLong";

        public const string System_Runtime_InteropServices_CULong = "System.Runtime.InteropServices.CULong";

        public const string System_Runtime_InteropServices_NFloat = "System.Runtime.InteropServices.NFloat";

        public const string ComVariantMarshaller = "System.Runtime.InteropServices.Marshalling.ComVariantMarshaller";
        public const string WasmImportLinkageAttribute = "System.Runtime.InteropServices.WasmImportLinkageAttribute";
    }
}
