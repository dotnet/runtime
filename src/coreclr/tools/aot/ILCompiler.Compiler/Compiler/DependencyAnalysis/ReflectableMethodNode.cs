// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that is visible to reflection.
    /// </summary>
    public class ReflectableMethodNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ReflectableMethodNode(MethodDesc method)
        {
            Debug.Assert(!method.IsCanonicalMethod(CanonicalFormKind.Any) ||
                method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            _method = method;
        }

        public MethodDesc Method => _method;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_method.GetTypicalMethodDefinition()));

            DependencyList dependencies = new DependencyList();
            factory.MetadataManager.GetDependenciesDueToReflectability(ref dependencies, factory, _method);

            // No runtime artifacts needed if this is a generic definition
            if (_method.IsGenericMethodDefinition || _method.OwningType.IsGenericDefinition)
            {
                return dependencies;
            }

            // Ensure we consistently apply reflectability to all methods sharing the same definition.
            // Different instantiations of the method have a conditional dependency on the definition node that
            // brings a ReflectableMethod of the instantiated method if it's necessary for it to be reflectable.
            MethodDesc typicalMethod = _method.GetTypicalMethodDefinition();
            if (typicalMethod != _method)
            {
                dependencies.Add(factory.ReflectableMethod(typicalMethod), "Definition of the reflectable method");
            }

            MethodDesc canonMethod = _method.GetCanonMethodTarget(CanonicalFormKind.Specific);
            if (canonMethod != _method)
            {
                dependencies.Add(factory.ReflectableMethod(canonMethod), "Canonical version of the reflectable method");
            }

            // Make sure we generate the method body and other artifacts.
            if (MetadataManager.IsMethodSupportedInReflectionInvoke(_method))
            {
                if (_method.IsVirtual)
                {
                    if (_method.HasInstantiation)
                    {
                        dependencies.Add(factory.GVMDependencies(_method.GetCanonMethodTarget(CanonicalFormKind.Specific)), "GVM callable reflectable method");
                    }
                    else
                    {
                        // Virtual method use is tracked on the slot defining method only.
                        MethodDesc slotDefiningMethod = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(_method);
                        if (!factory.VTable(slotDefiningMethod.OwningType).HasFixedSlots)
                            dependencies.Add(factory.VirtualMethodUse(slotDefiningMethod), "Virtually callable reflectable method");
                    }
                }

                if (!_method.IsAbstract)
                {
                    dependencies.Add(factory.MethodEntrypoint(canonMethod), "Body of a reflectable method");

                    if (_method.HasInstantiation
                        && _method != canonMethod)
                        dependencies.Add(factory.MethodGenericDictionary(_method), "Dictionary of a reflectable method");
                }
            }

            return dependencies;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable method: " + _method.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
