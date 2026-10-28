// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;
using SignatureTypeContext = Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers.SignatureTypeContext;
using SignatureTypeInfoProvider = Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers.SignatureTypeInfoProvider;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class SignatureTypeInfoProviderTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnresolvedTypeReferenceRetainsSignatureKind(MockTarget.Architecture arch)
    {
        ModuleHandle moduleHandle = new(0x1000);
        TypeReferenceHandle typeReference =
            (TypeReferenceHandle)MetadataTokens.Handle(0x01000001);

        Mock<ILoader> loader = new();
        TargetNUInt flags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(
                moduleHandle,
                ModuleLookupMapKind.TypeRefToMethodTable,
                (uint)MetadataTokens.GetToken(typeReference),
                out flags))
            .Returns(TargetPointer.Null);

        Mock<IRuntimeTypeSystem> rts = new();
        SignatureTypeInfoProvider provider = CreateProvider(arch, moduleHandle, loader, rts);

        SignatureTypeInfo result = provider.GetTypeFromReference(
            reader: default,
            typeReference,
            rawTypeKind: (byte)SignatureTypeKind.Class);

        Assert.Equal(CorElementType.Class, result.ElementType);
        Assert.Null(result.ExactTypeHandle);
        Assert.Null(result.GenericTypeDefinition);
        Assert.Empty(result.TypeArguments);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GenericInstantiationRetainsShapeWhenExactTypeIsUnavailable(MockTarget.Architecture arch)
    {
        ModuleHandle moduleHandle = new(0x1000);
        ITypeHandle genericDefinition = Mock.Of<ITypeHandle>();
        ITypeHandle typeArgument = Mock.Of<ITypeHandle>();
        SignatureTypeInfo argumentInfo = new(CorElementType.Class, typeArgument);

        Mock<ILoader> loader = new();
        Mock<IRuntimeTypeSystem> rts = new();
        rts.Setup(r => r.GetConstructedType(
                genericDefinition,
                CorElementType.GenericInst,
                0,
                It.IsAny<ImmutableArray<ITypeHandle?>>(),
                SignatureCallingConvention.Default))
            .Returns((ITypeHandle?)null);

        SignatureTypeInfoProvider provider = CreateProvider(arch, moduleHandle, loader, rts);

        SignatureTypeInfo result = provider.GetGenericInstantiation(
            new SignatureTypeInfo(CorElementType.ValueType, genericDefinition),
            [argumentInfo]);

        Assert.Equal(CorElementType.ValueType, result.ElementType);
        Assert.Null(result.ExactTypeHandle);
        Assert.Same(genericDefinition, result.GenericTypeDefinition);
        Assert.Equal(argumentInfo, Assert.Single(result.TypeArguments));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GenericTypeParameterUsesOwningTypeShape(MockTarget.Architecture arch)
    {
        ModuleHandle moduleHandle = new(0x1000);
        SignatureTypeInfo argumentInfo = new(CorElementType.Class, Mock.Of<ITypeHandle>());
        SignatureTypeContext context = new(
            Method: null,
            new SignatureTypeInfo(
                CorElementType.ValueType,
                exactTypeHandle: null,
                genericTypeDefinition: Mock.Of<ITypeHandle>(),
                [argumentInfo]));

        SignatureTypeInfoProvider provider = CreateProvider(
            arch,
            moduleHandle,
            new Mock<ILoader>(),
            new Mock<IRuntimeTypeSystem>());

        SignatureTypeInfo result = provider.GetGenericTypeParameter(context, index: 0);

        Assert.Equal(argumentInfo, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFieldTypeInfoUsesOwningTypeShape(MockTarget.Architecture arch)
    {
        TargetPointer fieldDesc = new(0x1000);
        TargetPointer enclosingMethodTable = new(0x2000);
        TargetPointer modulePointer = new(0x3000);
        ModuleHandle moduleHandle = new(0x4000);
        ITypeHandle enclosingType = Mock.Of<ITypeHandle>();
        SignatureTypeInfo argumentInfo = new(CorElementType.Class, exactTypeHandle: null);

        using MetadataReaderProvider metadataProvider = BuildMetadataWithGenericField();
        MetadataReader metadataReader = metadataProvider.GetMetadataReader();

        Mock<IRuntimeTypeSystem> rts = new();
        rts.Setup(r => r.GetMTOfEnclosingClass(fieldDesc)).Returns(enclosingMethodTable);
        rts.Setup(r => r.GetTypeHandle(enclosingMethodTable)).Returns(enclosingType);
        rts.Setup(r => r.GetModule(enclosingType)).Returns(modulePointer);
        rts.Setup(r => r.GetFieldDescMemberDef(fieldDesc)).Returns(0x04000001);

        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(modulePointer)).Returns(moduleHandle);

        Mock<IEcmaMetadata> ecmaMetadata = new();
        ecmaMetadata.Setup(e => e.GetMetadata(moduleHandle)).Returns(metadataReader);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(rts)
            .AddMockContract(loader)
            .AddMockContract(ecmaMetadata)
            .Build();
        CallingConvention_1 callingConvention = new(target);
        SignatureTypeInfo owningType = new(
            CorElementType.ValueType,
            exactTypeHandle: null,
            genericTypeDefinition: enclosingType,
            [argumentInfo]);

        SignatureTypeInfo result =
            callingConvention.GetFieldTypeInfo(fieldDesc, owningType);

        Assert.Equal(argumentInfo, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnavailableValueTypeLayoutIsIndeterminate(MockTarget.Architecture arch)
    {
        Mock<IRuntimeTypeSystem> rts = new();
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(rts)
            .Build();
        CallingConvention_1 callingConvention = new(target);
        CdacTypeHandle typeHandle = new(
            new SignatureTypeInfo(
                CorElementType.ValueType,
                exactTypeHandle: null,
                genericTypeDefinition: Mock.Of<ITypeHandle>()),
            target,
            callingConvention);

        Assert.True(typeHandle.HasIndeterminateSize());
        Assert.Throws<NotImplementedException>(() => typeHandle.GetSize());
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFieldAlignmentUsesRuntimeLayoutOrPointerSize(MockTarget.Architecture arch)
    {
        ITypeHandle exactTypeHandle = Mock.Of<ITypeHandle>();
        Mock<IRuntimeTypeSystem> rts = new();
        rts.Setup(r => r.GetClassAlignmentRequirement(exactTypeHandle)).Returns(16);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(rts)
            .Build();
        CallingConvention_1 callingConvention = new(target);

        CdacTypeHandle exact = new(
            new SignatureTypeInfo(CorElementType.ValueType, exactTypeHandle),
            target,
            callingConvention);
        CdacTypeHandle unresolved = new(
            new SignatureTypeInfo(CorElementType.ValueType, exactTypeHandle: null),
            target,
            callingConvention);

        Assert.Equal(16, exact.GetFieldAlignment());
        Assert.Equal(target.PointerSize, unresolved.GetFieldAlignment());
        rts.Verify(r => r.GetClassAlignmentRequirement(exactTypeHandle), Times.Once);
    }

    [Theory]
    [InlineData("c1", new byte[] { 0xc0, 0x0b })]
    [InlineData("c2", new byte[] { 0xc0, 0x33 })]
    public void VarArgMapPreservesTargetCookieEncoding(string stackWalkVersion, byte[] expected)
    {
        using MetadataReaderProvider metadataProvider = BuildMetadataWithGenericField();
        ModuleHandle moduleHandle = new(0x4000);
        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(new TargetPointer(0x3000))).Returns(moduleHandle);
        Mock<IEcmaMetadata> metadata = new();
        metadata.Setup(m => m.GetMetadata(moduleHandle)).Returns(metadataProvider.GetMetadataReader());
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.X86);
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(RuntimeInfoOperatingSystem.Windows);
        IRuntimeTypeSystem rts = new SignatureTestTypeSystem();
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false })
            .AddMockContract(loader)
            .AddMockContract(metadata)
            .AddMockContract(runtimeInfo)
            .AddMockContract(rts)
            .AddMockContract(Mock.Of<IExecutionManager>())
            .AddMockContract(Mock.Of<IGCInfo>())
            .AddContract<IStackWalk>(version: stackWalkVersion)
            .Build();

        Assert.True(new CallingConvention_1(target).TryComputeArgGCRefMapBlob(
            new MethodDescHandle(new TargetPointer(0x1000)), out byte[] blob));
        Assert.Equal(expected, blob);
        GCRefMapDecoder decoder = new(blob, legacyTokens: GCRefMapDecoder.UsesLegacyTokens(target));
        Assert.Equal(0u, decoder.ReadStackPop());
        Assert.Equal(GCRefMapToken.Skip, decoder.ReadToken());
        Assert.Equal(GCRefMapToken.Skip, decoder.ReadToken());
        Assert.Equal(GCRefMapToken.VASigCookie, decoder.ReadToken());
        Assert.True(decoder.AtEnd);
    }

    [Theory]
    [InlineData(false, true, new byte[] { 0x08 })]
    [InlineData(false, false, new byte[] { })]
    [InlineData(true, true, new byte[] { 0x22 })]
    [InlineData(true, false, new byte[] { })]
    public void NestedByRefLikeMapRequiresCompleteFieldLayout(
        bool inlineArray, bool nestedTypeAvailable, byte[] expected)
    {
        // ARM32 passes the struct by value. An unresolved nested Span-like field must
        // decline the map, not turn its interior pointer into a non-GC slot.
        using MetadataReaderProvider metadataProvider = BuildMetadataWithGenericField();
        ModuleHandle moduleHandle = new(0x4000);
        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(new TargetPointer(0x3000))).Returns(moduleHandle);
        TargetNUInt flags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(
                moduleHandle, ModuleLookupMapKind.TypeDefToMethodTable, 0x02000001, out flags))
            .Returns(new TargetPointer((ulong)CorElementType.ValueType));
        Mock<IEcmaMetadata> metadata = new();
        metadata.Setup(m => m.GetMetadata(moduleHandle)).Returns(metadataProvider.GetMetadataReader());
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.Arm);
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(RuntimeInfoOperatingSystem.Unix);
        IRuntimeTypeSystem rts = new SignatureTestTypeSystem(
            byRefLikeStruct: true, nestedTypeAvailable, inlineArray);
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(
            new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false })
            .AddMockContract(loader)
            .AddMockContract(metadata)
            .AddMockContract(runtimeInfo)
            .AddMockContract(rts)
            .Build();

        bool success = new CallingConvention_1(target).TryComputeArgGCRefMapBlob(
            new MethodDescHandle(new TargetPointer(0x1000)), out byte[] blob);

        Assert.Equal(nestedTypeAvailable, success);
        Assert.Equal(expected, blob);
        if (!nestedTypeAvailable)
        {
            Assert.Empty(new ByRefPointerOffsetsReporter(target).Find(
                rts.GetTypeHandle(new TargetPointer((ulong)CorElementType.ValueType))));
        }
    }

    private sealed class SignatureTestTypeSystem(
        bool byRefLikeStruct = false, bool nestedTypeAvailable = true, bool inlineArray = false) : IRuntimeTypeSystem
    {
        private sealed record TypeHandle(CorElementType ElementType, bool Nested = false) : ITypeHandle
        {
            public TargetPointer Address => new((ulong)ElementType + (Nested ? 0x100ul : 0));
        }

        private static readonly ITypeHandle s_nestedType = new TypeHandle(CorElementType.ValueType, Nested: true);

        public TargetPointer GetMethodTable(MethodDescHandle method) => new((ulong)CorElementType.Object);
        public ITypeHandle GetTypeHandle(TargetPointer address) => new TypeHandle((CorElementType)address.Value);
        public TargetPointer GetModule(ITypeHandle type) => new(0x3000);
        public ReadOnlySpan<ITypeHandle> GetInstantiation(ITypeHandle type) => [];
        public CorElementType GetSignatureCorElementType(ITypeHandle type) => ((TypeHandle)type).ElementType;
        public ITypeHandle GetPrimitiveType(CorElementType type) => new TypeHandle(type);
        public TargetPointer GetWellKnownMethodTable(WellKnownMethodTable type) => new((ulong)CorElementType.Object);
        public GenericContextLoc GetGenericContextLoc(MethodDescHandle method) => GenericContextLoc.None;
        public bool IsAsyncMethod(MethodDescHandle method) => false;
        public bool IsValueType(ITypeHandle type) => ((TypeHandle)type).ElementType == CorElementType.ValueType;
        public CorElementType GetInternalCorElementType(ITypeHandle type) => ((TypeHandle)type).ElementType;
        public uint GetBaseSize(ITypeHandle type) => 24;
        public bool RequiresAlign8(ITypeHandle type) => false;
        public bool TryGetHFAElementSize(ITypeHandle type, out int size)
        {
            size = 0;
            return false;
        }
        public bool IsByRefLike(ITypeHandle type) => IsValueType(type);
        public bool ContainsGCPointers(ITypeHandle type) => false;
        public bool IsInlineArray(ITypeHandle type) => inlineArray && type != s_nestedType;
        public uint GetNumInstanceFieldBytes(ITypeHandle type) => type == s_nestedType ? 8u : 16u;
        public IEnumerable<TargetPointer> GetFieldDescList(ITypeHandle type)
            => type == s_nestedType ? [new(0x6000)] : [new(0x5000)];
        public bool IsFieldDescStatic(TargetPointer field) => false;
        public CorElementType GetFieldDescType(TargetPointer field)
            => field.Value == 0x5000 ? CorElementType.ValueType : CorElementType.Byref;
        public uint GetFieldDescOffset(TargetPointer field, FieldDefinition? fieldDef)
            => field.Value == 0x5000 && !inlineArray ? 4u : 0u;
        public ITypeHandle? GetFieldDescApproxTypeHandle(TargetPointer field)
            => nestedTypeAvailable ? s_nestedType : null;

        public bool TryGetMethodSignature(MethodDescHandle method, out ReadOnlySpan<byte> signature)
        {
            signature = byRefLikeStruct
                ? [0x00, 0x01, 0x01, 0x11, 0x04] // static void (valuetype TypeDef1)
                : [0x05, 0x00, 0x01]; // static vararg void ()
            return true;
        }
    }

    private static SignatureTypeInfoProvider CreateProvider(
        MockTarget.Architecture arch,
        ModuleHandle moduleHandle,
        Mock<ILoader> loader,
        Mock<IRuntimeTypeSystem> rts)
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(loader)
            .AddMockContract(rts)
            .Build();
        return new SignatureTypeInfoProvider(target, moduleHandle);
    }

    private static MetadataReaderProvider BuildMetadataWithGenericField()
    {
        MetadataBuilder metadataBuilder = new();
        metadataBuilder.AddModule(
            generation: 0,
            metadataBuilder.GetOrAddString(nameof(SignatureTypeInfoProviderTests)),
            metadataBuilder.GetOrAddGuid(Guid.Empty),
            encId: default,
            encBaseId: default);

        BlobBuilder fieldSignature = new();
        new BlobEncoder(fieldSignature)
            .FieldSignature()
            .GenericTypeParameter(0);
        FieldDefinitionHandle fieldHandle = metadataBuilder.AddFieldDefinition(
            attributes: default,
            metadataBuilder.GetOrAddString("Field"),
            metadataBuilder.GetOrAddBlob(fieldSignature));
        metadataBuilder.AddTypeDefinition(
            attributes: default,
            @namespace: default,
            metadataBuilder.GetOrAddString("<Module>"),
            baseType: default,
            fieldHandle,
            MetadataTokens.MethodDefinitionHandle(1));

        BlobBuilder metadata = new();
        new MetadataRootBuilder(metadataBuilder).Serialize(metadata, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
        return MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(metadata.ToArray()));
    }
}
