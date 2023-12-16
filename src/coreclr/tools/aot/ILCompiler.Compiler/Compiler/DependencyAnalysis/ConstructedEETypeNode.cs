// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Runtime;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ConstructedEETypeNode : EETypeNode
    {
        public ConstructedEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
            CheckCanGenerateConstructedEEType(factory, type);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler) + " constructed";

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => false;

        protected override bool EmitVirtualSlots => true;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = base.ComputeNonRelocationBasedDependencies(factory);

            // Ensure that we track the necessary type symbol if we are working with a constructed type symbol.
            // The emitter will ensure we don't emit both, but this allows us assert that we only generate
            // relocs to nodes we emit.
            dependencyList.Add(factory.NecessaryTypeSymbol(_type), "NecessaryType for constructed type");

            if (_type is MetadataType mdType)
                ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref dependencyList, factory, mdType.Module);

            DefType closestDefType = _type.GetClosestDefType();

            if (_type.IsArray)
            {
                // Array MethodTable depends on System.Array's virtuals. Array EETypes don't point to
                // their base type (i.e. there's no reloc based dependency making this "just work").
                dependencyList.Add(factory.ConstructedTypeSymbol(_type.BaseType), "Array base type");

                ArrayType arrayType = (ArrayType)_type;
                if (arrayType.IsMdArray && arrayType.Rank == 1)
                {
                    // Allocating an MDArray of Rank 1 with zero lower bounds results in allocating
                    // an SzArray instead. Make sure the type loader can find the SzArray type.
                    dependencyList.Add(factory.ConstructedTypeSymbol(arrayType.ElementType.MakeArrayType()), "Rank 1 array");
                }
            }

            dependencyList.Add(factory.VTable(closestDefType), "VTable");

            if (factory.TypeSystemContext.SupportsUniversalCanon)
            {
                foreach (var instantiationType in _type.Instantiation)
                {
                    if (instantiationType.IsValueType)
                    {
                        // All valuetype generic parameters of a constructed type may be effectively constructed. This is generally not that
                        // critical, but in the presence of universal generics the compiler may generate a Box followed by calls to ToString,
                        // GetHashcode or Equals in ways that cannot otherwise be detected by dependency analysis. Thus force all struct type
                        // generic parameters to be considered constructed when walking dependencies of a constructed generic
                        dependencyList.Add(factory.ConstructedTypeSymbol(instantiationType.ConvertToCanonForm(CanonicalFormKind.Specific)),
                        "Struct generic parameters in constructed types may be assumed to be used as constructed in constructed generic types");
                    }
                }
            }

            // Ask the metadata manager if we have any dependencies due to the presence of the EEType.
            factory.MetadataManager.GetDependenciesDueToEETypePresence(ref dependencyList, factory, _type);

            factory.InteropStubManager.AddInterestingInteropConstructedTypeDependencies(ref dependencyList, factory, _type);

            return dependencyList;
        }

        protected override ISymbolNode GetBaseTypeNode(NodeFactory factory)
        {
            return _type.BaseType != null ? factory.ConstructedTypeSymbol(_type.BaseType) : null;
        }

        protected override FrozenRuntimeTypeNode GetFrozenRuntimeTypeNode(NodeFactory factory)
        {
            return factory.SerializedConstructedRuntimeTypeObject(_type);
        }

        protected override ISymbolNode GetNonNullableValueTypeArrayElementTypeNode(NodeFactory factory)
        {
            return factory.ConstructedTypeSymbol(((ArrayType)_type).ElementType);
        }

        protected override IEETypeNode GetInterfaceTypeNode(NodeFactory factory, TypeDesc interfaceType)
        {
            // The interface type will be visible to reflection and should be considered constructed.
            return factory.ConstructedTypeSymbol(interfaceType);
        }

        protected override int GCDescSize => GCDescEncoder.GetGCDescSize(_type);

        protected override void OutputGCDesc(ref ObjectDataBuilder builder)
        {
            GCDescEncoder.EncodeGCDesc(ref builder, _type);
        }

        public static bool CreationAllowed(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                case TypeFlags.ByRef:
                    // Pointers and byrefs are not boxable
                    return false;
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                    // TODO: any validation for arrays?
                    break;

                default:
                    // Generic definition EETypes can't be allocated
                    if (type.IsGenericDefinition)
                        return false;

                    // Full MethodTable of System.Canon should never be used.
                    if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                        return false;

                    // The global "<Module>" type can never be allocated.
                    if (((MetadataType)type).IsModuleType)
                        return false;

                    break;
            }

            return true;
        }

        public static void CheckCanGenerateConstructedEEType(NodeFactory factory, TypeDesc type)
        {
            if (!CreationAllowed(type))
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadGeneral, type);
        }

        public override int ClassCode => 590142654;
    }
}
