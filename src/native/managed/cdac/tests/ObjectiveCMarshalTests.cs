// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

using MockObject = Microsoft.Diagnostics.DataContractReader.Tests.MockDescriptors.Object;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ObjectiveCMarshalTests
{
    private static void ObjectiveCMarshalContractHelper(MockTarget.Architecture arch, Action<MockObject> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);
        MockObject objectBuilder = new(rtsBuilder);

        configure?.Invoke(objectBuilder);

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, objectBuilder.Types, objectBuilder.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.ObjectiveCMarshal == ((IContractFactory<IObjectiveCMarshal>)new ObjectiveCMarshalFactory()).CreateContract(target, 1)
                && c.SyncBlock == ((IContractFactory<ISyncBlock>)new SyncBlockFactory()).CreateContract(target, 1)));

        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetTaggedMemory_NoSyncBlockIndex_ReturnsFalse(MockTarget.Architecture arch)
    {
        TargetPointer testObjectAddress = default;
        ObjectiveCMarshalContractHelper(arch,
            (objectBuilder) =>
            {
                uint objectHeaderSize = objectBuilder.Types[DataType.ObjectHeader].Size!.Value;
                testObjectAddress = objectBuilder.AddObject(0, prefixSize: objectHeaderSize);
            },
            (target) =>
            {
                IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
                bool result = contract.TryGetTaggedMemory(testObjectAddress, out TargetNUInt size, out TargetPointer taggedMemory);

                Assert.False(result);
                Assert.Equal(default, size);
                Assert.Equal(TargetPointer.Null, taggedMemory);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetTaggedMemory_NullTaggedMemory_ReturnsFalse(MockTarget.Architecture arch)
    {
        TargetPointer testObjectAddress = default;
        ObjectiveCMarshalContractHelper(arch,
            (objectBuilder) =>
            {
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(
                    0, syncBlockIndex: 0,
                    rcw: TargetPointer.Null, ccw: TargetPointer.Null, ccf: TargetPointer.Null,
                    taggedMemory: TargetPointer.Null);
            },
            (target) =>
            {
                IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
                bool result = contract.TryGetTaggedMemory(testObjectAddress, out TargetNUInt size, out TargetPointer taggedMemory);

                Assert.False(result);
                Assert.Equal(default, size);
                Assert.Equal(TargetPointer.Null, taggedMemory);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetTaggedMemory_HasTaggedMemory_ReturnsTrueWithCorrectValues(MockTarget.Architecture arch)
    {
        TargetPointer testObjectAddress = default;
        TargetPointer expectedTaggedMemory = new TargetPointer(0x5000);
        ObjectiveCMarshalContractHelper(arch,
            (objectBuilder) =>
            {
                testObjectAddress = objectBuilder.AddObjectWithSyncBlock(
                    0, syncBlockIndex: 0,
                    rcw: TargetPointer.Null, ccw: TargetPointer.Null, ccf: TargetPointer.Null,
                    taggedMemory: expectedTaggedMemory);
            },
            (target) =>
            {
                IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;
                bool result = contract.TryGetTaggedMemory(testObjectAddress, out TargetNUInt size, out TargetPointer taggedMemory);

                Assert.True(result);
                Assert.Equal(expectedTaggedMemory, taggedMemory);
                Assert.Equal(2 * (ulong)target.PointerSize, size.Value);
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetTaggedMemory_MultipleObjects_IndependentResults(MockTarget.Architecture arch)
    {
        TargetPointer objectWithTaggedMemory = default;
        TargetPointer objectWithoutTaggedMemory = default;
        TargetPointer expectedTaggedMemory = new TargetPointer(0x7000);
        ObjectiveCMarshalContractHelper(arch,
            (objectBuilder) =>
            {
                uint syncBlockIndex = 0;
                objectWithTaggedMemory = objectBuilder.AddObjectWithSyncBlock(
                    0, syncBlockIndex: syncBlockIndex++,
                    rcw: TargetPointer.Null, ccw: TargetPointer.Null, ccf: TargetPointer.Null,
                    taggedMemory: expectedTaggedMemory);
                objectWithoutTaggedMemory = objectBuilder.AddObjectWithSyncBlock(
                    0, syncBlockIndex: syncBlockIndex++,
                    rcw: TargetPointer.Null, ccw: TargetPointer.Null, ccf: TargetPointer.Null,
                    taggedMemory: TargetPointer.Null);
            },
            (target) =>
            {
                IObjectiveCMarshal contract = target.Contracts.ObjectiveCMarshal;

                bool result1 = contract.TryGetTaggedMemory(objectWithTaggedMemory, out TargetNUInt size1, out TargetPointer tagged1);
                Assert.True(result1);
                Assert.Equal(expectedTaggedMemory, tagged1);
                Assert.Equal(2 * (ulong)target.PointerSize, size1.Value);

                bool result2 = contract.TryGetTaggedMemory(objectWithoutTaggedMemory, out TargetNUInt size2, out TargetPointer tagged2);
                Assert.False(result2);
                Assert.Equal(default, size2);
                Assert.Equal(TargetPointer.Null, tagged2);
            });
    }
}
