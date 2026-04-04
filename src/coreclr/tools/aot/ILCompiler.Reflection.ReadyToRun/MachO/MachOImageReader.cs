// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace ILCompiler.Reflection.ReadyToRun.MachO
{
    /// <summary>
    /// Wrapper around Mach-O file that implements IBinaryImageReader
    /// </summary>
    public class MachOImageReader : IBinaryImageReader
    {
        private readonly byte[] _image;
        private readonly MachHeader _header;
        private int? _rtrHeaderRva;

        public Machine Machine { get; }
        public OperatingSystem OperatingSystem => OperatingSystem.Apple;
        public ulong ImageBase => 0;

        public MachOImageReader(byte[] image)
        {
            _image = image;

            // Read the MachO header
            Read(0, out _header);
            if (!_header.Is64Bit)
                throw new BadImageFormatException("Only 64-bit Mach-O files are supported");

            // Determine machine type from CPU type
            Machine = GetMachineType(_header.CpuType);
        }

        public ImmutableArray<byte> GetEntireImage()
            => Unsafe.As<byte[], ImmutableArray<byte>>(ref Unsafe.AsRef(in _image));

        public int GetOffset(int rva)
        {
            if (TryGetContainingSegment((ulong)rva, out Segment64LoadCommand segment))
            {
                // Calculate file offset from segment base and RVA
                ulong offsetWithinSegment = (ulong)rva - segment.GetVMAddress(_header);
                ulong fileOffset = segment.GetFileOffset(_header) + offsetWithinSegment;
                System.Diagnostics.Debug.Assert(fileOffset <= int.MaxValue);
                return (int)fileOffset;
            }
            else
            {
                throw new BadImageFormatException("Failed to convert RVA to offset: " + rva);
            }
        }

        public bool TryGetReadyToRunHeader(out int rva, out bool isComposite)
        {
            if (!_rtrHeaderRva.HasValue)
            {
                // Look for RTR_HEADER symbol in the Mach-O symbol table
                // Mach-O R2R images are always composite (no regular R2R format)
                if (TryFindSymbol("RTR_HEADER", out ulong symbolValue))
                {
                    System.Diagnostics.Debug.Assert(symbolValue <= int.MaxValue);
                    _rtrHeaderRva = (int)symbolValue;
                }
                else
                {
                    _rtrHeaderRva = 0;
                }
            }

            rva = _rtrHeaderRva.Value;
            isComposite = rva != 0; // Mach-O R2R images are always composite
            return rva != 0;
        }

        public IAssemblyMetadata GetStandaloneAssemblyMetadata() => null;

        public IAssemblyMetadata GetManifestAssemblyMetadata(System.Reflection.Metadata.MetadataReader manifestReader)
            => new ManifestAssemblyMetadata(manifestReader);

        public void DumpImageInformation(TextWriter writer)
        {
            writer.WriteLine($"FileType: {_header.FileType}");
            writer.WriteLine($"CpuType: 0x{_header.CpuType:X}");
            writer.WriteLine($"NumberOfCommands: {_header.NumberOfCommands}");
            writer.WriteLine($"SizeOfCommands: {_header.SizeOfCommands} byte(s)");

            writer.WriteLine("Sections:");
            EnumerateSections((segmentName, section) =>
            {
                string sectionName = section.SectionName.GetString();
                ulong vmAddr = section.GetVMAddress(_header);
                ulong size = section.GetSize(_header);
                writer.WriteLine($"  {segmentName},{sectionName,-16} 0x{vmAddr:X8} - 0x{vmAddr + size:X8}");
            });
        }

        public Dictionary<string, int> GetSections()
        {
            Dictionary<string, int> sectionMap = [];
            EnumerateSections((segmentName, section) =>
            {
                string sectionName = section.SectionName.GetString();
                ulong size = section.GetSize(_header);
                System.Diagnostics.Debug.Assert(size <= int.MaxValue);
                sectionMap[$"{segmentName},{sectionName}"] = (int)size;
            });
            return sectionMap;
        }

        private unsafe void EnumerateSections(Action<string, Section64LoadCommand> callback)
        {
            long commandsPtr = sizeof(MachHeader);
            for (int i = 0; i < _header.NumberOfCommands; i++)
            {
                Read(commandsPtr, out LoadCommand loadCommand);

                if (loadCommand.GetCommandType(_header) == MachLoadCommandType.Segment64)
                {
                    Read(commandsPtr, out Segment64LoadCommand segment);
                    uint sectionsCount = segment.GetSectionsCount(_header);
                    string segmentName = segment.Name.GetString();

                    // Sections come immediately after the segment load command
                    long sectionPtr = commandsPtr + sizeof(Segment64LoadCommand);
                    for (uint j = 0; j < sectionsCount; j++)
                    {
                        Read(sectionPtr, out Section64LoadCommand section);
                        callback(segmentName, section);
                        sectionPtr += sizeof(Section64LoadCommand);
                    }
                }

                commandsPtr += loadCommand.GetCommandSize(_header);
            }
        }

        private static Machine GetMachineType(uint cpuType)
        {
            // https://github.com/apple-oss-distributions/xnu/blob/f6217f891ac0bb64f3d375211650a4c1ff8ca1ea/osfmk/mach/machine.h
            const uint CPU_TYPE_ARM64 = 0x0100000C;
            const uint CPU_TYPE_X86_64 = 0x01000007;
            return cpuType switch
            {
                CPU_TYPE_ARM64 => Machine.Arm64,
                CPU_TYPE_X86_64 => Machine.Amd64,
                _ => throw new NotSupportedException($"Unsupported MachO CPU type: {cpuType:X8}")
            };
        }

        /// <summary>
        /// Finds a symbol in the symbol table by name.
        /// </summary>
        /// <param name="symbolName">The name of the symbol to find (without leading underscore).</param>
        /// <param name="symbolValue">The value of the symbol if found.</param>
        /// <returns>True if the symbol was found, false otherwise.</returns>
        private unsafe bool TryFindSymbol(string symbolName, out ulong symbolValue)
        {
            symbolValue = 0;

            // Find the symbol table load command
            long commandsPtr = sizeof(MachHeader);
            SymbolTableLoadCommand symtabCommand = default;
            bool foundSymtab = false;

            for (int i = 0; i < _header.NumberOfCommands; i++)
            {
                Read(commandsPtr, out LoadCommand loadCommand);

                if (loadCommand.GetCommandType(_header) == MachLoadCommandType.SymbolTable)
                {
                    Read(commandsPtr, out symtabCommand);
                    foundSymtab = true;
                    break;
                }

                commandsPtr += loadCommand.GetCommandSize(_header);
            }

            if (!foundSymtab || symtabCommand.IsDefault)
            {
                return false;
            }

            uint symbolTableOffset = symtabCommand.GetSymbolTableOffset(_header);
            uint symbolsCount = symtabCommand.GetSymbolsCount(_header);
            uint stringTableOffset = symtabCommand.GetStringTableOffset(_header);
            uint stringTableSize = symtabCommand.GetStringTableSize(_header);

            for (uint i = 0; i < symbolsCount; i++)
            {
                long symOffset = symbolTableOffset + (i * sizeof(NList64));

                // Read the symbol table entry
                Read(symOffset, out NList64 symbol);

                uint strIndex = symbol.GetStringTableIndex(_header);
                if (strIndex >= stringTableSize)
                {
                    continue;
                }

                // Read symbol name from string table
                string name = ReadCString(stringTableOffset + strIndex, stringTableSize - strIndex);

                // Symbol names in Mach-O can have a leading underscore
                if (name == symbolName || (name.Length > 0 && name[0] == '_' && name.AsSpan(1).SequenceEqual(symbolName)))
                {
                    symbolValue = symbol.GetValue(_header);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the segment that contains the specified virtual memory address.
        /// </summary>
        /// <param name="vmAddress">The virtual memory address to find.</param>
        /// <param name="segment">The segment containing the VM address if found.</param>
        /// <returns>True if a containing segment was found, false otherwise.</returns>
        private unsafe bool TryGetContainingSegment(ulong vmAddress, out Segment64LoadCommand segment)
        {
            segment = default;

            // Iterate through all load commands to find segments
            long commandsPtr = sizeof(MachHeader);

            for (int i = 0; i < _header.NumberOfCommands; i++)
            {
                Read(commandsPtr, out LoadCommand loadCommand);

                if (loadCommand.GetCommandType(_header) == MachLoadCommandType.Segment64)
                {
                    Read(commandsPtr, out Segment64LoadCommand seg);

                    // Check if the VM address falls within this segment
                    ulong segmentVMAddr = seg.GetVMAddress(_header);
                    ulong segmentVMSize = seg.GetVMSize(_header);
                    if (vmAddress >= segmentVMAddr && vmAddress < segmentVMAddr + segmentVMSize)
                    {
                        segment = seg;
                        return true;
                    }
                }

                commandsPtr += loadCommand.GetCommandSize(_header);
            }

            return false;
        }

        /// <summary>
        /// Reads a null-terminated C string from the image.
        /// </summary>
        private string ReadCString(uint offset, uint maxLength)
        {
            if (offset < 0 || offset >= _image.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            // Find the null terminator in the image array
            int length;
            long end = Math.Min(offset + maxLength, _image.Length);
            for (length = 0; offset + length < end; length++)
            {
                if (_image[offset + length] == 0)
                {
                    break;
                }
            }

            System.Diagnostics.Debug.Assert(offset <= int.MaxValue);
            return System.Text.Encoding.UTF8.GetString(_image, (int)offset, length);
        }

        public void Read<T>(long offset, out T result) where T : unmanaged
        {
            unsafe
            {
                if (offset < 0 || offset + sizeof(T) > _image.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                fixed (byte* ptr = &_image[offset])
                {
                    result = Unsafe.ReadUnaligned<T>(ptr);
                }
            }
        }
    }
}
