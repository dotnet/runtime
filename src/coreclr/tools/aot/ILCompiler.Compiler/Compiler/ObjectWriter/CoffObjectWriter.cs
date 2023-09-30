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

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    public sealed class CoffObjectWriter : ObjectWriter
    {
        private Machine _machine;
        private List<(CoffSectionHeader Header, Stream Stream, List<CoffRelocation> Relocations, string ComdatName)> _sections = new();

        // Symbol table
        private List<CoffSymbolRecord> _symbols = new();
        private Dictionary<string, uint> _symbolNameToIndex = new();
        private Dictionary<int, CoffSectionSymbol> _sectionNumberToComdatAuxRecord = new();
        private HashSet<string> _referencedMethods = new();

        // Exception handling
        private SectionWriter _xdataSectionWriter;
        private SectionWriter _pdataSectionWriter;

        // Debugging
        private SectionWriter _debugTypesSectionWriter;
        private SectionWriter _debugSymbolSectionWriter;
        private CodeViewFileTableBuilder _debugFileTableBuilder;
        private CodeViewSymbolsBuilder _debugSymbolsBuilder;
        private CodeViewTypesBuilder _debugTypesBuilder;

        private ObjectNodeSection PDataSection = new ObjectNodeSection("pdata", SectionType.ReadOnly);
        private ObjectNodeSection GfidsSection = new ObjectNodeSection(".gfids$y", SectionType.ReadOnly);
        private ObjectNodeSection DebugTypesSection = new ObjectNodeSection(".debug$T", SectionType.ReadOnly);
        private ObjectNodeSection DebugSymbolSection = new ObjectNodeSection(".debug$S", SectionType.ReadOnly);

        private CoffObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _machine = factory.Target.Architecture switch
            {
                TargetArchitecture.X64 => Machine.Amd64,
                TargetArchitecture.ARM64 => Machine.Arm64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };
        }

        protected override void CreateSection(ObjectNodeSection section, Stream sectionStream)
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

            if (section.ComdatName != null)
            {
                sectionHeader.SectionCharacteristics |=
                    SectionCharacteristics.LinkerComdat;
            }

            _sections.Add((sectionHeader, sectionStream, new List<CoffRelocation>(), section.ComdatName));
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

        protected internal override void EmitRelocation(
            int sectionIndex,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            switch (relocType)
            {
                case RelocType.IMAGE_REL_BASED_ARM64_BRANCH26:
                case RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21:
                case RelocType.IMAGE_REL_SECREL:
                case RelocType.IMAGE_REL_SECTION:
                    Debug.Assert(addend == 0);
                    break;

                case RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A:
                    if (addend != 0)
                    {
                        uint addInstr = BinaryPrimitives.ReadUInt32LittleEndian(data);
                        addInstr &= 0xFFC003FF; // keep bits 31-22, 9-0
                        addInstr |= (uint)(addend << 10); // Occupy 21-10.
                        BinaryPrimitives.WriteUInt32LittleEndian(data, addInstr); // write the assembled instruction
                    }
                    break;

                case RelocType.IMAGE_REL_BASED_DIR64:
                    if (addend != 0)
                    {
                        BinaryPrimitives.WriteInt64LittleEndian(
                            data,
                            BinaryPrimitives.ReadInt64LittleEndian(data) +
                            addend);
                        addend = 0;
                    }
                    break;

                case RelocType.IMAGE_REL_BASED_RELPTR32:
                    addend += 4;
                    if (addend != 0)
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(
                            data,
                            BinaryPrimitives.ReadInt32LittleEndian(data) +
                            addend);
                        addend = 0;
                    }
                    break;

                case RelocType.IMAGE_REL_BASED_REL32:
                case RelocType.IMAGE_REL_BASED_ADDR32NB:
                case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                    if (addend != 0)
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(
                            data,
                            BinaryPrimitives.ReadInt32LittleEndian(data) +
                            addend);
                        addend = 0;
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported relocation: {relocType}");
            }

            base.EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
        }

        protected override void EmitReferencedMethod(string symbolName)
        {
            _referencedMethods.Add(symbolName);
        }

        protected override void EmitSymbolTable()
        {
            var definedSymbols = GetDefinedSymbols();

            int sectionIndex = 0;
            foreach (var (sectionHeader, _, _, comdatName) in _sections)
            {
                if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.LinkerComdat))
                {
                    // We find the defining section of the COMDAT symbol. That one is marked
                    // as "ANY" selection type. All the other ones are marked as associated.
                    SymbolDefinition definingSymbol = definedSymbols[comdatName];
                    int definingSectionIndex = definingSymbol.SectionIndex;

                    var auxRecord = new CoffSectionSymbol
                    {
                        // SizeOfRawData, NumberOfRelocations, NumberOfLineNumbers
                        // CheckSum will be filled later in EmitObjectFile

                        Number = (uint)(1 + definingSectionIndex),
                        Selection = definingSectionIndex == sectionIndex ?
                            CoffComdatSelect.IMAGE_COMDAT_SELECT_ANY :
                            CoffComdatSelect.IMAGE_COMDAT_SELECT_ASSOCIATIVE,
                    };

                    _sectionNumberToComdatAuxRecord[sectionIndex] = auxRecord;
                    _symbols.Add(new CoffSymbol
                    {
                        Name = sectionHeader.Name,
                        Value = 0,
                        SectionIndex = (uint)(1 + sectionIndex),
                        StorageClass = 3, // IMAGE_SYM_CLASS_STATIC
                        NumberOfAuxiliaryRecords = 1
                    });
                    _symbols.Add(auxRecord);

                    if (definingSectionIndex == sectionIndex)
                    {
                        _symbolNameToIndex.Add(comdatName, (uint)_symbols.Count);
                        _symbols.Add(new CoffSymbol
                        {
                            Name = comdatName,
                            Value = (uint)definingSymbol.Value,
                            SectionIndex = (uint)(1 + definingSymbol.SectionIndex),
                            StorageClass = 2 // IMAGE_SYM_CLASS_EXTERNAL
                        });
                    }
                }
                sectionIndex++;
            }

            foreach (var (symbolName, symbolDefinition) in definedSymbols)
            {
                if (!_symbolNameToIndex.ContainsKey(symbolName))
                {
                    _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                    _symbols.Add(new CoffSymbol
                    {
                        Name = symbolName,
                        Value = (uint)symbolDefinition.Value,
                        SectionIndex = (uint)(1 + symbolDefinition.SectionIndex),
                        StorageClass = 2 // IMAGE_SYM_CLASS_EXTERNAL
                    });
                }
            }

            foreach (var symbolName in GetUndefinedSymbols())
            {
                _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                _symbols.Add(new CoffSymbol
                {
                    Name = symbolName,
                    StorageClass = 2 // IMAGE_SYM_CLASS_EXTERNAL
                });
            }

            if (_options.HasFlag(ObjectWritingOptions.ControlFlowGuard))
            {
                // Create section with control flow guard symbols
                SectionWriter gfidsSectionWriter = GetOrCreateSection(GfidsSection);

                Span<byte> tempBuffer = stackalloc byte[4];
                foreach (var symbolName in _referencedMethods)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, _symbolNameToIndex[symbolName]);
                    gfidsSectionWriter.Stream.Write(tempBuffer);
                }

                // Emit the feat.00 symbol that controls various linker behaviors
                _symbols.Add(new CoffSymbol
                {
                    Name = "@feat.00",
                    StorageClass = 3, // IMAGE_SYM_CLASS_STATIC
                    SectionIndex = uint.MaxValue, // IMAGE_SYM_ABSOLUTE
                    Value = 0x800, // cfGuardCF flags this object as control flow guard aware
                });
            }
        }

        protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
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

                if (_machine == Machine.Amd64)
                {
                    foreach (var relocation in relocationList)
                    {
                        coffRelocations.Add(new CoffRelocation
                        {
                            VirtualAddress = (uint)relocation.Offset,
                            SymbolTableIndex = _symbolNameToIndex[relocation.SymbolName],
                            Type = relocation.Type switch
                            {
                                RelocType.IMAGE_REL_BASED_ABSOLUTE => CoffRelocationType.IMAGE_REL_AMD64_ADDR32NB,
                                RelocType.IMAGE_REL_BASED_ADDR32NB => CoffRelocationType.IMAGE_REL_AMD64_ADDR32NB,
                                RelocType.IMAGE_REL_BASED_HIGHLOW => CoffRelocationType.IMAGE_REL_AMD64_ADDR32,
                                RelocType.IMAGE_REL_BASED_DIR64 => CoffRelocationType.IMAGE_REL_AMD64_ADDR64,
                                RelocType.IMAGE_REL_BASED_REL32 => CoffRelocationType.IMAGE_REL_AMD64_REL32,
                                RelocType.IMAGE_REL_BASED_RELPTR32 => CoffRelocationType.IMAGE_REL_AMD64_REL32,
                                RelocType.IMAGE_REL_SECREL => CoffRelocationType.IMAGE_REL_AMD64_SECREL,
                                RelocType.IMAGE_REL_SECTION => CoffRelocationType.IMAGE_REL_AMD64_SECTION,
                                _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                            },
                        });
                    }
                }
                else if (_machine == Machine.Arm64)
                {
                    foreach (var relocation in relocationList)
                    {
                        coffRelocations.Add(new CoffRelocation
                        {
                            VirtualAddress = (uint)relocation.Offset,
                            SymbolTableIndex = _symbolNameToIndex[relocation.SymbolName],
                            Type = relocation.Type switch
                            {
                                RelocType.IMAGE_REL_BASED_ABSOLUTE => CoffRelocationType.IMAGE_REL_ARM64_ADDR32NB,
                                RelocType.IMAGE_REL_BASED_ADDR32NB => CoffRelocationType.IMAGE_REL_ARM64_ADDR32NB,
                                RelocType.IMAGE_REL_BASED_HIGHLOW => CoffRelocationType.IMAGE_REL_ARM64_ADDR32,
                                RelocType.IMAGE_REL_BASED_DIR64 => CoffRelocationType.IMAGE_REL_ARM64_ADDR64,
                                RelocType.IMAGE_REL_BASED_REL32 => CoffRelocationType.IMAGE_REL_ARM64_REL32,
                                RelocType.IMAGE_REL_BASED_RELPTR32 => CoffRelocationType.IMAGE_REL_ARM64_REL32,
                                RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 => CoffRelocationType.IMAGE_REL_ARM64_BRANCH26,
                                RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 => CoffRelocationType.IMAGE_REL_ARM64_PAGEBASE_REL21,
                                RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A => CoffRelocationType.IMAGE_REL_ARM64_PAGEOFFSET_12A,
                                RelocType.IMAGE_REL_SECREL => CoffRelocationType.IMAGE_REL_ARM64_SECREL,
                                RelocType.IMAGE_REL_SECTION => CoffRelocationType.IMAGE_REL_ARM64_SECTION,
                                _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                            },
                        });
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported architecture");
                }
            }
        }

        protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                SectionWriter xdataSectionWriter;
                SectionWriter pdataSectionWriter;
                Span<byte> tempBuffer = stackalloc byte[4];
                bool shareSymbol = ShouldShareSymbol((ObjectNode)nodeWithCodeInfo);

                pdataSectionWriter = shareSymbol ? GetOrCreateSection(GetSharedSection(PDataSection, currentSymbolName)) : _pdataSectionWriter;

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string unwindSymbolName = $"_unwind{i}{currentSymbolName}";

                    if (shareSymbol)
                    {
                        // Ideally we would use `currentSymbolName` here and produce an
                        // associative COMDAT symbol but link.exe cannot always handle that
                        // and produces errors about duplicate symbols that point into the
                        // associative section, so we are stuck with one section per each
                        // unwind symbol.
                        xdataSectionWriter = GetOrCreateSection(GetSharedSection(ObjectNodeSection.XDataSection, unwindSymbolName));
                    }
                    else
                    {
                        xdataSectionWriter = _xdataSectionWriter;
                    }

                    // Need to emit the UNWIND_INFO at 4-byte alignment to ensure that the
                    // pointer has the lower two bits in .pdata section set to zero. On ARM64
                    // non-zero bits would mean a compact encoding.
                    xdataSectionWriter.EmitAlignment(4);

                    xdataSectionWriter.EmitSymbolDefinition(unwindSymbolName);

                    // Emit UNWIND_INFO
                    xdataSectionWriter.Stream.Write(blob);

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        xdataSectionWriter.Stream.WriteByte((byte)flags);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        xdataSectionWriter.Stream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = GetMangledName(associatedDataNode);
                            xdataSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, symbolName, 0);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = GetMangledName(ehInfo);
                            xdataSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, symbolName, 0);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            xdataSectionWriter.Stream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    // Emit RUNTIME_FUNCTION
                    pdataSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, currentSymbolName, start);
                    // Only x64 has the End symbol
                    if (_machine == Machine.Amd64)
                    {
                        pdataSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, currentSymbolName, end);
                    }
                    // Unwind info pointer
                    pdataSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_ADDR32NB, unwindSymbolName, 0);
                }
            }
        }

        protected override void EmitSectionsAndLayout()
        {
        }

        protected override void EmitObjectFile(string objectFilePath)
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
            foreach (var section in _sections)
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
            foreach (var section in _sections)
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
            foreach (var section in _sections)
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

            // Write symbol table
            Debug.Assert(outputFileStream.Position == coffHeader.PointerToSymbolTable);
            foreach (var coffSymbol in _symbols)
            {
                coffSymbol.Write(outputFileStream, stringTable, coffHeader.IsBigObj);
            }

            // Write string table
            stringTable.Write(outputFileStream);
        }

        protected override void CreateEhSections()
        {
            // Create .xdata and .pdata
            _xdataSectionWriter = GetOrCreateSection(ObjectNodeSection.XDataSection);
            _pdataSectionWriter = GetOrCreateSection(PDataSection);
        }

        protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
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
                _nodeFactory.NameMangler, _nodeFactory.Target.Architecture,
                _debugTypesSectionWriter.Stream);
            return _debugTypesBuilder;
        }

        protected override void EmitDebugFunctionInfo(
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
                var sectionWriter = GetOrCreateSection(GetSharedSection(DebugSymbolSection, methodName));
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

        protected override void EmitDebugSections()
        {
            _debugSymbolsBuilder.WriteUserDefinedTypes(_debugTypesBuilder.UserDefinedTypes);
            _debugFileTableBuilder.Write(_debugSymbolSectionWriter.Stream);
        }

        protected override void EmitDebugStaticVars()
        {
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            using CoffObjectWriter objectWriter = new CoffObjectWriter(factory, options);
            objectWriter.EmitObject(objectFilePath, nodes, dumper, logger);
        }

        private sealed class CoffHeader
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
                0xc7, 0xa1, 0xba, 0xd1, 0xee, 0xba, 0xa9, 0x4b,
                0xaf, 0x20, 0xfa, 0xf6, 0x6a, 0xa4, 0xdc, 0xb8,
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
                sizeof(ushort) + // Signature 2 (NumberOfSections = 0xffff)
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
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), 0xffff);
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
                    string longName = $"/{stringTable.GetStringOffset(Name)}\0\0\0\0\0\0";
                    for (int i = 0; i < 8; i++)
                    {
                        buffer[i] = (byte)longName[i];
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

        private enum CoffRelocationType
        {
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

        private sealed class CoffSymbol : CoffSymbolRecord
        {
            public string Name { get; set; }
            public uint Value { get; set; }
            public uint SectionIndex { get; set; }
            public ushort Type { get; set; }
            public byte StorageClass { get; set; }
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
                buffer[sliceIndex + 2] = StorageClass;
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

        private sealed class CoffStringTable
        {
            private MemoryStream _stream = new();
            private Dictionary<string, uint> _stringToOffset = new();

            public uint GetStringOffset(string str)
            {
                uint offset;

                if (_stringToOffset.TryGetValue(str, out offset))
                {
                    return offset;
                }

                offset = (uint)(_stream.Position + 4);
                var strBytes = Encoding.UTF8.GetBytes(str); // TODO: Pool buffers
                _stream.Write(strBytes);
                _stream.WriteByte(0);
                _stringToOffset[str] = offset;

                return offset;
            }

            public void Write(FileStream stream)
            {
                Span<byte> stringTableSize = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(stringTableSize, (int)(_stream.Length + 4));
                stream.Write(stringTableSize);
                _stream.Position = 0;
                _stream.CopyTo(stream);
            }
        }

        private static class JamCrc32
        {
            private static uint[] Table =
            {
                0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f,
                0xe963a535, 0x9e6495a3, 0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
                0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91, 0x1db71064, 0x6ab020f2,
                0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
                0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec, 0x14015c4f, 0x63066cd9,
                0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
                0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b, 0x35b5a8fa, 0x42b2986c,
                0xdbbbc9d6, 0xacbcf940, 0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
                0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423,
                0xcfba9599, 0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
                0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d, 0x76dc4190, 0x01db7106,
                0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
                0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d,
                0x91646c97, 0xe6635c01, 0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
                0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
                0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
                0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2, 0x4adfa541, 0x3dd895d7,
                0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
                0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa,
                0xbe0b1010, 0xc90c2086, 0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
                0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81,
                0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
                0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683, 0xe3630b12, 0x94643b84,
                0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
                0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb,
                0x196c3671, 0x6e6b06e7, 0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
                0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5, 0xd6d6a3e8, 0xa1d1937e,
                0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
                0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60, 0xdf60efc3, 0xa867df55,
                0x316e8eef, 0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
                0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe, 0xb2bd0b28,
                0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
                0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f,
                0x72076785, 0x05005713, 0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
                0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242,
                0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
                0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69,
                0x616bffd3, 0x166ccf45, 0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
                0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc,
                0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
                0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6, 0xbad03605, 0xcdd70693,
                0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
                0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
            };

            public static uint CalculateChecksum(Stream stream)
            {
                uint crc = 0;
                Span<byte> buffer = stackalloc byte[4096];
                while (stream.Position < stream.Length)
                {
                    int length = stream.Read(buffer);
                    for (int i = 0; i < length; i++)
                    {
                        crc = Table[(crc ^ buffer[i]) & 0xff] ^ (crc >> 8);
                    }
                }
                return crc;
            }
        }
    }
}
