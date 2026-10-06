// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

using Map = ILLink.Shared.TypeMapXmlWriter.Map;

namespace ILCompiler;

internal sealed class TypeMapOutput
{
    private readonly CustomAttributeTypeNameFormatter _typeNameFormatter = new CustomAttributeTypeNameFormatter();

    public SortedDictionary<string, (Map External, Map Proxy)> GetTypeMapGroups(NodeFactory factory)
    {
        var groups = new SortedDictionary<string, (Map External, Map Proxy)>(StringComparer.Ordinal);

        foreach (IExternalTypeMapNode original in factory.TypeMapManager.GetExternalTypeMaps())
        {
            IExternalTypeMapNode node = original.ToAnalysisBasedNode(factory);
            string name = GetName(node.TypeMapGroup);
            var map = new Map();

            if (node is AnalyzedExternalTypeMapNode analyzed)
            {
                foreach (KeyValuePair<string, TypeDesc> entry in analyzed.Entries)
                    map.Entries.Add((entry.Key, GetName(entry.Value)));
            }

            groups[name] = (map, null);
        }

        foreach (IProxyTypeMapNode original in factory.TypeMapManager.GetProxyTypeMaps())
        {
            IProxyTypeMapNode node = original.ToAnalysisBasedNode(factory);
            string name = GetName(node.TypeMapGroup);
            var map = new Map();

            if (node is AnalyzedProxyTypeMapNode analyzed)
            {
                foreach (KeyValuePair<TypeDesc, TypeDesc> entry in analyzed.Entries)
                    map.Entries.Add((GetName(entry.Key), GetName(entry.Value)));
            }

            groups.TryGetValue(name, out var maps);
            maps.Proxy = map;
            groups[name] = maps;
        }

        return groups;
    }

    private string GetName(TypeDesc type) => _typeNameFormatter.FormatName(type, options: true);
}
