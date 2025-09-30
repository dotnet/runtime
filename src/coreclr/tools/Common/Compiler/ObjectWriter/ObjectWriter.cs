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
        private protected sealed record SymbolDefinition(int SectionIndex, long Value, int Size = 0, bool Global = false);
        protected sealed record SymbolicRelocation(long Offset, RelocType Type, string SymbolName, long Addend = 0);
        private sealed record BlockToRelocate(int SectionIndex, long Offset, byte[] Data, Relocation[] Relocations);

        private protected readonly NodeFactory _nodeFactory;
        private protected readonly ObjectWritingOptions _options;
        private protected readonly OutputInfoBuilder _outputInfoBuilder;
        private readonly bool _isSingleFileCompilation;

        private readonly Dictionary<ISymbolNode, string> _mangledNameMap = new();

        private readonly byte _insPaddingByte;

        // Standard sections
        private readonly Dictionary<string, int> _sectionNameToSectionIndex = new(StringComparer.Ordinal);
        private readonly List<SectionData> _sectionIndexToData = new();
        private readonly List<List<SymbolicRelocation>> _sectionIndexToRelocations = new();
        private protected readonly List<OutputSection> _outputSectionLayout = [];

        // Symbol table
        private readonly Dictionary<string, SymbolDefinition> _definedSymbols = new(StringComparer.Ordinal);

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

        private protected abstract void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream);

        protected internal abstract void UpdateSectionAlignment(int sectionIndex, int alignment);

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
        private protected SectionWriter GetOrCreateSection(ObjectNodeSection section, string comdatName = null, string symbolName = null)
        {
            int sectionIndex;
            SectionData sectionData;

            if (comdatName is not null || !_sectionNameToSectionIndex.TryGetValue(section.Name, out sectionIndex))
            {
                sectionData = new SectionData(section.Type == SectionType.Executable ? _insPaddingByte : (byte)0);
                sectionIndex = _sectionIndexToData.Count;
                CreateSection(section, comdatName, symbolName, sectionIndex, sectionData.GetReadStream());
                _sectionIndexToData.Add(sectionData);
                _sectionIndexToRelocations.Add(new());
                if (comdatName is null)
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
            string symbolName,
            long addend)
        {
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
                    long maskThumbBitOut = relocType is IMAGE_REL_BASED_THUMB_BRANCH24 or IMAGE_REL_BASED_THUMB_MOV32_PCREL ? 1 : 0;
                    long maskThumbBitIn = relocType is IMAGE_REL_BASED_THUMB_MOV32_PCREL ? 1 : 0;
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
            string symbolName,
            long addend)
        {
            _sectionIndexToRelocations[sectionIndex].Add(new SymbolicRelocation(offset, relocType, symbolName, addend));
        }

        private protected bool SectionHasRelocations(int sectionIndex)
        {
            return _sectionIndexToRelocations[sectionIndex].Count > 0;
        }

        private protected virtual void EmitReferencedMethod(string symbolName) { }

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
            string symbolName,
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
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols);

        private protected virtual string ExternCName(string name) => name;

        private protected string GetMangledName(ISymbolNode symbolNode)
        {
            string symbolName;

            if (!_mangledNameMap.TryGetValue(symbolNode, out symbolName))
            {
                symbolName = ExternCName(symbolNode.GetMangledName(_nodeFactory.NameMangler));
                _mangledNameMap.Add(symbolNode, symbolName);
            }

            return symbolName;
        }

        private protected virtual void EmitSectionsAndLayout()
        {
        }

        private protected abstract void EmitObjectFile(Stream outputFileStream);

        partial void EmitDebugInfo(IReadOnlyCollection<DependencyNode> nodes, Logger logger);

        private SortedSet<string> GetUndefinedSymbols()
        {
            SortedSet<string> undefinedSymbolSet = new SortedSet<string>(StringComparer.Ordinal);
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

        public void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
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
            foreach (DependencyNode depNode in nodes)
            {
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

                string currentSymbolName = null;
                if (symbolNode != null)
                {
                    currentSymbolName = GetMangledName(symbolNode);
                }

                ObjectNodeSection section = node.GetSection(_nodeFactory);
                SectionWriter sectionWriter = ShouldShareSymbol(node, section) ?
                    GetOrCreateSection(section, currentSymbolName, currentSymbolName) :
                    GetOrCreateSection(section);

                sectionWriter.EmitAlignment(nodeContents.Alignment);

                bool isMethod = node is IMethodBodyNode or AssemblyStubNode;
#if !READYTORUN
                bool recordSize = isMethod;
#else
                bool recordSize = true;
#endif
                long thumbBit = _nodeFactory.Target.Architecture == TargetArchitecture.ARM && isMethod ? 1 : 0;
                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    string mangledName = n == node ? currentSymbolName : GetMangledName(n);
                    sectionWriter.EmitSymbolDefinition(
                        mangledName,
                        n.Offset + thumbBit,
                        n.Offset == 0 && recordSize ? nodeContents.Data.Length : 0);

                    _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, n.Offset, mangledName));

                    if (_nodeFactory.GetSymbolAlternateName(n, out bool isHidden) is string alternateName)
                    {
                        string alternateCName = ExternCName(alternateName);
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

                        _outputInfoBuilder?.AddSymbol(new OutputSymbol(sectionWriter.SectionIndex, n.Offset, alternateCName));
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
                    var outputNode = new OutputNode(sectionWriter.SectionIndex, checked((int)sectionWriter.Position), nodeContents.Data.Length, currentSymbolName);
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

                // Write the data. Note that this has to be done last as not to advance
                // the section writer position.
                sectionWriter.EmitData(nodeContents.Data);
            }

            foreach (ISymbolRangeNode range in symbolRangeNodes)
            {
                ISymbolNode startNode = range.StartNode(_nodeFactory);
                ISymbolNode endNode = range.EndNode(_nodeFactory);

#if !READYTORUN
                startNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, startNode);
                endNode = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, endNode);
