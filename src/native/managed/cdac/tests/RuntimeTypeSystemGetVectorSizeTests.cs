// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class RuntimeTypeSystemGetVectorSizeTests
{
    private static readonly Lazy<SyntheticVectorMetadata> s_syntheticMetadata = new(SyntheticVectorMetadata.Create);

    [Theory]
    [InlineData("Vector64`1", 8)]
    [InlineData("Vector128`1", 16)]
    public void GetVectorSize_KnownIntrinsic_ReturnsSize(string typeName, int expectedSize)
    {
        SyntheticVectorMetadata metadata = s_syntheticMetadata.Value;
        (Target target, TypeHandle handle) = CreateTarget(
            metadata,
            rts => MockDescriptors.CallingConvention.AddVectorMethodTable(
                rts,
                typeName,
                expectedSize,
                metadata.GetTypeDefToken(typeName)));

        Assert.Equal(expectedSize, target.Contracts.RuntimeTypeSystem.GetVectorSize(handle));
    }

    [Fact]
    public void GetVectorSize_NotIntrinsicType_ReturnsZero()
    {
        (Target target, TypeHandle handle) = CreateTarget(
            s_syntheticMetadata.Value,
            rts => MockDescriptors.CallingConvention.AddValueTypeMethodTable(rts, "Vector128`1", structSize: 16, fields: []));

        Assert.Equal(0, target.Contracts.RuntimeTypeSystem.GetVectorSize(handle));
    }

    [Fact]
    public void GetVectorSize_NoMetadata_ReturnsZero()
    {
        SyntheticVectorMetadata metadata = s_syntheticMetadata.Value;
        (Target target, TypeHandle handle) = CreateTarget(
            syntheticMetadata: null,
            rts => MockDescriptors.CallingConvention.AddVectorMethodTable(
                rts,
                "Vector128`1",
                16,
                metadata.GetTypeDefToken("Vector128`1")));

        Assert.Equal(0, target.Contracts.RuntimeTypeSystem.GetVectorSize(handle));
    }

    [Fact]
    public void GetVectorSize_UnhandledIntrinsicName_ReturnsZero()
    {
        // Use Vector128 metadata/token but give it a name the runtime doesn't
        // recognize. The real Vector128 token resolves to the correct TypeDef row
        // whose name IS recognized, so we need a type name the runtime won't match.
        // The simplest approach: build a one-off metadata image with a fake type.
        SyntheticVectorMetadata fakeMetadata = SyntheticVectorMetadata.CreateWithExtraType(
            "System.Runtime.Intrinsics", "FakeVector`1");
        (Target target, TypeHandle handle) = CreateTarget(
            fakeMetadata,
            rts => MockDescriptors.CallingConvention.AddVectorMethodTable(
                rts,
                "FakeVector`1",
                32,
                fakeMetadata.GetTypeDefToken("FakeVector`1")));

        Assert.Equal(0, target.Contracts.RuntimeTypeSystem.GetVectorSize(handle));
    }

    [Fact]
    public void GetVectorSize_SystemNumericsVector_ReturnsFieldBytes()
    {
        SyntheticVectorMetadata metadata = s_syntheticMetadata.Value;
        (Target target, TypeHandle handle) = CreateTarget(
            metadata,
            rts => MockDescriptors.CallingConvention.AddVectorMethodTable(
                rts,
                "Vector`1",
                32,
                metadata.GetTypeDefToken("Vector`1")));

        Assert.Equal(32, target.Contracts.RuntimeTypeSystem.GetVectorSize(handle));
    }

    private static (Target Target, TypeHandle Handle) CreateTarget(
        SyntheticVectorMetadata? syntheticMetadata,
        Func<MockDescriptors.RuntimeTypeSystem, MockMethodTable> addMethodTable)
    {
        ulong methodTableAddress = 0;
        (Target target, _) = CallingConventionTestHelpers.CreateTargetWithMethod(
            CallConvCases.AMD64Windows,
            hasThis: false,
            (rts, sig) =>
            {
                MockMethodTable methodTable = addMethodTable(rts);
                methodTableAddress = methodTable.Address;
                sig.Return(CorElementType.Void);
            },
            syntheticMetadata: syntheticMetadata);

        return (target, target.Contracts.RuntimeTypeSystem.GetTypeHandle(new TargetPointer(methodTableAddress)));
    }
}
