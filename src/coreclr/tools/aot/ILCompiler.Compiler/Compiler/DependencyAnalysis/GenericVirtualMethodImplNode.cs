// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a use of a generic virtual method body implementation.
    /// </summary>
    public class GenericVirtualMethodImplNode : DependencyNodeCore<NodeFactory>
    {
        private const int UniversalCanonGVMDepthHeuristic_CanonDepth = 2;
        private readonly MethodDesc _method;

        public GenericVirtualMethodImplNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.HasInstantiation);

            // This is either a generic virtual method or a MethodImpl for a static interface method.
            // We can't test for static MethodImpl so at least sanity check it's static and noninterface.
            Debug.Assert(method.IsVirtual || (method.Signature.IsStatic && !method.OwningType.IsInterface));

            _method = method;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => "__GVMImplNode_" + factory.NameMangler.GetMangledMethodName(_method);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;

            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref dependencies, factory, _method);

            bool validInstantiation =
                _method.IsSharedByGenericInstantiations || (      // Non-exact methods are always valid instantiations (always pass constraints check)
                    _method.Instantiation.CheckValidInstantiationArguments() &&
                    _method.OwningType.Instantiation.CheckValidInstantiationArguments() &&
                    _method.CheckConstraints());

            if (validInstantiation)
            {
                if (factory.TypeSystemContext.SupportsUniversalCanon && _method.IsGenericDepthGreaterThan(UniversalCanonGVMDepthHeuristic_CanonDepth))
                {
                    // fall back to using the universal generic variant of the generic method
                    return dependencies;
                }

                bool getUnboxingStub = _method.OwningType.IsValueType && !_method.Signature.IsStatic;
                dependencies ??= new DependencyList();
                dependencies.Add(factory.MethodEntrypoint(_method, getUnboxingStub), "GVM Dependency - Canon method");

                if (_method.IsSharedByGenericInstantiations)
                {
                    dependencies.Add(factory.NativeLayout.TemplateMethodEntry(_method), "GVM Dependency - Template entry");
                    dependencies.Add(factory.NativeLayout.TemplateMethodLayout(_method), "GVM Dependency - Template");
                }
                else
                {
                    dependencies.Add(factory.ExactMethodInstantiationsHashtableEntry(_method), "GVM Dependency - runtime lookups");
                }
            }

            return dependencies;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;

        public override bool HasDynamicDependencies => false;

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
