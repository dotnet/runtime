// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Tracks usage of a virtual or interface method call and dynamically discovers
    /// implementations on types as they are added to the dependency graph.
    ///
    /// For each discovered concrete type (InheritedVirtualMethodsNode as an "interesting" type marker),
    /// this node resolves the method implementation and adds it as a compilation dependency. Used to
    /// find implementations that have non canonical generic type arguments.
    /// </summary>
    public class ReadyToRunVirtualMethodDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ReadyToRunVirtualMethodDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.IsVirtual);

            _method = method;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override bool HasDynamicDependencies => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            // InheritedVirtualMethodsNode is only added during phase 0 (in TypeFixupSignature),
            // so no new interesting nodes will appear in later phases.
            if (factory.CompilationCurrentPhase > 0)
                return Array.Empty<CombinedDependencyListEntry>();

            List<CombinedDependencyListEntry> dynamicDependencies = new List<CombinedDependencyListEntry>();

            bool methodIsShared = _method.IsSharedByGenericInstantiations;

            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                DependencyNodeCore<NodeFactory> entry = markedNodes[i];

                if (entry is not InheritedVirtualMethodsNode typeNode)
                    continue;

                TypeDesc potentialOverrideType = typeNode.Type;

                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

                if (_method.OwningType.IsInterface)
                {
                    foreach (MethodDesc impl in potentialOverrideType.ResolveCanonicalInterfaceMethodImplementations(_method, out _))
                    {
                        AddMethodDependency(dynamicDependencies, factory, impl.GetCanonMethodTarget(CanonicalFormKind.Specific));
                    }
                }
                else
                {
                    MethodDesc canonTarget = potentialOverrideType.ResolveCanonicalClassVirtualMethodOverride(_method);
                    if (canonTarget is not null)
                    {
                        AddMethodDependency(dynamicDependencies, factory, canonTarget);
                    }
                }
            }

            return dynamicDependencies;
        }

        private void AddMethodDependency(
            List<CombinedDependencyListEntry> dynamicDependencies,
            NodeFactory factory,
            MethodDesc canonMethod)
        {
            if (!factory.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
                return;

            try
            {
                factory.DetectGenericCycles(_method, canonMethod);
                dynamicDependencies.Add(new CombinedDependencyListEntry(
                    factory.CompiledMethodNode(canonMethod), null, "Virtual dispatch implementation on discovered type"));
            }
            catch (TypeSystemException) { }
        }

        protected override string GetName(NodeFactory factory) =>
            "__ReadyToRunVirtualMethodDependencies_" + factory.NameMangler.GetMangledMethodName(_method);
    }
}
