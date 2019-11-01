// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Diagnostics;

    using System.Threading.Tasks;

    internal sealed partial class Parser
    {
        public async Task StartParsingAsync(XmlReader reader, string targetNamespace)
        {
            _reader = reader;
            _positionInfo = PositionInfo.GetPositionInfo(reader);
            _namespaceManager = reader.NamespaceManager;
            if (_namespaceManager == null)
            {
                _namespaceManager = new XmlNamespaceManager(_nameTable);
                _isProcessNamespaces = true;
            }
            else
            {
                _isProcessNamespaces = false;
            }
            while (reader.NodeType != XmlNodeType.Element && await reader.ReadAsync().ConfigureAwait(false)) { }

            _markupDepth = int.MaxValue;
            _schemaXmlDepth = reader.Depth;
            SchemaType rootType = _schemaNames.SchemaTypeFromRoot(reader.LocalName, reader.NamespaceURI);

            string code;
            if (!CheckSchemaRoot(rootType, out code))
            {
                throw new XmlSchemaException(code, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
            }

            if (_schemaType == SchemaType.XSD)
            {
                _schema = new XmlSchema();
                _schema.BaseUri = new Uri(reader.BaseURI, UriKind.RelativeOrAbsolute);
                _builder = new XsdBuilder(reader, _namespaceManager, _schema, _nameTable, _schemaNames, _eventHandler);
            }
            else
            {
                Debug.Assert(_schemaType == SchemaType.XDR);
                _xdrSchema = new SchemaInfo();
                _xdrSchema.SchemaType = SchemaType.XDR;
                _builder = new XdrBuilder(reader, _namespaceManager, _xdrSchema, targetNamespace, _nameTable, _schemaNames, _eventHandler);
                ((XdrBuilder)_builder).XmlResolver = _xmlResolver;
            }
        }
    };
} // namespace System.Xml
