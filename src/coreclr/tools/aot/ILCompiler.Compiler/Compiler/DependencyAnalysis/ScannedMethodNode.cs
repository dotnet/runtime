// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that should be scanned by an IL scanner and its dependencies
    /// analyzed.
    /// </summary>
    public class ScannedMethodNode : DependencyNodeCore<NodeFactory>, IMethodBodyNode
    {
        private readonly MethodDesc _method;
        private DependencyList _dependencies;
        private CombinedDependencyList _conditionalDependencies;

        // If we failed to scan the method, the dependencies reported by the node will
        // be for a throwing method body. This field will store the underlying cause of the failure.
        private TypeSystemException _exception;

        public ScannedMethodNode(MethodDesc method)
        {
            Debug.Assert(!method.IsAbstract);
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            _method = method;
        }

        public MethodDesc Method => _method;

        public TypeSystemException Exception => _exception;

        public int Offset => 0;

        public bool RepresentsIndirectionCell => false;

        public override bool HasConditionalStaticDependencies => _conditionalDependencies != null;

        public override bool StaticDependenciesAreComputed => _dependencies != null;

        public void InitializeDependencies(NodeFactory factory, (DependencyList, CombinedDependencyList) dependencies, TypeSystemException scanningException = null)
        {
            _dependencies = dependencies.Item1;
            _conditionalDependencies = dependencies.Item2;

            if (factory.TypeSystemContext.IsSpecialUnboxingThunk(_method))
            {
                // Special unboxing thunks reference a MethodAssociatedDataNode that points to the non-unboxing version.
                // This dependency is redundant with the dependency list we constructed above, with a notable
                // exception of special unboxing thunks for byref-like types. Those don't actually unbox anything
                // and their body is a dummy. We capture the dependency here.
                MethodDesc nonUnboxingMethod = factory.TypeSystemContext.GetTargetOfSpecialUnboxingThunk(_method);
                _dependencies.Add(new DependencyListEntry(factory.MethodEntrypoint(nonUnboxingMethod, false), "Non-unboxing method"));
            }

            TypeDesc owningType = _method.OwningType;
            if (factory.PreinitializationManager.HasEagerStaticConstructor(owningType))
            {
                _dependencies.Add(factory.EagerCctorIndirection(owningType.GetStaticConstructor()), "Eager .cctor");
            }

            _exception = scanningException;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            Debug.Assert(_dependencies != null);
            return _dependencies;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => _conditionalDependencies;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
        public override bool InterestingForDynamicDependencyAnalysis => _method.HasInstantiation || _method.OwningType.HasInstantiation;
        public override bool HasDynamicDependencies => false;

        int ISortableNode.ClassCode => -1381809560;

        int ISortableNode.CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(Method, ((ScannedMethodNode)other).Method);
        }
    }
}
