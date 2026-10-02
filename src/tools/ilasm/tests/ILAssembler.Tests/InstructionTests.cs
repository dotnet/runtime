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
        [InlineData(124, false)]
        [InlineData(126, false)]
        public void BackwardBranchOptimization_UsesNativeConservativeLimit(int padding, bool shortened)
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


        [Fact]
        public void DataLabelReference_FixedUpCorrectly()
        {
            // Test that .data with a reference to another label (&Label) is patched with the correct RVA
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

            // The pointer field should contain the RVA of the target data
            // Read the actual data from the PE at the pointer location
            var pointerSection = pe.GetSectionData(pointerRva);
            int storedRva = BinaryPrimitives.ReadInt32LittleEndian(pointerSection.GetContent().AsSpan(0, 4));

            // The stored RVA should equal the target's RVA
            Assert.Equal(targetRva, storedRva);

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
            int storedRva1 = BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(ptr1Rva).GetContent().AsSpan(0, 4));
            int storedRva2 = BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(ptr2Rva).GetContent().AsSpan(0, 4));

            // Both should point to the target
            Assert.Equal(targetRva, storedRva1);
            Assert.Equal(targetRva, storedRva2);
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
        public void SwitchInstruction_CommaLabels()
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

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
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

    }
}
