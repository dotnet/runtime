// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System;
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.XPath;
    using MS.Internal.Xml.Cache;

    internal class NavigatorOutput : RecordOutput
    {
        private XPathDocument _doc;
        private int _documentIndex;
        private XmlRawWriter _wr;

        internal XPathNavigator Navigator
        {
            get { return ((IXPathNavigable)_doc).CreateNavigator(); }
        }

        internal NavigatorOutput(string baseUri)
        {
            _doc = new XPathDocument();
            _wr = _doc.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, baseUri);
        }

        public Processor.OutputResult RecordDone(RecordBuilder record)
        {
            Debug.Assert(record != null);

            BuilderInfo mainNode = record.MainNode;
            _documentIndex++;
            switch (mainNode.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        _wr.WriteStartElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
                        for (int attrib = 0; attrib < record.AttributeCount; attrib++)
                        {
                            _documentIndex++;
                            Debug.Assert(record.AttributeList[attrib] is BuilderInfo);
                            BuilderInfo attrInfo = (BuilderInfo)record.AttributeList[attrib];
                            if (attrInfo.NamespaceURI == XmlReservedNs.NsXmlNs)
                            {
                                if (attrInfo.Prefix.Length == 0)
                                    _wr.WriteNamespaceDeclaration(string.Empty, attrInfo.Value);
                                else
                                    _wr.WriteNamespaceDeclaration(attrInfo.LocalName, attrInfo.Value);
                            }
                            else
                            {
                                _wr.WriteAttributeString(attrInfo.Prefix, attrInfo.LocalName, attrInfo.NamespaceURI, attrInfo.Value);
                            }
                        }

                        _wr.StartElementContent();

                        if (mainNode.IsEmptyTag)
                            _wr.WriteEndElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
                        break;
                    }

                case XmlNodeType.Text:
                    _wr.WriteString(mainNode.Value);
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    _wr.WriteString(mainNode.Value);
                    break;

                case XmlNodeType.ProcessingInstruction:
                    _wr.WriteProcessingInstruction(mainNode.LocalName, mainNode.Value);
                    break;
                case XmlNodeType.Comment:
                    _wr.WriteComment(mainNode.Value);
                    break;

                case XmlNodeType.Document:
                    break;

                case XmlNodeType.EndElement:
                    _wr.WriteEndElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);
                    break;

                default:
                    Debug.Fail("Invalid NodeType on output: " + mainNode.NodeType);
                    break;
            }
            record.Reset();
            return Processor.OutputResult.Continue;
        }

        public void TheEnd()
        {
            _wr.Close();
        }
    }
}
