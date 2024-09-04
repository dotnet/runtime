using Xunit;
using System.IO;
using System.Linq;
using Melanzana.Streams;
using System;

namespace Melanzana.MachO.Tests
{
    public class ReadTests
    {
        [Fact]
        public void ReadExecutable()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Melanzana.MachO.Tests.Data.a.out")!;
            var objectFile = MachReader.Read(aOutStream).First();

            var segments = objectFile.LoadCommands.OfType<MachSegment>().ToArray();
            Assert.Equal("__PAGEZERO", segments[0].Name);
            Assert.Equal("__TEXT", segments[1].Name);
            Assert.Equal("__LINKEDIT", segments[2].Name);

            var symbolTable = objectFile.LoadCommands.OfType<MachSymbolTable>().FirstOrDefault();
            Assert.NotNull(symbolTable);
            var symbols = symbolTable!.Symbols.ToArray();
            Assert.Equal(2, symbols.Length);
            Assert.Equal("__mh_execute_header", symbols[0].Name);
            Assert.Equal(0x100000000u, symbols[0].Value);
            Assert.Equal("_main", symbols[1].Name);
            Assert.Equal(0x100003fa4u, symbols[1].Value);

            var buildVersion = objectFile.LoadCommands.OfType<MachBuildVersion>().FirstOrDefault();
            Assert.NotNull(buildVersion);
            Assert.Equal(MachPlatform.MacOS, buildVersion!.Platform);
            Assert.Equal("12.0.0", buildVersion!.MinimumPlatformVersion.ToString());
            Assert.Equal("12.0.0", buildVersion!.SdkVersion.ToString());
            Assert.Equal(1, buildVersion!.ToolVersions.Count);
            Assert.Equal(MachBuildTool.Ld, buildVersion!.ToolVersions[0].BuildTool);
            Assert.Equal("711.0.0", buildVersion!.ToolVersions[0].Version.ToString());
        }

        [Fact]
        public void ReadObjectFile()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Melanzana.MachO.Tests.Data.a.o")!;
            var objectFile = MachReader.Read(aOutStream).First();

            var segments = objectFile.LoadCommands.OfType<MachSegment>().ToArray();
            Assert.Single(segments);

            var textSection = segments[0].Sections[0];
            var compactUnwindSection = segments[0].Sections[1];

            Assert.Equal("__TEXT", textSection.SegmentName);
            Assert.Equal("__text", textSection.SectionName);
            Assert.Equal("__LD", compactUnwindSection.SegmentName);
            Assert.Equal("__compact_unwind", compactUnwindSection.SectionName);

            var relocations = compactUnwindSection.Relocations;
            Assert.Single(relocations);
            var relec0 = relocations.First();
            Assert.Equal(0, relec0.Address);
            Assert.False(relec0.IsPCRelative);
            Assert.False(relec0.IsExternal);
            Assert.Equal(1u, relec0.SymbolOrSectionIndex);
            Assert.Equal(8, relec0.Length);
            Assert.Equal(MachRelocationType.Arm64Unsigned, relec0.RelocationType);

            var symbolTable = objectFile.LoadCommands.OfType<MachSymbolTable>().FirstOrDefault();
            Assert.NotNull(symbolTable);
            var symbols = symbolTable!.Symbols.ToArray();
            Assert.Equal("ltmp0", symbols[0].Name);
            Assert.Equal(textSection, symbols[0].Section);
            Assert.Equal(0u, symbols[0].Value);
            Assert.Equal("ltmp1", symbols[1].Name);
            Assert.Equal(compactUnwindSection, symbols[1].Section);
            Assert.Equal(0x18u, symbols[1].Value);
            Assert.Equal("_main", symbols[2].Name);
            Assert.Equal(textSection, symbols[2].Section);
            Assert.Equal(0u, symbols[2].Value);
        }
    }
}
