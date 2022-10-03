// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using GCStaticRegionConstants = Internal.Runtime.GCStaticRegionConstants;

namespace ILCompiler.DependencyAnalysis
{
    public class GCStaticsNode : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly MetadataType _type;
        private readonly TypePreinit.PreinitializationInfo _preinitializationInfo;

        public GCStaticsNode(MetadataType type, PreinitializationManager preinitManager)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;

            if (preinitManager.IsPreinitialized(type))
                _preinitializationInfo = preinitManager.GetPreinitializationInfo(_type);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.GCStatics(_type));
        }

        public int Offset => 0;
        public MetadataType Type => _type;

        public static string GetMangledName(TypeDesc type, NameMangler nameMangler)
        {
            return nameMangler.NodeMangler.GCStatics(type);
        }

        private ISymbolNode GetGCStaticEETypeNode(NodeFactory factory)
        {
            GCPointerMap map = GCPointerMap.FromStaticLayout(_type);
            return factory.GCStaticEEType(map);
        }

        public GCStaticsPreInitDataNode NewPreInitDataNode()
        {
            Debug.Assert(_preinitializationInfo != null && _preinitializationInfo.IsPreinitialized);
            return new GCStaticsPreInitDataNode(_preinitializationInfo);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = new DependencyList();

            if (factory.PreinitializationManager.HasEagerStaticConstructor(_type))
            {
                dependencyList.Add(factory.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor");
            }

            ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref dependencyList, factory, _type.Module);

            dependencyList.Add(factory.GCStaticsRegion, "GCStatics Region");

            dependencyList.Add(factory.GCStaticIndirection(_type), "GC statics indirection");
            EETypeNode.AddDependenciesForStaticsNode(factory, _type, ref dependencyList);

            return dependencyList;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section => ObjectNodeSection.DataSection;
        public override bool IsShareable => EETypeNode.IsTypeNodeShareable(_type);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);

            builder.RequireInitialPointerAlignment();

            int delta = GCStaticRegionConstants.Uninitialized;

            // Set the flag that indicates next pointer following MethodTable is the preinit data
            bool isPreinitialized = _preinitializationInfo != null && _preinitializationInfo.IsPreinitialized;
            if (isPreinitialized)
                delta |= GCStaticRegionConstants.HasPreInitializedData;
                
            builder.EmitPointerReloc(GetGCStaticEETypeNode(factory), delta);

            if (isPreinitialized)
                builder.EmitPointerReloc(factory.GCStaticsPreInitDataNode(_type));

            builder.AddSymbol(this);

            return builder.ToObjectData();
        }

        public override int ClassCode => -522346696;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((GCStaticsNode)other)._type);
        }
    }
}
