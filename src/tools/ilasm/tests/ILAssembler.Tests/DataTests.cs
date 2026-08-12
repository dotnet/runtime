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
        [Fact]
        public void DataItems_EmitExpectedTypedBytesAndRepeatCounts()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .data D_STRING = char*("Hi")
                .data D_BYTES = bytearray(01 02 03 04)
                .data D_BRACES = { int32(0x11223344), int16(0x5566), int8(0x77) }
                .data D_REPEAT = int32(7)[3]
                .data D_F32 = float32(1.5)
                .data D_F64 = float64(2.25)
                .data D_ZERO8 = int8[3]
                .data D_ZERO16 = int16[2]
                .data D_ZERO32 = int32[2]
                .data D_ZERO64 = int64[2]
                .data D_ZEROF32 = float32[2]
                .data D_ZEROF64 = float64[2]

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

        [Fact]
        public void LargeByteArray_StreamsIntoDataSectionWithoutLosingBytes()
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

                .data D_LARGE = bytearray ({{literal}})

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

        private static byte[] ReadData(PEReader pe, FieldDefinition field, int length)
        {
            int rva = field.GetRelativeVirtualAddress();
            Assert.NotEqual(0, rva);
            return pe.GetSectionData(rva).GetContent().AsSpan(0, length).ToArray();
        }
    }
}
