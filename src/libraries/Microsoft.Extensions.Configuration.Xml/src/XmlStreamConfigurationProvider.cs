// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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

            XmlConfigurationElement root = null;

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
                            var element = new XmlConfigurationElement(reader.LocalName, GetName(reader));

                            if (currentPath.Count == 0)
                            {
                                root = element;
                            }
                            else
                            {
                                var parent = currentPath.Peek();

                                // If parent already has a dictionary of children, update the collection accordingly
                                if (parent.ChildrenBySiblingName != null)
                                {
                                    // check if this element has appeared before, elements are considered siblings if their SiblingName properties match
                                    if (!parent.ChildrenBySiblingName.TryGetValue(element.SiblingName, out var siblings))
                                    {
                                        siblings = new List<XmlConfigurationElement>();
                                        parent.ChildrenBySiblingName.Add(element.SiblingName, siblings);
                                    }
                                    siblings.Add(element);
                                }
                                else
                                {
                                    // Performance optimization: parents with a single child don't even initialize a dictionary
                                    if (parent.SingleChild == null)
                                    {
                                        parent.SingleChild = element;
                                    }
                                    else
                                    {
                                        // If we encounter a second child after assigning "SingleChild", we clear SingleChild and initialize the dictionary
                                        var children = new Dictionary<string, List<XmlConfigurationElement>>(StringComparer.OrdinalIgnoreCase);

                                        // Special case: the first and second child have the same sibling name
                                        if (string.Equals(parent.SingleChild.SiblingName, element.SiblingName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            children.Add(element.SiblingName, new List<XmlConfigurationElement>
                                            {
                                                parent.SingleChild,
                                                element
                                            });
                                        }
                                        else
                                        {
                                            children.Add(parent.SingleChild.SiblingName, new List<XmlConfigurationElement> { parent.SingleChild });
                                            children.Add(element.SiblingName, new List<XmlConfigurationElement> { element });
                                        }

                                        parent.ChildrenBySiblingName = children;
                                        parent.SingleChild = null;
                                    }

                                }
                            }

                            currentPath.Push(element);

                            ReadAttributes(reader, element);

                            // If current element is self-closing
                            if (reader.IsEmptyElement)
                            {
                                currentPath.Pop();
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
                                    var lineInfo = reader as IXmlLineInfo;
                                    var lineNumber = lineInfo?.LineNumber;
                                    var linePosition = lineInfo?.LinePosition;
                                    parent.TextContent = new XmlConfigurationElementTextContent(string.Empty, lineNumber, linePosition);
                                }
                            }
                            break;

                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                            if (currentPath.Count != 0)
                            {
                                var lineInfo = reader as IXmlLineInfo;
                                var lineNumber = lineInfo?.LineNumber;
                                var linePosition = lineInfo?.LinePosition;

                                XmlConfigurationElement parent = currentPath.Peek();

                                parent.TextContent = new XmlConfigurationElementTextContent(reader.Value, lineNumber, linePosition);
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

        private static void ReadAttributes(XmlReader reader, XmlConfigurationElement element)
        {
            if (reader.AttributeCount > 0)
            {
                element.Attributes = new List<XmlConfigurationElementAttributeValue>();
            }

            var lineInfo = reader as IXmlLineInfo;

            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                var lineNumber = lineInfo?.LineNumber;
                var linePosition = lineInfo?.LinePosition;

                // If there is a namespace attached to current attribute
                if (!string.IsNullOrEmpty(reader.NamespaceURI))
                {
                    throw new FormatException(SR.Format(SR.Error_NamespaceIsNotSupported, GetLineInfo(reader)));
                }

                element.Attributes.Add(new XmlConfigurationElementAttributeValue(reader.LocalName, reader.Value, lineNumber, linePosition));
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

        private static IDictionary<string, string> ProvideConfiguration(XmlConfigurationElement root)
        {
            var configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (root == null)
            {
                return configuration;
            }

            var rootPrefix = new Prefix();

            // The root element only contributes to the prefix via its Name attribute
            if (!string.IsNullOrEmpty(root.Name))
            {
                rootPrefix.Push(root.Name);
            }

            ProcessElementAttributes(rootPrefix, root);
            ProcessElementContent(rootPrefix, root);
            ProcessElementChildren(rootPrefix, root);

            return configuration;

            void ProcessElement(Prefix prefix, XmlConfigurationElement element)
            {
                ProcessElementAttributes(prefix, element);

                ProcessElementContent(prefix, element);

                ProcessElementChildren(prefix, element);
            }

            void ProcessElementAttributes(Prefix prefix, XmlConfigurationElement element)
            {
                // Add attributes to configuration values
                if (element.Attributes != null)
                {
                    for (var i = 0; i < element.Attributes.Count; i++)
                    {
                        var attribute = element.Attributes[i];

                        prefix.Push(attribute.Attribute);

                        AddToConfiguration(prefix.AsString, attribute.Value, attribute.LineNumber, attribute.LinePosition);

                        prefix.Pop();
                    }
                }
            }

            void ProcessElementContent(Prefix prefix, XmlConfigurationElement element)
            {
                // Add text content to configuration values
                if (element.TextContent != null)
                {
                    var textContent = element.TextContent;
                    AddToConfiguration(prefix.AsString, textContent.TextContent, textContent.LineNumber, textContent.LinePosition);
                }
            }

            void ProcessElementChildren(Prefix prefix, XmlConfigurationElement element)
            {
                if (element.SingleChild != null)
                {
                    var child = element.SingleChild;

                    ProcessElementChild(prefix, child, null);

                    return;
                }

                if (element.ChildrenBySiblingName == null)
                {
                    return;
                }

                // Recursively walk through the children of this element
                foreach (var childrenWithSameSiblingName in element.ChildrenBySiblingName.Values)
                {
                    if (childrenWithSameSiblingName.Count == 1)
                    {
                        var child = childrenWithSameSiblingName[0];

                        ProcessElementChild(prefix, child, null);
                    }
                    else
                    {
                        // Multiple children with the same sibling name. Add the current index to the prefix
                        for (int i = 0; i < childrenWithSameSiblingName.Count; i++)
                        {
                            var child = childrenWithSameSiblingName[i];

                            ProcessElementChild(prefix, child, i);
                        }
                    }
                }
            }

            void ProcessElementChild(Prefix prefix, XmlConfigurationElement child, int? index)
            {
                // Add element name to prefix
                prefix.Push(child.ElementName);

                // Add value of name attribute to prefix
                var hasName = !string.IsNullOrEmpty(child.Name);
                if (hasName)
                {
                    prefix.Push(child.Name);
                }

                // Add index to the prefix
                if (index != null)
                {
                    prefix.Push(index.Value.ToString(CultureInfo.InvariantCulture));
                }

                ProcessElement(prefix, child);

                // Remove index
                if (index != null)
                {
                    prefix.Pop();
                }

                // Remove 'Name' attribute
                if (hasName)
                {
                    prefix.Pop();
                }

                // Remove element name
                prefix.Pop();
            }

            void AddToConfiguration(string key, string value, int? lineNumber, int? linePosition)
            {
#if NETSTANDARD2_1
                if (!configuration.TryAdd(key, value))
                {
                    var lineInfo = lineNumber == null || linePosition == null
                        ? string.Empty
                        : SR.Format(SR.Msg_LineInfo, lineNumber.Value, linePosition.Value);
                    throw new FormatException(SR.Format(SR.Error_KeyIsDuplicated, key, lineInfo));
                }
#else
                if (configuration.ContainsKey(key))
                {
                    var lineInfo = lineNumber == null || linePosition == null
                        ? string.Empty
                        : SR.Format(SR.Msg_LineInfo, lineNumber.Value, linePosition.Value);
                    throw new FormatException(SR.Format(SR.Error_KeyIsDuplicated, key, lineInfo));
                }

                configuration.Add(key, value);
#endif
            }
        }
    }

    /// <summary>
    /// Helper class to build the configuration keys in a way that does not require string.Join
    /// </summary>
    internal sealed class Prefix
    {
        private readonly StringBuilder _sb;
        private readonly Stack<int> _lengths;

        public Prefix()
        {
            _sb = new StringBuilder();
            _lengths = new Stack<int>();
        }

        public string AsString => _sb.ToString();

        public void Push(string value)
        {
            if (_sb.Length != 0)
            {
                _sb.Append(ConfigurationPath.KeyDelimiter);
                _sb.Append(value);
                _lengths.Push(value.Length + ConfigurationPath.KeyDelimiter.Length);
            }
            else
            {
                _sb.Append(value);
                _lengths.Push(value.Length);
            }
        }

        public void Pop()
        {
            var length = _lengths.Pop();

            _sb.Remove(_sb.Length - length, length);
        }
    }
}
