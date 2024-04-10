// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ContractDescriptorParserTests
{

    [Fact]
    public void ParsesEmptyContract()
    {
        ReadOnlyMemory<byte> json = """
        {
            "Version": 1,
            "Baseline": "empty",
            "Contracts": {},
            "Types": {},
            "Globals": {}
        }
        """u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Equal(1, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Types);
        Assert.NotNull(descriptor.Extras["Globals"]);
    }

    [Fact]
    public void ParseSizedTypes()
    {
        ReadOnlyMemory<byte> json = """
        {
            "Version": 1,
            "Baseline": "empty",
            "Contracts": {},
            "Types":
            {
                "pointer": { "!" : 8},
                "int": { "!" : 4},
                "Point": {
                    "x": { "Type": "int", "Offset": 4 },
                    "y": { "Type": "int", "Offset": 8 }
                }
            }
        }
        """u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Equal(1, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Equal(3, descriptor.Types.Count);
        Assert.Equal(8u, descriptor.Types["pointer"].Size);
        Assert.Equal(4u, descriptor.Types["int"].Size);
        Assert.Equal(2, descriptor.Types["Point"].Fields.Count);
        Assert.Equal(4, descriptor.Types["Point"].Fields["x"].Offset);
        Assert.Equal(8, descriptor.Types["Point"].Fields["y"].Offset);
        Assert.Equal(0u, descriptor.Types["Point"].Size);
    }
}
