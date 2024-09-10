// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ContractDescriptorParserTests
{
    [Fact]
    public void ParsesEmptyContract()
    {
        Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
        ReadOnlySpan<byte> json = "{}"u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Null(descriptor.Version);
        Assert.Null(descriptor.Baseline);
        Assert.Null(descriptor.Contracts);
        Assert.Null(descriptor.Types);
        Assert.Null(descriptor.Extras);
    }
    [Fact]
    public void ParsesTrivialContract()
    {
        ReadOnlySpan<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": {},
            "globals": {}
        }
        """u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Types);
        Assert.Empty(descriptor.Globals);
        Assert.Null(descriptor.Extras);
    }

    [Fact]
    public void ParseSizedTypes()
    {
        ReadOnlySpan<byte> json = """
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
            },
            "globals": {}
        }
        """u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Globals);
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
        Assert.Null(descriptor.Types["Point3D"].Size);
    }

    [Fact]
    public void ParseContractsCaseSensitive()
    {
        ReadOnlySpan<byte> json = """
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
        """u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Equal(2, descriptor.Contracts.Count);
        Assert.Equal(1, descriptor.Contracts["foo"]);
        Assert.Equal(2, descriptor.Contracts["Foo"]);
    }

    [Fact]
    public void ParsesGlobals()
    {
        ReadOnlySpan<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": {},
            "globals": {
                "globalInt": 1,
                "globalPtr": [2],
                "globalTypedInt": [3, "uint8"],
                "globalTypedPtr": [[4], "uintptr"],
                "globalHex": "0x1234",
                "globalNegative": -2,
                "globalStringyInt": "17",
                "globalStringyNegative": "-2",
                "globalNegativeHex": "-0xff",
                "globalBigStringyInt": "0x123456789abcdef",
                "globalStringyPtr": ["0x1234"],
                "globalTypedStringyInt": ["0x1234", "int"],
                "globalTypedStringyPtr": [["0x1234"], "int"]
            }
        }
        """u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Types);
        Assert.Equal(13, descriptor.Globals.Count);
        Assert.Equal((ulong)1, descriptor.Globals["globalInt"].Value);
        Assert.False(descriptor.Globals["globalInt"].Indirect);
        Assert.Equal((ulong)2, descriptor.Globals["globalPtr"].Value);
        Assert.True(descriptor.Globals["globalPtr"].Indirect);
        Assert.Equal((ulong)3, descriptor.Globals["globalTypedInt"].Value);
        Assert.False(descriptor.Globals["globalTypedInt"].Indirect);
        Assert.Equal("uint8", descriptor.Globals["globalTypedInt"].Type);
        Assert.Equal((ulong)4, descriptor.Globals["globalTypedPtr"].Value);
        Assert.True(descriptor.Globals["globalTypedPtr"].Indirect);
        Assert.Equal("uintptr", descriptor.Globals["globalTypedPtr"].Type);
        Assert.Equal((ulong)0x1234, descriptor.Globals["globalHex"].Value);
        Assert.False(descriptor.Globals["globalHex"].Indirect);
        Assert.Equal((ulong)0xfffffffffffffffe, descriptor.Globals["globalNegative"].Value);
        Assert.False(descriptor.Globals["globalNegative"].Indirect);
        Assert.Equal((ulong)17, descriptor.Globals["globalStringyInt"].Value);
        Assert.False(descriptor.Globals["globalStringyInt"].Indirect);
        Assert.Equal((ulong)0xfffffffffffffffe, descriptor.Globals["globalStringyNegative"].Value);
        Assert.False(descriptor.Globals["globalStringyNegative"].Indirect);
        Assert.Equal((ulong)0xffffffffffffff01, descriptor.Globals["globalNegativeHex"].Value);
        Assert.False(descriptor.Globals["globalNegativeHex"].Indirect);
        Assert.Equal((ulong)0x123456789abcdef, descriptor.Globals["globalBigStringyInt"].Value);
        Assert.False(descriptor.Globals["globalBigStringyInt"].Indirect);
        Assert.Equal((ulong)0x1234, descriptor.Globals["globalStringyPtr"].Value);
        Assert.True(descriptor.Globals["globalStringyPtr"].Indirect);
        Assert.Equal("int", descriptor.Globals["globalTypedStringyInt"].Type);
        Assert.Equal((ulong)0x1234, descriptor.Globals["globalTypedStringyInt"].Value);
        Assert.False(descriptor.Globals["globalTypedStringyInt"].Indirect);
        Assert.Equal("int", descriptor.Globals["globalTypedStringyPtr"].Type);
        Assert.Equal((ulong)0x1234, descriptor.Globals["globalTypedStringyPtr"].Value);
        Assert.True(descriptor.Globals["globalTypedStringyPtr"].Indirect);
    }

    [Fact]
    void ParsesExoticOffsets()
    {
        ReadOnlySpan<byte> json = """
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {},
            "types": {
                "OddStruct": {
                    "a": -12,
                    "b": "0x12",
                    "c": "-0x12",
                    "d": ["0x100", "int"]
                }
            },
            "globals": {
            }
        }
        """u8;
        ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact(json);
        Assert.Equal(0, descriptor.Version);
        Assert.Equal("empty", descriptor.Baseline);
        Assert.Empty(descriptor.Contracts);
        Assert.Empty(descriptor.Globals);
        Assert.Equal(1, descriptor.Types.Count);
        Assert.Equal(4, descriptor.Types["OddStruct"].Fields.Count);
        Assert.Equal(-12, descriptor.Types["OddStruct"].Fields["a"].Offset);
        Assert.Equal(0x12, descriptor.Types["OddStruct"].Fields["b"].Offset);
        Assert.Equal(-0x12, descriptor.Types["OddStruct"].Fields["c"].Offset);
        Assert.Equal(0x100, descriptor.Types["OddStruct"].Fields["d"].Offset);
        Assert.Equal("int", descriptor.Types["OddStruct"].Fields["d"].Type);
    }
}
