// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class TypeInformationTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnresolvedTypeReferenceRetainsSignatureKind(MockTarget.Architecture arch)
    {
        ModuleHandle moduleHandle = new(0x1000);
        TargetPointer typeRefMap = new(0x2000);
        TypeReferenceHandle typeReference =
            (TypeReferenceHandle)MetadataTokens.Handle(0x01000001);

        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetLookupTables(moduleHandle))
            .Returns(new ModuleLookupTables { TypeRefToMethodTable = typeRefMap });
        TargetNUInt flags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(
                typeRefMap,
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
            .AddContract<ITypeInformation>("c1")
            .Build();
        SignatureTypeInfo owningType = new(
            CorElementType.ValueType,
            exactTypeHandle: null,
            genericTypeDefinition: enclosingType,
            [argumentInfo]);

        SignatureTypeInfo result =
            target.Contracts.TypeInformation.GetFieldTypeInfo(fieldDesc, owningType);

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
        CdacTypeHandle typeHandle = new(
            new SignatureTypeInfo(
                CorElementType.ValueType,
                exactTypeHandle: null,
                genericTypeDefinition: Mock.Of<ITypeHandle>()),
            target);

        Assert.True(typeHandle.HasIndeterminateSize());
        Assert.Throws<NotImplementedException>(() => typeHandle.GetSize());
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
            metadataBuilder.GetOrAddString("TypeInformationTests"),
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
