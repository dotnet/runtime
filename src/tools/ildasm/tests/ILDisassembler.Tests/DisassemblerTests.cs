// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Xunit;

namespace ILDisassembler.Tests;

public class DisassemblerTests
{
    [Fact]
    public void CanDisassembleSelf()
    {
        // Disassemble this test assembly itself
        var assemblyPath = typeof(DisassemblerTests).Assembly.Location;

        using var disassembler = new Disassembler(assemblyPath);

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        disassembler.Disassemble(writer);

        var result = output.ToString();

        // Basic sanity checks
        Assert.Contains(".assembly", result);
        Assert.Contains(".module", result);
        Assert.Contains(".class", result);
        Assert.Contains("ILDisassembler.Tests", result);
    }

    [Fact]
    public void DisassemblyContainsAssemblyReferences()
    {
        var assemblyPath = typeof(DisassemblerTests).Assembly.Location;

        using var disassembler = new Disassembler(assemblyPath);

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        disassembler.Disassemble(writer);

        var result = output.ToString();

        // Should reference System.Runtime
        Assert.Contains(".assembly extern", result);
    }

    [Fact]
    public void DisassemblyContainsMethods()
    {
        var assemblyPath = typeof(DisassemblerTests).Assembly.Location;

        using var disassembler = new Disassembler(assemblyPath);

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        disassembler.Disassemble(writer);

        var result = output.ToString();

        // Should have methods
        Assert.Contains(".method", result);
        Assert.Contains("cil managed", result);
    }
}
