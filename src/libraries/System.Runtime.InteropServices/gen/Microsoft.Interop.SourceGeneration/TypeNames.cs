// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    public static class TypeNames
    {
        public const string DllImportAttribute = "System.Runtime.InteropServices.DllImportAttribute";
        public const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
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

        public const string VirtualMethodIndexAttribute = "System.Runtime.InteropServices.Marshalling.VirtualMethodIndexAttribute";

        public const string IUnmanagedVirtualMethodTableProvider = "System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider";

        public const string IUnmanagedInterfaceType_Metadata = "System.Runtime.InteropServices.Marshalling.IUnmanagedInterfaceType";

        public const string System_Span_Metadata = "System.Span`1";
        public const string System_Span = "System.Span";
        public const string System_ReadOnlySpan_Metadata = "System.ReadOnlySpan`1";
        public const string System_ReadOnlySpan = "System.ReadOnlySpan";

        public const string System_IntPtr = "System.IntPtr";

        public const string System_Activator = "System.Activator";

        public const string System_Exception = "System.Exception";

        public const string System_GC = "System.GC";

        public const string System_Type = "System.Type";

        public const string System_Int16 = "System.Int16";
        public const string @short = "short";

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

        public const string System_Runtime_InteropServices_ArrayMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.ArrayMarshaller`2";

        public const string System_Runtime_InteropServices_PointerArrayMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller`2";

        public const string System_Runtime_InteropServices_ArrayMarshaller = "System.Runtime.InteropServices.Marshalling.ArrayMarshaller";

        public const string System_Runtime_InteropServices_PointerArrayMarshaller = "System.Runtime.InteropServices.Marshalling.PointerArrayMarshaller";

        public const string System_Runtime_InteropServices_SafeHandle = "System.Runtime.InteropServices.SafeHandle";

        public const string System_Runtime_InteropServices_OutAttribute = "System.Runtime.InteropServices.OutAttribute";

        public const string System_Runtime_InteropServices_InAttribute = "System.Runtime.InteropServices.InAttribute";

        public const string System_Runtime_CompilerServices_SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

        public const string System_Runtime_CompilerServices_Unsafe = "System.Runtime.CompilerServices.Unsafe";

        public const string System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute = "System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute";

        public const string DefaultDllImportSearchPathsAttribute = "System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute";

        public const string DllImportSearchPath = "System.Runtime.InteropServices.DllImportSearchPath";

        public const string System_CodeDom_Compiler_GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";

        public const string System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute = "System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute";

        public const string System_Guid = "System.Guid";

        public const string System_Runtime_CompilerServices_RuntimeHelpers = "System.Runtime.CompilerServices.RuntimeHelpers";

        public const string GeneratedComInterfaceAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute";

        public const string InterfaceTypeAttribute = "System.Runtime.InteropServices.InterfaceTypeAttribute";

        public const string ComInterfaceTypeAttribute = "System.Runtime.InteropServices.ComInterfaceType";

        public const string System_Runtime_InteropServices_GuidAttribute = "System.Runtime.InteropServices.GuidAttribute";

        public const string System_Runtime_InteropServices_ComWrappers = "System.Runtime.InteropServices.ComWrappers";

        public const string System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch = "System.Runtime.InteropServices.ComWrappers.ComInterfaceDispatch";

        public const string System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry = "System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry";

        public const string System_Runtime_InteropServices_NativeMemory = "System.Runtime.InteropServices.NativeMemory";

        public const string StrategyBasedComWrappers = "System.Runtime.InteropServices.Marshalling.StrategyBasedComWrappers";

        public const string IIUnknownInterfaceType = "System.Runtime.InteropServices.Marshalling.IIUnknownInterfaceType";
        public const string IUnknownDerivedAttribute = "System.Runtime.InteropServices.Marshalling.IUnknownDerivedAttribute";
        public const string IIUnknownDerivedDetails = "System.Runtime.InteropServices.Marshalling.IIUnknownDerivedDetails";

        public const string ComWrappersUnwrapper = "System.Runtime.InteropServices.Marshalling.ComWrappersUnwrapper";
        public const string UnmanagedObjectUnwrapperAttribute = "System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapperAttribute`1";

        public const string IUnmanagedObjectUnwrapper = "System.Runtime.InteropServices.Marshalling.IUnmanagedObjectUnwrapper";
        public const string UnmanagedObjectUnwrapper = "System.Runtime.InteropServices.Marshalling.UnmanagedObjectUnwrapper";

        public const string GeneratedComClassAttribute = "System.Runtime.InteropServices.Marshalling.GeneratedComClassAttribute";
        public const string ComExposedClassAttribute = "System.Runtime.InteropServices.Marshalling.ComExposedClassAttribute";
        public const string IComExposedClass = "System.Runtime.InteropServices.Marshalling.IComExposedClass";

        public const string UnreachableException = "System.Diagnostics.UnreachableException";

        public const string System_Runtime_InteropServices_Marshalling_SafeHandleMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller`1";
    }
}
