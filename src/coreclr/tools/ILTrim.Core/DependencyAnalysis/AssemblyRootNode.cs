// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using AssemblyRootMode = Mono.Linker.AssemblyRootMode;

namespace ILCompiler.DependencyAnalysis
{
    internal class AssemblyRootNode : DependencyNodeCore<NodeFactory>
    {
        private readonly string _assemblyName;
        private readonly AssemblyRootMode _mode;

        public AssemblyRootNode(string assemblyName, AssemblyRootMode mode)
            => (_assemblyName, _mode) = (assemblyName, mode);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            // TODO: what is the failure mode of illink here?
            var module = (EcmaModule)factory.TypeSystemContext.ResolveAssembly(AssemblyNameInfo.Parse(_assemblyName));

            switch (_mode)
            {
                case AssemblyRootMode.AllMembers:
                    // TODO
                    break;
                case AssemblyRootMode.EntryPoint:
                    // TODO: what is the failure mode of illink here?
                    MethodDefinitionHandle entrypointToken = (MethodDefinitionHandle)MetadataTokens.Handle(module.PEReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
                    yield return new DependencyListEntry(factory.MethodDefinition(module, entrypointToken), "Entrypoint");
                    break;
                case AssemblyRootMode.VisibleMembers:
                    // TODO
                    break;
                case AssemblyRootMode.Library:
                    // TODO
                    break;
            }
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        protected override string GetName(NodeFactory context) => $"Assembly root: {_assemblyName} ({_mode})";
    }
}
