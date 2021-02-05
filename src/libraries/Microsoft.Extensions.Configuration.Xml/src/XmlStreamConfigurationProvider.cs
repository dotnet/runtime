// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            var readerSettings = new XmlReaderSettings()
            {
                CloseInput = false, // caller will close the stream
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            XmlConfigurationElement? root = null;

            using (XmlReader reader = decryptor.CreateDecryptingXmlReader(stream, readerSettings))
            {
                // keep track of the tree we followed to get where we are (breadcrumb style)
                var currentPath = new Stack<XmlConfigurationElement>();

                XmlNodeType preNodeType = reader.NodeType;

                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            {
                                var element = new XmlConfigurationElement(reader.LocalName, GetName(reader), GetLineInfo(reader));
                                XmlConfigurationElement parent = currentPath.Count != 0 ? currentPath.Peek() : null;

                                if (parent == null)
                                {
                                    root = element;
                                }
                                else
                                {
                                    if (parent.Children == null)
                                    {
                                        parent.Children = new List<XmlConfigurationElement>();
                                    }
                                    else
                                    {
                                        // check if this element has appeared before, elements are considered siblings if their element names match
                                        XmlConfigurationElement sibling = parent.FindSiblingInChildren(element);

                                        if (sibling != null)
                                        {
                                            var siblings = sibling.Siblings;

                                            // If this is the first sibling, we must initialize the siblings list
                                            if (siblings == null)
                                            {
                                                siblings = sibling.Siblings = new List<XmlConfigurationElement> { sibling };
                                            }

                                            // Add the current element to the shared siblings list and give it access to the shared list
                                            siblings.Add(element);
                                            element.Siblings = siblings;
                                        }
                                    }

                                    parent.Children.Add(element);
                                }

                                currentPath.Push(element);

                                ProcessAttributes(reader, element);

                                // If current element is self-closing
                                if (reader.IsEmptyElement)
                                {
                                    currentPath.Pop();
                                }
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (currentPath.Count != 0)
                            {
                                XmlConfigurationElement parent = currentPath.Pop();

                                // If this EndElement node comes right after an Element node,
                                // it means there is no text/CDATA node in current element
                                if (preNodeType == XmlNodeType.Element)
                                {
                                    parent.TextContent = new XmlConfigurationElementTextContent(string.Empty, GetLineInfo(reader));
                                }
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            if (currentPath.Count != 0)
                            {
                                XmlConfigurationElement parent = currentPath.Peek();

                                parent.TextContent = new XmlConfigurationElementTextContent(reader.Value, GetLineInfo(reader));
                            }
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                        case XmlNodeType.Comment:
                        case XmlNodeType.Whitespace:
                            // Ignore certain types of nodes
                            break;

                        default:
                            throw new FormatException(SR.Format(SR.Error_UnsupportedNodeType, reader.NodeType, GetLineInfo(reader)));
                    }
                    preNodeType = reader.NodeType;

                    // If this element is a self-closing element,
                    // we pretend that we just processed an EndElement node
                    // because a self-closing element contains an end within itself
                    if (preNodeType == XmlNodeType.Element && reader.IsEmptyElement)
                    {
                        preNodeType = XmlNodeType.EndElement;
                    }
                }
            }

            return ProvideConfiguration(root);
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

        private static void ProcessAttributes(XmlReader reader, XmlConfigurationElement element)
        {
            if (reader.AttributeCount > 0)
            {
                element.Attributes = new List<XmlConfigurationElementAttributeValue>();
            }

            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException(SR.Format(SR.Error_NamespaceIsNotSupported, GetLineInfo(reader)));
                }

                element.Attributes.Add(new XmlConfigurationElementAttributeValue(reader.LocalName, reader.Value, GetLineInfo(reader)));
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

        private static IDictionary<string, string> ProvideConfiguration(XmlConfigurationElement? root)
        {
            var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (root == null)
            {
                return configuration;
            }

            var rootPrefix = new List<string>();

            // The root element only contributes to the prefix via its Name attribute
            if (!string.IsNullOrEmpty(root.Name))
            {
                rootPrefix.Add(root.Name);
            }

            ProcessElementAttributes(rootPrefix, root);
            ProcessElementContent(rootPrefix, root);
            ProcessElementChildren(rootPrefix, root);

            return configuration;

            void ProcessElement(List<string> prefix, XmlConfigurationElement element)
            {
                // Add element name to prefix
                prefix.Add(element.ElementName);

                // Add value of name attribute to prefix
                if (!string.IsNullOrEmpty(element.Name))
                {
                    prefix.Add(element.Name);
                }

                // Add sibling index to prefix
                if (element.Siblings != null)
                {
                    prefix.Add(element.Siblings.IndexOf(element).ToString(CultureInfo.InvariantCulture));
                }

                ProcessElementAttributes(prefix, element);

                ProcessElementContent(prefix, element);

                ProcessElementChildren(prefix, element);

                // Remove 'Name' attribute
                if (!string.IsNullOrEmpty(element.Name))
                {
                    prefix.RemoveAt(prefix.Count - 1);
                }

                // Remove sibling index
                if (element.Siblings != null)
                {
                    prefix.RemoveAt(prefix.Count - 1);
                }

                // Remove element name
                prefix.RemoveAt(prefix.Count - 1);
            }

            void ProcessElementAttributes(List<string> prefix, XmlConfigurationElement element)
            {
                // Add attributes to configuration values
                if (element.Attributes != null)
                {
                    for (var i = 0; i < element.Attributes.Count; i++)
                    {
                        var attribute = element.Attributes[i];

                        prefix.Add(attribute.Attribute);

                        AddToConfiguration(ConfigurationPath.Combine(prefix), attribute.Value, attribute.LineInfo);

                        prefix.RemoveAt(prefix.Count - 1);
                    }
                }
            }

            void ProcessElementContent(List<string> prefix, XmlConfigurationElement element)
            {
                // Add text content to configuration values
                if (element.TextContent != null)
                {
                    AddToConfiguration(ConfigurationPath.Combine(prefix), element.TextContent.TextContent, element.TextContent.LineInfo);
                }
            }

            void ProcessElementChildren(List<string> prefix, XmlConfigurationElement element)
            {
                // Recursively walk through the children of this element
                if (element.Children != null)
                {
                    for (var i = 0; i < element.Children.Count; i++)
                    {
                        var child = element.Children[i];

                        ProcessElement(prefix, child);
                    }
                }
            }

            void AddToConfiguration(string key, string value, string lineInfo)
            {
                if (configuration.ContainsKey(key))
                {
                    throw new FormatException(SR.Format(SR.Error_KeyIsDuplicated, key, lineInfo));
                }

                configuration.Add(key, value);
            }
        }
    }
}
