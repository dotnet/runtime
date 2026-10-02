// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

using static ILCompiler.DependencyAnalysis.ObjectNode;
using static ILCompiler.DependencyAnalysis.RelocType;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;

namespace ILCompiler.ObjectWriter
{
    public abstract partial class ObjectWriter
    {
        protected virtual CodeDataLayout LayoutMode => CodeDataLayout.Unified;
        private protected sealed record SymbolDefinition(int SectionIndex, long Value, int Size = 0, bool Global = false);
        protected sealed record SymbolicRelocation(long Offset, RelocType Type, Utf8String SymbolName, long Addend = 0);
        private sealed record BlockToRelocate(int SectionIndex, long Offset, byte[] Data, Relocation[] Relocations);
        private protected sealed record ChecksumsToCalculate(int SectionIndex, long Offset, Relocation[] ChecksumRelocations);

        private protected readonly NodeFactory _nodeFactory;
        private protected readonly ObjectWritingOptions _options;
        private protected readonly OutputInfoBuilder _outputInfoBuilder;
        private readonly bool _isSingleFileCompilation;
        protected readonly Utf8StringBuilder _utf8StringBuilder = new();

        private readonly Dictionary<ISymbolNode, Utf8String> _mangledNameMap = new();

        private const uint Arm64BranchLinkInstruction = 0x94000000;
        private const int Arm64BranchRegionSize = 0x04000000;
        private readonly byte _insPaddingByte;
        private int _arm64BranchThunkId;

        // Standard sections
        private readonly Dictionary<string, int> _sectionNameToSectionIndex = new(StringComparer.Ordinal);
        private readonly List<SectionData> _sectionIndexToData = new();
        private readonly List<List<SymbolicRelocation>> _sectionIndexToRelocations = new();
        private protected readonly List<OutputSection> _outputSectionLayout = [];

        // Symbol table
        private readonly Dictionary<Utf8String, SymbolDefinition> _definedSymbols = new();

