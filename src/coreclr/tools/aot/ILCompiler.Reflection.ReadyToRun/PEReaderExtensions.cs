// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

using Internal.Runtime;
using Internal.ReadyToRunConstants;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class PEExportTable
    {
        public readonly bool HasExportTable;
        public readonly int ExportTableHeaderLength;

        private readonly Dictionary<string, int> _namedExportRva;
        private readonly Dictionary<int, int> _ordinalRva;

        private PEExportTable(PEReader peReader)
        {
            _namedExportRva = new Dictionary<string, int>();
            _ordinalRva = new Dictionary<int, int>();

            DirectoryEntry exportTable = peReader.PEHeaders.PEHeader.ExportTableDirectory;
            if ((exportTable.Size == 0) || (exportTable.RelativeVirtualAddress == 0))
                return;

            HasExportTable = true;

            PEMemoryBlock peImage = peReader.GetEntireImage();
            BlobReader exportTableHeader = peImage.GetReader(peReader.GetOffset(exportTable.RelativeVirtualAddress), exportTable.Size);
            if (exportTableHeader.Length == 0)
            {
                return;
            }

            ExportTableHeaderLength = exportTableHeader.Length;

            // +0x00: reserved
            exportTableHeader.ReadUInt32();
            // +0x04: TODO: time/date stamp
            exportTableHeader.ReadUInt32();
            // +0x08: major version
            exportTableHeader.ReadUInt16();
            // +0x0A: minor version
            exportTableHeader.ReadUInt16();
            // +0x0C: DLL name RVA
            exportTableHeader.ReadUInt32();
            // +0x10: ordinal base
            int minOrdinal = exportTableHeader.ReadInt32();
            // +0x14: number of entries in the address table
            int addressEntryCount = exportTableHeader.ReadInt32();
            // +0x18: number of name pointers
            int namePointerCount = exportTableHeader.ReadInt32();
            // +0x1C: export address table RVA
            int addressTableRVA = exportTableHeader.ReadInt32();
            // +0x20: name pointer RVA
            int namePointerRVA = exportTableHeader.ReadInt32();
            // +0x24: ordinal table RVA
            int ordinalTableRVA = exportTableHeader.ReadInt32();

            int[] addressTable = new int[addressEntryCount];
            BlobReader addressTableReader = peImage.GetReader(peReader.GetOffset(addressTableRVA), sizeof(int) * addressEntryCount);
            for (int entryIndex = 0; entryIndex < addressEntryCount; entryIndex++)
            {
                addressTable[entryIndex] = addressTableReader.ReadInt32();
            }

            ushort[] ordinalTable = new ushort[namePointerCount];
            BlobReader ordinalTableReader = peImage.GetReader(peReader.GetOffset(ordinalTableRVA), sizeof(ushort) * namePointerCount);
            for (int entryIndex = 0; entryIndex < namePointerCount; entryIndex++)
            {
                ushort ordinalIndex = ordinalTableReader.ReadUInt16();
                ordinalTable[entryIndex] = ordinalIndex;
                _ordinalRva.Add(entryIndex + minOrdinal, addressTable[ordinalIndex]);
            }

            BlobReader namePointerReader = peImage.GetReader(peReader.GetOffset(namePointerRVA), sizeof(int) * namePointerCount);
            for (int entryIndex = 0; entryIndex < namePointerCount; entryIndex++)
            {
                int nameRVA = namePointerReader.ReadInt32();
                if (nameRVA != 0)
                {
                    int nameOffset = peReader.GetOffset(nameRVA);
                    BlobReader nameReader = peImage.GetReader(nameOffset, peImage.Length - nameOffset);
                    StringBuilder nameBuilder = new StringBuilder();
                    for (byte ascii; (ascii = nameReader.ReadByte()) != 0;)
                    {
                        nameBuilder.Append((char)ascii);
                    }
                    _namedExportRva.Add(nameBuilder.ToString(), addressTable[ordinalTable[entryIndex]]);
                }
                else
                {
                    Console.Error.WriteLine($"Found a zero RVA when reading name pointers for entry #{entryIndex}/{namePointerCount}");
                }
            }
        }

        public static PEExportTable Parse(PEReader peReader)
        {
            return new PEExportTable(peReader);
        }

        public void DumpToConsoleError()
        {
            Console.Error.WriteLine($"HasExportTable: {HasExportTable}");
            Console.Error.WriteLine($"ExportTableHeaderLength: {ExportTableHeaderLength}");
            Console.Error.WriteLine($"_namedExportRva: {_namedExportRva.Count} item(s)");
            int i = 0;
            foreach (var kvp in _namedExportRva)
            {
                Console.Error.WriteLine($"  '{kvp.Key}': {kvp.Value}");
                if (i++ > 64)
                {
                    Console.Error.WriteLine("  ... stopped dumping named exports because there are too many.");
                    break;
                }
            }
        }

        public bool TryGetValue(string exportName, out int rva) => _namedExportRva.TryGetValue(exportName, out rva);
        public bool TryGetValue(int ordinal, out int rva) => _ordinalRva.TryGetValue(ordinal, out rva);
    }

    public static class PEReaderExtensions
    {
        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="reader">PE reader representing the executable image to parse</param>
        /// <param name="rva">The relative virtual address</param>
        public static int GetOffset(this PEReader reader, int rva)
        {
            int index = reader.PEHeaders.GetContainingSectionIndex(rva);
            if (index == -1)
            {
                throw new BadImageFormatException("Failed to convert invalid RVA to offset: " + rva);
            }
            SectionHeader containingSection = reader.PEHeaders.SectionHeaders[index];
            return rva - containingSection.VirtualAddress + containingSection.PointerToRawData;
        }

        /// <summary>
        /// Parse export table directory for a given PE reader.
        /// </summary>
        /// <param name="reader">PE reader representing the executable image to parse</param>
        public static PEExportTable GetExportTable(this PEReader reader)
        {
            return PEExportTable.Parse(reader);
        }

        /// <summary>
        /// Check whether the file is a composite ReadyToRun image and returns the RVA of its ReadyToRun header if positive.
        /// </summary>
        /// <param name="reader">PEReader representing the executable to check for the presence of ReadyToRun header</param>
        /// <param name="rva">RVA of the ReadyToRun header if available, 0 when not</param>
        /// <returns>true when the PEReader represents a ReadyToRun image, false otherwise</returns>
        public static bool TryGetCompositeReadyToRunHeader(this PEReader reader, out int rva)
        {
            return reader.GetExportTable().TryGetValue("RTR_HEADER", out rva);
        }

        /// <summary>
        /// Check whether the file is a ReadyToRun image created from platform neutral (AnyCPU) IL image.
        /// </summary>
        /// <param name="reader">PEReader representing the executable to check</param>
        /// <returns>true when the PEReader represents a ReadyToRun image created from AnyCPU IL image, false otherwise</returns>
        public static bool IsReadyToRunPlatformNeutralSource(this PEReader peReader)
        {
            var managedNativeDirectory = peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
            if (managedNativeDirectory.Size < 16 /* sizeof(ReadyToRunHeader) */)
                return false;

            var reader = peReader.GetSectionData(managedNativeDirectory.RelativeVirtualAddress).GetReader();
            if (reader.ReadUInt32() != ReadyToRunHeaderConstants.Signature)
                return false;

            reader.ReadUInt16(); // MajorVersion
            reader.ReadUInt16(); // MinorVersion

            return (reader.ReadUInt32() & (uint)ReadyToRunFlags.READYTORUN_FLAG_PlatformNeutralSource) != 0;
        }
    }
}
