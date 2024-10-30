// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Data;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class MethodDescTests
{
    private static void MethodDescHelper(MockTarget.Architecture arch, Action<MockDescriptors.MethodDescriptors> configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        MockMemorySpace.Builder builder = new(targetTestHelpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder) {
            // arbtrary address range
            TypeSystemAllocator = builder.CreateAllocator(start: 0x00000000_4a000000, end: 0x00000000_4b000000),
        };

        MockDescriptors.Object objectBuilder = new(rtsBuilder) {
            // arbtrary adress range
            ManagedObjectAllocator = builder.CreateAllocator(start: 0x00000000_10000000, end: 0x00000000_20000000),
        };
        var methodDescChunkAllocator = builder.CreateAllocator(start: 0x00000000_20002000, end: 0x00000000_20003000);
        var methodDescBuilder = new MockDescriptors.MethodDescriptors(rtsBuilder)
        {
            MethodDescChunkAllocator = methodDescChunkAllocator,
        };

        builder = builder
            .SetContracts([ nameof (Contracts.Object), nameof (Contracts.RuntimeTypeSystem) ])
            .SetGlobals(MockDescriptors.MethodDescriptors.Globals(targetTestHelpers))
            .SetTypes(rtsBuilder.Types);

        methodDescBuilder.AddGlobalPointers();

        configure?.Invoke(methodDescBuilder);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);
        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void MethodDescGetMethodDescTokenOk(MockTarget.Architecture arch)
    {
        TargetPointer testMethodDescAddress = default;
        const int MethodDefToken = 0x06 << 24;
        const ushort expectedRidRangeStart = 0x2000; // arbitrary (larger than  1<< TokenRemainderBitCount)
        const ushort expectedRidRemainder = 0x10; // arbitrary
        const uint expectedRid = expectedRidRangeStart | expectedRidRemainder; // arbitrary
        uint expectedToken = MethodDefToken | expectedRid;
        MethodDescHelper(arch,
        (builder) =>
        {
            var objectMethodTable = MethodTableTests.AddSystemObjectMethodTable(builder.RTSBuilder).MethodTable;

            byte count = 10; // arbitrary
            byte methodDescSize = (byte)(builder.Types[DataType.MethodDesc].Size.Value / builder.MethodDescAlignment);
            byte chunkSize = (byte)(count * methodDescSize);
            var chunk = builder.AddMethodDescChunk(objectMethodTable, "testMethod", count, chunkSize, tokenRange: expectedRidRangeStart);

            byte methodDescNum = 3; // abitrary, less than "count"
            byte methodDescIndex = (byte)(methodDescNum * methodDescSize);
            Span<byte> dest = builder.BorrowMethodDesc(chunk, methodDescIndex);
            builder.SetMethodDesc(dest, methodDescIndex, tokenRemainder: expectedRidRemainder);

            testMethodDescAddress = builder.GetMethodDescAddress(chunk, methodDescIndex);

        },
        (target) =>
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

            var handle = rts.GetMethodDescHandle(testMethodDescAddress);
            Assert.NotEqual(TargetPointer.Null, handle.Address);

            uint token = rts.GetMethodToken(handle);
            Assert.Equal(expectedToken, token);
        });
    }
}
