// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public static class NameSyntaxes
    {
        private static NameSyntax? _DllImportAttribute;
        public static NameSyntax DllImportAttribute => _DllImportAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.DllImportAttribute);

        private static NameSyntax? _LibraryImportAttribute;
        public static NameSyntax LibraryImportAttribute => _LibraryImportAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.LibraryImportAttribute);

        private static NameSyntax? _System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute;
        public static NameSyntax System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute => _System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_DynamicInterfaceCastableImplementationAttribute);

        private static NameSyntax? _System_Runtime_InteropServices_MarshalAsAttribute;
        public static NameSyntax System_Runtime_InteropServices_MarshalAsAttribute => _System_Runtime_InteropServices_MarshalAsAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_MarshalAsAttribute);

        private static NameSyntax? _DefaultDllImportSearchPathsAttribute;
        public static NameSyntax DefaultDllImportSearchPathsAttribute => _DefaultDllImportSearchPathsAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.DefaultDllImportSearchPathsAttribute);

        private static NameSyntax? _SuppressGCTransitionAttribute;
        public static NameSyntax SuppressGCTransitionAttribute => _SuppressGCTransitionAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.SuppressGCTransitionAttribute);

        private static NameSyntax? _UnmanagedCallConvAttribute;
        public static NameSyntax UnmanagedCallConvAttribute => _UnmanagedCallConvAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.UnmanagedCallConvAttribute);

        private static NameSyntax? _System_Runtime_CompilerServices_SkipLocalsInitAttribute;
        public static NameSyntax System_Runtime_CompilerServices_SkipLocalsInitAttribute => _System_Runtime_CompilerServices_SkipLocalsInitAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute);

        private static NameSyntax? _System_CodeDom_Compiler_GeneratedCodeAttribute;
        public static NameSyntax System_CodeDom_Compiler_GeneratedCodeAttribute => _System_CodeDom_Compiler_GeneratedCodeAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.System_CodeDom_Compiler_GeneratedCodeAttribute);

        private static NameSyntax? _UnmanagedCallersOnlyAttribute;
        public static NameSyntax UnmanagedCallersOnlyAttribute => _UnmanagedCallersOnlyAttribute ??= ParseName(TypeNames.GlobalAlias + TypeNames.UnmanagedCallersOnlyAttribute);
    }

    public static class TypeSyntaxes
    {
        public static TypeSyntax Void { get; } = PredefinedType(Token(SyntaxKind.VoidKeyword));

        public static TypeSyntax VoidStar { get; } = PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)));

        public static TypeSyntax VoidStarStar { get; } = PointerType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))));

        public static TypeSyntax Nint { get; } = ParseTypeName(TypeNames.Nint);

        private static TypeSyntax? _StringMarshalling;
        public static TypeSyntax StringMarshalling => _StringMarshalling ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.StringMarshalling);

        private static TypeSyntax? _System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry;
        public static TypeSyntax System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry => _System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry);

        private static TypeSyntax? _System_Runtime_InteropServices_NativeMemory;
        public static TypeSyntax System_Runtime_InteropServices_NativeMemory => _System_Runtime_InteropServices_NativeMemory ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_NativeMemory);

        private static TypeSyntax? _StrategyBasedComWrappers;
        public static TypeSyntax StrategyBasedComWrappers => _StrategyBasedComWrappers ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.StrategyBasedComWrappers);

        private static TypeSyntax? _IUnmanagedVirtualMethodTableProvider;
        public static TypeSyntax IUnmanagedVirtualMethodTableProvider => _IUnmanagedVirtualMethodTableProvider ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.IUnmanagedVirtualMethodTableProvider);

        private static TypeSyntax? _IIUnknownInterfaceType;
        public static TypeSyntax IIUnknownInterfaceType => _IIUnknownInterfaceType ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.IIUnknownInterfaceType);

        private static TypeSyntax? _IIUnknownDerivedDetails;
        public static TypeSyntax IIUnknownDerivedDetails => _IIUnknownDerivedDetails ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.IIUnknownDerivedDetails);

        private static TypeSyntax? _UnmanagedObjectUnwrapper;
        public static TypeSyntax UnmanagedObjectUnwrapper => _UnmanagedObjectUnwrapper ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.UnmanagedObjectUnwrapper);

        private static TypeSyntax? _IComExposedClass;
        public static TypeSyntax IComExposedClass => _IComExposedClass ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.IComExposedClass);

        private static TypeSyntax? _UnreachableException;
        public static TypeSyntax UnreachableException => _UnreachableException ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.UnreachableException);

        private static TypeSyntax? _System_Runtime_CompilerServices_RuntimeHelpers;
        public static TypeSyntax System_Runtime_CompilerServices_RuntimeHelpers => _System_Runtime_CompilerServices_RuntimeHelpers ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_CompilerServices_RuntimeHelpers);

        private static TypeSyntax? _System_Runtime_InteropServices_ComWrappers;
        public static TypeSyntax System_Runtime_InteropServices_ComWrappers => _System_Runtime_InteropServices_ComWrappers ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_ComWrappers);

        private static TypeSyntax? _System_IntPtr;
        public static TypeSyntax System_IntPtr => _System_IntPtr ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_IntPtr);

        private static TypeSyntax? _System_Guid;
        public static TypeSyntax System_Guid => _System_Guid ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Guid);

        private static TypeSyntax? _DllImportSearchPath;
        public static TypeSyntax DllImportSearchPath => _DllImportSearchPath ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.DllImportSearchPath);

        private static TypeSyntax? _System_Type;
        public static TypeSyntax System_Type => _System_Type ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Type);

        private static TypeSyntax? _System_Activator;
        public static TypeSyntax System_Activator => _System_Activator ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Activator);

        private static TypeSyntax? _System_Runtime_InteropServices_Marshal;
        public static TypeSyntax System_Runtime_InteropServices_Marshal => _System_Runtime_InteropServices_Marshal ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_Marshal);

        private static TypeSyntax? _System_Runtime_InteropServices_UnmanagedType;
        public static TypeSyntax System_Runtime_InteropServices_UnmanagedType => _System_Runtime_InteropServices_UnmanagedType ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_UnmanagedType);

        private static TypeSyntax? _System_Runtime_InteropServices_MemoryMarshal;
        public static TypeSyntax System_Runtime_InteropServices_MemoryMarshal => _System_Runtime_InteropServices_MemoryMarshal ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_MemoryMarshal);

        private static TypeSyntax? _System_Exception;
        public static TypeSyntax System_Exception => _System_Exception ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Exception);

        private static TypeSyntax? _System_GC;
        public static TypeSyntax System_GC => _System_GC ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_GC);

        private static TypeSyntax? _System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch;
        public static TypeSyntax System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch => _System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch);

        private static TypeSyntax? _System_Runtime_CompilerServices_Unsafe;
        public static TypeSyntax System_Runtime_CompilerServices_Unsafe => _System_Runtime_CompilerServices_Unsafe ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.System_Runtime_CompilerServices_Unsafe);

        private static TypeSyntax? _CallConvCdecl;
        private static TypeSyntax? _CallConvFastcall;
        private static TypeSyntax? _CallConvMemberFunction;
        private static TypeSyntax? _CallConvStdcall;
        private static TypeSyntax? _CallConvSuppressGCTransition;
        private static TypeSyntax? _CallConvThiscall;
        public static TypeSyntax CallConv(string callConv)
        {
            return callConv switch
            {
                "Cdecl" => _CallConvCdecl ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvCdeclName),
                "Fastcall" => _CallConvFastcall ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvFastcallName),
                "MemberFunction" => _CallConvMemberFunction ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvMemberFunctionName),
                "Stdcall" => _CallConvStdcall ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvStdcallName),
                "SuppressGCTransition" => _CallConvSuppressGCTransition ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvSuppressGCTransitionName),
                "Thiscall" => _CallConvThiscall ??= ParseTypeName(TypeNames.GlobalAlias + TypeNames.CallConvThiscallName),
                _ => throw new ArgumentException($"Unexpected CallConv: {callConv}")
            };
        }
    }

    public static class TypeNames
    {
        public const string GlobalAlias = "global::";

        public const string DllImportAttribute = "System.Runtime.InteropServices.DllImportAttribute";
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
        public const string GeneratedComInterfaceAttribute_ShortName = "GeneratedComInterfaceAttribute";

        public const string InterfaceTypeAttribute = "System.Runtime.InteropServices.InterfaceTypeAttribute";

        public const string ComInterfaceType = "System.Runtime.InteropServices.ComInterfaceType";

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

        public const string System_Runtime_InteropServices_Marshalling_ComInterfaceMarshaller_Metadata = "System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller`1";

        public const string System_Runtime_InteropServices_Marshalling_ComObject = "System.Runtime.InteropServices.Marshalling.ComObject";

        public const string System_Runtime_InteropServices_BestFitMappingAttribute = "System.Runtime.InteropServices.BestFitMappingAttribute";

        public const string System_Runtime_InteropServices_CLong = "System.Runtime.InteropServices.CLong";

        public const string System_Runtime_InteropServices_CULong = "System.Runtime.InteropServices.CULong";

        public const string System_Runtime_InteropServices_NFloat = "System.Runtime.InteropServices.NFloat";

        public const string CallConvCdeclName = "System.Runtime.CompilerServices.CallConvCdecl";
        public const string CallConvFastcallName = "System.Runtime.CompilerServices.CallConvFastcall";
        public const string CallConvStdcallName = "System.Runtime.CompilerServices.CallConvStdcall";
        public const string CallConvThiscallName = "System.Runtime.CompilerServices.CallConvThiscall";
        public const string CallConvSuppressGCTransitionName = "System.Runtime.CompilerServices.CallConvSuppressGCTransition";
        public const string CallConvMemberFunctionName = "System.Runtime.CompilerServices.CallConvMemberFunction";
        public const string Nint = "nint";
        public const string ComVariantMarshaller = "System.Runtime.InteropServices.Marshalling.ComVariantMarshaller";
    }
}
