// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodWithGCInfo : ObjectNode, IMethodBodyNode, ISymbolDefinitionNode
    {
        public readonly MethodGCInfoNode GCInfoNode;

        private readonly MethodDesc _method;

        private ObjectData _methodCode;
        private FrameInfo[] _frameInfos;
        private FrameInfo[] _coldFrameInfos;
        private byte[] _gcInfo;
        private ObjectData _ehInfo;
        private byte[] _debugLocInfos;
        private byte[] _debugVarInfos;
        private DebugEHClauseInfo[] _debugEHClauseInfos;
        private List<ISymbolNode> _fixups;
        private MethodDesc[] _inlinedMethods;
        private bool _lateTriggeredCompilation;

        public MethodWithGCInfo(MethodDesc methodDesc)
        {
            GCInfoNode = new MethodGCInfoNode(this);
            _fixups = new List<ISymbolNode>();
            _method = methodDesc;
        }

        protected override void OnMarked(NodeFactory context)
        {
            // Once past phase 1, no new methods which are interesting for compilation may be marked except for methods
            // specially enabled for higher phases
            if (context.CompilationCurrentPhase > 1)
            {
                SetCode(new ObjectNode.ObjectData(Array.Empty<byte>(), null, 1, Array.Empty<ISymbolDefinitionNode>()));
                InitializeFrameInfos(Array.Empty<FrameInfo>());
                InitializeColdFrameInfos(Array.Empty<FrameInfo>());
            }
            _lateTriggeredCompilation = context.CompilationCurrentPhase != 0;
            RegisterInlineeModuleIndices(context);
        }

        private void RegisterInlineeModuleIndices(NodeFactory factory)
        {
            if (_inlinedMethods != null)
            {
                foreach (var inlinee in _inlinedMethods)
                {
                    MethodDesc inlineeDefinition = inlinee.GetTypicalMethodDefinition();
                    if (!(inlineeDefinition is EcmaMethod ecmaInlineeDefinition))
                    {
                        // We don't record non-ECMA methods because they don't have tokens that
                        // diagnostic tools could reason about anyway.
                        continue;
                    }

                    if (!factory.CompilationModuleGroup.VersionsWithMethodBody(inlinee) && !factory.CompilationModuleGroup.CrossModuleInlineable(inlinee))
                    {
                        // We cannot record inlining info across version bubble as cross-bubble assemblies
                        // are not guaranteed to preserve token values unless CrossModule inlining is in place
                        // Otherwise non-versionable methods may be inlined across the version bubble.

                        Debug.Assert(inlinee.IsNonVersionable());
                        continue;
                    }
                    factory.ManifestMetadataTable.EnsureModuleIndexable(ecmaInlineeDefinition.Module);
                }
            }
        }

        public override int DependencyPhaseForDeferredStaticComputation => _lateTriggeredCompilation ? 2 : 0;

        public void SetCode(ObjectData data)
        {
            Debug.Assert(_methodCode == null);
            _methodCode = data;
        }

        public MethodDesc Method => _method;

        public List<ISymbolNode> Fixups => _fixups;

        public int Size => _methodCode.Data.Length;

        public bool IsEmpty => _methodCode.Data.Length == 0;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            return _methodCode;
        }

        /// <summary>
        /// This helper structure represents the "coordinates" of a single
        /// indirection cell in the import tables (index of the import
        /// section table and offset within the table).
        /// </summary>
        private struct FixupCell
        {
            public static readonly IComparer<FixupCell> Comparer = new CellComparer();

            public int TableIndex;
            public int ImportOffset;

            public FixupCell(int tableIndex, int importOffset)
            {
                TableIndex = tableIndex;
                ImportOffset = importOffset;
            }

            private class CellComparer : IComparer<FixupCell>
            {
                public int Compare(FixupCell a, FixupCell b)
                {
                    int result = a.TableIndex.CompareTo(b.TableIndex);
                    if (result == 0)
                    {
                        result = a.ImportOffset.CompareTo(b.ImportOffset);
                    }
                    return result;
                }
            }
        }

        public MethodColdCodeNode ColdCodeNode { get; set; }

        public byte[] GetFixupBlob(NodeFactory factory)
        {
            Relocation[] relocations = GetData(factory, relocsOnly: true).Relocs;

            if (ColdCodeNode != null)
            {
                Relocation[] coldRelocations = ColdCodeNode.GetData(factory, relocsOnly: true).Relocs;
                if (relocations == null)
                {
                    relocations = coldRelocations;
                }
                else if (coldRelocations != null)
                {
                    relocations = Enumerable.Concat(relocations, coldRelocations).ToArray();
                }
            }

            if (relocations == null)
            {
                return null;
            }

            List<FixupCell> fixupCells = null;

            foreach (Relocation reloc in relocations)
            {
                if (reloc.Target is Import fixupCell && fixupCell.EmitPrecode)
                {
                    if (fixupCells == null)
                    {
                        fixupCells = new List<FixupCell>();
                    }
                    fixupCells.Add(new FixupCell(fixupCell.Table.IndexFromBeginningOfArray, fixupCell.OffsetFromBeginningOfArray));
                }
            }

            foreach (ISymbolNode node in _fixups)
            {
                if (fixupCells == null)
                {
                    fixupCells = new List<FixupCell>();
                }

                Import fixupCell = (Import)node;
                fixupCells.Add(new FixupCell(fixupCell.Table.IndexFromBeginningOfArray, fixupCell.OffsetFromBeginningOfArray));
            }

            if (fixupCells == null)
            {
                return null;
            }

            fixupCells.MergeSortAllowDuplicates(FixupCell.Comparer);

            // Deduplicate fixupCells
            int j = 0;
            for (int i = 1; i < fixupCells.Count; i++)
            {
                if (FixupCell.Comparer.Compare(fixupCells[j], fixupCells[i]) != 0)
                {
                    j++;
                    if (i != j)
                    {
                        fixupCells[j] = fixupCells[i];
                    }
                }
            }

            // Move j to point after the last valid fixupCell in the array
            j++;

            if (j < fixupCells.Count)
            {
                fixupCells.RemoveRange(j, fixupCells.Count - j);
            }

            NibbleWriter writer = new NibbleWriter();

            int curTableIndex = -1;
            int curOffset = 0;

            foreach (FixupCell cell in fixupCells)
            {
                Debug.Assert(cell.ImportOffset % factory.Target.PointerSize == 0);
                int offset = cell.ImportOffset / factory.Target.PointerSize;

                if (cell.TableIndex != curTableIndex)
                {
                    // Write delta relative to the previous table index
                    Debug.Assert(cell.TableIndex > curTableIndex);
                    if (curTableIndex != -1)
                    {
                        writer.WriteUInt(0); // table separator, so add except for the first entry
                        writer.WriteUInt((uint)(cell.TableIndex - curTableIndex)); // add table index delta
                    }
                    else
                    {
                        writer.WriteUInt((uint)cell.TableIndex);
                    }
                    curTableIndex = cell.TableIndex;

                    // This is the first fixup in the current table.
                    // We will write it out completely (without delta-encoding)
                    writer.WriteUInt((uint)offset);
                }
                else if (offset != curOffset) // ignore duplicate fixup cells
                {
                    // This is not the first entry in the current table.
                    // We will write out the delta relative to the previous fixup value
                    int delta = offset - curOffset;
                    Debug.Assert(delta > 0);
                    writer.WriteUInt((uint)delta);
                }

                // future entries for this table would be relative to this rva
                curOffset = offset;
            }

            writer.WriteUInt(0); // table separator
            writer.WriteUInt(0); // fixup list ends

            return writer.ToArray();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = new DependencyList(new DependencyListEntry[] { new DependencyListEntry(GCInfoNode, "Unwind & GC info") });

            if (this.ColdCodeNode != null)
            {
                dependencyList.Add(this.ColdCodeNode, "cold");
            }

            foreach (ISymbolNode node in _fixups)
            {
                dependencyList.Add(node, "classMustBeLoadedBeforeCodeIsRun");
            }

            return dependencyList;
        }

        public override bool StaticDependenciesAreComputed => _methodCode != null;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append("MethodWithGCInfo(");
            AppendMangledName(factory.NameMangler, sb);
            sb.Append(")");
            return sb.ToString();
        }

        public override int ClassCode => 315213488;

        public override ObjectNodeSection Section
        {
            get
            {
                return _method.Context.Target.IsWindows ? ObjectNodeSection.ManagedCodeWindowsContentSection : ObjectNodeSection.ManagedCodeUnixContentSection;
            }
        }

        public FrameInfo[] FrameInfos => _frameInfos;

        public FrameInfo[] ColdFrameInfos => _coldFrameInfos;

        public byte[] GCInfo => _gcInfo;
        public ObjectData EHInfo => _ehInfo;
        public MethodDesc[] InlinedMethods => _inlinedMethods;

        public void InitializeFrameInfos(FrameInfo[] frameInfos)
        {
            Debug.Assert(_frameInfos == null);
            if (frameInfos != null)
            {
                _frameInfos = frameInfos;
            }
            else
            {
                // On x86, fake a single frame info representing the entire method
                _frameInfos = new FrameInfo[] 
                {
                    new FrameInfo((FrameInfoFlags)0, startOffset: 0, endOffset: 0, blobData: Array.Empty<byte>())
                };
            }
        }

        public void InitializeColdFrameInfos(FrameInfo[] coldFrameInfos)
        {
            Debug.Assert(_coldFrameInfos == null);
            _coldFrameInfos = coldFrameInfos;
            // TODO: x86 (see InitializeFrameInfos())
        }

        public void InitializeGCInfo(byte[] gcInfo)
        {
            Debug.Assert(_gcInfo == null);
            _gcInfo = gcInfo;
        }

        public void InitializeEHInfo(ObjectData ehInfo)
        {
            Debug.Assert(_ehInfo == null);
            _ehInfo = ehInfo;
        }

        public byte[] DebugLocInfos => _debugLocInfos;
        public byte[] DebugVarInfos => _debugVarInfos;
        public DebugEHClauseInfo[] DebugEHClauseInfos => _debugEHClauseInfos;

        public void InitializeDebugLocInfos(OffsetMapping[] debugLocInfos)
        {
            Debug.Assert(_debugLocInfos == null);
            // Process the debug info from JIT format to R2R format immediately as it is large
            // and not used in the rest of the process except to emit.
            _debugLocInfos = DebugInfoTableNode.CreateBoundsBlobForMethod(debugLocInfos);
        }

        public void InitializeDebugVarInfos(NativeVarInfo[] debugVarInfos)
        {
            Debug.Assert(_debugVarInfos == null);
            // Process the debug info from JIT format to R2R format immediately as it is large
            // and not used in the rest of the process except to emit.
            _debugVarInfos = DebugInfoTableNode.CreateVarBlobForMethod(debugVarInfos, _method.Context.Target);
        }

        public void InitializeDebugEHClauseInfos(DebugEHClauseInfo[] debugEHClauseInfos)
        {
            Debug.Assert(_debugEHClauseInfos == null);
            _debugEHClauseInfos = debugEHClauseInfos;
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            MethodWithGCInfo otherNode = (MethodWithGCInfo)other;
            return comparer.Compare(_method, otherNode._method);
        }

        public void InitializeInliningInfo(MethodDesc[] inlinedMethods, NodeFactory factory)
        {
            Debug.Assert(_inlinedMethods == null);
            _inlinedMethods = inlinedMethods;
            if (this.Marked)
                RegisterInlineeModuleIndices(factory);
        }

        public int Offset => 0;
        public override bool IsShareable => throw new NotImplementedException();
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => IsEmpty;

        public override string ToString() => _method.ToString();
    }
}
