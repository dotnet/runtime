// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    // This node represents the concept of a virtual method being used.
    // It has no direct dependencies, but may be referred to by conditional static
    // dependencies, or static dependencies from elsewhere.
    //
    // It is used to keep track of uses of virtual methods to ensure that the
    // vtables are properly constructed
    internal sealed class VirtualMethodUseNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _decl;

        public MethodDesc Method => _decl;

        public VirtualMethodUseNode(MethodDesc decl)
        {
            Debug.Assert(!decl.IsRuntimeDeterminedExactMethod);
            Debug.Assert(decl.IsVirtual);

            // Virtual method use always represents the slot defining method of the virtual.
            // Places that might see virtual methods being used through an override need to normalize
            // to the slot defining method.
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(decl) == decl);

            // Generic virtual methods are tracked by an orthogonal mechanism.
            Debug.Assert(!decl.HasInstantiation);

            _decl = decl;
        }

        protected override string GetName(NodeFactory factory) => $"VirtualMethodUse {_decl}";

        protected override void OnMarked(NodeFactory factory)
        {
            // If the VTable slice is getting built on demand, the fact that the virtual method is used means
            // that the slot is used.
            var lazyVTableSlice = factory.VTable(_decl.OwningType) as LazilyBuiltVTableSliceNode;
            lazyVTableSlice?.AddEntry(_decl);
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            MethodDesc canonDecl = _decl.GetCanonMethodTarget(CanonicalFormKind.Specific);
            if (canonDecl != _decl)
                dependencies.Add(factory.VirtualMethodUse(canonDecl), "Canonical method");

            dependencies.Add(factory.VTable(_decl.OwningType), "VTable of a VirtualMethodUse");

            // Do not report things like Foo<object, __Canon>.Frob().
            if (!_decl.IsCanonicalMethod(CanonicalFormKind.Any) || canonDecl == _decl)
                factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref dependencies, factory, _decl);

            if (VariantInterfaceMethodUseNode.IsVariantMethodCall(factory, _decl))
                dependencies.Add(factory.VariantInterfaceMethodUse(_decl.GetTypicalMethodDefinition()), "Variant interface call");

            return dependencies;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
