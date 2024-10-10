// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.IO;
using System.Linq;
using Microsoft.NET.HostModel.MachO.Streams;
using System;

namespace Microsoft.NET.HostModel.MachO.Tests
{
    public class ReadTests
    {
        public static MachObjectFile GetMachExecutable()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.out")!;
            var objectFile = MachReader.Read(aOutStream).Single();
            return objectFile;
        }

        [Fact]
        public void ReadExecutable()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.out")!;
            var objectFile = MachReader.Read(aOutStream).First();

            var segments = objectFile.LoadCommands.OfType<MachSegment>().ToArray();
            Assert.Equal("__PAGEZERO", segments[0].Name);
            Assert.Equal("__TEXT", segments[1].Name);
            Assert.Equal("__DATA_CONST", segments[2].Name);
            Assert.Equal("__LINKEDIT", segments[3].Name);

            var symbolTable = objectFile.LoadCommands.OfType<MachSymbolTable>().FirstOrDefault();
            Assert.NotNull(symbolTable);
            var symbols = symbolTable!.Symbols.ToArray();
            Assert.Equal(3, symbols.Length);
            Assert.Equal("__mh_execute_header", symbols[0].Name);
            Assert.Equal(0x100000000u, symbols[0].Value);
            Assert.Equal("_main", symbols[1].Name);
            Assert.Equal(0x100003F70u, symbols[1].Value);
            Assert.Equal("_printf", symbols[2].Name);
            Assert.Equal(0u, symbols[2].Value);

            var buildVersion = objectFile.LoadCommands.OfType<MachBuildVersion>().FirstOrDefault();
            Assert.NotNull(buildVersion);
            Assert.Equal(MachPlatform.MacOS, buildVersion!.Platform);
            Assert.Equal("14.0.0", buildVersion!.MinimumPlatformVersion.ToString());
            Assert.Equal("15.0.0", buildVersion!.SdkVersion.ToString());
            Assert.Equal(1, buildVersion!.ToolVersions.Count);
            Assert.Equal(MachBuildTool.Ld, buildVersion!.ToolVersions[0].BuildTool);
            Assert.Equal("1115.7.3", buildVersion!.ToolVersions[0].Version.ToString());
        }

        [Fact]
        public void ReadObjectFile()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.o")!;
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
            Assert.Equal(0x20u, symbols[1].Value);
            Assert.Equal("_add", symbols[2].Name);
            Assert.Equal(textSection, symbols[2].Section);
            Assert.Equal(0u, symbols[2].Value);
        }
    }
}
