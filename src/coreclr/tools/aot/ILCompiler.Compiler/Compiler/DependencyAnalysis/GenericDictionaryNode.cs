// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a generic dictionary for a concrete generic type instantiation
    /// or generic method instantiation. The dictionary is used from canonical code
    /// at runtime to look up runtime artifacts that depend on the concrete
    /// context the generic type or method was instantiated with.
    /// </summary>
    public abstract class GenericDictionaryNode : DehydratableObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly NodeFactory _factory;

        protected abstract TypeSystemContext Context { get; }

        public abstract TypeSystemEntity OwningEntity { get; }

        public abstract Instantiation TypeInstantiation { get; }

        public abstract Instantiation MethodInstantiation { get; }

        public abstract DictionaryLayoutNode GetDictionaryLayout(NodeFactory factory);

        public sealed override bool StaticDependenciesAreComputed => true;

        public sealed override bool IsShareable => true;

        int ISymbolNode.Offset => 0;

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);

        protected abstract int HeaderSize { get; }

        int ISymbolDefinitionNode.Offset => HeaderSize;

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory)
            => GetDictionaryLayout(_factory).DictionarySection(_factory);

        public GenericDictionaryNode(NodeFactory factory)
        {
            _factory = factory;
        }

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.RequireInitialPointerAlignment();

            DictionaryLayoutNode layout = GetDictionaryLayout(factory);

            // Node representing the generic dictionary layout might be one of two kinds:
            // With fixed slots, or where slots are added as we're expanding the graph.
            // If it's the latter, we can't touch the collection of slots before the graph expansion
            // is complete (relocsOnly == false). It's someone else's responsibility
            // to make sure the dependencies are properly generated.
            // If this is a dictionary layout with fixed slots, it's the responsibility of
            // each dictionary to ensure the targets are marked.
            if (layout.HasFixedSlots || !relocsOnly)
            {
                // TODO: pass the layout we already have to EmitDataInternal
                EmitDataInternal(ref builder, factory, relocsOnly);
            }

            return builder.ToObjectData();
        }

        protected virtual void EmitDataInternal(ref ObjectDataBuilder builder, NodeFactory factory, bool fixedLayoutOnly)
        {
            DictionaryLayoutNode layout = GetDictionaryLayout(factory);
            layout.EmitDictionaryData(ref builder, factory, this, fixedLayoutOnly: fixedLayoutOnly);
        }

        protected sealed override string GetName(NodeFactory factory)
        {
            return this.GetMangledName(factory.NameMangler);
        }

        public override int ClassCode => ClassCode;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return CompareToImpl((ObjectNode)other, comparer);
        }
    }

    public sealed class TypeGenericDictionaryNode : GenericDictionaryNode
    {
        private TypeDesc _owningType;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.TypeGenericDictionary(_owningType));
        }

        protected override int HeaderSize => 0;
        public override Instantiation TypeInstantiation => _owningType.Instantiation;
        public override Instantiation MethodInstantiation => default(Instantiation);
        protected override TypeSystemContext Context => _owningType.Context;
        public override TypeSystemEntity OwningEntity => _owningType;
        public TypeDesc OwningType => _owningType;

        public override DictionaryLayoutNode GetDictionaryLayout(NodeFactory factory)
        {
            return factory.GenericDictionaryLayout(_owningType.ConvertToCanonForm(CanonicalFormKind.Specific));
        }

        public override bool HasConditionalStaticDependencies => true;

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => GetDictionaryLayout(factory).IsEmpty;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            // Include the layout as a dependency if the canonical type isn't imported
            TypeDesc canonicalOwningType = _owningType.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (factory.CompilationModuleGroup.ContainsType(canonicalOwningType) || !factory.CompilationModuleGroup.ShouldReferenceThroughImportTable(canonicalOwningType))
                result.Add(GetDictionaryLayout(factory), "Layout");

            // Lazy generic use of the Activator.CreateInstance<T> heuristic requires tracking type parameters that are used in lazy generics.
            if (factory.LazyGenericsPolicy.UsesLazyGenerics(_owningType))
            {
                foreach (var arg in _owningType.Instantiation)
                {
                    // Skip types that do not have a default constructor (not interesting).
                    if (arg.IsValueType || arg.GetDefaultConstructor() == null || !ConstructedEETypeNode.CreationAllowed(arg))
                        continue;

                    result.Add(new DependencyListEntry(
                        factory.ConstructedTypeSymbol(arg.ConvertToCanonForm(CanonicalFormKind.Specific)),
                        "Default constructor for lazy generics"));
                }
            }

            return result;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            // The generic dictionary layout is shared between all the canonically equivalent
            // instantiations. We need to track the dependencies of all canonical method bodies
            // that use the same dictionary layout.
            foreach (var method in _owningType.GetAllMethods())
            {
                if (!EETypeNode.MethodHasNonGenericILMethodBody(method))
                    continue;

                // If a canonical method body was compiled, we need to track the dictionary
                // dependencies in the context of the concrete type that owns this dictionary.
                yield return new CombinedDependencyListEntry(
                    factory.ShadowConcreteMethod(method),
                    factory.MethodEntrypoint(method.GetCanonMethodTarget(CanonicalFormKind.Specific)),
                    "Generic dictionary dependency");
            }
        }

        public TypeGenericDictionaryNode(TypeDesc owningType, NodeFactory factory)
            : base(factory)
        {
            Debug.Assert(!owningType.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(!owningType.IsRuntimeDeterminedSubtype);
            Debug.Assert(owningType.HasInstantiation);
            Debug.Assert(owningType.ConvertToCanonForm(CanonicalFormKind.Specific) != owningType);

            _owningType = owningType;
        }

        public override int ClassCode => 889700584;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_owningType, ((TypeGenericDictionaryNode)other)._owningType);
        }
    }

    public sealed class MethodGenericDictionaryNode : GenericDictionaryNode
    {
        private MethodDesc _owningMethod;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.NodeMangler.MethodGenericDictionary(_owningMethod));
        }
        protected override int HeaderSize => _owningMethod.Context.Target.PointerSize;
        public override Instantiation TypeInstantiation => _owningMethod.OwningType.Instantiation;
        public override Instantiation MethodInstantiation => _owningMethod.Instantiation;
        protected override TypeSystemContext Context => _owningMethod.Context;
        public override TypeSystemEntity OwningEntity => _owningMethod;
        public MethodDesc OwningMethod => _owningMethod;
        public override bool HasConditionalStaticDependencies => false;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            MethodDesc canonicalTarget = _owningMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
            if (factory.CompilationModuleGroup.ContainsMethodBody(canonicalTarget, false))
                dependencies.Add(GetDictionaryLayout(factory), "Layout");

            // TODO-SIZE: We probably don't need to add these for all dictionaries
            GenericMethodsHashtableNode.GetGenericMethodsHashtableDependenciesForMethod(ref dependencies, factory, _owningMethod);

            factory.InteropStubManager.AddMarshalAPIsGenericDependencies(ref dependencies, factory, _owningMethod);

            // Lazy generic use of the Activator.CreateInstance<T> heuristic requires tracking type parameters that are used in lazy generics.
            if (factory.LazyGenericsPolicy.UsesLazyGenerics(_owningMethod))
            {
                foreach (var arg in _owningMethod.OwningType.Instantiation)
                {
                    // Skip types that do not have a default constructor (not interesting).
                    if (arg.IsValueType || arg.GetDefaultConstructor() == null || !ConstructedEETypeNode.CreationAllowed(arg))
                        continue;

                    dependencies.Add(new DependencyListEntry(
                        factory.ConstructedTypeSymbol(arg.ConvertToCanonForm(CanonicalFormKind.Specific)),
                        "Default constructor for lazy generics"));
                }
                foreach (var arg in _owningMethod.Instantiation)
                {
                    // Skip types that do not have a default constructor (not interesting).
                    if (arg.IsValueType || arg.GetDefaultConstructor() == null || !ConstructedEETypeNode.CreationAllowed(arg))
                        continue;

                    dependencies.Add(new DependencyListEntry(
                        factory.ConstructedTypeSymbol(arg.ConvertToCanonForm(CanonicalFormKind.Specific)),
                        "Default constructor for lazy generics"));
                }
            }

            // Make sure the dictionary can also be populated
            dependencies.Add(factory.ShadowConcreteMethod(_owningMethod), "Dictionary contents");

            return dependencies;
        }

        public override DictionaryLayoutNode GetDictionaryLayout(NodeFactory factory)
        {
            return factory.GenericDictionaryLayout(_owningMethod.GetCanonMethodTarget(CanonicalFormKind.Specific));
        }

        protected override void EmitDataInternal(ref ObjectDataBuilder builder, NodeFactory factory, bool fixedLayoutOnly)
        {
            // Method generic dictionaries get prefixed by the hash code of the owning method
            // to allow quick lookups of additional details by the type loader.

            builder.EmitInt(_owningMethod.GetHashCode());
            if (builder.TargetPointerSize == 8)
                builder.EmitInt(0);

            Debug.Assert(builder.CountBytes == ((ISymbolDefinitionNode)this).Offset);

            // Lazy method dictionaries are generated by the compiler, but they have no entries within them. (They are used solely to identify the exact method)
            // The dictionary layout may be filled in by various needs for generic lookups, but those are handled in a lazy fashion.
            if (factory.LazyGenericsPolicy.UsesLazyGenerics(OwningMethod))
                return;

            base.EmitDataInternal(ref builder, factory, fixedLayoutOnly);
        }

        public MethodGenericDictionaryNode(MethodDesc owningMethod, NodeFactory factory)
            : base(factory)
        {
            Debug.Assert(!owningMethod.IsSharedByGenericInstantiations);
            Debug.Assert(owningMethod.HasInstantiation);
            Debug.Assert(owningMethod.GetCanonMethodTarget(CanonicalFormKind.Specific) != owningMethod);

            _owningMethod = owningMethod;
        }

        public override int ClassCode => -1245704203;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_owningMethod, ((MethodGenericDictionaryNode)other)._owningMethod);
        }
    }
}
