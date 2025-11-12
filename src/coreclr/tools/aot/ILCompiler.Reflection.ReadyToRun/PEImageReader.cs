// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// Wrapper around PEReader that implements IBinaryImageReader
    /// </summary>
    public class PEImageReader : IBinaryImageReader
    {
        private readonly PEReader _peReader;
        private readonly Machine _machine;
        private readonly OperatingSystem _operatingSystem;

        public PEImageReader(PEReader peReader)
        {
            _peReader = peReader;

            // Extract machine and OS from PE header
            // The OS is encoded in the machine type
            uint rawMachine = (uint)_peReader.PEHeaders.CoffHeader.Machine;
            _operatingSystem = OperatingSystem.Unknown;

            foreach (OperatingSystem os in System.Enum.GetValues(typeof(OperatingSystem)))
            {
                Machine candidateMachine = (Machine)(rawMachine ^ (uint)os);
                if (System.Enum.IsDefined(typeof(Machine), candidateMachine))
                {
                    _machine = candidateMachine;
                    _operatingSystem = os;
                    break;
                }
            }

            if (_operatingSystem == OperatingSystem.Unknown)
            {
                throw new BadImageFormatException($"Invalid PE Machine type: {rawMachine}");
            }
        }

        public ImmutableArray<byte> GetEntireImage()
        {
            return _peReader.GetEntireImage().GetContent();
        }

        public int GetOffset(int rva)
        {
            return _peReader.GetOffset(rva);
        }

        public bool TryGetCompositeReadyToRunHeader(out int rva)
        {
            return _peReader.TryGetCompositeReadyToRunHeader(out rva);
        }

        public Machine Machine => _machine;

        public OperatingSystem OperatingSystem => _operatingSystem;

        public bool HasMetadata => _peReader.HasMetadata;

        public ulong ImageBase => _peReader.PEHeaders.PEHeader.ImageBase;

        public PEReader PEReader => _peReader;

        public bool IsILLibrary => _peReader.PEHeaders.CorHeader?.Flags.HasFlag(CorFlags.ILLibrary) ?? false;

        public void DumpImageInformation(TextWriter writer)
        {
            writer.WriteLine($"MetadataSize: {PEReader.PEHeaders.MetadataSize} byte(s)");

            if (PEReader.PEHeaders.PEHeader is PEHeader header)
            {
                writer.WriteLine($"SizeOfImage: {header.SizeOfImage} byte(s)");
                writer.WriteLine($"ImageBase: 0x{header.ImageBase:X}");
                writer.WriteLine($"FileAlignment: 0x{header.FileAlignment:X}");
                writer.WriteLine($"SectionAlignment: 0x{header.SectionAlignment:X}");
            }
            else
            {
                writer.WriteLine("No PEHeader");
            }

            writer.WriteLine($"CorHeader.Flags: {PEReader.PEHeaders.CorHeader?.Flags}");

            writer.WriteLine("Sections:");
            foreach (var section in PEReader.PEHeaders.SectionHeaders)
                writer.WriteLine($"  {section.Name} {section.VirtualAddress} - {(section.VirtualAddress + section.VirtualSize)}");

            var exportTable = PEReader.GetExportTable();
            exportTable.DumpToConsoleError();
        }

        public Dictionary<string, int> GetSections()
        {
            Dictionary<string, int> sectionMap = [];
            foreach (SectionHeader sectionHeader in PEReader.PEHeaders.SectionHeaders)
            {
                sectionMap.Add(sectionHeader.Name, sectionHeader.SizeOfRawData);
            }

            return sectionMap;
        }
    }
}
