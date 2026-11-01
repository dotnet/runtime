// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class PlatformMetadataTests
{
    private const ulong MetadataAddress = 0x1000;

    [Fact]
    public void MissingMaskLeavesReturnAddressUnchanged()
    {
        Target target = CreateTarget();
        Assert.Equal(0UL, target.Contracts.PlatformMetadata.GetPointerAuthenticationMask());
        Assert.Equal(0x1234UL, CodePointerUtils.StripPtrAuthFromReturnAddress(new(0x1234), target).Value);
    }

    [Theory]
    [InlineData(0xff7f800000000000UL, 0xa53afffebc33aae4UL, 0x00007ffebc33aae4UL)]
    [InlineData(0xff70000000000000UL, 0xa53afffebc33aae4UL, 0x000afffebc33aae4UL)]
    public void ClearsOnlyMaskBits(ulong mask, ulong signedAddress, ulong address)
    {
        Target target = CreateTarget(mask);

        Assert.Equal(address, CodePointerUtils.StripPtrAuthFromReturnAddress(new(signedAddress), target).Value);
    }

    private static Target CreateTarget(ulong? mask = null)
    {
        MockTarget.Architecture arch = new() { Is64Bit = true, IsLittleEndian = true };
        TestPlaceholderTarget.Builder builder = new(arch);
        TargetTestHelpers helpers = new(arch);
        Dictionary<string, Target.FieldInfo> fields = [];
        byte[] data = new byte[8];
        if (mask.HasValue)
        {
            fields[nameof(Data.PlatformMetadata.PointerAuthenticationMask)] = new() { Offset = 0 };
            helpers.Write(data, mask.Value);
        }
        builder.MemoryBuilder.AddHeapFragment(new() { Name = "PlatformMetadata", Address = MetadataAddress, Data = data });
        return builder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo> { [DataType.PlatformMetadata] = new() { Fields = fields } })
            .AddGlobals((Constants.Globals.PlatformMetadata, MetadataAddress))
            .AddContract<IPlatformMetadata>("c1")
            .Build();
    }
}
