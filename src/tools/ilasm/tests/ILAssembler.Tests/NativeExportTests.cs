// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class NativeExportTests
    {
        [Fact]
        public void ExportDirective_WithoutVTableFixup_DoesNotEmitNativeExportArtifacts()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .export [1] as MyExport
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.False(pe.PEHeaders.SectionHeaders.Any(section => section.Name == ".sdata"));

            DirectoryEntry exportTableDirectory = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            Assert.Equal(0, exportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, exportTableDirectory.Size);
        }

        [Fact]
        public void VTableFixupAndExport_EmitClrAndPeDirectories()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Zed() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as Zed
                        ret
                    }

                    .method public static void Alpha() cil managed
                    {
                        .vtentry 1 : 2
                        .export [3] as Alpha
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var sdataSection = pe.PEHeaders.SectionHeaders.Single(section => section.Name == ".sdata");
            DirectoryEntry vtableFixups = pe.PEHeaders.CorHeader!.VtableFixupsDirectory;
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;

            Assert.Equal(sdataSection.VirtualAddress, vtableFixups.RelativeVirtualAddress);
            Assert.Equal(8, vtableFixups.Size);
            Assert.NotEqual(0, exports.RelativeVirtualAddress);
            Assert.True(exports.Size > 0);

            int dllNameRva = ReadInt32(pe, exports.RelativeVirtualAddress + 12);
            int baseOrdinal = ReadInt32(pe, exports.RelativeVirtualAddress + 16);
            int numberOfFunctions = ReadInt32(pe, exports.RelativeVirtualAddress + 20);
            int numberOfNames = ReadInt32(pe, exports.RelativeVirtualAddress + 24);
            int addressTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 28);
            int namePointerTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 32);
            int ordinalTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 36);

            Assert.Equal(1, baseOrdinal);
            Assert.Equal(3, numberOfFunctions);
            Assert.Equal(2, numberOfNames);
            Assert.Equal("output.dll", ReadAsciiString(pe, dllNameRva));
            Assert.Equal("Alpha", ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva)));
            Assert.Equal("Zed", ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva + 4)));
            Assert.Equal(2, ReadUInt16(pe, ordinalTableRva));
            Assert.Equal(0, ReadUInt16(pe, ordinalTableRva + 2));
            Assert.Equal(sdataSection.VirtualAddress + 16, ReadInt32(pe, addressTableRva));
            Assert.Equal(0, ReadInt32(pe, addressTableRva + 4));
            Assert.Equal(sdataSection.VirtualAddress + 22, ReadInt32(pe, addressTableRva + 8));
        }

        [Theory]
        [InlineData(Machine.Amd64, "48A1")]
        [InlineData(Machine.Arm, "DFF800F0")]
        [InlineData(Machine.Arm64, "50000058")]
        public void ExportStub_TargetMachine_EmitsExpectedInstructionPrefix(
            Machine machine,
            string expectedPrefix)
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Exported() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as Exported
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options { Machine = machine });
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            int addressTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 28);
            int stubRva = ReadInt32(pe, addressTableRva);
            byte[] prefix = Convert.FromHexString(expectedPrefix);

            Assert.Equal(machine, pe.PEHeaders.CoffHeader.Machine);
            Assert.NotEqual(0, exports.RelativeVirtualAddress);
            Assert.True(
                pe.GetSectionData(stubRva).GetContent().AsSpan(0, prefix.Length).SequenceEqual(prefix));
        }

        private static int ReadInt32(PEReader pe, int rva) =>
            BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(int)));

        private static ushort ReadUInt16(PEReader pe, int rva) =>
            BinaryPrimitives.ReadUInt16LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(ushort)));

        private static string ReadAsciiString(PEReader pe, int rva)
        {
            var content = pe.GetSectionData(rva).GetContent();
            int length = content.AsSpan().IndexOf((byte)0);
            return Encoding.ASCII.GetString(content.AsSpan(0, length));
        }

    }
}
