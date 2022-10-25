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

        protected abstract void UpdateSectionAlignment(int sectionIndex, int alignment, out bool isExecutable);

        protected int GetOrCreateSection(
            ObjectNodeSection section,
            out Stream sectionStream,
            out List<SymbolicRelocation> relocationList)
        {
            int sectionIndex;

            if (!_sectionNameToSectionIndex.TryGetValue((section.Name, section.ComdatName), out sectionIndex))
            {
                CreateSection(section, out sectionStream);
                sectionIndex = _sectionNameToSectionIndex.Count;
                _sectionNameToSectionIndex.Add((section.Name, section.ComdatName), sectionIndex);
                _sectionIndexToStream.Add(sectionStream);
                _sectionIndexToRelocations.Add(relocationList = new());
            }
            else
            {
                sectionStream = _sectionIndexToStream[sectionIndex];
                relocationList = _sectionIndexToRelocations[sectionIndex];
            }

            return sectionIndex;
        }

        protected bool ShouldShareSymbol(ObjectNode node)
        {
            // TODO: Not supported yet
            if (_nodeFactory.Target.OperatingSystem == TargetOS.OSX ||
                _nodeFactory.Target.OperatingSystem == TargetOS.Linux)
                return false;

            // Foldable sections are always COMDATs
            ObjectNodeSection section = node.Section;
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

        protected abstract void EmitRelocation(
            int sectionIndex,
            List<SymbolicRelocation> relocationList,
            int offset,
            Span<byte> data,
            RelocType relocType,
            string symbolName,
            int addend);

        /// <summary>
        /// Emit symbolic definitions into object file symbols.
        /// </summary>
        protected abstract void EmitSymbolTable();

        /// <summary>
        /// Emit symbolic relocations into object file as format specific
        /// relocations.
        /// </summary>
        /// <remarks>
        /// This methods is guaranteed to run after <see cref="EmitSymbolTable" />.
        /// </remarks>
        protected abstract void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList);

        protected virtual string ExternCName(string name) => name;

        protected void EmitSymbolDefinition(string name, SymbolDefinition definition)
        {
            _definedSymbols.Add(name, definition);
        }

        protected abstract void EmitUnwindInfo(int sectionIndex, long methodStart, INodeWithCodeInfo nodeWithCodeInfo);

        protected void EmitAlignment(int sectionIndex, Stream sectionStream, int alignment)
        {
            UpdateSectionAlignment(sectionIndex, alignment, out bool isExecutable);

            int padding = (int)(((sectionStream.Position + alignment - 1) & ~(alignment - 1)) - sectionStream.Position);
            Span<byte> buffer = stackalloc byte[padding];
            byte paddingByte = isExecutable ? _insPaddingByte : (byte)0;
            buffer.Fill(paddingByte);
            sectionStream.Write(buffer);
        }

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
            INodeWithDebugInfo debugNode);

        protected abstract void EmitDebugSections();

        protected abstract void EmitDebugStaticVars();

        protected void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            // Pre-create some of the sections
            GetOrCreateSection(ObjectNodeSection.TextSection, out _, out _);
            if (_nodeFactory.Target.OperatingSystem == TargetOS.Windows)
            {
                GetOrCreateSection(ObjectNodeSection.ManagedCodeWindowsContentSection, out _, out _);
            }
            else
            {
                GetOrCreateSection(ObjectNodeSection.ManagedCodeUnixContentSection, out _, out _);
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

                ObjectNodeSection section = node.Section;
                if (ShouldShareSymbol(node))
                {
                    section = GetSharedSection(
                        section,
                        ExternCName(((ISymbolNode)node).GetMangledName(_nodeFactory.NameMangler)));
                }

                Stream sectionStream;
                List<SymbolicRelocation> relocationList;
                int sectionIndex = GetOrCreateSection(section, out sectionStream, out relocationList);

                EmitAlignment(sectionIndex, sectionStream, nodeContents.Alignment);

                long methodStart = sectionStream.Position;

                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    bool isMethod = n.Offset == 0 && node is IMethodNode or AssemblyStubNode;
                    var symbolDefinition = new SymbolDefinition(
                        sectionIndex,
                        methodStart + n.Offset,
                        isMethod ? nodeContents.Data.Length : 0);
                    EmitSymbolDefinition(ExternCName(n.GetMangledName(_nodeFactory.NameMangler)), symbolDefinition);
                    if (_nodeFactory.GetSymbolAlternateName(n) is string alternateName)
                    {
                        EmitSymbolDefinition(ExternCName(alternateName), symbolDefinition);
                    }
                }

                if (nodeContents.Relocs != null)
                {
                    foreach (var reloc in nodeContents.Relocs)
                    {
                        EmitRelocation(
                            sectionIndex,
                            relocationList,
                            (int)(methodStart + reloc.Offset),
                            nodeContents.Data.AsSpan(reloc.Offset),
                            reloc.RelocType,
                            ExternCName(reloc.Target.GetMangledName(_nodeFactory.NameMangler)),
                            reloc.Target.Offset);
                    }
                }

                sectionStream.Write(nodeContents.Data);

                // Emit unwinding frames and LSDA
                if (node is INodeWithCodeInfo nodeWithCodeInfo)
                {
                    EmitUnwindInfo(sectionIndex, methodStart, nodeWithCodeInfo);
                }
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

                    if (node is IMethodNode methodNode)
                    {
                        uint methodTypeIndex = _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method);
                        string methodName = ExternCName(methodNode.GetMangledName(_nodeFactory.NameMangler));

                        if (node is INodeWithDebugInfo debugNode &&
                            _definedSymbols.TryGetValue(methodName, out var methodSymbol))
                        {
                            EmitDebugFunctionInfo(methodTypeIndex, methodName, methodSymbol, debugNode);
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
