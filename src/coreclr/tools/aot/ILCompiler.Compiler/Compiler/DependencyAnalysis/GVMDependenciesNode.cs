// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a use of a generic virtual method slot. This node only tracks
    /// the use of the slot definition.
    /// This analysis node is used for computing GVM dependencies for the following cases:
    ///    1) Derived types where the GVM is overridden
    ///    2) Variant-interfaces GVMs
    /// This analysis node will ensure that the proper GVM instantiations are compiled on types.
    /// We only analyze the canonical forms of generic virtual methods to limit the amount of generic
    /// expansion we need to deal with in the compiler.
    /// </summary>
    public class GVMDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public GVMDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.HasInstantiation);
            Debug.Assert(method.IsVirtual);
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method) == method);

            _method = method;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => "__GVMDependenciesNode_" + factory.NameMangler.GetMangledMethodName(_method);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            if (!_method.IsAbstract)
            {
                yield return new DependencyListEntry(factory.GenericVirtualMethodImpl(_method), "Implementation of the generic virtual method");
            }

            if (!_method.OwningType.IsInterface)
            {
                yield return new DependencyListEntry(factory.TypeGVMEntries(_method.OwningType.GetTypeDefinition()), "Resolution metadata");
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;

        public override bool HasDynamicDependencies
        {
            get
            {
                TypeDesc methodOwningType = _method.OwningType;

                // SearchDynamicDependencies wouldn't come up with anything for these
                if (!methodOwningType.IsInterface &&
                    (methodOwningType.IsSealed() || _method.IsFinal))
                    return false;

                return true;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            List<CombinedDependencyListEntry> dynamicDependencies = new List<CombinedDependencyListEntry>();

            TypeDesc methodOwningType = _method.OwningType;
            bool methodIsShared = _method.IsSharedByGenericInstantiations;

            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                DependencyNodeCore<NodeFactory> entry = markedNodes[i];

                // This method is often called with a long list of ScannedMethodNode
                // or MethodCodeNode nodes. We are not interested in those. In order
                // to make the type check as cheap as possible we check for specific
                // *sealed* types instead of doing `entry is EETypeNode` which has
                // to walk the whole class hierarchy for the non matching nodes.
                if (entry is not ConstructedEETypeNode constructedEETypeNode)
                    continue;

                TypeDesc potentialOverrideType = constructedEETypeNode.Type;
                if (!potentialOverrideType.IsDefType || potentialOverrideType.IsInterface)
                    continue;

                // If method is canonical, don't allow using it with non-canonical types - we can wait until
                // we see the __Canon instantiation. If there isn't one, the canonical method wouldn't be useful anyway.
                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

                bool foundImpl = false;

                if (methodOwningType.IsInterface)
                {
                    foreach (MethodDesc implementingMethod in potentialOverrideType.ResolveCanonicalInterfaceMethodImplementations(_method, out foundImpl))
                    {
                        MethodDesc canonImpl = implementingMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                        // Static virtuals cannot be further overridden so this is an impl use. Otherwise it's a virtual slot use.
                        if (implementingMethod.Signature.IsStatic)
                            dynamicDependencies.Add(new CombinedDependencyListEntry(factory.GenericVirtualMethodImpl(canonImpl), null, "ImplementingMethodInstantiation"));
                        else
                            dynamicDependencies.Add(new CombinedDependencyListEntry(factory.GVMDependencies(canonImpl), null, "ImplementingMethodInstantiation"));

                        TypeSystemEntity origin = (implementingMethod.OwningType != potentialOverrideType) ? potentialOverrideType : null;
                        factory.MetadataManager.NoteOverridingMethod(_method, implementingMethod, origin);
                    }
                }
                else
                {
                    MethodDesc canonTarget = potentialOverrideType.ResolveCanonicalClassVirtualMethodOverride(_method);
                    if (canonTarget is not null)
                    {
                        dynamicDependencies.Add(new CombinedDependencyListEntry(
                            factory.GenericVirtualMethodImpl(canonTarget), null, "DerivedMethodInstantiation"));

                        factory.MetadataManager.NoteOverridingMethod(_method, canonTarget);

                        foundImpl = true;
                    }
                }

                if (foundImpl)
                {
                    TypeDesc currentType = potentialOverrideType;
                    do
                    {
                        dynamicDependencies.Add(new CombinedDependencyListEntry(factory.TypeGVMEntries(currentType.GetTypeDefinition()), null, "Resolution metadata"));
                        currentType = currentType.BaseType;
                    }
                    while (currentType != null);
                }
            }

            return dynamicDependencies;
        }
    }
}
