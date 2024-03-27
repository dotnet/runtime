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

            TargetArchitecture targetArchitecture = factory.Target.Architecture;
            if (targetArchitecture == TargetArchitecture.ARM)
            {
                result.Add(factory.InitialInterfaceDispatchStub, "Initial interface dispatch stub");
            }
            else
            {
                result.Add(factory.ExternSymbol("RhpInitialDynamicInterfaceDispatch"), "Initial interface dispatch stub");
            }

            // We counter-intuitively ask for a constructed type symbol. This is needed due to IDynamicInterfaceCastable.
            // If this dispatch cell is ever used with an object that implements IDynamicIntefaceCastable, user code will
            // see a RuntimeTypeHandle representing this interface.
            result.Add(factory.ConstructedTypeSymbol(_targetMethod.OwningType), "Interface type");

            return result;
        }

        public override void EncodeData(ref ObjectDataBuilder objData, NodeFactory factory, bool relocsOnly)
        {
            TargetArchitecture targetArchitecture = factory.Target.Architecture;
            if (targetArchitecture == TargetArchitecture.ARM)
            {
                objData.EmitPointerReloc(factory.InitialInterfaceDispatchStub);
            }
            else
            {
                objData.EmitPointerReloc(factory.ExternSymbol("RhpInitialDynamicInterfaceDispatch"));
            }

            // We counter-intuitively ask for a constructed type symbol. This is needed due to IDynamicInterfaceCastable.
            // If this dispatch cell is ever used with an object that implements IDynamicIntefaceCastable, user code will
            // see a RuntimeTypeHandle representing this interface.
            IEETypeNode interfaceType = factory.ConstructedTypeSymbol(_targetMethod.OwningType);
            if (factory.Target.SupportsRelativePointers)
            {
                if (interfaceType.RepresentsIndirectionCell)
                {
                    objData.EmitReloc(interfaceType, RelocType.IMAGE_REL_BASED_RELPTR32,
                        (int)InterfaceDispatchCellCachePointerFlags.CachePointerIsIndirectedInterfaceRelativePointer);
                }
                else
                {
                    objData.EmitReloc(interfaceType, RelocType.IMAGE_REL_BASED_RELPTR32,
                        (int)InterfaceDispatchCellCachePointerFlags.CachePointerIsInterfaceRelativePointer);
                }

                if (objData.TargetPointerSize == 8)
                {
                    // IMAGE_REL_BASED_RELPTR is a 32-bit relocation. However, the cell needs a full pointer
                    // width there since a pointer to the cache will be written into the cell. Emit additional
                    // 32 bits on targets whose pointer size is 64 bit.
                    objData.EmitInt(0);
                }
            }
            else
            {
                // There are no free bits in the cache flags, but we could support the indirection cell case
                // by repurposing "CachePointerIsIndirectedInterfaceRelativePointer" to mean "relative indirect
                // if the target supports it, simple indirect otherwise".
                Debug.Assert(!interfaceType.RepresentsIndirectionCell);
                objData.EmitPointerReloc(interfaceType,
                    (int)InterfaceDispatchCellCachePointerFlags.CachePointerIsInterfacePointerOrMetadataToken);
            }
        }

        public override int ClassCode => -2023802120;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(_targetMethod, ((InterfaceDispatchCellNode)other)._targetMethod);
            return compare != 0 ? compare : comparer.Compare(_callSiteIdentifier, ((InterfaceDispatchCellNode)other)._callSiteIdentifier);
        }
    }
}
