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
        private List<INodeWithRuntimeDeterminedDependencies> _runtimeDependencies;

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
                _runtimeDependencies = new List<INodeWithRuntimeDeterminedDependencies>();
                return Array.Empty<DependencyListEntry>();
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            MethodDesc analyzedMethod = _methodIL.OwningMethod;

            // Look for any generic specialization of this method. If any are found, specialize any dataflow dependencies.
            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                if (markedNodes[i] is not IMethodBodyNode methodBody)
                    continue;

                MethodDesc method = methodBody.Method;
                if (method.GetTypicalMethodDefinition() != analyzedMethod)
                    continue;

                // Instantiate all runtime dependencies for the found generic specialization
                foreach (var n in _runtimeDependencies)
                {
                    foreach (var d in n.InstantiateDependencies(factory, method.OwningType.Instantiation, method.Instantiation))
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
