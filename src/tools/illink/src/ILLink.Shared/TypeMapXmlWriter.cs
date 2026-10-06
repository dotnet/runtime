// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

#nullable enable

namespace ILLink.Shared;

internal sealed class TypeMapXmlWriter : IDisposable
{
    private readonly XmlWriter _writer;
    private bool _done;

    public sealed class Map
    {
        public List<(string Key, string Value)> Entries { get; } = [];
    }

    private TypeMapXmlWriter(XmlWriter writer)
    {
        _writer = writer;

        _writer.WriteStartDocument();
        _writer.WriteStartElement("typemaps");
        _writer.WriteAttributeString("version", "1");
    }

    // Reflection type identifiers escape a different set of characters than assembly display names.
    public static string EscapeTypeName(string name)
    {
        var builder = new StringBuilder();
        foreach (char character in name)
        {
            if (character is '[' or ']' or '&' or '*' or ',' or '+' or '\\')
                builder.Append('\\');
            builder.Append(character);
        }

        return builder.ToString();
    }

    public static void WriteTypeMapsToStream(Stream output, SortedDictionary<string, (Map? External, Map? Proxy)> groups)
    {
        using XmlWriter xmlWriter = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Entitize
        });

        using var writer = new TypeMapXmlWriter(xmlWriter);
        writer.WriteTypeMaps(groups);
    }

    private void WriteTypeMaps(SortedDictionary<string, (Map? External, Map? Proxy)> groups)
    {
        foreach (KeyValuePair<string, (Map? External, Map? Proxy)> group in groups)
        {
            _writer.WriteStartElement("group");
            _writer.WriteAttributeString("type", group.Key);

            if (group.Value.External is not null)
                WriteMap(group.Value.External, "external");

            if (group.Value.Proxy is not null)
                WriteMap(group.Value.Proxy, "proxy");

            _writer.WriteEndElement();
        }
    }

    private void WriteMap(Map map, string mapKind)
    {
        _writer.WriteStartElement(mapKind);

        map.Entries.Sort(static (left, right) =>
        {
            int result = StringComparer.Ordinal.Compare(left.Key, right.Key);

            return result != 0 ? result : StringComparer.Ordinal.Compare(left.Value, right.Value);
        });

        foreach (var (key, value) in map.Entries)
        {
            _writer.WriteStartElement("entry");
            _writer.WriteAttributeString("key", key);
            _writer.WriteAttributeString("value", value);
            _writer.WriteEndElement();
        }

        _writer.WriteEndElement();
    }

    public void Close()
    {
        if (!_done)
        {
            _done = true;

            _writer.WriteEndElement();
            _writer.WriteEndDocument();
        }
    }

    void IDisposable.Dispose()
    {
        Close();
    }
}
