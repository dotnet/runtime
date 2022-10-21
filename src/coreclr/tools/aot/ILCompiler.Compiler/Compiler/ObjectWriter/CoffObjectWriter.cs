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
        private List<(CoffSectionHeader Header, Stream Stream)> _sections = new();
        private List<CoffSymbol> _symbols = new();
        private Dictionary<string, int> _symbolNameToIndex = new();

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
            _sections.Add((sectionHeader, sectionStream));
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
                _symbolNameToIndex.Add(symbolName, _symbols.Count);
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
                _symbolNameToIndex.Add(symbolName, _symbols.Count);
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
                        BinaryPrimitives.WriteInt32LittleEndian(tempBuffer, _symbolNameToIndex[symbolName]);
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
            // All relocations are emitted when writting final object file
        }

        protected override void EmitUnwindInfo(int sectionIndex, long methodStart, INodeWithCodeInfo nodeWithCodeInfo)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                Span<byte> tempBuffer = stackalloc byte[4];
                string currentSymbolName = ExternCName(symbolDefinitionNode.GetMangledName(_nodeFactory.NameMangler));

                int xdataSectionIndex = GetOrCreateSection(
                    ObjectNodeSection.XDataSection, out var xdataStream, out var xdataRelocations);
                int pdataSectionIndex = GetOrCreateSection(
                    PDataSection, out var pdataStream, out var pdataRelocations);

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    // TODO: {_nodeFactory.NameMangler.CompilationUnitPrefix} ?
                    string unwindSymbolName = $"_unwind{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    UpdateSectionAlignment(xdataSectionIndex, 4, out _);
                    EmitSymbolDefinition(unwindSymbolName, new SymbolDefinition(xdataSectionIndex, xdataStream.Position));
                    if (start != 0)
                    {
                        EmitSymbolDefinition(framSymbolName, new SymbolDefinition(sectionIndex, methodStart + start, 0));
                    }

                    xdataStream.Write(blob);

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        xdataStream.WriteByte((byte)flags);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        xdataStream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = ExternCName(associatedDataNode.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                xdataSectionIndex,
                                xdataRelocations,
                                (int)xdataStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_ADDR32NB,
                                symbolName,
                                0);
                            xdataStream.Write(tempBuffer);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = ExternCName(ehInfo.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                xdataSectionIndex,
                                xdataRelocations,
                                (int)xdataStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_ADDR32NB,
                                symbolName,
                                0);
                            xdataStream.Write(tempBuffer);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            xdataStream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    // Emit UNWIND_INFO
                    // TODO: Other architectures

                    // Start
                    tempBuffer.Clear();
                    EmitRelocation(
                        pdataSectionIndex,
                        pdataRelocations,
                        (int)pdataStream.Position,
                        tempBuffer,
                        RelocType.IMAGE_REL_BASED_ADDR32NB,
                        currentSymbolName,
                        start);
                    pdataStream.Write(tempBuffer);

                    // End
                    tempBuffer.Clear();
                    EmitRelocation(
                        pdataSectionIndex,
                        pdataRelocations,
                        (int)pdataStream.Position,
                        tempBuffer,
                        RelocType.IMAGE_REL_BASED_ADDR32NB,
                        currentSymbolName,
                        end);
                    pdataStream.Write(tempBuffer);

                    // Unwind info pointer
                    tempBuffer.Clear();
                    EmitRelocation(
                        pdataSectionIndex,
                        pdataRelocations,
                        (int)pdataStream.Position,
                        tempBuffer,
                        RelocType.IMAGE_REL_BASED_ADDR32NB,
                        unwindSymbolName,
                        0);
                    pdataStream.Write(tempBuffer);
                }
            }
        }

        protected override void EmitSectionsAndLayout()
        {
        }

        protected override void EmitObjectFile(string objectFilePath)
        {
            const int CoffHeaderSize =
                sizeof(short) + // Machine
                sizeof(short) + // NumberOfSections
                sizeof(int) +   // TimeDateStamp
                sizeof(int) +   // PointerToSymbolTable
                sizeof(int) +   // NumberOfSymbols
                sizeof(short) + // SizeOfOptionalHeader
                sizeof(ushort); // Characteristics
            const int CoffSectionHeaderSize =
                8 +             // Name size
                sizeof(int) +   // VirtualSize
                sizeof(int) +   // VirtualAddress
                sizeof(int) +   // SizeOfRawData
                sizeof(int) +   // PointerToRawData
                sizeof(int) +   // PointerToRelocations
                sizeof(int) +   // PointerToLineNumbers
                sizeof(short) + // NumberOfRelocations
                sizeof(short) + // NumberOfLineNumbers
                sizeof(int);    // SectionCharacteristics
            const int CoffRelocationSize =
                sizeof(int) +   // Address
                sizeof(int) +   // Symbol index
                sizeof(short);  // Type
            const int CoffSymbolSize =
                8 +             // Name size
                sizeof(int) +   // Value
                sizeof(short) + // Section index
                sizeof(short) + // Type
                sizeof(byte) +  // Storage class
                sizeof(byte);   // Auxiliary symbol count

            using var outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            using var binaryWriter = new BinaryWriter(outputFileStream); // TODO: Big endian?

            var stringTable = new MemoryStream();

            // Calculate size of section data
            int sectionIndex = 0;
            foreach ((CoffSectionHeader sectionHeader, Stream sectionStream) in _sections)
            {
                sectionHeader.SizeOfRawData = (int)sectionStream.Length;
                GetSection(sectionIndex, out _, out var relocationList);
                if (relocationList.Count <= ushort.MaxValue)
                {
                    sectionHeader.NumberOfRelocations = (ushort)relocationList.Count;
                }
                else
                {
                    sectionHeader.NumberOfRelocations = ushort.MaxValue;
                    sectionHeader.SectionCharacteristics |= SectionCharacteristics.LinkerNRelocOvfl;
                }
                sectionIndex++;
            }

            // Assign offsets to section data
            int dataOffset = CoffHeaderSize + _sections.Count * CoffSectionHeaderSize;
            sectionIndex = 0;
            foreach ((CoffSectionHeader sectionHeader, _) in _sections)
            {
                GetSection(sectionIndex, out _, out var relocationList);

                if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    sectionHeader.PointerToRawData = 0;
                }
                else
                {
                    sectionHeader.PointerToRawData = dataOffset;
                    dataOffset += sectionHeader.SizeOfRawData;
                }

                sectionHeader.PointerToRelocations = relocationList.Count > 0 ? dataOffset : 0;
                dataOffset += relocationList.Count * CoffRelocationSize;
                if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.LinkerNRelocOvfl))
                {
                    dataOffset += CoffRelocationSize;
                }

                sectionIndex++;
            }

            int symbolTableOffset = dataOffset;
            int stringTableOffset =
                symbolTableOffset + _symbols.Count * CoffSymbolSize +
                sizeof(int); // Count

            // Write COFF header
            binaryWriter.Write((ushort)_machine);
            binaryWriter.Write((ushort)_sections.Count);
            binaryWriter.Write((uint)0u); // TimeDateStamp
            binaryWriter.Write(symbolTableOffset);
            binaryWriter.Write((uint)_symbols.Count);
            binaryWriter.Write((ushort)0u); // SizeOfOptionalHeader
            binaryWriter.Write((ushort)0u); // Characteristics

            // Write COFF section headers
            foreach ((CoffSectionHeader sectionHeader, _) in _sections)
            {
                WritePaddedName(sectionHeader.Name, isSectionName: true);
                binaryWriter.Write(sectionHeader.VirtualSize);
                binaryWriter.Write(sectionHeader.VirtualAddress);
                binaryWriter.Write(sectionHeader.SizeOfRawData);
                binaryWriter.Write(sectionHeader.PointerToRawData);
                binaryWriter.Write(sectionHeader.PointerToRelocations);
                binaryWriter.Write(sectionHeader.PointerToLineNumbers);
                binaryWriter.Write(sectionHeader.NumberOfRelocations);
                binaryWriter.Write(sectionHeader.NumberOfLineNumbers);
                binaryWriter.Write((int)sectionHeader.SectionCharacteristics);

                // Relocation code below assumes that addresses are 0-indexed
                Debug.Assert(sectionHeader.VirtualAddress == 0);
            }

            // Writer section content and relocations
            sectionIndex = 0;
            foreach ((CoffSectionHeader sectionHeader, Stream sectionStream) in _sections)
            {
                if (!sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.ContainsUninitializedData))
                {
                    Debug.Assert(outputFileStream.Position == sectionHeader.PointerToRawData);
                    sectionStream.Position = 0;
                    sectionStream.CopyTo(outputFileStream);
                }

                GetSection(sectionIndex, out _, out var relocationList);

                if (relocationList.Count > 0)
                {
                    Debug.Assert(outputFileStream.Position == sectionHeader.PointerToRelocations);
                    if (sectionHeader.SectionCharacteristics.HasFlag(SectionCharacteristics.LinkerNRelocOvfl))
                    {
                        binaryWriter.Write((int)relocationList.Count);
                        binaryWriter.Write((int)0);
                        binaryWriter.Write((ushort)0u);
                    }

                    foreach (var relocation in relocationList)
                    {
                        // Addends are emitted directly into code in EmitRelocation
                        Debug.Assert(relocation.Addend == 0);

                        binaryWriter.Write((int)relocation.Offset);
                        binaryWriter.Write((int)_symbolNameToIndex[relocation.SymbolName]);

                        // FIXME: Other architectures
                        var relocationType = relocation.Type switch
                        {
                            RelocType.IMAGE_REL_BASED_ABSOLUTE => 3u,
                            RelocType.IMAGE_REL_BASED_ADDR32NB => 3u,
                            RelocType.IMAGE_REL_BASED_HIGHLOW => 2u,
                            RelocType.IMAGE_REL_BASED_DIR64 => 1u,
                            RelocType.IMAGE_REL_BASED_REL32 => 4u,
                            RelocType.IMAGE_REL_BASED_RELPTR32 => 4u,
                            _ => throw new NotSupportedException($"Unsupported relocation: {relocation.Type}")
                        };

                        binaryWriter.Write((ushort)relocationType);
                    }
                }

                sectionIndex++;
            }

            // Write symbol table
            Debug.Assert(outputFileStream.Position == symbolTableOffset);
            foreach (var coffSymbol in _symbols)
            {
                WritePaddedName(coffSymbol.Name);
                binaryWriter.Write(coffSymbol.Value);
                binaryWriter.Write(coffSymbol.SectionIndex);
                binaryWriter.Write(coffSymbol.Type);
                binaryWriter.Write(coffSymbol.StorageClass);
                binaryWriter.Write(coffSymbol.AuxiliaryCount);
            }

            // Write string table
            binaryWriter.Write((int)(stringTable.Length + 4));
            Debug.Assert(outputFileStream.Position == stringTableOffset);
            stringTable.Position = 0;
            stringTable.CopyTo(outputFileStream);

            void WritePaddedName(string name, bool isSectionName = false)
            {
                // TODO: Reuse buffer
                var nameBytes = Encoding.UTF8.GetBytes(name);
                if (nameBytes.Length <= 8)
                {
                    binaryWriter.Write(nameBytes);
                    if (nameBytes.Length < 8)
                    {
                        binaryWriter.Write(stackalloc byte[8 - nameBytes.Length]);
                    }
                }
                else
                {
                    if (!isSectionName)
                    {
                        binaryWriter.Write((uint)0u);
                        binaryWriter.Write((uint)(stringTable.Position + 4));
                    }
                    else
                    {
                        string longName = $"/{stringTable.Position + 4}\0\0\0\0\0\0";
                        for (int i = 0; i < 8; i++)
                        {
                            binaryWriter.Write((byte)longName[i]);
                        }
                    }
                    stringTable.Write(nameBytes);
                    stringTable.WriteByte(0);
                }
            }
        }

        protected override void CreateEhSections()
        {
            // Create .xdata and .pdata
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
        }

        private sealed class CoffSymbol
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public short SectionIndex { get; set; }
            public short Type { get; set; }
            public byte StorageClass { get; set; }
            public byte AuxiliaryCount { get; set; }
        }
    }
}
