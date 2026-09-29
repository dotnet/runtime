// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.SourceLink.Tools;

using ILCompiler.DependencyAnalysis;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler
{
    public class SourceLinkWriter : ObjectDumper
    {
        private readonly TextWriter _writer;
        private readonly HashSet<string> _generatedSourceMappings;
        private readonly Dictionary<EcmaModule, SourceLinkMap> _sourceLinkMaps;

        public SourceLinkWriter(string sourceLinkFileName)
        {
            _writer = File.CreateText(sourceLinkFileName);
            _generatedSourceMappings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _sourceLinkMaps = new Dictionary<EcmaModule, SourceLinkMap>();
        }

        internal override void Begin()
        {
            _writer.Write("{\"documents\":{");
        }

        protected override void DumpObjectNode(NodeFactory factory, ObjectNode node, ObjectData objectData)
        {
            if (node is not INodeWithDebugInfo debugInfoNode
                || node is not IMethodBodyNode methodBodyNode
                || methodBodyNode.Method.GetTypicalMethodDefinition() is not EcmaMethod ecmaMethod)
            {
                return;
            }

            if (!_sourceLinkMaps.TryGetValue(ecmaMethod.Module, out SourceLinkMap map))
            {
                ReadOnlySpan<byte> sourceLinkBytes = ecmaMethod.Module.PdbReader is PdbSymbolReader reader ? reader.GetSourceLinkData() : default;
                string sourceLinkData = sourceLinkBytes.IsEmpty
                    ? "{\"documents\":{}}" : Encoding.UTF8.GetString(sourceLinkBytes);
                _sourceLinkMaps.Add(ecmaMethod.Module, map = SourceLinkMap.Parse(sourceLinkData));
            }

            if (map.Entries.Count == 0)
            {
                return;
            }

            foreach (NativeSequencePoint sequencePoint in debugInfoNode.GetNativeSequencePoints())
            {
                if (_generatedSourceMappings.Contains(sequencePoint.FileName))
                    continue;

                if (!map.TryGetUri(sequencePoint.FileName, out string uri))
                    continue;

                if (_generatedSourceMappings.Count != 0)
                    _writer.Write(", ");

                _writer.Write($"\"{JsonEscape(sequencePoint.FileName)}\": \"{JsonEscape(uri)}\"");

                _generatedSourceMappings.Add(sequencePoint.FileName);
            }

            static string JsonEscape(string str)
                => str.Replace(@"\", @"\\").Replace("\"", "\\\"");
        }

        internal override void End()
        {
            _writer.WriteLine("}}");
            _writer.Dispose();
        }
    }
}
