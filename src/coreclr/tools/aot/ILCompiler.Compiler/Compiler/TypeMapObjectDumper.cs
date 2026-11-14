// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

using Map = ILLink.Shared.TypeMapXmlWriter.Map;

namespace ILCompiler;

public sealed class TypeMapObjectDumper(string fileName) : ObjectDumper
{
    private readonly CustomAttributeTypeNameFormatter _typeNameFormatter = new CustomAttributeTypeNameFormatter();
    private readonly SortedDictionary<string, (Map External, Map Proxy)> _groups = new(StringComparer.Ordinal);

    internal override void Begin()
    {
    }

    protected override void DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectNode.ObjectData objectData)
    {
        if (node is ExternalTypeMapObjectNode externalMaps)
        {
            foreach (IExternalTypeMapNode mapNode in externalMaps.GetTypeMaps())
            {
                var map = new Map();
                foreach (KeyValuePair<string, TypeDesc> entry in mapNode.GetEntries(factory))
                    map.Entries.Add((entry.Key, GetName(entry.Value)));

                string name = GetName(mapNode.TypeMapGroup);
                _groups.TryGetValue(name, out var maps);
                maps.External = map;
                _groups[name] = maps;
            }
        }

        if (node is ProxyTypeMapObjectNode proxyMaps)
        {
            foreach (IProxyTypeMapNode mapNode in proxyMaps.GetTypeMaps())
            {
                var map = new Map();
                foreach (KeyValuePair<TypeDesc, TypeDesc> entry in mapNode.GetEntries(factory))
                    map.Entries.Add((GetName(entry.Key), GetName(entry.Value)));

                string name = GetName(mapNode.TypeMapGroup);
                _groups.TryGetValue(name, out var maps);
                maps.Proxy = map;
                _groups[name] = maps;
            }
        }
    }

    internal override void End()
    {
        using FileStream output = new FileStream(fileName, FileMode.Create);
        ILLink.Shared.TypeMapXmlWriter.WriteTypeMapsToStream(output, _groups);
    }

    private string GetName(TypeDesc type) => _typeNameFormatter.FormatName(type, options: true);
}
