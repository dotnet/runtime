// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Microsoft.Extensions.Configuration.Xml
{
    /// <summary>
    /// An XML file based <see cref="IConfigurationProvider"/>.
    /// </summary>
    public class XmlStreamConfigurationProvider : StreamConfigurationProvider
    {
        private const string NameAttributeKey = "Name";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The <see cref="XmlStreamConfigurationSource"/>.</param>
        public XmlStreamConfigurationProvider(XmlStreamConfigurationSource source) : base(source) { }

        /// <summary>
        /// Read a stream of XML values into a key/value dictionary.
        /// </summary>
        /// <param name="stream">The stream of XML data.</param>
        /// <param name="decryptor">The <see cref="XmlDocumentDecryptor"/> to use to decrypt.</param>
        /// <returns>The <see cref="IDictionary{String, String}"/> which was read from the stream.</returns>
        public static IDictionary<string, string> Read(Stream stream, XmlDocumentDecryptor decryptor)
        {
            var configurationValues = new List<IXmlConfigurationValue>();

            var readerSettings = new XmlReaderSettings()
            {
                CloseInput = false, // caller will close the stream
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            using (XmlReader reader = decryptor.CreateDecryptingXmlReader(stream, readerSettings))
            {
                // record all elements we encounter to check for repeated elements
                var allElements = new List<XmlConfigurationElement>();

                // keep track of the tree we followed to get where we are (breadcrumb style)
                var currentPath = new Stack<XmlConfigurationElement>();

                XmlNodeType preNodeType = reader.NodeType;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            var parent = currentPath.Any() ? currentPath.Peek() : null;
                            var element = new XmlConfigurationElement(parent, reader.LocalName, GetName(reader), GetLineInfo(reader));

                            // check if this element has appeared before
                            var sibling = allElements.Where(e => e.IsSibling(element)).OrderByDescending(e => e.Index).FirstOrDefault();
                            if (sibling != null)
                            {
                                sibling.Multiple = element.Multiple = true;
                                element.Index = sibling.Index + 1;
                            }

                            currentPath.Push(element);
                            allElements.Add(element);

                            ProcessAttributes(reader, currentPath, configurationValues);

                            // If current element is self-closing
                            if (reader.IsEmptyElement)
                            {
                                currentPath.Pop();
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (currentPath.Any())
                            {
                                // If this EndElement node comes right after an Element node,
                                // it means there is no text/CDATA node in current element
                                if (preNodeType == XmlNodeType.Element)
                                {
                                    var configurationValue = new XmlConfigurationElementContent(currentPath, string.Empty, GetLineInfo(reader));
                                    configurationValues.Add(configurationValue);
                                }

                                currentPath.Pop();
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            {
                                var configurationValue = new XmlConfigurationElementContent(currentPath, reader.Value, GetLineInfo(reader));
                                configurationValues.Add(configurationValue);
                                break;
                            }
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                            // Ignore certain types of nodes
                            break;

                        default:
                            throw new FormatException(SR.Format(SR.Error_UnsupportedNodeType, reader.NodeType,
                                GetLineInfo(reader)));
                    }
                    preNodeType = reader.NodeType;

                    // If this element is a self-closing element,
                    // we pretend that we just processed an EndElement node
                    // because a self-closing element contains an end within itself
                    if (preNodeType == XmlNodeType.Element &&
                        reader.IsEmptyElement)
                    {
                        preNodeType = XmlNodeType.EndElement;
                    }
                }
            }

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var configurationValue in configurationValues)
            {
                var key = configurationValue.Key;
                if (data.ContainsKey(key))
                {
                    throw new FormatException(SR.Format(SR.Error_KeyIsDuplicated, key, configurationValue.LineInfo));
                }
                data[key] = configurationValue.Value;
            }

            return data;
        }

        /// <summary>
        /// Loads XML configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to load ini configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = Read(stream, XmlDocumentDecryptor.Instance);
        }

        private static string GetLineInfo(XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            return lineInfo == null ? string.Empty :
                SR.Format(SR.Msg_LineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private static void ProcessAttributes(XmlReader reader, Stack<XmlConfigurationElement> elementPath, IList<IXmlConfigurationValue> data)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException(SR.Format(SR.Error_NamespaceIsNotSupported, GetLineInfo(reader)));
                }

                data.Add(new XmlConfigurationElementAttributeValue(elementPath, reader.LocalName, reader.Value, GetLineInfo(reader)));
            }

            // Go back to the element containing the attributes we just processed
            reader.MoveToElement();
        }

        // The special attribute "Name" only contributes to prefix
        // This method retrieves the Name of the element, if the attribute is present
        // Unfortunately XmlReader.GetAttribute cannot be used, as it does not support looking for attributes in a case insensitive manner
        private static string GetName(XmlReader reader)
        {
            string name = null;

            while (reader.MoveToNextAttribute())
            {
                if (string.Equals(reader.LocalName, NameAttributeKey, StringComparison.OrdinalIgnoreCase))
                {
                    // If there is a namespace attached to current attribute
                    if (!string.IsNullOrEmpty(reader.NamespaceURI))
                    {
                        throw new FormatException(SR.Format(SR.Error_NamespaceIsNotSupported, GetLineInfo(reader)));
                    }
                    name = reader.Value;
                    break;
                }
            }

            // Go back to the element containing the name we just processed
            reader.MoveToElement();

            return name;
        }
    }
}
