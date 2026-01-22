// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class InterfaceDispatchCellNode : SortableDependencyNode, ISymbolDefinitionNode
    {
        private const int InvalidOffset = -1;

        private readonly MethodDesc _targetMethod;
        private readonly ISortableSymbolNode _callSiteIdentifier;

        private int _offset;

        internal MethodDesc TargetMethod => _targetMethod;

        internal ISortableSymbolNode CallSiteIdentifier => _callSiteIdentifier;

        public InterfaceDispatchCellNode(MethodDesc targetMethod, ISortableSymbolNode callSiteIdentifier)
        {
            Debug.Assert(targetMethod.OwningType.IsInterface);
            Debug.Assert(!targetMethod.IsSharedByGenericInstantiations);
            _targetMethod = targetMethod;
            _callSiteIdentifier = callSiteIdentifier;
            _offset = InvalidOffset;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix)
                .Append("__InterfaceDispatchCell_"u8)
                .Append(nameMangler.GetMangledMethodName(_targetMethod));

            if (_callSiteIdentifier != null)
            {
                sb.Append('_');
                _callSiteIdentifier.AppendMangledName(nameMangler, sb);
            }
        }

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                Debug.Assert(_offset != InvalidOffset);
                return _offset;
            }
        }

        int ISymbolNode.Offset => 0;

        public int Size
        {
            get
            {
                // The size of the dispatch cell is 2 * PointerSize:
                // a cached thisObj MethodTable, and a code pointer.
                return _targetMethod.Context.Target.PointerSize * 2;
            }
        }

        public void InitializeOffset(int offset)
        {
            Debug.Assert(_offset == InvalidOffset);
            _offset = offset;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        internal IEETypeNode GetInterfaceTypeNode(NodeFactory factory)
        {
            // If this dispatch cell is ever used with an object that implements IDynamicIntefaceCastable, user code will
            // see a RuntimeTypeHandle representing this interface.
            if (factory.DevirtualizationManager.CanHaveDynamicInterfaceImplementations(_targetMethod.OwningType))
            {
                return factory.ConstructedTypeSymbol(_targetMethod.OwningType);
            }
            else
            {
                return factory.NecessaryTypeSymbol(_targetMethod.OwningType);
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            if (!factory.VTable(_targetMethod.OwningType).HasKnownVirtualMethodUse)
            {
                result.Add(factory.VirtualMethodUse(_targetMethod), "Interface method use");
            }

            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref result, factory, _targetMethod);

            result.Add(GetInterfaceTypeNode(factory), "Interface type");

            return result;
        }

        public override int ClassCode => -2023802120;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(_targetMethod, ((InterfaceDispatchCellNode)other)._targetMethod);
            return compare != 0 ? compare : comparer.Compare(_callSiteIdentifier, ((InterfaceDispatchCellNode)other)._callSiteIdentifier);
        }

        public bool RepresentsIndirectionCell => false;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
