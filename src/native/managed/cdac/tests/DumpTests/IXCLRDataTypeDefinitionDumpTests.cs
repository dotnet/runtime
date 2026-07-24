// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.TestInfrastructure.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

public unsafe class IXCLRDataTypeDefinitionDumpTests : DumpTestBase
{
    private const int HResultErrorInsufficientBuffer = unchecked((int)0x8007007A);

    protected override string DebuggeeName => "TypeHierarchy";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_ReturnsExpectedName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32");

        uint nameLen;
        int hr = typeDefinition.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);
        Assert.Equal(checked((uint)("System.Int32".Length + 1)), nameLen);

        char[] nameBuffer = new char[nameLen];
        uint actualNameLen;
        fixed (char* name = nameBuffer)
        {
            hr = typeDefinition.GetName(0, nameLen, &actualNameLen, name);
        }

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal(nameLen, actualNameLen);
        Assert.Equal("System.Int32", new string(nameBuffer, 0, (int)actualNameLen - 1));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_WithoutTypeHandle_ReturnsMetadataName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32", includeTypeHandle: false);

        uint nameLen;
        int hr = typeDefinition.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);

        char[] nameBuffer = new char[nameLen];
        fixed (char* name = nameBuffer)
        {
            hr = typeDefinition.GetName(0, nameLen, null, name);
        }

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal("System.Int32", new string(nameBuffer, 0, (int)nameLen - 1));
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_TruncatedBuffer_ReturnsInsufficientBuffer(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32");

        uint nameLen;
        int hr = typeDefinition.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);

        uint bufferLength = nameLen - 1;
        char[] nameBuffer = new char[bufferLength];
        uint actualNameLen;
        fixed (char* name = nameBuffer)
        {
            hr = typeDefinition.GetName(0, bufferLength, &actualNameLen, name);
        }

        AssertHResult(HResultErrorInsufficientBuffer, hr);
        Assert.Equal(nameLen, actualNameLen);
        Assert.Equal('\0', nameBuffer[^1]);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_InvalidFlags_ReturnsInvalidArgument(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32");

        int hr = typeDefinition.GetName(1, 0, null, null);

        AssertHResult(HResults.E_INVALIDARG, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetTokenAndScope_ReturnsExpectedValues(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, expectedModule, expectedToken) =
            CreateTypeDefinition("System.Int32");
        DacComNullableByRef<IXCLRDataModule> moduleOut = new(isNullRef: false);

        uint token;
        int hr = typeDefinition.GetTokenAndScope(&token, moduleOut);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal(expectedToken, token);
        ClrDataModule module = Assert.IsType<ClrDataModule>(moduleOut.Interface);
        Assert.Equal(expectedModule, module.Address);

        hr = typeDefinition.GetTokenAndScope(null, new DacComNullableByRef<IXCLRDataModule>(isNullRef: true));
        AssertHResult(HResults.S_OK, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetCorElementType_ReturnsExpectedType(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32");

        uint elementType;
        int hr = typeDefinition.GetCorElementType(&elementType);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal((uint)CorElementType.I4, elementType);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetCorElementType_InvalidStateReturnsError(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var (typeDefinition, _, _) = CreateTypeDefinition("System.Int32");
        var (metadataTypeDefinition, _, _) =
            CreateTypeDefinition("System.Int32", includeTypeHandle: false);

        int hr = typeDefinition.GetCorElementType(null);
        AssertHResult(HResults.E_INVALIDARG, hr);

        uint elementType;
        hr = metadataTypeDefinition.GetCorElementType(&elementType);
        AssertHResult(HResults.E_NOTIMPL, hr);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    public void GetName_TypeDescRoot(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ITypeHandle int32TypeHandle = Target.Contracts.ManagedTypeSource.GetTypeHandle("System.Int32");
        ITypeHandle? pointerTypeHandle = rts.GetConstructedType(
            int32TypeHandle,
            CorElementType.Ptr,
            rank: 0,
            ImmutableArray<ITypeHandle?>.Empty,
            default);
        Assert.NotNull(pointerTypeHandle);
        IXCLRDataTypeDefinition definition = new ClrDataTypeDefinition(
            Target,
            rts.GetModule(int32TypeHandle),
            pointerTypeHandle,
            rts.GetTypeDefToken(int32TypeHandle),
            legacyImpl: null);

        uint nameLen;
        int hr = definition.GetName(0, 0, &nameLen, null);
        AssertHResult(HResults.S_OK, hr);
        char* name = stackalloc char[checked((int)nameLen)];
        hr = definition.GetName(0, nameLen, &nameLen, name);

        AssertHResult(HResults.S_OK, hr);
        Assert.Equal("System.Int32*", new string(name));
    }

    private (IXCLRDataTypeDefinition Definition, TargetPointer Module, uint Token) CreateTypeDefinition(
        string typeName,
        bool includeTypeHandle = true)
    {
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;
        ITypeHandle typeHandle = Target.Contracts.ManagedTypeSource.GetTypeHandle(typeName);
        TargetPointer module = rts.GetModule(typeHandle);
        uint token = rts.GetTypeDefToken(typeHandle);
        var definition = new ClrDataTypeDefinition(
            Target,
            module,
            includeTypeHandle ? typeHandle : null,
            token,
            legacyImpl: null);

        return (definition, module, token);
    }
}
