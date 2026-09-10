// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class TypeHandleTests
{
    private const ulong ModuleAddress = 0x1000;

    public static IEnumerable<object[]> TypeDescNames()
    {
        foreach (object[] architecture in new MockTarget.StdArch())
        {
            yield return [.. architecture, CorElementType.Void, "System.Void"];
            yield return [.. architecture, CorElementType.I4, "System.Int32"];
            yield return [.. architecture, CorElementType.String, "System.String"];
            yield return [.. architecture, CorElementType.TypedByRef, "System.TypedReference"];
            yield return [.. architecture, CorElementType.I, "System.IntPtr"];
            yield return [.. architecture, CorElementType.Object, "System.Object"];
            yield return [.. architecture, CorElementType.ValueType, ""];
            yield return [.. architecture, CorElementType.FnPtr, "FNPTR"];
        }
    }

    public static IEnumerable<object[]> ModifierTypeDescNames()
    {
        foreach (object[] architecture in new MockTarget.StdArch())
        {
            yield return [.. architecture, CorElementType.Byref, "System.Int32&"];
            yield return [.. architecture, CorElementType.Ptr, "System.Int32*"];
            yield return [.. architecture, CorElementType.SzArray, "System.Int32[]"];
            yield return [.. architecture, CorElementType.Array, "System.Int32[]"];
        }
    }

    [Theory]
    [MemberData(nameof(TypeDescNames))]
    public void GetName_TypeDescMatchesNative(MockTarget.Architecture architecture, CorElementType kind, string expected)
    {
        TargetTypeHandle typeHandle = new(0x2002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDescs.Add(typeHandle);
        runtimeTypeSystem.ElementTypes[typeHandle] = kind;
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);

        Assert.Equal(expected, typeHandle.GetName(target));
    }

    [Theory]
    [MemberData(nameof(ModifierTypeDescNames))]
    public void GetName_ModifierTypeDescMatchesNative(MockTarget.Architecture architecture, CorElementType kind, string expected)
    {
        TargetTypeHandle typeHandle = new(0x2002);
        TargetTypeHandle elementType = new(0x3002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDescs.Add(typeHandle);
        runtimeTypeSystem.TypeDescs.Add(elementType);
        runtimeTypeSystem.ElementTypes[typeHandle] = kind;
        runtimeTypeSystem.ElementTypes[elementType] = CorElementType.I4;
        runtimeTypeSystem.TypeParameters[typeHandle] = elementType;
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);

        Assert.Equal(expected, typeHandle.GetName(target));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetName_GenericVariablesUseIndex(MockTarget.Architecture architecture)
    {
        TargetTypeHandle typeVariable = new(0x2002);
        TargetTypeHandle methodVariable = new(0x3002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDescs.Add(typeVariable);
        runtimeTypeSystem.TypeDescs.Add(methodVariable);
        runtimeTypeSystem.ElementTypes[typeVariable] = CorElementType.Var;
        runtimeTypeSystem.ElementTypes[methodVariable] = CorElementType.MVar;
        runtimeTypeSystem.GenericVariables[typeVariable] = (ModuleAddress, 0, 7);
        runtimeTypeSystem.GenericVariables[methodVariable] = (ModuleAddress, 0, 7);
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);

        Assert.Equal("!7", typeVariable.GetName(target));
        Assert.Equal("!!7", methodVariable.GetName(target));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetName_MethodTablesMatchNative(MockTarget.Architecture architecture)
    {
        MetadataBuilder metadataBuilder = CreateMetadataBuilder();
        TypeDefinitionHandle rawNameTypeDef = AddTypeDefinition(metadataBuilder, "Name,Space", "Type+Name");
        TypeDefinitionHandle containerTypeDef = AddTypeDefinition(metadataBuilder, "Tests", "Container`1");
        TypeDefinitionHandle int32TypeDef = AddTypeDefinition(metadataBuilder, "System", "Int32");
        TypeDefinitionHandle nestedTypeDef = AddTypeDefinition(metadataBuilder, "", "Inner", TypeAttributes.NestedPublic);
        TypeDefinitionHandle enclosingTypeDef = AddTypeDefinition(metadataBuilder, "Tests", "Outer");
        metadataBuilder.AddNestedType(nestedTypeDef, enclosingTypeDef);
        using MetadataReaderProvider provider = CreateMetadataReader(metadataBuilder, out MetadataReader reader);

        TargetTypeHandle rawNameType = new(0x2000);
        TargetTypeHandle containerType = new(0x3000);
        TargetTypeHandle int32Type = new(0x4000);
        TargetTypeHandle nestedType = new(0x5000);
        TargetTypeHandle arrayType = new(0x6000);
        TargetTypeHandle rankOneArrayType = new(0x7000);

        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDefTokens[rawNameType] = (uint)MetadataTokens.GetToken(rawNameTypeDef);
        runtimeTypeSystem.TypeDefTokens[containerType] = (uint)MetadataTokens.GetToken(containerTypeDef);
        runtimeTypeSystem.TypeDefTokens[int32Type] = (uint)MetadataTokens.GetToken(int32TypeDef);
        runtimeTypeSystem.TypeDefTokens[nestedType] = (uint)MetadataTokens.GetToken(nestedTypeDef);
        runtimeTypeSystem.TypeModules[rawNameType] = ModuleAddress;
        runtimeTypeSystem.TypeModules[containerType] = ModuleAddress;
        runtimeTypeSystem.TypeModules[int32Type] = ModuleAddress;
        runtimeTypeSystem.TypeModules[nestedType] = ModuleAddress;
        runtimeTypeSystem.Instantiations[containerType] = [int32Type];
        runtimeTypeSystem.ArrayRanks[arrayType] = 2;
        runtimeTypeSystem.ElementTypes[arrayType] = CorElementType.Array;
        runtimeTypeSystem.TypeParameters[arrayType] = int32Type;
        runtimeTypeSystem.ArrayRanks[rankOneArrayType] = 1;
        runtimeTypeSystem.ElementTypes[rankOneArrayType] = CorElementType.Array;
        runtimeTypeSystem.TypeParameters[rankOneArrayType] = int32Type;

        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem, reader);

        Assert.Equal("Name,Space.Type+Name", rawNameType.GetName(target));
        Assert.Equal("Tests.Container`1[System.Int32]", containerType.GetName(target));
        Assert.Equal("Inner", nestedType.GetName(target));
        Assert.Equal("System.Int32[,]", arrayType.GetName(target));
        Assert.Equal("System.Int32[*]", rankOneArrayType.GetName(target));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeInstance_GetName(MockTarget.Architecture architecture)
    {
        TargetTypeHandle typeHandle = new(0x2002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDescs.Add(typeHandle);
        runtimeTypeSystem.ElementTypes[typeHandle] = CorElementType.I4;
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);
        IXCLRDataTypeInstance typeInstance = new ClrDataTypeInstance(target, typeHandle, null, new());

        uint nameLen = 0;
        Assert.Equal(HResults.S_OK, typeInstance.GetName(0, 0, &nameLen, null));
        Assert.Equal(13u, nameLen);

        char[] nameBuffer = new char[nameLen];
        fixed (char* name = nameBuffer)
        {
            Assert.Equal(HResults.S_OK, typeInstance.GetName(0, nameLen, &nameLen, name));
            Assert.Equal("System.Int32", new string(name, 0, (int)nameLen - 1));

            Assert.Equal(CorDbgHResults.ERROR_INSUFFICIENT_BUFFER, typeInstance.GetName(0, 4, &nameLen, name));
            Assert.Equal("Sys", new string(name, 0, 3));
        }

        Assert.Equal(HResults.E_INVALIDARG, typeInstance.GetName(1, 0, null, null));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeDefinition_GetNameFollowsBufferProtocol(MockTarget.Architecture architecture)
    {
        TargetTypeHandle typeHandle = new(0x2002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeDescs.Add(typeHandle);
        runtimeTypeSystem.ElementTypes[typeHandle] = CorElementType.I4;
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);
        IXCLRDataTypeDefinition typeDefinition = new ClrDataTypeDefinition(target, ModuleAddress, 0x02000001, typeHandle, null, new());

        uint nameLen = 0;
        Assert.Equal(HResults.S_OK, typeDefinition.GetName(0, 0, &nameLen, null));
        Assert.Equal((uint)"System.Int32".Length + 1, nameLen);

        char[] nameBuffer = new char[nameLen];
        fixed (char* name = nameBuffer)
        {
            Assert.Equal(HResults.S_OK, typeDefinition.GetName(0, nameLen, &nameLen, name));
            Assert.Equal("System.Int32", new string(name));
        }

        char[] truncatedBuffer = new char[4];
        fixed (char* name = truncatedBuffer)
        {
            Assert.Equal(CorDbgHResults.ERROR_INSUFFICIENT_BUFFER, typeDefinition.GetName(0, (uint)truncatedBuffer.Length, &nameLen, name));
            Assert.Equal("Sys", new string(name));
        }
        Assert.Equal((uint)"System.Int32".Length + 1, nameLen);

        Assert.Equal(HResults.E_INVALIDARG, typeDefinition.GetName(1, 0, null, null));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeDefinition_GetNameUsesMetadataForNullTypeHandle(MockTarget.Architecture architecture)
    {
        MetadataBuilder metadataBuilder = CreateMetadataBuilder();
        TypeDefinitionHandle typeDef = AddTypeDefinition(metadataBuilder, "Tests", "MetadataOnly");
        using MetadataReaderProvider provider = CreateMetadataReader(metadataBuilder, out MetadataReader reader);
        TestPlaceholderTarget target = CreateTarget(architecture, new TestRuntimeTypeSystem(), reader);
        IXCLRDataTypeDefinition typeDefinition = new ClrDataTypeDefinition(
            target,
            ModuleAddress,
            (uint)MetadataTokens.GetToken(typeDef),
            null,
            null,
            new());

        char[] nameBuffer = new char[32];
        uint nameLen = 0;
        fixed (char* name = nameBuffer)
        {
            Assert.Equal(HResults.S_OK, typeDefinition.GetName(0, (uint)nameBuffer.Length, &nameLen, name));
            Assert.Equal("Tests.MetadataOnly", new string(name));
        }
        Assert.Equal((uint)"Tests.MetadataOnly".Length + 1, nameLen);

        uint elementType = 0;
        Assert.Equal(HResults.E_NOTIMPL, typeDefinition.GetCorElementType(&elementType));
        Assert.Equal(HResults.E_POINTER, typeDefinition.GetCorElementType(null));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeDefinition_GetCorElementTypeReturnsInternalType(MockTarget.Architecture architecture)
    {
        TargetTypeHandle typeHandle = new(0x2002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.ElementTypes[typeHandle] = CorElementType.I4;
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);
        IXCLRDataTypeDefinition typeDefinition = new ClrDataTypeDefinition(target, ModuleAddress, 0x02000001, typeHandle, null, new());

        uint elementType = 0;
        Assert.Equal(HResults.S_OK, typeDefinition.GetCorElementType(&elementType));
        Assert.Equal((uint)CorElementType.I4, elementType);
        Assert.Equal(HResults.E_POINTER, typeDefinition.GetCorElementType(null));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeDefinition_GetTokenAndScopeReturnsConstructorValues(MockTarget.Architecture architecture)
    {
        const uint Token = 0x02000001;

        TargetTypeHandle typeHandle = new(0x2002);
        TestRuntimeTypeSystem runtimeTypeSystem = new();
        TestPlaceholderTarget target = CreateTarget(architecture, runtimeTypeSystem);
        IXCLRDataTypeDefinition typeDefinition = new ClrDataTypeDefinition(target, ModuleAddress, Token, typeHandle, null, new());

        uint token = 0;
        DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, typeDefinition.GetTokenAndScope(&token, moduleOut));
        Assert.Equal(Token, token);
        Assert.Equal(new TargetPointer(ModuleAddress), Assert.IsType<ClrDataModule>(moduleOut.Interface).Address);

        Assert.Equal(
            HResults.S_OK,
            typeDefinition.GetTokenAndScope(null, new DacComNullableByRef<IXCLRDataModule>(isNullRef: true)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ClrDataTypeInstance_GetDefinition(MockTarget.Architecture architecture)
    {
        const uint TypeDefToken = 0x02000001;
        TargetPointer definitionTypeAddress = new(0x3000);
        TargetTypeHandle typeHandle = new(0x4000);
        TargetTypeHandle definitionType = new(definitionTypeAddress);
        Contracts.ModuleHandle moduleHandle = new(ModuleAddress);

        TestRuntimeTypeSystem runtimeTypeSystem = new();
        runtimeTypeSystem.TypeModules[typeHandle] = ModuleAddress;
        runtimeTypeSystem.TypeModules[definitionType] = ModuleAddress;
        runtimeTypeSystem.TypeDefTokens[typeHandle] = TypeDefToken;
        runtimeTypeSystem.TypeDefTokens[definitionType] = TypeDefToken;
        runtimeTypeSystem.TypeHandles[definitionTypeAddress] = definitionType;

        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(new TargetPointer(ModuleAddress))).Returns(moduleHandle);
        TargetNUInt lookupFlags = default;
        loader.Setup(l => l.GetModuleLookupMapElement(
                moduleHandle,
                ModuleLookupMapKind.TypeDefToMethodTable,
                TypeDefToken,
                out lookupFlags))
            .Returns(definitionTypeAddress);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(architecture)
            .AddMockContract<IRuntimeTypeSystem>(runtimeTypeSystem)
            .AddMockContract(loader)
            .Build();
        IXCLRDataTypeInstance typeInstance = new ClrDataTypeInstance(target, typeHandle, null, new());
        DacComNullableByRef<IXCLRDataTypeDefinition> typeDefinition = new(isNullRef: false);

        Assert.Equal(HResults.S_OK, typeInstance.GetDefinition(typeDefinition));
        Assert.IsType<ClrDataTypeDefinition>(typeDefinition.Interface);

        DacComNullableByRef<IXCLRDataTypeDefinition> nullTypeDefinition = new(isNullRef: true);
        Assert.Equal(HResults.E_POINTER, typeInstance.GetDefinition(nullTypeDefinition));

        loader.Setup(l => l.GetModuleLookupMapElement(
                moduleHandle,
                ModuleLookupMapKind.TypeDefToMethodTable,
                TypeDefToken,
                out lookupFlags))
            .Returns(TargetPointer.Null);
        DacComNullableByRef<IXCLRDataTypeDefinition> unloadedTypeDefinition = new(isNullRef: false);
        Assert.Equal(HResults.S_OK, typeInstance.GetDefinition(unloadedTypeDefinition));
        Assert.IsType<ClrDataTypeDefinition>(unloadedTypeDefinition.Interface);
    }

    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture architecture,
        TestRuntimeTypeSystem runtimeTypeSystem,
        MetadataReader? metadataReader = null)
    {
        TestPlaceholderTarget.Builder builder = new TestPlaceholderTarget.Builder(architecture)
            .AddMockContract<IRuntimeTypeSystem>(runtimeTypeSystem);

        if (metadataReader is MetadataReader reader)
        {
            Contracts.ModuleHandle module = new(ModuleAddress);
            Mock<ILoader> loader = new();
            loader.Setup(l => l.GetModuleHandleFromModulePtr(new TargetPointer(ModuleAddress))).Returns(module);
            Mock<IEcmaMetadata> ecmaMetadata = new();
            ecmaMetadata.Setup(e => e.GetMetadata(module)).Returns(reader);
            builder.AddMockContract(loader);
            builder.AddMockContract(ecmaMetadata);
        }

        return builder.Build();
    }

    private static MetadataBuilder CreateMetadataBuilder()
    {
        MetadataBuilder builder = new();
        builder.AddModule(0, builder.GetOrAddString("M"), builder.GetOrAddGuid(Guid.NewGuid()), default, default);
        builder.AddAssembly(builder.GetOrAddString("Asm"), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.Sha1);
        AddTypeDefinition(builder, "", "<Module>");
        return builder;
    }

    private static TypeDefinitionHandle AddTypeDefinition(
        MetadataBuilder builder,
        string typeNamespace,
        string name,
        TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class)
        => builder.AddTypeDefinition(
            attributes,
            builder.GetOrAddString(typeNamespace),
            builder.GetOrAddString(name),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1));

    private static MetadataReaderProvider CreateMetadataReader(MetadataBuilder metadataBuilder, out MetadataReader reader)
    {
        BlobBuilder blob = new();
        new MetadataRootBuilder(metadataBuilder).Serialize(blob, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
        MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
        reader = provider.GetMetadataReader();
        return provider;
    }

    private sealed class TestRuntimeTypeSystem : IRuntimeTypeSystem
    {
        public HashSet<ITypeHandle> TypeDescs { get; } = [];
        public Dictionary<ITypeHandle, CorElementType> ElementTypes { get; } = [];
        public Dictionary<ITypeHandle, ITypeHandle> TypeParameters { get; } = [];
        public Dictionary<ITypeHandle, uint> ArrayRanks { get; } = [];
        public Dictionary<ITypeHandle, uint> TypeDefTokens { get; } = [];
        public Dictionary<ITypeHandle, TargetPointer> TypeModules { get; } = [];
        public Dictionary<TargetPointer, ITypeHandle> TypeHandles { get; } = [];
        public Dictionary<ITypeHandle, ITypeHandle[]> Instantiations { get; } = [];
        public Dictionary<ITypeHandle, (TargetPointer Module, uint Token, uint Index)> GenericVariables { get; } = [];

        public bool IsTypeDesc(ITypeHandle typeHandle) => TypeDescs.Contains(typeHandle);

        public CorElementType GetInternalCorElementType(ITypeHandle typeHandle) => ElementTypes[typeHandle];

        public CorElementType GetSignatureCorElementType(ITypeHandle typeHandle) => ElementTypes[typeHandle];

        public ITypeHandle GetTypeParam(ITypeHandle typeHandle) => TypeParameters[typeHandle];

        public bool HasTypeParam(ITypeHandle typeHandle) => TypeParameters.ContainsKey(typeHandle);

        public bool IsArray(ITypeHandle typeHandle, out uint rank) => ArrayRanks.TryGetValue(typeHandle, out rank);

        public uint GetTypeDefToken(ITypeHandle typeHandle) => TypeDefTokens.GetValueOrDefault(typeHandle);

        public TargetPointer GetModule(ITypeHandle typeHandle) => TypeModules.GetValueOrDefault(typeHandle);

        public ITypeHandle GetTypeHandle(TargetPointer address) => TypeHandles[address];

        public ReadOnlySpan<ITypeHandle> GetInstantiation(ITypeHandle typeHandle)
            => Instantiations.TryGetValue(typeHandle, out ITypeHandle[]? instantiation) ? instantiation : [];

        public bool IsGenericTypeDefinition(ITypeHandle typeHandle) => false;

        public bool IsGenericVariable(ITypeHandle typeHandle, out TargetPointer module, out uint token, out uint index)
        {
            if (GenericVariables.TryGetValue(typeHandle, out (TargetPointer Module, uint Token, uint Index) genericVariable))
            {
                module = genericVariable.Module;
                token = genericVariable.Token;
                index = genericVariable.Index;
                return true;
            }

            module = TargetPointer.Null;
            token = 0;
            index = 0;
            return false;
        }

        public bool IsFunctionPointer(ITypeHandle typeHandle, out ReadOnlySpan<ITypeHandle> retAndArgTypes, out SignatureCallingConvention callConv)
        {
            retAndArgTypes = [];
            callConv = SignatureCallingConvention.Default;
            return false;
        }
    }
}
