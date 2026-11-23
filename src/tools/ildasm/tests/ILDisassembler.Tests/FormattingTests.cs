// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ILDisassembler.Tests;

public class FormattingTests
{
    // Expected values are what glibc printf produces for the same format.
    [Theory]
    [InlineData(1.5, 8, false, "1.5")]
    [InlineData(0.1, 17, false, "0.10000000000000001")]
    [InlineData(100.0, 8, false, "100")]
    [InlineData(1e20, 8, false, "1e+20")]
    [InlineData(1.25e-5, 8, false, "1.25e-05")]
    [InlineData(0.0001, 8, false, "0.0001")]
    [InlineData(123456789.0, 8, false, "1.2345679e+08")]
    [InlineData(-2.5, 17, false, "-2.5")]
    [InlineData(0.5, 17, true, "0.50000000000000000")]
    [InlineData(1.5, 8, true, "1.5000000")]
    [InlineData(1e20, 8, true, "1.0000000e+20")]
    [InlineData(1e100, 17, false, "1e+100")]
    [InlineData(1e100, 17, true, "1.0000000000000000e+100")]
    [InlineData(double.PositiveInfinity, 8, false, "inf")]
    [InlineData(double.NegativeInfinity, 8, false, "-inf")]
    public void FormatGMatchesPrintf(double value, int precision, bool alternate, string expected)
    {
        Assert.Equal(expected, Dasm.FormatG(value, precision, alternate));
    }

    [Fact]
    public void FormatGPrintsTheSignOfNaN()
    {
        // glibc prints the sign bit of a NaN; double.NaN has it set.
        Assert.Equal("-nan", Dasm.FormatG(double.NaN, 8));
        Assert.Equal("nan", Dasm.FormatG(BitConverter.Int64BitsToDouble(0x7FF8000000000000), 8));
    }

    [Theory]
    [InlineData("Foo", "Foo")]
    [InlineData("System.Object", "System.Object")]
    [InlineData("add", "'add'")]
    [InlineData(".ctor", ".ctor")]
    [InlineData(".cctor", ".cctor")]
    [InlineData(".hidden", "'.hidden'")]
    [InlineData("trailing.", "'trailing.'")]
    [InlineData("a..b", "'a..b'")]
    [InlineData("List`1", "List`1")]
    [InlineData("<Module>", "'<Module>'")]
    [InlineData("1abc", "'1abc'")]
    [InlineData("a#b", "'a#b'")]
    [InlineData("#ab", "#ab")]
    [InlineData("q?", "q?")]
    [InlineData("it's", "'it\\'s'")]
    [InlineData("tab\there", "'tab\\there'")]
    [InlineData("café", "'café'")]
    public void ProperNameQuotesKeywordsAndSpecialNames(string name, string expected)
    {
        Assert.Equal(expected, CreateDasm(new Options()).ProperName(name));
    }

    [Fact]
    public void QuoteAllNamesQuotesEverythingButConstructors()
    {
        Dasm dasm = CreateDasm(new Options { QuoteAllNames = true });
        Assert.Equal("'Foo'", dasm.ProperName("Foo"));
        Assert.Equal(".ctor", dasm.ProperName(".ctor"));
    }

    private static Dasm CreateDasm(Options options)
    {
        byte[] image = RoundTripTests.Assemble("""
            .assembly test { }
            .module test.dll
            """);
        return new Dasm(new System.Reflection.PortableExecutable.PEReader(new System.IO.MemoryStream(image)), options,
            System.IO.TextWriter.Null, System.IO.TextWriter.Null);
    }
}
