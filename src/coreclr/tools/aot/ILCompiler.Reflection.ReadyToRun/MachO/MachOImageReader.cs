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
        private readonly Machine _machine;
        private readonly ulong _imageBase;
        private int? _rtrHeaderRva;

        public MachOImageReader(byte[] image)
        {
            _image = image;

            // Determine machine type from MachO CPU type
            _machine = DetermineMachineType();
            _imageBase = 0; // MachO typically uses 0 as base for .dylib files
        }

        private Machine DetermineMachineType()
        {
            // For now, we'll need to read the MachO header directly from the image
            // to determine the CPU type since MachObjectFile doesn't expose this
            unsafe
            {
                fixed (byte* imagePtr = _image)
                {
                    uint magic = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_image, 0, 4));

                    // Check if this is a 64-bit Mach-O
                    bool is64Bit = (MachMagic)magic is MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian;

                    if (!is64Bit)
                    {
                        throw new BadImageFormatException("Only 64-bit Mach-O files are supported");
                    }

                    // Read CPU type at offset 4
                    uint cpuType = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_image, 4, 4));

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
            }
        }

        public ImmutableArray<byte> GetEntireImage()
        {
            return Unsafe.As<byte[], ImmutableArray<byte>>(ref Unsafe.AsRef(in _image));
        }

        public int GetOffset(int rva)
        {
            if (TryGetContainingSegment((ulong)rva, out Segment64LoadCommand segment, out MachHeader header))
            {
                // Calculate file offset from segment base and RVA
                ulong offsetWithinSegment = (ulong)rva - segment.GetVMAddress(header);
                ulong fileOffset = segment.GetFileOffset(header) + offsetWithinSegment;
                return (int)fileOffset;
            }
            else
            {
                throw new BadImageFormatException("Failed to convert RVA to offset: " + rva);
            }
        }

        public bool TryGetCompositeReadyToRunHeader(out int rva)
        {
            if (!_rtrHeaderRva.HasValue)
            {
                // Look for RTR_HEADER symbol in the Mach-O symbol table
                _rtrHeaderRva = FindRTRHeaderSymbol();
            }

            rva = _rtrHeaderRva.Value;
            return rva != 0;
        }

        private int FindRTRHeaderSymbol()
        {
            if (TryFindSymbol("RTR_HEADER", out ulong symbolValue))
            {
                return (int)symbolValue;
            }

            return 0;
        }

        public Machine Machine => _machine;

        public OperatingSystem OperatingSystem => OperatingSystem.Apple;

        public bool HasMetadata => false; // Mach-O composite R2R images don't have embedded ECMA metadata

        public ulong ImageBase => _imageBase;

        public bool IsILLibrary => false;

        public void DumpImageInformation(TextWriter writer)
        {
            // TODO: Print information from the Mach-O header
        }

        public Dictionary<string, int> GetSections()
        {
            // TODO: Get all the sections
            return [];
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

            // Read the header
            Read(0, out MachHeader header);

            // Find the symbol table load command
            long commandsPtr = sizeof(MachHeader);
            SymbolTableLoadCommand symtabCommand = default;
            bool foundSymtab = false;

            for (int i = 0; i < header.NumberOfCommands; i++)
            {
                Read(commandsPtr, out LoadCommand loadCommand);

                if (loadCommand.GetCommandType(header) == MachLoadCommandType.SymbolTable)
                {
                    Read(commandsPtr, out symtabCommand);
                    foundSymtab = true;
                    break;
                }

                commandsPtr += loadCommand.GetCommandSize(header);
            }

            if (!foundSymtab || symtabCommand.IsDefault)
            {
                return false;
            }

            uint symbolTableOffset = symtabCommand.GetSymbolTableOffset(header);
            uint symbolsCount = symtabCommand.GetSymbolsCount(header);
            uint stringTableOffset = symtabCommand.GetStringTableOffset(header);
            uint stringTableSize = symtabCommand.GetStringTableSize(header);

            for (uint i = 0; i < symbolsCount; i++)
            {
                long symOffset = symbolTableOffset + (i * sizeof(NList64));

                // Read the symbol table entry
                Read(symOffset, out NList64 symbol);

                uint strIndex = symbol.GetStringTableIndex(header);
                if (strIndex >= stringTableSize)
                {
                    continue;
                }

                // Read symbol name from string table
                string name = ReadCString(stringTableOffset + strIndex, stringTableSize - strIndex);

                // Symbol names in Mach-O can have a leading underscore
                if (name == symbolName || (name.Length > 0 && name[0] == '_' && name.Substring(1) == symbolName))
                {
                    symbolValue = symbol.GetValue(header);
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
        /// <param name="header">The Mach-O header (output parameter).</param>
        /// <returns>True if a containing segment was found, false otherwise.</returns>
        private unsafe bool TryGetContainingSegment(ulong vmAddress, out Segment64LoadCommand segment, out MachHeader header)
        {
            segment = default;

            // Read the header
            Read(0, out header);

            // Iterate through all load commands to find segments
            long commandsPtr = sizeof(MachHeader);

            for (int i = 0; i < header.NumberOfCommands; i++)
            {
                Read(commandsPtr, out LoadCommand loadCommand);

                if (loadCommand.GetCommandType(header) == MachLoadCommandType.Segment64)
                {
                    Read(commandsPtr, out Segment64LoadCommand seg);

                    // Check if the VM address falls within this segment
                    ulong segmentVMAddr = seg.GetVMAddress(header);
                    ulong segmentVMSize = seg.GetVMSize(header);
                    if (vmAddress >= segmentVMAddr && vmAddress < segmentVMAddr + segmentVMSize)
                    {
                        segment = seg;
                        return true;
                    }
                }

                commandsPtr += loadCommand.GetCommandSize(header);
            }

            return false;
        }

        /// <summary>
        /// Reads a null-terminated C string from the image.
        /// </summary>
        private string ReadCString(long offset, uint maxLength)
        {
            if (offset < 0 || offset >= _image.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            // Find the null terminator in the image array
            int length;
            int end = (int)Math.Min(offset + maxLength, _image.Length);
            for (length = 0; offset + length < end; length++)
            {
                if (_image[offset + length] == 0)
                {
                    break;
                }
            }

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
                    result = *(T*)ptr;
                }
            }
        }
    }
}
