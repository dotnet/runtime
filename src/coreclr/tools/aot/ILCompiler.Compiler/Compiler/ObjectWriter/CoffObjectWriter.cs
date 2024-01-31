// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.CoffObjectWriter.CoffRelocationType;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// COFF object file format writer for Windows targets.
    /// </summary>
    /// <remarks>
    /// The PE/COFF object format is described in the official specifciation at
    /// https://learn.microsoft.com/en-us/windows/win32/debug/pe-format. However,
    /// numerous extensions are missing in the specification. The most notable
    /// ones are listed below.
    ///
    /// Object files with more than 65279 sections use an extended big object
    /// file format that is recognized by the Microsoft linker. Many of the
    /// internal file structures are different. The code below denotes it by
    /// "BigObj" in parameters and variables.
    ///
    /// Section names longer than 8 bytes need to be written indirectly in the
    /// string table. The PE/COFF specification describes the /NNNNNNN syntax
    /// for referencing them. However, if the string table gets big enough the
    /// syntax no longer works. There's an undocumented //BBBBBB syntax where
    /// base64 offset is used instead.
    ///
    /// CodeView debugging format uses 16-bit section index relocations. Once
    /// the number of sections exceeds 2^16 the same file format is still used.
    /// The linker treats the CodeView relocations symbolically.
    /// </remarks>
    internal sealed class CoffObjectWriter : ObjectWriter
    {
        private sealed record SectionDefinition(CoffSectionHeader Header, Stream Stream, List<CoffRelocation> Relocations, string ComdatName, string SymbolName);

        private readonly Machine _machine;
        private readonly List<SectionDefinition> _sections = new();

        // Symbol table
        private readonly List<CoffSymbolRecord> _symbols = new();
        private readonly Dictionary<string, uint> _symbolNameToIndex = new(StringComparer.Ordinal);
        private readonly Dictionary<int, CoffSectionSymbol> _sectionNumberToComdatAuxRecord = new();
        private readonly HashSet<string> _referencedMethods = new();

        // Exception handling
        private SectionWriter _pdataSectionWriter;

        // Debugging
        private SectionWriter _debugTypesSectionWriter;
        private SectionWriter _debugSymbolSectionWriter;
        private CodeViewFileTableBuilder _debugFileTableBuilder;
        private CodeViewSymbolsBuilder _debugSymbolsBuilder;
        private CodeViewTypesBuilder _debugTypesBuilder;

        private static readonly ObjectNodeSection PDataSection = new ObjectNodeSection("pdata", SectionType.ReadOnly);
        private static readonly ObjectNodeSection GfidsSection = new ObjectNodeSection(".gfids$y", SectionType.ReadOnly);
        private static readonly ObjectNodeSection DebugTypesSection = new ObjectNodeSection(".debug$T", SectionType.ReadOnly);
        private static readonly ObjectNodeSection DebugSymbolSection = new ObjectNodeSection(".debug$S", SectionType.ReadOnly);

        public CoffObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _machine = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => Machine.I386,
                TargetArchitecture.X64 => Machine.Amd64,
                TargetArchitecture.ARM64 => Machine.Arm64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            var sectionHeader = new CoffSectionHeader
            {
                Name =
                    section == ObjectNodeSection.TLSSection ? ".tls$" :
                    section == ObjectNodeSection.HydrationTargetSection ? "hydrated" :
                    (section.Name.StartsWith(".") ? section.Name : "." + section.Name),
                SectionCharacteristics = section.Type switch
                {
                    SectionType.ReadOnly =>
                        SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData,
                    SectionType.Writeable =>
                        SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite |
                        SectionCharacteristics.ContainsInitializedData,
                    SectionType.Executable =>
                        SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute |
                        SectionCharacteristics.ContainsCode,
                    SectionType.Uninitialized =>
                        SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite |
                        SectionCharacteristics.ContainsUninitializedData,
                    _ => 0
                }
            };

            if (section == DebugTypesSection)
            {
                sectionHeader.SectionCharacteristics =
                    SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData |
                    SectionCharacteristics.MemDiscardable;
            }

            if (comdatName is not null)
            {
                sectionHeader.SectionCharacteristics |= SectionCharacteristics.LinkerComdat;

                // We find the defining section of the COMDAT symbol. That one is marked
                // as "ANY" selection type. All the other ones are marked as associated.
                bool isPrimary = Equals(comdatName, symbolName);
                uint sectionIndex = (uint)_sections.Count + 1u;
                uint definingSectionIndex = isPrimary ? sectionIndex : ((CoffSymbol)_symbols[(int)_symbolNameToIndex[comdatName]]).SectionIndex;

                var auxRecord = new CoffSectionSymbol
                {
                    // SizeOfRawData, NumberOfRelocations, NumberOfLineNumbers
                    // CheckSum will be filled later in EmitObjectFile

                    Number = definingSectionIndex,
                    Selection = isPrimary ?
                        CoffComdatSelect.IMAGE_COMDAT_SELECT_ANY :
                        CoffComdatSelect.IMAGE_COMDAT_SELECT_ASSOCIATIVE,
                };

                _sectionNumberToComdatAuxRecord[_sections.Count] = auxRecord;
                _symbols.Add(new CoffSymbol
                {
                    Name = sectionHeader.Name,
                    Value = 0,
                    SectionIndex = sectionIndex,
                    StorageClass = CoffSymbolClass.IMAGE_SYM_CLASS_STATIC,
                    NumberOfAuxiliaryRecords = 1,
                });
                _symbols.Add(auxRecord);

                if (symbolName is not null)
                {
                    _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                    _symbols.Add(new CoffSymbol
                    {
                        Name = symbolName,
                        Value = 0,
                        SectionIndex = sectionIndex,
                        StorageClass = isPrimary ? CoffSymbolClass.IMAGE_SYM_CLASS_EXTERNAL : CoffSymbolClass.IMAGE_SYM_CLASS_STATIC,
                    });
                }
            }

            _sections.Add(new SectionDefinition(sectionHeader, sectionStream, new List<CoffRelocation>(), comdatName, symbolName));
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            Debug.Assert(alignment > 0 && BitOperations.IsPow2((uint)alignment));
            int minimumAlignment = (BitOperations.Log2((uint)alignment) + 1) << 20;
            int currentAlignment = (int)(_sections[sectionIndex].Header.SectionCharacteristics & SectionCharacteristics.AlignMask);

            if (currentAlignment < minimumAlignment)
            {
                _sections[sectionIndex].Header.SectionCharacteristics =
                    (_sections[sectionIndex].Header.SectionCharacteristics & ~SectionCharacteristics.AlignMask) |
                    (SectionCharacteristics)minimumAlignment;
            }
        }

        protected internal override unsafe void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            long addend)
        {
            if (relocType is IMAGE_REL_BASED_RELPTR32)
            {
                addend += 4;
            }

            if (addend != 0)
            {
                fixed (byte *pData = data)
                {
                    long inlineValue = Relocation.ReadValue(relocType, (void*)pData);
                    Relocation.WriteValue(relocType, (void*)pData, inlineValue + addend);
                }
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, 0);
        }

        private protected override void EmitReferencedMethod(string symbolName)
        {
            _referencedMethods.Add(symbolName);
        }

        private protected override void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols)
        {
            foreach (var (symbolName, symbolDefinition) in definedSymbols)
            {
                if (_symbolNameToIndex.TryGetValue(symbolName, out uint symbolIndex))
                {
                    // Update value for COMDAT symbols
                    ((CoffSymbol)_symbols[(int)symbolIndex]).Value = (uint)symbolDefinition.Value;
                }
                else
                {
                    _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                    _symbols.Add(new CoffSymbol
                    {
                        Name = symbolName,
                        Value = (uint)symbolDefinition.Value,
                        SectionIndex = (uint)(1 + symbolDefinition.SectionIndex),
                        StorageClass = CoffSymbolClass.IMAGE_SYM_CLASS_EXTERNAL,
                    });
                }
            }

            foreach (var symbolName in undefinedSymbols)
            {
                _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                _symbols.Add(new CoffSymbol
                {
                    Name = symbolName,
                    StorageClass = CoffSymbolClass.IMAGE_SYM_CLASS_EXTERNAL,
                });
            }

            if (_options.HasFlag(ObjectWritingOptions.ControlFlowGuard))
            {
                // Create section with control flow guard symbols
                SectionWriter gfidsSectionWriter = GetOrCreateSection(GfidsSection);

                foreach (var symbolName in _referencedMethods)
                {
                    gfidsSectionWriter.WriteLittleEndian<uint>(_symbolNameToIndex[symbolName]);
                }

                // Emit the feat.00 symbol that controls various linker behaviors
                _symbols.Add(new CoffSymbol
                {
                    Name = "@feat.00",
                    StorageClass = CoffSymbolClass.IMAGE_SYM_CLASS_STATIC,
                    SectionIndex = uint.MaxValue, // IMAGE_SYM_ABSOLUTE
                    Value = 0x800, // cfGuardCF flags this object as control flow guard aware
                });
            }
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            CoffSectionHeader sectionHeader = _sections[sectionIndex].Header;
            List<CoffRelocation> coffRelocations = _sections[sectionIndex].Relocations;
            if (relocationList.Count > 0)
            {
                if (relocationList.Count <= ushort.MaxValue)
                {
                    sectionHeader.NumberOfRelocations = (ushort)relocationList.Count;
                }
                else
                {
                    // Write an overflow relocation with the real count of relocations
                    sectionHeader.NumberOfRelocations = ushort.MaxValue;
                    sectionHeader.SectionCharacteristics |= SectionCharacteristics.LinkerNRelocOvfl;
                    coffRelocations.Add(new CoffRelocation { VirtualAddress = (uint)(relocationList.Count + 1) });
                }

                switch (_machine)
                {
                    case Machine.I386:
                        foreach (var relocation in relocationList)
                        {
                            coffRelocations.Add(new CoffRelocation
                            {
                                VirtualAddress = (uint)relocation.Offset,
                                SymbolTableIndex = _symbolNameToIndex[relocation.SymbolName],
                                Type = relocation.Type switch
                                {
                                    IMAGE_REL_BASED_ABSOLUTE => IMAGE_REL_I386_DIR32NB,
                                    IMAGE_REL_BASED_ADDR32NB => IMAGE_REL_I386_DIR32NB,
                                    IMAGE_REL_BASED_HIGHLOW => IMAGE_REL_I386_DIR32,
                                    IMAGE_REL_BASED_REL32 => IMAGE_REL_I386_REL32,
                                    IMAGE_REL_BASED_RELPTR32 => IMAGE_REL_I386_REL32,
                                    IMAGE_REL_SECREL => IMAGE_REL_I386_SECREL,
                                    IMAGE_REL_SECTION => IMAGE_REL_I386_SECTION,
                                    _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                                },
                            });
                        }
                        break;

                    case Machine.Amd64:
                        foreach (var relocation in relocationList)
                        {
                            coffRelocations.Add(new CoffRelocation
                            {
                                VirtualAddress = (uint)relocation.Offset,
                                SymbolTableIndex = _symbolNameToIndex[relocation.SymbolName],
                                Type = relocation.Type switch
                                {
                                    IMAGE_REL_BASED_ABSOLUTE => IMAGE_REL_AMD64_ADDR32NB,
                                    IMAGE_REL_BASED_ADDR32NB => IMAGE_REL_AMD64_ADDR32NB,
                                    IMAGE_REL_BASED_HIGHLOW => IMAGE_REL_AMD64_ADDR32,
                                    IMAGE_REL_BASED_DIR64 => IMAGE_REL_AMD64_ADDR64,
                                    IMAGE_REL_BASED_REL32 => IMAGE_REL_AMD64_REL32,
                                    IMAGE_REL_BASED_RELPTR32 => IMAGE_REL_AMD64_REL32,
                                    IMAGE_REL_SECREL => IMAGE_REL_AMD64_SECREL,
                                    IMAGE_REL_SECTION => IMAGE_REL_AMD64_SECTION,
                                    _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                                },
                            });
                        }
                        break;

                    case Machine.Arm64:
                        foreach (var relocation in relocationList)
                        {
                            coffRelocations.Add(new CoffRelocation
                            {
                                VirtualAddress = (uint)relocation.Offset,
                                SymbolTableIndex = _symbolNameToIndex[relocation.SymbolName],
                                Type = relocation.Type switch
                                {
                                    IMAGE_REL_BASED_ABSOLUTE => IMAGE_REL_ARM64_ADDR32NB,
                                    IMAGE_REL_BASED_ADDR32NB => IMAGE_REL_ARM64_ADDR32NB,
                                    IMAGE_REL_BASED_HIGHLOW => IMAGE_REL_ARM64_ADDR32,
                                    IMAGE_REL_BASED_DIR64 => IMAGE_REL_ARM64_ADDR64,
                                    IMAGE_REL_BASED_REL32 => IMAGE_REL_ARM64_REL32,
                                    IMAGE_REL_BASED_RELPTR32 => IMAGE_REL_ARM64_REL32,
                                    IMAGE_REL_BASED_ARM64_BRANCH26 => IMAGE_REL_ARM64_BRANCH26,
                                    IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => IMAGE_REL_ARM64_PAGEBASE_REL21,
                                    IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => IMAGE_REL_ARM64_PAGEOFFSET_12A,
                                    IMAGE_REL_SECREL => IMAGE_REL_ARM64_SECREL,
                                    IMAGE_REL_SECTION => IMAGE_REL_ARM64_SECTION,
                                    _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                                },
                            });
                        }
                        break;

                    default:
                        throw new NotSupportedException("Unsupported architecture");
                }
            }
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                SectionWriter xdataSectionWriter;
                SectionWriter pdataSectionWriter;
                bool shareSymbol = ShouldShareSymbol((ObjectNode)nodeWithCodeInfo);

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string unwindSymbolName = $"_unwind{i}{currentSymbolName}";

                    if (shareSymbol)
                    {
                        // Produce an associative COMDAT symbol.
                        xdataSectionWriter = GetOrCreateSection(ObjectNodeSection.XDataSection, currentSymbolName, unwindSymbolName);
                        pdataSectionWriter = GetOrCreateSection(PDataSection, currentSymbolName, null);
                    }
                    else
                    {
                        // Produce a COMDAT section for each unwind symbol and let linker
                        // do the deduplication across the ones with identical content.
                        xdataSectionWriter = GetOrCreateSection(ObjectNodeSection.XDataSection, unwindSymbolName, unwindSymbolName);
                        pdataSectionWriter = _pdataSectionWriter;
                    }

                    // Need to emit the UNWIND_INFO at 4-byte alignment to ensure that the
                    // pointer has the lower two bits in .pdata section set to zero. On ARM64
                    // non-zero bits would mean a compact encoding.
                    xdataSectionWriter.EmitAlignment(4);

                    xdataSectionWriter.EmitSymbolDefinition(unwindSymbolName);

                    // Emit UNWIND_INFO
                    xdataSectionWriter.Write(blob);

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        xdataSectionWriter.WriteByte((byte)flags);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo is not null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode is not null ? FrameInfoFlags.HasAssociatedData : 0;

                        xdataSectionWriter.WriteByte((byte)flags);

                        if (associatedDataNode is not null)
                        {
                            xdataSectionWriter.EmitSymbolReference(
                                IMAGE_REL_BASED_ADDR32NB,
                                GetMangledName(associatedDataNode));
                        }

                        if (ehInfo is not null)
                        {
                            xdataSectionWriter.EmitSymbolReference(
                                IMAGE_REL_BASED_ADDR32NB,
                                GetMangledName(ehInfo));
                        }

                        if (nodeWithCodeInfo.GCInfo is not null)
                        {
                            xdataSectionWriter.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    // Emit RUNTIME_FUNCTION
                    pdataSectionWriter.EmitAlignment(4);
                    pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, currentSymbolName, start);
                    // Only x64 has the End symbol
                    if (_machine == Machine.Amd64)
                    {
                        pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, currentSymbolName, end);
                    }
                    // Unwind info pointer
                    pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, unwindSymbolName, 0);
                }
            }
        }

        private protected override void EmitObjectFile(string objectFilePath)
        {
            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            var stringTable = new CoffStringTable();
            var coffHeader = new CoffHeader
            {
                Machine = _machine,
                NumberOfSections = (uint)_sections.Count,
                NumberOfSymbols = (uint)_symbols.Count,
            };

            // Calculate size of section data and assign offsets
            uint dataOffset = (uint)(coffHeader.Size + _sections.Count * CoffSectionHeader.Size);
            int sectionIndex = 0;
            foreach (SectionDefinition section in _sections)
            {
                section.Header.SizeOfRawData = (uint)section.Stream.Length;

                // Section content
                if (section.Header.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    section.Header.PointerToRawData = 0;
                }
                else
                {
                    section.Header.PointerToRawData = dataOffset;
                    dataOffset += section.Header.SizeOfRawData;
                }

                // Section relocations
                section.Header.PointerToRelocations = section.Relocations.Count > 0 ? dataOffset : 0;
                dataOffset += (uint)(section.Relocations.Count * CoffRelocation.Size);

                sectionIndex++;
            }

            coffHeader.PointerToSymbolTable = dataOffset;

            // Write COFF header
            coffHeader.Write(outputFileStream);

            // Write COFF section headers
            sectionIndex = 0;
            foreach (SectionDefinition section in _sections)
            {
                section.Header.Write(outputFileStream, stringTable);

                // Relocation code below assumes that addresses are 0-indexed
                Debug.Assert(section.Header.VirtualAddress == 0);

                // Update COMDAT section symbol
                if (_sectionNumberToComdatAuxRecord.TryGetValue(sectionIndex, out var auxRecord))
                {
                    auxRecord.SizeOfRawData = section.Header.SizeOfRawData;
                    auxRecord.NumberOfRelocations = section.Header.NumberOfRelocations;
                    auxRecord.NumberOfLineNumbers = section.Header.NumberOfLineNumbers;

                    section.Stream.Position = 0;
                    auxRecord.CheckSum = JamCrc32.CalculateChecksum(section.Stream);
                }

                sectionIndex++;
            }

            // Writer section content and relocations
            foreach (SectionDefinition section in _sections)
            {
                if (!section.Header.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    Debug.Assert(outputFileStream.Position == section.Header.PointerToRawData);
                    section.Stream.Position = 0;
                    section.Stream.CopyTo(outputFileStream);
                }

                if (section.Relocations.Count > 0)
                {
                    foreach (var relocation in section.Relocations)
                    {
                        relocation.Write(outputFileStream);
                    }
                }
            }

            // Optimize the string table
            foreach (var coffSymbolRecord in _symbols)
            {
                if (coffSymbolRecord is CoffSymbol coffSymbol)
                {
                    stringTable.ReserveString(coffSymbol.Name);
                }
            }

            // Write symbol table
            Debug.Assert(outputFileStream.Position == coffHeader.PointerToSymbolTable);
            foreach (var coffSymbolRecord in _symbols)
            {
                coffSymbolRecord.Write(outputFileStream, stringTable, coffHeader.IsBigObj);
            }

            // Write string table
            stringTable.Write(outputFileStream);
        }

        private protected override void CreateEhSections()
        {
            // Create .pdata
            _pdataSectionWriter = GetOrCreateSection(PDataSection);
        }

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            _debugFileTableBuilder = new CodeViewFileTableBuilder();

            _debugSymbolSectionWriter = GetOrCreateSection(DebugSymbolSection);
            _debugSymbolSectionWriter.EmitAlignment(4);
            _debugSymbolsBuilder = new CodeViewSymbolsBuilder(
                _nodeFactory.Target.Architecture,
                _debugSymbolSectionWriter);

            _debugTypesSectionWriter = GetOrCreateSection(DebugTypesSection);
            _debugTypesSectionWriter.EmitAlignment(4);
            _debugTypesBuilder = new CodeViewTypesBuilder(
                _nodeFactory.NameMangler, _nodeFactory.Target.PointerSize,
                _debugTypesSectionWriter);
            return _debugTypesBuilder;
        }

        private protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints)
        {
            DebugEHClauseInfo[] clauses = null;
            CodeViewSymbolsBuilder debugSymbolsBuilder;

            if (debugNode is INodeWithCodeInfo nodeWithCodeInfo)
            {
                clauses = nodeWithCodeInfo.DebugEHClauseInfos;
            }

            if (ShouldShareSymbol((ObjectNode)debugNode))
            {
                // If the method is emitted in COMDAT section then we need to create an
                // associated COMDAT section for the debugging symbols.
                var sectionWriter = GetOrCreateSection(DebugSymbolSection, methodName, null);
                debugSymbolsBuilder = new CodeViewSymbolsBuilder(_nodeFactory.Target.Architecture, sectionWriter);
            }
            else
            {
                debugSymbolsBuilder = _debugSymbolsBuilder;
            }

            debugSymbolsBuilder.EmitSubprogramInfo(
                methodName,
                methodSymbol.Size,
                methodTypeIndex,
                debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                clauses ?? Array.Empty<DebugEHClauseInfo>());

            if (hasSequencePoints)
            {
                debugSymbolsBuilder.EmitLineInfo(
                    _debugFileTableBuilder,
                    methodName,
                    methodSymbol.Size,
                    debugNode.GetNativeSequencePoints());
            }
        }

        private protected override void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            _debugSymbolsBuilder.WriteUserDefinedTypes(_debugTypesBuilder.UserDefinedTypes);
            _debugFileTableBuilder.Write(_debugSymbolSectionWriter);
        }

        private struct CoffHeader
        {
            public Machine Machine { get; set; }
            public uint NumberOfSections { get; set; }
            public uint TimeDateStamp { get; set; }
            public uint PointerToSymbolTable { get; set; }
            public uint NumberOfSymbols { get; set; }
            public ushort SizeOfOptionalHeader { get; set; }
            public ushort Characteristics { get; set; }

            // Maximum number of section that can be handled Microsoft linker
            // before it bails out. We automatically switch to big object file
            // layout after that.
            public bool IsBigObj => NumberOfSections > 65279;

            private static ReadOnlySpan<byte> BigObjMagic => new byte[]
            {
                0xC7, 0xA1, 0xBA, 0xD1, 0xEE, 0xBA, 0xA9, 0x4B,
                0xAF, 0x20, 0xFA, 0xF6, 0x6A, 0xA4, 0xDC, 0xB8,
            };

            private const int RegularSize =
                sizeof(ushort) + // Machine
                sizeof(ushort) + // NumberOfSections
                sizeof(uint) +   // TimeDateStamp
                sizeof(uint) +   // PointerToSymbolTable
                sizeof(uint) +   // NumberOfSymbols
                sizeof(ushort) + // SizeOfOptionalHeader
                sizeof(ushort);  // Characteristics

            private const int BigObjSize =
                sizeof(ushort) + // Signature 1 (Machine = Unknown)
                sizeof(ushort) + // Signature 2 (NumberOfSections = 0xFFFF)
                sizeof(ushort) + // Version (2)
                sizeof(ushort) + // Machine
                sizeof(uint) +   // TimeDateStamp
                16 +             // BigObjMagic
                sizeof(uint) +   // Reserved1
                sizeof(uint) +   // Reserved2
                sizeof(uint) +   // Reserved3
                sizeof(uint) +   // Reserved4
                sizeof(uint) +   // NumberOfSections
                sizeof(uint) +   // PointerToSymbolTable
                sizeof(uint);    // NumberOfSymbols

            public int Size => IsBigObj ? BigObjSize : RegularSize;

            public void Write(FileStream stream)
            {
                if (!IsBigObj)
                {
                    Span<byte> buffer = stackalloc byte[RegularSize];

                    BinaryPrimitives.WriteInt16LittleEndian(buffer, (short)Machine);
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), (ushort)NumberOfSections);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), TimeDateStamp);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), PointerToSymbolTable);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12), NumberOfSymbols);
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(16), SizeOfOptionalHeader);
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(18), Characteristics);

                    stream.Write(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[BigObjSize];

                    buffer.Clear();
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), 0xFFFF);
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4), 2);
                    BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(6), (short)Machine);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), TimeDateStamp);
                    BigObjMagic.CopyTo(buffer.Slice(12));
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(44), NumberOfSections);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(48), PointerToSymbolTable);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(52), NumberOfSymbols);

                    Debug.Assert(SizeOfOptionalHeader == 0);
                    Debug.Assert(Characteristics == 0);

                    stream.Write(buffer);
                }
            }
        }

        private sealed class CoffSectionHeader
        {
            public string Name { get; set; }
            public uint VirtualSize { get; set; }
            public uint VirtualAddress { get; set; }
            public uint SizeOfRawData { get; set; }
            public uint PointerToRawData { get; set; }
            public uint PointerToRelocations { get; set; }
            public uint PointerToLineNumbers { get; set; }
            public ushort NumberOfRelocations { get; set; }
            public ushort NumberOfLineNumbers { get; set; }
            public SectionCharacteristics SectionCharacteristics { get; set; }

            private const int NameSize = 8;

            public const int Size =
                NameSize +       // Name size
                sizeof(uint) +   // VirtualSize
                sizeof(uint) +   // VirtualAddress
                sizeof(uint) +   // SizeOfRawData
                sizeof(uint) +   // PointerToRawData
                sizeof(uint) +   // PointerToRelocations
                sizeof(uint) +   // PointerToLineNumbers
                sizeof(ushort) + // NumberOfRelocations
                sizeof(ushort) + // NumberOfLineNumbers
                sizeof(uint);    // SectionCharacteristics

            public void Write(FileStream stream, CoffStringTable stringTable)
            {
                Span<byte> buffer = stackalloc byte[Size];

                var nameBytes = Encoding.UTF8.GetByteCount(Name);
                if (nameBytes <= NameSize)
                {
                    Encoding.UTF8.GetBytes(Name, buffer);
                    if (nameBytes < NameSize)
                    {
                        buffer.Slice(nameBytes, 8 - nameBytes).Clear();
                    }
                }
                else
                {
                    buffer.Clear();
                    buffer[0] = (byte)'/';
                    uint offset = stringTable.GetStringOffset(Name);
                    if (offset <= 9999999)
                    {
                        Span<char> charBuffer = stackalloc char[16];
                        int charsWritten;
                        offset.TryFormat(charBuffer, out charsWritten);
                        for (int i = 0; i < charsWritten; i++)
                        {
                            buffer[1 + i] = (byte)charBuffer[i];
                        }
                    }
                    else
                    {
                        ReadOnlySpan<byte> s_base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;
                        // Maximum expressible offset is 64^6 which is less than uint.MaxValue
                        buffer[1] = (byte)'/';
                        for (int i = 0; i < 6; i++)
                        {
                            buffer[7 - i] = s_base64Alphabet[(int)(offset % 64)];
                            offset /= 64;
                        }
                    }
                }

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize), VirtualSize);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 4), VirtualAddress);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 8), SizeOfRawData);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 12), PointerToRawData);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 16), PointerToRelocations);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 20), PointerToLineNumbers);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(NameSize + 24), NumberOfRelocations);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(NameSize + 26), NumberOfLineNumbers);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 28), (uint)SectionCharacteristics);

                stream.Write(buffer);
            }
        }

        internal enum CoffRelocationType
        {
            IMAGE_REL_I386_ABSOLUTE = 0,
            IMAGE_REL_I386_DIR32 = 6,
            IMAGE_REL_I386_DIR32NB = 7,
            IMAGE_REL_I386_SECTION = 10,
            IMAGE_REL_I386_SECREL = 11,
            IMAGE_REL_I386_REL32 = 20,

            IMAGE_REL_AMD64_ABSOLUTE = 0,
            IMAGE_REL_AMD64_ADDR64 = 1,
            IMAGE_REL_AMD64_ADDR32 = 2,
            IMAGE_REL_AMD64_ADDR32NB = 3,
            IMAGE_REL_AMD64_REL32 = 4,
            IMAGE_REL_AMD64_REL32_1 = 5,
            IMAGE_REL_AMD64_REL32_2 = 6,
            IMAGE_REL_AMD64_REL32_3 = 7,
            IMAGE_REL_AMD64_REL32_4 = 8,
            IMAGE_REL_AMD64_REL32_5 = 9,
            IMAGE_REL_AMD64_SECTION = 10,
            IMAGE_REL_AMD64_SECREL = 11,
            IMAGE_REL_AMD64_SECREL7 = 12,
            IMAGE_REL_AMD64_TOKEN = 13,
            IMAGE_REL_AMD64_SREL32 = 14,
            IMAGE_REL_AMD64_PAIR = 15,
            IMAGE_REL_AMD64_SSPAN32 = 16,

            IMAGE_REL_ARM64_ABSOLUTE = 0,
            IMAGE_REL_ARM64_ADDR32 = 1,
            IMAGE_REL_ARM64_ADDR32NB = 2,
            IMAGE_REL_ARM64_BRANCH26 = 3,
            IMAGE_REL_ARM64_PAGEBASE_REL21 = 4,
            IMAGE_REL_ARM64_REL21 = 5,
            IMAGE_REL_ARM64_PAGEOFFSET_12A = 6,
            IMAGE_REL_ARM64_PAGEOFFSET_12L = 7,
            IMAGE_REL_ARM64_SECREL = 8,
            IMAGE_REL_ARM64_SECREL_LOW12A = 9,
            IMAGE_REL_ARM64_SECREL_HIGH12A = 10,
            IMAGE_REL_ARM64_SECREL_LOW12L = 11,
            IMAGE_REL_ARM64_TOKEN = 12,
            IMAGE_REL_ARM64_SECTION = 13,
            IMAGE_REL_ARM64_ADDR64 = 14,
            IMAGE_REL_ARM64_BRANCH19 = 15,
            IMAGE_REL_ARM64_BRANCH14 = 16,
            IMAGE_REL_ARM64_REL32 = 17,
        }

        private sealed class CoffRelocation
        {
            public uint VirtualAddress { get; set; }
            public uint SymbolTableIndex { get; set; }
            public CoffRelocationType Type { get; set; }

            public const int Size =
                sizeof(uint) +  // VirtualAddress
                sizeof(uint) +  // SymbolTableIndex
                sizeof(ushort); // Type

            public void Write(FileStream stream)
            {
                Span<byte> buffer = stackalloc byte[Size];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer, VirtualAddress);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), SymbolTableIndex);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(8), (ushort)Type);

                stream.Write(buffer);
            }
        }

        private abstract class CoffSymbolRecord
        {
            public abstract void Write(Stream stream, CoffStringTable stringTable, bool isBigObj);
        }

        private enum CoffSymbolClass : byte
        {
            IMAGE_SYM_CLASS_EXTERNAL = 2,
            IMAGE_SYM_CLASS_STATIC = 3,
            IMAGE_SYM_CLASS_LABEL = 6,
        }

        private sealed class CoffSymbol : CoffSymbolRecord
        {
            public string Name { get; set; }
            public uint Value { get; set; }
            public uint SectionIndex { get; set; }
            public ushort Type { get; set; }
            public CoffSymbolClass StorageClass { get; set; }
            public byte NumberOfAuxiliaryRecords { get; set; }

            private const int NameSize = 8;

            private const int RegularSize =
                NameSize +       // Name size
                sizeof(uint) +   // Value
                sizeof(ushort) + // Section index
                sizeof(ushort) + // Type
                sizeof(byte) +   // Storage class
                sizeof(byte);    // Auxiliary symbol count

            private const int BigObjSize =
                NameSize +       // Name size
                sizeof(uint) +   // Value
                sizeof(uint) +   // Section index
                sizeof(ushort) + // Type
                sizeof(byte) +   // Storage class
                sizeof(byte);    // Auxiliary symbol count

            public override void Write(Stream stream, CoffStringTable stringTable, bool isBigObj)
            {
                Span<byte> buffer = stackalloc byte[isBigObj ? BigObjSize : RegularSize];

                int nameBytes = Encoding.UTF8.GetByteCount(Name);
                if (nameBytes <= NameSize)
                {
                    Encoding.UTF8.GetBytes(Name, buffer);
                    if (nameBytes < NameSize)
                    {
                        buffer.Slice(nameBytes, 8 - nameBytes).Clear();
                    }
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer, 0);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), stringTable.GetStringOffset(Name));
                }

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize), Value);
                int sliceIndex;
                if (isBigObj)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(NameSize + 4), SectionIndex);
                    sliceIndex = NameSize + 8;
                }
                else
                {
                    Debug.Assert(SectionIndex == uint.MaxValue || SectionIndex < ushort.MaxValue);
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(NameSize + 4), (ushort)SectionIndex);
                    sliceIndex = NameSize + 6;
                }
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(sliceIndex), Type);
                buffer[sliceIndex + 2] = (byte)StorageClass;
                buffer[sliceIndex + 3] = NumberOfAuxiliaryRecords;

                stream.Write(buffer);
            }
        }

        private enum CoffComdatSelect
        {
            IMAGE_COMDAT_SELECT_NODUPLICATES = 1,
            IMAGE_COMDAT_SELECT_ANY = 2,
            IMAGE_COMDAT_SELECT_SAME_SIZE = 3,
            IMAGE_COMDAT_SELECT_EXACT_MATCH = 4,
            IMAGE_COMDAT_SELECT_ASSOCIATIVE = 5,
            IMAGE_COMDAT_SELECT_LARGEST = 6,
        }

        private sealed class CoffSectionSymbol : CoffSymbolRecord
        {
            public uint SizeOfRawData { get; set; }
            public ushort NumberOfRelocations { get; set; }
            public ushort NumberOfLineNumbers { get; set; }
            public uint CheckSum { get; set; }
            public uint Number { get; set; }
            public CoffComdatSelect Selection { get; set; }

            private const int RegularSize =
                sizeof(uint) +   // SizeOfRawData
                sizeof(ushort) + // NumberOfRelocations
                sizeof(ushort) + // NumberOfLineNumbers
                sizeof(uint) +   // CheckSum
                sizeof(ushort) + // Number
                sizeof(byte) +   // Selection
                3;               // Reserved

            private const int BigObjSize = RegularSize + 2;

            public override void Write(Stream stream, CoffStringTable stringTable, bool isBigObj)
            {
                Span<byte> buffer = stackalloc byte[isBigObj ? BigObjSize : RegularSize];

                buffer.Clear();
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, SizeOfRawData);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(4), NumberOfRelocations);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(6), NumberOfLineNumbers);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), CheckSum);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(12), (ushort)Number);
                buffer[14] = (byte)Selection;
                if (isBigObj)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(16), (ushort)(Number >> 16));
                }

                stream.Write(buffer);
            }
        }

        private sealed class CoffStringTable : StringTableBuilder
        {
            public new uint Size => (uint)(base.Size + 4);

            public new uint GetStringOffset(string text)
            {
                return base.GetStringOffset(text) + 4;
            }

            public new void Write(FileStream stream)
            {
                Span<byte> stringTableSize = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(stringTableSize, Size);
                stream.Write(stringTableSize);
                base.Write(stream);
            }
        }

        /// <summary>
        /// Checksum algorithm used for COMDAT sections. This is similar to standard
        /// CRC32 but starts with 0 as initial value instead of ~0.
        /// </summary>
        private static class JamCrc32
        {
            public static uint CalculateChecksum(Stream stream)
            {
                // NOTE:
                // This can be generated by Crc32ReflectedTable.Generate(0xEDB88320u);
                // We embed the pre-generated version since it's small.
                ReadOnlySpan<uint> table =
                [
                    0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F,
                    0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988,
                    0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 0x1DB71064, 0x6AB020F2,
                    0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7,
                    0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
                    0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172,
                    0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C,
                    0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
                    0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423,
                    0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
                    0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190, 0x01DB7106,
                    0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
                    0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D,
                    0x91646C97, 0xE6635C01, 0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E,
                    0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
                    0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
                    0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7,
                    0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0,
                    0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA,
                    0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
                    0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81,
                    0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A,
                    0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 0xE3630B12, 0x94643B84,
                    0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
                    0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
                    0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC,
                    0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 0xD6D6A3E8, 0xA1D1937E,
                    0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B,
                    0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55,
                    0x316E8EEF, 0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
                    0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28,
                    0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
                    0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F,
                    0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38,
                    0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
                    0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
                    0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69,
                    0x616BFFD3, 0x166CCF45, 0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2,
                    0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC,
                    0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
                    0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693,
                    0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94,
                    0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
                ];

                uint crc = 0;
                Span<byte> buffer = stackalloc byte[4096];
                while (stream.Position < stream.Length)
                {
                    int length = stream.Read(buffer);
                    for (int i = 0; i < length; i++)
                    {
                        crc = table[(byte)(crc ^ buffer[i])] ^ (crc >> 8);
                    }
                }
                return crc;
            }
        }
    }
}
