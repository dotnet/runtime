// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class DataTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void DataDeclaration_AtEndOfDocument_EmitsFieldRva(string suffix)
        {
            string source = """
                .assembly test {}
                .class public DataHolder
                {
                    .field public static int32 Value at ValueData
                }
                .data ValueData = int32(42)
                """ + suffix;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            FieldDefinition field = reader.GetFieldDefinition(Assert.Single(reader.FieldDefinitions));
            Assert.Equal(".sdata", pe.PEHeaders.SectionHeaders[
                pe.PEHeaders.GetContainingSectionIndex(field.GetRelativeVirtualAddress())].Name);
            Assert.Equal(new byte[] { 42, 0, 0, 0 }, ReadData(pe, field, sizeof(int)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void DataItems_EmitExpectedTypedBytesAndRepeatCounts(string section)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }

                .data {{section}} D_STRING = char*("Hi")
                .data {{section}} D_BYTES = bytearray(01 02 03 04)
                .data {{section}} D_BRACES = { int32(0x11223344), int16(0x5566), int8(0x77) }
                .data {{section}} D_REPEAT = int32(7)[3]
                .data {{section}} D_F32 = float32(1.5)
                .data {{section}} D_F64 = float64(2.25)
                .data {{section}} D_ZERO8 = int8[3]
                .data {{section}} D_ZERO16 = int16[2]
                .data {{section}} D_ZERO32 = int32[2]
                .data {{section}} D_ZERO64 = int64[2]
                .data {{section}} D_ZEROF32 = float32[2]
                .data {{section}} D_ZEROF64 = float64[2]

                .class public explicit ansi sealed DataHolder extends [mscorlib]System.ValueType
                {
                    .size 64
                    .field [0] public static int8 StringData at D_STRING
                    .field [4] public static int8 ByteData at D_BYTES
                    .field [8] public static int8 BracedData at D_BRACES
                    .field [16] public static int8 RepeatedData at D_REPEAT
                    .field [28] public static int8 Float32Data at D_F32
                    .field [32] public static int8 Float64Data at D_F64
                    .field [40] public static int8 Zero8Data at D_ZERO8
                    .field [44] public static int8 Zero16Data at D_ZERO16
                    .field [48] public static int8 Zero32Data at D_ZERO32
                    .field [52] public static int8 Zero64Data at D_ZERO64
                    .field [56] public static int8 ZeroFloat32Data at D_ZEROF32
                    .field [60] public static int8 ZeroFloat64Data at D_ZEROF64
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));

            Assert.Equal(
                [0x48, 0x00, 0x69, 0x00],
                ReadData(pe, fields["StringData"], 4));
            Assert.Equal(
                [0x01, 0x02, 0x03, 0x04],
                ReadData(pe, fields["ByteData"], 4));
            Assert.Equal(
                [0x44, 0x33, 0x22, 0x11, 0x66, 0x55, 0x77],
                ReadData(pe, fields["BracedData"], 7));

            byte[] repeatedData = ReadData(pe, fields["RepeatedData"], 12);
            Assert.Equal(new[] { 7, 7, 7 }, MemoryMarshal.Cast<byte, int>(repeatedData).ToArray());
            Assert.Equal(1.5f, BitConverter.ToSingle(ReadData(pe, fields["Float32Data"], 4)));
            Assert.Equal(2.25, BitConverter.ToDouble(ReadData(pe, fields["Float64Data"], 8)));

            Assert.All(ReadData(pe, fields["Zero8Data"], 3), value => Assert.Equal(0, value));
            Assert.All(ReadData(pe, fields["Zero16Data"], 4), value => Assert.Equal(0, value));
            Assert.All(ReadData(pe, fields["Zero32Data"], 8), value => Assert.Equal(0, value));
            Assert.All(ReadData(pe, fields["Zero64Data"], 16), value => Assert.Equal(0, value));
            Assert.All(ReadData(pe, fields["ZeroFloat32Data"], 8), value => Assert.Equal(0, value));
            Assert.All(ReadData(pe, fields["ZeroFloat64Data"], 16), value => Assert.Equal(0, value));
        }

        [Fact]
        public void ClassScopedDataDirective_EmitsFieldRvaValue()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public explicit ansi sealed DataHolder extends [mscorlib]System.ValueType
                {
                    .size 4
                    .data ClassData = int32(1234)
                    .field [0] public static int32 Value at ClassData
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(Assert.Single(reader.FieldDefinitions));

            Assert.Equal(
                1234,
                BitConverter.ToInt32(
                    pe.GetSectionData(field.GetRelativeVirtualAddress()).GetContent().AsSpan(0, sizeof(int))));
        }

        [Fact]
        public void DataDirectives_EmitExpectedMappedFieldDataBytes()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .data IntData = int32(0x12345678)
                .data ByteData = bytearray (AA BB CC DD)
                .data FloatData = float32(3.5)

                .class public explicit ansi sealed beforefieldinit DataHolder extends [mscorlib]System.ValueType
                {
                    .size 16
                    .field [0] public static int32 IntField at IntData
                    .field [4] public static int32 ByteField at ByteData
                    .field [8] public static float32 FloatField at FloatData
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(definition => reader.GetString(definition.Name) == "DataHolder");
            var fields = new Dictionary<string, FieldDefinition>();

            foreach (var fieldHandle in type.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                fields.Add(reader.GetString(field.Name), field);
            }

            int intRva = fields["IntField"].GetRelativeVirtualAddress();
            int byteRva = fields["ByteField"].GetRelativeVirtualAddress();
            int floatRva = fields["FloatField"].GetRelativeVirtualAddress();

            Assert.Equal([0x78, 0x56, 0x34, 0x12], pe.GetSectionData(intRva).GetContent().Take(4).ToArray());
            Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], pe.GetSectionData(byteRva).GetContent().Take(4).ToArray());
            Assert.Equal([0x00, 0x00, 0x60, 0x40], pe.GetSectionData(floatRva).GetContent().Take(4).ToArray());
        }

        [Fact]
        public void Diagnostic_InvalidMetadataToken()
        {
            // Reference an invalid token in an exported type declaration
            // Uses an assembly reference instead of a file to avoid file entry point issues
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern ForwardedAssembly
                    mdtoken(0x99999999)
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidMetadataToken, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData(Machine.I386, false)]
        [InlineData(Machine.I386, true)]
        [InlineData(Machine.Amd64, false)]
        [InlineData(Machine.Amd64, true)]
        [InlineData(Machine.Arm64, false)]
        [InlineData(Machine.Arm64, true)]
        public void TlsData_EmitsDirectoryAndFieldRvas(Machine machine, bool dll)
        {
            string source = """
                .assembly test { }
                .data tls int8(0xAA)
                .data tls First = int32(0x12345678)
                .class public DataHolder
                {
                    .data tls Second = int16(0x1122)
                    .field public static int16 SecondField at Second
                    .field public static int32 FirstField at First
                    .field public static int32 AliasField at First
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source, new Options { Machine = machine, Dll = dll });
            SectionHeader tls = Assert.Single(pe.PEHeaders.SectionHeaders, section => section.Name == ".tls");
            Assert.Equal(
                SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite |
                SectionCharacteristics.ContainsInitializedData,
                tls.SectionCharacteristics);
            Assert.Equal(7, tls.VirtualSize);
            Assert.Equal(
                new byte[] { 0xAA, 0x78, 0x56, 0x34, 0x12, 0x22, 0x11 },
                pe.GetSectionData(tls.VirtualAddress).GetContent(0, tls.VirtualSize));

            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, FieldDefinition> fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));
            Assert.Equal(tls.VirtualAddress + 5, fields["SecondField"].GetRelativeVirtualAddress());
            Assert.Equal(tls.VirtualAddress + 1, fields["FirstField"].GetRelativeVirtualAddress());
            Assert.Equal(tls.VirtualAddress + 1, fields["AliasField"].GetRelativeVirtualAddress());

            PEHeader header = pe.PEHeaders.PEHeader!;
            DirectoryEntry directory = header.ThreadLocalStorageTableDirectory;
            int pointerSize = machine == Machine.I386 ? sizeof(uint) : sizeof(ulong);
            Assert.Equal((4 * pointerSize) + (2 * sizeof(uint)), directory.Size);
            Assert.Equal(".sdata", pe.PEHeaders.SectionHeaders[
                pe.PEHeaders.GetContainingSectionIndex(directory.RelativeVirtualAddress)].Name);
            ReadOnlySpan<byte> data = pe.GetSectionData(directory.RelativeVirtualAddress)
                .GetContent(0, directory.Size).AsSpan();
            Assert.Equal(header.ImageBase + (uint)tls.VirtualAddress, ReadPointer(0));
            Assert.Equal(header.ImageBase + (uint)(tls.VirtualAddress + tls.VirtualSize), ReadPointer(pointerSize));
            int indexRva = checked((int)(ReadPointer(2 * pointerSize) - header.ImageBase));
            int callbacksRva = checked((int)(ReadPointer(3 * pointerSize) - header.ImageBase));
            Assert.Equal(0U, BinaryPrimitives.ReadUInt32LittleEndian(pe.GetSectionData(indexRva).GetContent().AsSpan()));
            Assert.All(pe.GetSectionData(callbacksRva).GetContent(0, pointerSize), value => Assert.Equal(0, value));
            Assert.Equal(0U, BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4 * pointerSize)));
            Assert.Equal(0U, BinaryPrimitives.ReadUInt32LittleEndian(data.Slice((4 * pointerSize) + sizeof(uint))));
            Assert.False(pe.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly));
            Assert.Equal(machine == Machine.I386, pe.PEHeaders.CorHeader.Flags.HasFlag(CorFlags.Requires32Bit));

            ulong ReadPointer(int offset) => pointerSize == sizeof(uint)
                ? BinaryPrimitives.ReadUInt32LittleEndian(pe.GetSectionData(directory.RelativeVirtualAddress + offset).GetContent().AsSpan())
                : BinaryPrimitives.ReadUInt64LittleEndian(pe.GetSectionData(directory.RelativeVirtualAddress + offset).GetContent().AsSpan());
        }

        [Theory]
        [InlineData("", ".sdata")]
        [InlineData("cil", ".text")]
        [InlineData("tls", ".tls")]
        public void DataSection_PreservesAnonymousAndMethodScopedData(string section, string expectedSection)
        {
            string source = $$"""
                .assembly test { }
                .data {{section}} int8(0xAA)
                .data {{section}} Value = int32(42)
                .field public static int32 ValueField at Value
                .field public static int16 MethodField at MethodData
                .method public static void M() cil managed
                {
                    .data {{section}} MethodData = int16(1234)
                    ret
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            FieldDefinition[] fields = reader.FieldDefinitions.Select(reader.GetFieldDefinition).ToArray();
            Assert.Equal(expectedSection, pe.PEHeaders.SectionHeaders[
                pe.PEHeaders.GetContainingSectionIndex(fields[0].GetRelativeVirtualAddress())].Name);
            Assert.Equal(fields[0].GetRelativeVirtualAddress() + sizeof(int), fields[1].GetRelativeVirtualAddress());
            Assert.Equal(42, BitConverter.ToInt32(ReadData(pe, fields[0], sizeof(int))));
            Assert.Equal(1234, BitConverter.ToInt16(ReadData(pe, fields[1], sizeof(short))));
            Assert.Equal(section != "tls", pe.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly));
            if (section == "cil")
            {
                Assert.DoesNotContain(pe.PEHeaders.SectionHeaders, header => header.Name is ".sdata" or ".tls");
            }
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("", 1)]
        [InlineData("cil", 0)]
        [InlineData("cil", 1)]
        [InlineData("tls", 0)]
        [InlineData("tls", 1)]
        public void AnonymousData_EmitsSectionsOnlyForNonemptyNonCilData(string section, int count)
        {
            string source = $$"""
                .assembly test { }
                .data {{section}} int32[{{count}}]
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            bool hasTlsData = section == "tls" && count != 0;
            Assert.Equal(hasTlsData, pe.PEHeaders.SectionHeaders.Any(header => header.Name == ".tls"));
            Assert.Equal(hasTlsData, pe.PEHeaders.PEHeader!.ThreadLocalStorageTableDirectory.Size != 0);
            Assert.Equal(!hasTlsData, pe.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly));
            Assert.Equal(
                section != "cil" && count != 0,
                pe.PEHeaders.SectionHeaders.Any(header => header.Name == ".sdata"));
        }

        [Fact]
        public void ExecutableWithMappedFieldData_HasExecutablePeHeaderAndFieldRva()
        {
            string source = """
                .mscorlib
                .assembly dataseg { }
                .field static int8 result at ResultData
                .data ResultData = bytearray (63)
                .method static int32 Main()
                {
                    .entrypoint
                    ldsfld int8 result
                    ldc.i4.1
                    add
                    ret
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options { OutputFileName = "dataseg.exe" });

            Assert.True(pe.PEHeaders.IsExe);
            Assert.Equal(
                Characteristics.ExecutableImage | Characteristics.Bit32Machine,
                pe.PEHeaders.CoffHeader.Characteristics);

            MetadataReader reader = pe.GetMetadataReader();
            FieldDefinition field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(FieldAttributes.Static | FieldAttributes.HasFieldRVA, field.Attributes);
            Assert.Equal(
                0x63,
                pe.GetSectionData(field.GetRelativeVirtualAddress()).GetContent(0, 1)[0]);
        }

        [Fact]
        public void Float64Data_IntegerLiteral_PreservesValue()
        {
            string source = """
                .assembly test { }
                .data D = float64(4503599627370496.)
                .class public auto ansi Test
                {
                    .field public static float64 Value at D
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            ReadOnlySpan<byte> data = pe.GetSectionData(field.GetRelativeVirtualAddress()).GetContent().AsSpan(0, sizeof(double));

            Assert.Equal(4503599627370496d, BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void LargeByteArray_StreamsIntoDataSectionWithoutLosingBytes(string section)
        {
            const int Length = 64 * 1024;
            byte[] expected = new byte[Length];
            for (int i = 0; i < Length; i++)
            {
                expected[i] = (byte)((i * 31) + 7);
            }

            StringBuilder literal = new(Length * 3);
            for (int i = 0; i < Length; i++)
            {
                if (i > 0)
                {
                    literal.Append(i % 32 == 0 ? '\n' : ' ');
                }

                literal.Append(expected[i].ToString("X2", CultureInfo.InvariantCulture));
            }

            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }

                .data {{section}} D_LARGE = bytearray ({{literal}})

                .class public explicit ansi sealed DataHolder extends [mscorlib]System.ValueType
                {
                    .size 8
                    .field [0] public static int8 LargeData at D_LARGE
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .Single(definition => reader.GetString(definition.Name) == "LargeData");

            Assert.Equal(expected, ReadData(pe, field, Length));
        }

        [Fact]
        public void DataDeclaration_SyntaxVariantsPreserveOffsetsAndReferenceFixups()
        {
            string source = """
                .assembly test { }
                .data int8(0x7A)
                .data cil CilData = int8(0x22)
                .data TargetData = int32(0x12345678)
                .data ParenthesizedReference = &(TargetData)

                .class public explicit ansi sealed DataHolder
                {
                    .field [0] public static int8 CilValue at CilData
                    .field [1] public static int32 TargetValue at TargetData
                    .field [5] public static int32 ParenthesizedValue at ParenthesizedReference
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, FieldDefinition> fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));

            int cilRva = fields["CilValue"].GetRelativeVirtualAddress();
            int targetRva = fields["TargetValue"].GetRelativeVirtualAddress();
            int parenthesizedRva = fields["ParenthesizedValue"].GetRelativeVirtualAddress();

            Assert.Equal(".text", pe.PEHeaders.SectionHeaders[
                pe.PEHeaders.GetContainingSectionIndex(cilRva)].Name);
            Assert.Equal(".sdata", pe.PEHeaders.SectionHeaders[
                pe.PEHeaders.GetContainingSectionIndex(targetRva)].Name);
            Assert.Equal(targetRva + sizeof(int), parenthesizedRva);
            Assert.Equal(0x22, Assert.Single(ReadData(pe, fields["CilValue"], 1)));
            Assert.Equal(0x12345678, BitConverter.ToInt32(ReadData(pe, fields["TargetValue"], sizeof(int))));
            Assert.Equal(
                pe.PEHeaders.PEHeader!.ImageBase + (uint)targetRva,
                BitConverter.ToUInt32(ReadData(pe, fields["ParenthesizedValue"], sizeof(int))));
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void MalformedDataDeclaration_DoesNotLeakBytesOrLabelIntoNextDocument(string section)
        {
            ImmutableArray<SourceText> documents =
            [
                new SourceText($$"""
                    .assembly test { }
                    .data {{section}} Broken = { int8(0xAA), }
                    """, "broken.il"),
                new SourceText("""
                    .data Good = int32(0x12345678)
                    .class public auto ansi DataHolder
                    {
                        .field public static int8 Missing at Broken
                        .field public static int32 Value at Good
                    }
                    """, "valid.il"),
            ];

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, FieldDefinition> fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));

            Assert.Equal(0, fields["Missing"].GetRelativeVirtualAddress());
            Assert.Equal(
                0x12345678,
                BitConverter.ToInt32(ReadData(pe, fields["Value"], sizeof(int))));
        }

        [Fact]
        public void OutOfRangeDataRepeatCount_DoesNotAliasFollowingData()
        {
            string source = """
                .assembly test { }
                .data Broken = int8[999999999999999999999999]
                .data Good = int32(0x12345678)
                .class public auto ansi DataHolder
                {
                    .field public static int8 Missing at Broken
                    .field public static int32 Value at Good
                }
                """;

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.LiteralOutOfRange, diagnostic.Id);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, FieldDefinition> fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));

            Assert.Equal(0, fields["Missing"].GetRelativeVirtualAddress());
            Assert.Equal(
                0x12345678,
                BitConverter.ToInt32(ReadData(pe, fields["Value"], sizeof(int))));
        }

        private static byte[] ReadData(PEReader pe, FieldDefinition field, int length)
        {
            int rva = field.GetRelativeVirtualAddress();
            Assert.NotEqual(0, rva);
            return pe.GetSectionData(rva).GetContent().AsSpan(0, length).ToArray();
        }
    }
}
