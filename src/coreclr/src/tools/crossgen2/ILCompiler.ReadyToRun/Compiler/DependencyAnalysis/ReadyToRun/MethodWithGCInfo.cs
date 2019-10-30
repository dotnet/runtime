// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodWithGCInfo : ObjectNode, IReadyToRunMethodCodeNode, IMethodBodyNode
    {
        public readonly MethodGCInfoNode GCInfoNode;

        private readonly MethodDesc _method;
        public SignatureContext SignatureContext { get; }

        private ObjectData _methodCode;
        private FrameInfo[] _frameInfos;
        private byte[] _gcInfo;
        private ObjectData _ehInfo;
        private OffsetMapping[] _debugLocInfos;
        private NativeVarInfo[] _debugVarInfos;
        private DebugEHClauseInfo[] _debugEHClauseInfos;

        public MethodWithGCInfo(MethodDesc methodDesc, SignatureContext signatureContext)
        {
            GCInfoNode = new MethodGCInfoNode(this);
            _method = methodDesc;
            SignatureContext = signatureContext;
        }

        public void SetCode(ObjectData data)
        {
            Debug.Assert(_methodCode == null);
            _methodCode = data;
        }

        public MethodDesc Method => _method;

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


        public byte[] GetFixupBlob(NodeFactory factory)
        {
            Relocation[] relocations = GetData(factory, relocsOnly: true).Relocs;

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

            if (fixupCells == null)
            {
                return null;
            }

            fixupCells.Sort(FixupCell.Comparer);

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
            return new DependencyList(new DependencyListEntry[] { new DependencyListEntry(GCInfoNode, "Unwind & GC info") });
        }

        public override bool StaticDependenciesAreComputed => _methodCode != null;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
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
        public byte[] GCInfo => _gcInfo;
        public ObjectData EHInfo => _ehInfo;

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

        public OffsetMapping[] DebugLocInfos => _debugLocInfos;
        public NativeVarInfo[] DebugVarInfos => _debugVarInfos;
        public DebugEHClauseInfo[] DebugEHClauseInfos => _debugEHClauseInfos;

        public void InitializeDebugLocInfos(OffsetMapping[] debugLocInfos)
        {
            Debug.Assert(_debugLocInfos == null);
            _debugLocInfos = debugLocInfos;
        }

        public void InitializeDebugVarInfos(NativeVarInfo[] debugVarInfos)
        {
            Debug.Assert(_debugVarInfos == null);
            _debugVarInfos = debugVarInfos;
        }

        public void InitializeDebugEHClauseInfos(DebugEHClauseInfo[] debugEHClauseInfos)
        {
            Debug.Assert(_debugEHClauseInfos == null);
            _debugEHClauseInfos = debugEHClauseInfos;
        }

        public int CompareToImpl(ISortableSymbolNode other, CompilerComparer comparer)
        {
            throw new NotImplementedException();
        }

        public int Offset => 0;
        public override bool IsShareable => throw new NotImplementedException();
    }
}
