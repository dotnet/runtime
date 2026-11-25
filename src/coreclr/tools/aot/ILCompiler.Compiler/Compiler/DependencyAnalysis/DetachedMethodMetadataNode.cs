// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that is not available to reflection (it is not public in an assembly that disables private reflection)
    /// but that the runtime type loader needs to identify by its metadata (generic methods, generic virtual methods and so on).
    /// It has a minimal metadata record (name, signature, generic parameters) that is not part of the metadata of the owning type, so reflection
    /// can not find it.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class DetachedMethodMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public DetachedMethodMetadataNode(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            _method = method;
        }

        public MethodDesc Method => _method;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory factory)
        {
            MethodSignature sig = _method.Signature;
            const string reason = "Detached method signature metadata";
            TypeMetadataNode.AddMetadataDependencies(sink, factory, sig.ReturnType, reason);
            foreach (TypeDesc paramType in sig)
            {
                TypeMetadataNode.AddMetadataDependencies(sink, factory, paramType, reason);
            }

            if (sig.HasEmbeddedSignatureData)
            {
                foreach (var sigData in sig.GetEmbeddedSignatureData())
                    if (sigData.type != null)
                        TypeMetadataNode.AddMetadataDependencies(sink, factory, sigData.type, "Modifier in a detached method signature");
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Detached method metadata: " + _method.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory factory) { }
    }
}
