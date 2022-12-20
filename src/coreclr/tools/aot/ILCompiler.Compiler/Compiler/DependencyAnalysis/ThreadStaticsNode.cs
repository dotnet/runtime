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

        public ThreadStaticsNode(MetadataType type, NodeFactory factory)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;
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
            sb.Append(GetMangledName(_type, nameMangler));
        }

        private ISymbolNode GetGCStaticEETypeNode(NodeFactory factory)
        {
            GCPointerMap map = GCPointerMap.FromThreadStaticLayout(_type);
            return factory.GCStaticEEType(map);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            result.Add(new DependencyListEntry(GetGCStaticEETypeNode(factory), "ThreadStatic MethodTable"));

            if (factory.PreinitializationManager.HasEagerStaticConstructor(_type))
            {
                result.Add(new DependencyListEntry(factory.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor"));
            }

            ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref result, factory, _type.Module);

            EETypeNode.AddDependenciesForStaticsNode(factory, _type, ref result);
            return result;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override void EncodeData(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            // At runtime, an instance of the GCStaticEEType will be created and a GCHandle to it
            // will be written in this location.
            builder.RequireInitialPointerAlignment();
            builder.EmitPointerReloc(GetGCStaticEETypeNode(factory));
        }

        public override int ClassCode => 2091208431;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((ThreadStaticsNode)other)._type);
        }
    }
}
