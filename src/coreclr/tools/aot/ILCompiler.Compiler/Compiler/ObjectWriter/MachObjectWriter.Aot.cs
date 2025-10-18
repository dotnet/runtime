// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.MachNative;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Mach-O object file format writer for Apple macOS and iOS-like targets.
    /// </summary>
    /// <remarks>
    /// Old version of the Mach-O file format specification is mirrored at
    /// https://github.com/aidansteele/osx-abi-macho-file-format-reference.
    ///
    /// There are some notable differences when compared to ELF or COFF:
    /// - The maximum number of sections in object file is limited to 255.
    /// - Sections are subdivided by their symbols and treated by the
    ///   linker as subsections (often referred to as atoms by the linker).
    ///
    /// The consequences of these design decisions is the COMDAT sections are
    /// modeled in entirely different way. Dead code elimination works on the
    /// atom level, so relative relocations within the same section have to be
    /// preserved.
    ///
    /// Debug information uses the standard DWARF format. It is, however, not
    /// linked into the intermediate executable files. Instead the linker creates
    /// a map between the final executable and the object files. Debuggers like
    /// lldb then use this map to read the debug information from the object
    /// file directly. As a consequence the DWARF information is not generated
    /// with relocations for the DWARF sections themselves since it's never
    /// needed.
    ///
    /// While Mach-O uses the DWARF exception handling information for unwind
    /// tables it also supports a compact representation for common prolog types.
    /// Unofficial reference of the format can be found at
    /// https://faultlore.com/blah/compact-unwinding/. It's necessary to emit
    /// at least the stub entries pointing to the DWARF information but due
    /// to limits in the linked file format it's advisable to use the compact
    /// encoding whenever possible.
    ///
    /// The Apple linker is extremely picky in which relocation types are allowed
    /// inside the DWARF sections, both for debugging and exception handling.
    /// </remarks>
    internal sealed partial class MachObjectWriter : UnixObjectWriter
    {
        private sealed record CompactUnwindCode(string PcStartSymbolName, uint PcLength, uint Code, string LsdaSymbolName = null, string PersonalitySymbolName = null);

        // Exception handling sections
        private MachSection _compactUnwindSection;
        private MemoryStream _compactUnwindStream;
        private readonly List<CompactUnwindCode> _compactUnwindCodes = new();
        private readonly uint _compactUnwindDwarfCode;

        private bool IsEhFrameSection(int sectionIndex) => sectionIndex == EhFrameSectionIndex;

        partial void EmitCompactUnwindTable(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            _compactUnwindStream = new MemoryStream(32 * _compactUnwindCodes.Count);
            // Preset the size of the compact unwind section which is not generated yet
            _compactUnwindStream.SetLength(32 * _compactUnwindCodes.Count);

            _compactUnwindSection = new MachSection("__LD", "__compact_unwind", _compactUnwindStream)
            {
                Log2Alignment = 3,
                Flags = S_REGULAR | S_ATTR_DEBUG,
            };

            IList<MachSymbol> symbols = _symbolTable;
            Span<byte> tempBuffer = stackalloc byte[8];
            foreach (var cu in _compactUnwindCodes)
            {
                EmitCompactUnwindSymbol(cu.PcStartSymbolName);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, cu.PcLength);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer.Slice(4), cu.Code);
                _compactUnwindStream.Write(tempBuffer);
                EmitCompactUnwindSymbol(cu.PersonalitySymbolName);
                EmitCompactUnwindSymbol(cu.LsdaSymbolName);
            }

            void EmitCompactUnwindSymbol(string symbolName)
            {
                Span<byte> tempBuffer = stackalloc byte[8];
                if (symbolName is not null)
                {
                    SymbolDefinition symbol = definedSymbols[symbolName];
                    MachSection section = _sections[symbol.SectionIndex];
                    BinaryPrimitives.WriteUInt64LittleEndian(tempBuffer, section.VirtualAddress + (ulong)symbol.Value);
                    _compactUnwindSection.Relocations.Add(
                        new MachRelocation
                        {
                            Address = (int)_compactUnwindStream.Position,
                            SymbolOrSectionIndex = (byte)(1 + symbol.SectionIndex), // 1-based
                            Length = 8,
                            RelocationType = ARM64_RELOC_UNSIGNED,
                            IsExternal = false,
                            IsPCRelative = false,
                        }
                    );
                }
                _compactUnwindStream.Write(tempBuffer);
            }
        }

        private static uint GetArm64CompactUnwindCode(byte[] blobData)
        {
            if (blobData == null || blobData.Length == 0)
            {
                return UNWIND_ARM64_MODE_FRAMELESS;
            }

            Debug.Assert(blobData.Length % 8 == 0);

            short spReg = -1;

            int codeOffset = 0;
            short cfaRegister = spReg;
            int cfaOffset = 0;
            int spOffset = 0;

            const int REG_DWARF_X19 = 19;
            const int REG_DWARF_X30 = 30;
            const int REG_DWARF_FP = 29;
            const int REG_DWARF_D8 = 72;
            const int REG_DWARF_D15 = 79;
            const int REG_IDX_X19 = 0;
            const int REG_IDX_X28 = 9;
            const int REG_IDX_FP = 10;
            const int REG_IDX_LR = 11;
            const int REG_IDX_D8 = 12;
            const int REG_IDX_D15 = 19;
            Span<int> registerOffset = stackalloc int[20];

            registerOffset.Fill(int.MinValue);

            // First process all the CFI codes to figure out the layout of X19-X28, FP, LR, and
            // D8-D15 on the stack.
            int offset = 0;
            while (offset < blobData.Length)
            {
                codeOffset = Math.Max(codeOffset, blobData[offset++]);
                CFI_OPCODE opcode = (CFI_OPCODE)blobData[offset++];
                short dwarfReg = BinaryPrimitives.ReadInt16LittleEndian(blobData.AsSpan(offset));
                offset += sizeof(short);
                int cfiOffset = BinaryPrimitives.ReadInt32LittleEndian(blobData.AsSpan(offset));
                offset += sizeof(int);

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfaRegister = dwarfReg;

                        if (spOffset != 0)
                        {
                            for (int i = 0; i < registerOffset.Length; i++)
                                if (registerOffset[i] != int.MinValue)
                                    registerOffset[i] -= spOffset;

                            cfaOffset += spOffset;
                            spOffset = 0;
                        }

                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        Debug.Assert(cfaRegister == spReg);
                        if (dwarfReg >= REG_DWARF_X19 && dwarfReg <= REG_DWARF_X30) // X19 - X28, FP, LR
                        {
                            registerOffset[dwarfReg - REG_DWARF_X19 + REG_IDX_X19] = cfiOffset;
                        }
                        else if (dwarfReg >= REG_DWARF_D8 && dwarfReg <= REG_DWARF_D15) // D8 - D15
                        {
                            registerOffset[dwarfReg - REG_DWARF_D8 + REG_IDX_D8] = cfiOffset;
                        }
                        else
                        {
                            // We cannot represent this register in the compact unwinding format,
                            // fallback to DWARF immediately.
                            return UNWIND_ARM64_MODE_DWARF;
                        }
                        break;

                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        if (cfaRegister != spReg)
                        {
                            cfaOffset += cfiOffset;
                        }
                        else
                        {
                            spOffset += cfiOffset;

                            for (int i = 0; i < registerOffset.Length; i++)
                                if (registerOffset[i] != int.MinValue)
                                    registerOffset[i] += cfiOffset;
                        }
                        break;
                }
            }

            uint unwindCode;
            int nextOffset;

            if (cfaRegister == REG_DWARF_FP &&
                cfaOffset == 16 &&
                registerOffset[REG_IDX_FP] == -16 &&
                registerOffset[REG_IDX_LR] == -8)
            {
                // Frame format - FP/LR are saved on the top. SP is restored to FP+16
                unwindCode = UNWIND_ARM64_MODE_FRAME;
                nextOffset = -24;
            }
            else if (cfaRegister == -1 && spOffset <= 65520 &&
                     registerOffset[REG_IDX_FP] == int.MinValue && registerOffset[REG_IDX_LR] == int.MinValue)
            {
                // Frameless format - FP/LR are not saved, SP must fit within the representable range
                uint encodedSpOffset = (uint)(spOffset / 16) << 12;
                unwindCode = UNWIND_ARM64_MODE_FRAMELESS | encodedSpOffset;
                nextOffset = spOffset - 8;
            }
            else
            {
                return UNWIND_ARM64_MODE_DWARF;
            }

            // Check that the integer register pairs are in the right order and mark
            // a flag for each successive pair that is present.
            for (int i = REG_IDX_X19; i < REG_IDX_X28; i += 2)
            {
                if (registerOffset[i] == int.MinValue)
                {
                    if (registerOffset[i + 1] != int.MinValue)
                        return UNWIND_ARM64_MODE_DWARF;
                }
                else if (registerOffset[i] == nextOffset)
                {
                    if (registerOffset[i + 1] != nextOffset - 8)
                        return UNWIND_ARM64_MODE_DWARF;
                    nextOffset -= 16;
                    unwindCode |= UNWIND_ARM64_FRAME_X19_X20_PAIR << (i >> 1);
                }
            }

            // Check that the floating point register pairs are in the right order and mark
            // a flag for each successive pair that is present.
            for (int i = REG_IDX_D8; i < REG_IDX_D15; i += 2)
            {
                if (registerOffset[i] == int.MinValue)
                {
                    if (registerOffset[i + 1] != int.MinValue)
                        return UNWIND_ARM64_MODE_DWARF;
                }
                else if (registerOffset[i] == nextOffset)
                {
                    if (registerOffset[i + 1] != nextOffset - 8)
                        return UNWIND_ARM64_MODE_DWARF;
                    nextOffset -= 16;
                    unwindCode |= UNWIND_ARM64_FRAME_D8_D9_PAIR << (i >> 1);
                }
            }

            return unwindCode;
        }

        private protected override bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob)
        {
            uint encoding = _compactUnwindDwarfCode;

            if (_cpuType == CPU_TYPE_ARM64)
            {
                encoding = GetArm64CompactUnwindCode(blob);
            }

            _compactUnwindCodes.Add(new CompactUnwindCode(
                PcStartSymbolName: startSymbolName,
                PcLength: (uint)length,
                Code: encoding | (encoding != _compactUnwindDwarfCode && lsdaSymbolName is not null ? 0x40000000u : 0), // UNWIND_HAS_LSDA
                LsdaSymbolName: encoding != _compactUnwindDwarfCode ? lsdaSymbolName : null
            ));

            return encoding != _compactUnwindDwarfCode;
        }

        private protected override bool UseFrameNames => true;
    }
}
