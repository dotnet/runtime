// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    // This node represents the concept of a variant interface method being used.
    // It has no direct dependencies, but may be referred to by conditional static
    // dependencies, or static dependencies from elsewhere.
    //
    // We only track the generic definition of the interface method being used.
    // There's a potential optimization opportunity because e.g. the fact that
    // IEnumerable<string>.GetEnumerator() is used doesn't mean that
    // IEnumerable<char>.GetEnumerator() is used, but this complicates the tracking.
    internal class VariantInterfaceMethodUseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _decl;

        public VariantInterfaceMethodUseNode(MethodDesc decl)
        {
            Debug.Assert(!decl.IsRuntimeDeterminedExactMethod);
            Debug.Assert(decl.IsVirtual);
            Debug.Assert(decl.OwningType.IsInterface);

            // Virtual method use always represents the slot defining method of the virtual.
            // Places that might see virtual methods being used through an override need to normalize
            // to the slot defining method.
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(decl) == decl);

            // Generic virtual methods are tracked by an orthogonal mechanism.
            Debug.Assert(!decl.HasInstantiation);

            // We currently track this for definitions only
            Debug.Assert(decl.IsTypicalMethodDefinition);

            _decl = decl;
        }

        public static bool IsVariantInterfaceImplementation(NodeFactory factory, TypeDesc providingType, DefType implementedInterface)
        {
            Debug.Assert(implementedInterface.IsInterface);
            Debug.Assert(!implementedInterface.IsGenericDefinition);

            // If any of the implemented interfaces have variance, calls against compatible interface methods
            // could result in interface methods of this type being used (e.g. IEnumerable<object>.GetEnumerator()
            // can dispatch to an implementation of IEnumerable<string>.GetEnumerator()).
            bool result = false;
            if (implementedInterface.HasVariance)
            {
                TypeDesc interfaceDefinition = implementedInterface.GetTypeDefinition();
                for (int i = 0; i < interfaceDefinition.Instantiation.Length; i++)
                {
                    var variantParameter = (GenericParameterDesc)interfaceDefinition.Instantiation[i];
                    if (variantParameter.Variance != 0)
                    {
                        // Variant interface parameters that are instantiated over valuetypes are
                        // not actually variant. Neither are contravariant parameters instantiated
                        // over sealed types (there won't be another interface castable to it
                        // through variance on that parameter).
                        TypeDesc variantArgument = implementedInterface.Instantiation[i];
                        if (!variantArgument.IsValueType
                            && (!variantArgument.IsSealed() || variantParameter.IsCovariant))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            if (!result &&
                (providingType.IsArray || providingType.GetTypeDefinition() == factory.ArrayOfTEnumeratorType) &&
                implementedInterface.HasInstantiation)
            {
                // We need to also do this for generic interfaces on arrays because they have a weird casting rule
                // that doesn't require the implemented interface to be variant to consider it castable.
                // For value types, we only need this when the array is castable by size (int[] and ICollection<uint>),
                // or it's a reference type (Derived[] and ICollection<Base>).
                TypeDesc elementType = providingType.IsArray ? ((ArrayType)providingType).ElementType : providingType.Instantiation[0];
                result =
                    CastingHelper.IsArrayElementTypeCastableBySize(elementType) ||
                    (elementType.IsDefType && !elementType.IsValueType);
            }

            return result;
        }

        public static bool IsVariantMethodCall(NodeFactory factory, MethodDesc calledMethod)
        {
            Debug.Assert(calledMethod.IsVirtual);

            TypeDesc owningType = calledMethod.OwningType;

            if (!owningType.IsInterface)
                return false;

            bool result = false;
            if (owningType.HasVariance)
            {
                TypeDesc owningTypeDefinition = owningType.GetTypeDefinition();
                for (int i = 0; i < owningTypeDefinition.Instantiation.Length; i++)
                {
                    var variantParameter = (GenericParameterDesc)owningTypeDefinition.Instantiation[i];
                    if (variantParameter.Variance != 0)
                    {
                        // Variant interface parameters that are instantiated over valuetypes are
                        // not actually variant. Neither are contravariant parameters instantiated
                        // over sealed types (there won't be another interface castable to it
                        // through variance on that parameter).
                        TypeDesc variantArgument = owningType.Instantiation[i];
                        if (!variantArgument.IsValueType
                            && (!variantArgument.IsSealed() || variantParameter.IsCovariant))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            if (!result && factory.TypeSystemContext.IsGenericArrayInterfaceType(owningType))
            {
                // We need to also do this for generic interfaces on arrays because they have a weird casting rule
                // that doesn't require the implemented interface to be variant to consider it castable.
                // For value types, we only need this when the array is castable by size (int[] and ICollection<uint>),
                // or it's a reference type (Derived[] and ICollection<Base>).
                TypeDesc elementType = owningType.Instantiation[0];
                result =
                    CastingHelper.IsArrayElementTypeCastableBySize(elementType) ||
                    (elementType.IsDefType && !elementType.IsValueType);
            }

            return result;
        }

        protected override string GetName(NodeFactory factory) => $"VariantInterfaceMethodUse {_decl.ToString()}";

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
