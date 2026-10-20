// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class InstructionTests
    {
        public static TheoryData<int, byte[]> IntegerConstants { get; } = new()
        {
            { int.MinValue, [0x20, 0, 0, 0, 0x80] },
            { -129, [0x20, 0x7F, 0xFF, 0xFF, 0xFF] },
            { -128, [0x1F, 0x80] },
            { -1, [0x15] },
            { 0, [0x16] },
            { 1, [0x17] },
            { 2, [0x18] },
            { 3, [0x19] },
            { 4, [0x1A] },
            { 5, [0x1B] },
            { 6, [0x1C] },
            { 7, [0x1D] },
            { 8, [0x1E] },
            { 9, [0x1F, 9] },
            { 127, [0x1F, 0x7F] },
            { 128, [0x20, 0x80, 0, 0, 0] },
            { int.MaxValue, [0x20, 0xFF, 0xFF, 0xFF, 0x7F] },
        };

        public static TheoryData<string, byte, byte, int> VariableOpcodes { get; } = new()
        {
            { "ldarg", 0x09, 0x0E, 0x02 },
            { "ldarga", 0x0A, 0x0F, -1 },
            { "starg", 0x0B, 0x10, -1 },
            { "ldloc", 0x0C, 0x11, 0x06 },
            { "ldloca", 0x0D, 0x12, -1 },
            { "stloc", 0x0E, 0x13, 0x0A },
        };

        public static TheoryData<string, byte, byte> BranchOpcodes { get; } = new()
        {
            { "br", 0x38, 0x2B },
            { "brfalse", 0x39, 0x2C },
            { "brtrue", 0x3A, 0x2D },
            { "beq", 0x3B, 0x2E },
            { "bge", 0x3C, 0x2F },
            { "bgt", 0x3D, 0x30 },
            { "ble", 0x3E, 0x31 },
            { "blt", 0x3F, 0x32 },
            { "bne.un", 0x40, 0x33 },
            { "bge.un", 0x41, 0x34 },
            { "bgt.un", 0x42, 0x35 },
            { "ble.un", 0x43, 0x36 },
            { "blt.un", 0x44, 0x37 },
            { "leave", 0xDD, 0xDE },
        };

        [Theory]
        [MemberData(nameof(IntegerConstants))]
        public void IntegerConstantEncodings(int value, byte[] optimized)
        {
            byte[] longForm = new byte[5];
            longForm[0] = 0x20;
            BinaryPrimitives.WriteInt32LittleEndian(longForm.AsSpan(1), value);
            foreach (bool optimize in new[] { false, true })
            {
                Assert.Equal((optimize ? optimized : longForm).Append((byte)0x2A).ToArray(),
                    DocumentCompilerTestHelpers.CompileMethodIL($"ldc.i4 {value}\nret", new Options { Optimize = optimize }));
                byte[] shortForm = [0x1F, unchecked((byte)value)];
                byte[] expected = optimize && value is >= -1 and <= 8 ? optimized : shortForm;
                Assert.Equal(expected.Append((byte)0x2A).ToArray(),
                    DocumentCompilerTestHelpers.CompileMethodIL($"ldc.i4.s {value}\nret", new Options { Optimize = optimize }));
            }
        }

        [Theory]
        [MemberData(nameof(VariableOpcodes))]
        public void VariableEncodings(string opcode, byte longOpcode, byte shortOpcode, int macroBase)
        {
            foreach (bool optimize in new[] { false, true })
            {
                foreach (int index in new[] { 0, 1, 2, 3, 4, 255, 256, ushort.MaxValue })
                {
                    byte[] expected = optimize && macroBase >= 0 && index < 4
                        ? [(byte)(macroBase + index)]
                        : optimize && index <= byte.MaxValue
                            ? [shortOpcode, (byte)index]
                            : [0xFE, longOpcode, (byte)index, (byte)(index >> 8)];
                    Assert.Equal(expected.Append((byte)0x2A).ToArray(), DocumentCompilerTestHelpers.CompileMethodIL(
                        $"{opcode} {index}\nret", new Options { Optimize = optimize }));
                    expected = optimize && macroBase >= 0 && index < 4
                        ? [(byte)(macroBase + index)]
                        : [shortOpcode, unchecked((byte)index)];
                    Assert.Equal(expected.Append((byte)0x2A).ToArray(), DocumentCompilerTestHelpers.CompileMethodIL(
                        $"{opcode}.s {index}\nret", new Options { Optimize = optimize }));
                }

                string name = opcode.Contains("arg") ? "arg" : "local";
                byte[] namedExpected = optimize
                    ? macroBase >= 0 ? [(byte)macroBase, 0x2A] : [shortOpcode, 0, 0x2A]
                    : [0xFE, longOpcode, 0, 0, 0x2A];
                Assert.Equal(namedExpected, DocumentCompilerTestHelpers.CompileMethodIL(
                    $"{opcode} {name}\nret", new Options { Optimize = optimize }));
                if (opcode.Contains("arg"))
                {
                    namedExpected = optimize
                        ? macroBase >= 0 ? [(byte)(macroBase + 1), 0x2A] : [shortOpcode, 1, 0x2A]
                        : [0xFE, longOpcode, 1, 0, 0x2A];
                    Assert.Equal(namedExpected, DocumentCompilerTestHelpers.CompileMethodIL(
                        $"{opcode} arg\nret", new Options { Optimize = optimize }, "public void M(int32 arg)"));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NestedLocalDeclarations_PreserveSlotIndices(bool optimize)
        {
            const string instructions = """
                ldloc local
                {
                    .locals (int32 inner)
                    ldloc inner
                }
                ldloc local
                ret
                """;
            byte[] expected = optimize ? [0x06, 0x07, 0x06, 0x2A]
                : [0xFE, 0x0C, 0, 0, 0xFE, 0x0C, 1, 0, 0xFE, 0x0C, 0, 0, 0x2A];
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(instructions, new Options { Optimize = optimize }));
        }

        [Theory]
        [MemberData(nameof(BranchOpcodes))]
        public void BranchEncodings(string opcode, byte longOpcode, byte shortOpcode)
        {
            foreach (bool optimize in new[] { false, true })
            {
                var options = new Options { Optimize = optimize };
                Assert.Equal(new byte[] { longOpcode, 1, 0, 0, 0, 0, 0x2A },
                    DocumentCompilerTestHelpers.CompileMethodIL($"{opcode} END\nnop\nEND: ret", options));
                Assert.Equal(new byte[] { shortOpcode, 1, 0, 0x2A },
                    DocumentCompilerTestHelpers.CompileMethodIL($"{opcode}.s END\nnop\nEND: ret", options));
                byte[] backward = optimize ? [0, shortOpcode, 0xFD, 0x2A] : [0, longOpcode, 0xFA, 0xFF, 0xFF, 0xFF, 0x2A];
                Assert.Equal(backward, DocumentCompilerTestHelpers.CompileMethodIL($"START: nop\n{opcode} START\nret", options));
                Assert.Equal(new byte[] { 0, shortOpcode, 0xFD, 0x2A },
                    DocumentCompilerTestHelpers.CompileMethodIL($"START: nop\n{opcode}.s START\nret", options));
                foreach (int distance in new[] { int.MinValue, -129, -128, -1, 0, 127, 128, int.MaxValue })
                {
                    byte[] expected;
                    if (optimize && distance is >= -128 and <= 127)
                    {
                        expected = [shortOpcode, unchecked((byte)distance), 0x2A];
                    }
                    else
                    {
                        expected = new byte[6];
                        expected[0] = longOpcode;
                        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(1), distance);
                        expected[5] = 0x2A;
                    }
                    Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL($"{opcode} {distance}\nret", options));
                }
            }
        }

        [Theory]
        [InlineData(123, true)]
        [InlineData(124, true)]
        [InlineData(125, true)]
        [InlineData(126, true)]
        [InlineData(127, false)]
        [InlineData(128, false)]
        public void BackwardBranchOptimization_UsesShortInstructionLimit(int padding, bool shortened)
        {
            byte[] il = DocumentCompilerTestHelpers.CompileMethodIL(
                "START:\n" + string.Concat(Enumerable.Repeat("nop\n", padding)) + "br START\nret", new Options { Optimize = true });
            Assert.All(il.Take(padding), value => Assert.Equal(0, value));
            Assert.Equal(shortened ? 0x2B : 0x38, il[padding]);
            Assert.Equal(shortened ? -padding - 2 : -padding - 5,
                shortened ? (sbyte)il[padding + 1] : BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(padding + 1)));
            Assert.Equal(padding + (shortened ? 2 : 5) + 1, il.Length);
        }

        [Theory]
        [MemberData(nameof(BranchOpcodes))]
        public void BackwardBranchFamilies_ShortenAtSignedByteLimit(string opcode, byte longOpcode, byte shortOpcode)
        {
            foreach (int padding in new[] { 123, 124, 125, 126, 127, 128 })
            {
                string source = "START:\n" + string.Concat(Enumerable.Repeat("nop\n", padding)) + $"{opcode} START\nret";
                foreach (bool optimize in new[] { false, true })
                {
                    bool shortened = optimize && padding <= 126;
                    int instructionSize = shortened ? 2 : 5;
                    byte[] expected = new byte[padding + instructionSize + 1];
                    expected[padding] = shortened ? shortOpcode : longOpcode;
                    if (shortened)
                    {
                        expected[padding + 1] = unchecked((byte)(-padding - instructionSize));
                    }
                    else
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(padding + 1), -padding - instructionSize);
                    }
                    expected[^1] = 0x2A;
                    Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(source, new Options { Optimize = optimize }));
                }
            }
        }

        [Theory]
        [InlineData(127, false)]
        [InlineData(128, true)]
        [InlineData(-128, false)]
        [InlineData(-129, true)]
        public void ShortNumericBranch_OperandBoundaries(int distance, bool error)
        {
            foreach (bool optimize in new[] { false, true })
            {
                string source = DocumentCompilerTestHelpers.MethodSource($"br.s {distance}\nret");
                var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                    source, new Options { Optimize = optimize, ErrorTolerant = true });
                Assert.Equal(error ? 1 : 0, diagnostics.Length);
                if (error)
                {
                    Assert.Equal(DiagnosticIds.BranchOffsetOutOfRange, diagnostics[0].Id);
                }
                Assert.NotNull(result);
                using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
                Assert.Equal(new byte[] { 0x2B, unchecked((byte)distance), 0x2A },
                    DocumentCompilerTestHelpers.GetMethodBody(pe).GetILBytes());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SwitchOffsets_AreRelativeToEndOfTable(bool optimize)
        {
            byte[] expected = new byte[32];
            expected[1] = 0x45;
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(2), 6);
            int[] distances = [-30, -1000, 0, 1, int.MaxValue, int.MinValue];
            for (int i = 0; i < distances.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(6 + i * 4), distances[i]);
            }
            expected[^1] = 0x2A;
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(
                "BACK: nop\nswitch (BACK, -1000, 0, END, 2147483647, -2147483648)\nnop\nEND: ret", new Options { Optimize = optimize }));
            Assert.Equal(new byte[] { 0x45, 0, 0, 0, 0, 0x2A },
                DocumentCompilerTestHelpers.CompileMethodIL("switch ()\nret", new Options { Optimize = optimize }));
        }

        [Theory]
        [InlineData(254)]
        [InlineData(255)]
        [InlineData(256)]
        [InlineData(257)]
        [InlineData(4094)]
        [InlineData(4095)]
        [InlineData(4096)]
        [InlineData(4097)]
        public void ChunkBoundaries_PreserveOpcodesOperandsAndTokenFixups(int padding)
        {
            string instructions = string.Concat(Enumerable.Repeat("nop\n", padding)) + """
                ldarg.s arg
                pop
                readonly.
                ldtoken Test
                pop
                ldstr "x"
                pop
                br END
                .emitbyte 255
                END: ret
                """;
            foreach (bool optimize in new[] { false, true })
            {
                byte[] suffix = [0x26, 0xFE, 0x1E, 0xD0, 2, 0, 0, 2, 0x26, 0x72, 1, 0, 0, 0x70, 0x26, 0x38, 1, 0, 0, 0, 0xFF, 0x2A];
                byte[] argument = optimize ? [0x02] : [0x0E, 0];
                byte[] expected = new byte[padding].Concat(argument).Concat(suffix).ToArray();
                Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(instructions, new Options { Optimize = optimize }));
            }

        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FixedWidthAndMacroInstructions_AreNotRewritten(bool optimize)
        {
            const string instructions = """
                ldc.i4.m1
                ldarg.0
                ldloc.0
                stloc.0
                ldc.i8 1
                ldc.r4 1.0
                ldc.r8 1.0
                ret
                """;
            byte[] expected =
            [
                0x15, 0x02, 0x06, 0x0A,
                0x21, 1, 0, 0, 0, 0, 0, 0, 0,
                0x22, 0, 0, 0x80, 0x3F,
                0x23, 0, 0, 0, 0, 0, 0, 0xF0, 0x3F,
                0x2A,
            ];
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(instructions, new Options { Optimize = optimize }));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GlobalMethod_FixesBranchesAndExceptionLabels(bool optimize)
        {
            const string source = """
                .assembly test {}
                .method public static void M() cil managed
                {
                    .try START to END finally handler END to LAST
                    START: br END
                    nop
                    END: endfinally
                    LAST: ret
                }
                """;
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Optimize = optimize });
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe);
            Assert.Equal(new byte[] { 0x38, 1, 0, 0, 0, 0, 0xDC, 0x2A }, body.GetILBytes());
            ExceptionRegion region = Assert.Single(body.ExceptionRegions);
            Assert.Equal(6, region.TryLength);
            Assert.Equal(6, region.HandlerOffset);
            Assert.Equal(1, region.HandlerLength);
        }

        [Fact]
        public void GlobalMethod_ReportsShortBranchOverflow()
        {
            const string source = """
                .assembly test {}
                .method public static void M() cil managed
                {
                    br.s 128
                    ret
                }
                """;
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.BranchOffsetOutOfRange, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            Assert.Equal(new byte[] { 0x2B, 0x80, 0x2A }, DocumentCompilerTestHelpers.GetMethodBody(pe).GetILBytes());
        }

        [Theory]
        [InlineData(65)]
        [InlineData(1025)]
        public void SwitchTableAcrossChunks_FixesEveryOperand(int count)
        {
            string targets = "START, " + string.Join(", ", Enumerable.Repeat("END", count - 1));
            byte[] expected = new byte[7 + 4 * count];
            expected[1] = 0x45;
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(2), count);
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(6), -(6 + 4 * count));
            expected[^1] = 0x2A;
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(
                $"START: nop\nswitch ({targets})\nEND: ret", new Options()));
        }

        [Theory]
        [InlineData(63, true)]
        [InlineData(64, false)]
        public void MethodHeader_UsesFinalCodeSize(int codeSize, bool tiny)
        {
            string source = ".assembly test {} .method public static void M() cil managed { .maxstack 8\n"
                + string.Concat(Enumerable.Repeat("nop\n", codeSize - 1)) + "ret }";
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            int rva = reader.GetMethodDefinition(reader.MethodDefinitions.Single()).RelativeVirtualAddress;
            ImmutableArray<byte> raw = pe.GetSectionData(rva).GetContent();
            Assert.Equal(tiny ? 2 : 3, raw[0] & 3);
            if (tiny)
            {
                Assert.Equal(codeSize, raw[0] >> 2);
            }
            else
            {
                Assert.Equal(codeSize, BinaryPrimitives.ReadInt32LittleEndian(raw.AsSpan().Slice(4)));
                Assert.Equal(8, BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan().Slice(2)));
            }
            Assert.Equal(codeSize, DocumentCompilerTestHelpers.GetMethodBody(pe).GetILBytes()!.Length);
        }

        [Theory]
        [InlineData("ldc.i4 0", false, new byte[] { 0x20, 0, 0, 0, 0, 0x2A })]
        [InlineData("ldc.i4.s 0", false, new byte[] { 0x1F, 0, 0x2A })]
        [InlineData("ldc.i4 0", true, new byte[] { 0x16, 0x2A })]
        [InlineData("ldc.i4.s 0", true, new byte[] { 0x16, 0x2A })]
        [InlineData("ldarg 0", false, new byte[] { 0xFE, 0x09, 0, 0, 0x2A })]
        [InlineData("ldarg arg", false, new byte[] { 0xFE, 0x09, 0, 0, 0x2A })]
        [InlineData("ldarg 0", true, new byte[] { 0x02, 0x2A })]
        [InlineData("ldloc local", true, new byte[] { 0x06, 0x2A })]
        public void Optimize_SelectsInstructionForm(string instruction, bool optimize, byte[] expected)
        {
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(
                instruction + "\nret", new Options { Optimize = optimize }));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1000)]
        [InlineData(-6)]
        [InlineData(0)]
        [InlineData(1000)]
        [InlineData(int.MaxValue)]
        public void NumericBranch_PreservesRawOffset(int offset)
        {
            byte[] expected = new byte[6];
            expected[0] = 0x38;
            BinaryPrimitives.WriteInt32LittleEndian(expected.AsSpan(1), offset);
            expected[5] = 0x2A;
            Assert.Equal(expected, DocumentCompilerTestHelpers.CompileMethodIL(
                $"br {offset}\nret", new Options()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShortBranchOverflow_IsDiagnosedAndRetained(bool errorTolerant)
        {
            string source = DocumentCompilerTestHelpers.MethodSource("br.s END\n" +
                string.Concat(Enumerable.Repeat("nop\n", 128)) + "END: ret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { ErrorTolerant = errorTolerant });
            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.BranchOffsetOutOfRange, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal("br.s", source.Substring(diagnostic.Location.Span.Start, diagnostic.Location.Span.Length));
            if (!errorTolerant)
            {
                Assert.Null(result);
                return;
            }

            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            byte[] il = DocumentCompilerTestHelpers.GetMethodBody(pe).GetILBytes()!;
            Assert.Equal(131, il.Length);
            Assert.Equal(0x2B, il[0]);
            Assert.Equal(0x80, il[1]);
            Assert.All(il.AsSpan(2, 128).ToArray(), value => Assert.Equal(0, value));
            Assert.Equal(0x2A, il[^1]);
        }

        [Fact]
        public void Diagnostic_LabelNotFound()
        {
            // Reference an undefined label in a branch instruction
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        br UndefinedLabel
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LabelNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UndefinedBranchTarget_WithErrorTolerantOption_PreservesNativeFatHeaderBehavior(bool fold)
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 3
                        br UndefinedLabel
                        ret
                    }
                    .method public static void OtherMethod() cil managed
                    {
                        .maxstack 3
                        br UndefinedLabel
                        ret
                    }
                }
                """;

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true, Fold = fold });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.LabelNotFound);
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinition method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "TestMethod");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);
            MethodDefinition otherMethod = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "OtherMethod");

            Assert.Equal(3, body.MaxStack);
            Assert.True(body.LocalVariablesInitialized);
            Assert.Equal(fold, method.RelativeVirtualAddress == otherMethod.RelativeVirtualAddress);
            Assert.Equal(body.GetILBytes(), pe.GetMethodBody(otherMethod.RelativeVirtualAddress).GetILBytes());
        }

        [Fact]
        public void Diagnostic_SwitchLabelNotFound_PointsToInstruction()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        switch (UndefinedLabel)
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LabelNotFound, error.Id);
            Assert.Equal(source.IndexOf("switch", StringComparison.Ordinal), error.Location.Span.Start);
        }

        [Theory]
        [InlineData("ldc.i4")]
        [InlineData("ldc.i8")]
        [InlineData("ldarg")]
        [InlineData("br")]
        [InlineData("call instance void")]
        [InlineData("ldsfld int32")]
        [InlineData("ldsfld mdtoken(")]
        [InlineData("box")]
        [InlineData("ldtoken")]
        [InlineData("calli default void(")]
        [InlineData("calli vararg void(class [mscorlib]System.Tuple`1<method void *(int32 modreq(")]
        [InlineData("ldc.r8 float64(")]
        [InlineData("ldc.r8 bytearray(00 00")]
        [InlineData("ldstr \"A\" +")]
        [InlineData("ldstr ansi(\"A\"")]
        [InlineData("ldstr bytearray(48 00")]
        [InlineData("switch (L0,")]
        public void MalformedSimpleInstruction_DoesNotLeakMethodState(string malformedInstruction)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void Bad() cil managed
                    {
                        {{malformedInstruction}}
                    }

                    .method public static void Good() cil managed
                    {
                        ret
                    }
                }
                """;

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ =>
                {
                    Assert.Fail("Expected no includes");
                    return default;
                },
                _ =>
                {
                    Assert.Fail("Expected no resources");
                    return default;
                },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            byte[] il = GetMethodIL(pe, "Good");

            Assert.Equal([0x2A], il);
        }

        [Fact]
        public void MalformedCalliSignature_DoesNotMaterializeDiscardedReferences()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void Bad() cil managed
                    {
                        calli default void(class [Unused]Payload
                    }

                    .method public static void Good() cil managed
                    {
                        call void [Used]Target::M()
                        ret
                    }
                }
                """;

            DocumentCompiler compiler = new();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ =>
                {
                    Assert.Fail("Expected no includes");
                    return default;
                },
                _ =>
                {
                    Assert.Fail("Expected no resources");
                    return default;
                },
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(result);

            BlobBuilder image = new();
            result!.Serialize(image);
            using PEReader pe = new(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            string[] assemblyReferences = reader.AssemblyReferences
                .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
                .ToArray();

            Assert.Contains("Used", assemblyReferences);
            Assert.DoesNotContain("Unused", assemblyReferences);
            Assert.Equal([0x28, 0x01, 0x00, 0x00, 0x0A, 0x2A], GetMethodIL(pe, "Good"));
        }


        [Fact]
        public void DataLabelReference_FixedUpCorrectly()
        {
            // Test that .data with a reference to another label (&Label) is patched with the correct address
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data TargetData = int32(0x12345678)
                .data PointerData = &TargetData
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 8
                    .field [0] public static int32 Target at TargetData
                    .field [4] public static int32 Pointer at PointerData
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "DataHolder");

            var fields = testType.GetFields()
                .Select(reader.GetFieldDefinition)
                .ToDictionary(f => reader.GetString(f.Name));

            // Both fields should have RVAs
            int targetRva = fields["Target"].GetRelativeVirtualAddress();
            int pointerRva = fields["Pointer"].GetRelativeVirtualAddress();
            Assert.NotEqual(0, targetRva);
            Assert.NotEqual(0, pointerRva);

            // The pointer field should contain the address of the target data
            // Read the actual data from the PE at the pointer location
            var pointerSection = pe.GetSectionData(pointerRva);
            uint storedAddress = BinaryPrimitives.ReadUInt32LittleEndian(pointerSection.GetContent().AsSpan(0, 4));

            Assert.Equal(pe.PEHeaders.PEHeader!.ImageBase + (uint)targetRva, storedAddress);

            // Verify the target data contains the expected value
            var targetSection = pe.GetSectionData(targetRva);
            int targetValue = BinaryPrimitives.ReadInt32LittleEndian(targetSection.GetContent().AsSpan(0, 4));
            Assert.Equal(0x12345678, targetValue);
        }


        [Fact]
        public void DataLabelReference_MultipleReferences_AllFixedUp()
        {
            // Test multiple references to the same label
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data Target = int32(42)
                .data Ptr1 = &Target
                .data Ptr2 = &Target
                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 12
                    .field [0] public static int32 TargetField at Target
                    .field [4] public static int32 Ptr1Field at Ptr1
                    .field [8] public static int32 Ptr2Field at Ptr2
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var testType = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "DataHolder");

            var fields = testType.GetFields()
                .Select(reader.GetFieldDefinition)
                .ToDictionary(f => reader.GetString(f.Name));

            int targetRva = fields["TargetField"].GetRelativeVirtualAddress();
            int ptr1Rva = fields["Ptr1Field"].GetRelativeVirtualAddress();
            int ptr2Rva = fields["Ptr2Field"].GetRelativeVirtualAddress();

            // Read both pointer values
            uint storedAddress1 = BinaryPrimitives.ReadUInt32LittleEndian(pe.GetSectionData(ptr1Rva).GetContent().AsSpan(0, 4));
            uint storedAddress2 = BinaryPrimitives.ReadUInt32LittleEndian(pe.GetSectionData(ptr2Rva).GetContent().AsSpan(0, 4));

            // Both should point to the target
            ulong targetAddress = pe.PEHeaders.PEHeader!.ImageBase + (uint)targetRva;
            Assert.Equal(targetAddress, storedAddress1);
            Assert.Equal(targetAddress, storedAddress2);
        }


        [Fact]
        public void HexLabelName_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        br AA
                        nop
                    AA: ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void PrefixInstruction_Volatile_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .field public static int32 myField
                    .method public static void M() cil managed
                    {
                        volatile.
                        ldsfld int32 Test::myField
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void PrefixInstruction_Tail_ParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 M() cil managed
                    {
                        ldc.i4.0
                        tail.
                        call int32 Test::M()
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void LdelemU8_InstructionParsedCorrectly()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(unsigned int64[] arr) cil managed
                    {
                        ldarg.0
                        ldc.i4.0
                        ldelem.u8
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void UnusedInstruction_ParsedCorrectly()
        {
            string source = """
                .assembly test { }
                .method public static void F() cil managed
                {
                    IL_0000: unused
                    IL_0001: ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal([0xFE, 0x22, 0x2A], il);
        }

        [Fact]
        public void Ldtoken_NumericTokenEmittedCorrectly()
        {
            string source = """
                .assembly test { }
                .method public static void F() cil managed
                {
                    ldtoken 0
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal([0xD0, 0x00, 0x00, 0x00, 0x00, 0x2A], il);
        }

        [Fact]
        public void Calli_WritesOpcodeAndSignatureToken()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x29, il[0]);
            var signatureHandle = (StandaloneSignatureHandle)MetadataTokens.EntityHandle(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1)));
            Assert.Equal(0x2A, il[5]);

            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, signatureHandle);
            Assert.Equal(SignatureCallingConvention.Default, signature.Header.CallingConvention);
            Assert.False(signature.Header.IsInstance);
            Assert.False(signature.Header.HasExplicitThis);
            Assert.Equal(PrimitiveTypeCode.Void, signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);
        }

        [Theory]
        [InlineData("explicit instance")]
        [InlineData("instance explicit")]
        public void Calli_ExplicitInstance_PreservesExplicitThis(string callingConvention)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli {{callingConvention}} void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, MetadataTokens.StandaloneSignatureHandle(1));

            Assert.Equal(SignatureCallingConvention.Default, signature.Header.CallingConvention);
            Assert.True(signature.Header.IsInstance);
            Assert.True(signature.Header.HasExplicitThis);
            Assert.Equal(PrimitiveTypeCode.Void, signature.ReturnType);
            Assert.Empty(signature.ParameterTypes);
        }

        [Fact]
        public void Calli_VarArgSignature_DoesNotCountSentinelAsParameter()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        calli vararg void(int32, ..., string, int64)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            MethodSignature<PrimitiveTypeCode> signature = DecodeCalliSignature(reader, MetadataTokens.StandaloneSignatureHandle(1));

            Assert.Equal(SignatureCallingConvention.VarArgs, signature.Header.CallingConvention);
            Assert.Equal(1, signature.RequiredParameterCount);
            Assert.Equal([PrimitiveTypeCode.Int32, PrimitiveTypeCode.String, PrimitiveTypeCode.Int64], signature.ParameterTypes.ToArray());
        }

        [Fact]
        public void ReferenceAndCalliOperands_NestedSignaturesDecodeCorrectly()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldsfld method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string) class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>::Callback
                        pop
                        call void class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>::Invoke(method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string))
                        ldc.i4.0
                        conv.i
                        calli vararg void(class [mscorlib]System.Tuple`1<class [mscorlib]System.Tuple`1<int32>>, ..., method vararg void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string))
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            MemberReference fieldReference = reader.MemberReferences
                .Select(reader.GetMemberReference)
                .Single(reference => reader.GetString(reference.Name) == "Callback");
            Assert.Equal(
                "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)",
                fieldReference.DecodeFieldSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));

            MemberReference methodReference = reader.MemberReferences
                .Select(reader.GetMemberReference)
                .Single(reference => reader.GetString(reference.Name) == "Invoke");
            MethodSignature<string> methodSignature =
                methodReference.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("void", methodSignature.ReturnType);
            Assert.Equal(
                new[] { "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)" },
                methodSignature.ParameterTypes);

            MethodSignature<string> calliSignature = reader
                .GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1))
                .DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal(SignatureCallingConvention.VarArgs, calliSignature.Header.CallingConvention);
            Assert.Equal(1, calliSignature.RequiredParameterCount);
            Assert.Equal(
                new[]
                {
                    "[mscorlib]System.Tuple`1<[mscorlib]System.Tuple`1<int32>>",
                    "method void *(int32 modreq([mscorlib]System.Runtime.CompilerServices.IsVolatile), ..., string)",
                },
                calliSignature.ParameterTypes);
        }

        [Theory]
        [InlineData("ret")]
        [InlineData("ldc.i4.1\nlocalloc\npop\nret")]
        public void MaxStackDirective_BelowEightMatchesNativeFatHeaderBehavior(string instructions)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .maxstack 3
                        {{instructions}}
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");

            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);
            Assert.Equal(3, body.MaxStack);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Fact]
        public void MaxStackDirective_DoesNotInitializeExistingLocals()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .maxstack 3
                        .locals (int32 V_0)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(3, body.MaxStack);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void MethodWithoutMaxStack_UsesNativeDefault()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.i4.0
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(8, body.MaxStack);
            Assert.False(body.LocalVariablesInitialized);
        }

        [Fact]
        public void ZeroInitWithoutLocals_ForcesFatHeader()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        .zeroinit
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            MethodBodyBlock body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(8, body.MaxStack);
            Assert.True(body.LocalVariablesInitialized);
        }

        [Theory]
        [InlineData("4294967295", 4294967295d)]
        [InlineData("4503599627370496", 4503599627370496d)]
        [InlineData("-4294967295", -4294967295d)]
        [InlineData("0xFFFFFFFF", 4294967295d)]
        [InlineData("4294967295.", 4294967295d)]
        public void LdcR8_IntegerLiteral_PreservesValue(string literal, double expected)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.r8 {{literal}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x23, il[0]);
            Assert.Equal(expected, BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(il.AsSpan(1))));
        }

        [Theory]
        [InlineData("01", 1)]
        [InlineData("012", 10)]
        [InlineData("-012", -10)]
        public void LdcI4_MultiDigitOctalLiteral_EmitsExpectedValue(string literal, int expected)
        {
            string source = $$"""
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.i4 {{literal}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "F");

            int actual = il[0] switch
            {
                0x15 => -1,
                >= 0x16 and <= 0x1E => il[0] - 0x16,
                0x1F => unchecked((sbyte)il[1]),
                0x20 => BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1)),
                _ => throw new InvalidOperationException($"Unexpected ldc.i4 opcode 0x{il[0]:X2}"),
            };

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LdcR4_IntegerLiteral_PreservesValue()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .method public static void F() cil managed
                    {
                        ldc.r4 4294967295
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(method => reader.GetString(method.Name) == "F");
            byte[] il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;

            Assert.Equal(0x22, il[0]);
            Assert.Equal(4294967295f, BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1))));
        }

        private static MethodSignature<PrimitiveTypeCode> DecodeCalliSignature(MetadataReader reader, StandaloneSignatureHandle handle)
        {
            BlobReader blobReader = reader.GetBlobReader(reader.GetStandaloneSignature(handle).Signature);
            var decoder = new SignatureDecoder<PrimitiveTypeCode, object?>(PrimitiveTypeProvider.Instance, reader, genericContext: null);

            return decoder.DecodeMethodSignature(ref blobReader);
        }

        private sealed class PrimitiveTypeProvider : ISignatureTypeProvider<PrimitiveTypeCode, object?>
        {
            public static PrimitiveTypeProvider Instance { get; } = new();

            public PrimitiveTypeCode GetArrayType(PrimitiveTypeCode elementType, ArrayShape shape) => elementType;
            public PrimitiveTypeCode GetByReferenceType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetFunctionPointerType(MethodSignature<PrimitiveTypeCode> signature) => PrimitiveTypeCode.IntPtr;
            public PrimitiveTypeCode GetGenericInstantiation(PrimitiveTypeCode genericType, ImmutableArray<PrimitiveTypeCode> typeArguments) => genericType;
            public PrimitiveTypeCode GetGenericMethodParameter(object? genericContext, int index) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetGenericTypeParameter(object? genericContext, int index) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetModifiedType(PrimitiveTypeCode modifier, PrimitiveTypeCode unmodifiedType, bool isRequired) => unmodifiedType;
            public PrimitiveTypeCode GetPinnedType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetPointerType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;
            public PrimitiveTypeCode GetSZArrayType(PrimitiveTypeCode elementType) => elementType;
            public PrimitiveTypeCode GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
            public PrimitiveTypeCode GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => PrimitiveTypeCode.Object;
        }

        [Fact]
        public void Ldtoken_FieldReference_IsBackpatched()
        {
            string source = """
                .assembly Test { }
                .class public auto ansi Test
                {
                    .field public static int32 F
                    .method public static void M() cil managed
                    {
                        ldtoken field int32 Test::F
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.ldtoken);

            DocumentCompilerTestHelpers.AssertFieldDefToken(reader, token, "F");
        }


        [Fact]
        public void MethodNameF1_NotConfusedWithHexByte()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 f1() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            var methods = typeDef.GetMethods().ToArray();
            Assert.Single(methods);
            Assert.Equal("f1", reader.GetString(reader.GetMethodDefinition(methods[0]).Name));
        }


        [Fact]
        public void SwitchInstruction_NamedLabels_EmitsExpectedBranchTable()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        switch (L0, L1, L2)
                    L0: nop
                    L1: nop
                    L2: ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            int switchOffset = Array.IndexOf(il, (byte)0x45);

            Assert.True(switchOffset >= 0);
            Assert.Equal(3, BitConverter.ToInt32(il, switchOffset + 1));
            Assert.Equal(0, BitConverter.ToInt32(il, switchOffset + 5));
            Assert.Equal(1, BitConverter.ToInt32(il, switchOffset + 9));
            Assert.Equal(2, BitConverter.ToInt32(il, switchOffset + 13));
        }

        [Theory]
        [InlineData("ldc.r4", "1.5", 1.5)]
        [InlineData("ldc.r8", "1.5", 1.5)]
        [InlineData("ldc.r8", ".5", 0.5)]
        [InlineData("ldc.r8", "5e+1", 50.0)]
        [InlineData("ldc.r8", "-1.25e-2", -0.0125)]
        [InlineData("ldc.r8", "float32(0x3F800000)", 1.0)]
        public void FloatingPointInstruction_TextAndFloat32BitForms_EmitExpectedValue(
            string opcode,
            string literal,
            double expected)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{opcode}} {{literal}}
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            if (opcode == "ldc.r4")
            {
                Assert.Equal(0x22, il[0]);
                Assert.Equal((float)expected, BitConverter.ToSingle(il, 1));
            }
            else
            {
                Assert.Equal(0x23, il[0]);
                Assert.Equal(expected, BitConverter.ToDouble(il, 1));
            }
        }

        [Fact]
        public void FloatingPointInstruction_Float64BitPattern_EmitsExpectedDouble()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float64 GetPi() cil managed
                    {
                        ldc.r8 float64(0x400921FB54442D18)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "GetPi");

            Assert.Equal(0x23, il[0]);
            Assert.Equal(Math.PI, BitConverter.ToDouble(il, 1), 14);
        }

        [Fact]
        public void FloatingPointInstruction_IntegerOverflow_ReportsDiagnostic()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        ldc.r8 99999999999999999999999999999999
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LiteralOutOfRange, error.Id);
        }

        [Fact]
        public void FloatingPointInstruction_ByteForms_EmitExpectedConstants()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static float32 GetSingle() cil managed
                    {
                        ldc.r4 (00 00 80 3F)
                        ret
                    }

                    .method public static float64 GetDouble() cil managed
                    {
                        ldc.r8 bytearray(00 00 00 00 00 00 F0 3F)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] singleIL = GetMethodIL(pe, "GetSingle");
            byte[] doubleIL = GetMethodIL(pe, "GetDouble");

            Assert.Equal(0x22, singleIL[0]);
            Assert.Equal(1f, BitConverter.ToSingle(singleIL, 1));
            Assert.Equal(0x23, doubleIL[0]);
            Assert.Equal(1d, BitConverter.ToDouble(doubleIL, 1));
        }

        [Fact]
        public void CalliInstruction_EmitsStandaloneSignatureToken()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 M(native int functionPointer) cil managed
                    {
                        ldarg.0
                        calli default int32()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.StandAloneSig));

            var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(1));
            MethodSignature<string> decodedSignature =
                signature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal(SignatureCallingConvention.Default, decodedSignature.Header.CallingConvention);
            Assert.Equal("int32", decodedSignature.ReturnType);
            Assert.Empty(decodedSignature.ParameterTypes);

            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "M", ILOpcode.calli);
            Assert.Equal(MetadataTokens.GetToken(MetadataTokens.StandaloneSignatureHandle(1)), token);
        }

        [Theory]
        [InlineData("unmanaged cdecl", (int)SignatureCallingConvention.CDecl, false, false)]
        [InlineData("unmanaged stdcall", (int)SignatureCallingConvention.StdCall, false, false)]
        [InlineData("unmanaged thiscall", (int)SignatureCallingConvention.ThisCall, false, false)]
        [InlineData("unmanaged fastcall", (int)SignatureCallingConvention.FastCall, false, false)]
        [InlineData("unmanaged", (int)SignatureCallingConvention.Unmanaged, false, false)]
        [InlineData("vararg", (int)SignatureCallingConvention.VarArgs, false, false)]
        [InlineData("instance default", (int)SignatureCallingConvention.Default, true, false)]
        [InlineData("callconv(0x05)", (int)SignatureCallingConvention.VarArgs, false, false)]
        public void CalliInstruction_CallingConvention_EmitsDecodedSignature(
            string callingConvention,
            int expectedCallingConvention,
            bool expectedInstance,
            bool expectedExplicitThis)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        ldc.i4.0
                        conv.i
                        calli {{callingConvention}} void()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var signature = reader.GetStandaloneSignature(Assert.Single(
                Enumerable.Range(1, reader.GetTableRowCount(TableIndex.StandAloneSig))
                    .Select(MetadataTokens.StandaloneSignatureHandle)));
            MethodSignature<string> decoded =
                signature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);

            Assert.Equal((SignatureCallingConvention)expectedCallingConvention, decoded.Header.CallingConvention);
            Assert.Equal(expectedInstance, decoded.Header.IsInstance);
            Assert.True(
                decoded.Header.HasExplicitThis == expectedExplicitThis,
                $"Expected explicit-this={expectedExplicitThis}, header=0x{decoded.Header.RawValue:X2}");
            Assert.Equal("void", decoded.ReturnType);
            Assert.Empty(decoded.ParameterTypes);
        }

        [Fact]
        public void LdstrInstruction_ComposedAnsiAndRawForms_EmitExpectedUserStrings()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static string GetUtf16() cil managed
                    {
                        ldstr bytearray(48 00 69 00)
                        ret
                    }

                    .method public static string GetAnsi() cil managed
                    {
                        ldstr ansi("A" + "B")
                        ret
                    }

                    .method public static string GetOddAnsi() cil managed
                    {
                        ldstr ansi("A" + "BC")
                        ret
                    }

                    .method public static string GetComposed() cil managed
                    {
                        ldstr "A" + "B"
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("Hi", ReadLdstrValue(pe, reader, "GetUtf16"));
            Assert.Equal("\u4241", ReadLdstrValue(pe, reader, "GetAnsi"));
            Assert.Equal("\u4241\u0043", ReadLdstrValue(pe, reader, "GetOddAnsi"));
            Assert.Equal("AB", ReadLdstrValue(pe, reader, "GetComposed"));
        }

        [Fact]
        public void Ldstr_WithControlAndQuotedEscapes_EmitsExpectedUserStrings()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public static string ControlEscapes() cil managed
                    {
                        ldstr "A\bB\fC\vD\aE\?F\'G"
                        ret
                    }

                    .method public static string QuotedLiteral() cil managed
                    {
                        ldstr "double\"quoted\?"
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("A\bB\fC\vD\aE?F'G", ReadLdstrValue(pe, reader, "ControlEscapes"));
            Assert.Equal("double\"quoted?", ReadLdstrValue(pe, reader, "QuotedLiteral"));
        }

        [Fact]
        public void Ldstr_WithLineContinuationAndShortOrHighOctalEscapes_EmitsExpectedUserString()
        {
            string source =
                ".assembly extern System.Runtime { }\n" +
                ".assembly test { }\n" +
                ".class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object\n" +
                "{\n" +
                "    .method public static string FallbackEscapes() cil managed\n" +
                "    {\n" +
                "        ldstr \"line\\\n" +
                "              continued\\12\\400!\"\n" +
                "        ret\n" +
                "    }\n" +
                "}\n";

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            Assert.Equal("linecontinued12400!", ReadLdstrValue(pe, reader, "FallbackEscapes"));
        }

        [Fact]
        public void SwitchInstruction_IntegerOffsets_EmitsExpectedBranchTable()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static int32 M(int32 'value') cil managed
                    {
                        ldarg.0
                        switch (3, 6)
                        ldc.i4.0
                        ret
                        ldc.i4.1
                        ret
                        ldc.i4.2
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");
            int switchOffset = Array.IndexOf(il, (byte)0x45);

            Assert.True(switchOffset >= 0);
            Assert.Equal(2, BitConverter.ToInt32(il, switchOffset + 1));
            Assert.Equal(3, BitConverter.ToInt32(il, switchOffset + 5));
            Assert.Equal(6, BitConverter.ToInt32(il, switchOffset + 9));
        }

        [Theory]
        [InlineData("switch ()")]
        [InlineData("switch ( )")]
        public void SwitchInstruction_Empty_EmitsEmptyBranchTable(string instruction)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void M() cil managed
                    {
                        {{instruction}}
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            byte[] il = GetMethodIL(pe, "M");

            Assert.Equal(0x45, il[0]);
            Assert.Equal(0, BitConverter.ToInt32(il, 1));
        }

        [Fact]
        public void LdtokenInstruction_TypeReference_EmitsTypeReferenceToken()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static valuetype [mscorlib]System.RuntimeTypeHandle GetHandle() cil managed
                    {
                        ldtoken [mscorlib]System.Int32
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            byte[] il = GetMethodIL(pe, "GetHandle");

            Assert.Equal(0xD0, il[0]);
            int token = BitConverter.ToInt32(il, 1);
            var handle = MetadataTokens.EntityHandle(token);
            Assert.Equal(HandleKind.TypeReference, handle.Kind);
            Assert.Equal("Int32", reader.GetString(reader.GetTypeReference((TypeReferenceHandle)handle).Name));
        }

        [Fact]
        public void InstructionOperandForms_EmitExpectedTokensAndRawOperands()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 Value

                    .method public specialname rtspecialname instance void .ctor() cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }

                    .method public static void Exercise(object 'value') cil managed
                    {
                        br 0
                        ldsfld mdtoken(0x04000001)
                        pop
                        ldsfld int32 Test::Value
                        pop
                        ldsfld mdtoken(0x01000001)
                        pop
                        ldarg.0
                        unaligned. 1
                        ldind.i4
                        pop
                        ldarg.0
                        callvirt instance string [mscorlib]System.Object::ToString()
                        pop
                        newobj instance void Test::.ctor()
                        pop
                        ldc.i4.0
                        ldnull
                        ldc.i4.0
                        conv.i
                        calli default void(int32, string)
                        ldtoken Test
                        pop
                        switch ()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            byte[] il = GetMethodIL(pe, "Exercise");

            ImmutableArray<int> fieldTokens =
                DocumentCompilerTestHelpers.GetTokenOperands(pe, reader, "Exercise", ILOpcode.ldsfld);
            Assert.Equal(3, fieldTokens.Length);
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(fieldTokens[0]).Kind);
            Assert.Equal(HandleKind.FieldDefinition, MetadataTokens.EntityHandle(fieldTokens[1]).Kind);
            Assert.Equal(HandleKind.TypeReference, MetadataTokens.EntityHandle(fieldTokens[2]).Kind);

            int callvirtToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.callvirt);
            Assert.Equal(
                "ToString",
                reader.GetString(reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(callvirtToken)).Name));

            int newobjToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.newobj);
            Assert.Equal(
                ".ctor",
                reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(newobjToken)).Name));

            int typeToken =
                DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Exercise", ILOpcode.ldtoken);
            Assert.Equal(
                "Test",
                reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(typeToken)).Name));

            var calliSignature = reader.GetStandaloneSignature(
                MetadataTokens.StandaloneSignatureHandle(reader.GetTableRowCount(TableIndex.StandAloneSig)));
            MethodSignature<string> decodedCalli =
                calliSignature.DecodeMethodSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null);
            Assert.Equal("void", decodedCalli.ReturnType);
            Assert.Equal(new[] { "int32", "string" }, decodedCalli.ParameterTypes);

            Assert.True(ContainsSequence(il, [0x38, 0x00, 0x00, 0x00, 0x00]));
            Assert.True(ContainsSequence(il, [0xFE, 0x12, 0x01]));
            Assert.True(ContainsSequence(il, [0x45, 0x00, 0x00, 0x00, 0x00]));
        }


        [Fact]
        public void FieldRVA_DataLabelEmitted()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .data D_1 = int32(42)
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .field public static int32 myData at D_1
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // The FieldRVA table should have an entry
            int fieldRvaCount = reader.GetTableRowCount(TableIndex.FieldRva);
            Assert.True(fieldRvaCount >= 1, $"FieldRVA table should have at least 1 entry, has {fieldRvaCount}");
        }

        private static byte[] GetMethodIL(PEReader pe, string methodName)
        {
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == methodName);
            return pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        }

        private static string ReadLdstrValue(PEReader pe, MetadataReader reader, string methodName)
        {
            byte[] il = GetMethodIL(pe, methodName);
            Assert.Equal(0x72, il[0]);
            int token = BitConverter.ToInt32(il, 1);
            Assert.Equal(0x70, (token >> 24) & 0xFF);
            return reader.GetUserString(MetadataTokens.UserStringHandle(token & 0x00FFFFFF));
        }

        private static bool ContainsSequence(byte[] bytes, ReadOnlySpan<byte> sequence)
        {
            for (int i = 0; i <= bytes.Length - sequence.Length; i++)
            {
                if (bytes.AsSpan(i, sequence.Length).SequenceEqual(sequence))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
