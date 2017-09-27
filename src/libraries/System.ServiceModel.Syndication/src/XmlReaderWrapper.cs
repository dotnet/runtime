using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    internal class XmlReaderWrapper : XmlReader
    {
        private XmlReader reader;

        private Func<XmlReaderWrapper, Task<string>> getValueFunc;
        private Func<XmlReaderWrapper, Task<XmlNodeType>> moveToContentFunc;
        private Func<XmlReaderWrapper, Task> skipFunc;
        private Func<XmlReaderWrapper, Task<bool>> readFunc;
        private Func<XmlReaderWrapper, Task<string>> readInnerXmlFunc;

        private XmlReaderWrapper(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            this.reader = reader;

            if (this.reader.Settings.Async)
            {
                InitAsync();
            }
            else
            {
                Init();
            }
        }

        public static XmlReaderWrapper CreateFromReader(XmlReader reader)
        {
            XmlReaderWrapper wrappedReader = reader as XmlReaderWrapper;
            return wrappedReader != null ? wrappedReader : new XmlReaderWrapper(reader);
        }

        private void InitAsync()
        {
            this.getValueFunc = new Func<XmlReaderWrapper, Task<string>>((thisPtr) => { return thisPtr.reader.GetValueAsync(); });
            this.moveToContentFunc = new Func<XmlReaderWrapper, Task<XmlNodeType>>((thisPtr) => { return thisPtr.reader.MoveToContentAsync(); });
            this.skipFunc = new Func<XmlReaderWrapper, Task>((thisPtr) => { return thisPtr.reader.SkipAsync(); });
            this.readFunc = new Func<XmlReaderWrapper, Task<bool>>((thisPtr) => { return thisPtr.reader.ReadAsync(); });
            this.readInnerXmlFunc = new Func<XmlReaderWrapper, Task<string>>((thisPtr) => { return thisPtr.reader.ReadInnerXmlAsync(); });
        }

        private void Init()
        {
            this.getValueFunc = new Func<XmlReaderWrapper, Task<string>>((thisPtr) => { return Task.FromResult<string>(thisPtr.reader.Value); });
            this.moveToContentFunc = new Func<XmlReaderWrapper, Task<XmlNodeType>>((thisPtr) => { return Task.FromResult<XmlNodeType>(this.reader.MoveToContent()); });
            this.skipFunc = new Func<XmlReaderWrapper, Task>((thisPtr) => { this.reader.Skip(); return Task.CompletedTask; });
            this.readFunc = new Func<XmlReaderWrapper, Task<bool>>((thisPtr) => { return Task.FromResult<bool>(this.reader.Read()); });
            this.readInnerXmlFunc = new Func<XmlReaderWrapper, Task<string>>((thisPtr) => { return Task.FromResult<string>(this.reader.ReadInnerXml()); });
        }

        public override XmlNodeType NodeType
        {
            get
            {
                return this.reader.NodeType;
            }
        }

        public override string LocalName
        {
            get
            {
                return this.reader.LocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return this.reader.NamespaceURI;
            }
        }

        public override string Prefix
        {
            get
            {
                return this.reader.Prefix;
            }
        }

        public override string Value
        {
            get
            {
                return this.reader.Value;
            }
        }

        public override int Depth
        {
            get
            {
                return this.reader.Depth;
            }
        }

        public override string BaseURI
        {
            get
            {
                return this.reader.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return this.reader.IsEmptyElement;
            }
        }

        public override int AttributeCount
        {
            get
            {
                return this.reader.AttributeCount;
            }
        }

        public override bool EOF
        {
            get
            {
                return this.reader.EOF;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                return this.reader.ReadState;
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return this.reader.NameTable;
            }
        }

        public override string GetAttribute(string name)
        {
            return this.reader.GetAttribute(name);
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return this.reader.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            return this.reader.GetAttribute(i);
        }

        public override string LookupNamespace(string prefix)
        {
            return this.reader.LookupNamespace(prefix);
        }

        public override bool MoveToAttribute(string name)
        {
            return this.reader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return this.reader.MoveToAttribute(name, ns);
        }

        public override bool MoveToElement()
        {
            return this.reader.MoveToElement();
        }

        public override bool MoveToFirstAttribute()
        {
            return this.reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return this.reader.MoveToNextAttribute();
        }

        public override bool Read()
        {
            return this.reader.Read();
        }

        public override bool ReadAttributeValue()
        {
            return this.reader.ReadAttributeValue();
        }

        public override void ResolveEntity()
        {
            this.reader.ResolveEntity();
        }

        public override Task<string> GetValueAsync()
        {
            return this.getValueFunc(this);
        }

        public override Task<XmlNodeType> MoveToContentAsync()
        {
            return this.moveToContentFunc(this);
        }

        public override Task SkipAsync()
        {
            return this.skipFunc(this);
        }

        public override Task<bool> ReadAsync()
        {
            return this.readFunc(this);
        }

        public override Task<string> ReadInnerXmlAsync()
        {
            return readInnerXmlFunc(this);
        }

        public static async Task WriteNodeAsync(XmlDictionaryWriter writer, XmlReader reader, bool defattr)
        {
            char[] writeNodeBuffer = null;
            const int WriteNodeBufferSize = 1024;

            if (null == reader)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            bool canReadChunk = reader.CanReadValueChunk;
            int d = reader.NodeType == XmlNodeType.None ? -1 : reader.Depth;
            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        writer.WriteAttributes(reader, defattr);
                        if (reader.IsEmptyElement)
                        {
                            writer.WriteEndElement();
                            break;
                        }
                        break;
                    case XmlNodeType.Text:
                        if (canReadChunk)
                        {
                            if (writeNodeBuffer == null)
                            {
                                writeNodeBuffer = new char[WriteNodeBufferSize];
                            }
                            int read;
                            while ((read = reader.ReadValueChunk(writeNodeBuffer, 0, WriteNodeBufferSize)) > 0)
                            {
                                writer.WriteChars(writeNodeBuffer, 0, read);
                            }
                        }
                        else
                        {
                            writer.WriteString(await reader.GetValueAsync());
                        }
                        break;

                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        writer.WriteWhitespace(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.CDATA:
                        writer.WriteCData(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.EntityReference:
                        writer.WriteEntityRef(reader.Name);
                        break;

                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.ProcessingInstruction:
                        writer.WriteProcessingInstruction(reader.Name, await reader.GetValueAsync());
                        break;

                    case XmlNodeType.DocumentType:
                        writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), await reader.GetValueAsync());
                        break;

                    case XmlNodeType.Comment:
                        writer.WriteComment(await reader.GetValueAsync());
                        break;

                    case XmlNodeType.EndElement:
                        writer.WriteFullEndElement();
                        break;
                }
            } while (await reader.ReadAsync() && (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement)));
        }
    }

    internal static class XmlReaderExtensions
    {
        static private uint IsTextualNodeBitmap = 0x6018; // 00 0110 0000 0001 1000

        public static async Task ReadStartElementAsync(this XmlReader reader)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }

            await reader.ReadAsync();
        }

        public static async Task<string> ReadElementStringAsync(this XmlReader reader)
        {
            string result = string.Empty;

            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                //throw new XmlException(Res.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            if (!reader.IsEmptyElement)
            {
                await reader.ReadAsync();
                result = reader.ReadString();
                if (reader.NodeType != XmlNodeType.EndElement)
                {
                throw new XmlException();
                    throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
                    //throw new XmlException(Res.Xml_UnexpectedNodeInSimpleContent, new string[] { this.NodeType.ToString(), "ReadElementString" }, this as IXmlLineInfo);
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
                    throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
                    //throw new InvalidOperationException(Res.GetString(Res.Xml_InvalidOperation));
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
                //throw new XmlException(Res.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
            }
            await reader.ReadAsync();
        }

        public static async Task ReadStartElementAsync(this XmlReader reader, string localname, string ns)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
            {
                //throw new XmlException(Res.Xml_InvalidNodeType, this.NodeType.ToString(), this as IXmlLineInfo);
                throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            if (reader.LocalName == localname && reader.NamespaceURI == ns)
            {
                await reader.ReadAsync();
            }
            else
            {
                throw new XmlException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
                //throw new XmlException(Res.Xml_ElementNotFoundNs, new string[2] { localname, ns }, this as IXmlLineInfo);
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
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            if (reader.Name == name)
            {
                await reader.ReadAsync();
            }
            else
            {
                throw new InvalidOperationException("name doesn’t match");
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
                throw new XmlException("InvalidNodeType");
            }
            if (reader.Name != name)
            {
                throw new XmlException("ElementNotFound");
            }

            if (!reader.IsEmptyElement)
            {
                result = await ReadStringAsync(reader);

                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException("InvalidNodeType");
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
                throw new XmlException("InvalidNodeType");
            }

            if (reader.LocalName != localname || reader.NamespaceURI != ns)
            {
                throw new XmlException("ElementNotFound");
            }

            if (!reader.IsEmptyElement)
            {
                result = await ReadStringAsync(reader);

                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException("InvalidNodeType");
                }
                await reader.ReadAsync();
            }
            else
            {
                await reader.ReadAsync();
            }
            return result;
        }

    }

}
