// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an interface implementation that is retained on a constructed type.
    /// </summary>
    public sealed class InterfaceImplementationNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaModule _module;
        private readonly InterfaceImplementationHandle _handle;

        public InterfaceImplementationNode(
            EcmaModule module,
            InterfaceImplementationHandle handle)
        {
            _module = module;
            _handle = handle;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Interface implementation {_handle}";
        }

        public override bool HasConditionalStaticDependencies => false;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            InterfaceImplementation implementation =
                _module.MetadataReader.GetInterfaceImplementation(_handle);

            yield return new(
                factory.GetNodeForTypeToken(_module, implementation.Interface),
                "Interface implementation token");
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(
            NodeFactory factory)
            => null;

        public override bool StaticDependenciesAreComputed => true;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(
            List<DependencyNodeCore<NodeFactory>> markedNodes,
            int firstNode,
            NodeFactory factory)
            => null;
    }
}
