// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal class XmlReaderWrapper : XmlReader
    {
        private XmlReader _reader;

        public static XmlReader CreateFromReader(XmlReader reader)
        {
            if (reader is XmlReaderWrapper || (reader.Settings != null && reader.Settings.Async))
            {
                return reader;
            }

            return new XmlReaderWrapper(reader);
        }

        private XmlReaderWrapper(XmlReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            if (_reader.Settings != null && _reader.Settings.Async)
            {
                throw new ArgumentException("Async XmlReader should not be wrapped", nameof(reader));
            }
        }

        public override XmlNodeType NodeType => _reader.NodeType;

        public override string LocalName => _reader.LocalName;

        public override string NamespaceURI => _reader.NamespaceURI;

        public override string Prefix => _reader.Prefix;

        public override string Value => _reader.Value;

        public override int Depth => _reader.Depth;

        public override string BaseURI => _reader.BaseURI;

        public override bool IsEmptyElement => _reader.IsEmptyElement;

        public override int AttributeCount => _reader.AttributeCount;

        public override bool EOF => _reader.EOF;

        public override ReadState ReadState => _reader.ReadState;

        public override XmlNameTable NameTable => _reader.NameTable;

        public override string GetAttribute(string name) => _reader.GetAttribute(name);

        public override string GetAttribute(string name, string namespaceURI) => _reader.GetAttribute(name, namespaceURI);

        public override string GetAttribute(int i) => _reader.GetAttribute(i);

        public override string LookupNamespace(string prefix) => _reader.LookupNamespace(prefix);

        public override bool MoveToAttribute(string name) => _reader.MoveToAttribute(name);

        public override bool MoveToAttribute(string name, string ns) => _reader.MoveToAttribute(name, ns);

        public override bool MoveToElement() => _reader.MoveToElement();

        public override bool MoveToFirstAttribute() => _reader.MoveToFirstAttribute();

        public override bool MoveToNextAttribute() => _reader.MoveToNextAttribute();

        public override bool Read() => _reader.Read();

        public override bool ReadAttributeValue() => _reader.ReadAttributeValue();

        public override void ResolveEntity() => _reader.ResolveEntity();

        public override Task<string> GetValueAsync() => Task.FromResult(_reader.Value);

        public override Task<XmlNodeType> MoveToContentAsync() => Task.FromResult(_reader.MoveToContent());

        public override Task<bool> ReadAsync() => Task.FromResult(_reader.Read());

        public override Task<string> ReadInnerXmlAsync() => Task.FromResult(_reader.ReadInnerXml());

        public override Task SkipAsync()
        {
            _reader.Skip();
            return Task.CompletedTask;
        }
    }

    internal static class XmlReaderExtensions
    {
        private const uint IsTextualNodeBitmap = 0x6018; // 00 0110 0000 0001 1000

        public static async Task ReadStartElementAsync(this XmlReader reader)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }

            await reader.ReadAsync();
        }

        public static async Task<string> ReadElementStringAsync(this XmlReader reader)
        {
            string result = string.Empty;

            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }

            if (!reader.IsEmptyElement)
            {
                await reader.ReadAsync();
                result = reader.ReadString();
                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw CreateXmlException(SR.Format(SR.Xml_UnexpectedNodeInSimpleContent, reader.NodeType.ToString(), nameof(ReadElementStringAsync)), reader);
                }

                await reader.ReadAsync();
            }
            else
            {
                await reader.ReadAsync();
            }

            return result;
        }

        public static async Task<string> ReadStringAsync(this XmlReader reader)
        {
            if (reader.ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            reader.MoveToElement();
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.IsEmptyElement)
                {
                    return string.Empty;
                }
                else if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    return string.Empty;
                }
            }

            string result = string.Empty;
            while (IsTextualNode(reader.NodeType))
            {
                result += await reader.GetValueAsync();
                if (!await reader.ReadAsync())
                {
                    break;
                }
            }

            return result;
        }

        static internal bool IsTextualNode(XmlNodeType nodeType)
        {
#if DEBUG
            // This code verifies IsTextualNodeBitmap mapping of XmlNodeType to a bool specifying
            // whether the node is 'textual' = Text, CDATA, Whitespace or SignificantWhitespace.
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.None)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Element)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Attribute)));
            Debug.Assert(0 != (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Text)));
            Debug.Assert(0 != (IsTextualNodeBitmap & (1 << (int)XmlNodeType.CDATA)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.EntityReference)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Entity)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.ProcessingInstruction)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Comment)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Document)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.DocumentType)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.DocumentFragment)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Notation)));
            Debug.Assert(0 != (IsTextualNodeBitmap & (1 << (int)XmlNodeType.Whitespace)));
            Debug.Assert(0 != (IsTextualNodeBitmap & (1 << (int)XmlNodeType.SignificantWhitespace)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.EndElement)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.EndEntity)));
            Debug.Assert(0 == (IsTextualNodeBitmap & (1 << (int)XmlNodeType.XmlDeclaration)));