        private protected ObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder = null)
        {
            _nodeFactory = factory;
            _options = options;
            _outputInfoBuilder = outputInfoBuilder;
            _isSingleFileCompilation = _nodeFactory.CompilationModuleGroup.IsSingleFileCompilation;

            // Padding byte for code sections (NOP for x86/x64)
            _insPaddingByte = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => 0x90,
                TargetArchitecture.X64 => 0x90,
                _ => 0
            };
        }
        private protected virtual bool UsesSubsectionsViaSymbols => false;
        private protected virtual bool UseArm64BranchRangeExtensionThunks => false;

        private protected abstract void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream);

        protected internal abstract void UpdateSectionAlignment(int sectionIndex, int alignment);

        /// <summary>
        /// Get the section in the image where nodes in the passed in section should actually be emitted.
        /// </summary>
        /// <param name="section">A node's requested section.</param>
        /// <returns>The section to actually emit the node into.</returns>
        /// <remarks>
        /// Sections in an image can be very expensive, and unlike linkable formats,
        /// sections cannot be merged after the fact.
        /// This method allows formats that want to merge sections during emit to do so.
        /// </remarks>
        private protected virtual ObjectNodeSection GetEmitSection(ObjectNodeSection section) => section;

        private protected SectionWriter GetOrCreateSection(ObjectNodeSection section)
            => GetOrCreateSection(section, default, default);

        /// <summary>
        /// Get or creates an object file section.
        /// </summary>
        /// <param name="section">Base section name and type definition.</param>
        /// <param name="comdatName">Name of the COMDAT symbol or null.</param>
        /// <param name="symbolName">Name of the section definiting symbol for COMDAT or null</param>
        /// <returns>Writer for a given section.</returns>
        /// <remarks>
        /// When creating a COMDAT section both <paramref name="comdatName"/> and <paramref name="symbolName"/>
        /// has to be specified. <paramref name="comdatName"/> specifies the group section. For the primary
        /// symbol both <paramref name="comdatName"/> and <paramref name="symbolName"/> will be the same.
        /// For associated sections, such as exception or debugging information, the <paramref name="symbolName"/>
        /// will be different.
        /// </remarks>
        private protected SectionWriter GetOrCreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName)
        {
            int sectionIndex;
            SectionData sectionData;

            section = GetEmitSection(section);

            if (!comdatName.IsNull || !_sectionNameToSectionIndex.TryGetValue(section.Name, out sectionIndex))
            {
                sectionData = new SectionData(section.Type == SectionType.Executable ? _insPaddingByte : (byte)0);
                sectionIndex = _sectionIndexToData.Count;
                CreateSection(section, comdatName, symbolName, sectionIndex, sectionData.GetReadStream());
                _sectionIndexToData.Add(sectionData);
                _sectionIndexToRelocations.Add(new());
                if (comdatName.IsNull)
                {
                    _sectionNameToSectionIndex.Add(section.Name, sectionIndex);
                }
            }
            else
            {
                sectionData = _sectionIndexToData[sectionIndex];
            }

            return new SectionWriter(
                this,
                sectionIndex,
                sectionData);
        }

        private protected bool ShouldShareSymbol(ObjectNode node)
        {
            if (UsesSubsectionsViaSymbols)
                return false;

            return ShouldShareSymbol(node, node.GetSection(_nodeFactory));
        }

        private protected bool ShouldShareSymbol(ObjectNode node, ObjectNodeSection section)
        {
            if (UsesSubsectionsViaSymbols)
                return false;

            // Foldable sections are always COMDATs
            if (section == ObjectNodeSection.FoldableReadOnlyDataSection)
                return true;

            if (_isSingleFileCompilation)
                return false;

            if (node is not ISymbolNode)
                return false;

            if (node is IUniqueSymbolNode)
                return false;

            return true;
        }

        private unsafe void EmitOrResolveRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            Utf8String symbolName,
            long addend)
        {
            if (!UsesSubsectionsViaSymbols &&
                relocType is IMAGE_REL_BASED_REL32 or IMAGE_REL_BASED_RELPTR32 or IMAGE_REL_BASED_ARM64_BRANCH26
                or IMAGE_REL_BASED_THUMB_BRANCH24 or IMAGE_REL_BASED_THUMB_MOV32_PCREL &&
                _definedSymbols.TryGetValue(symbolName, out SymbolDefinition definedSymbol) &&
                definedSymbol.SectionIndex == sectionIndex)
            {
                // Resolve the relocation to already defined symbol and write it into data
                fixed (byte* pData = data)
                {
                    // Method symbols should be defined with the thumb bit (+1) set per the AAELF ABI
                    // convention. For BRANCH24, the encoding cannot represent the thumb bit
                    // (per AAELF formula ((S + A) | T) – P), so strip it from the symbol value.
                    long symbolValue = relocType is IMAGE_REL_BASED_THUMB_BRANCH24
                        ? definedSymbol.Value & ~1L
                        : definedSymbol.Value;
                    long adjustedAddend = addend;

                    adjustedAddend -= relocType switch
                    {
                        IMAGE_REL_BASED_REL32 => 4,
                        IMAGE_REL_BASED_THUMB_BRANCH24 => 4,
                        IMAGE_REL_BASED_THUMB_MOV32_PCREL => 12,
                        _ => 0
                    };

                    adjustedAddend += symbolValue;
                    adjustedAddend += Relocation.ReadValue(relocType, (void*)pData);
                    adjustedAddend -= offset;

                    if (relocType is IMAGE_REL_BASED_THUMB_BRANCH24 && !Relocation.FitsInThumb2BlRel24((int)adjustedAddend))
                    {
                        EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
                    }
                    else
                    {
                        Relocation.WriteValue(relocType, (void*)pData, adjustedAddend);
                    }
                }
            }
            else if (relocType is IMAGE_REL_SYMBOL_SIZE &&
                _definedSymbols.TryGetValue(symbolName, out definedSymbol))
            {
                fixed (byte* pData = data)
                {
                    long adjustedAddend = addend + Relocation.ReadValue(relocType, (void*)pData);
                    Relocation.WriteValue(relocType, (void*)pData, definedSymbol.Size + adjustedAddend);
                }
            }
            else
            {
                EmitRelocation(sectionIndex, offset, data, relocType, symbolName, addend);
            }
        }

        /// <summary>
        /// Emits a single relocation into a given section.
        /// </summary>
        /// <remarks>
        /// The relocation is not resolved until <see cref="EmitRelocations" /> is called
        /// later when symbol table is already generated.
        /// </remarks>
        protected internal virtual void EmitRelocation(
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            Utf8String symbolName,
            long addend)
        {
            _sectionIndexToRelocations[sectionIndex].Add(new SymbolicRelocation(offset, relocType, symbolName, addend));
        }

        private protected bool SectionHasRelocations(int sectionIndex)
        {
            return _sectionIndexToRelocations[sectionIndex].Count > 0;
        }

        private protected virtual void EmitReferencedMethod(Utf8String symbolName) { }

        /// <summary>
        /// Emit symbolic relocations into object file as format specific
        /// relocations.
        /// </summary>
        /// <remarks>
        /// This methods is guaranteed to run after <see cref="EmitSymbolTable" />.
        /// </remarks>
        private protected abstract void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList);

        /// <summary>
        /// Emit new symbol definition at specified location in a given section.
        /// </summary>
        /// <remarks>
        /// The symbols are emitted into the object file representation later by
        /// <see cref="EmitSymbolTable" />. Various formats have restrictions on
        /// the order of the symbols so any necessary sorting is done when the
        /// symbol table is created.
        /// </remarks>
        protected internal void EmitSymbolDefinition(
            int sectionIndex,
            Utf8String symbolName,
            long offset = 0,
            int size = 0,
            bool global = false)
        {
            _definedSymbols.Add(
                symbolName,
                new SymbolDefinition(sectionIndex, offset, size, global));
        }

        /// <summary>
        /// Emit symbolic definitions into object file symbols.
        /// </summary>
        private protected abstract void EmitSymbolTable(
            IDictionary<Utf8String, SymbolDefinition> definedSymbols,
            SortedSet<Utf8String> undefinedSymbols);

        private protected virtual Utf8String ExternCName(Utf8String name) => name;

        private protected Utf8String GetMangledName(ISymbolNode symbolNode)
        {
            Utf8String symbolName;

            if (!_mangledNameMap.TryGetValue(symbolNode, out symbolName))
            {
                symbolNode.AppendMangledName(_nodeFactory.NameMangler, _utf8StringBuilder.Clear());
                symbolName = ExternCName(_utf8StringBuilder.ToUtf8String());
                _mangledNameMap.Add(symbolNode, symbolName);
            }

            return symbolName;
        }

        private protected virtual void EmitSectionsAndLayout()
        {
        }

        private protected abstract void EmitObjectFile(Stream outputFileStream);

        partial void EmitDebugInfo(IReadOnlyCollection<DependencyNode> nodes, Logger logger);

        private SortedSet<Utf8String> GetUndefinedSymbols()
        {
            SortedSet<Utf8String> undefinedSymbolSet = new SortedSet<Utf8String>();
            foreach (var relocationList in _sectionIndexToRelocations)
            {
                foreach (var symbolicRelocation in relocationList)
                {
                    if (!_definedSymbols.ContainsKey(symbolicRelocation.SymbolName))
                    {
                        undefinedSymbolSet.Add(symbolicRelocation.SymbolName);
                    }
                }
            }
            return undefinedSymbolSet;
        }

        public virtual void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            // Pre-create some of the sections
            GetOrCreateSection(ObjectNodeSection.TextSection);
            if (_nodeFactory.Target.OperatingSystem == TargetOS.Windows)
            {
                GetOrCreateSection(ObjectNodeSection.ManagedCodeWindowsContentSection);
            }
            else
            {
                GetOrCreateSection(ObjectNodeSection.ManagedCodeUnixContentSection);
            }

            // Create sections for exception handling
            if (_options.HasFlag(ObjectWritingOptions.GenerateUnwindInfo))
            {
                PrepareForUnwindInfo();
            }

            ProgressReporter progressReporter = default;
            if (logger.IsVerbose)
            {
                int count = 0;
                foreach (var node in nodes)
                    if (node is ObjectNode)
                        count++;

                logger.LogMessage($"Writing {count} object nodes...");

                progressReporter = new ProgressReporter(logger, count);
            }

            List<ISymbolRangeNode> symbolRangeNodes = [];
            List<BlockToRelocate> blocksToRelocate = [];
            List<ChecksumsToCalculate> checksumRelocations = [];
            Dictionary<int, Arm64BranchRegion> arm64BranchRegions = null;
            List<Arm64BranchRelocation> arm64BranchRelocations = null;
            foreach (DependencyNode depNode in nodes)
            {
                // TODO-WASM: emit symbol ranges properly when code and data are separated
                // Right now we still need to determine placements for some traditionally text-placed nodes,
                // such as DebugDirectoryEntryNode and AssemblyStubNode
                if (depNode is ISymbolRangeNode symbolRange)
                {
                    symbolRangeNodes.Add(symbolRange);
                    continue;
                }

                if (depNode is not ObjectNode node)
                    continue;

                if (logger.IsVerbose)
                    progressReporter.LogProgress();

                if (node.ShouldSkipEmittingObjectNode(_nodeFactory))
                    continue;

                ISymbolNode symbolNode = node as ISymbolNode;

                if (symbolNode is not null)
                {
                    ISymbolNode deduplicatedSymbolNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, symbolNode);
                    if (deduplicatedSymbolNode != symbolNode)
                    {
                        dumper?.ReportFoldedNode(_nodeFactory, node, deduplicatedSymbolNode);
                        continue;
                    }
                }

                ObjectData nodeContents = node.GetData(_nodeFactory);

                dumper?.DumpObjectNode(_nodeFactory, node, nodeContents);

                Utf8String currentSymbolName = default;
                if (symbolNode != null)
                {
                    currentSymbolName = GetMangledName(symbolNode);
                }

                ObjectNodeSection section = node.GetSection(_nodeFactory);
                SectionWriter sectionWriter = ShouldShareSymbol(node, section) ?
                    GetOrCreateSection(section, currentSymbolName, currentSymbolName) :
                    GetOrCreateSection(section);

                if (section.NeedsAlignment)
                {
                    sectionWriter.EmitAlignment(nodeContents.Alignment);
                }

                bool isMethod = node is IPCodeSymbolNode;
                long thumbBit = _nodeFactory.Target.Architecture == TargetArchitecture.ARM && isMethod ? 1 : 0;

                if (node is WasmTypeNode signature)
                {
                    RecordMethodSignature(signature);
                }

                if (node is INodeWithTypeSignature codeNode && _nodeFactory.Target.IsWasm)
                {
                    Debug.Assert(codeNode.Signature != null, $"Wasm code node {codeNode.GetType()} has null signature");

                    // Record only information we can get from the MethodDesc here. The actual
                    // body will be emitted by the call to EmitData() at the end
                    // of this loop iteration.
                    RecordMethodDeclaration(codeNode);
                }

                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    Utf8String mangledName = n == node ? currentSymbolName : GetMangledName(n);
                    Debug.Assert(((ulong)thumbBit & (ulong)(uint)n.Offset) == 0);
                    sectionWriter.EmitSymbolDefinition(
                        mangledName,
                        n.Offset + thumbBit,
                        n.Offset == 0 ? nodeContents.Data.Length : 0);

                    _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, (ulong)(sectionWriter.Position + n.Offset), mangledName));

                    Utf8String alternateName = _nodeFactory.GetSymbolAlternateName(n, out bool isHidden);
                    if (!alternateName.IsNull)
                    {
                        Utf8String alternateCName = ExternCName(alternateName);
                        sectionWriter.EmitSymbolDefinition(
                            alternateCName,
                            n.Offset + thumbBit,
                            n.Offset == 0 ? nodeContents.Data.Length : 0,
                            global: !isHidden);

                        if (n is IMethodNode)
                        {
                            // https://github.com/dotnet/runtime/issues/105330: consider exports CFG targets
                            EmitReferencedMethod(alternateCName);
                        }

                        _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, (ulong)(sectionWriter.Position + n.Offset), alternateCName));
                    }

                    if (node.Phase == (int)SortableDependencyNode.ObjectNodePhase.Ordered)
                    {
                        RecordWellKnownSymbol(currentSymbolName, (SortableDependencyNode.ObjectNodeOrder)node.ClassCode);
                    }
                }

                Relocation[] relocations = nodeContents.Relocs;
                if (relocations is not null && UseArm64BranchRangeExtensionThunks &&
                    _nodeFactory.Target.Architecture == TargetArchitecture.ARM64)
                {
                    ArrayBuilder<Relocation> remainingRelocations = default;
                    foreach (Relocation reloc in relocations)
                    {
                        // B relocations can represent intra-method hot/cold transitions where x16 may be live.
                        // BL #0 identifies a direct call with no encoded addend that can safely use a call thunk.
                        if (reloc.RelocType != RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 ||
                            BinaryPrimitives.ReadUInt32LittleEndian(nodeContents.Data.AsSpan(reloc.Offset)) != Arm64BranchLinkInstruction)
                        {
                            remainingRelocations.Add(reloc);
                            continue;
                        }

                        ISymbolNode relocTarget = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, reloc.Target);
                        if (relocTarget is not IArm64BranchThunkTarget)
                        {
                            remainingRelocations.Add(reloc);
                            continue;
                        }

                        Utf8String relocSymbolName = GetMangledName(relocTarget);
                        arm64BranchRegions ??= new Dictionary<int, Arm64BranchRegion>();
                        if (!arm64BranchRegions.TryGetValue(sectionWriter.SectionIndex, out Arm64BranchRegion region))
                        {
                            region = new Arm64BranchRegion(sectionWriter.Position);
                            arm64BranchRegions.Add(sectionWriter.SectionIndex, region);
                        }

                        var branchRelocation = new Arm64BranchRelocation(
                            sectionWriter.SectionIndex,
                            sectionWriter.Position + reloc.Offset,
                            nodeContents.Data,
                            reloc.Offset,
                            relocTarget,
                            relocSymbolName);
                        region.Relocations.Add(branchRelocation);
                        arm64BranchRelocations ??= new List<Arm64BranchRelocation>();
                        arm64BranchRelocations.Add(branchRelocation);
                    }

                    relocations = remainingRelocations.ToArray();
                }

                if (relocations is not null)
                {
                    if (relocations.Length != 0)
                    {
                        blocksToRelocate.Add(new BlockToRelocate(
                            sectionWriter.SectionIndex,
                            sectionWriter.Position,
                            nodeContents.Data,
                            relocations));
                    }

#if DEBUG
                    // Pointer relocs should be aligned at pointer boundaries within the image.
                    // Processing misaligned relocs (especially relocs that straddle page boundaries) can be
                    // expensive on Windows. But: we can't guarantee this on x86, and Wasm doesn't have reloc pointer alignment requirements.
                    if (_nodeFactory.Target.Architecture is not TargetArchitecture.X86 and not TargetArchitecture.Wasm32)
                    {
                        bool hasPointerRelocs = false;
                        foreach (Relocation reloc in nodeContents.Relocs)
                        {
                            if ((reloc.RelocType is RelocType.IMAGE_REL_BASED_DIR64 && _nodeFactory.Target.PointerSize == 8) ||
                                (reloc.RelocType is RelocType.IMAGE_REL_BASED_HIGHLOW && _nodeFactory.Target.PointerSize == 4))
                            {
                                hasPointerRelocs = true;
                                Debug.Assert(reloc.Offset % _nodeFactory.Target.PointerSize == 0);
                            }
                        }
                        Debug.Assert(!hasPointerRelocs || (nodeContents.Alignment % _nodeFactory.Target.PointerSize) == 0);
                    }
#endif
                }

                // Emit unwinding frames and LSDA
                if (_options.HasFlag(ObjectWritingOptions.GenerateUnwindInfo))
                {
                    EmitUnwindInfoForNode(node, currentSymbolName, sectionWriter);
                }

                if (_outputInfoBuilder is not null)
                {
                    var outputNode = new OutputNode(sectionWriter.SectionIndex, checked((ulong)sectionWriter.Position), nodeContents.Data.Length, GetNodeTypeName(node.GetType()));
                    _outputInfoBuilder.AddNode(outputNode, nodeContents.DefinedSymbols[0]);
                    if (nodeContents.Relocs is not null)
                    {
                        foreach (Relocation reloc in nodeContents.Relocs)
                        {
                            RelocType fileReloc = Relocation.GetFileRelocationType(reloc.RelocType);
                            if (fileReloc != RelocType.IMAGE_REL_BASED_ABSOLUTE)
                            {
                                _outputInfoBuilder.AddRelocation(outputNode, fileReloc);
                            }
                        }
                    }
                }

                // Note that this has to be done last as not to advance the section writer position.
                sectionWriter.EmitData(nodeContents.Data);

                if (arm64BranchRegions is not null &&
                    arm64BranchRegions.TryGetValue(sectionWriter.SectionIndex, out Arm64BranchRegion arm64BranchRegion) &&
                    sectionWriter.Position - arm64BranchRegion.StartOffset >= Arm64BranchRegionSize)
                {
                    EmitArm64BranchThunks(sectionWriter, arm64BranchRegion, onlyIfRequired: false);
                    arm64BranchRegion.Reset(sectionWriter.Position);
                }
            }

            if (arm64BranchRegions is not null)
            {
                foreach ((int sectionIndex, Arm64BranchRegion region) in arm64BranchRegions)
                {
                    var sectionWriter = new SectionWriter(this, sectionIndex, _sectionIndexToData[sectionIndex]);
                    EmitArm64BranchThunks(sectionWriter, region, onlyIfRequired: true);
                }
            }

            foreach (ISymbolRangeNode range in symbolRangeNodes)
            {
                ISymbolNode startNode = range.StartNode(_nodeFactory);
                ISymbolNode endNode = range.EndNode(_nodeFactory);

                if (startNode is null != endNode is null)
                {
                    throw new InvalidOperationException("Both or neither of the symbols that define a symbol range must be non-null.");
                }

                if (startNode is null)
                {
                    // Emit empty symbol ranges as an empty symbol at the end of the text section.
                    var writer = GetOrCreateSection(ObjectNodeSection.TextSection);
                    writer.EmitSymbolDefinition(GetMangledName(range));
                    continue;
                }

                startNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, startNode);
                endNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, endNode);
                Utf8String startNodeName = GetMangledName(startNode);
                Utf8String endNodeName = GetMangledName(endNode);

                Utf8String rangeNodeName = GetMangledName(range);

                if (!_definedSymbols.TryGetValue(endNodeName, out SymbolDefinition endSymbol))
                {
                    throw new InvalidOperationException("The end symbol of the symbol range must be emitted into the same object.");
                }

                EmitSymbolRangeDefinition(rangeNodeName, startNodeName, endNodeName, endSymbol);
            }

            if (arm64BranchRelocations is not null)
            {
                foreach (Arm64BranchRelocation branchRelocation in arm64BranchRelocations)
                {
                    bool canReachTarget = CanArm64BranchReach(
                        branchRelocation.SectionIndex,
                        branchRelocation.Offset,
                        branchRelocation.TargetSymbolName,
                        branchRelocation.Target.Offset);

                    Utf8String targetSymbolName;
                    long targetAddend;
                    if (canReachTarget)
                    {
                        targetSymbolName = branchRelocation.TargetSymbolName;
                        targetAddend = branchRelocation.Target.Offset;
                    }
                    else
                    {
                        Debug.Assert(branchRelocation.Thunk.HasValue);
                        targetSymbolName = branchRelocation.Thunk.Value.ThunkSymbolName;
                        targetAddend = 0;
                    }

                    EmitOrResolveRelocation(
                        branchRelocation.SectionIndex,
                        branchRelocation.Offset,
                        branchRelocation.Data.AsSpan(branchRelocation.RelocationOffset),
                        RelocType.IMAGE_REL_BASED_ARM64_BRANCH26,
                        targetSymbolName,
                        targetAddend);

                    if (_options.HasFlag(ObjectWritingOptions.ControlFlowGuard))
                    {
                        HandleControlFlowForRelocation(branchRelocation.Target, branchRelocation.TargetSymbolName);
                    }
                }
            }

            foreach (BlockToRelocate blockToRelocate in blocksToRelocate)
            {
                ArrayBuilder<Relocation> checksumRelocationsBuilder = default;
                foreach (Relocation reloc in blockToRelocate.Relocations)
                {
                    ISymbolNode relocTarget = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, reloc.Target);

                    if (reloc.RelocType == RelocType.IMAGE_REL_FILE_CHECKSUM_CALLBACK)
                    {
                        // Checksum relocations don't get emitted into the image.
                        // We manually proces them after we do all other object emission.
                        checksumRelocationsBuilder.Add(reloc);
                        continue;
                    }

                    Utf8String relocSymbolName = GetMangledName(relocTarget);

                    EmitOrResolveRelocation(
                        blockToRelocate.SectionIndex,
                        blockToRelocate.Offset + reloc.Offset,
                        blockToRelocate.Data.AsSpan(reloc.Offset),
                        reloc.RelocType,
                        relocSymbolName,
                        relocTarget.Offset);

                    if (_options.HasFlag(ObjectWritingOptions.ControlFlowGuard))
                    {
                        HandleControlFlowForRelocation(relocTarget, relocSymbolName);
                    }
                }
                checksumRelocations.Add(new ChecksumsToCalculate(blockToRelocate.SectionIndex, blockToRelocate.Offset, checksumRelocationsBuilder.ToArray()));
            }
            blocksToRelocate.Clear();

            EmitSectionsAndLayout();

            if (_options.HasFlag(ObjectWritingOptions.GenerateDebugInfo))
            {
                EmitDebugInfo(nodes, logger);
            }

            EmitSymbolTable(_definedSymbols, GetUndefinedSymbols());

            int relocSectionIndex = 0;
            foreach (List<SymbolicRelocation> relocationList in _sectionIndexToRelocations)
            {
                EmitRelocations(relocSectionIndex, relocationList);
                relocSectionIndex++;
            }

            EmitObjectFile(outputFileStream);

            if (checksumRelocations.Count > 0)
            {
                EmitChecksums(outputFileStream, checksumRelocations);
            }

            if (_outputInfoBuilder is not null)
            {
                foreach (var outputSection in _outputSectionLayout)
                {
                    _outputInfoBuilder.AddSection(outputSection);
                }
            }
        }

        private protected virtual void RecordMethodDeclaration(INodeWithTypeSignature node)
        {
            Debug.Assert(LayoutMode == CodeDataLayout.Separate);
        }

        private protected virtual void RecordMethodSignature(WasmTypeNode signature)
        {
            Debug.Assert(LayoutMode == CodeDataLayout.Separate);
        }

        private protected virtual void RecordWellKnownSymbol(Utf8String currentSymbolName, SortableDependencyNode.ObjectNodeOrder classCode)
        {
        }

        private protected virtual void EmitSymbolRangeDefinition(Utf8String rangeNodeName, Utf8String startNodeName, Utf8String endNodeName, SymbolDefinition endSymbol)
        {
            if (!_definedSymbols.TryGetValue(startNodeName, out var startSymbol))
            {
                throw new InvalidOperationException("The start symbol of the symbol range must be emitted into the same object.");
            }

            if (startSymbol.SectionIndex != endSymbol.SectionIndex)
            {
                throw new InvalidOperationException("The symbols that define a symbol range must be in the same section.");
            }
            // Don't use SectionWriter here as it emits symbols relative to the current writing position.
            EmitSymbolDefinition(startSymbol.SectionIndex, rangeNodeName, startSymbol.Value, checked((int)(endSymbol.Value - startSymbol.Value + endSymbol.Size)));
        }

        private static string GetNodeTypeName(Type nodeType)
        {
            string name = nodeType.ToString();
            int firstGeneric = name.IndexOf('[');

            if (firstGeneric < 0)
            {
                firstGeneric = name.Length;
            }

            int lastDot = name.LastIndexOf('.', firstGeneric - 1, firstGeneric);

            if (lastDot > 0)
            {
                name = name.Substring(lastDot + 1);
            }

            return name;
        }

        private void EmitArm64BranchThunks(SectionWriter sectionWriter, Arm64BranchRegion region, bool onlyIfRequired)
        {
            if (region.Relocations.Count == 0)
                return;

            Dictionary<ISymbolNode, Arm64BranchThunk> thunks = new Dictionary<ISymbolNode, Arm64BranchThunk>();
            foreach (Arm64BranchRelocation branchRelocation in region.Relocations)
            {
                if (onlyIfRequired && CanArm64BranchReach(
                    branchRelocation.SectionIndex,
                    branchRelocation.Offset,
                    branchRelocation.TargetSymbolName,
                    branchRelocation.Target.Offset))
                {
                    continue;
                }

                if (!thunks.ContainsKey(branchRelocation.Target))
                {
                    Utf8String thunkSymbolName = new Utf8StringBuilder()
                        .Append("__arm64_branch_thunk_"u8)
                        .Append(_arm64BranchThunkId++)
                        .ToUtf8String();
                    thunks.Add(
                        branchRelocation.Target,
                        new Arm64BranchThunk(
                            thunkSymbolName,
                            branchRelocation.TargetSymbolName,
                            branchRelocation.Target.Offset));
                }
            }

            if (thunks.Count == 0)
                return;

            sectionWriter.EmitAlignment(sizeof(uint));
            foreach (Arm64BranchThunk thunk in thunks.Values)
            {
                const int ThunkSize = 3 * sizeof(uint);
                const uint AdrpX16Instruction = 0x90000010;
                const uint AddX16Instruction = 0x91000210;
                const uint BranchX16Instruction = 0xD61F0200;
                byte[] thunkData = new byte[ThunkSize];

                BinaryPrimitives.WriteUInt32LittleEndian(thunkData, AdrpX16Instruction);
                BinaryPrimitives.WriteUInt32LittleEndian(thunkData.AsSpan(sizeof(uint)), AddX16Instruction);
                BinaryPrimitives.WriteUInt32LittleEndian(thunkData.AsSpan(2 * sizeof(uint)), BranchX16Instruction);

                sectionWriter.EmitSymbolDefinition(thunk.ThunkSymbolName, size: ThunkSize);
                EmitRelocation(
                    sectionWriter.SectionIndex,
                    sectionWriter.Position,
                    thunkData,
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21,
                    thunk.TargetSymbolName,
                    thunk.TargetAddend);
                EmitRelocation(
                    sectionWriter.SectionIndex,
                    sectionWriter.Position + sizeof(uint),
                    thunkData.AsSpan(sizeof(uint)),
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A,
                    thunk.TargetSymbolName,
                    thunk.TargetAddend);
                sectionWriter.EmitData(thunkData);
            }

            foreach (Arm64BranchRelocation branchRelocation in region.Relocations)
            {
                if (thunks.TryGetValue(branchRelocation.Target, out Arm64BranchThunk thunk))
                {
                    branchRelocation.Thunk = thunk;
                }
            }
        }

        private bool CanArm64BranchReach(int sectionIndex, long offset, Utf8String targetSymbolName, long targetAddend)
        {
            return _definedSymbols.TryGetValue(targetSymbolName, out SymbolDefinition definedSymbol) &&
                definedSymbol.SectionIndex == sectionIndex &&
                Relocation.FitsInArm64Rel28(definedSymbol.Value + targetAddend - offset);
        }

        private sealed class Arm64BranchRegion
        {
            public Arm64BranchRegion(long startOffset)
            {
                StartOffset = startOffset;
            }

            public long StartOffset { get; private set; }
            public List<Arm64BranchRelocation> Relocations { get; } = new List<Arm64BranchRelocation>();

            public void Reset(long startOffset)
            {
                StartOffset = startOffset;
                Relocations.Clear();
            }
        }

        private sealed class Arm64BranchRelocation
        {
            public Arm64BranchRelocation(
                int sectionIndex,
                long offset,
                byte[] data,
                int relocationOffset,
                ISymbolNode target,
                Utf8String targetSymbolName)
            {
                SectionIndex = sectionIndex;
                Offset = offset;
                Data = data;
                RelocationOffset = relocationOffset;
                Target = target;
                TargetSymbolName = targetSymbolName;
            }

            public int SectionIndex { get; }
            public long Offset { get; }
            public byte[] Data { get; }
            public int RelocationOffset { get; }
            public ISymbolNode Target { get; }
            public Utf8String TargetSymbolName { get; }
            public Arm64BranchThunk? Thunk { get; set; }
        }

        private readonly struct Arm64BranchThunk
        {
            public Arm64BranchThunk(Utf8String thunkSymbolName, Utf8String targetSymbolName, long targetAddend)
            {
                ThunkSymbolName = thunkSymbolName;
                TargetSymbolName = targetSymbolName;
                TargetAddend = targetAddend;
            }

            public Utf8String ThunkSymbolName { get; }
            public Utf8String TargetSymbolName { get; }
            public long TargetAddend { get; }
        }

        private void EmitChecksums(Stream outputFileStream, List<ChecksumsToCalculate> checksumRelocations)
        {
            // Defer writing the computed values until all checksums are computed so each one is
            // calculated over the original image and not a value written by an earlier checksum.
            List<(long Offset, byte[] Value)> pendingWrites = ComputeChecksums(outputFileStream, checksumRelocations);

            foreach ((long offset, byte[] value) in pendingWrites)
            {
                outputFileStream.Seek(offset, SeekOrigin.Begin);
                outputFileStream.Write(value);
            }
        }

        private protected virtual List<(long Offset, byte[] Value)> ComputeChecksums(Stream outputFileStream, List<ChecksumsToCalculate> checksumRelocations)
        {
            List<(long Offset, byte[] Value)> pendingWrites = [];
            foreach (var block in checksumRelocations)
            {
                foreach (var reloc in block.ChecksumRelocations)
                {
                    IChecksumNode checksum = (IChecksumNode)reloc.Target;

                    byte[] checksumValue = new byte[checksum.ChecksumSize];
                    outputFileStream.Seek(0, SeekOrigin.Begin);
                    checksum.EmitChecksum(outputFileStream, checksumValue);

                    var checksumOffset = (long)_outputSectionLayout[block.SectionIndex].FilePosition + block.Offset + reloc.Offset;
                    pendingWrites.Add((checksumOffset, checksumValue));
                }
            }

            return pendingWrites;
        }

        partial void HandleControlFlowForRelocation(ISymbolNode relocTarget, Utf8String relocSymbolName);

        partial void PrepareForUnwindInfo();

        partial void EmitUnwindInfoForNode(ObjectNode node, Utf8String currentSymbolName, SectionWriter sectionWriter);

        protected static ReadOnlySpan<byte> FormatUtf8Int(Span<byte> buffer, int number)
        {
            bool b = number.TryFormat(buffer, out int bytesWritten);
            Debug.Assert(b);
            return buffer.Slice(0, bytesWritten);
        }

        private struct ProgressReporter
        {
            private readonly Logger _logger;
            private readonly int _total;
            private int _current;
            private int _lastReportedStep;

            // Will report progress every (100 / 10) = 10%
            private const int Steps = 10;

            public ProgressReporter(Logger logger, int total)
            {
                _logger = logger;
                _total = total;
                _current = 0;
                _lastReportedStep = 0;
            }

            public void LogProgress()
            {
                _current++;

                int step = (_current * Steps) / _total;
                if (step > _lastReportedStep)
                {
                    _logger.LogMessage($"{step * (100 / Steps)}%...");
                    _lastReportedStep = step;
                }
            }
        }
    }
}
