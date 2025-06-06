// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class AnalysisBasedTypeMapManager : TypeMapManager
    {
        private readonly IEnumerable<IExternalTypeMapNode> _externalTypeMapNodes;
        private readonly IEnumerable<IProxyTypeMapNode> _proxyTypeMapNodes;

        internal AnalysisBasedTypeMapManager(TypeMapStates typeMaps, IEnumerable<TypeDesc> usedExternalTypeMaps, IEnumerable<TypeDesc> usedProxyTypeMaps) : base(typeMaps)
        {
            List<IExternalTypeMapNode> externalTypeMapNodes = [];
            List<IProxyTypeMapNode> proxyTypeMapNodes = [];

            foreach (TypeDesc typeMapGroup in usedExternalTypeMaps)
            {
                externalTypeMapNodes.Add(_typeMaps[typeMapGroup].GetExternalTypeMapNode(typeMapGroup));
            }

            foreach (TypeDesc typeMapGroup in usedProxyTypeMaps)
            {
                proxyTypeMapNodes.Add(_typeMaps[typeMapGroup].GetProxyTypeMapNode(typeMapGroup));
            }

            _externalTypeMapNodes = externalTypeMapNodes;
            _proxyTypeMapNodes = proxyTypeMapNodes;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            const string reason = "Used Type Map Group";

            foreach (IExternalTypeMapNode externalTypeMap in _externalTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(externalTypeMap, reason);
            }

            foreach (IProxyTypeMapNode proxyTypeMap in _proxyTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(proxyTypeMap, reason);
            }
        }

        internal override IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps() => _externalTypeMapNodes;
        internal override IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps() => _proxyTypeMapNodes;
    }
}
