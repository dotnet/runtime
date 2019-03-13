// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    public abstract class XmlDictionaryWriter : XmlWriter
    {
        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream)
        {
            return CreateBinaryWriter(stream, null);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary dictionary)
        {
            return CreateBinaryWriter(stream, dictionary, null);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary dictionary, XmlBinaryWriterSession session)
        {
            return CreateBinaryWriter(stream, dictionary, session, true);
        }

        public static XmlDictionaryWriter CreateBinaryWriter(Stream stream, IXmlDictionary dictionary, XmlBinaryWriterSession session, bool ownsStream)
        {
            XmlBinaryWriter writer = new XmlBinaryWriter();
            writer.SetOutput(stream, dictionary, session, ownsStream);
            return writer;
        }

        private static readonly Encoding s_UTF8Encoding = new UTF8Encoding(false);
        public static XmlDictionaryWriter CreateTextWriter(Stream stream)
        {
            return CreateTextWriter(stream, s_UTF8Encoding, true);
        }

        public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding)
        {
            return CreateTextWriter(stream, encoding, true);
        }

        public static XmlDictionaryWriter CreateTextWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            XmlUTF8TextWriter writer = new XmlUTF8TextWriter();
            writer.SetOutput(stream, encoding, ownsStream);
            var asyncWriter = new XmlDictionaryAsyncCheckWriter(writer);
            return asyncWriter;
        }

        public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo)
        {
            return CreateMtomWriter(stream, encoding, maxSizeInBytes, startInfo, null, null, true, true);
        }

        public static XmlDictionaryWriter CreateMtomWriter(Stream stream, Encoding encoding, int maxSizeInBytes, string startInfo, string boundary, string startUri, bool writeMessageHeaders, bool ownsStream)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_MtomEncoding);
        }

        public static XmlDictionaryWriter CreateDictionaryWriter(XmlWriter writer)
        {
            if (writer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(writer));

            XmlDictionaryWriter dictionaryWriter = writer as XmlDictionaryWriter;

            if (dictionaryWriter == null)
            {
                dictionaryWriter = new XmlWrappedWriter(writer);
            }

            return dictionaryWriter;
        }

        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            WriteBase64(buffer, index, count);
            return Task.CompletedTask;
        }

        public void WriteStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            WriteStartElement((string)null, localName, namespaceUri);
        }

        public virtual void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            WriteStartElement(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public void WriteStartAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            WriteStartAttribute((string)null, localName, namespaceUri);
        }

        public virtual void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            WriteStartAttribute(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public void WriteAttributeString(XmlDictionaryString localName, XmlDictionaryString namespaceUri, string value)
        {
            WriteAttributeString((string)null, localName, namespaceUri, value);
        }

        public virtual void WriteXmlnsAttribute(string prefix, string namespaceUri)
        {
            if (namespaceUri == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));
            if (prefix == null)
            {
                if (LookupPrefix(namespaceUri) != null)
                    return;
#pragma warning suppress 56506 // Microsoft, namespaceUri is already checked
                prefix = namespaceUri.Length == 0 ? string.Empty : string.Concat("d", namespaceUri.Length.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
            }
            WriteAttributeString("xmlns", prefix, null, namespaceUri);
        }

        public virtual void WriteXmlnsAttribute(string prefix, XmlDictionaryString namespaceUri)
        {
            WriteXmlnsAttribute(prefix, XmlDictionaryString.GetString(namespaceUri));
        }

        public virtual void WriteXmlAttribute(string localName, string value)
        {
            WriteAttributeString("xml", localName, null, value);
        }

        public virtual void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString value)
        {
            WriteXmlAttribute(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(value));
        }

        public void WriteAttributeString(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, string value)
        {
            WriteStartAttribute(prefix, localName, namespaceUri);
            WriteString(value);
            WriteEndAttribute();
        }

        public void WriteElementString(XmlDictionaryString localName, XmlDictionaryString namespaceUri, string value)
        {
            WriteElementString((string)null, localName, namespaceUri, value);
        }

        public void WriteElementString(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, string value)
        {
            WriteStartElement(prefix, localName, namespaceUri);
            WriteString(value);
            WriteEndElement();
        }

        public virtual void WriteString(XmlDictionaryString value)
        {
            WriteString(XmlDictionaryString.GetString(value));
        }

        public virtual void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (localName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(localName)));
            if (namespaceUri == null)
                namespaceUri = XmlDictionaryString.Empty;
