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
        private readonly IEnumerable<ExternalTypeMapNode> _externalTypeMapNodes;
        private readonly IEnumerable<ProxyTypeMapNode> _proxyTypeMapNodes;
        private readonly IEnumerable<InvalidExternalTypeMapNode> _invalidExternalTypeMapNodes;
        private readonly IEnumerable<InvalidProxyTypeMapNode> _invalidProxyTypeMapNodes;

        internal AnalysisBasedTypeMapManager(TypeMapStates typeMaps, IEnumerable<TypeDesc> usedExternalTypeMaps, IEnumerable<TypeDesc> usedProxyTypeMaps) : base(typeMaps)
        {
            List<ExternalTypeMapNode> externalTypeMapNodes = [];
            List<InvalidExternalTypeMapNode> invalidExternalTypeMapNodes = [];
            List<InvalidProxyTypeMapNode> invalidProxyTypeMapNodes = [];
            List<ProxyTypeMapNode> proxyTypeMapNodes = [];

            foreach (TypeDesc typeMapGroup in usedExternalTypeMaps)
            {
                object externalTypeMapNode = _typeMaps[typeMapGroup].GetExternalTypeMapNode(typeMapGroup);
                switch (externalTypeMapNode)
                {
                    case ExternalTypeMapNode valid:
                        externalTypeMapNodes.Add(valid);
                        break;
                    case InvalidExternalTypeMapNode invalid:
                        invalidExternalTypeMapNodes.Add(invalid);
                        break;
                    default:
                        Debug.Fail("External type map node should be a known node type.");
                        break;
                }
            }

            foreach (TypeDesc typeMapGroup in usedProxyTypeMaps)
            {
                object proxyTypeMapNode = _typeMaps[typeMapGroup].GetProxyTypeMapNode(typeMapGroup);
                switch (proxyTypeMapNode)
                {
                    case ProxyTypeMapNode valid:
                        proxyTypeMapNodes.Add(valid);
                        break;
                    case InvalidProxyTypeMapNode invalid:
                        invalidProxyTypeMapNodes.Add(invalid);
                        break;
                    default:
                        Debug.Fail("External type map node should be a known node type.");
                        break;
                }
            }

            _externalTypeMapNodes = externalTypeMapNodes;
            _proxyTypeMapNodes = proxyTypeMapNodes;
            _invalidProxyTypeMapNodes = invalidProxyTypeMapNodes;
            _invalidExternalTypeMapNodes = invalidExternalTypeMapNodes;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            const string reason = "Used Type Map Group";

            foreach (ExternalTypeMapNode externalTypeMap in _externalTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(externalTypeMap, reason);
            }

            foreach (InvalidExternalTypeMapNode invalidExternalMap in _invalidExternalTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(invalidExternalMap, reason);
            }

            foreach (ProxyTypeMapNode proxyTypeMap in _proxyTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(proxyTypeMap, reason);
            }

            foreach (InvalidProxyTypeMapNode invalidProxyMap in _invalidProxyTypeMapNodes)
            {
                rootProvider.AddCompilationRoot(invalidProxyMap, reason);
            }
        }

        internal override IEnumerable<ExternalTypeMapNode> GetExternalTypeMaps() => _externalTypeMapNodes;
        internal override IEnumerable<ProxyTypeMapNode> GetProxyTypeMaps() => _proxyTypeMapNodes;
        internal override IEnumerable<InvalidExternalTypeMapNode> GetInvalidExternalTypeMaps() => _invalidExternalTypeMapNodes;
        internal override IEnumerable<InvalidProxyTypeMapNode> GetInvalidProxyTypeMaps() => _invalidProxyTypeMapNodes;
    }
}
