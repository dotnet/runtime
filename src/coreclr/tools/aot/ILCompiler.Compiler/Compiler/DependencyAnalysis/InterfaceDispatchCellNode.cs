// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class InterfaceDispatchCellNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private readonly MethodDesc _targetMethod;
        private readonly string _callSiteIdentifier;

        internal MethodDesc TargetMethod => _targetMethod;

        internal string CallSiteIdentifier => _callSiteIdentifier;

        public InterfaceDispatchCellNode(MethodDesc targetMethod, string callSiteIdentifier)
        {
            Debug.Assert(targetMethod.OwningType.IsInterface);
            Debug.Assert(!targetMethod.IsSharedByGenericInstantiations);
            _targetMethod = targetMethod;
            _callSiteIdentifier = callSiteIdentifier;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(GetMangledName(nameMangler, _targetMethod, _callSiteIdentifier));
        }

        int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;

        int ISymbolNode.Offset => 0;

        public override bool IsShareable => false;

        public static string GetMangledName(NameMangler nameMangler, MethodDesc method, string callSiteIdentifier)
        {
            string name = nameMangler.CompilationUnitPrefix + "__InterfaceDispatchCell_" + nameMangler.GetMangledMethodName(method);

            if (!string.IsNullOrEmpty(callSiteIdentifier))
            {
                name += "_" + callSiteIdentifier;
            }

            return name;
        }

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

            result.Add(factory.NecessaryTypeSymbol(_targetMethod.OwningType), "Interface type");

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

            IEETypeNode interfaceType = factory.NecessaryTypeSymbol(_targetMethod.OwningType);
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

        protected override void OnMarked(NodeFactory factory)
        {
            factory.InterfaceDispatchCellSection.AddEmbeddedObject(this);
        }

        public override int ClassCode => -2023802120;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = comparer.Compare(_targetMethod, ((InterfaceDispatchCellNode)other)._targetMethod);
            return compare != 0 ? compare : string.Compare(_callSiteIdentifier, ((InterfaceDispatchCellNode)other)._callSiteIdentifier);
        }
    }
}
