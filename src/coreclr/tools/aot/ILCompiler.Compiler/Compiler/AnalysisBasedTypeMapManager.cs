// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class AnalysisBasedTypeMapManager : TypeMapManager
    {
        private readonly IEnumerable<TypeDesc> _usedExternalTypeMaps;
        private readonly IEnumerable<TypeDesc> _usedProxyTypeMaps;

        internal AnalysisBasedTypeMapManager(TypeMapStates typeMaps, IEnumerable<TypeDesc> usedExternalTypeMaps, IEnumerable<TypeDesc> usedProxyTypeMaps) : base(typeMaps)
        {
            _usedExternalTypeMaps = usedExternalTypeMaps;
            _usedProxyTypeMaps = usedProxyTypeMaps;
        }

        public override void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            const string reason = "Used Type Map Group";
            foreach (TypeDesc typeMapGroup in _usedExternalTypeMaps)
            {
                rootProvider.AddCompilationRoot(_typeMaps[typeMapGroup].GetExternalTypeMapNode(typeMapGroup), reason);
            }

            foreach (TypeDesc typeMapGroup in _usedProxyTypeMaps)
            {
                rootProvider.AddCompilationRoot(_typeMaps[typeMapGroup].GetProxyTypeMapNode(typeMapGroup), reason);
            }
        }
    }
}
