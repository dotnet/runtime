// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Xunit;

namespace ILDisassembler.Tests;

public class InstructionDisassemblerTests
{
    [Theory]
    [InlineData(new byte[] { 0x00 }, "nop")]                    // nop
    [InlineData(new byte[] { 0x2A }, "ret")]                    // ret
    [InlineData(new byte[] { 0x14 }, "ldnull")]                 // ldnull
    [InlineData(new byte[] { 0x16 }, "ldc.i4.0")]               // ldc.i4.0
    [InlineData(new byte[] { 0x17 }, "ldc.i4.1")]               // ldc.i4.1
    [InlineData(new byte[] { 0x1F, 0x2A }, "ldc.i4.s 42")]      // ldc.i4.s 42
    [InlineData(new byte[] { 0x06 }, "ldloc.0")]                // ldloc.0
    [InlineData(new byte[] { 0x0A }, "stloc.0")]                // stloc.0
    [InlineData(new byte[] { 0x02 }, "ldarg.0")]                // ldarg.0
    [InlineData(new byte[] { 0x58 }, "add")]                    // add
    [InlineData(new byte[] { 0x59 }, "sub")]                    // sub
    public unsafe void DisassemblesBasicInstructions(byte[] ilBytes, string expected)
    {
        // Create a mock MetadataReader - we don't need it for these basic instructions
        var disasm = new InstructionDisassembler(null!);

        fixed (byte* ptr = ilBytes)
        {
            var reader = new BlobReader(ptr, ilBytes.Length);
            var result = disasm.DisassembleInstruction(ref reader);
            Assert.Equal(expected, result);
        }
    }
}
