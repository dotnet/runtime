// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xml.XPath
{
    internal static class XmlWriterExtensions
    {
        private static void WriteLocalNamespaces(this XmlWriter writer, XPathNavigator nsNav)
        {
            string prefix = nsNav.LocalName;
            string ns = nsNav.Value;

            if (nsNav.MoveToNextNamespace(XPathNamespaceScope.Local))
            {
                writer.WriteLocalNamespaces(nsNav);
            }

            if (prefix.Length == 0)
            {
                writer.WriteAttributeString(string.Empty, "xmlns", XmlConst.ReservedNsXmlNs, ns);
            }
            else
            {
                writer.WriteAttributeString("xmlns", prefix, XmlConst.ReservedNsXmlNs, ns);
            }
        }
        public static void WriteNode(this XmlWriter writer, XPathNavigator navigator, bool defattr)
        {
            if (navigator == null)
            {
                throw new ArgumentNullException(nameof(navigator));
            }
            int iLevel = 0;

            navigator = navigator.Clone();

            while (true)
            {
                bool mayHaveChildren = false;
                XPathNodeType nodeType = navigator.NodeType;

                switch (nodeType)
                {
                    case XPathNodeType.Element:
                        writer.WriteStartElement(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);

                        // Copy attributes
                        if (navigator.MoveToFirstAttribute())
                        {
                            do
                            {
                                writer.WriteStartAttribute(navigator.Prefix, navigator.LocalName, navigator.NamespaceURI);
                                // copy string value to writer
                                writer.WriteString(navigator.Value);
                                writer.WriteEndAttribute();
                            } while (navigator.MoveToNextAttribute());
                            navigator.MoveToParent();
                        }

                        // Copy namespaces
                        if (navigator.MoveToFirstNamespace(XPathNamespaceScope.Local))
                        {
                            writer.WriteLocalNamespaces(navigator);
                            navigator.MoveToParent();
                        }
                        mayHaveChildren = true;
                        break;
                    case XPathNodeType.Attribute:
                        // do nothing on root level attribute
                        break;
                    case XPathNodeType.Text:
                        writer.WriteString(navigator.Value);
                        break;
                    case XPathNodeType.SignificantWhitespace:
                    case XPathNodeType.Whitespace:
                        writer.WriteWhitespace(navigator.Value);
                        break;
                    case XPathNodeType.Root:
                        mayHaveChildren = true;
                        break;
                    case XPathNodeType.Comment:
                        writer.WriteComment(navigator.Value);
                        break;
                    case XPathNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(navigator.LocalName, navigator.Value);
                        break;
                    case XPathNodeType.Namespace:
                        // do nothing on root level namespace
                        break;
                    default:
                        Debug.Fail("Unmatched state in switch");
                        break;
                }

                if (mayHaveChildren)
                {
                    // If children exist, move down to next level
                    if (navigator.MoveToFirstChild())
                    {
                        iLevel++;
                        continue;
                    }
                    else
                    {
                        // EndElement
                        if (navigator.NodeType == XPathNodeType.Element)
                        {
                            if (navigator.IsEmptyElement)
                            {
                                writer.WriteEndElement();
                            }
                            else
                            {
                                writer.WriteFullEndElement();
                            }
                        }
                    }
                }

                // No children
                while (true)
                {
                    if (iLevel == 0)
                    {
                        // The entire subtree has been copied
                        return;
                    }

                    if (navigator.MoveToNext())
                    {
                        // Found a sibling, so break to outer loop
                        break;
                    }

                    // No siblings, so move up to previous level
                    iLevel--;
                    navigator.MoveToParent();

                    // EndElement
                    if (navigator.NodeType == XPathNodeType.Element)
                        writer.WriteFullEndElement();
                }
            }
        }
    }
}