#endif
                string startNodeName = GetMangledName(startNode);
                string endNodeName = GetMangledName(endNode);

                string rangeNodeName = GetMangledName(range);

                if (!_definedSymbols.TryGetValue(startNodeName, out var startSymbol)
                    || !_definedSymbols.TryGetValue(endNodeName, out var endSymbol))
                {
                    throw new InvalidOperationException("The symbols defined by a symbol range must be emitted into the same object.");
                }

                if (startSymbol.SectionIndex != endSymbol.SectionIndex)
                {
                    throw new InvalidOperationException("The symbols that define a symbol range must be in the same section.");
                }

                SectionWriter sectionWriter = new SectionWriter(
                    this,
                    startSymbol.SectionIndex,
                    _sectionIndexToData[startSymbol.SectionIndex]);

                sectionWriter.EmitSymbolDefinition(rangeNodeName, startSymbol.Value, checked((int)(endSymbol.Value - startSymbol.Value)));
            }

            foreach (BlockToRelocate blockToRelocate in blocksToRelocate)
            {
                foreach (Relocation reloc in blockToRelocate.Relocations)
                {
#if READYTORUN
                    ISymbolNode relocTarget = reloc.Target;
#else
                    ISymbolNode relocTarget = _nodeFactory.ObjectInterner.GetDeduplicatedSymbol(_nodeFactory, reloc.Target);
#endif

                    string relocSymbolName = GetMangledName(relocTarget);

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

            if (_outputInfoBuilder is not null)
            {
                foreach (var outputSection in _outputSectionLayout)
                {
                    _outputInfoBuilder.AddSection(outputSection);
                }
            }
        }

        partial void HandleControlFlowForRelocation(ISymbolNode relocTarget, string relocSymbolName);

        partial void PrepareForUnwindInfo();

        partial void EmitUnwindInfoForNode(ObjectNode node, string currentSymbolName, SectionWriter sectionWriter);

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
