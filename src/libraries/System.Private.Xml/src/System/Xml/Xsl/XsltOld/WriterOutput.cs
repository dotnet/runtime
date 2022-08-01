// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class WriterOutput : IRecordOutput
    {
        private XmlWriter _writer;
        private readonly Processor _processor;

        internal WriterOutput(Processor processor, XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            _writer = writer;
            _processor = processor;
        }

        // RecordOutput interface method implementation
        //
        public Processor.OutputResult RecordDone(RecordBuilder record)
        {
            BuilderInfo mainNode = record.MainNode;

            switch (mainNode.NodeType)
            {
                case XmlNodeType.Element:
                    _writer.WriteStartElement(mainNode.Prefix, mainNode.LocalName, mainNode.NamespaceURI);

                    WriteAttributes(record.AttributeList, record.AttributeCount);

                    if (mainNode.IsEmptyTag)
                    {
                        _writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _writer.WriteString(mainNode.Value);
                    break;
                case XmlNodeType.CDATA:
                    Debug.Fail("XSLT never gives us CDATA");
                    _writer.WriteCData(mainNode.Value);
                    break;
                case XmlNodeType.EntityReference:
                    _writer.WriteEntityRef(mainNode.LocalName);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    _writer.WriteProcessingInstruction(mainNode.LocalName, mainNode.Value);
                    break;
                case XmlNodeType.Comment:
                    _writer.WriteComment(mainNode.Value);
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    _writer.WriteRaw(mainNode.Value);
                    break;
                case XmlNodeType.EndElement:
                    _writer.WriteFullEndElement();
                    break;

                case XmlNodeType.None:
                case XmlNodeType.Attribute:
                case XmlNodeType.Entity:
                case XmlNodeType.Notation:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.EndEntity:
                    break;
                default:
                    Debug.Fail($"Invalid NodeType on output: {mainNode.NodeType}");
                    break;
            }

            record.Reset();
            return Processor.OutputResult.Continue;
        }

        public void TheEnd()
        {
            _writer.Flush();
            _writer = null!;
        }

        private void WriteAttributes(ArrayList list, int count)
        {
            Debug.Assert(list.Count >= count);
            for (int attrib = 0; attrib < count; attrib++)
            {
                Debug.Assert(list[attrib] is BuilderInfo);
                BuilderInfo attribute = (BuilderInfo)list[attrib]!;
                _writer.WriteAttributeString(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI, attribute.Value);
            }
        }
    }
}