#pragma warning suppress 56506 // Microsoft, XmlDictionaryString.Empty is never null
            WriteQualifiedName(localName.Value, namespaceUri.Value);
        }

        public virtual void WriteValue(XmlDictionaryString value)
        {
            WriteValue(XmlDictionaryString.GetString(value));
        }


        public virtual void WriteValue(UniqueId value)
        {
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));

            WriteString(value.ToString());
        }

        public virtual void WriteValue(Guid value)
        {
            WriteString(value.ToString());
        }

        public virtual void WriteValue(TimeSpan value)
        {
            WriteString(XmlConvert.ToString(value));
        }

        public virtual void WriteValue(IStreamProvider value)
        {
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(value)));

            Stream stream = value.GetStream();
            if (stream == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.XmlInvalidStream));
            int blockSize = 256;
            int bytesRead = 0;
            byte[] block = new byte[blockSize];
            while (true)
            {
                bytesRead = stream.Read(block, 0, blockSize);
                if (bytesRead > 0)
                    WriteBase64(block, 0, bytesRead);
                else
                    break;
                if (blockSize < 65536 && bytesRead == blockSize)
                {
                    blockSize = blockSize * 16;
                    block = new byte[blockSize];
                }
            }
            value.ReleaseStream(stream);
        }

        public virtual Task WriteValueAsync(IStreamProvider value)
        {
            WriteValue(value);
            return Task.CompletedTask;
        }

        public virtual bool CanCanonicalize
        {
            get
            {
                return false;
            }
        }

        public virtual void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        public virtual void EndCanonicalization()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        private void WriteElementNode(XmlDictionaryReader reader, bool defattr)
        {
            XmlDictionaryString localName;
            XmlDictionaryString namespaceUri;
            if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
            {
                WriteStartElement(reader.Prefix, localName, namespaceUri);
            }
            else
            {
                WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
            }
            if (defattr || !reader.IsDefault)
            {
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
                        {
                            WriteStartAttribute(reader.Prefix, localName, namespaceUri);
                        }
                        else
                        {
                            WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        }
                        while (reader.ReadAttributeValue())
                        {
                            if (reader.NodeType == XmlNodeType.EntityReference)
                            {
                                WriteEntityRef(reader.Name);
                            }
                            else
                            {
                                WriteTextNode(reader, true);
                            }
                        }
                        WriteEndAttribute();
                    }
                    while (reader.MoveToNextAttribute());
                    reader.MoveToElement();
                }
            }
            if (reader.IsEmptyElement)
            {
                WriteEndElement();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, string prefix, string localName, string namespaceUri, Type type)
        {
            if (type == typeof(bool))
                BooleanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(short))
                Int16ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(int))
                Int32ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(long))
                Int64ArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(float))
                SingleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(double))
                DoubleArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(decimal))
                DecimalArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(DateTime))
                DateTimeArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(Guid))
                GuidArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(TimeSpan))
                TimeSpanArrayHelperWithString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else
            {
                WriteElementNode(reader, false);
                reader.Read();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Type type)
        {
            if (type == typeof(bool))
                BooleanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(short))
                Int16ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(int))
                Int32ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(long))
                Int64ArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(float))
                SingleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(double))
                DoubleArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(decimal))
                DecimalArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(DateTime))
                DateTimeArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(Guid))
                GuidArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else if (type == typeof(TimeSpan))
                TimeSpanArrayHelperWithDictionaryString.Instance.WriteArray(this, prefix, localName, namespaceUri, reader);
            else
            {
                WriteElementNode(reader, false);
                reader.Read();
            }
        }

        private void WriteArrayNode(XmlDictionaryReader reader, Type type)
        {
            XmlDictionaryString localName;
            XmlDictionaryString namespaceUri;
            if (reader.TryGetLocalNameAsDictionaryString(out localName) && reader.TryGetNamespaceUriAsDictionaryString(out namespaceUri))
                WriteArrayNode(reader, reader.Prefix, localName, namespaceUri, type);
            else
                WriteArrayNode(reader, reader.Prefix, reader.LocalName, reader.NamespaceURI, type);
        }

        protected virtual void WriteTextNode(XmlDictionaryReader reader, bool isAttribute)
        {
            XmlDictionaryString value;
            if (reader.TryGetValueAsDictionaryString(out value))
            {
                WriteString(value);
            }
            else
            {
                WriteString(reader.Value);
            }
            if (!isAttribute)
            {
                reader.Read();
            }
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            XmlDictionaryReader dictionaryReader = reader as XmlDictionaryReader;
            if (dictionaryReader != null)
                WriteNode(dictionaryReader, defattr);
            else
                base.WriteNode(reader, defattr);
        }

        public virtual void WriteNode(XmlDictionaryReader reader, bool defattr)
        {
            if (reader == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(reader)));
            int d = (reader.NodeType == XmlNodeType.None ? -1 : reader.Depth);
            do
            {
                XmlNodeType nodeType = reader.NodeType;
                Type type;
                if (nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace)
                {
                    // This will advance if necessary, so we don't need to call Read() explicitly
                    WriteTextNode(reader, false);
                }
                else if (reader.Depth > d && reader.IsStartArray(out type))
                {
                    WriteArrayNode(reader, type);
                }
                else
                {
                    // These will not advance, so we must call Read() explicitly
                    switch (nodeType)
                    {
                        case XmlNodeType.Element:
                            WriteElementNode(reader, defattr);
                            break;
                        case XmlNodeType.CDATA:
                            WriteCData(reader.Value);
                            break;
                        case XmlNodeType.EntityReference:
                            WriteEntityRef(reader.Name);
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            WriteProcessingInstruction(reader.Name, reader.Value);
                            break;
                        case XmlNodeType.DocumentType:
                            WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                            break;
                        case XmlNodeType.Comment:
                            WriteComment(reader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            WriteFullEndElement();
                            break;
                    }
                    if (!reader.Read())
                        break;
                }
            }
            while (d < reader.Depth || (d == reader.Depth && reader.NodeType == XmlNodeType.EndElement));
        }

        private void CheckArray(Array array, int offset, int count)
        {
            if (array == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(array)));
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > array.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > array.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset)));
        }

        // bool
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, bool[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int16
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, short[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int32
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, int[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int64
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, long[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // float
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, float[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // double
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, double[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // decimal
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // DateTime
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Guid
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, Guid[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // TimeSpan
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            for (int i = 0; i < count; i++)
            {
                WriteStartElement(prefix, localName, namespaceUri);
                WriteValue(array[offset + i]);
                WriteEndElement();
            }
        }

        public virtual void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
        {
            WriteArray(prefix, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && WriteState != WriteState.Closed)
            {
                Close();
            }
        }

        public override void Close() { }

        private class XmlWrappedWriter : XmlDictionaryWriter
        {
            private XmlWriter _writer;
            private int _depth;
            private int _prefix;

            public XmlWrappedWriter(XmlWriter writer)
            {
                _writer = writer;
                _depth = 0;
            }

            public override void Close()
            {
                _writer.Dispose();
            }

            public override void Flush()
            {
                _writer.Flush();
            }

            public override string LookupPrefix(string namespaceUri)
            {
                return _writer.LookupPrefix(namespaceUri);
            }

            public override void WriteAttributes(XmlReader reader, bool defattr)
            {
                _writer.WriteAttributes(reader, defattr);
            }

            public override void WriteBase64(byte[] buffer, int index, int count)
            {
                _writer.WriteBase64(buffer, index, count);
            }

            public override void WriteBinHex(byte[] buffer, int index, int count)
            {
                _writer.WriteBinHex(buffer, index, count);
            }

            public override void WriteCData(string text)
            {
                _writer.WriteCData(text);
            }

            public override void WriteCharEntity(char ch)
            {
                _writer.WriteCharEntity(ch);
            }

            public override void WriteChars(char[] buffer, int index, int count)
            {
                _writer.WriteChars(buffer, index, count);
            }

            public override void WriteComment(string text)
            {
                _writer.WriteComment(text);
            }

            public override void WriteDocType(string name, string pubid, string sysid, string subset)
            {
                _writer.WriteDocType(name, pubid, sysid, subset);
            }

            public override void WriteEndAttribute()
            {
                _writer.WriteEndAttribute();
            }

            public override void WriteEndDocument()
            {
                _writer.WriteEndDocument();
            }

            public override void WriteEndElement()
            {
                _writer.WriteEndElement();
                _depth--;
            }

            public override void WriteEntityRef(string name)
            {
                _writer.WriteEntityRef(name);
            }

            public override void WriteFullEndElement()
            {
                _writer.WriteFullEndElement();
            }

            public override void WriteName(string name)
            {
                _writer.WriteName(name);
            }

            public override void WriteNmToken(string name)
            {
                _writer.WriteNmToken(name);
            }

            public override void WriteNode(XmlReader reader, bool defattr)
            {
                _writer.WriteNode(reader, defattr);
            }

            public override void WriteProcessingInstruction(string name, string text)
            {
                _writer.WriteProcessingInstruction(name, text);
            }

            public override void WriteQualifiedName(string localName, string namespaceUri)
            {
                _writer.WriteQualifiedName(localName, namespaceUri);
            }

            public override void WriteRaw(char[] buffer, int index, int count)
            {
                _writer.WriteRaw(buffer, index, count);
            }

            public override void WriteRaw(string data)
            {
                _writer.WriteRaw(data);
            }

            public override void WriteStartAttribute(string prefix, string localName, string namespaceUri)
            {
                _writer.WriteStartAttribute(prefix, localName, namespaceUri);
                _prefix++;
            }

            public override void WriteStartDocument()
            {
                _writer.WriteStartDocument();
            }

            public override void WriteStartDocument(bool standalone)
            {
                _writer.WriteStartDocument(standalone);
            }

            public override void WriteStartElement(string prefix, string localName, string namespaceUri)
            {
                _writer.WriteStartElement(prefix, localName, namespaceUri);
                _depth++;
                _prefix = 1;
            }

            public override WriteState WriteState
            {
                get
                {
                    return _writer.WriteState;
                }
            }

            public override void WriteString(string text)
            {
                _writer.WriteString(text);
            }

            public override void WriteSurrogateCharEntity(char lowChar, char highChar)
            {
                _writer.WriteSurrogateCharEntity(lowChar, highChar);
            }

            public override void WriteWhitespace(string whitespace)
            {
                _writer.WriteWhitespace(whitespace);
            }

            public override void WriteValue(object value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(string value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(bool value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(DateTimeOffset value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(double value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(int value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteValue(long value)
            {
                _writer.WriteValue(value);
            }

            public override void WriteXmlnsAttribute(string prefix, string namespaceUri)
            {
                if (namespaceUri == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));
                if (prefix == null)
                {
                    if (LookupPrefix(namespaceUri) != null)
                        return;

                    if (namespaceUri.Length == 0)
                    {
                        prefix = string.Empty;
                    }
                    else
                    {
                        string depthStr = _depth.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                        string prefixStr = _prefix.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
                        prefix = string.Concat("d", depthStr, "p", prefixStr);
                    }
                }
                WriteAttributeString("xmlns", prefix, null, namespaceUri);
            }

            public override string XmlLang
            {
                get
                {
                    return _writer.XmlLang;
                }
            }

            public override XmlSpace XmlSpace
            {
                get
                {
                    return _writer.XmlSpace;
                }
            }
        }
    }
}
