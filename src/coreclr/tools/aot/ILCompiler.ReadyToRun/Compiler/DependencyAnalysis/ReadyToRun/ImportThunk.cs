// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;
using System.Diagnostics;

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
