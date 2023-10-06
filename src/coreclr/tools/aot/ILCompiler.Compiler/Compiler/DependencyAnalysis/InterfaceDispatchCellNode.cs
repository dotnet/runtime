// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class InterfaceDispatchCellNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private readonly MethodDesc _targetMethod;
        private readonly ISortableSymbolNode _callSiteIdentifier;

        internal MethodDesc TargetMethod => _targetMethod;

        internal ISortableSymbolNode CallSiteIdentifier => _callSiteIdentifier;

        public InterfaceDispatchCellNode(MethodDesc targetMethod, ISortableSymbolNode callSiteIdentifier)
        {
            Debug.Assert(targetMethod.OwningType.IsInterface);
            Debug.Assert(!targetMethod.IsSharedByGenericInstantiations);
            _targetMethod = targetMethod;
            _callSiteIdentifier = callSiteIdentifier;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix)
                .Append("__InterfaceDispatchCell_")
                .Append(nameMangler.GetMangledMethodName(_targetMethod));

            if (_callSiteIdentifier != null)
            {
                sb.Append('_');
                _callSiteIdentifier.AppendMangledName(nameMangler, sb);
            }
        }

        int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;

        int ISymbolNode.Offset => 0;

        public override bool IsShareable => false;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            if (!factory.VTable(_targetMethod.OwningType).HasFixedSlots)
            {
                result.Add(factory.VirtualMethodUse(_targetMethod), "Interface method use");
            }

            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref result, factory, _targetMethod);

            result.Add(factory.NecessaryTypeSymbol(_targetMethod.OwningType), "Interface type");

            return result;
        }

        public override void EncodeData(ref ObjectDataBuilder objData, NodeFactory factory, bool relocsOnly)
        {
            objData.EmitPointerReloc(factory.NecessaryTypeSymbol(_targetMethod.OwningType));
        }

        public override int ClassCode => -2023802120;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(_targetMethod, ((InterfaceDispatchCellNode)other)._targetMethod);
            return compare != 0 ? compare : comparer.Compare(_callSiteIdentifier, ((InterfaceDispatchCellNode)other)._callSiteIdentifier);
        }
    }
}
