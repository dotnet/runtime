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
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;
using static ILCompiler.DependencyAnalysis.ObjectNode;
using static ILCompiler.DependencyAnalysis.RelocType;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    public abstract partial class ObjectWriter
    {
        protected virtual CodeDataLayoutMode LayoutMode => CodeDataLayoutMode.Unified;
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

        private readonly byte? _insPaddingByte;

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

            // Padding byte for code sections (NOP for x86/x64) and null for Wasm
            _insPaddingByte = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => 0x90,
                TargetArchitecture.X64 => 0x90,
                TargetArchitecture.Wasm32 => null,
                _ => 0
            };
        }
        private protected virtual bool UsesSubsectionsViaSymbols => false;

        private protected abstract void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream);

        protected internal abstract void UpdateSectionAlignment(int sectionIndex, int alignment);

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

            if (!comdatName.IsNull || !_sectionNameToSectionIndex.TryGetValue(section.Name, out sectionIndex))
            {
                if (_insPaddingByte.HasValue)
                {
                    sectionData = new SectionData(section.Type == SectionType.Executable ? _insPaddingByte.Value : (byte)0);
                }
                else
                {
                    sectionData = new SectionData();
                }

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
            Logger logger,
            int sectionIndex,
            long offset,
            Span<byte> data,
            RelocType relocType,
            Utf8String symbolName,
            long addend)
        {
            if (_nodeFactory.Target.IsWasm)
            {
                logger.LogMessage($"Emitting relocation in section {sectionIndex} of type {relocType} at offset {offset}");
                return;
            }

            if (!UsesSubsectionsViaSymbols &&
                relocType is IMAGE_REL_BASED_REL32 or IMAGE_REL_BASED_RELPTR32 or IMAGE_REL_BASED_ARM64_BRANCH26
                or IMAGE_REL_BASED_THUMB_BRANCH24 or IMAGE_REL_BASED_THUMB_MOV32_PCREL &&
                _definedSymbols.TryGetValue(symbolName, out SymbolDefinition definedSymbol) &&
                definedSymbol.SectionIndex == sectionIndex)
            {
                // Resolve the relocation to already defined symbol and write it into data
                fixed (byte *pData = data)
                {
                    // RyuJIT generates the Thumb bit in the addend and we also get it from
                    // the symbol value. The AAELF ABI specification defines the R_ARM_THM_JUMP24
                    // and R_ARM_THM_MOVW_PREL_NC relocations using the formula ((S + A) | T) – P.
                    // The thumb bit is thus supposed to be only added once.
                    // For R_ARM_THM_JUMP24 the thumb bit cannot be encoded, so mask it out.
                    //
                    // R2R doesn't use add the thumb bit to the symbol value, so we don't need to do this here.
#if !READYTORUN
                    long maskThumbBitOut = relocType is IMAGE_REL_BASED_THUMB_BRANCH24 or IMAGE_REL_BASED_THUMB_MOV32_PCREL ? 1 : 0;
                    long maskThumbBitIn = relocType is IMAGE_REL_BASED_THUMB_MOV32_PCREL ? 1 : 0;
#else
                    long maskThumbBitOut = 0;
                    long maskThumbBitIn = 0;
#endif
                    long adjustedAddend = addend;

                    adjustedAddend -= relocType switch
                    {
                        IMAGE_REL_BASED_REL32 => 4,
                        IMAGE_REL_BASED_THUMB_BRANCH24 => 4,
                        IMAGE_REL_BASED_THUMB_MOV32_PCREL => 12,
                        _ => 0
                    };

                    adjustedAddend += definedSymbol.Value & ~maskThumbBitOut;
                    adjustedAddend += Relocation.ReadValue(relocType, (void*)pData);
                    adjustedAddend |= definedSymbol.Value & maskThumbBitIn;
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
                    Relocation.WriteValue(relocType, (void*)pData, definedSymbol.Size);
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

        private protected abstract void EmitObjectFile(Stream outputFileStream, Logger logger = null);

        partial void EmitDebugInfo(IReadOnlyCollection<DependencyNode> nodes, Logger logger);

        private SortedSet<Utf8String> GetUndefinedSymbols()
        {
            SortedSet<Utf8String> undefinedSymbolSet = new SortedSet<Utf8String>();
            foreach (var relocationList in _sectionIndexToRelocations)
            foreach (var symbolicRelocation in relocationList)
            {
                if (!_definedSymbols.ContainsKey(symbolicRelocation.SymbolName))
                {
                    undefinedSymbolSet.Add(symbolicRelocation.SymbolName);
                }
            }
            return undefinedSymbolSet;
        }


        public virtual void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            logger.LogMessage("Starting object file emission...");
            // Pre-create some of the sections
            GetOrCreateSection(ObjectNodeSection.TextSection);
            if (_nodeFactory.Target.OperatingSystem == TargetOS.Windows)
            {
                GetOrCreateSection(ObjectNodeSection.ManagedCodeWindowsContentSection);
            }
            else if (_nodeFactory.Target.OperatingSystem != TargetOS.Browser)
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
            foreach (DependencyNode depNode in nodes)
            {
                logger.LogMessage("--------------------------------------------------------------");
                logger.LogMessage($"Processing dependency node of type {depNode.GetType()}");
                logger.LogMessage("--------------------------------------------------------------");
                if (depNode is ISymbolRangeNode symbolRange)
                {
                    logger.LogMessage($"Deferring emission of symbol range node {GetMangledName(symbolRange)}");
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

#if !READYTORUN
                ISymbolNode deduplicatedSymbolNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, symbolNode);
                if (deduplicatedSymbolNode != symbolNode)
                {
                    dumper?.ReportFoldedNode(_nodeFactory, node, deduplicatedSymbolNode);
                    continue;
                }
#endif

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

                bool isMethod = node is IMethodBodyNode or AssemblyStubNode;
#if !READYTORUN
                bool recordSize = isMethod;
                long thumbBit = _nodeFactory.Target.Architecture == TargetArchitecture.ARM && isMethod ? 1 : 0;
#else
                bool recordSize = true;
                // R2R records the thumb bit in the addend when needed, so we don't have to do it here.
                long thumbBit = 0;
#endif

                // TODO-WASM: handle AssemblyStub case here
                if (node is IMethodBodyNode methodNode && LayoutMode is CodeDataLayoutMode.Separate)
                {
                    RecordMethod((ISymbolDefinitionNode)node, methodNode.Method, nodeContents);
                }

                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    logger.LogMessage($"Emitting defined symbol {GetMangledName(n)} at offset {n.Offset} in section {section.Name}");
                    Utf8String mangledName = n == node ? currentSymbolName : GetMangledName(n);
                    sectionWriter.EmitSymbolDefinition(
                        mangledName,
                        n.Offset + thumbBit,
                        n.Offset == 0 && recordSize ? nodeContents.Data.Length : 0);

                    _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, (ulong)(sectionWriter.Position + n.Offset), mangledName));

                    Utf8String alternateName = _nodeFactory.GetSymbolAlternateName(n, out bool isHidden);
                    if (!alternateName.IsNull)
                    {
                        Utf8String alternateCName = ExternCName(alternateName);
                        sectionWriter.EmitSymbolDefinition(
                            alternateCName,
                            n.Offset + thumbBit,
                            n.Offset == 0 && recordSize ? nodeContents.Data.Length : 0,
                            global: !isHidden);

                        if (n is IMethodNode)
                        {
                            // https://github.com/dotnet/runtime/issues/105330: consider exports CFG targets
                            EmitReferencedMethod(alternateCName);
                        }

                        _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, (ulong)(sectionWriter.Position + n.Offset), alternateCName));
                    }
                }

                if (nodeContents.Relocs is not null)
                {
                    blocksToRelocate.Add(new BlockToRelocate(
                        sectionWriter.SectionIndex,
                        sectionWriter.Position,
                        nodeContents.Data,
                        nodeContents.Relocs));

#if DEBUG
                    // Pointer relocs should be aligned at pointer boundaries within the image.
                    // Processing misaligned relocs (especially relocs that straddle page boundaries) can be
                    // expensive on Windows. But: we can't guarantee this on x86.
                    if (_nodeFactory.Target.Architecture != TargetArchitecture.X86)
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

                // Write the data if:
                // 1. We are in unified code/data layout mode so separating code and data nodes doesn't matter, OR
                // 2. We are writing non-text nodes
                // Note that this has to be done last as not to advance the section writer position. 
                if (LayoutMode == CodeDataLayoutMode.Unified || node.GetSection(_nodeFactory) != ObjectNodeSection.TextSection)
                {
                    sectionWriter.EmitData(nodeContents.Data);
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

#if !READYTORUN
                startNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, startNode);
                endNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, endNode);
