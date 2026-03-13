// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.ReadyToRunConstants;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis.Wasm;
using Internal.JitInterface;
using Internal.TypeSystem;
using System.Collections.Generic;
using ILCompiler.ObjectWriter.WasmInstructions;
using System;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public enum ImportThunkKind
    {
        Eager,
        Lazy,
        DelayLoadHelper,
        DelayLoadHelperWithExistingIndirectionCell,
        VirtualStubDispatch,
    }

    public class WasmImportThunkPortableEntrypoint : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        Import _import;
        ReadyToRunHelper _helperId;
        bool _useVirtualCall;
        bool _useJumpableStub;

        public WasmImportThunkPortableEntrypoint(NodeFactory factory, Import import, ReadyToRunHelper helperId, bool useVirtualCall, bool useJumpableStub)
        {
            _import = import;
            _helperId = helperId;
            _useVirtualCall = useVirtualCall;
            _useJumpableStub = useJumpableStub;
            Debug.Assert(import.Signature is MethodFixupSignature);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmImportThunkPortableEntrypoint->"u8);
            _import.AppendMangledName(nameMangler, sb);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override int ClassCode => 1738294057;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmImportThunkPortableEntrypoint otherNode = (WasmImportThunkPortableEntrypoint)other;
            int result = _import.CompareToImpl((Import)otherNode._import, comparer);
            if (result != 0)
                return result;

            Debug.Assert(otherNode._helperId == _helperId);
            Debug.Assert(_useVirtualCall == otherNode._useVirtualCall);
            Debug.Assert(_useJumpableStub == otherNode._useJumpableStub);

            return 0;
        }

        public override ObjectData GetData(NodeFactory factory, System.Boolean relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder();
            builder.AddSymbol(this);
            builder.EmitReloc(_import, RelocType.IMAGE_REL_BASED_ADDR32NB);
            var typeNode = factory.WasmTypeNode(((MethodFixupSignature)(_import.Signature)).Method);

            builder.EmitReloc(factory.WasmImportThunk(typeNode, _helperId, _import.Table, _useVirtualCall, _useJumpableStub), RelocType.IMAGE_REL_BASED_ADDR32NB);
            return builder.ToObjectData();
        }
    }

    public partial class WasmImportThunk : ObjectNode, INodeWithTypeSignature, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeSystemContext _context;
        private readonly Import _helperCell;
        private readonly WasmTypeNode _typeNode;

        private readonly ImportThunkKind _thunkKind;

        private readonly ImportSectionNode _containingImportSection;

        public int Offset => 0;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        /// <summary>
        /// Import thunks are used to call a runtime-provided helper which fixes up an indirection cell in a particular
        /// import section. Optionally they may also contain a relocation for a specific indirection cell to fix up.
        /// </summary>
        public WasmImportThunk(NodeFactory factory, WasmTypeNode typeNode, ReadyToRunHelper helperId, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
        {
            _context = factory.TypeSystemContext;
            _typeNode = typeNode;
            _helperCell = factory.GetReadyToRunHelperCell(helperId);
            _containingImportSection = containingImportSection;

            if (useVirtualCall)
            {
                throw new System.Exception();
            }
            else if (useJumpableStub)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelperWithExistingIndirectionCell;
            }
            else if (helperId == ReadyToRunHelper.GetString)
            {
                throw new System.Exception();
            }
            else if (helperId == ReadyToRunHelper.DelayLoad_MethodCall ||
                helperId == ReadyToRunHelper.DelayLoad_Helper ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_Obj ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_ObjObj)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelper;
            }
            else
            {
                throw new System.Exception();
            }
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("WasmDelayLoadHelper->"u8);
            _helperCell.AppendMangledName(nameMangler, sb);
            sb.Append($"(ImportSection:{_containingImportSection.Name},Kind:{_thunkKind})");
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 948271336;

        MethodSignature INodeWithTypeSignature.Signature => WasmLowering.RaiseSignature(_typeNode.Type, _context);

        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => false;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            WasmImportThunk otherNode = (WasmImportThunk)other;
            int result = ((int)_thunkKind).CompareTo((int)otherNode._thunkKind);
            if (result != 0)
                return result;

            result = _typeNode.CompareToImpl(otherNode._typeNode, comparer);
            if (result != 0)
                return result;

            result = ((ImportSectionNode)_containingImportSection).CompareToImpl((ImportSectionNode)otherNode._containingImportSection, comparer);
            if (result != 0)
                return result;

            return comparer.Compare(_helperCell, otherNode._helperCell);
        }

        public override ObjectData GetData(NodeFactory factory, System.Boolean relocsOnly = false)
        {
            Debug.Assert(_thunkKind == ImportThunkKind.DelayLoadHelper);
            ISymbolNode helperTypeIndex = factory.WasmTypeNode(new CorInfoWasmType[] { CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32, CorInfoWasmType.CORINFO_WASM_TYPE_I32 });
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), new Relocation[] {
                    new Relocation(RelocType.WASM_TYPE_INDEX_LEB,0, _typeNode),
                    new Relocation(RelocType.WASM_TYPE_INDEX_LEB,0, helperTypeIndex),
                    new Relocation(RelocType.WASM_MEMORY_ADDR_SLEB,0, _helperCell)
                }
                , 0, new ISymbolDefinitionNode[] { this });
            }
            // The arguments are $sp, ARG0-ARGN, PortableEntrypointThunk.
            // The general logic is...
            // Compute stack offset needed.
            int currentOffset = 0;

            foreach (WasmValueType type in _typeNode.Type.Params.Types)
            {
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
            }

            // Align stack to 16 byte boundaries
            int sizeOfStoredLocals = AlignmentHelper.AlignUp(currentOffset, 16);

            List<WasmExpr> expressions = new List<WasmExpr>();
            // local.get 0
            expressions.Add(Local.Get(0));
            // i32.const {sizeOfStoredLocals}
            expressions.Add(I32.Const(currentOffset));
            // i32.sub
            expressions.Add(I32.Sub);
            // local.tee 0
            expressions.Add(Local.Tee(0));

            // table.set {stack pointer global}  // This is a callout from managed to native, we need to set the global stack pointer so that C++ code will work
            expressions.Add(Global.Set(0));

            //
            // ; Stash all the locals away
            // for (int i = 0; i < N; i++)
            // {
            //   local.get 0
            //   local.get (i+1)
            //   i32(or i64/fp32/fp64).store (offset from base)
            // }

            // In the calling convention, the first arg is the sp arg, and the last is the portable entrypoint arg. Each of those are treated specially
            currentOffset = 0;
            for (int i = 1; i < _typeNode.Type.Params.Types.Length - 1; i++)
            {
                expressions.Add(Local.Get(0));
                expressions.Add(Local.Get(i));
                WasmValueType type = _typeNode.Type.Params.Types[i];
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        if (type == WasmValueType.I32)
                        {
                            I32.Store((ulong)currentOffset);
                        }
                        else
                        {
                            F32.Store((ulong)currentOffset);
                        }
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        if (type == WasmValueType.I32)
                        {
                            I64.Store((ulong)currentOffset);
                        }
                        else
                        {
                            F64.Store((ulong)currentOffset);
                        }
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        V128.Store((ulong)currentOffset);
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
            }
            //
            // ; Call the right helper to fill in the table
            // local.get (PortableEntrypointThunk)
            int portableEntrypointLocalIndex = _typeNode.Type.Params.Types.Length - 1;
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            // table.get {module base}
            expressions.Add(Global.Get(1)); // Module base?
            // i32.const (RVA of R2RHelperID)
            expressions.Add(I32.ConstRVA(_helperCell));
            // i32.add
            expressions.Add(I32.Add);
            // i32.load 0
            expressions.Add(I32.Load(0));
            // call_indirect (i32, i32, i32) (returns i32)
            expressions.Add(ControlFlow.CallIndirect(helperTypeIndex, 0));

            // local.set (PortableEntrypointThunk)  // At this point we can overwrite the incoming portable entrypoint local, since it will no longer be used
            expressions.Add(Local.Set(portableEntrypointLocalIndex));
            //
            // ;Setup sp arg
            // local.get 0
            expressions.Add(Local.Get(0));
            // i32.const {sizeofstoredlocals}
            expressions.Add(I32.Const(sizeOfStoredLocals));
            // i32.add
            expressions.Add(I32.Add);
            //
            // ; Setup normal args
            // for (int i = 0; i < N; i++)
            // {
            //   local.get 0
            //   i32(or i64/fp32/fp64).load (offset from base)
            //   local.set (i+1)
            // }
            // In the calling convention, the first arg is the sp arg, and the last is the portable entrypoint arg. Each of those are treated specially
            currentOffset = 0;
            for (int i = 1; i < _typeNode.Type.Params.Types.Length - 1; i++)
            {
                expressions.Add(Local.Get(0));
                WasmValueType type = _typeNode.Type.Params.Types[i];
                switch (type)
                {
                    case WasmValueType.I32:
                    case WasmValueType.F32:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 4);
                        if (type == WasmValueType.I32)
                        {
                            I32.Load((ulong)currentOffset);
                        }
                        else
                        {
                            F32.Load((ulong)currentOffset);
                        }
                        currentOffset += 4;
                        break;
                    case WasmValueType.I64:
                    case WasmValueType.F64:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 8);
                        if (type == WasmValueType.I32)
                        {
                            I64.Load((ulong)currentOffset);
                        }
                        else
                        {
                            F64.Load((ulong)currentOffset);
                        }
                        currentOffset += 8;
                        break;
                    case WasmValueType.V128:
                        currentOffset = AlignmentHelper.AlignUp(currentOffset, 16);
                        V128.Load((ulong)currentOffset);
                        currentOffset += 16;
                        break;

                    default:
                        throw new System.Exception("Unexpected wasm type arg");
                }
                expressions.Add(Local.Set(i));
            }
            // ; Add the portable entrypoint arg
            // local.get (PortableEntrypointThunk)
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            //
            // ; Load the actual target to jump to
            // local.get (PortableEntrypointThunk)
            expressions.Add(Local.Get(portableEntrypointLocalIndex));
            // i32.load 0
            expressions.Add(I32.Load(0));
            // return_call_indirect (actual type index)  ; We can use return_call_index here, or call_index. Semantically they are identical
            expressions.Add(ControlFlow.CallIndirect(_typeNode, 0));

            // Encode as a complete function body
            WasmFunctionBody funcBody = new WasmFunctionBody(_typeNode.Type, expressions.ToArray());
            byte[] encodedThunk = new byte[funcBody.EncodeSize()];
            funcBody.Encode(encodedThunk.AsSpan());
            return new ObjectData(encodedThunk, funcBody.GetRelocations(), 1, new ISymbolDefinitionNode[] { this });
        }
    }

    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk : AssemblyStubNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly Import _helperCell;

        private readonly ImportThunkKind _thunkKind;

        private readonly ImportSectionNode _containingImportSection;

        /// <summary>
        /// Import thunks are used to call a runtime-provided helper which fixes up an indirection cell in a particular
        /// import section. Optionally they may also contain a relocation for a specific indirection cell to fix up.
        /// </summary>
        public ImportThunk(NodeFactory factory, ReadyToRunHelper helperId, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
        {
            _helperCell = factory.GetReadyToRunHelperCell(helperId);
            _containingImportSection = containingImportSection;

            if (useVirtualCall)
            {
                _thunkKind = ImportThunkKind.VirtualStubDispatch;
            }
            else if (useJumpableStub)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelperWithExistingIndirectionCell;
            }
            else if (helperId == ReadyToRunHelper.GetString)
            {
                _thunkKind = ImportThunkKind.Lazy;
            }
            else if (helperId == ReadyToRunHelper.DelayLoad_MethodCall ||
                helperId == ReadyToRunHelper.DelayLoad_Helper ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_Obj ||
                helperId == ReadyToRunHelper.DelayLoad_Helper_ObjObj)
            {
                _thunkKind = ImportThunkKind.DelayLoadHelper;
            }
            else
            {
                _thunkKind = ImportThunkKind.Eager;
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("DelayLoadHelper->"u8);
            _helperCell.AppendMangledName(nameMangler, sb);
            sb.Append($"(ImportSection:{_containingImportSection.Name},Kind:{_thunkKind})");
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        public override int ClassCode => 433266948;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ImportThunk otherNode = (ImportThunk)other;
            int result = ((int)_thunkKind).CompareTo((int)otherNode._thunkKind);
            if (result != 0)
                return result;

            result = ((ImportSectionNode)_containingImportSection).CompareToImpl((ImportSectionNode)otherNode._containingImportSection, comparer);
            if (result != 0)
                return result;

            return comparer.Compare(_helperCell, otherNode._helperCell);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            Debug.Assert(base.ComputeNonRelocationBasedDependencies(factory) == null);
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.DelayLoadMethodCallThunks, "MethodCallThunksList");
            return dependencies;
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.DelayLoadMethodCallThunks.OnNodeInRangeMarked(this);
        }
    }
}
