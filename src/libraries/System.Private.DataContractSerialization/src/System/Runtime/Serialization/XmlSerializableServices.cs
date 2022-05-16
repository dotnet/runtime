// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Schema;

    public static class XmlSerializableServices
    {
        public static XmlNode[] ReadNodes(XmlReader xmlReader)
        {
            ArgumentNullException.ThrowIfNull(xmlReader);

            XmlDocument doc = new XmlDocument();
            List<XmlNode> nodeList = new List<XmlNode>();
            if (xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    if (IsValidAttribute(xmlReader))
                    {
                        XmlNode? node = doc.ReadNode(xmlReader);
                        if (node == null)
                            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile));
                        nodeList.Add(node);
                    }
                } while (xmlReader.MoveToNextAttribute());
            }
            xmlReader.MoveToElement();
            if (!xmlReader.IsEmptyElement)
            {
                int startDepth = xmlReader.Depth;
                xmlReader.Read();
                while (xmlReader.Depth > startDepth && xmlReader.NodeType != XmlNodeType.EndElement)
                {
                    XmlNode? node = doc.ReadNode(xmlReader);
                    if (node == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.UnexpectedEndOfFile));
                    nodeList.Add(node);
                }
            }
            return nodeList.ToArray();
        }

        private static bool IsValidAttribute(XmlReader xmlReader)
        {
            return xmlReader.NamespaceURI != Globals.SerializationNamespace &&
                                   xmlReader.NamespaceURI != Globals.SchemaInstanceNamespace &&
                                   xmlReader.Prefix != "xmlns" &&
                                   xmlReader.LocalName != "xmlns";
        }

        public static void WriteNodes(XmlWriter xmlWriter, XmlNode?[]? nodes)
        {
            ArgumentNullException.ThrowIfNull(xmlWriter);

            if (nodes != null)
                for (int i = 0; i < nodes.Length; i++)
                    if (nodes[i] != null)
                        nodes[i]!.WriteTo(xmlWriter);
        }

        internal static string AddDefaultSchemaMethodName = "AddDefaultSchema";
        public static void AddDefaultSchema(XmlSchemaSet schemas, XmlQualifiedName typeQName)
        {
            ArgumentNullException.ThrowIfNull(schemas);
            ArgumentNullException.ThrowIfNull(typeQName);

            SchemaExporter.AddDefaultXmlType(schemas, typeQName.Name, typeQName.Namespace);
        }
    }
}
