// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal partial class CodeSnippets
    {
        private readonly IComInterfaceAttributeProvider _attributeProvider;
        public CodeSnippets(IComInterfaceAttributeProvider attributeProvider)
        {
            _attributeProvider = attributeProvider;
        }

        private string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null)
            => _attributeProvider.VirtualMethodIndex(
                index,
                ImplicitThisParameter,
                Direction,
                StringMarshalling,
                StringMarshallingCustomType,
                SetLastError,
                ExceptionMarshalling,
                ExceptionMarshallingType);

        private string UnmanagedObjectUnwrapper(Type t) => _attributeProvider.UnmanagedObjectUnwrapper(t);

        private string GeneratedComInterface => _attributeProvider.GeneratedComInterface;

        private string UnmanagedCallConv(Type[]? CallConvs = null)
        {
            var arguments = CallConvs?.Length is 0 or null ? "" : "(CallConvs = new[] {" + string.Join(", ", CallConvs!.Select(t => $"typeof({t.FullName})")) + "})";
            return "[global::System.Runtime.InteropServices.UnmanagedCallConvAttribute"
                + arguments + "]";
        }

        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";
        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";

        public string SpecifiedMethodIndexNoExplicitParameters => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0)}}
                void Method();
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string SpecifiedMethodIndexNoExplicitParametersNoImplicitThis => $$"""
            
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0, ImplicitThisParameter: false)}}
                void Method();
            
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
            
                {{UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl) })}}
                {{VirtualMethodIndex(0)}}
                void Method();
                {{UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })}}
                {{VirtualMethodIndex(1)}}
                void Method1();
            
                [SuppressGCTransition]
                {{UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })}}
                {{VirtualMethodIndex(2)}}
                void Method2();
            
                [SuppressGCTransition]
                {{UnmanagedCallConv()}}
                {{VirtualMethodIndex(3)}}
                void Method3();
            
                [SuppressGCTransition]
                {{VirtualMethodIndex(4)}}
                void Method4();
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;
        public string BasicParametersAndModifiers(string typeName, string methodModifiers = "", string preDeclaration = "") => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            
            [assembly:DisableRuntimeMarshalling]
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0)}}
                {{methodModifiers}} {{typeName}} Method({{typeName}} value, in {{typeName}} inValue, ref {{typeName}} refValue, out {{typeName}} outValue);
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string BasicParametersAndModifiersManagedToUnmanaged(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            
            [assembly:DisableRuntimeMarshalling]
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0, Direction: MarshalDirection.ManagedToUnmanaged)}}
                {{typeName}} Method({{typeName}} value, in {{typeName}} inValue, ref {{typeName}} refValue, out {{typeName}} outValue);
            }
            
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;
        public string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).FullName!);
        public string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            
            [assembly:DisableRuntimeMarshalling]
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0)}}
                {{typeName}} Method({{typeName}} value, in {{typeName}} inValue, out {{typeName}} outValue);
            }
            
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string BasicParametersAndModifiersNoImplicitThis(string typeName) => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0, ImplicitThisParameter: false)}}
                {{typeName}} Method({{typeName}} value, in {{typeName}} inValue, ref {{typeName}} refValue, out {{typeName}} outValue);
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string BasicParametersAndModifiersNoImplicitThis<T>() => BasicParametersAndModifiersNoImplicitThis(typeof(T).FullName!);
        public string MarshalUsingCollectionCountInfoParametersAndModifiers<T>() => MarshalUsingCollectionCountInfoParametersAndModifiers(typeof(T).ToString());
        public string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            [assembly:DisableRuntimeMarshalling]
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0)}}
                [return:MarshalUsing(ConstantElementCount=10)]
                {{collectionType}} Method(
                    [MarshalUsing(CountElementName = "pSize")] {{collectionType}} p,
                    int pSize,
                    [MarshalUsing(CountElementName = "pInSize")] in {{collectionType}} pIn,
                    in int pInSize,
                    int pRefSize,
                    [MarshalUsing(CountElementName = "pRefSize")] ref {{collectionType}} pRef,
                    [MarshalUsing(CountElementName = "pOutSize")] out {{collectionType}} pOut,
                    out int pOutSize);
            }
            
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string BasicReturnTypeComExceptionHandling(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0, ExceptionMarshalling: ExceptionMarshalling.Com)}}
                {{typeName}} Method();
            }
            
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string BasicReturnTypeCustomExceptionHandling(string typeName, string customExceptionType, string preDeclaration = "") => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            
            {{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}}
            {{GeneratedComInterface}}
            partial interface INativeAPI
            {
                {{VirtualMethodIndex(0, ExceptionMarshallingType: Type.GetType(customExceptionType))}}
                {{typeName}} Method();
            }
            {{_attributeProvider.AdditionalUserRequiredInterfaces("INativeAPI")}}
            """;

        public string DerivedComInterfaceType => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{GeneratedComInterface}}
            partial interface IComInterface
            {
                void Method();
            }
            {{GeneratedComInterface}}
            partial interface IComInterface2 : IComInterface
            {
                void Method2();
            }
            """;
        public string DerivedComInterfaceTypeMultipleComInterfaceBases => $$"""
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            
            {{GeneratedComInterface}}
            partial interface IComInterface
            {
                void Method();
            }
            {{GeneratedComInterface}}
            partial interface IOtherComInterface
            {
                void MethodA();
            }
            {{GeneratedComInterface}}
            partial interface IComInterface2 : IComInterface, IOtherComInterface
            {
                void Method2();
            }
            """;
        public class ManagedToUnmanaged : IVirtualMethodIndexSignatureProvider
        {
            public MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public bool ImplicitThisParameter => true;

            public ManagedToUnmanaged(IComInterfaceAttributeProvider attributeProvider)
            {
                AttributeProvider = attributeProvider;
            }

            public IComInterfaceAttributeProvider AttributeProvider { get; }
        }
        public class ManagedToUnmanagedNoImplicitThis : IVirtualMethodIndexSignatureProvider
        {
            public MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public bool ImplicitThisParameter => false;

            public ManagedToUnmanagedNoImplicitThis(IComInterfaceAttributeProvider attributeProvider)
            {
                AttributeProvider = attributeProvider;
            }

            public IComInterfaceAttributeProvider AttributeProvider { get; }
        }
        public class UnmanagedToManaged : IVirtualMethodIndexSignatureProvider
        {
            public MarshalDirection Direction => MarshalDirection.UnmanagedToManaged;

            public bool ImplicitThisParameter => true;

            public UnmanagedToManaged(IComInterfaceAttributeProvider attributeProvider)
            {
                AttributeProvider = attributeProvider;
            }

            public IComInterfaceAttributeProvider AttributeProvider { get; }
        }
        public class Bidirectional : IVirtualMethodIndexSignatureProvider
        {
            public MarshalDirection Direction => MarshalDirection.Bidirectional;

            public bool ImplicitThisParameter => true;

            public Bidirectional(IComInterfaceAttributeProvider attributeProvider)
            {
                AttributeProvider = attributeProvider;
            }

            public IComInterfaceAttributeProvider AttributeProvider { get; }
        }
    }
}
