// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class MethodDescTests
{
    private static void MethodDescHelper(MockTarget.Architecture arch, Action<MockDescriptors.Object> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        Dictionary<DataType, Target.TypeInfo> types = new();
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(types, builder) {
            // arbtrary address range
            TypeSystemAllocator = builder.CreateAllocator(start: 0x00000000_4a000000, end: 0x00000000_4b000000),
        };

        MockDescriptors.MethodDescriptors.AddTypes(targetTestHelpers, types);

        MockDescriptors.Object objectBuilder = new(rtsBuilder) {
            // arbtrary adress range
            ManagedObjectAllocator = builder.CreateAllocator(start: 0x00000000_10000000, end: 0x00000000_20000000),
        };
        MockDescriptors.Object.AddTypes(types, targetTestHelpers);
        builder = builder
            .SetContracts([ nameof (Contracts.Object), nameof (Contracts.RuntimeTypeSystem) ])
            .SetGlobals(MockDescriptors.Object.Globals(targetTestHelpers))
            .SetTypes(types);

        objectBuilder.AddGlobalPointers();

        configure?.Invoke(objectBuilder);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);
        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MethodDescIsValid(MockTarget.Architecture arch)
    {
        TargetPointer testMethodDescAddress = default;
        MethodDescHelper(arch,
        (builder) =>
        {
            var methodDescChunkAllocator = builder.Builder.CreateAllocator(start: 0x00000000_20002000, end: 0x00000000_20003000);
            var methodDescBuilder = new MockDescriptors.MethodDescriptors(builder.RTSBuilder)
            {
                MethodDescChunkAllocator = methodDescChunkAllocator,
            };
            byte count = 10; // arbitrary
            byte methodDescSize = (byte)(builder.Types[DataType.MethodDesc].Size.Value / methodDescBuilder.MethodDescAlignment);
            byte chunkSize = (byte)(count * methodDescSize);
            var chunk = methodDescBuilder.AddMethodDescChunk(builder.TestStringMethodTableAddress, "testStringMethod", count, chunkSize);

            byte methodDescNum = 3; // abitrary, less than "count"
            byte methodDescIndex = (byte)(methodDescNum * methodDescSize);
            Span<byte> dest = methodDescBuilder.BorrowMethodDesc(chunk, methodDescIndex);
            methodDescBuilder.SetMethodDesc(dest, methodDescIndex);

            testMethodDescAddress = methodDescBuilder.GetMethodDescAddress(chunk, methodDescIndex);

        },
        (target) =>
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

            var handle = rts.GetMethodDescHandle(testMethodDescAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);
        });
    }
}
