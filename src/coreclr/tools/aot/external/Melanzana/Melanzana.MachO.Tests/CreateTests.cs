using Xunit;
using System.IO;
using System.Linq;
using Melanzana.Streams;
using System;

namespace Melanzana.MachO.Tests
{
    public class CreateTests
    {
        [Fact]
        public void CreateExecutable()
        {
            // Let's try to build a new macho!
            var objectFile = new MachObjectFile();

            // Header
            objectFile.CpuType = MachCpuType.Arm64;
            objectFile.FileType = MachFileType.Execute;
            objectFile.Flags = MachHeaderFlags.PIE | MachHeaderFlags.TwoLevel | MachHeaderFlags.DynamicLink | MachHeaderFlags.NoUndefinedReferences;

            // Segments
            var pageZeroSegment = new MachSegment(objectFile, "__PAGEZERO")
            {
                VirtualAddress = 0,
                Size = 0x100000000,
            };

            var textSegment = new MachSegment(objectFile, "__TEXT")
            {
                FileOffset = 0,
                VirtualAddress = 0x100000000,
                Size = 0x4000,
                InitialProtection = MachVmProtection.Execute | MachVmProtection.Read,
                MaximumProtection = MachVmProtection.Execute | MachVmProtection.Read,
            };

            var textSection = new MachSection(objectFile, "__TEXT", "__text")
            {
                Log2Alignment = 2,
                Type = MachSectionType.Regular,
                Attributes = MachSectionAttributes.SomeInstructions | MachSectionAttributes.PureInstructions,
            };

            using (var textWriter = textSection.GetWriteStream())
            {
                textWriter.Write(new byte[] { 0x00, 0x00, 0x80, 0x52 }); // mov w0, #0
                textWriter.Write(new byte[] { 0xc0, 0x03, 0x5f, 0xd6 }); // ret
                textSection.FileOffset = 0x4000u - (uint)textWriter.Position;
                textSection.VirtualAddress = textSegment.VirtualAddress + textSection.FileOffset;
            }

            var linkEditSegment = new MachSegment(objectFile, "__LINKEDIT")
            {
                VirtualAddress = textSection.VirtualAddress + textSection.Size,
                // FileOffset = 
                // FileSize =
                InitialProtection = MachVmProtection.Read,
                MaximumProtection = MachVmProtection.Read,
            };

            // TODO: This test is incomplete. We should have a layout calculator and a validation.
        }

        [Fact]
        public void CreateObjectFile()
        {
            var objectFile = new MachObjectFile();

            objectFile.CpuType = MachCpuType.Arm64;
            objectFile.CpuSubType = 0;
            objectFile.FileType = MachFileType.Object;
            objectFile.Flags = MachHeaderFlags.SubsectionsViaSymbols;
            objectFile.IsLittleEndian = true;

            var segment = new MachSegment(objectFile, "")
            {
                InitialProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
                MaximumProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
            };

            var textSection = new MachSection(objectFile, "__TEXT", "__text")
            {
                Log2Alignment = 2,
                Type = MachSectionType.Regular,
                Attributes = MachSectionAttributes.SomeInstructions | MachSectionAttributes.PureInstructions,
            };

            var compactUnwindSection = new MachSection(objectFile, "__LD", "__compact_unwind")
            {
                Log2Alignment = 3,
                Type = MachSectionType.Regular,
                Attributes = MachSectionAttributes.Debug,
            };

            segment.Sections.Add(textSection);
            segment.Sections.Add(compactUnwindSection);
            objectFile.LoadCommands.Add(segment);

            using (var textWriter = textSection.GetWriteStream())
            {
                textWriter.Write(new byte[] { 0xff, 0x43, 0x00, 0xd1 }); // sub sp, sp, #0x10
                textWriter.Write(new byte[] { 0x00, 0x00, 0x80, 0x52 }); // mov w0, #0
                textWriter.Write(new byte[] { 0xff, 0x0f, 0x00, 0xb9 }); // str wzr, [sp, #0xc]
                textWriter.Write(new byte[] { 0xff, 0x43, 0x00, 0x91 }); // add sp, sp, #0x10
                textWriter.Write(new byte[] { 0xc0, 0x03, 0x5f, 0xd6 }); // ret
            }

            using (var compactUnwindWriter = compactUnwindSection.GetWriteStream())
            {
                // Address of _main
                compactUnwindSection.Relocations.Add(new MachRelocation
                {
                    Address = 0,
                    SymbolOrSectionIndex = 1, // TODO: Better symbolic reference?
                    Length = 8,
                    RelocationType = MachRelocationType.Arm64Unsigned,
                    IsExternal = false,
                    IsPCRelative = false,
                });
                compactUnwindWriter.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }); 

                compactUnwindWriter.Write(new byte[] { 0x14, 0x00, 0x00, 0x00 }); // Length of _main
                compactUnwindWriter.Write(new byte[] { 0x00, 0x10, 0x00, 0x02 }); // Unwind code
                compactUnwindWriter.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }); // No personality
                compactUnwindWriter.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }); // No LSDA
            }

            // Verify the relocation was written correctly
            Assert.NotNull(compactUnwindSection.RelocationData);
            Assert.Equal(8u, compactUnwindSection.RelocationData!.Size);
            using (var s = compactUnwindSection.RelocationData.GetReadStream())
            {
                byte[] r = new byte[8];
                s.ReadFully(r);
                Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x06 }, r);
            }

            objectFile.LoadCommands.Add(new MachBuildVersion
            {
                TargetPlatform = MachPlatform.MacOS,
                MinimumPlatformVersion = new Version(12, 0, 0),
                SdkVersion = new Version(12, 0, 0),
            });

            var symbolTable = new MachSymbolTable(objectFile);
            objectFile.LoadCommands.Add(symbolTable);
            // FIXME: Values
            symbolTable.Symbols.Add(new MachSymbol { Name = "ltmp0", Section = textSection, Value = 0, Descriptor = 0, Type = MachSymbolType.Section });
            symbolTable.Symbols.Add(new MachSymbol { Name = "ltmp1", Section = compactUnwindSection, Value = 0x18, Descriptor = 0, Type = MachSymbolType.Section });
            symbolTable.Symbols.Add(new MachSymbol { Name = "_main", Section = textSection, Value = 0, Descriptor = 0, Type = MachSymbolType.Section | MachSymbolType.External });
            objectFile.LoadCommands.Add(new MachDynamicLinkEditSymbolTable(symbolTable));

            objectFile.UpdateLayout();
            Assert.Equal(0u, segment.VirtualAddress);
            Assert.Equal(0x38u, segment.Size);
            Assert.Equal(0x188u, segment.FileOffset);
            Assert.Equal(0x38u, segment.FileSize);
            Assert.Equal(0u, textSection.VirtualAddress);
            Assert.Equal(0x14u, textSection.Size);
            Assert.Equal(0x188u, textSection.FileOffset);
            Assert.Equal(0x18u, compactUnwindSection.VirtualAddress);
            Assert.Equal(0x20u, compactUnwindSection.Size);
            Assert.Equal(0x1a0u, compactUnwindSection.FileOffset);

            // Ensure that write doesn't crash
            var binaryFile = new MemoryStream();
            MachWriter.Write(binaryFile, objectFile);
            Assert.Equal(528, binaryFile.Length);

            // Ensure that the file can be read again
            binaryFile.Position = 0;
            _ = MachReader.Read(binaryFile);
        }
    }
}
