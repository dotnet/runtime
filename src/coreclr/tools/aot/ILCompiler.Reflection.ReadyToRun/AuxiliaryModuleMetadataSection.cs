// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public readonly record struct AuxiliaryModuleEntry(int FilenameRva, int RelativeTableBase, int MethodCount, uint ActivationStatus);

    /// <summary>
    /// The browser-wasm AuxiliaryModuleMetadata section: a fixed header followed by one entry per auxiliary module.
    /// </summary>
    public sealed class AuxiliaryModuleMetadataSection
    {
        public const int HeaderSize = 6 * sizeof(uint);
        public const int EntrySize = 4 * sizeof(uint);

        public int EntryPointsFilenameRva { get; }
        public int InstanceMethodEntryPointsRva { get; }
        public int MethodDefEntryPointsInfoRva { get; }
        public uint EntryPointsModuleActivationStatus { get; }
        public int PrimaryModuleMethodCount { get; }
        public IReadOnlyList<AuxiliaryModuleEntry> Entries { get; }

        /// <summary>Number of bytes occupied by the header and the entry table.</summary>
        public int Size => HeaderSize + Entries.Count * EntrySize;

        public AuxiliaryModuleMetadataSection(ReadyToRunReader reader, int offset, int endOffset)
        {
            if (endOffset - offset < HeaderSize)
                throw new BadImageFormatException("AuxiliaryModuleMetadata section is smaller than its header.");

            NativeReader imageReader = reader.ImageReader;
            EntryPointsFilenameRva = imageReader.ReadInt32(ref offset);
            InstanceMethodEntryPointsRva = imageReader.ReadInt32(ref offset);
            MethodDefEntryPointsInfoRva = imageReader.ReadInt32(ref offset);
            EntryPointsModuleActivationStatus = imageReader.ReadUInt32(ref offset);
            PrimaryModuleMethodCount = imageReader.ReadInt32(ref offset);
            int auxiliaryModuleCount = imageReader.ReadInt32(ref offset);

            if (auxiliaryModuleCount < 0 || (long)auxiliaryModuleCount * EntrySize > endOffset - offset)
                throw new BadImageFormatException($"AuxiliaryModuleMetadata section is too small for {auxiliaryModuleCount} entries.");

            var entries = new AuxiliaryModuleEntry[auxiliaryModuleCount];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new AuxiliaryModuleEntry(
                    imageReader.ReadInt32(ref offset),
                    imageReader.ReadInt32(ref offset),
                    imageReader.ReadInt32(ref offset),
                    imageReader.ReadUInt32(ref offset));
            }

            Entries = entries;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"EntryPointsFilenameRVA:            0x{EntryPointsFilenameRva:X8}");
            sb.AppendLine($"InstanceMethodEntryPointsRVA:      0x{InstanceMethodEntryPointsRva:X8}");
            sb.AppendLine($"MethodDefEntryPointsInfoRVA:       0x{MethodDefEntryPointsInfoRva:X8}");
            sb.AppendLine($"EntryPointsModuleActivationStatus: {EntryPointsModuleActivationStatus}");
            sb.AppendLine($"PrimaryModuleMethodCount:          {PrimaryModuleMethodCount}");
            sb.AppendLine($"AuxiliaryModuleCount:              {Entries.Count}");

            if (Entries.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine("  Index | FilenameRVA | RelativeTableBase | MethodCount | ActivationStatus");
                sb.AppendLine("--------------------------------------------------------------------------");
                for (int i = 0; i < Entries.Count; i++)
                {
                    AuxiliaryModuleEntry entry = Entries[i];
                    sb.AppendLine($"{i,7} |  0x{entry.FilenameRva:X8} | {entry.RelativeTableBase,17} | {entry.MethodCount,11} | {entry.ActivationStatus,16}");
                }
            }

            return sb.ToString();
        }
    }
}
