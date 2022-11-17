// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using EcmaMethod = Internal.TypeSystem.Ecma.EcmaMethod;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that has metadata generated in the current compilation.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class MethodMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public MethodMetadataNode(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            _method = method;
        }

        public MethodDesc Method => _method;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.TypeMetadata((MetadataType)_method.OwningType), "Owning type metadata");

            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(ref dependencies, factory, ((EcmaMethod)_method));

            MethodSignature sig = _method.Signature;
            const string reason = "Method signature metadata";
            TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, sig.ReturnType, reason);
            foreach (TypeDesc paramType in sig)
            {
                TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, paramType, reason);
            }

            return dependencies;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Method metadata: " + _method.ToString();
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_method));
            Debug.Assert(factory.MetadataManager.CanGenerateMetadata(_method));
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
