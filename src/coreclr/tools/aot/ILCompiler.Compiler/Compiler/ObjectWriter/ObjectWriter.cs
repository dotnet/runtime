// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    public abstract class ObjectWriter : IDisposable
    {
        protected sealed record SymbolDefinition(int SectionIndex, long Value, int Size = 0, bool Global = false);
        protected sealed record SymbolicRelocation(long Offset, RelocType Type, string SymbolName, long Addend = 0);

        protected readonly NodeFactory _nodeFactory;
        protected readonly ObjectWritingOptions _options;
        private readonly bool _isSingleFileCompilation;

        private readonly Dictionary<ISymbolNode, string> _mangledNameMap = new();

        private readonly byte _insPaddingByte;

        // Standard sections
        private readonly Dictionary<string, int> _sectionNameToSectionIndex = new(StringComparer.Ordinal);
        private readonly List<ObjectWriterStream> _sectionIndexToStream = new();
        private readonly List<List<SymbolicRelocation>> _sectionIndexToRelocations = new();

        // Symbol table
        private readonly Dictionary<string, SymbolDefinition> _definedSymbols = new(StringComparer.Ordinal);

        // Debugging
        private UserDefinedTypeDescriptor _userDefinedTypeDescriptor;

        protected ObjectWriter(NodeFactory factory, ObjectWritingOptions options)
        {
            _nodeFactory = factory;
            _options = options;
            _isSingleFileCompilation = _nodeFactory.CompilationModuleGroup.IsSingleFileCompilation;

            // Padding byte for code sections (NOP for x86/x64)
            _insPaddingByte = factory.Target.Architecture switch
            {
                TargetArchitecture.X86 => 0x90,
                TargetArchitecture.X64 => 0x90,
                _ => 0
            };
        }

        public void Dispose()
        {
            // Close all the streams
            foreach (ObjectWriterStream sectionStream in _sectionIndexToStream)
            {
                sectionStream.Close();
            }
        }

        protected abstract void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream);

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
        protected SectionWriter GetOrCreateSection(ObjectNodeSection section, string comdatName = null, string symbolName = null)
        {
            int sectionIndex;
            ObjectWriterStream sectionStream;

            if (comdatName is not null || !_sectionNameToSectionIndex.TryGetValue(section.Name, out sectionIndex))
            {
                sectionStream = new ObjectWriterStream(section.Type == SectionType.Executable ? _insPaddingByte : (byte)0);
                sectionIndex = _sectionIndexToStream.Count;
                CreateSection(section, comdatName, symbolName, sectionStream);
                _sectionIndexToStream.Add(sectionStream);
                _sectionIndexToRelocations.Add(new());
                if (comdatName is null)
                {
                    _sectionNameToSectionIndex.Add(section.Name, sectionIndex);
                }
            }
            else
            {
                sectionStream = _sectionIndexToStream[sectionIndex];
            }

            return new SectionWriter(
                this,
                sectionIndex,
                sectionStream);
        }

        protected bool ShouldShareSymbol(ObjectNode node)
        {
            if (_nodeFactory.Target.IsOSXLike)
                return false;

            return ShouldShareSymbol(node, node.GetSection(_nodeFactory));
        }

        protected bool ShouldShareSymbol(ObjectNode node, ObjectNodeSection section)
        {
            if (_nodeFactory.Target.IsOSXLike)
                return false;

            // Foldable sections are always COMDATs
            if (section == ObjectNodeSection.FoldableManagedCodeUnixContentSection ||
                section == ObjectNodeSection.FoldableManagedCodeWindowsContentSection ||
                section == ObjectNodeSection.FoldableReadOnlyDataSection)
                return true;

            if (_isSingleFileCompilation)
                return false;

            if (node is not ISymbolNode)
                return false;

            // These intentionally clash with one another, but are merged with linker directives so should not be COMDAT folded
            if (node is ModulesSectionNode)
                return false;

            return true;
        }

        protected static ObjectNodeSection GetSharedSection(ObjectNodeSection section, string key)
        {
            string standardSectionPrefix = "";
            if (section.IsStandardSection)
                standardSectionPrefix = ".";

            return new ObjectNodeSection(standardSectionPrefix + section.Name, section.Type, key);
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

        protected bool SectionHasRelocations(int sectionIndex)
        {
            return _sectionIndexToRelocations[sectionIndex].Count > 0;
        }

        protected virtual void EmitReferencedMethod(string symbolName) { }

        /// <summary>
        /// Emit symbolic relocations into object file as format specific
        /// relocations.
        /// </summary>
        /// <remarks>
        /// This methods is guaranteed to run after <see cref="EmitSymbolTable" />.
        /// </remarks>
        protected abstract void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList);

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
        protected abstract void EmitSymbolTable(
            IDictionary<string, SymbolDefinition> definedSymbols,
            SortedSet<string> undefinedSymbols);

        protected virtual string ExternCName(string name) => name;

        protected string GetMangledName(ISymbolNode symbolNode)
        {
            string symbolName;

            if (!_mangledNameMap.TryGetValue(symbolNode, out symbolName))
            {
                symbolName = ExternCName(symbolNode.GetMangledName(_nodeFactory.NameMangler));
                _mangledNameMap.Add(symbolNode, symbolName);
            }

            return symbolName;
        }

        protected abstract void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName);

        protected uint GetVarTypeIndex(bool isStateMachineMoveNextMethod, DebugVarInfoMetadata debugVar)
        {
            uint typeIndex;
            try
            {
                if (isStateMachineMoveNextMethod && debugVar.DebugVarInfo.VarNumber == 0)
                {
                    typeIndex = _userDefinedTypeDescriptor.GetStateMachineThisVariableTypeIndex(debugVar.Type);
                    // FIXME
                    // varName = "locals";
                }
                else
                {
                    typeIndex = _userDefinedTypeDescriptor.GetVariableTypeIndex(debugVar.Type);
                }
            }
            catch (TypeSystemException)
            {
                typeIndex = 0; // T_NOTYPE
                // FIXME
                // Debug.Fail();
            }
            return typeIndex;
        }

        protected abstract void EmitSectionsAndLayout();

        protected abstract void EmitObjectFile(string objectFilePath);

        protected abstract void CreateEhSections();

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

        protected abstract ITypesDebugInfoWriter CreateDebugInfoBuilder();

        protected abstract void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints);

        protected abstract void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols);

        protected void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
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
            CreateEhSections();

            // Debugging
            if (_options.HasFlag(ObjectWritingOptions.GenerateDebugInfo))
            {
                _userDefinedTypeDescriptor = new UserDefinedTypeDescriptor(CreateDebugInfoBuilder(), _nodeFactory);
            }

            foreach (DependencyNode depNode in nodes)
            {
                ObjectNode node = depNode as ObjectNode;
                if (node == null || node.ShouldSkipEmittingObjectNode(_nodeFactory))
                {
                    continue;
                }

                ObjectData nodeContents = node.GetData(_nodeFactory);

                dumper?.DumpObjectNode(_nodeFactory, node, nodeContents);

                string currentSymbolName = null;
                if (node is ISymbolNode symbolNode)
                {
                    currentSymbolName = GetMangledName(symbolNode);
                }

                ObjectNodeSection section = node.GetSection(_nodeFactory);
                SectionWriter sectionWriter = ShouldShareSymbol(node, section) ?
                    GetOrCreateSection(section, currentSymbolName, currentSymbolName) :
                    GetOrCreateSection(section);

                sectionWriter.EmitAlignment(nodeContents.Alignment);

                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    bool isMethod = n.Offset == 0 && node is IMethodNode or AssemblyStubNode;
                    sectionWriter.EmitSymbolDefinition(
                        n == node ? currentSymbolName : GetMangledName(n),
                        n.Offset,
                        isMethod ? nodeContents.Data.Length : 0);
                    if (_nodeFactory.GetSymbolAlternateName(n) is string alternateName)
                    {
                        sectionWriter.EmitSymbolDefinition(
                            ExternCName(alternateName),
                            n.Offset,
                            isMethod ? nodeContents.Data.Length : 0,
                            global: true);
                    }
                }

                if (nodeContents.Relocs != null)
                {
                    foreach (Relocation reloc in nodeContents.Relocs)
                    {
                        string relocSymbolName = GetMangledName(reloc.Target);

                        sectionWriter.EmitRelocation(
                            reloc.Offset,
                            nodeContents.Data.AsSpan(reloc.Offset),
                            reloc.RelocType,
                            relocSymbolName,
                            reloc.Target.Offset);

                        if (_options.HasFlag(ObjectWritingOptions.ControlFlowGuard) &&
                            reloc.Target is IMethodNode or AssemblyStubNode)
                        {
                            // For now consider all method symbols address taken.
                            // We could restrict this in the future to those that are referenced from
                            // reflection tables, EH tables, were actually address taken in code, or are referenced from vtables.
                            EmitReferencedMethod(relocSymbolName);
                        }
                    }
                }

                // Emit unwinding frames and LSDA
                if (node is INodeWithCodeInfo nodeWithCodeInfo)
                {
                    EmitUnwindInfo(sectionWriter, nodeWithCodeInfo, currentSymbolName);
                }

                // Write the data. Note that this has to be done last as not to advance
                // the section writer position.
                sectionWriter.EmitData(nodeContents.Data);
            }

            EmitSectionsAndLayout();

            if (_options.HasFlag(ObjectWritingOptions.GenerateDebugInfo))
            {
                foreach (DependencyNode depNode in nodes)
                {
                    ObjectNode node = depNode as ObjectNode;
                    if (node == null || node.ShouldSkipEmittingObjectNode(_nodeFactory))
                    {
                        continue;
                    }

                    // Ensure any allocated MethodTables have debug info
                    if (node is ConstructedEETypeNode methodTable)
                    {
                        _userDefinedTypeDescriptor.GetTypeIndex(methodTable.Type, needsCompleteType: true);
                    }

                    if (node is INodeWithDebugInfo debugNode and ISymbolDefinitionNode symbolDefinitionNode and IMethodNode methodNode)
                    {
                        bool hasSequencePoints = debugNode.GetNativeSequencePoints().Any();
                        uint methodTypeIndex = hasSequencePoints ? _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method) : 0;
                        string methodName = GetMangledName(symbolDefinitionNode);

                        if (_definedSymbols.TryGetValue(methodName, out var methodSymbol))
                        {
                            EmitDebugFunctionInfo(methodTypeIndex, methodName, methodSymbol, debugNode, hasSequencePoints);
                        }
                    }
                }

                // Ensure all fields associated with generated static bases have debug info
                foreach (MetadataType typeWithStaticBase in _nodeFactory.MetadataManager.GetTypesWithStaticBases())
                {
                    _userDefinedTypeDescriptor.GetTypeIndex(typeWithStaticBase, needsCompleteType: true);
                }

                EmitDebugSections(_definedSymbols);
            }

            EmitSymbolTable(_definedSymbols, GetUndefinedSymbols());

            int relocSectionIndex = 0;
            foreach (List<SymbolicRelocation> relocationList in _sectionIndexToRelocations)
            {
                EmitRelocations(relocSectionIndex, relocationList);
                relocSectionIndex++;
            }

            EmitObjectFile(objectFilePath);
        }
    }
}
