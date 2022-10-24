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
    public class CoffObjectWriter : ObjectWriter
    {
        private Machine _machine;
        private List<(CoffSectionHeader Header, Stream Stream, List<CoffRelocation> Relocations)> _sections = new();

        // Symbol table
        private List<CoffSymbol> _symbols = new();
        private Dictionary<string, uint> _symbolNameToIndex = new();

        // Exception handling
        private int _xdataSectionIndex;
        private int _pdataSectionIndex;
        private Stream _xdataStream;
        private Stream _pdataStream;
        private List<SymbolicRelocation> _xdataRelocations;
        private List<SymbolicRelocation> _pdataRelocations;

        private ObjectNodeSection PDataSection = new ObjectNodeSection("pdata", SectionType.ReadOnly);
        private ObjectNodeSection GfidsSection = new ObjectNodeSection(".gfids$y", SectionType.ReadOnly);

        protected CoffObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
            _machine = factory.Target.Architecture switch
            {
                TargetArchitecture.X64 => Machine.Amd64,
                TargetArchitecture.ARM64 => Machine.Arm64,
                _ => throw new NotSupportedException("Unsupported architecture")
            };
        }

        protected override void CreateSection(ObjectNodeSection section, out Stream sectionStream)
        {
            var sectionHeader = new CoffSectionHeader
            {
                Name = section.Name.StartsWith(".") ? section.Name : "." + section.Name,
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
                    _ => 0
                }
            };

            if (section.Name == "bss")
            {
                sectionHeader.SectionCharacteristics =
                    SectionCharacteristics.MemRead | SectionCharacteristics.MemWrite |
                    SectionCharacteristics.ContainsUninitializedData;
            }

            sectionStream = new MemoryStream();
            _sections.Add((sectionHeader, sectionStream, new List<CoffRelocation>()));
        }

        protected override void UpdateSectionAlignment(int sectionIndex, int alignment, out bool isExecutable)
        {
            Debug.Assert(alignment > 0 && BitOperations.IsPow2((uint)alignment));
            int minimumAlignment = BitOperations.Log2((uint)alignment) << 20;
            int currentAlignment = (int)(_sections[sectionIndex].Header.SectionCharacteristics & SectionCharacteristics.AlignMask);

            if (currentAlignment < minimumAlignment)
            {
                _sections[sectionIndex].Header.SectionCharacteristics =
                    (_sections[sectionIndex].Header.SectionCharacteristics & ~SectionCharacteristics.AlignMask) |
                    (SectionCharacteristics)minimumAlignment;
            }

            isExecutable = _sections[sectionIndex].Header.SectionCharacteristics.HasFlag(SectionCharacteristics.MemExecute);
        }

        protected override void EmitRelocation(
            int sectionIndex,
            List<SymbolicRelocation> relocationList,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            if (relocType == RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 ||
                relocType == RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 ||
                relocType == RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A)
            {
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
                }
            }
            else if (relocType == RelocType.IMAGE_REL_BASED_RELPTR32)
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
            else if (relocType == RelocType.IMAGE_REL_BASED_REL32 ||
                     relocType == RelocType.IMAGE_REL_BASED_ADDR32NB ||
                     relocType == RelocType.IMAGE_REL_BASED_ABSOLUTE)
            {
                if (addend != 0)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(
                        data,
                        BinaryPrimitives.ReadInt32LittleEndian(data) +
                        addend);
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported relocation: {relocType}");
            }

            relocationList.Add(new SymbolicRelocation(offset, relocType, symbolName, 0));
        }

        protected override void EmitSymbolTable()
        {
            foreach (var (symbolName, symbolDefinition) in GetDefinedSymbols())
            {
                _symbolNameToIndex.Add(symbolName, (uint)_symbols.Count);
                _symbols.Add(new CoffSymbol
                {
                    Name = symbolName,
                    Value = (int)symbolDefinition.Value,
                    SectionIndex = (short)(1 + symbolDefinition.SectionIndex),
                    StorageClass = 2 // IMAGE_SYM_CLASS_EXTERNAL
                });
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
                GetOrCreateSection(GfidsSection, out var sectionStream, out _);

                Span<byte> tempBuffer = stackalloc byte[4];
                foreach (var (symbolName, symbolDefinition) in GetDefinedSymbols())
                {
                    // For now consider all method symbols address taken.
                    // We could restrict this in the future to those that are referenced from
                    // reflection tables, EH tables, were actually address taken in code, or are referenced from vtables.
                    if (symbolDefinition.Size > 0)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, _symbolNameToIndex[symbolName]);
                        sectionStream.Write(tempBuffer);
                    }
                }

                // Emit the feat.00 symbol that controls various linker behaviors
                _symbols.Add(new CoffSymbol
                {
                    Name = "@feat.00",
                    StorageClass = 3, // IMAGE_SYM_CLASS_STATIC
                    SectionIndex = -1, // IMAGE_SYM_ABSOLUTE
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
                    coffRelocations.Add(new CoffRelocation { VirtualAddress = (uint)relocationList.Count });
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

        protected override void EmitUnwindInfo(int sectionIndex, long methodStart, INodeWithCodeInfo nodeWithCodeInfo)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                Span<byte> tempBuffer = stackalloc byte[4];
                string currentSymbolName = ExternCName(symbolDefinitionNode.GetMangledName(_nodeFactory.NameMangler));

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    // TODO: {_nodeFactory.NameMangler.CompilationUnitPrefix} ?
                    string unwindSymbolName = $"_unwind{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    // Need to emit the UNWIND_INFO at 4-byte alignment to ensure that the
                    // pointer has the lower two bits in .pdata section set to zero. On ARM64
                    // non-zero bits would mean a compact encoding.
                    EmitAlignment(_xdataSectionIndex, _xdataStream, 4);

                    EmitSymbolDefinition(unwindSymbolName, new SymbolDefinition(_xdataSectionIndex, _xdataStream.Position));
                    if (start != 0)
                    {
                        EmitSymbolDefinition(framSymbolName, new SymbolDefinition(sectionIndex, methodStart + start, 0));
                    }

                    // Emit UNWIND_INFO
                    _xdataStream.Write(blob);

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        _xdataStream.WriteByte((byte)flags);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        _xdataStream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = ExternCName(associatedDataNode.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _xdataSectionIndex,
                                _xdataRelocations,
                                (int)_xdataStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_ADDR32NB,
                                symbolName,
                                0);
                            _xdataStream.Write(tempBuffer);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = ExternCName(ehInfo.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _xdataSectionIndex,
                                _xdataRelocations,
                                (int)_xdataStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_ADDR32NB,
                                symbolName,
                                0);
                            _xdataStream.Write(tempBuffer);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            _xdataStream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    // Emit RUNTIME_FUNCTION

                    // Start
                    tempBuffer.Clear();
                    EmitRelocation(
                        _pdataSectionIndex,
                        _pdataRelocations,
                        (int)_pdataStream.Position,
                        tempBuffer,
                        RelocType.IMAGE_REL_BASED_ADDR32NB,
                        currentSymbolName,
                        start);
                    _pdataStream.Write(tempBuffer);

                    // Only x64 has the End symbol
                    if (_machine == Machine.Amd64)
                    {
                        // End
                        tempBuffer.Clear();
                        EmitRelocation(
                            _pdataSectionIndex,
                            _pdataRelocations,
                            (int)_pdataStream.Position,
                            tempBuffer,
                            RelocType.IMAGE_REL_BASED_ADDR32NB,
                            currentSymbolName,
                            end);
                        _pdataStream.Write(tempBuffer);
                    }

                    // Unwind info pointer
                    tempBuffer.Clear();
                    EmitRelocation(
                        _pdataSectionIndex,
                        _pdataRelocations,
                        (int)_pdataStream.Position,
                        tempBuffer,
                        RelocType.IMAGE_REL_BASED_ADDR32NB,
                        unwindSymbolName,
                        0);
                    _pdataStream.Write(tempBuffer);
                }
            }
        }

        protected override void EmitSectionsAndLayout()
        {
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            var stringTable = new MemoryStream();

            // Calculate size of section data and assign offsets
            int dataOffset = CoffHeader.Size + _sections.Count * CoffSectionHeader.Size;
            int sectionIndex = 0;
            foreach (var section in _sections)
            {
                section.Header.SizeOfRawData = (int)section.Stream.Length;

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
                dataOffset += section.Relocations.Count * CoffRelocation.Size;

                sectionIndex++;
            }

            int symbolTableOffset = dataOffset;
            int stringTableOffset =
                symbolTableOffset + _symbols.Count * CoffSymbol.Size +
                sizeof(int); // Count

            // Write COFF header
            var coffHeader = new CoffHeader
            {
                Machine = _machine,
                NumberOfSections = (ushort)_sections.Count,
                PointerToSymbolTable = (uint)symbolTableOffset,
                NumberOfSymbols = (uint)_symbols.Count,
            };
            coffHeader.Write(outputFileStream);

            // Write COFF section headers
            foreach ((CoffSectionHeader sectionHeader, _, _) in _sections)
            {
                sectionHeader.Write(outputFileStream, stringTable);

                // Relocation code below assumes that addresses are 0-indexed
                Debug.Assert(sectionHeader.VirtualAddress == 0);
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
            Debug.Assert(outputFileStream.Position == symbolTableOffset);
            foreach (var coffSymbol in _symbols)
            {
                coffSymbol.Write(outputFileStream, stringTable);
            }

            // Write string table
            Span<byte> stringTableSize = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(stringTableSize, (int)(stringTable.Length + 4));
            outputFileStream.Write(stringTableSize);
            Debug.Assert(outputFileStream.Position == stringTableOffset);
            stringTable.Position = 0;
            stringTable.CopyTo(outputFileStream);
        }

        protected override void CreateEhSections()
        {
            // Create .xdata and .pdata
            _xdataSectionIndex = GetOrCreateSection(ObjectNodeSection.XDataSection, out _xdataStream, out _xdataRelocations);
            _pdataSectionIndex = GetOrCreateSection(PDataSection, out _pdataStream, out _pdataRelocations);
        }

        protected override ITypesDebugInfoWriter CreateDebugInfoBuilder() => null;

        protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
        }

        protected override void EmitDebugSections()
        {
        }

        protected override void EmitDebugStaticVars()
        {
        }

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            // Not supported yet
            options &= ~ObjectWritingOptions.GenerateDebugInfo;

            using CoffObjectWriter objectWriter = new CoffObjectWriter(factory, options);
            objectWriter.EmitObject(objectFilePath, nodes, dumper, logger);
        }

        private sealed class CoffHeader
        {
            public Machine Machine { get; set; }
            public ushort NumberOfSections { get; set; }
            public uint TimeDateStamp { get; set; }
            public uint PointerToSymbolTable { get; set; }
            public uint NumberOfSymbols { get; set; }
            public ushort SizeOfOptionalHeader { get; set; }
            public ushort Characteristics { get; set; }

            public const int Size =
                sizeof(ushort) + // Machine
                sizeof(ushort) + // NumberOfSections
                sizeof(uint) +   // TimeDateStamp
                sizeof(uint) +   // PointerToSymbolTable
                sizeof(uint) +   // NumberOfSymbols
                sizeof(ushort) + // SizeOfOptionalHeader
                sizeof(ushort);  // Characteristics

            public void Write(Stream stream)
            {
                Span<byte> buffer = stackalloc byte[Size];

                BinaryPrimitives.WriteInt16LittleEndian(buffer, (short)Machine);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(2), NumberOfSections);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), TimeDateStamp);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(8), PointerToSymbolTable);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(12), NumberOfSymbols);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(16), SizeOfOptionalHeader);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(18), Characteristics);

                stream.Write(buffer);
            }
        }

        private sealed class CoffSectionHeader
        {
            public string Name { get; set; }
            public int VirtualSize { get; set; }
            public int VirtualAddress { get; set; }
            public int SizeOfRawData { get; set; }
            public int PointerToRawData { get; set; }
            public int PointerToRelocations { get; set; }
            public int PointerToLineNumbers { get; set; }
            public ushort NumberOfRelocations { get; set; }
            public ushort NumberOfLineNumbers { get; set; }
            public SectionCharacteristics SectionCharacteristics { get; set; }

            private const int NameSize = 8;

            public const int Size =
                NameSize +      // Name size
                sizeof(int) +   // VirtualSize
                sizeof(int) +   // VirtualAddress
                sizeof(int) +   // SizeOfRawData
                sizeof(int) +   // PointerToRawData
                sizeof(int) +   // PointerToRelocations
                sizeof(int) +   // PointerToLineNumbers
                sizeof(short) + // NumberOfRelocations
                sizeof(short) + // NumberOfLineNumbers
                sizeof(int);    // SectionCharacteristics

            public void Write(Stream stream, Stream stringTableStream)
            {
                Span<byte> buffer = stackalloc byte[Size];

                var nameBytes = Encoding.UTF8.GetBytes(Name); // TODO: Pool buffers
                if (nameBytes.Length <= NameSize)
                {
                    nameBytes.CopyTo(buffer);
                    if (nameBytes.Length < NameSize)
                    {
                        buffer.Slice(nameBytes.Length, 8 - nameBytes.Length).Clear();
                    }
                }
                else
                {
                    string longName = $"/{stringTableStream.Position + 4}\0\0\0\0\0\0";
                    for (int i = 0; i < 8; i++)
                    {
                        buffer[i] = (byte)longName[i];
                    }
                    stringTableStream.Write(nameBytes);
                    stringTableStream.WriteByte(0);
                }

                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize), VirtualSize);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 4), VirtualAddress);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 8), SizeOfRawData);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 12), PointerToRawData);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 16), PointerToRelocations);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 20), PointerToLineNumbers);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(NameSize + 24), NumberOfRelocations);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(NameSize + 26), NumberOfLineNumbers);
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize + 28), (int)SectionCharacteristics);

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

            public void Write(Stream stream)
            {
                Span<byte> buffer = stackalloc byte[Size];

                BinaryPrimitives.WriteUInt32LittleEndian(buffer, VirtualAddress);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4), SymbolTableIndex);
                BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(8), (ushort)Type);

                stream.Write(buffer);
            }
        }

        private sealed class CoffSymbol
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public short SectionIndex { get; set; }
            public short Type { get; set; }
            public byte StorageClass { get; set; }
            public byte AuxiliaryCount { get; set; }

            private const int NameSize = 8;

            public const int Size =
                NameSize +      // Name size
                sizeof(int) +   // Value
                sizeof(short) + // Section index
                sizeof(short) + // Type
                sizeof(byte) +  // Storage class
                sizeof(byte);   // Auxiliary symbol count

            public void Write(Stream stream, Stream stringTableStream)
            {
                Span<byte> buffer = stackalloc byte[Size];

                var nameBytes = Encoding.UTF8.GetBytes(Name); // TODO: Pool buffers
                if (nameBytes.Length <= NameSize)
                {
                    nameBytes.CopyTo(buffer);
                    if (nameBytes.Length < NameSize)
                    {
                        buffer.Slice(nameBytes.Length, 8 - nameBytes.Length).Clear();
                    }
                }
                else
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer, 0);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(4, 4), (uint)(stringTableStream.Position + 4));
                    stringTableStream.Write(nameBytes);
                    stringTableStream.WriteByte(0);
                }

                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(NameSize), Value);
                BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(NameSize + 4), SectionIndex);
                BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(NameSize + 6), Type);
                buffer[NameSize + 8] = StorageClass;
                buffer[NameSize + 9] = AuxiliaryCount;

                stream.Write(buffer);
            }
        }
    }
}
