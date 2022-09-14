// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml;

using Internal.Text;

using ILCompiler.DependencyAnalysis;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler
{
    public class XmlObjectDumper : ObjectDumper
    {
        private readonly string _fileName;
        private SHA256 _sha256;
        private XmlWriter _writer;

        public XmlObjectDumper(string fileName)
        {
            _fileName = fileName;
        }

        internal override void Begin()
        {
            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                Indent = true,
            };

            _sha256 = SHA256.Create();
            _writer = XmlWriter.Create(File.CreateText(_fileName), settings);
            _writer.WriteStartElement("ObjectNodes");
        }

        protected override void DumpObjectNode(NameMangler mangler, ObjectNode node, ObjectData objectData)
        {
            string name = null;

            _writer.WriteStartElement(GetObjectNodeName(node));

            var symbolNode = node as ISymbolNode;
            if (symbolNode != null)
            {
                Utf8StringBuilder sb = new Utf8StringBuilder();
                symbolNode.AppendMangledName(mangler, sb);
                name = sb.ToString();
                _writer.WriteAttributeString("Name", name);
            }

            _writer.WriteAttributeString("Length", objectData.Data.Length.ToStringInvariant());
            _writer.WriteAttributeString("Hash", HashData(objectData.Data));
            _writer.WriteEndElement();

            var nodeWithCodeInfo = node as INodeWithCodeInfo;
            if (nodeWithCodeInfo != null)
            {
                _writer.WriteStartElement("GCInfo");
                _writer.WriteAttributeString("Name", name);
                _writer.WriteAttributeString("Length", nodeWithCodeInfo.GCInfo.Length.ToStringInvariant());
                _writer.WriteAttributeString("Hash", HashData(nodeWithCodeInfo.GCInfo));
                _writer.WriteEndElement();
            }
        }

        private string HashData(byte[] data)
        {
            return BitConverter.ToString(_sha256.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        internal override void End()
        {
            _writer.WriteEndElement();
            _writer.Dispose();
            _writer = null;
        }
    }
}
