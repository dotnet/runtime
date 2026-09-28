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

        public Machine Machine { get; }
        public OperatingSystem OperatingSystem { get; }
        public ulong ImageBase => _peReader.PEHeaders.PEHeader.ImageBase;

        public PEImageReader(PEReader peReader)
        {
            _peReader = peReader;

            // Extract machine and OS from PE header
            // The OS is encoded in the machine type
            uint rawMachine = (uint)_peReader.PEHeaders.CoffHeader.Machine;
            OperatingSystem = OperatingSystem.Unknown;

            foreach (OperatingSystem os in System.Enum.GetValues(typeof(OperatingSystem)))
            {
                Machine candidateMachine = (Machine)(rawMachine ^ (uint)os);
                if (System.Enum.IsDefined(typeof(Machine), candidateMachine))
                {
                    Machine = candidateMachine;
                    OperatingSystem = os;
                    break;
                }
            }

            if (OperatingSystem == OperatingSystem.Unknown)
            {
                throw new BadImageFormatException($"Invalid PE Machine type: {rawMachine}");
            }
        }

        public ImmutableArray<byte> GetEntireImage() => _peReader.GetEntireImage().GetContent();

        public int GetOffset(int rva) => _peReader.GetOffset(rva);

        public bool TryGetReadyToRunHeader(out int rva, out bool isComposite)
        {
            if ((_peReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
            {
                // Composite R2R - check for RTR_HEADER export
                if (_peReader.TryGetCompositeReadyToRunHeader(out rva))
                {
                    isComposite = true;
                    return true;
                }
            }
            else
            {
                var r2rHeaderDirectory = _peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                if (r2rHeaderDirectory.Size != 0)
                {
                    rva = r2rHeaderDirectory.RelativeVirtualAddress;
                    isComposite = false;
                    return true;
                }
            }

            rva = 0;
            isComposite = false;
            return false;
        }

        public IAssemblyMetadata GetStandaloneAssemblyMetadata()
            => _peReader.HasMetadata ? new StandaloneAssemblyMetadata(_peReader) : null;

        public IAssemblyMetadata GetManifestAssemblyMetadata(System.Reflection.Metadata.MetadataReader manifestReader)
            => new ManifestAssemblyMetadata(_peReader, manifestReader);

        public void DumpImageInformation(TextWriter writer)
        {
            writer.WriteLine($"MetadataSize: {_peReader.PEHeaders.MetadataSize} byte(s)");

            if (_peReader.PEHeaders.PEHeader is PEHeader header)
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

            writer.WriteLine($"CorHeader.Flags: {_peReader.PEHeaders.CorHeader?.Flags}");

            writer.WriteLine("Sections:");
            foreach (var section in _peReader.PEHeaders.SectionHeaders)
                writer.WriteLine($"  {section.Name} {section.VirtualAddress} - {(section.VirtualAddress + section.VirtualSize)}");

            var exportTable = _peReader.GetExportTable();
            exportTable.DumpToConsoleError();
        }

        public Dictionary<string, int> GetSections()
        {
            Dictionary<string, int> sectionMap = [];
            foreach (SectionHeader sectionHeader in _peReader.PEHeaders.SectionHeaders)
            {
                sectionMap.Add(sectionHeader.Name, sectionHeader.SizeOfRawData);
            }

            return sectionMap;
        }
    }
}
