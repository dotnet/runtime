// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal partial class CodeSnippets : IComInterfaceAttributeProvider
    {
        private readonly GeneratorKind _generator;
        GeneratorKind IComInterfaceAttributeProvider.Generator => _generator;

        private string VirtualMethodIndex(
            int index,
            bool? ImplicitThisParameter = null,
            MarshalDirection? Direction = null,
            StringMarshalling? StringMarshalling = null,
            Type? StringMarshallingCustomType = null,
            bool? SetLastError = null,
            ExceptionMarshalling? ExceptionMarshalling = null,
            Type? ExceptionMarshallingType = null)
            => ((IComInterfaceAttributeProvider)this).VirtualMethodIndex(
                index,
                ImplicitThisParameter,
                Direction,
                StringMarshalling,
                StringMarshallingCustomType,
                SetLastError,
                ExceptionMarshalling,
                ExceptionMarshallingType);

        private string UnmanagedObjectUnwrapper(Type t) => ((IComInterfaceAttributeProvider)this).UnmanagedObjectUnwrapper(t);

        private string GeneratedComInterface => ((IComInterfaceAttributeProvider)this).GeneratedComInterface;

        public CodeSnippets(GeneratorKind generator)
        {
            this._generator = generator;
        }

        private string UnmanagedCallConv(Type[]? CallConvs = null)
        {
            var arguments = CallConvs?.Length is 0 or null ? "" : "(CallConvs = new[] {" + string.Join(", ", CallConvs!.Select(t => $"typeof({t.FullName})")) + "})";
            return "[global::System.Runtime.InteropServices.UnmanagedCallConvAttribute"
                + arguments + "]";
        }

        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";
        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";
        public const string INativeAPI_IUnmanagedInterfaceTypeImpl = $$"""
            partial interface INativeAPI
            {
                {{INativeAPI_IUnmanagedInterfaceTypeMethodImpl}}
            }
            """;

        public const string INativeAPI_IUnmanagedInterfaceTypeMethodImpl = """
                static unsafe void* IUnmanagedInterfaceType.VirtualMethodTableManagedImplementation => null;
            """;

        public static string NativeInterfaceUsage() => @"
// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider, INativeAPI.Native
{
    public VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(System.Type type) => throw null;
}
";

        public string SpecifiedMethodIndexNoExplicitParameters => $@"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0)}
    void Method();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string SpecifiedMethodIndexNoExplicitParametersNoImplicitThis => $@"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0, ImplicitThisParameter: false)}
    void Method();

}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{

    {UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl) })}
    {VirtualMethodIndex(0)}
    void Method();
    {UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })}
    {VirtualMethodIndex(1)}
    void Method1();

    [SuppressGCTransition]
    {UnmanagedCallConv(CallConvs: new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })}
    {VirtualMethodIndex(2)}
    void Method2();

    [SuppressGCTransition]
    {UnmanagedCallConv()}
    {VirtualMethodIndex(3)}
    void Method3();

    [SuppressGCTransition]
    {VirtualMethodIndex(4)}
    void Method4();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicParametersAndModifiers(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0)}
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicParametersAndModifiersManagedToUnmanaged(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0, Direction: MarshalDirection.ManagedToUnmanaged)}
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;
        public string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).FullName!);
        public string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

[assembly:DisableRuntimeMarshalling]

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0)}
    {typeName} Method({typeName} value, in {typeName} inValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicParametersAndModifiersNoImplicitThis(string typeName) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0, ImplicitThisParameter: false)}
    {typeName} Method({typeName} value, in {typeName} inValue, ref {typeName} refValue, out {typeName} outValue);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicParametersAndModifiersNoImplicitThis<T>() => BasicParametersAndModifiersNoImplicitThis(typeof(T).FullName!);
        public string MarshalUsingCollectionCountInfoParametersAndModifiers<T>() => MarshalUsingCollectionCountInfoParametersAndModifiers(typeof(T).ToString());
        public string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0)}
    [return:MarshalUsing(ConstantElementCount=10)]
    {collectionType} Method(
        {collectionType} p,
        in {collectionType} pIn,
        int pRefSize,
        [MarshalUsing(CountElementName = ""pRefSize"")] ref {collectionType} pRef,
        [MarshalUsing(CountElementName = ""pOutSize"")] out {collectionType} pOut,
        out int pOutSize);
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicReturnTypeComExceptionHandling(string typeName, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0, ExceptionMarshalling : ExceptionMarshalling.Com)}
    {typeName} Method();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public string BasicReturnTypeCustomExceptionHandling(string typeName, string customExceptionType, string preDeclaration = "") => $@"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{preDeclaration}

{UnmanagedObjectUnwrapper(typeof(UnmanagedObjectUnwrapper.TestUnwrapper))}
{GeneratedComInterface}
partial interface INativeAPI : IUnmanagedInterfaceType
{{
    {VirtualMethodIndex(0, ExceptionMarshallingType : Type.GetType(customExceptionType))}
    {typeName} Method();
}}" + NativeInterfaceUsage() + INativeAPI_IUnmanagedInterfaceTypeImpl;

        public class ManagedToUnmanaged : IVirtualMethodIndexSignatureProvider<ManagedToUnmanaged>
        {
            public ManagedToUnmanaged(GeneratorKind generator) => Generator = generator;
            public static MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public static bool ImplicitThisParameter => true;

            public GeneratorKind Generator { get; }

            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
        public class ManagedToUnmanagedNoImplicitThis : IVirtualMethodIndexSignatureProvider<ManagedToUnmanagedNoImplicitThis>
        {
            public static MarshalDirection Direction => MarshalDirection.ManagedToUnmanaged;

            public static bool ImplicitThisParameter => false;

            public GeneratorKind Generator { get; }

            public ManagedToUnmanagedNoImplicitThis(GeneratorKind generator) => Generator = generator;

            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
        public class UnmanagedToManaged : IVirtualMethodIndexSignatureProvider<UnmanagedToManaged>
        {
            public static MarshalDirection Direction => MarshalDirection.UnmanagedToManaged;

            public static bool ImplicitThisParameter => true;

            public GeneratorKind Generator { get; }

            public UnmanagedToManaged(GeneratorKind generator) => Generator = generator;

            // Unmanaged-to-managed-only stubs don't provide implementations for the interface, so we don't want to try to use the generated nested interface
            // since it won't have managed implementations for the methods
            public static string NativeInterfaceUsage() => string.Empty;
        }
        public class Bidirectional : IVirtualMethodIndexSignatureProvider<Bidirectional>
        {
            public static MarshalDirection Direction => MarshalDirection.Bidirectional;

            public static bool ImplicitThisParameter => true;

            public GeneratorKind Generator { get; }

            public Bidirectional(GeneratorKind generator) => Generator = generator;

            public static string NativeInterfaceUsage() => CodeSnippets.NativeInterfaceUsage();
        }
    }
}
