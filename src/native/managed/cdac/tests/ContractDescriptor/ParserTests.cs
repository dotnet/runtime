// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public class ParserTests
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
                "globalTypedNegative": [-5, "int32"],
                "globalString": "Hello",
                "globalTypedString": ["World", "string"],
                "globalStringSpecialChars" : "\"Hello World\"",
                "globalIntLarge": 18446744073709551615,
                "globalIntLargeNegative": -9223372036854775808,
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

        Assert.Equal(19, descriptor.Globals.Count);

        Assert.Equal((ulong)1, descriptor.Globals["globalInt"].NumericValue);
        Assert.Null(descriptor.Globals["globalInt"].StringValue);
        AssertDirect(descriptor.Globals["globalInt"]);

        Assert.Equal((ulong)2, descriptor.Globals["globalPtr"].NumericValue);
        Assert.Null(descriptor.Globals["globalPtr"].StringValue);
        AssertIndirect(descriptor.Globals["globalPtr"]);

        Assert.Equal((ulong)3, descriptor.Globals["globalTypedInt"].NumericValue);
        Assert.Null(descriptor.Globals["globalTypedInt"].StringValue);
        AssertDirect(descriptor.Globals["globalTypedInt"]);
        Assert.Equal("uint8", descriptor.Globals["globalTypedInt"].Type);

        Assert.Equal((ulong)4, descriptor.Globals["globalTypedPtr"].NumericValue);
        Assert.Null(descriptor.Globals["globalTypedPtr"].StringValue);
        AssertIndirect(descriptor.Globals["globalTypedPtr"]);
        Assert.Equal("uintptr", descriptor.Globals["globalTypedPtr"].Type);

        Assert.Equal((ulong)0x1234, descriptor.Globals["globalHex"].NumericValue);
        Assert.Equal("0x1234", descriptor.Globals["globalHex"].StringValue);
        AssertDirect(descriptor.Globals["globalHex"]);

        Assert.Equal(unchecked((ulong)-2), descriptor.Globals["globalNegative"].NumericValue);
        Assert.Null(descriptor.Globals["globalNegative"].StringValue);
        AssertDirect(descriptor.Globals["globalNegative"]);

        Assert.Equal(unchecked((ulong)-5), descriptor.Globals["globalTypedNegative"].NumericValue);
        Assert.Null(descriptor.Globals["globalTypedNegative"].StringValue);
        AssertDirect(descriptor.Globals["globalTypedNegative"]);
        Assert.Equal("int32", descriptor.Globals["globalTypedNegative"].Type);

        Assert.Equal("Hello", descriptor.Globals["globalString"].StringValue);
        AssertDirect(descriptor.Globals["globalString"]);

        Assert.Equal("World", descriptor.Globals["globalTypedString"].StringValue);
        AssertDirect(descriptor.Globals["globalTypedString"]);
        Assert.Equal("string", descriptor.Globals["globalTypedString"].Type);

        Assert.Equal("\"Hello World\"", descriptor.Globals["globalStringSpecialChars"].StringValue);
        AssertDirect(descriptor.Globals["globalStringSpecialChars"]);

        Assert.Equal(ulong.MaxValue, descriptor.Globals["globalIntLarge"].NumericValue);
        Assert.Null(descriptor.Globals["globalIntLarge"].StringValue);
        AssertDirect(descriptor.Globals["globalIntLarge"]);

        Assert.Equal(unchecked((ulong)long.MinValue), descriptor.Globals["globalIntLargeNegative"].NumericValue);
        Assert.Null(descriptor.Globals["globalIntLargeNegative"].StringValue);
        AssertDirect(descriptor.Globals["globalIntLargeNegative"]);

        Assert.Equal((ulong)17, descriptor.Globals["globalStringyInt"].NumericValue);
        Assert.Equal("17", descriptor.Globals["globalStringyInt"].StringValue);
        AssertDirect(descriptor.Globals["globalStringyInt"]);

        Assert.Equal(unchecked((ulong)-2), descriptor.Globals["globalStringyNegative"].NumericValue);
        Assert.Equal("-2", descriptor.Globals["globalStringyNegative"].StringValue);
        AssertDirect(descriptor.Globals["globalStringyNegative"]);

        Assert.Equal(unchecked((ulong)-0xff), descriptor.Globals["globalNegativeHex"].NumericValue);
        Assert.Equal("-0xff", descriptor.Globals["globalNegativeHex"].StringValue);
        AssertDirect(descriptor.Globals["globalNegativeHex"]);

        Assert.Equal((ulong)0x123456789abcdef, descriptor.Globals["globalBigStringyInt"].NumericValue);
        Assert.Equal("0x123456789abcdef", descriptor.Globals["globalBigStringyInt"].StringValue);
        AssertDirect(descriptor.Globals["globalBigStringyInt"]);

        Assert.Equal((ulong)0x1234, descriptor.Globals["globalStringyPtr"].NumericValue);
        Assert.Equal("0x1234", descriptor.Globals["globalStringyPtr"].StringValue);
        AssertIndirect(descriptor.Globals["globalStringyPtr"]);

        Assert.Equal((ulong)0x1234, descriptor.Globals["globalTypedStringyInt"].NumericValue);
        Assert.Equal("0x1234", descriptor.Globals["globalTypedStringyInt"].StringValue);
        AssertDirect(descriptor.Globals["globalTypedStringyInt"]);
        Assert.Equal("int", descriptor.Globals["globalTypedStringyInt"].Type);

        Assert.Equal((ulong)0x1234, descriptor.Globals["globalTypedStringyPtr"].NumericValue);
        Assert.Equal("0x1234", descriptor.Globals["globalTypedStringyPtr"].StringValue);
        AssertIndirect(descriptor.Globals["globalTypedStringyPtr"]);
        Assert.Equal("int", descriptor.Globals["globalTypedStringyPtr"].Type);

        void AssertIndirect(ContractDescriptorParser.GlobalDescriptor descriptor)
            => Assert.True(descriptor.Indirect);

        void AssertDirect(ContractDescriptorParser.GlobalDescriptor descriptor)
            => Assert.False(descriptor.Indirect);
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
