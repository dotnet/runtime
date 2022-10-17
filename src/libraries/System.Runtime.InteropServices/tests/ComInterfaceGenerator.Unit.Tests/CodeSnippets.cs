// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Interop.UnitTests;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal partial class CodeSnippets : ICustomMarshallingSignatureTestProvider
    {
        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";
        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";
        public const string INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl = $$"""
            partial interface INativeAPI
            {
                {{INativeAPI_NoCasting_IUnmanagedInterfaceTypeMethodImpl}}
            }
            """;

        public const string INativeAPI_NoCasting_IUnmanagedInterfaceTypeMethodImpl = """
                static unsafe void* IUnmanagedInterfaceType<INativeAPI, NoCasting>.VirtualMethodTableManagedImplementation => null;
                static unsafe void* IUnmanagedInterfaceType<INativeAPI, NoCasting>.GetUnmanagedWrapperForObject(INativeAPI obj) => null;
                static unsafe INativeAPI IUnmanagedInterfaceType<INativeAPI, NoCasting>.GetObjectForUnmanagedWrapper(void* ptr) => null;
                static NoCasting IUnmanagedInterfaceType<INativeAPI, NoCasting>.TypeKey => default;
            """;

        public static string NativeInterfaceUsage() => @"
// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider<NoCasting>, INativeAPI.Native
{
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(NoCasting typeKey) => throw null;
}
";

        public static readonly string SpecifiedMethodIndexNoExplicitParameters = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{
    [VirtualMethodIndex(0)]
    void Method();
}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;

        public static readonly string SpecifiedMethodIndexNoExplicitParametersNoImplicitThis = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    void Method();
}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;

        public static readonly string SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
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
}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiers(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).FullName!);
        public static string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    {typeName} Method({typeName} value, in {typeName} inValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string BasicParametersAndModifiersNoImplicitThis(string typeName) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;

        public static string BasicParametersAndModifiersNoImplicitThis<T>() => BasicParametersAndModifiersNoImplicitThis(typeof(T).FullName!);

        public static string BasicParameterByValue(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    void Method({typeName} value);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string BasicParameterWithByRefModifier(string modifier, string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    void Method({modifier} {typeName} value);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string BasicReturnType(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    {typeName} Method();
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string MarshalUsingParametersAndModifiers(string typeName, string marshallerTypeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    [return: MarshalUsing(typeof({marshallerTypeName}))]
    {typeName} Method(
        [MarshalUsing(typeof({marshallerTypeName}))] {typeName} p,
        [MarshalUsing(typeof({marshallerTypeName}))] in {typeName} pIn,
        [MarshalUsing(typeof({marshallerTypeName}))] ref {typeName} pRef,
        [MarshalUsing(typeof({marshallerTypeName}))] out {typeName} pOut);
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string MarshalUsingCollectionCountInfoParametersAndModifiers<T>() => MarshalUsingCollectionCountInfoParametersAndModifiers(typeof(T).ToString());
        public static string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
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
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string MarshalUsingCollectionParametersAndModifiers(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    [return:MarshalUsing(typeof({marshallerType}), ConstantElementCount=10)]
    {collectionType} Method(
        [MarshalUsing(typeof({marshallerType}))] {collectionType} p,
        [MarshalUsing(typeof({marshallerType}))] in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize
        );
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;
        public static string MarshalUsingCollectionReturnValueLength(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    int Method(
        [MarshalUsing(typeof({marshallerType}), CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out {collectionType} pOut
        );
}}" + NativeInterfaceUsage() + INativeAPI_NoCasting_IUnmanagedInterfaceTypeImpl;

        public static string MarshalUsingCollectionOutConstantLength(string collectionType, string predeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    int Method(
        [MarshalUsing(ConstantElementCount = 10)] out {collectionType} pOut
        );
}}
";
        public static string MarshalUsingCollectionReturnConstantLength(string collectionType, string predeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    [return:MarshalUsing(ConstantElementCount = 10)]
    {collectionType} Method();
}}
";
        public static string CustomElementMarshalling(string collectionType, string elementMarshaller, string predeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

readonly record struct NoCasting {{}}
partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI, NoCasting>
{{
    [VirtualMethodIndex(0)]
    [return:MarshalUsing(ConstantElementCount=10)]
    [return:MarshalUsing(typeof({elementMarshaller}), ElementIndirectionDepth = 1)]
    TestCollection<int> Method(
        [MarshalUsing(typeof({elementMarshaller}), ElementIndirectionDepth = 1)] {collectionType} p,
        [MarshalUsing(typeof({elementMarshaller}), ElementIndirectionDepth = 1)] in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(CountElementName = ""pRefSize""), MarshalUsing(typeof({elementMarshaller}), ElementIndirectionDepth = 1)] ref {collectionType} pRef,
        [MarshalUsing(CountElementName = ""pOutSize"")][MarshalUsing(typeof({elementMarshaller}), ElementIndirectionDepth = 1)] out {collectionType} pOut,
        out int pOutSize
        );
}}
";
    }
}
