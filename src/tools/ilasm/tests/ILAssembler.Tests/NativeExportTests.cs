// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            Assert.False(pe.PEHeaders.SectionHeaders.Any(section => section.Name == ".sdata"));

            DirectoryEntry exportTableDirectory = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            Assert.Equal(0, exportTableDirectory.RelativeVirtualAddress);
            Assert.Equal(0, exportTableDirectory.Size);
        }

        [Fact]
        public void VTableFixupAndExport_UseDeclaredWritableDataAndExecutableStubs()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .data VTReference = &(VT)
                .vtfixup [2] int32 fromunmanaged at VT
                .field public static int32 VTStorage at VT
                .field public static int32 VTReferenceStorage at VTReference
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
            var textSection = pe.PEHeaders.SectionHeaders.Single(section => section.Name == ".text");
            DirectoryEntry vtableFixups = pe.PEHeaders.CorHeader!.VtableFixupsDirectory;
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;

            Assert.Equal(sdataSection.VirtualAddress, vtableFixups.RelativeVirtualAddress);
            Assert.Equal(8, vtableFixups.Size);
            Assert.True(sdataSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead));
            Assert.True(sdataSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemWrite));
            Assert.True(sdataSection.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsInitializedData));
            Assert.False(sdataSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemExecute));
            Assert.False(sdataSection.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsCode));
            Assert.True(textSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemRead));
            Assert.True(textSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemExecute));
            Assert.True(textSection.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsCode));
            Assert.False(textSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemWrite));
            Assert.NotEqual(0, exports.RelativeVirtualAddress);
            Assert.True(exports.Size > 0);
            Assert.Equal(".sdata", GetContainingSection(pe, exports.RelativeVirtualAddress).Name);

            MetadataReader reader = pe.GetMetadataReader();
            var fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));
            int vtableDataRva = fields["VTStorage"].GetRelativeVirtualAddress();
            int referenceDataRva = fields["VTReferenceStorage"].GetRelativeVirtualAddress();
            Assert.Equal(".sdata", GetContainingSection(pe, vtableDataRva).Name);
            Assert.Equal(
                pe.PEHeaders.PEHeader!.ImageBase + (uint)vtableDataRva,
                ReadUInt32(pe, referenceDataRva));
            Assert.True(HasBaseRelocation(pe, referenceDataRva, 3));
            Assert.Equal(vtableDataRva, ReadInt32(pe, vtableFixups.RelativeVirtualAddress));
            Assert.Equal(2, ReadUInt16(pe, vtableFixups.RelativeVirtualAddress + sizeof(int)));

            int firstMethodToken = ReadInt32(pe, vtableDataRva);
            int secondMethodToken = ReadInt32(pe, vtableDataRva + sizeof(int));
            Assert.Equal(HandleKind.MethodDefinition, MetadataTokens.EntityHandle(firstMethodToken).Kind);
            Assert.Equal(HandleKind.MethodDefinition, MetadataTokens.EntityHandle(secondMethodToken).Kind);

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
            int zedStubRva = ReadInt32(pe, addressTableRva);
            int alphaStubRva = ReadInt32(pe, addressTableRva + 8);
            Assert.Equal(".text", GetContainingSection(pe, zedStubRva).Name);
            Assert.Equal(".text", GetContainingSection(pe, alphaStubRva).Name);
            Assert.Equal(0, ReadInt32(pe, addressTableRva + 4));
        }

        [Theory]
        [InlineData(Machine.I386, false, false)]
        [InlineData(Machine.I386, false, true)]
        [InlineData(Machine.I386, true, false)]
        [InlineData(Machine.I386, true, true)]
        [InlineData(Machine.Amd64, false, false)]
        [InlineData(Machine.Amd64, false, true)]
        [InlineData(Machine.Amd64, true, false)]
        [InlineData(Machine.Amd64, true, true)]
        [InlineData(Machine.Arm64, false, false)]
        [InlineData(Machine.Arm64, false, true)]
        [InlineData(Machine.Arm64, true, false)]
        [InlineData(Machine.Arm64, true, true)]
        public void MixedDataSections_PreserveFieldsReferencesAndExports(Machine machine, bool tls, bool vtable)
        {
            string pointerType = machine == Machine.I386 ? "int32" : "int64";
            string tlsDeclarations = tls
                ? """
                  .data tls TlsValue = int32(0x11223344)
                  .data tls TlsPointer = &(CilValue)
                  .field public static int32 TlsField at TlsValue
                  .field public static native int TlsReference at TlsPointer
                  """
                : string.Empty;
            string vtableDeclaration = vtable ? $".vtfixup [1] {pointerType} fromunmanaged at VT" : string.Empty;
            string vtableEntry = vtable ? ".vtentry 1 : 1" : string.Empty;
            string target = tls ? "TlsValue" : "RegularValue";
            string source = $$"""
                .assembly test { }
                .data cil CilValue = int32(0x12345678)
                .data VT = {{pointerType}}(0)
                {{tlsDeclarations}}
                .data cil CilPointer = &({{target}})
                .data RegularValue = int32(0x55667788)
                .data RegularPointer = &(CilValue)
                {{vtableDeclaration}}
                .field public static int32 CilField at CilValue
                .field public static int32 RegularField at RegularValue
                .field public static native int CilReference at CilPointer
                .field public static native int RegularReference at RegularPointer
                .field public static {{pointerType}} VTableField at VT
                .method public static void Exported() cil managed
                {
                    {{vtableEntry}}
                    .export [1] as Exported
                    ret
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source, new Options { Machine = machine });
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, int> fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name), field => field.GetRelativeVirtualAddress());
            Assert.Equal(".text", GetContainingSection(pe, fields["CilField"]).Name);
            Assert.Equal(".text", GetContainingSection(pe, fields["CilReference"]).Name);
            Assert.Equal(".sdata", GetContainingSection(pe, fields["RegularField"]).Name);
            Assert.Equal(".sdata", GetContainingSection(pe, fields["RegularReference"]).Name);
            Assert.Equal(".sdata", GetContainingSection(pe, fields["VTableField"]).Name);
            Assert.Equal(0x12345678, ReadInt32(pe, fields["CilField"]));
            Assert.Equal(0x55667788, ReadInt32(pe, fields["RegularField"]));
            Assert.Equal(vtable ? 0x06000001 : 0, ReadInt32(pe, fields["VTableField"]));
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            if (vtable)
            {
                Assert.Equal(fields["VTableField"], ReadInt32(
                    pe, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress));
                int stubRva = ReadInt32(pe, ReadInt32(pe, exports.RelativeVirtualAddress + 28));
                Assert.Equal(".text", GetContainingSection(pe, stubRva).Name);
            }
            else
            {
                Assert.Equal(default, pe.PEHeaders.CorHeader!.VtableFixupsDirectory);
                Assert.Equal(default, exports);
            }

            int relocationType = machine == Machine.I386 ? 3 : 10;
            ulong imageBase = pe.PEHeaders.PEHeader.ImageBase;
            Assert.Equal(imageBase + (uint)fields["CilField"], ReadPointer(fields["RegularReference"]));
            Assert.True(HasBaseRelocation(pe, fields["RegularReference"], relocationType));
            Assert.Equal(imageBase + (uint)fields[tls ? "TlsField" : "RegularField"], ReadPointer(fields["CilReference"]));
            Assert.True(HasBaseRelocation(pe, fields["CilReference"], relocationType));
            if (tls)
            {
                Assert.Equal(".tls", GetContainingSection(pe, fields["TlsField"]).Name);
                Assert.Equal(".tls", GetContainingSection(pe, fields["TlsReference"]).Name);
                Assert.Equal(0x11223344, ReadInt32(pe, fields["TlsField"]));
                Assert.Equal(imageBase + (uint)fields["CilField"], ReadPointer(fields["TlsReference"]));
                Assert.True(HasBaseRelocation(pe, fields["TlsReference"], relocationType));
                int tlsDirectoryRva = pe.PEHeaders.PEHeader.ThreadLocalStorageTableDirectory.RelativeVirtualAddress;
                int pointerSize = machine == Machine.I386 ? sizeof(uint) : sizeof(ulong);
                for (int i = 0; i < 4; i++)
                {
                    Assert.True(HasBaseRelocation(pe, tlsDirectoryRva + (i * pointerSize), relocationType));
                }
            }
            else
            {
                Assert.DoesNotContain(pe.PEHeaders.SectionHeaders, section => section.Name == ".tls");
                Assert.Equal(default, pe.PEHeaders.PEHeader.ThreadLocalStorageTableDirectory);
            }

            ulong ReadPointer(int rva) => machine == Machine.I386 ? ReadUInt32(pe, rva) : ReadUInt64(pe, rva);
        }

        [Theory]
        [InlineData(Machine.I386, "FF25", 2, sizeof(uint), 16, 6, 3, "")]
        [InlineData(Machine.Amd64, "48A1", 2, sizeof(ulong), 4, 12, 10, "")]
        [InlineData(Machine.Arm64, "90000058100240F900021FD6", 16, sizeof(ulong), 8, 24, 10, "")]
        [InlineData(Machine.I386, "FF25", 2, sizeof(uint), 16, 6, 3, "cil")]
        [InlineData(Machine.Amd64, "48A1", 2, sizeof(ulong), 4, 12, 10, "cil")]
        [InlineData(Machine.Arm64, "90000058100240F900021FD6", 16, sizeof(ulong), 8, 24, 10, "cil")]
        public void ExportStub_TargetMachine_IsExecutableAndRelocatable(
            Machine machine,
            string expectedPrefix,
            int addressOffset,
            int addressSize,
            int addressAlignment,
            int stubSize,
            int relocationType,
            string section)
        {
            string slotType = addressSize == sizeof(ulong) ? "int64" : "int32";
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data {{section}} VT = {{slotType}}(0)
                .vtfixup [1] {{slotType}} fromunmanaged at VT
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
            int slotRva = ReadInt32(pe, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
            byte[] prefix = Convert.FromHexString(expectedPrefix);
            var stubSection = GetContainingSection(pe, stubRva);

            Assert.Equal(machine, pe.PEHeaders.CoffHeader.Machine);
            Assert.Equal(section == "cil" ? ".text" : ".sdata", GetContainingSection(pe, slotRva).Name);
            Assert.Equal(0x06000001, ReadInt32(pe, slotRva));
            Assert.NotEqual(0, exports.RelativeVirtualAddress);
            Assert.Equal(".text", stubSection.Name);
            Assert.True(stubSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemExecute));
            Assert.False(stubSection.SectionCharacteristics.HasFlag(SectionCharacteristics.MemWrite));
            Assert.Equal(0, (stubRva + addressOffset) % addressAlignment);
            Assert.Equal(stubSize, stubSection.VirtualSize - (stubRva - stubSection.VirtualAddress));
            Assert.True(pe.PEHeaders.PEHeader.DllCharacteristics.HasFlag(DllCharacteristics.DynamicBase));
            Assert.True(
                pe.GetSectionData(stubRva).GetContent().AsSpan(0, prefix.Length).SequenceEqual(prefix));

            ulong expectedAddress = pe.PEHeaders.PEHeader.ImageBase + (uint)slotRva;
            ulong actualAddress = addressSize == sizeof(ulong)
                ? ReadUInt64(pe, stubRva + addressOffset)
                : ReadUInt32(pe, stubRva + addressOffset);
            Assert.Equal(expectedAddress, actualAddress);
            Assert.Equal(
                ".reloc",
                GetContainingSection(
                    pe,
                    pe.PEHeaders.PEHeader.BaseRelocationTableDirectory.RelativeVirtualAddress).Name);
            Assert.True(HasBaseRelocation(pe, stubRva + addressOffset, relocationType));
        }

        [Theory]
        [InlineData(Machine.I386, "int32", sizeof(uint), 3, "")]
        [InlineData(Machine.Amd64, "int64", sizeof(ulong), 10, "")]
        [InlineData(Machine.I386, "int32", sizeof(uint), 3, "cil")]
        [InlineData(Machine.Amd64, "int64", sizeof(ulong), 10, "cil")]
        [InlineData(Machine.I386, "int32", sizeof(uint), 3, "tls")]
        [InlineData(Machine.Amd64, "int64", sizeof(ulong), 10, "tls")]
        public void DataLabelReference_UsesPointerWidthAbsoluteAddressAndRelocation(
            Machine machine,
            string pointerType,
            int pointerSize,
            int relocationType,
            string section)
        {
            string source = $$"""
                .assembly test { }
                .data {{section}} Target = int32(0x12345678)
                .data {{section}} Pointer = &(Target)
                .class public explicit ansi sealed DataHolder
                {
                    .field [0] public static int32 TargetField at Target
                    .field [4] public static {{pointerType}} PointerField at Pointer
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options { Machine = machine });
            MetadataReader reader = pe.GetMetadataReader();
            var fields = reader.FieldDefinitions
                .Select(reader.GetFieldDefinition)
                .ToDictionary(field => reader.GetString(field.Name));
            int targetRva = fields["TargetField"].GetRelativeVirtualAddress();
            int pointerRva = fields["PointerField"].GetRelativeVirtualAddress();
            ulong pointer = pointerSize == sizeof(ulong)
                ? ReadUInt64(pe, pointerRva)
                : ReadUInt32(pe, pointerRva);

            Assert.Equal(pe.PEHeaders.PEHeader!.ImageBase + (uint)targetRva, pointer);
            Assert.True(HasBaseRelocation(pe, pointerRva, relocationType));
            Assert.False(pe.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly));
        }

        [Theory]
        [InlineData(Machine.I386, sizeof(uint), 3)]
        [InlineData(Machine.Amd64, sizeof(ulong), 10)]
        [InlineData(Machine.Arm64, sizeof(ulong), 10)]
        public void TlsDirectory_WithoutOtherFixups_EmitsBaseRelocations(
            Machine machine, int pointerSize, int relocationType)
        {
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                ".assembly test {} .data tls int32(42)", new Options { Machine = machine });
            DirectoryEntry directory = pe.PEHeaders.PEHeader!.ThreadLocalStorageTableDirectory;
            for (int i = 0; i < 4; i++)
            {
                Assert.True(HasBaseRelocation(pe, directory.RelativeVirtualAddress + (i * pointerSize), relocationType));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void ZeroSlotVTableFixup_EmptyData_DoesNotRequireBackingBytes(string section)
        {
            string source = $$"""
                .assembly test {}
                .data {{section}} Empty = int8[0]
                .vtfixup [0] int32 at Empty
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            DirectoryEntry directory = pe.PEHeaders.CorHeader!.VtableFixupsDirectory;
            Assert.Equal(8, directory.Size);
            Assert.Equal(0, ReadUInt16(pe, directory.RelativeVirtualAddress + sizeof(int)));
            Assert.DoesNotContain(pe.PEHeaders.SectionHeaders, header => header.Name == ".tls");
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void DataLabelReference_MissingTarget_ReportsDiagnosticAndOmitsRelocation(string section)
        {
            string source = $$"""
                .assembly test { }
                .data {{section}} Pointer = &(Missing)
                .class public explicit ansi sealed DataHolder
                {
                    .field [0] public static int32 PointerField at Pointer
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.LabelNotFound);
                Assert.Contains("Missing", error.Message);

                MetadataReader reader = pe.GetMetadataReader();
                FieldDefinition field = reader.GetFieldDefinition(Assert.Single(reader.FieldDefinitions));
                int pointerRva = field.GetRelativeVirtualAddress();
                Assert.Equal(0U, ReadUInt32(pe, pointerRva));
                Assert.False(HasBaseRelocation(pe, pointerRva, 3));
                Assert.Equal(section != "tls", pe.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly));
            }
        }

        [Theory]
        [InlineData(Machine.I386, "int64")]
        [InlineData(Machine.Amd64, "int32")]
        [InlineData(Machine.Arm64, "int32")]
        [InlineData(Machine.I386, "int32 int64")]
        [InlineData(Machine.I386, "")]
        public void VTableFixup_InvalidWidthForTarget_ReportsDiagnostic(
            Machine machine,
            string widthAttributes)
        {
            string source = $$"""
                .assembly test { }
                .data VT = int64(0)
                .vtfixup [1] {{widthAttributes}} fromunmanaged at VT
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) =
                CompileErrorTolerant(source, new Options { Machine = machine });
            using (pe)
            {
                Assert.Contains(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InvalidVTableWidth);
                Assert.Equal(0, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
            }
        }

        [Theory]
        [InlineData(2, 1, "invalid VTable entry")]
        [InlineData(1, 2, "invalid VTable slot")]
        public void VTableEntry_InvalidAssociationWithoutExport_ReportsDiagnostic(
            int vtableEntry,
            int vtableSlot,
            string expectedMessage)
        {
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Method() cil managed
                    {
                        .vtentry {{vtableEntry}} : {{vtableSlot}}
                        ret
                    }
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InvalidVTableEntry);
                Assert.Contains(expectedMessage, error.Message);
                int slotRva = ReadInt32(
                    pe,
                    pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
                Assert.Equal(0, ReadInt32(pe, slotRva));
            }
        }

        [Fact]
        public void Export_DuplicateOrdinalForDifferentTargets_ReportsDiagnosticAndOmitsDuplicate()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void First() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as First
                        ret
                    }
                    .method public static void Second() cil managed
                    {
                        .vtentry 1 : 2
                        .export [1] as Second
                        ret
                    }
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.DuplicateExportOrdinal);
                Assert.Contains("Second", error.Message);

                DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
                Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 20));
                Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 24));
                int namePointerTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 32);
                Assert.Equal("First", ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva)));
            }
        }

        [Fact]
        public void Export_DuplicateOrdinalForSameTarget_AllowsAliases()
        {
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void First() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as FirstAlias
                        ret
                    }
                    .method public static void Second() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as SecondAlias
                        ret
                    }
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            int addressTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 28);
            int namePointerTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 32);
            int ordinalTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 36);

            Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 20));
            Assert.Equal(2, ReadInt32(pe, exports.RelativeVirtualAddress + 24));
            Assert.NotEqual(0, ReadInt32(pe, addressTableRva));
            Assert.Equal("FirstAlias", ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva)));
            Assert.Equal(
                "SecondAlias",
                ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva + sizeof(int))));
            Assert.Equal(0, ReadUInt16(pe, ordinalTableRva));
            Assert.Equal(0, ReadUInt16(pe, ordinalTableRva + sizeof(ushort)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void ExportOrdinal_NonPositive_ReportsDiagnosticAndOmitsExport(int ordinal)
        {
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Exported() cil managed
                    {
                        .vtentry 1 : 1
                        .export [{{ordinal}}] as InvalidOrdinal
                        ret
                    }
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InvalidExportOrdinal);
                Assert.Contains(ordinal.ToString(), error.Message);
                Assert.Equal(0, pe.PEHeaders.PEHeader!.ExportTableDirectory.RelativeVirtualAddress);
            }
        }

        [Fact]
        public void ExportOrdinal_ExtremeSparseRange_ReportsDiagnosticAndOmitsInvalidExport()
        {
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Low() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as Low
                        ret
                    }
                    .method public static void High() cil managed
                    {
                        .vtentry 1 : 2
                        .export [{{int.MaxValue}}] as InvalidHigh
                        ret
                    }
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.ExportOrdinalRangeTooLarge);
                Assert.Contains(int.MaxValue.ToString(), error.Message);

                DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
                Assert.NotEqual(0, exports.RelativeVirtualAddress);
                Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 16));
                Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 20));
                Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 24));
                int namePointerTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 32);
                Assert.Equal("Low", ReadAsciiString(pe, ReadInt32(pe, namePointerTableRva)));
            }
        }

        [Fact]
        public void ExportOrdinal_MaxValueAsBase_EmitsSingleEntry()
        {
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Exported() cil managed
                    {
                        .vtentry 1 : 1
                        .export [{{int.MaxValue}}] as Exported
                        ret
                    }
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;

            Assert.Equal(int.MaxValue, ReadInt32(pe, exports.RelativeVirtualAddress + 16));
            Assert.Equal(1, ReadInt32(pe, exports.RelativeVirtualAddress + 20));
            Assert.Equal(0, ReadUInt16(pe, ReadInt32(pe, exports.RelativeVirtualAddress + 36)));
        }

        [Fact]
        public void ExportOrdinal_MaximumWordIndex_PreservesSparseExports()
        {
            const int BaseOrdinal = 100000;
            const int MaximumOrdinal = BaseOrdinal + ushort.MaxValue;
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void First() cil managed
                    {
                        .vtentry 1 : 1
                        .export [{{BaseOrdinal}}] as First
                        ret
                    }
                    .method public static void Last() cil managed
                    {
                        .vtentry 1 : 2
                        .export [{{MaximumOrdinal}}] as Last
                        ret
                    }
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            DirectoryEntry exports = pe.PEHeaders.PEHeader!.ExportTableDirectory;
            int ordinalTableRva = ReadInt32(pe, exports.RelativeVirtualAddress + 36);

            Assert.Equal(BaseOrdinal, ReadInt32(pe, exports.RelativeVirtualAddress + 16));
            Assert.Equal(ushort.MaxValue + 1, ReadInt32(pe, exports.RelativeVirtualAddress + 20));
            Assert.Equal(0, ReadUInt16(pe, ordinalTableRva));
            Assert.Equal(ushort.MaxValue, ReadUInt16(pe, ordinalTableRva + sizeof(ushort)));
        }

        [Theory]
        [InlineData(2, 1, "invalid VTable entry")]
        [InlineData(1, 2, "invalid VTable slot")]
        public void Export_InvalidVTableReference_ReportsDiagnosticAndOmitsExport(
            int vtableEntry,
            int vtableSlot,
            string expectedMessage)
        {
            string source = $$"""
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Exported() cil managed
                    {
                        .vtentry {{vtableEntry}} : {{vtableSlot}}
                        .export [1] as InvalidReference
                        ret
                    }
                }
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InvalidVTableEntry);
                Assert.Contains(expectedMessage, error.Message);
                Assert.Equal(0, pe.PEHeaders.PEHeader!.ExportTableDirectory.RelativeVirtualAddress);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(ushort.MaxValue + 1)]
        public void VTableFixup_InvalidSlotCount_ReportsDiagnosticAndOmitsFixup(int slotCount)
        {
            string source = $$"""
                .assembly test { }
                .data VT = int32(0)
                .vtfixup [{{slotCount}}] int32 fromunmanaged at VT
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InvalidVTableSlotCount);
                Assert.Contains(slotCount.ToString(), error.Message);
                Assert.Equal(0, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
            }
        }

        [Fact]
        public void VTableFixup_MissingDataLabel_ReportsDiagnosticAndOmitsFixup()
        {
            string source = """
                .assembly test { }
                .vtfixup [1] int32 fromunmanaged at Missing
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.LabelNotFound);
                Assert.Contains("Missing", error.Message);
                Assert.Equal(0, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("cil")]
        [InlineData("tls")]
        public void VTableFixup_InsufficientDataLabelStorage_ReportsDiagnosticAndOmitsFixup(string section)
        {
            string source = $$"""
                .assembly test { }
                .data {{section}} VT = int32(0)
                .data {{section}} FollowingData = int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                """;

            (ImmutableArray<Diagnostic> diagnostics, PEReader pe) = CompileErrorTolerant(source);
            using (pe)
            {
                Diagnostic error = Assert.Single(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.InsufficientVTableData);
                Assert.Contains("'VT' has 4 bytes available", error.Message);
                Assert.Equal(0, pe.PEHeaders.CorHeader!.VtableFixupsDirectory.RelativeVirtualAddress);
            }
        }

        private static int ReadInt32(PEReader pe, int rva) =>
            BinaryPrimitives.ReadInt32LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(int)));

        private static uint ReadUInt32(PEReader pe, int rva) =>
            BinaryPrimitives.ReadUInt32LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(uint)));

        private static ulong ReadUInt64(PEReader pe, int rva) =>
            BinaryPrimitives.ReadUInt64LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(ulong)));

        private static ushort ReadUInt16(PEReader pe, int rva) =>
            BinaryPrimitives.ReadUInt16LittleEndian(pe.GetSectionData(rva).GetContent().AsSpan(0, sizeof(ushort)));

        private static SectionHeader GetContainingSection(PEReader pe, int rva) =>
            pe.PEHeaders.SectionHeaders[pe.PEHeaders.GetContainingSectionIndex(rva)];

        private static bool HasBaseRelocation(PEReader pe, int targetRva, int expectedType)
        {
            DirectoryEntry directory = pe.PEHeaders.PEHeader!.BaseRelocationTableDirectory;
            if (directory.RelativeVirtualAddress == 0 || directory.Size == 0)
            {
                return false;
            }

            ReadOnlySpan<byte> content = pe
                .GetSectionData(directory.RelativeVirtualAddress)
                .GetContent(0, directory.Size)
                .AsSpan();
            int blockOffset = 0;
            while (blockOffset < content.Length)
            {
                int pageRva = BinaryPrimitives.ReadInt32LittleEndian(content.Slice(blockOffset));
                int blockSize = BinaryPrimitives.ReadInt32LittleEndian(content.Slice(blockOffset + sizeof(int)));
                if (blockSize < 8 || blockOffset + blockSize > content.Length)
                {
                    return false;
                }

                int entryCount = (blockSize - 8) / sizeof(ushort);
                for (int i = 0; i < entryCount; i++)
                {
                    ushort entry = BinaryPrimitives.ReadUInt16LittleEndian(
                        content.Slice(blockOffset + 8 + (i * sizeof(ushort))));
                    int type = entry >> 12;
                    int rva = pageRva + (entry & 0x0FFF);
                    if (type == expectedType && rva == targetRva)
                    {
                        return true;
                    }
                }

                blockOffset += blockSize;
            }

            return false;
        }

        private static string ReadAsciiString(PEReader pe, int rva)
        {
            var content = pe.GetSectionData(rva).GetContent();
            int length = content.AsSpan().IndexOf((byte)0);
            return Encoding.ASCII.GetString(content.AsSpan(0, length));
        }

        private static (ImmutableArray<Diagnostic> Diagnostics, PEReader PEReader) CompileErrorTolerant(
            string source) =>
            CompileErrorTolerant(source, new Options());

        private static (ImmutableArray<Diagnostic> Diagnostics, PEReader PEReader) CompileErrorTolerant(
            string source,
            Options options)
        {
            options.ErrorTolerant = true;
            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                options);

            Assert.NotNull(result);
            var image = new BlobBuilder();
            result!.Serialize(image);
            return (diagnostics, new PEReader(image.ToImmutableArray()));
        }
    }
}
