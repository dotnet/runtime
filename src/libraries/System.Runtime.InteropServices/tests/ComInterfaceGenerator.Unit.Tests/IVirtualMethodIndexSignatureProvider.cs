// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal interface IVirtualMethodIndexSignatureProvider<TProvider> : ICustomMarshallingSignatureTestProvider
        where TProvider : IVirtualMethodIndexSignatureProvider<TProvider>
    {
        public static abstract MarshalDirection Direction { get; }
        public static abstract bool ImplicitThisParameter { get; }

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

        public static abstract string NativeInterfaceUsage();
        static string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiers(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiersNoRef(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    {typeName} Method({typeName} value, in {typeName} inValue, out {typeName} outValue);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        static string ICustomMarshallingSignatureTestProvider.BasicParameterByValue(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    void Method({typeName} value);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        static string ICustomMarshallingSignatureTestProvider.BasicParameterWithByRefModifier(string modifier, string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    void Method({modifier} {typeName} value);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.BasicReturnType(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    {typeName} Method();
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.MarshalUsingParametersAndModifiers(string typeName, string marshallerTypeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    [return: MarshalUsing(typeof({marshallerTypeName}))]
    {typeName} Method(
        [MarshalUsing(typeof({marshallerTypeName}))] {typeName} p,
        [MarshalUsing(typeof({marshallerTypeName}))] in {typeName} pIn,
        [MarshalUsing(typeof({marshallerTypeName}))] ref {typeName} pRef,
        [MarshalUsing(typeof({marshallerTypeName}))] out {typeName} pOut);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    [return:MarshalUsing(ConstantElementCount=10)]
    {collectionType} Method(
        {collectionType} p,
        in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize);
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionParametersAndModifiers(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    [return:MarshalUsing(typeof({marshallerType}), ConstantElementCount=10)]
    {collectionType} Method(
        [MarshalUsing(typeof({marshallerType}))] {collectionType} p,
        [MarshalUsing(typeof({marshallerType}))] in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize
        );
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnValueLength(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    int Method(
        [MarshalUsing(typeof({marshallerType}), CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out {collectionType} pOut
        );
}}" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        static string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionOutConstantLength(string collectionType, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    int Method(
        [MarshalUsing(ConstantElementCount = 10)] out {collectionType} pOut
        );
}}
" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnConstantLength(string collectionType, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
    [return:MarshalUsing(ConstantElementCount = 10)]
    {collectionType} Method();
}}
" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        static string ICustomMarshallingSignatureTestProvider.CustomElementMarshalling(string collectionType, string elementMarshaller, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

partial interface INativeAPI : IUnmanagedInterfaceType<INativeAPI>
{{
    [VirtualMethodIndex(0, ImplicitThisParameter = {TProvider.ImplicitThisParameter.ToString().ToLowerInvariant()}, Direction = MarshalDirection.{TProvider.Direction})]
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
" + TProvider.NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
    }
}
