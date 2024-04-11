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
        ReadOnlyMemory<byte> json = "{}"u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Null(descriptor.Version);
        Assert.Null(descriptor.Baseline);
        Assert.Null(descriptor.Contracts);
        Assert.Null(descriptor.Types);
        Assert.Null(descriptor.Extras);
    }
    [Fact]
    public void ParsesTrivialContract()
    {
        ReadOnlyMemory<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": {},
            "globals": {}
        }
        """u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Types);
        Assert.NotNull(descriptor.Extras["globals"]);
    }

    [Fact]
    public void ParseSizedTypes()
    {
        ReadOnlyMemory<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types":
            {
                "pointer": { "!" : 8},
                "int": { "!" : 4},
                "Point": {
                    "x": [ 4, "int"],
                    "y": 8,
                    "!": 12
                },
                "Point3D": { // no size
                    "r": [ 0, "double"],
                    "phi": 8,
                    "rho": 16
                }
            }
        }
        """u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Equal(4, descriptor.Types.Count);
        Assert.Equal(8u, descriptor.Types["pointer"].Size);
        Assert.Equal(4u, descriptor.Types["int"].Size);
        Assert.Equal(2, descriptor.Types["Point"].Fields.Count);
        Assert.Equal(4, descriptor.Types["Point"].Fields["x"].Offset);
        Assert.Equal(8, descriptor.Types["Point"].Fields["y"].Offset);
        Assert.Equal("int", descriptor.Types["Point"].Fields["x"].Type);
        Assert.Null(descriptor.Types["Point"].Fields["y"].Type);
        Assert.Equal(12u, descriptor.Types["Point"].Size);
        Assert.Equal(3, descriptor.Types["Point3D"].Fields.Count);
        Assert.Equal(0, descriptor.Types["Point3D"].Fields["r"].Offset);
        Assert.Equal(8, descriptor.Types["Point3D"].Fields["phi"].Offset);
        Assert.Equal(16, descriptor.Types["Point3D"].Fields["rho"].Offset);
        Assert.Equal("double", descriptor.Types["Point3D"].Fields["r"].Type);
        Assert.Null(descriptor.Types["Point3D"].Fields["phi"].Type);
        Assert.Equal(0u, descriptor.Types["Point3D"].Size);
    }

    [Fact]
    public void ParseContractsCaseSensitive()
    {
        ReadOnlyMemory<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "foo": 1,
                "Foo": 2
            },
            "types": {},
            "globals": {}
        }
        """u8.ToArray();
        ContractDescriptorParser.CompactContractDescriptor descriptor = ContractDescriptorParser.Parse(json.Span);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Equal(2, descriptor.Contracts.Count);
        Assert.Equal(1, descriptor.Contracts["foo"]);
        Assert.Equal(2, descriptor.Contracts["Foo"]);
    }
}
