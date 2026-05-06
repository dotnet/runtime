// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.IL;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Node that performs dataflow analysis to find dynamic dependencies (e.g. reflection use) of the given method.
    /// </summary>
    public class DataflowAnalyzedMethodNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodIL _methodIL;
        private List<(MethodDesc OwningMethod, INodeWithRuntimeDeterminedDependencies Dependency)> _runtimeDependencies;

        public DataflowAnalyzedMethodNode(MethodIL methodIL)
        {
            Debug.Assert(methodIL.OwningMethod.IsTypicalMethodDefinition);
            Debug.Assert(!CompilerGeneratedState.IsNestedFunctionOrStateMachineMember(methodIL.OwningMethod));
            _methodIL = methodIL;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;
            try
            {
                return Dataflow.ReflectionMethodBodyScanner.ScanAndProcessReturnValue(factory, mdManager.FlowAnnotations, mdManager.Logger, _methodIL, out _runtimeDependencies);
            }
            catch (TypeSystemException)
            {
                // Something wrong with the input - missing references, etc.
                // The method body likely won't compile either, so we don't care.
                _runtimeDependencies = new List<(MethodDesc, INodeWithRuntimeDeterminedDependencies)>();
                return Array.Empty<DependencyListEntry>();
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            // Look for any generic specialization of this method or its compiler-generated callees (local methods, lambdas).
            // If any are found, specialize the dataflow dependencies that originated from that method.
            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                if (markedNodes[i] is not IMethodBodyNode methodBody)
                    continue;

                MethodDesc method = methodBody.Method;
                MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

                // Instantiate runtime dependencies whose owning method matches this method body's typical definition
                foreach (var n in _runtimeDependencies)
                {
                    if (n.OwningMethod != typicalMethod)
                        continue;

                    foreach (var d in n.Dependency.InstantiateDependencies(factory, method.OwningType.Instantiation, method.Instantiation, isConcreteInstantiation: !method.IsSharedByGenericInstantiations))
                    {
                        yield return new CombinedDependencyListEntry(d.Node, null, d.Reason);
                    }
                }
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Dataflow analysis for " + _methodIL.OwningMethod.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => _runtimeDependencies.Count > 0;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
    }
}
