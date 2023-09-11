// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Numerics;
using System.Buffers;
using System.Buffers.Binary;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

using Melanzana.MachO;
using LibObjectFile.Dwarf;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public class MachObjectWriter : UnixObjectWriter
    {
        private sealed record CompactUnwindCode(string PcStartSymbolName, uint PcLength, uint Code, string LsdaSymbolName = null, string PersonalitySymbolName = null);

        private TargetOS _targetOS;
        private MachObjectFile _objectFile;
        private MachSegment _segment;

        // Exception handling sections
        private MachSection _compactUnwindSection;
        private List<CompactUnwindCode> _compactUnwindCodes = new();
        private uint _compactUnwindDwarfCode;

        // Symbol table
        private Dictionary<int, uint> _sectionIndexToSymbolIndex = new();
        private Dictionary<string, uint> _symbolNameToIndex = new();
        private MachSymbolTable _symbolTable;
        private MachDynamicLinkEditSymbolTable _dySymbolTable;

        private MachObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _objectFile = new MachObjectFile();
            _objectFile.FileType = MachFileType.Object;
            _objectFile.IsLittleEndian = true;

            switch (factory.Target.Architecture)
            {
                case TargetArchitecture.ARM64:
                    _objectFile.CpuType = MachCpuType.Arm64;
                    _objectFile.CpuSubType = (uint)MachArm64CpuSubType.All;
                    _compactUnwindDwarfCode = 0x3_00_00_00u;
                    break;
                case TargetArchitecture.X64:
                    _objectFile.CpuType = MachCpuType.X86_64;
                    _objectFile.CpuSubType = (uint)X8664CpuSubType.All;
                    _compactUnwindDwarfCode = 0x4_00_00_00u;
                    break;
                default:
                    throw new NotSupportedException("Unsupported architecture");
            }

            // Mach-O object files have single segment with no name
            _segment = new MachSegment(_objectFile, "")
            {
                InitialProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
                MaximumProtection = MachVmProtection.Execute | MachVmProtection.Read | MachVmProtection.Write,
            };
            _objectFile.LoadCommands.Add(_segment);

            _targetOS = factory.Target.OperatingSystem;
        }

        protected override void EmitSectionsAndLayout()
        {
            _compactUnwindSection = new MachSection(_objectFile, "__LD", "__compact_unwind")
            {
                Log2Alignment = 3,
                Type = MachSectionType.Regular,
                Attributes = MachSectionAttributes.Debug,
                // Preset the size of the compact unwind section which is not generated yet
                Size = 32u * (ulong)_compactUnwindCodes.Count,
            };
            _segment.Sections.Add(_compactUnwindSection);

            // Insert all the load commands to ensure we have correct layout
            _symbolTable = new MachSymbolTable(_objectFile);
            _dySymbolTable = new MachDynamicLinkEditSymbolTable();
            _objectFile.LoadCommands.Add(_symbolTable);
            _objectFile.LoadCommands.Add(_dySymbolTable);

            var sdkVersion = new Version(16, 0, 0);
            switch (_targetOS)
            {
                case TargetOS.OSX:
                    _objectFile.LoadCommands.Add(new MachVersionMinMacOS
                    {
                        MinimumPlatformVersion = new Version(10, 12, 0),
                        SdkVersion = sdkVersion,
                    });
                    break;

                case TargetOS.MacCatalyst:
                    _objectFile.LoadCommands.Add(new MachBuildVersion
                    {
                        TargetPlatform = MachPlatform.MacCatalyst,
                        MinimumPlatformVersion = _objectFile.CpuType switch {
                            MachCpuType.X86_64 => new Version(13, 5, 0),
                            _ => new Version(14, 2, 0),
                        },
                        SdkVersion = sdkVersion,
                    });
                    break;

                case TargetOS.iOS:
                case TargetOS.iOSSimulator:
                case TargetOS.tvOS:
                case TargetOS.tvOSSimulator:
                    _objectFile.LoadCommands.Add(new MachBuildVersion
                    {
                        TargetPlatform = _targetOS switch
                        {
                            TargetOS.iOS => MachPlatform.IOS,
                            TargetOS.iOSSimulator => MachPlatform.IOSSimulator,
                            TargetOS.tvOS => MachPlatform.TvOS,
                            TargetOS.tvOSSimulator => MachPlatform.TvOSSimulator,
                            _ => (MachPlatform)0,
                        },
                        MinimumPlatformVersion = new Version(11, 0, 0),
                        SdkVersion = sdkVersion,
                    });
                    break;
            }

            // Layout the sections
            _objectFile.UpdateLayout();

            EmitCompactUnwindTable();
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            // Update layout again to account for symbol table and relocation tables
            _objectFile.UpdateLayout();

            using (var outputFileStream = new FileStream(objectFilePath, FileMode.Create))
            {
                MachWriter.Write(outputFileStream, _objectFile);
            }
        }

        protected override void CreateSection(ObjectNodeSection section, out Stream sectionStream)
        {
            string segmentName = section.Name switch
            {
                "rdata" => "__TEXT",
                ".eh_frame" => "__TEXT",
                _ => section.Type == SectionType.Executable ? "__TEXT" : "__DATA"
            };

            string sectionName = section.Name switch
            {
                "text" => "__text",
                "data" => "__data",
                "rdata" => "__const",
                "bss" => "__bss",
                ".eh_frame" => "__eh_frame",
                _ => section.Name
            };

            MachSectionAttributes attributes = section.Name switch
            {
                ".dotnet_eh_table" => MachSectionAttributes.Debug,
                ".eh_frame" => MachSectionAttributes.LiveSupport | MachSectionAttributes.StripStaticSymbols | MachSectionAttributes.NoTableOfContents,
                _ => section.Type == SectionType.Executable ?
                    MachSectionAttributes.SomeInstructions | MachSectionAttributes.PureInstructions : 0
            };

            MachSectionType type = section.Name switch
            {
                "bss" => MachSectionType.ZeroFill,
                ".eh_frame" => MachSectionType.Coalesced,
                _ => section.Type == SectionType.Uninitialized ? MachSectionType.ZeroFill : MachSectionType.Regular
            };

            MachSection machSection = new MachSection(_objectFile, segmentName, sectionName)
            {
                Log2Alignment = 1,
                Type = type,
                Attributes = attributes,
            };

            sectionStream = machSection.GetWriteStream();
            _segment.Sections.Add(machSection);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            var machSection = _segment.Sections[sectionIndex];
            Debug.Assert(BitOperations.IsPow2(alignment));
            machSection.Log2Alignment = Math.Max(machSection.Log2Alignment, (uint)BitOperations.Log2((uint)alignment));
        }

        protected internal override void EmitRelocation(
            int sectionIndex,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            // For most relocations we write the addend directly into the
            // data. The exceptions are IMAGE_REL_BASED_ARM64_PAGEBASE_REL21
            // and IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A.

            if (relocType == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26)
            {
                Debug.Assert(_objectFile.CpuType == MachCpuType.Arm64);
                Debug.Assert(addend == 0);
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_DIR64)
            {
                if (addend != 0)
                {
                    BinaryPrimitives.WriteInt64LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt64LittleEndian(data) +
                        addend);
                    addend = 0;
                }
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_RELPTR32)
            {
                if (_objectFile.CpuType == MachCpuType.Arm64)
                {
                    // On ARM64 we need to represent PC relative relocations as
                    // subtraction and the PC offset is baked into the addend.
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        addend - offset);
                }
                else if (sectionIndex == EhFrameSectionIndex)
                {
                    // ld64 requires X86_64_RELOC_SUBTRACTOR + X86_64_RELOC_UNSIGNED
                    // for DWARF CFI sections
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        addend - offset);
                }
                else
                {
                    addend += 4;
                    if (addend != 0)
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(
                            data,
                            BinaryPrimitives.ReadInt32LittleEndian(data) +
                            addend);
                    }
                }
                addend = 0;
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_REL32)
            {
                Debug.Assert(_objectFile.CpuType != MachCpuType.Arm64);
                if (addend != 0)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        addend);
                    addend = 0;
                }
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        protected override void EmitSymbolTable()
        {
            uint symbolIndex = 0;

            // Create section base symbols. They're used for PC relative relocations
            // to subtract the base of the section.
            int sectionIndex = 0;
            foreach (var machSection in _segment.Sections)
            {
                var machSymbol = new MachSymbol
                {
                    Name = "lsection" + sectionIndex,
                    Section = machSection,
                    Value = machSection.VirtualAddress,
                    Descriptor = 0,
                    Type = MachSymbolType.Section,
                };
                _symbolTable.Symbols.Add(machSymbol);
                _sectionIndexToSymbolIndex[sectionIndex] = symbolIndex;
                symbolIndex++;
                sectionIndex++;
            }

            _dySymbolTable.LocalSymbolsIndex = 0;
            _dySymbolTable.LocalSymbolsCount = symbolIndex;

            // Sort and insert all defined symbols
            var definedSymbols = GetDefinedSymbols();
            var sortedDefinedSymbols = new List<MachSymbol>(definedSymbols.Count);
            foreach (var (name, definition) in definedSymbols)
            {
                var section = _segment.Sections[definition.SectionIndex];
                sortedDefinedSymbols.Add(new MachSymbol
                {
                    Name = name,
                    Section = section,
                    Value = section.VirtualAddress + (ulong)definition.Value,
                    Descriptor = 0,
                    Type = MachSymbolType.Section | MachSymbolType.External,
                });
            }
            sortedDefinedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (var definedSymbol in sortedDefinedSymbols)
            {
                _symbolTable.Symbols.Add(definedSymbol);
                _symbolNameToIndex[definedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            _dySymbolTable.ExternalSymbolsIndex = _dySymbolTable.LocalSymbolsCount;
            _dySymbolTable.ExternalSymbolsCount = (uint)definedSymbols.Count;

            List<MachSymbol> undefinedSymbols = new List<MachSymbol>();
            foreach (var externSymbol in GetUndefinedSymbols())
            {
                var machSymbol = new MachSymbol
                {
                    Name = externSymbol,
                    Section = null,
                    Value = 0,
                    Descriptor = 0,
                    Type = MachSymbolType.Undefined | MachSymbolType.External,
                };
                undefinedSymbols.Add(machSymbol);
            }

            // Sort and insert all undefined external symbols
            undefinedSymbols.Sort((symA, symB) => string.CompareOrdinal(symA.Name, symB.Name));
            foreach (var undefinedSymbol in undefinedSymbols)
            {
                _symbolTable.Symbols.Add(undefinedSymbol);
                _symbolNameToIndex[undefinedSymbol.Name] = symbolIndex;
                symbolIndex++;
            }

            _dySymbolTable.UndefinedSymbolsIndex = _dySymbolTable.LocalSymbolsCount + _dySymbolTable.ExternalSymbolsCount;
            _dySymbolTable.UndefinedSymbolsCount = (uint)undefinedSymbols.Count;
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            if (_objectFile.CpuType == MachCpuType.Arm64)
            {
                EmitRelocationsArm64(sectionIndex, relocationList);
            }
            else
            {
                EmitRelocationsX64(sectionIndex, relocationList);
            }
        }

        private void EmitRelocationsX64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            ICollection<MachRelocation> sectionRelocations = _segment.Sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (var symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_DIR64)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 8,
                            RelocationType = MachRelocationType.X86_64Unsigned,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_RELPTR32 && sectionIndex == EhFrameSectionIndex)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = _sectionIndexToSymbolIndex[sectionIndex],
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Subtractor,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Unsigned,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_RELPTR32 ||
                    symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_REL32)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.X86_64Branch,
                            IsExternal = true,
                            IsPCRelative = true,
                        });
                }
                else
                {
                    throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type);
                }
            }
        }

        private void EmitRelocationsArm64(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            ICollection<MachRelocation> sectionRelocations = _segment.Sections[sectionIndex].Relocations;

            relocationList.Reverse();
            foreach (var symbolicRelocation in relocationList)
            {
                uint symbolIndex = _symbolNameToIndex[symbolicRelocation.SymbolName];

                if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Branch26,
                            IsExternal = true,
                            IsPCRelative = true,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 ||
                    symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A)
                {
                    if (symbolicRelocation.Addend != 0)
                    {
                        sectionRelocations.Add(
                            new MachRelocation
                            {
                                Address = symbolicRelocation.Offset,
                                SymbolOrSectionIndex = (uint)symbolicRelocation.Addend,
                                Length = 4,
                                RelocationType = MachRelocationType.Arm64Addend,
                                IsExternal = false,
                                IsPCRelative = false,
                            });
                    }

                    MachRelocationType type = symbolicRelocation.Type switch
                    {
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => MachRelocationType.Arm64Page21,
                        RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => MachRelocationType.Arm64PageOffset21,
                        _ => 0
                    };

                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = type,
                            IsExternal = true,
                            IsPCRelative = symbolicRelocation.Type != RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_DIR64)
                {
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 8,
                            RelocationType = MachRelocationType.Arm64Unsigned,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else if (symbolicRelocation.Type == RelocType.IMAGE_REL_BASED_RELPTR32)
                {
                    // This one is tough... needs to be represented by ARM64_RELOC_SUBTRACTOR + ARM64_RELOC_UNSIGNED.
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = _sectionIndexToSymbolIndex[sectionIndex],
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Subtractor,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                    sectionRelocations.Add(
                        new MachRelocation
                        {
                            Address = symbolicRelocation.Offset,
                            SymbolOrSectionIndex = symbolIndex,
                            Length = 4,
                            RelocationType = MachRelocationType.Arm64Unsigned,
                            IsExternal = true,
                            IsPCRelative = false,
                        });
                }
                else
                {
                    throw new NotSupportedException("Unknown relocation type: " + symbolicRelocation.Type);
                }
            }
        }

        private void EmitCompactUnwindTable()
        {
            using var compactUnwindStream = _compactUnwindSection.GetWriteStream();
            var definedSymbols = GetDefinedSymbols();

            IList<MachSymbol> symbols = _symbolTable.Symbols;
            Span<byte> tempBuffer = stackalloc byte[8];
            foreach (var cu in _compactUnwindCodes)
            {
                EmitCompactUnwindSymbol(cu.PcStartSymbolName);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, cu.PcLength);
                BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer.Slice(4), cu.Code);
                compactUnwindStream.Write(tempBuffer);
                EmitCompactUnwindSymbol(cu.PersonalitySymbolName);
                EmitCompactUnwindSymbol(cu.LsdaSymbolName);
            }

            void EmitCompactUnwindSymbol(string symbolName)
            {
                Span<byte> tempBuffer = stackalloc byte[8];
                if (symbolName != null)
                {
                    SymbolDefinition symbol = definedSymbols[symbolName];
                    MachSection section = _segment.Sections[symbol.SectionIndex];
                    BinaryPrimitives.WriteUInt64LittleEndian(tempBuffer, section.VirtualAddress + (ulong)symbol.Value);
                    _compactUnwindSection.Relocations.Add(
                        new MachRelocation
                        {
                            Address = (int)compactUnwindStream.Position,
                            SymbolOrSectionIndex = (byte)(1 + symbol.SectionIndex), // 1-based
                            Length = 8,
                            RelocationType = MachRelocationType.Arm64Unsigned,
                            IsExternal = false,
                            IsPCRelative = false,
                        }
                    );
                }
                compactUnwindStream.Write(tempBuffer);
            }
        }

        protected override string ExternCName(string name) => "_" + name;

        // This represents the following DWARF code:
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_offset: +16
        //   DW_CFA_offset: W29 -16
        //   DW_CFA_offset: W30 -8
        //   DW_CFA_advance_loc: 4
        //   DW_CFA_def_cfa_register: W29
        // which is generated for the following frame prolog/epilog:
        //   stp fp, lr, [sp, #-10]!
        //   mov fp, sp
        //   ...
        //   ldp fp, lr, [sp], #0x10
        //   ret
        private static ReadOnlySpan<byte> DwarfArm64EmptyFrame => new byte[]
        {
            0x04, 0x00, 0xFF, 0xFF, 0x10, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x02, 0x1E, 0x00, 0x08, 0x00, 0x00, 0x00,
            0x08, 0x01, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        protected override bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob)
        {
            uint encoding = _compactUnwindDwarfCode;

            if (_objectFile.CpuType == MachCpuType.Arm64)
            {
                if (blob.AsSpan().SequenceEqual(DwarfArm64EmptyFrame))
                {
                    // Frame-based encoding, no saved registers
                    encoding = 0x04000000;
                }
            }

            _compactUnwindCodes.Add(new CompactUnwindCode(
                PcStartSymbolName: startSymbolName,
                PcLength: (uint)length,
                Code: encoding,
                LsdaSymbolName: encoding != _compactUnwindDwarfCode ? lsdaSymbolName : null
            ));

            return encoding == _compactUnwindDwarfCode;
        }

        protected override ulong GetSectionVirtualAddress(int sectionIndex)
        {
            var machSection = _segment.Sections[sectionIndex];
            return machSection.VirtualAddress;
        }

        protected override void EmitDebugSections(DwarfFile dwarfFile)
        {
            ulong highPC = 0;
            foreach (var machSection in _segment.Sections)
            {
                if (machSection.Attributes.HasFlag(MachSectionAttributes.SomeInstructions))
                {
                    highPC = Math.Max(highPC, machSection.VirtualAddress + machSection.Size);
                }
            }

            foreach (var unit in dwarfFile.InfoSection.Units)
            {
                var rootDIE = (DwarfDIECompileUnit)unit.Root;
                rootDIE.LowPC = 0u;
                rootDIE.HighPC = (int)highPC;
                dwarfFile.AddressRangeTable.AddressSize = unit.AddressSize;
                dwarfFile.AddressRangeTable.Unit = unit;
                dwarfFile.AddressRangeTable.Ranges.Add(new DwarfAddressRange(0, 0, highPC));
            }

            var debugInfoSection = new MachSection(_objectFile, "__DWARF", "__debug_info") { Attributes = MachSectionAttributes.Debug };
            var debugAbbrevSection = new MachSection(_objectFile, "__DWARF", "__debug_abbrev") { Attributes = MachSectionAttributes.Debug };
            var debugAddressRangeSection = new MachSection(_objectFile, "__DWARF", "__debug_aranges") { Attributes = MachSectionAttributes.Debug };
            var debugStringSection = new MachSection(_objectFile, "__DWARF", "__debug_str") { Attributes = MachSectionAttributes.Debug };
            var debugLineSection = new MachSection(_objectFile, "__DWARF", "__debug_line") { Attributes = MachSectionAttributes.Debug };
            var debugLocationSection = new MachSection(_objectFile, "__DWARF", "__debug_loc") { Attributes = MachSectionAttributes.Debug };

            var outputContext = new DwarfWriterContext
            {
                IsLittleEndian = _objectFile.IsLittleEndian,
                EnableRelocation = false,
                AddressSize = DwarfAddressSize.Bit64,
                DebugLineStream = debugLineSection.GetWriteStream(),
                DebugAbbrevStream = debugAbbrevSection.GetWriteStream(),
                DebugStringStream = debugStringSection.GetWriteStream(),
                DebugAddressRangeStream = debugAddressRangeSection.GetWriteStream(),
                DebugInfoStream = debugInfoSection.GetWriteStream(),
                DebugLocationStream = debugLocationSection.GetWriteStream(),
            };

            dwarfFile.Write(outputContext);

            _segment.Sections.Add(debugInfoSection);
            _segment.Sections.Add(debugAbbrevSection);
            _segment.Sections.Add(debugAddressRangeSection);
            _segment.Sections.Add(debugStringSection);
            _segment.Sections.Add(debugLineSection);
            _segment.Sections.Add(debugLocationSection);

            _objectFile.UpdateLayout();
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            using MachObjectWriter objectWriter = new MachObjectWriter(factory, options);
            objectWriter.EmitObject(objectFilePath, nodes, dumper, logger);
        }
    }
}