#endif
            return 0 != (IsTextualNodeBitmap & (1 << (int)nodeType));
        }

        public static async Task ReadEndElementAsync(this XmlReader reader)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.EndElement)
            {
                throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }

            await reader.ReadAsync();
        }

        public static async Task ReadStartElementAsync(this XmlReader reader, string localname, string ns)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }
            if (reader.LocalName == localname && reader.NamespaceURI == ns)
            {
                await reader.ReadAsync();
            }
            else
            {
                throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
                throw CreateXmlException(SR.Format(SR.Xml_ElementNotFoundNs, localname, ns), reader);
            }
        }

        public static async Task<bool> IsStartElementAsync(this XmlReader reader)
        {
            return await reader.MoveToContentAsync() == XmlNodeType.Element;
        }

        public static async Task<bool> IsStartElementAsync(this XmlReader reader, string localname, string ns)
        {
            return (await reader.MoveToContentAsync() == XmlNodeType.Element) && (reader.LocalName == localname && reader.NamespaceURI == ns);
        }

        private static async Task ReadStartElementAsync(this XmlReader reader, string name)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }
            if (reader.Name == name)
            {
                await reader.ReadAsync();
            }
            else
            {
                throw CreateXmlException(SR.Format(SR.Xml_ElementNotFound, name), reader);
            }
        }

        private static async Task<bool> IsStartElementAsync(this XmlReader reader, string name)
        {
            return await reader.MoveToContentAsync() == XmlNodeType.Element && reader.Name == name;
        }

        private static async Task<string> ReadElementStringAsync(this XmlReader reader, string name)
        {
            string result = string.Empty;

            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }

            if (reader.Name != name)
            {
                throw CreateXmlException(SR.Format(SR.Xml_ElementNotFound, name), reader);
            }

            if (!reader.IsEmptyElement)
            {
                result = await ReadStringAsync(reader);
                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
                }

                await reader.ReadAsync();
            }
            else
            {
                await reader.ReadAsync();
            }

            return result;
        }

        private static async Task<string> ReadElementStringAsync(XmlReader reader, string localname, string ns)
        {
            string result = string.Empty;

            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
            }

            if (reader.LocalName != localname || reader.NamespaceURI != ns)
            {
                throw CreateXmlException(SR.Format(SR.Xml_ElementNotFound, localname), reader);
            }

            if (!reader.IsEmptyElement)
            {
                result = await ReadStringAsync(reader);

                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw CreateXmlException(SR.Format(SR.Xml_InvalidNodeType, reader.NodeType.ToString()), reader);
                }

                await reader.ReadAsync();
            }
            else
            {
                await reader.ReadAsync();
            }

            return result;
        }

        private static Exception CreateXmlException(string message, XmlReader reader)
        {
            var lineInfo = reader as IXmlLineInfo;
            int lineNumber = lineInfo == null ? 0 : lineInfo.LineNumber;
            int linePosition = lineInfo == null ? 0 : lineInfo.LinePosition;
            return new XmlException(message, null, lineNumber, linePosition);
        }
    }
}
