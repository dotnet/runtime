// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Interop.UnitTests;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal interface IVirtualMethodIndexSignatureProvider : ICustomMarshallingSignatureTestProvider
    {
        MarshalDirection Direction { get; }
        bool ImplicitThisParameter { get; }

        IComInterfaceAttributeProvider AttributeProvider { get; }

        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";
        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";

        string NativeInterfaceUsage();

        string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiers(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiersNoRef(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    {typeName} Method({typeName} value, in {typeName} inValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");

        string ICustomMarshallingSignatureTestProvider.BasicParameterByValue(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    void Method({typeName} value);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");

        string ICustomMarshallingSignatureTestProvider.BasicParameterWithByRefModifier(string modifier, string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    void Method({modifier} {typeName} value);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.BasicReturnType(string typeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    {typeName} Method();
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.MarshalUsingParametersAndModifiers(string typeName, string marshallerTypeName, string preDeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    [return: MarshalUsing(typeof({marshallerTypeName}))]
    {typeName} Method(
        [MarshalUsing(typeof({marshallerTypeName}))] {typeName} p,
        [MarshalUsing(typeof({marshallerTypeName}))] in {typeName} pIn,
        [MarshalUsing(typeof({marshallerTypeName}))] ref {typeName} pRef,
        [MarshalUsing(typeof({marshallerTypeName}))] out {typeName} pOut);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    [return:MarshalUsing(ConstantElementCount=10)]
    {collectionType} Method(
        {collectionType} p,
        in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize);
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionParametersAndModifiers(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    [return:MarshalUsing(typeof({marshallerType}), ConstantElementCount=10)]
    {collectionType} Method(
        [MarshalUsing(typeof({marshallerType}))] {collectionType} p,
        [MarshalUsing(typeof({marshallerType}))] in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(typeof({marshallerType}), CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize
        );
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnValueLength(string collectionType, string marshallerType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    int Method(
        [MarshalUsing(typeof({marshallerType}), CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out {collectionType} pOut
        );
}}" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionOutConstantLength(string collectionType, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    int Method(
        [MarshalUsing(ConstantElementCount = 10)] out {collectionType} pOut
        );
}}
" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnConstantLength(string collectionType, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
    [return:MarshalUsing(ConstantElementCount = 10)]
    {collectionType} Method();
}}
" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
        string ICustomMarshallingSignatureTestProvider.CustomElementMarshalling(string collectionType, string elementMarshaller, string predeclaration) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{predeclaration}

[assembly:DisableRuntimeMarshalling]

{AttributeProvider.UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{AttributeProvider.GeneratedComInterface}
partial interface INativeAPI
{{
    {AttributeProvider.VirtualMethodIndex(0, ImplicitThisParameter: ImplicitThisParameter, Direction: Direction)}
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
" + NativeInterfaceUsage() + AttributeProvider.AdditionalUserRequiredInterfaces("INativeAPI");
    }
}
