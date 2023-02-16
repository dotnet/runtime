// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Interop;
using Microsoft.Interop.UnitTests;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal partial class CodeSnippets
    {
        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";
        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";
        public const string INativeAPI_IUnmanagedInterfaceTypeImpl = $$"""
            partial interface INativeAPI
            {
                {{INativeAPI_IUnmanagedInterfaceTypeMethodImpl}}
            }
            """;

        public const string INativeAPI_IUnmanagedInterfaceTypeMethodImpl = """
                static int IUnmanagedInterfaceType<INativeAPI>.VirtualMethodTableLength => 1;
                static unsafe void* IUnmanagedInterfaceType<INativeAPI>.VirtualMethodTableManagedImplementation => null;
                static unsafe void* IUnmanagedInterfaceType<INativeAPI>.GetUnmanagedWrapperForObject(INativeAPI obj) => null;
                static unsafe INativeAPI IUnmanagedInterfaceType<INativeAPI>.GetObjectForUnmanagedWrapper(void* ptr) => null;
            """;

        public static string NativeInterfaceUsage() => @"
// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider, INativeAPI.Native
{
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(System.Type type) => throw null;
}
";

        public static readonly string SpecifiedMethodIndexNoExplicitParameters = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{
    [VirtualMethodIndex(0)]
    void Method();
}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public static readonly string SpecifiedMethodIndexNoExplicitParametersNoImplicitThis = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    void Method();
}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public static readonly string SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{

    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    [VirtualMethodIndex(0)]
    void Method();
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(1)]
    void Method1();

    [SuppressGCTransition]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(2)]
    void Method2();

    [SuppressGCTransition]
    [UnmanagedCallConv]
    [VirtualMethodIndex(3)]
    void Method3();

    [SuppressGCTransition]
    [VirtualMethodIndex(4)]
    void Method4();
}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiers(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0)]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiersManagedToUnmanaged(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, Direction = MarshalDirection.ManagedToUnmanaged)]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).FullName!);
        public static string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0)]
    {typeName} Method({typeName} value, in {typeName} inValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiersNoImplicitThis(string typeName) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public static string BasicParametersAndModifiersNoImplicitThis<T>() => BasicParametersAndModifiersNoImplicitThis(typeof(T).FullName!);
        public static string MarshalUsingCollectionCountInfoParametersAndModifiers<T>() => MarshalUsingCollectionCountInfoParametersAndModifiers(typeof(T).ToString());
        public static string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0)]
    [return:MarshalUsing(ConstantElementCount=10)]
    {collectionType} Method(
        {collectionType} p,
        in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public static string BasicReturnTypeComExceptionHandling(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ExceptionMarshalling = ExceptionMarshalling.Com)]
    {typeName} Method();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public static string BasicReturnTypeCustomExceptionHandling(string typeName, string customExceptionType, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, CustomExceptionMarshallingType = typeof({customExceptionType}))]
    {typeName} Method();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public class ManagedToUnmanaged : IVirtualMethodIndexSignatureProvider<ManagedToUnmanaged>
        {
            public static MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public static bool ImplicitThisParameter => true;

            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
        public class ManagedToUnmanagedNoImplicitThis : IVirtualMethodIndexSignatureProvider<ManagedToUnmanagedNoImplicitThis>
        {
            public static MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public static bool ImplicitThisParameter => false;

            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
        public class UnmanagedToManaged : IVirtualMethodIndexSignatureProvider<UnmanagedToManaged>
        {
            public static MarshalDirection Direction => MarshalDirection.UnmanagedToManaged;

            public static bool ImplicitThisParameter => true;

            // Unmanaged-to-managed-only stubs don't provide implementations for the interface, so we don't want to try to use the generated nested interface
            // since it won't have managed implementations for the methods
            public static string NativeInterfaceUsage() => string.Empty;
        }
        public class Bidirectional : IVirtualMethodIndexSignatureProvider<Bidirectional>
        {
            public static MarshalDirection Direction => MarshalDirection.Bidirectional;

            public static bool ImplicitThisParameter => true;
            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
    }
}
