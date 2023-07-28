// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the thread static region of a given type. This is very similar to <see cref="GCStaticsNode"/>,
    /// since the actual storage will be allocated on the GC heap at runtime and is allowed to contain GC pointers.
    /// </summary>
    public class ThreadStaticsNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private MetadataType _type;
        private InlinedThreadStatics _inlined;

        public ThreadStaticsNode(MetadataType type, NodeFactory factory)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;
        }

        public ThreadStaticsNode(InlinedThreadStatics inlined, NodeFactory factory)
        {
            _inlined = inlined;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected override void OnMarked(NodeFactory factory)
        {
            factory.ThreadStaticsRegion.AddEmbeddedObject(this);
        }

        public static string GetMangledName(TypeDesc type, NameMangler nameMangler)
        {
            return nameMangler.NodeMangler.ThreadStatics(type);
        }

        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            string mangledName = _type == null ? "_inlinedThreadStatics" : GetMangledName(_type, nameMangler);
            sb.Append(mangledName);
        }

        private ISymbolNode GetGCStaticEETypeNode(NodeFactory factory)
        {
            GCPointerMap map = _type != null ?
                GCPointerMap.FromThreadStaticLayout(_type) :
                GCPointerMap.FromInlinedThreadStatics(
                    _inlined.GetTypes(),
                    _inlined.GetOffsets(),
                    _inlined.GetSize(),
                    factory.Target.PointerSize);

            return factory.GCStaticEEType(map);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            result.Add(new DependencyListEntry(GetGCStaticEETypeNode(factory), "ThreadStatic MethodTable"));

            if (_type != null)
            {

                if (factory.PreinitializationManager.HasEagerStaticConstructor(_type))
                {
                    result.Add(new DependencyListEntry(factory.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor"));
                }

                ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref result, factory, _type.Module);
            }
            else
            {
                foreach (var type in _inlined.GetTypes())
                {
                    if (factory.PreinitializationManager.HasEagerStaticConstructor(type))
                    {
                        result.Add(new DependencyListEntry(factory.EagerCctorIndirection(type.GetStaticConstructor()), "Eager .cctor"));
                    }

                    ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref result, factory, type.Module);
                }
            }

            return result;
        }

        public override bool HasConditionalStaticDependencies =>
            _type != null ?
                _type.ConvertToCanonForm(CanonicalFormKind.Specific) != _type:
                false;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            Debug.Assert(_type != null);

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

        public override void EncodeData(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            // At runtime, an instance of the GCStaticEEType will be created and a GCHandle to it
            // will be written in this location.
            builder.RequireInitialPointerAlignment();
            builder.EmitPointerReloc(GetGCStaticEETypeNode(factory));
        }

        public MetadataType Type => _type;

        public override int ClassCode => 2091208431;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            // force the type of the storage block for inlined threadstatics to be "less"
            // than other storage blocks, - to ensure it is serialized as the item #0
            if (_type == null)
            {
                // there should only be at most one inlined storage type.
                Debug.Assert(other != null);
                return -1;
            }

            return comparer.Compare(_type, ((ThreadStaticsNode)other)._type);
        }
    }
}
