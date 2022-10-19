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

using LibObjectFile;
using LibObjectFile.Dwarf;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public abstract class UnixObjectWriter : IDisposable
    {
        protected sealed record SymbolDefinition(int SectionIndex, long Value, int Size = 0);
        protected sealed record SymbolicRelocation(int Offset, RelocType Type, string SymbolName, int Addend = 0);

        private NodeFactory _nodeFactory;
        private ObjectWritingOptions _options;

        // Debugging
        private DwarfBuilder _dwarfBuilder;
        private UserDefinedTypeDescriptor _userDefinedTypeDescriptor;

        private byte _insPaddingByte;

        // Standard sections
        private Dictionary<string, int> _sectionNameToSectionIndex = new();
        private List<Stream> _sectionIndexToStream = new();
        private List<List<SymbolicRelocation>> _sectionIndexToRelocations = new();

        // Exception handling sections
        private int _lsdaSectionIndex;
        private int _ehFrameSectionIndex;
        private DwarfCie _dwarfCie;
        private DwarfEhFrame _dwarfEhFrame;

        // Symbol table
        private Dictionary<string, SymbolDefinition> _definedSymbols = new();

        protected int EhFrameSectionIndex => _ehFrameSectionIndex;

        protected UnixObjectWriter(NodeFactory factory, ObjectWritingOptions options)
        {
            _nodeFactory = factory;
            _options = options;

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

            if (!_sectionNameToSectionIndex.TryGetValue(section.Name, out sectionIndex))
            {
                CreateSection(section, out sectionStream);
                sectionIndex = _sectionNameToSectionIndex.Count;
                _sectionNameToSectionIndex[section.Name] = sectionIndex;
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

        protected virtual bool EmitCompactUnwinding(DwarfFde fde) => false;

        private void EmitUnwindInfo(int sectionIndex, long methodStart, INodeWithCodeInfo nodeWithCodeInfo)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                var lsdaStream = _sectionIndexToStream[_lsdaSectionIndex];
                Span<byte> tempBuffer = stackalloc byte[4];
                long mainLsdaOffset = lsdaStream.Position;
                string currentSymbolName = ExternCName(symbolDefinitionNode.GetMangledName(_nodeFactory.NameMangler));

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    int len = frameInfo.BlobData.Length;
                    byte[] blob = frameInfo.BlobData;

                    string lsdaSymbolName = $"_lsda{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    EmitSymbolDefinition(lsdaSymbolName, new SymbolDefinition(_lsdaSectionIndex, lsdaStream.Position));
                    if (start != 0)
                    {
                        EmitSymbolDefinition(framSymbolName, new SymbolDefinition(sectionIndex, methodStart + start, 0));
                    }

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        lsdaStream.WriteByte((byte)flags);

                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(mainLsdaOffset - lsdaStream.Position));
                        lsdaStream.Write(tempBuffer);

                        // Emit relative offset from the main function
                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(start - frameInfos[0].StartOffset));
                        lsdaStream.Write(tempBuffer);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        lsdaStream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = ExternCName(associatedDataNode.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _lsdaSectionIndex,
                                _sectionIndexToRelocations[_lsdaSectionIndex],
                                (int)lsdaStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_RELPTR32,
                                symbolName,
                                0);
                            lsdaStream.Write(tempBuffer);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = ExternCName(ehInfo.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _lsdaSectionIndex,
                                _sectionIndexToRelocations[_lsdaSectionIndex],
                                (int)lsdaStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_RELPTR32,
                                symbolName,
                                0);
                            lsdaStream.Write(tempBuffer);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            lsdaStream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    var fde = new DwarfFde(_dwarfCie, DwarfFde.CfiCodeToInstructions(_dwarfCie, frameInfo.BlobData))
                    {
                        PcStartSymbolName = start != 0 ? framSymbolName : currentSymbolName,
                        PcLength = (ulong)(end - start),
                        LsdaSymbolName = lsdaSymbolName,
                    };

                    _dwarfEhFrame.AddFde(fde);

                    EmitCompactUnwinding(fde);
                }
            }
        }

        private void EmitAlignment(int sectionIndex, Stream sectionStream, int alignment)
        {
            UpdateSectionAlignment(sectionIndex, alignment, out bool isExecutable);

            int padding = (int)(((sectionStream.Position + alignment - 1) & ~(alignment - 1)) - sectionStream.Position);
            Span<byte> buffer = stackalloc byte[padding];
            byte paddingByte = isExecutable ? _insPaddingByte : (byte)0;
            buffer.Fill(paddingByte);
            sectionStream.Write(buffer);
        }

        private uint GetVarTypeIndex(bool isStateMachineMoveNextMethod, DebugVarInfoMetadata debugVar)
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

        protected virtual ulong GetSectionVirtualAddress(int sectionIndex) => 0;

        private void EmitDebugFunctionInfo(ObjectNode node)
        {
            if (node is IMethodNode methodNode)
            {
                uint methodTypeIndex = _userDefinedTypeDescriptor.GetMethodFunctionIdTypeIndex(methodNode.Method);
                string methodName = ExternCName(methodNode.GetMangledName(_nodeFactory.NameMangler));

                if (node is INodeWithDebugInfo debugNode &&
                    _definedSymbols.TryGetValue(methodName, out var methodSymbol))
                {
                    var lowPC = GetSectionVirtualAddress(methodSymbol.SectionIndex) + (ulong)methodSymbol.Value;
                    DebugEHClauseInfo[] clauses = null;

                    if (node is INodeWithCodeInfo nodeWithCodeInfo)
                    {
                        clauses = nodeWithCodeInfo.DebugEHClauseInfos;
                    }

                    _dwarfBuilder.EmitSubprogramInfo(
                        methodName,
                        lowPC,
                        methodSymbol.Size,
                        methodTypeIndex,
                        debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                        clauses ?? Array.Empty<DebugEHClauseInfo>());

                    _dwarfBuilder.EmitLineInfo(methodSymbol.SectionIndex, lowPC, debugNode.GetNativeSequencePoints());
                }
            }
        }

        protected abstract void EmitSectionsAndLayout();

        protected abstract void EmitObjectFile(string objectFilePath);

        protected abstract void EmitDebugSections(DwarfFile dwarfFile);

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

        protected void EmitObject(string objectFilePath, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            // Pre-create some of the sections
            GetOrCreateSection(ObjectNodeSection.TextSection, out _, out _);
            GetOrCreateSection(ObjectNodeSection.ManagedCodeUnixContentSection, out _, out _);

            // Create sections for exception handling
            _lsdaSectionIndex = GetOrCreateSection(new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly, null), out _, out _);
            _ehFrameSectionIndex = GetOrCreateSection(new ObjectNodeSection(".eh_frame", SectionType.ReadOnly, null), out _, out _);
            UpdateSectionAlignment(_lsdaSectionIndex, 8, out _);
            UpdateSectionAlignment(_ehFrameSectionIndex, 8, out _);

            // We always use the same CIE in DWARF EH frames, so create and emit it now
            bool is64Bit = _nodeFactory.Target.Architecture switch
            {
                TargetArchitecture.X86 => false,
                TargetArchitecture.ARM => false,
                _ => true
            };
            _dwarfCie = new DwarfCie(_nodeFactory.Target.Architecture);
            _dwarfEhFrame = new DwarfEhFrame(
                _sectionIndexToStream[_ehFrameSectionIndex],
                (offset, data, relocType, symbolName) => EmitRelocation(
                    _ehFrameSectionIndex, _sectionIndexToRelocations[_ehFrameSectionIndex],
                    offset, data, relocType, symbolName, 0),
                is64Bit);
            _dwarfEhFrame.AddCie(_dwarfCie);

            // Debugging
            if (_options.HasFlag(ObjectWritingOptions.GenerateDebugInfo))
            {
                _dwarfBuilder = new DwarfBuilder(_nodeFactory.NameMangler, _nodeFactory.Target.Architecture);
                _userDefinedTypeDescriptor = new UserDefinedTypeDescriptor(_dwarfBuilder, _nodeFactory);
            }

            foreach (DependencyNode depNode in nodes)
            {
                ObjectNode node = depNode as ObjectNode;
                if (node == null || node.ShouldSkipEmittingObjectNode(_nodeFactory))
                {
                    continue;
                }

                ObjectData nodeContents = node.GetData(_nodeFactory);

                Stream sectionStream;
                List<SymbolicRelocation> relocationList;
                int sectionIndex = GetOrCreateSection(node.Section, out sectionStream, out relocationList);

                EmitAlignment(sectionIndex, sectionStream, nodeContents.Alignment);

                long methodStart = sectionStream.Position;

                foreach (ISymbolDefinitionNode n in nodeContents.DefinedSymbols)
                {
                    var symbolDefinition = new SymbolDefinition(
                        sectionIndex,
                        methodStart + n.Offset,
                        n.Offset == 0 ? nodeContents.Data.Length : 0);
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

                    EmitDebugFunctionInfo(node);
                }

                _dwarfBuilder.EmitStaticVars(
                    symbolName =>
                    {
                        if (_definedSymbols.TryGetValue(ExternCName(symbolName), out var symbolDef))
                            return GetSectionVirtualAddress(symbolDef.SectionIndex) + (ulong)symbolDef.Value;
                        return 0;
                    }
                );

                EmitDebugSections(_dwarfBuilder.DwarfFile);
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
