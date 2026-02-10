// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.ReadyToRun
{
    public sealed class ReadyToRunTypeMapManager(ModuleDesc triggeringModule, TypeMapMetadata assemblyTypeMaps, ImportReferenceProvider importProvider) : TypeMapManager
    {
        public override void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            base.AttachToDependencyGraph(graph);
            foreach (var map in GetExternalTypeMaps())
            {
                graph.AddRoot(map, "External type map");
            }
            foreach (var map in GetProxyTypeMaps())
            {
                graph.AddRoot(map, "Proxy type map");
            }
        }

        protected override bool IsEmpty => assemblyTypeMaps.IsEmpty;

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
        }

        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps()
        {
            foreach (var map in assemblyTypeMaps.Maps)
            {
                yield return new ReadyToRunExternalTypeMapNode(triggeringModule, map.Key, map.Value, importProvider);
            }
        }

        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps()
        {
            foreach (var map in assemblyTypeMaps.Maps)
            {
                yield return new ReadyToRunProxyTypeMapNode(triggeringModule, map.Key, map.Value, importProvider);
            }
        }

        public override void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, INativeFormatTypeReferenceProvider commonFixupsTableNode)
        {
            base.AddToReadyToRunHeader(header, nodeFactory, commonFixupsTableNode);

            if (IsEmpty)
                return;


        }
    }
}
