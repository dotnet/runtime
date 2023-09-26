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

namespace ILCompiler.ObjectWriter
{
    public abstract class ObjectWriter : IDisposable
    {
        protected sealed record SymbolDefinition(int SectionIndex, long Value, int Size = 0);
        protected sealed record SymbolicRelocation(int Offset, RelocType Type, string SymbolName, int Addend = 0);

        protected NodeFactory _nodeFactory;
        protected ObjectWritingOptions _options;
        protected bool _isSingleFileCompilation;

        private Dictionary<ISymbolNode, string> _mangledNameMap = new();

        private byte _insPaddingByte;

        // Standard sections
        private Dictionary<(string, string), int> _sectionNameToSectionIndex = new();
        private List<Stream> _sectionIndexToStream = new();
        private List<List<SymbolicRelocation>> _sectionIndexToRelocations = new();

        // Symbol table
        private Dictionary<string, SymbolDefinition> _definedSymbols = new();

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
            foreach (var sectionStream in _sectionIndexToStream)
            {
                sectionStream.Close();
            }
        }

        protected abstract void CreateSection(ObjectNodeSection section, out Stream sectionStream);

        protected internal abstract void UpdateSectionAlignment(int sectionIndex, int alignment);

        protected SectionWriter GetOrCreateSection(ObjectNodeSection section)
        {
            int sectionIndex;
            Stream sectionStream;

            if (!_sectionNameToSectionIndex.TryGetValue((section.Name, section.ComdatName), out sectionIndex))
            {
                CreateSection(section, out sectionStream);
                sectionIndex = _sectionNameToSectionIndex.Count;
                _sectionNameToSectionIndex.Add((section.Name, section.ComdatName), sectionIndex);
                _sectionIndexToStream.Add(sectionStream);
                _sectionIndexToRelocations.Add(new());
            }
            else
            {
                sectionStream = _sectionIndexToStream[sectionIndex];
            }

            return new SectionWriter(
                this,
                sectionIndex,
                sectionStream,
                section.Type == SectionType.Executable ? _insPaddingByte : (byte)0);
        }

        protected bool ShouldShareSymbol(ObjectNode node)
        {
            // TODO: Not supported yet
            if (_nodeFactory.Target.IsOSXLike ||
                _nodeFactory.Target.OperatingSystem == TargetOS.Linux)
                return false;

            return ShouldShareSymbol(node, node.GetSection(_nodeFactory));
        }

        protected bool ShouldShareSymbol(ObjectNode node, ObjectNodeSection section)
        {
            // TODO: Not supported yet
            if (_nodeFactory.Target.IsOSXLike ||
                _nodeFactory.Target.OperatingSystem == TargetOS.Linux)
                return false;

            // Foldable sections are always COMDATs
            if (section == ObjectNodeSection.FoldableManagedCodeUnixContentSection ||
                section == ObjectNodeSection.FoldableManagedCodeWindowsContentSection ||
                section == ObjectNodeSection.FoldableReadOnlyDataSection)
                return true;

            if (_isSingleFileCompilation)
                return false;

            if (!(node is ISymbolNode))
                return false;

            // These intentionally clash with one another, but are merged with linker directives so should not be Comdat folded
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
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend)
        {
            _sectionIndexToRelocations[sectionIndex].Add(new SymbolicRelocation(offset, relocType, symbolName, addend));
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
            int offset = 0,
            int size = 0)
        {
            _definedSymbols.Add(
                symbolName,
                new SymbolDefinition(sectionIndex, offset, size));
        }

        /// <summary>
        /// Emit symbolic definitions into object file symbols.
        /// </summary>
        protected abstract void EmitSymbolTable();

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

        protected IDictionary<string, SymbolDefinition> GetDefinedSymbols() => _definedSymbols;

        protected ISet<string> GetUndefinedSymbols()
        {
            HashSet<string> undefinedSymbolSet = new HashSet<string>();
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

        protected virtual ulong GetSectionVirtualAddress(int sectionIndex) => 0;

        protected abstract void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints);

        protected abstract void EmitDebugSections();

        protected abstract void EmitDebugStaticVars();

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
                if (ShouldShareSymbol(node, section))
                {
                    section = GetSharedSection(section, currentSymbolName);
                }

                SectionWriter sectionWriter = GetOrCreateSection(section);

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
                            isMethod ? nodeContents.Data.Length : 0);
                    }
                }

                if (nodeContents.Relocs != null)
                {
                    foreach (var reloc in nodeContents.Relocs)
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

                    // Emit debug type information
                    if (node is ConstructedEETypeNode methodTable)
                    {
                        _userDefinedTypeDescriptor.GetTypeIndex(methodTable.Type, needsCompleteType: true);
                    }

                    if (node is INodeWithDebugInfo debugNode &&
                        node is ISymbolDefinitionNode symbolDefinitionNode)
                    {
                        bool hasSequencePoints = debugNode.GetNativeSequencePoints().Any();
                        uint methodTypeIndex = node is IMethodNode methodNode ?
                            _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method) :
                            0;
                        string methodName = GetMangledName(symbolDefinitionNode);

                        if (_definedSymbols.TryGetValue(methodName, out var methodSymbol))
                        {
                            EmitDebugFunctionInfo(methodTypeIndex, methodName, methodSymbol, debugNode, hasSequencePoints);
                        }
                    }
                }

                EmitDebugStaticVars();

                EmitDebugSections();
            }

            EmitSymbolTable();

            int relocSectionIndex = 0;
            foreach (var relocationList in _sectionIndexToRelocations)
            {
                EmitRelocations(relocSectionIndex, relocationList);
                relocSectionIndex++;
            }

            EmitObjectFile(objectFilePath);
        }
    }
}
