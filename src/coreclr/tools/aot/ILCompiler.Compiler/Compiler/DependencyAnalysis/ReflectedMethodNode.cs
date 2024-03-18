// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that is visible to reflection.
    /// The method can be on a non-generic type, generic type definition, or an instantiatied type.
    /// To match IL semantics, we maintain that a method on a generic type will be consistently
    /// reflection-accessible. Either the method is accessible on all instantiations or on none of them.
    /// Similar invariants hold for generic methods.
    /// </summary>
    public class ReflectedMethodNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public ReflectedMethodNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
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
                dependencies.Add(factory.ReflectedMethod(typicalMethod), "Definition of the reflectable method");
            }

            // Make sure we generate the method body and other artifacts.
            if (MetadataManager.IsMethodSupportedInReflectionInvoke(_method))
            {
                if (_method.IsVirtual)
                {
                    // Virtual method use is tracked on the slot defining method only.
                    MethodDesc slotDefiningMethod = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(_method);
                    if (_method.HasInstantiation)
                    {
                        // FindSlotDefiningMethod might uninstantiate. We might want to fix the method not to do that.
                        if (slotDefiningMethod.IsMethodDefinition)
                            slotDefiningMethod = factory.TypeSystemContext.GetInstantiatedMethod(slotDefiningMethod, _method.Instantiation);
                        dependencies.Add(factory.GVMDependencies(slotDefiningMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)), "GVM callable reflectable method");
                    }
                    else
                    {
                        if (ReflectionVirtualInvokeMapNode.NeedsVirtualInvokeInfo(factory, slotDefiningMethod) && !factory.VTable(slotDefiningMethod.OwningType).HasFixedSlots)
                            dependencies.Add(factory.VirtualMethodUse(slotDefiningMethod), "Virtually callable reflectable method");
                    }
                }

                if (!_method.IsAbstract)
                {
                    dependencies.Add(factory.MethodEntrypoint(_method), "Body of a reflectable method");
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
