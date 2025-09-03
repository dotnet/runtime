// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Runtime;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public sealed class MetadataEETypeNode : EETypeNode
    {
        public MetadataEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
        }

        protected override bool IsReflectionVisible => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler) + " with metadata";

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            // If there is a constructed version of this node in the graph, emit that instead
            if (ConstructedEETypeNode.CreationAllowed(_type))
                return factory.ConstructedTypeSymbol(_type).Marked;

            return false;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = base.ComputeNonRelocationBasedDependencies(factory);

            // Ensure that we track the necessary type symbol if we are working with a metadata type symbol.
            // The emitter will ensure we don't emit both, but this allows us assert that we only generate
            // relocs to nodes we emit.
            dependencyList.Add(factory.NecessaryTypeSymbol(_type), "NecessaryType for metadata type");

            if (_type is MetadataType mdType)
                ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref dependencyList, factory, mdType.Module);

            // Ask the metadata manager if we have any dependencies due to the presence of the EEType.
            factory.MetadataManager.GetDependenciesDueToEETypePresence(ref dependencyList, factory, _type);

            // Reflection-visible valuetypes are considered constructed due to APIs like RuntimeHelpers.Box,
            // or Enum.ToObject.
            if (_type.IsEnum)
                dependencyList.Add(factory.MaximallyConstructableType(_type), "Reflection visible valuetype");

            // Delegates can be constructed through runtime magic APIs so consider constructed.
            if (_type.IsDelegate)
                dependencyList.Add(factory.MaximallyConstructableType(_type), "Reflection visible delegate");

            // Arrays can be constructed through Array.CreateInstanceFromArrayType so consider constructed.
            if (_type.IsArray)
                dependencyList.Add(factory.MaximallyConstructableType(_type), "Reflection visible array");

            // TODO-SIZE: We need to separate tracking the use of static and instance virtual methods
            // Unconstructed MethodTables only need to track the static virtuals.
            // For now, conservatively upgrade to constructed types when static virtuals are present
            bool hasStaticVirtuals = false;
            foreach (MetadataType intface in _type.RuntimeInterfaces)
            {
                foreach (MethodDesc intfaceMethod in intface.GetAllVirtualMethods())
                {
                    if (intfaceMethod.Signature.IsStatic)
                    {
                        hasStaticVirtuals = true;
                        break;
                    }
                }
            }
            if (hasStaticVirtuals)
                dependencyList.Add(factory.MaximallyConstructableType(_type), "Has static virtual methods");

            return dependencyList;
        }

        protected override ISymbolNode GetBaseTypeNode(NodeFactory factory)
        {
            return _type.BaseType != null ? factory.MetadataTypeSymbol(_type.BaseType.NormalizeInstantiation()) : null;
        }

        protected override FrozenRuntimeTypeNode GetFrozenRuntimeTypeNode(NodeFactory factory)
        {
            return factory.SerializedMetadataRuntimeTypeObject(_type);
        }

        protected override ISymbolNode GetNonNullableValueTypeArrayElementTypeNode(NodeFactory factory)
        {
            return factory.MetadataTypeSymbol(((ArrayType)_type).ElementType);
        }

        protected override IEETypeNode GetInterfaceTypeNode(NodeFactory factory, TypeDesc interfaceType)
        {
            return factory.MetadataTypeSymbol(interfaceType);
        }

        public override int ClassCode => 99298112;
    }
}
