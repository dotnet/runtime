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
        private readonly GCStaticsPreInitDataNode _preinitializationInfo;

        public GCStaticsNode(MetadataType type, PreinitializationManager preinitManager)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;

            if (preinitManager.IsPreinitialized(type))
            {
                var info = preinitManager.GetPreinitializationInfo(_type);
                _preinitializationInfo = new GCStaticsPreInitDataNode(info);
            }
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
            bool requiresAlign8 = _type.GCStaticFieldAlignment.AsInt > factory.Target.PointerSize;
            return factory.GCStaticEEType(map, requiresAlign8);
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

            return dependencyList;
        }

        public override bool HasConditionalStaticDependencies => _type.ConvertToCanonForm(CanonicalFormKind.Specific) != _type;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            // If we have a type loader template for this type, we need to keep track of the generated
            // bases in the type info hashtable. The type symbol node does such accounting.
            return new CombinedDependencyListEntry[]
            {
                new CombinedDependencyListEntry(factory.NecessaryTypeSymbol(_type),
                    factory.NativeLayout.TemplateTypeLayout(_type.ConvertToCanonForm(CanonicalFormKind.Specific)),
                    "Keeping track of template-constructable type static bases"),
            };
        }

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.DataSection;
        public override bool IsShareable => EETypeNode.IsTypeNodeShareable(_type);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);

            // Even though we're only generating 32-bit relocs here (if SupportsRelativePointers),
            // align the blob at pointer boundary since at runtime we're going to write a pointer in here.
            builder.RequireInitialPointerAlignment();

            int delta = GCStaticRegionConstants.Uninitialized;

            // Set the flag that indicates next pointer following MethodTable is the preinit data
            bool isPreinitialized = _preinitializationInfo != null;
            if (isPreinitialized)
                delta |= GCStaticRegionConstants.HasPreInitializedData;

            if (factory.Target.SupportsRelativePointers)
                builder.EmitReloc(GetGCStaticEETypeNode(factory), RelocType.IMAGE_REL_BASED_RELPTR32, delta);
            else
                builder.EmitPointerReloc(GetGCStaticEETypeNode(factory), delta);

            if (isPreinitialized)
            {
                if (factory.Target.SupportsRelativePointers)
                    builder.EmitReloc(_preinitializationInfo, RelocType.IMAGE_REL_BASED_RELPTR32);
                else
                    builder.EmitPointerReloc(_preinitializationInfo);
            }
            else if (factory.Target.SupportsRelativePointers && factory.Target.PointerSize == 8)
            {
                // At runtime, we replace the EEType pointer with a full pointer to the data on the GC
                // heap. If the EEType pointer was 32-bit relative, and we don't have a 32-bit relative
                // pointer to the preinit data following it, and the pointer size on the target
                // machine is 8, we need to emit additional 4 bytes to make room for the full pointer.
                builder.EmitZeros(4);
            }

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