#endif
                Utf8String startNodeName = GetMangledName(startNode);
                Utf8String endNodeName = GetMangledName(endNode);

                Utf8String rangeNodeName = GetMangledName(range);

                if (!_definedSymbols.TryGetValue(endNodeName, out SymbolDefinition endSymbol))
                {
                    throw new InvalidOperationException("The end symbol of the symbol range must be emitted into the same object.");
                }

                EmitSymbolRangeDefinition(rangeNodeName, startNodeName, endNodeName, endSymbol);
            }

            foreach (BlockToRelocate blockToRelocate in blocksToRelocate)
            {
                ArrayBuilder<Relocation> checksumRelocationsBuilder = default;
                foreach (Relocation reloc in blockToRelocate.Relocations)
                {
#if READYTORUN
                    ISymbolNode relocTarget = reloc.Target;
#else
                    ISymbolNode relocTarget = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, reloc.Target);
#endif

                    if (reloc.RelocType == RelocType.IMAGE_REL_FILE_CHECKSUM_CALLBACK)
                    {
                        // Checksum relocations don't get emitted into the image.
                        // We manually proces them after we do all other object emission.
                        checksumRelocationsBuilder.Add(reloc);
                        continue;
                    }

                    Utf8String relocSymbolName = GetMangledName(relocTarget);

                    EmitOrResolveRelocation(
                        logger,
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

            EmitObjectFile(outputFileStream, logger);

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

        private protected virtual void RecordMethod(ISymbolDefinitionNode node, MethodDesc desc, ObjectData methodData)
        {
            if (LayoutMode != CodeDataLayoutMode.Separate)
            {
                throw new InvalidOperationException($"RecordMethod() must only be called on platforms with separated code and data, arch = {_nodeFactory.Target.Architecture}");
            }
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

        private void EmitChecksums(Stream outputFileStream, List<ChecksumsToCalculate> checksumRelocations)
        {
            MemoryStream originalOutputStream = new();
            outputFileStream.Seek(0, SeekOrigin.Begin);
            outputFileStream.CopyTo(originalOutputStream);
            byte[] originalOutput = originalOutputStream.ToArray();
            EmitChecksumsForObject(outputFileStream, checksumRelocations, originalOutput);
        }

        private protected virtual void EmitChecksumsForObject(Stream outputFileStream, List<ChecksumsToCalculate> checksumRelocations, ReadOnlySpan<byte> originalOutput)
        {
            foreach (var block in checksumRelocations)
            {
                foreach (var reloc in block.ChecksumRelocations)
                {
                    IChecksumNode checksum = (IChecksumNode)reloc.Target;

                    byte[] checksumValue = new byte[checksum.ChecksumSize];
                    checksum.EmitChecksum(originalOutput, checksumValue);

                    var checksumOffset = (long)_outputSectionLayout[block.SectionIndex].FilePosition + block.Offset + reloc.Offset;
                    outputFileStream.Seek(checksumOffset, SeekOrigin.Begin);
                    outputFileStream.Write(checksumValue);
                }
            }
        }

        partial void HandleControlFlowForRelocation(ISymbolNode relocTarget, Utf8String relocSymbolName);

        partial void PrepareForUnwindInfo();

        partial void EmitUnwindInfoForNode(ObjectNode node, Utf8String currentSymbolName, SectionWriter sectionWriter);

        public static void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, NodeFactory factory, ObjectWritingOptions options, IObjectDumper dumper, Logger logger)
        {
            var stopwatch = Stopwatch.StartNew();

            ObjectWriter objectWriter =
                factory.Target.IsApplePlatform ? new MachObjectWriter(factory, options) :
                factory.Target.OperatingSystem == TargetOS.Windows ? new CoffObjectWriter(factory, options) :
                new ElfObjectWriter(factory, options);

            using Stream outputFileStream = new FileStream(objectFilePath, FileMode.Create);
            objectWriter.EmitObject(outputFileStream, nodes, dumper, logger);

            stopwatch.Stop();
            if (logger.IsVerbose)
                logger.LogMessage($"Done writing object file in {stopwatch.Elapsed}");
        }

        protected static ReadOnlySpan<byte> FormatUtf8Int(Span<byte> buffer, int number)
        {
            bool b = number.TryFormat(buffer, out int bytesWritten);
            Debug.Assert(b);
            return buffer.Slice(0, bytesWritten);
        }

        private struct ProgressReporter
        {
            private readonly Logger _logger;
            private readonly int _increment;
            private int _current;

            // Will report progress every (100 / 10) = 10%
            private const int Steps = 10;

            public ProgressReporter(Logger logger, int total)
            {
                _logger = logger;
                _increment = total / Steps;
                _current = 0;
            }

            public void LogProgress()
            {
                _current++;

                int adjusted = _current + Steps - 1;
                if ((adjusted % _increment) == 0)
                {
                    _logger.LogMessage($"{(adjusted / _increment) * (100 / Steps)}%...");
                }
            }
        }
    }
}
