// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Internal.JitInterface;
using Internal.Pgo;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodWithGCInfo : ObjectNode, IMethodBodyNode, INodeWithFunclets, IMethodCodeNodeWithTypeSignature
    {
        // #PrecodeFixupMinimization
        //
        // Minimize per-method precode metadata while ensuring that every required
        // import cell is resolved before the compiled method executes.
        //
        // Direct calls covered by this optimization, currently only supported on
        // Wasm, bypass the callee's normal activation path. The caller must therefore
        // arrange callee activation through a method-entry fixup or module-eager
        // activation. Activating the callee also resolves its transitive requirements,
        // so the caller can omit fixups already covered by its direct callees or by
        // module activation.
        //
        // Keep two sets distinct: _fixups contains the remaining work for this
        // method's precode; the cached activation guarantees include requirements
        // satisfied by the module and direct callees as well. Callers need the full
        // guarantee set even after redundant entries have been removed from _fixups.
        // Coverage is by import-cell identity, not merely by matching signatures.
        //
        // Startup-eager promotion uses the same guarantees to move the requirements
        // of eagerly activated methods and their direct-call closure to module startup.
        // Those cells can then be removed from every method's precode without
        // invalidating its cached guarantees.
        //
        // Deferred call choices and code/data publication must be complete before
        // computing guarantees, and direct callees must be finalized before callers.

        public readonly MethodGCInfoNode GCInfoNode;

        private readonly MethodDesc _method;

        private ObjectData _methodCode;
        private FrameInfo[] _frameInfos;
        private FrameInfo[] _coldFrameInfos;
        private byte[] _gcInfo;
        private ObjectData _ehInfo;
        private FuncletKind[] _funcletKinds;
        private byte[] _debugLocInfos;
        private byte[] _debugVarInfos;
        private DebugEHClauseInfo[] _debugEHClauseInfos;
        private List<ISymbolNode> _fixups;
        private Import[] _guaranteedPrecodeFixups;
#if DEBUG
        private bool _precodeFixupsFinalized;
#endif
        private MethodDesc[] _inlinedMethods;
        private bool _lateTriggeredCompilation;
        private DependencyList _nonRelocationDependencies;

        // Preserve activation imports not represented by the final direct-call
        // relocations. See #PrecodeFixupMinimization.
        internal void AddDeferredWasmDependency(ISymbolNode node)
        {
            _nonRelocationDependencies ??= new DependencyList();
            _nonRelocationDependencies.Add(node, "Selected deferred Wasm call");
        }

        // Install finalized call relocations and frame ranges, remapping debug
        // offsets before encoding. See #PrecodeFixupMinimization for ordering.
        internal void FinalizeDeferredWasmCode(
            ObjectData code, FrameInfo[] frames, OffsetMapping[] locations, NativeVarInfo[] variables,
            Func<int, int> mapOffset = null)
        {
#if DEBUG
            Debug.Assert(!_precodeFixupsFinalized);
#endif
            _methodCode = code;
            _frameInfos = frames;
            if (locations is not null)
            {
                if (mapOffset is not null)
                {
                    foreach (ref OffsetMapping location in locations.AsSpan())
                    {
                        location.nativeOffset = checked((uint)mapOffset((int)location.nativeOffset));
                    }
                }
                InitializeDebugLocInfos(locations);
            }
            if (variables is not null)
            {
                if (mapOffset is not null)
                {
                    foreach (ref NativeVarInfo variable in variables.AsSpan())
                    {
                        variable.startOffset = checked((uint)mapOffset((int)variable.startOffset));
                        variable.endOffset = checked((uint)mapOffset((int)variable.endOffset));
                    }
                }
                InitializeDebugVarInfos(variables);
            }
        }

        public MethodWithGCInfo(MethodDesc methodDesc)
        {
            Debug.Assert(!methodDesc.IsUnboxingThunk());
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

        public bool IsJitHelper { get; set; }

        public List<ISymbolNode> Fixups => _fixups;

        internal ReadOnlySpan<Relocation> CodeRelocations => _methodCode?.Relocs;

        // Identify activation-guaranteeing direct calls from paired frame-identity
        // and call relocations. See #PrecodeFixupMinimization.
        internal MethodWithGCInfo GetDirectCallTarget(int relocationIndex)
        {
            ReadOnlySpan<Relocation> relocations = CodeRelocations;
            Relocation relocation = relocations[relocationIndex];
            if (relocation.RelocType == RelocType.WASM_FUNCTION_INDEX_LEB &&
                relocation.Target is MethodWithGCInfo target && target != this && relocationIndex > 0)
            {
                Relocation identity = relocations[relocationIndex - 1];
                if (identity.RelocType == RelocType.WASM_METHOD_FRAME_IDENTITY_SLEB &&
                    identity.Offset + Relocation.WASM_PADDED_RELOC_SIZE_32 + 1 == relocation.Offset &&
                    identity.Target is WasmMethodFrameIdentityNode { Method: var identityMethod } &&
                    identityMethod == target)
                {
                    return target;
                }
            }

            return null;
        }

        internal Import[] GuaranteedPrecodeFixups => Volatile.Read(ref _guaranteedPrecodeFixups);

        // Remove requirements covered by module activation or direct callees.
        // See #PrecodeFixupMinimization.
        internal void FinalizePrecodeFixups(HashSet<Import> scratch = null)
        {
#if DEBUG
            Debug.Assert(!_precodeFixupsFinalized);
            _precodeFixupsFinalized = true;
#endif

            HashSet<Import> coveredFixups = scratch ?? new HashSet<Import>();
            CollectCoveredPrecodeFixups(coveredFixups);

            if (coveredFixups.Count != 0)
            {
                _fixups.RemoveAll(fixup =>
                    fixup is Import import &&
                    coveredFixups.Contains(import));
            }
            coveredFixups.Clear();
        }

        // Compute and cache the full activation guarantees used by callers and
        // startup-eager promotion. See #PrecodeFixupMinimization.
        internal Import[] GetGuaranteedPrecodeFixups(HashSet<Import> scratch)
        {
            Import[] guarantees = GuaranteedPrecodeFixups;
            if (guarantees is not null)
            {
                return guarantees;
            }

#if DEBUG
            Debug.Assert(_precodeFixupsFinalized);
#endif
            Import[] seed = CollectCoveredPrecodeFixups(scratch);
            foreach (ISymbolNode fixup in _fixups)
            {
                if (fixup is Import import)
                {
                    scratch.Add(import);
                }
            }
            // The union contains seed, so equal counts mean its array can be shared unchanged.
            if (scratch.Count == seed.Length)
            {
                guarantees = seed;
            }
            else
            {
                guarantees = new Import[scratch.Count];
                scratch.CopyTo(guarantees);
            }
            scratch.Clear();

            return Interlocked.CompareExchange(ref _guaranteedPrecodeFixups, guarantees, null) ?? guarantees;
        }

        // Fill coveredFixups with module and direct-callee guarantees; return
        // a reusable array seed, not the complete collected union.
        // See #PrecodeFixupMinimization.
        private Import[] CollectCoveredPrecodeFixups(HashSet<Import> coveredFixups)
        {
            // Materialize callees before using the same scratch storage for this method.
            ReadOnlySpan<Relocation> relocations = CodeRelocations;
            for (int i = 0; i < relocations.Length; i++)
            {
                if (GetDirectCallTarget(i) is MethodWithGCInfo target)
                {
                    target.GetGuaranteedPrecodeFixups(coveredFixups);
                }
            }

            coveredFixups.Clear();
            Import[] seed = Array.Empty<Import>();

            CollectModuleEagerFixups(_methodCode, coveredFixups);
            if (ColdCodeNode is not null)
            {
                CollectModuleEagerFixups(ColdCodeNode.GetData(null), coveredFixups);
            }
            if (_nonRelocationDependencies is not null)
            {
                foreach (DependencyListEntry dependency in _nonRelocationDependencies)
                {
                    if (dependency.Node is Import { Table.IsEager: true } import)
                    {
                        coveredFixups.Add(import);
                    }
                }
            }

            for (int i = 0; i < relocations.Length; i++)
            {
                if (GetDirectCallTarget(i) is MethodWithGCInfo target)
                {
#if DEBUG
                    Debug.Assert(target._precodeFixupsFinalized);
#endif
                    Import[] targetFixups = target.GuaranteedPrecodeFixups;
                    foreach (Import fixup in targetFixups)
                    {
                        coveredFixups.Add(fixup);
                    }
                    if (targetFixups.Length > seed.Length)
                    {
                        seed = targetFixups;
                    }
                }
            }

            return seed;
        }

        // Recover module-eager imports from code and referenced read-only/read-write
        // data. Do not follow those blobs' data references, which may be
        // self-referential. See #PrecodeFixupMinimization.
        private static void CollectModuleEagerFixups(ObjectData data, HashSet<Import> fixups, bool includeDataReferences = true)
        {
            MethodReadOnlyDataNode readOnlyData = null;
            MethodReadWriteDataNode readWriteData = null;
            foreach (Relocation relocation in (data?.Relocs).AsSpan())
            {
                if (relocation.Target is Import { Table.IsEager: true } import)
                {
                    fixups.Add(import);
                }
                else if (includeDataReferences && relocation.Target is MethodReadOnlyDataNode roData && roData != readOnlyData)
                {
                    readOnlyData = roData;
                    CollectModuleEagerFixups(roData.GetData(null), fixups, includeDataReferences: false);
                }
                else if (includeDataReferences && relocation.Target is MethodReadWriteDataNode rwData && rwData != readWriteData)
                {
                    readWriteData = rwData;
                    CollectModuleEagerFixups(rwData.GetData(null), fixups, includeDataReferences: false);
                }
            }
        }

        // Remove precode entries whose resolution has been promoted to module startup.
        // See #PrecodeFixupMinimization.
        internal void ApplyModuleEagerFixups(HashSet<Import> fixups)
        {
#if DEBUG
            Debug.Assert(_precodeFixupsFinalized);
#endif

            _fixups.RemoveAll(fixup =>
                fixup is Import import &&
                fixups.Contains(import));
        }

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
            List<FixupCell> fixupCells = null;

            foreach (ISymbolNode node in _fixups)
            {
                Import fixupCell = (Import)node;
                if (fixupCell.Table.IsEager)
                {
                    continue;
                }

                if (fixupCells == null)
                {
                    fixupCells = new List<FixupCell>();
                }

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

        protected override void ComputeNonRelocationBasedDependencies(DependencySink sink, NodeFactory factory)
        {
            sink.Add(GCInfoNode, "Unwind & GC info");

            if (this.ColdCodeNode != null)
            {
                sink.Add(this.ColdCodeNode, "cold");
            }

            foreach (ISymbolNode node in _fixups)
            {
                sink.Add(node, "classMustBeLoadedBeforeCodeIsRun");
            }

            if (_nonRelocationDependencies != null)
            {
                foreach (DependencyListEntry dependency in _nonRelocationDependencies)
                {
                    sink.Add(dependency);
                }
            }
        }

        public override bool StaticDependenciesAreComputed => _methodCode != null;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public void AddRuntimeDeterminedStaticDependencies(DependencySink sink, NodeFactory factory, MethodDesc concreteMethod)
        {
            if (_nonRelocationDependencies is not null)
            {
                foreach (DependencyListEntry dependency in _nonRelocationDependencies)
                {
                    AddRuntimeDeterminedDependency(sink, factory, concreteMethod, dependency.Node);
                }
            }

            if (_methodCode.Relocs is not null)
            {
                foreach (Relocation relocation in _methodCode.Relocs)
                {
                    AddRuntimeDeterminedDependency(sink, factory, concreteMethod, relocation.Target);
                }
            }
        }

        public void AddRuntimeDeterminedConditionalDependencies(DependencySink sink, NodeFactory factory, MethodDesc concreteMethod)
        {
        }

        private static void AddRuntimeDeterminedDependency(DependencySink sink, NodeFactory factory, MethodDesc concreteMethod, object dependency)
        {
            if (dependency is INodeWithRuntimeDeterminedDependencies runtimeDeterminedDependency)
            {
                runtimeDeterminedDependency.AddDependencies(
                    sink,
                    factory,
                    concreteMethod.OwningType.Instantiation,
                    concreteMethod.Instantiation,
                    isConcreteInstantiation: !concreteMethod.IsSharedByGenericInstantiations,
                    otherReasonNode: null);
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            sb.Append("MethodWithGCInfo("u8);
            AppendMangledName(factory.NameMangler, sb);
            sb.Append(")"u8);
            return sb.ToString();
        }

        public override int ClassCode => 315213488;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return factory.Format switch
            {
                ReadyToRunContainerFormat.PE => ObjectNodeSection.ManagedCodeWindowsContentSection,
                ReadyToRunContainerFormat.Wasm => ObjectNodeSection.WasmCodeSection,
                _ => ObjectNodeSection.ManagedCodeUnixContentSection
            };
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

        public FuncletKind[] GetFuncletKinds() => _funcletKinds ?? [];

        public void InitializeFuncletKinds(FuncletKind[] funcletKinds)
        {
            Debug.Assert(_funcletKinds is null);
            _funcletKinds = funcletKinds;
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            MethodWithGCInfo otherNode = (MethodWithGCInfo)other;
            MethodDesc methodLayoutAnchor = GetMethodLayoutAnchor(_method, out int methodLayoutRank);
            MethodDesc otherLayoutAnchor = GetMethodLayoutAnchor(otherNode._method, out int otherLayoutRank);

            int result = comparer.Compare(methodLayoutAnchor, otherLayoutAnchor);
            if (result != 0)
            {
                return result;
            }

            result = methodLayoutRank.CompareTo(otherLayoutRank);
            if (result != 0)
            {
                return result;
            }

            return comparer.Compare(_method, otherNode._method);

            static MethodDesc GetMethodLayoutAnchor(MethodDesc method, out int layoutRank)
            {
                if (method is AsyncResumptionStub resumptionStub)
                {
                    Debug.Assert(resumptionStub.TargetMethod is not AsyncResumptionStub);
                    layoutRank = 1;
                    return resumptionStub.TargetMethod;
                }

                layoutRank = 0;
                return method;
            }
        }

        public void InitializeInliningInfo(MethodDesc[] inlinedMethods, NodeFactory factory)
        {
            Debug.Assert(_inlinedMethods == null);
            _inlinedMethods = inlinedMethods;
            if (this.Marked)
                RegisterInlineeModuleIndices(factory);
        }

        public void InitializeNonRelocationDependencies(DependencyList dependencies)
        {
            _nonRelocationDependencies = dependencies;
        }

        public int Offset => 0;
        public override bool IsShareable => throw new NotImplementedException();

        public bool AsyncVariant => _method.IsAsyncVariant();

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => IsEmpty;

        public override string ToString() => _method.ToString();
    }
}
