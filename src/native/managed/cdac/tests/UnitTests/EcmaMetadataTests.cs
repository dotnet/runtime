// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class EcmaMetadataTests
{
    private const uint MetadataSize = 0x20;

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetMetadataAddress_ReadWriteSavedCopy_ReturnsDynamicMetadata(MockTarget.Architecture arch)
    {
        IEcmaMetadata contract = CreateContractWithSavedMetadata(arch, out ModuleHandle handle, out TargetSpan expected);

        TargetSpan result = contract.GetMetadataAddress(handle, MetadataAddressKind.ReadWriteSavedCopy);

        Assert.Equal(expected, result);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetMetadata_RequireReadWriteMetadata_RejectsSavedCopy(MockTarget.Architecture arch)
    {
        IEcmaMetadata contract = CreateContractWithSavedMetadata(arch, out ModuleHandle handle, out _);

        Assert.Throws<ArgumentException>(() => contract.GetMetadata(handle, requireReadWriteMetadata: true));
    }

    private static IEcmaMetadata CreateContractWithSavedMetadata(
        MockTarget.Architecture arch,
        out ModuleHandle handle,
        out TargetSpan metadata)
    {
        TargetTestHelpers helpers = new(arch);
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x1000, 0x3000);
        TargetTestHelpers.LayoutResult moduleLayout = helpers.LayoutFields([
            new(nameof(Data.Module.DynamicMetadata), DataType.pointer),
            new(nameof(Data.Module.MetadataGeneration), DataType.uint32),
        ]);
        TargetTestHelpers.LayoutResult dynamicMetadataLayout = helpers.LayoutFields([
            new(nameof(Data.DynamicMetadata.Size), DataType.uint32),
            new(nameof(Data.DynamicMetadata.Data), DataType.uint8),
        ]);
        MockMemorySpace.HeapFragment module = allocator.Allocate(moduleLayout.Stride, "Module");
        MockMemorySpace.HeapFragment dynamicMetadata =
            allocator.Allocate(dynamicMetadataLayout.Stride + MetadataSize, "DynamicMetadata");

        helpers.WritePointer(
            module.Data.AsSpan().Slice(moduleLayout.Fields[nameof(Data.Module.DynamicMetadata)].Offset, helpers.PointerSize),
            dynamicMetadata.Address);
        helpers.Write(
            dynamicMetadata.Data.AsSpan().Slice(
                dynamicMetadataLayout.Fields[nameof(Data.DynamicMetadata.Size)].Offset,
                sizeof(uint)),
            MetadataSize);

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Module] = new() { Fields = moduleLayout.Fields, Size = moduleLayout.Stride },
            [DataType.DynamicMetadata] = new() { Fields = dynamicMetadataLayout.Fields, Size = dynamicMetadataLayout.Stride },
        };
        TestPlaceholderTarget target = targetBuilder
            .AddTypes(types)
            .AddContract<IEcmaMetadata>("c1")
            .Build();

        handle = new ModuleHandle(new TargetPointer(module.Address));
        metadata = new TargetSpan(
            new TargetPointer(dynamicMetadata.Address + (uint)dynamicMetadataLayout.Fields[nameof(Data.DynamicMetadata.Data)].Offset),
            MetadataSize);
        return target.Contracts.EcmaMetadata;
    }
}
