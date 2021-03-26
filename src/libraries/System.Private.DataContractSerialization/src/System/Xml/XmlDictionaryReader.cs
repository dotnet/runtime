// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public delegate void OnXmlDictionaryReaderClose(XmlDictionaryReader reader);

    public abstract class XmlDictionaryReader : XmlReader
    {
        internal const int MaxInitialArrayLength = 65535;

        public static XmlDictionaryReader CreateDictionaryReader(XmlReader reader)
        {
            if (reader == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(reader));

            XmlDictionaryReader? dictionaryReader = reader as XmlDictionaryReader;

            if (dictionaryReader == null)
            {
                dictionaryReader = new XmlWrappedReader(reader, null);
            }

            return dictionaryReader;
        }

        public static XmlDictionaryReader CreateBinaryReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            return CreateBinaryReader(buffer, 0, buffer.Length, quotas);
        }

        public static XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateBinaryReader(buffer, offset, count, null, quotas);
        }

        public static XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlDictionaryReaderQuotas quotas)
        {
            return CreateBinaryReader(buffer, offset, count, dictionary, quotas, null);
        }

        public static XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession? session)
        {
            return CreateBinaryReader(buffer, offset, count, dictionary, quotas, session, onClose: null);
        }

        public static XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count,
                                                             IXmlDictionary? dictionary,
                                                             XmlDictionaryReaderQuotas quotas,
                                                             XmlBinaryReaderSession? session,
                                                             OnXmlDictionaryReaderClose? onClose)
        {
            XmlBinaryReader reader = new XmlBinaryReader();
            reader.SetInput(buffer, offset, count, dictionary, quotas, session, onClose);
            return reader;
        }

        public static XmlDictionaryReader CreateBinaryReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateBinaryReader(stream, null, quotas);
        }

        public static XmlDictionaryReader CreateBinaryReader(Stream stream, IXmlDictionary? dictionary, XmlDictionaryReaderQuotas quotas)
        {
            return CreateBinaryReader(stream, dictionary, quotas, null);
        }

        public static XmlDictionaryReader CreateBinaryReader(Stream stream, IXmlDictionary? dictionary, XmlDictionaryReaderQuotas quotas, XmlBinaryReaderSession? session)
        {
            return CreateBinaryReader(stream, dictionary, quotas, session, onClose: null);
        }

        public static XmlDictionaryReader CreateBinaryReader(Stream stream,
                                                             IXmlDictionary? dictionary,
                                                             XmlDictionaryReaderQuotas quotas,
                                                             XmlBinaryReaderSession? session,
                                                             OnXmlDictionaryReaderClose? onClose)
        {
            XmlBinaryReader reader = new XmlBinaryReader();
            reader.SetInput(stream, dictionary, quotas, session, onClose);
            return reader;
        }

        public static XmlDictionaryReader CreateTextReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            if (buffer == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            return CreateTextReader(buffer, 0, buffer.Length, quotas);
        }

        public static XmlDictionaryReader CreateTextReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateTextReader(buffer, offset, count, null, quotas, null);
        }

        public static XmlDictionaryReader CreateTextReader(byte[] buffer, int offset, int count,
                                                           Encoding? encoding,
                                                           XmlDictionaryReaderQuotas quotas,
                                                           OnXmlDictionaryReaderClose? onClose)
        {
            XmlUTF8TextReader reader = new XmlUTF8TextReader();
            reader.SetInput(buffer, offset, count, encoding, quotas, onClose);
            return reader;
        }

        public static XmlDictionaryReader CreateTextReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateTextReader(stream, null, quotas, null);
        }

        public static XmlDictionaryReader CreateTextReader(Stream stream, Encoding? encoding,
                                                           XmlDictionaryReaderQuotas quotas,
                                                           OnXmlDictionaryReaderClose? onClose)
        {
            XmlUTF8TextReader reader = new XmlUTF8TextReader();
            reader.SetInput(stream, encoding, quotas, onClose);
            return reader;
        }

        public static XmlDictionaryReader CreateMtomReader(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas)
        {
            if (encoding == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(encoding));

            return CreateMtomReader(stream, new Encoding[1] { encoding }, quotas);
        }

        public static XmlDictionaryReader CreateMtomReader(Stream stream, Encoding[] encodings, XmlDictionaryReaderQuotas quotas)
        {
            return CreateMtomReader(stream, encodings, null, quotas);
        }

        public static XmlDictionaryReader CreateMtomReader(Stream stream, Encoding[] encodings, string? contentType, XmlDictionaryReaderQuotas quotas)
        {
            return CreateMtomReader(stream, encodings, contentType, quotas, int.MaxValue, null);
        }

        public static XmlDictionaryReader CreateMtomReader(Stream stream, Encoding[] encodings, string? contentType,
            XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose? onClose)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_MtomEncoding);
        }

        public static XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas)
        {
            if (encoding == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(encoding));

            return CreateMtomReader(buffer, offset, count, new Encoding[1] { encoding }, quotas);
        }

        public static XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, Encoding[] encodings, XmlDictionaryReaderQuotas quotas)
        {
            return CreateMtomReader(buffer, offset, count, encodings, null, quotas);
        }

        public static XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, Encoding[] encodings, string? contentType, XmlDictionaryReaderQuotas quotas)
        {
            return CreateMtomReader(buffer, offset, count, encodings, contentType, quotas, int.MaxValue, null);
        }

        public static XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, Encoding[] encodings, string? contentType,
            XmlDictionaryReaderQuotas quotas, int maxBufferSize, OnXmlDictionaryReaderClose? onClose)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_MtomEncoding);
        }

        public virtual bool CanCanonicalize
        {
            get
            {
                return false;
            }
        }

        public virtual XmlDictionaryReaderQuotas Quotas
        {
            get
            {
                return XmlDictionaryReaderQuotas.Max;
            }
        }

        public virtual void StartCanonicalization(Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        public virtual void EndCanonicalization()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        public virtual void MoveToStartElement()
        {
            if (!IsStartElement())
                XmlExceptionHelper.ThrowStartElementExpected(this);
        }

        public virtual void MoveToStartElement(string name)
        {
            if (!IsStartElement(name))
                XmlExceptionHelper.ThrowStartElementExpected(this, name);
        }

        public virtual void MoveToStartElement(string localName, string namespaceUri)
        {
            if (!IsStartElement(localName, namespaceUri))
                XmlExceptionHelper.ThrowStartElementExpected(this, localName, namespaceUri);
        }

        public virtual void MoveToStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (!IsStartElement(localName, namespaceUri))
                XmlExceptionHelper.ThrowStartElementExpected(this, localName, namespaceUri);
        }

        public virtual bool IsLocalName(string localName)
        {
            return this.LocalName == localName;
        }

        public virtual bool IsLocalName(XmlDictionaryString localName)
        {
            if (localName == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(localName));

            return IsLocalName(localName.Value);
        }

        public virtual bool IsNamespaceUri(string namespaceUri)
        {
            if (namespaceUri == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));
            return this.NamespaceURI == namespaceUri;
        }

        public virtual bool IsNamespaceUri(XmlDictionaryString namespaceUri)
        {
            if (namespaceUri == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));
            return IsNamespaceUri(namespaceUri.Value);
        }

        public virtual void ReadFullStartElement()
        {
            MoveToStartElement();
            if (IsEmptyElement)
                XmlExceptionHelper.ThrowFullStartElementExpected(this);
            Read();
        }

        public virtual void ReadFullStartElement(string name)
        {
            MoveToStartElement(name);
            if (IsEmptyElement)
                XmlExceptionHelper.ThrowFullStartElementExpected(this, name);
            Read();
        }

        public virtual void ReadFullStartElement(string localName, string namespaceUri)
        {
            MoveToStartElement(localName, namespaceUri);
            if (IsEmptyElement)
                XmlExceptionHelper.ThrowFullStartElementExpected(this, localName, namespaceUri);
            Read();
        }

        public virtual void ReadFullStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            MoveToStartElement(localName, namespaceUri);
            if (IsEmptyElement)
                XmlExceptionHelper.ThrowFullStartElementExpected(this, localName, namespaceUri);
            Read();
        }

        public virtual void ReadStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            MoveToStartElement(localName, namespaceUri);
            Read();
        }

        public virtual bool IsStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return IsStartElement(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public virtual int IndexOfLocalName(string[] localNames, string namespaceUri)
        {
            if (localNames == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(localNames));

            if (namespaceUri == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));

            if (this.NamespaceURI == namespaceUri)
            {
                string localName = this.LocalName;
                for (int i = 0; i < localNames.Length; i++)
                {
                    string value = localNames[i];
                    if (value == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(string.Format(CultureInfo.InvariantCulture, "localNames[{0}]", i));
                    if (localName == value)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public virtual int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString namespaceUri)
        {
            if (localNames == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(localNames));

            if (namespaceUri == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(namespaceUri));

            if (this.NamespaceURI == namespaceUri.Value)
            {
                string localName = this.LocalName;
                for (int i = 0; i < localNames.Length; i++)
                {
                    XmlDictionaryString value = localNames[i];
                    if (value == null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(string.Format(CultureInfo.InvariantCulture, "localNames[{0}]", i));
                    if (localName == value.Value)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public virtual string? GetAttribute(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return GetAttribute(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri));
        }

        public virtual bool TryGetBase64ContentLength(out int length)
        {
            length = 0;
            return false;
        }

        public virtual int ReadValueAsBase64(byte[] buffer, int offset, int count)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        public virtual byte[] ReadContentAsBase64()
        {
            return ReadContentAsBase64(Quotas.MaxArrayLength, MaxInitialArrayLength);
        }

        internal byte[] ReadContentAsBase64(int maxByteArrayContentLength, int maxInitialCount)
        {
            int length;
            if (TryGetBase64ContentLength(out length))
            {
                if (length <= maxInitialCount)
                {
                    byte[] buffer = new byte[length];
                    int read = 0;
                    while (read < length)
                    {
                        int actual = ReadContentAsBase64(buffer, read, length - read);
                        if (actual == 0)
                            XmlExceptionHelper.ThrowBase64DataExpected(this);
                        read += actual;
                    }
                    return buffer;
                }
            }
            return ReadContentAsBytes(true, maxByteArrayContentLength);
        }

        public override string ReadContentAsString()
        {
            return ReadContentAsString(Quotas.MaxStringContentLength);
        }

        protected string ReadContentAsString(int maxStringContentLength)
        {
            StringBuilder? sb = null;
            string result = string.Empty;
            bool done = false;
            while (true)
            {
                switch (this.NodeType)
                {
                    case XmlNodeType.Attribute:
                        result = this.Value;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        // merge text content
                        string value = this.Value;
                        if (result.Length == 0)
                        {
                            result = value;
                        }
                        else
                        {
                            if (sb == null)
                                sb = new StringBuilder(result);
                            if (sb.Length > maxStringContentLength - value.Length)
                                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, maxStringContentLength);
                            sb.Append(value);
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EndEntity:
                        // skip comments, pis and end entity nodes
                        break;
                    case XmlNodeType.EntityReference:
                        if (this.CanResolveEntity)
                        {
                            this.ResolveEntity();
                            break;
                        }
                        goto default;
                    case XmlNodeType.Element:
                    case XmlNodeType.EndElement:
                    default:
                        done = true;
                        break;
                }
                if (done)
                    break;
                if (this.AttributeCount != 0)
                    ReadAttributeValue();
                else
                    Read();
            }
            if (sb != null)
                result = sb.ToString();
            if (result.Length > maxStringContentLength)
                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, maxStringContentLength);
            return result;
        }

        public override string ReadString()
        {
            return ReadString(Quotas.MaxStringContentLength);
        }

        protected string ReadString(int maxStringContentLength)
        {
            if (this.ReadState != ReadState.Interactive)
                return string.Empty;
            if (this.NodeType != XmlNodeType.Element)
                MoveToElement();
            if (this.NodeType == XmlNodeType.Element)
            {
                if (this.IsEmptyElement)
                    return string.Empty;
                if (!Read())
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlInvalidOperation));
                if (this.NodeType == XmlNodeType.EndElement)
                    return string.Empty;
            }
            StringBuilder? sb = null;
            string result = string.Empty;
            while (IsTextNode(this.NodeType))
            {
                string value = this.Value;
                if (result.Length == 0)
                {
                    result = value;
                }
                else
                {
                    if (sb == null)
                        sb = new StringBuilder(result);
                    if (sb.Length > maxStringContentLength - value.Length)
                        XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, maxStringContentLength);
                    sb.Append(value);
                }
                if (!Read())
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlInvalidOperation));
            }
            if (sb != null)
                result = sb.ToString();
            if (result.Length > maxStringContentLength)
                XmlExceptionHelper.ThrowMaxStringContentLengthExceeded(this, maxStringContentLength);
            return result;
        }

        public virtual byte[] ReadContentAsBinHex()
        {
            return ReadContentAsBinHex(Quotas.MaxArrayLength);
        }

        protected byte[] ReadContentAsBinHex(int maxByteArrayContentLength)
        {
            return ReadContentAsBytes(false, maxByteArrayContentLength);
        }

        private byte[] ReadContentAsBytes(bool base64, int maxByteArrayContentLength)
        {
            byte[][] buffers = new byte[32][];
            byte[] buffer;
            // Its best to read in buffers that are a multiple of 3 so we don't break base64 boundaries when converting text
            int count = 384;
            int bufferCount = 0;
            int totalRead = 0;
            while (true)
            {
                buffer = new byte[count];
                buffers[bufferCount++] = buffer;
                int read = 0;
                while (read < buffer.Length)
                {
                    int actual;
                    if (base64)
                        actual = ReadContentAsBase64(buffer, read, buffer.Length - read);
                    else
                        actual = ReadContentAsBinHex(buffer, read, buffer.Length - read);
                    if (actual == 0)
                        break;
                    read += actual;
                }
                totalRead += read;
                if (read < buffer.Length)
                    break;
                count = count * 2;
            }
            buffer = new byte[totalRead];
            int offset = 0;
            for (int i = 0; i < bufferCount - 1; i++)
            {
                Buffer.BlockCopy(buffers[i], 0, buffer, offset, buffers[i].Length);
                offset += buffers[i].Length;
            }
            Buffer.BlockCopy(buffers[bufferCount - 1], 0, buffer, offset, totalRead - offset);
            return buffer;
        }

        protected bool IsTextNode(XmlNodeType nodeType)
        {
            return nodeType == XmlNodeType.Text ||
                nodeType == XmlNodeType.Whitespace ||
                nodeType == XmlNodeType.SignificantWhitespace ||
                nodeType == XmlNodeType.CDATA ||
                nodeType == XmlNodeType.Attribute;
        }

        public virtual int ReadContentAsChars(char[] chars, int offset, int count)
        {
            int read = 0;
            while (true)
            {
                XmlNodeType nodeType = this.NodeType;

                if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
                    break;

                if (IsTextNode(nodeType))
                {
                    read = ReadValueChunk(chars, offset, count);

                    if (read > 0)
                        break;

                    if (nodeType == XmlNodeType.Attribute /* || inAttributeText */)
                        break;

                    if (!Read())
                        break;
                }
                else
                {
                    if (!Read())
                        break;
                }
            }

            return read;
        }

        public override object ReadContentAs(Type type, IXmlNamespaceResolver? namespaceResolver)
        {
            if (type == typeof(Guid[]))
            {
                string[] values = (string[])ReadContentAs(typeof(string[]), namespaceResolver);
                Guid[] guids = new Guid[values.Length];
                for (int i = 0; i < values.Length; i++)
                    guids[i] = XmlConverter.ToGuid(values[i]);
                return guids;
            }
            if (type == typeof(UniqueId[]))
            {
                string[] values = (string[])ReadContentAs(typeof(string[]), namespaceResolver);
                UniqueId[] uniqueIds = new UniqueId[values.Length];
                for (int i = 0; i < values.Length; i++)
                    uniqueIds[i] = XmlConverter.ToUniqueId(values[i]);
                return uniqueIds;
            }
            return base.ReadContentAs(type, namespaceResolver);
        }

        public virtual string ReadContentAsString(string[] strings, out int index)
        {
            if (strings == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(strings));
            string s = ReadContentAsString();
            index = -1;
            for (int i = 0; i < strings.Length; i++)
            {
                string value = strings[i];
                if (value == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(string.Format(CultureInfo.InvariantCulture, "strings[{0}]", i));
                if (value == s)
                {
                    index = i;
                    return value;
                }
            }
            return s;
        }

        public virtual string ReadContentAsString(XmlDictionaryString[] strings, out int index)
        {
            if (strings == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(strings));
            string s = ReadContentAsString();
            index = -1;
            for (int i = 0; i < strings.Length; i++)
            {
                XmlDictionaryString value = strings[i];
                if (value == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(string.Format(CultureInfo.InvariantCulture, "strings[{0}]", i));
                if (value.Value == s)
                {
                    index = i;
                    return value.Value;
                }
            }
            return s;
        }

        public override decimal ReadContentAsDecimal()
        {
            return XmlConverter.ToDecimal(ReadContentAsString());
        }

        public override float ReadContentAsFloat()
        {
            return XmlConverter.ToSingle(ReadContentAsString());
        }

        public virtual UniqueId ReadContentAsUniqueId()
        {
            return XmlConverter.ToUniqueId(ReadContentAsString());
        }

        public virtual Guid ReadContentAsGuid()
        {
            return XmlConverter.ToGuid(ReadContentAsString());
        }

        public virtual TimeSpan ReadContentAsTimeSpan()
        {
            return XmlConverter.ToTimeSpan(ReadContentAsString());
        }

        public virtual void ReadContentAsQualifiedName(out string localName, out string namespaceUri)
        {
            string prefix;
            XmlConverter.ToQualifiedName(ReadContentAsString(), out prefix, out localName);
            namespaceUri = LookupNamespace(prefix)!;
            if (namespaceUri == null)
                XmlExceptionHelper.ThrowUndefinedPrefix(this, prefix);
        }

        /* string, bool, int, long, float, double, decimal, DateTime, base64, binhex, uniqueID, object, list*/
        public override string ReadElementContentAsString()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            string value;

            if (isEmptyElement)
            {
                Read();
                value = string.Empty;
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsString();
                ReadEndElement();
            }

            return value;
        }

        public override bool ReadElementContentAsBoolean()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            bool value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToBoolean(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsBoolean();
                ReadEndElement();
            }

            return value;
        }

        public override int ReadElementContentAsInt()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            int value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToInt32(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsInt();
                ReadEndElement();
            }

            return value;
        }

        public override long ReadElementContentAsLong()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            long value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToInt64(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsLong();
                ReadEndElement();
            }

            return value;
        }

        public override float ReadElementContentAsFloat()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            float value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToSingle(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsFloat();
                ReadEndElement();
            }

            return value;
        }

        public override double ReadElementContentAsDouble()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            double value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToDouble(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsDouble();
                ReadEndElement();
            }

            return value;
        }

        public override decimal ReadElementContentAsDecimal()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            decimal value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToDecimal(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsDecimal();
                ReadEndElement();
            }

            return value;
        }

        public override DateTime ReadElementContentAsDateTime()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            DateTime value;

            if (isEmptyElement)
            {
                Read();
                try
                {
                    value = DateTime.Parse(string.Empty, NumberFormatInfo.InvariantInfo);
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "DateTime", exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "DateTime", exception));
                }
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsDateTime();
                ReadEndElement();
            }

            return value;
        }

        public virtual UniqueId ReadElementContentAsUniqueId()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            UniqueId value;

            if (isEmptyElement)
            {
                Read();
                try
                {
                    value = new UniqueId(string.Empty);
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "UniqueId", exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "UniqueId", exception));
                }
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsUniqueId();
                ReadEndElement();
            }

            return value;
        }

        public virtual Guid ReadElementContentAsGuid()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            Guid value;

            if (isEmptyElement)
            {
                Read();
                try
                {
                    value = new Guid(string.Empty);
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "Guid", exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "Guid", exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(string.Empty, "Guid", exception));
                }
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsGuid();
                ReadEndElement();
            }

            return value;
        }

        public virtual TimeSpan ReadElementContentAsTimeSpan()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            TimeSpan value;

            if (isEmptyElement)
            {
                Read();
                value = XmlConverter.ToTimeSpan(string.Empty);
            }
            else
            {
                ReadStartElement();
                value = ReadContentAsTimeSpan();
                ReadEndElement();
            }

            return value;
        }

        public virtual byte[] ReadElementContentAsBase64()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            byte[] buffer;

            if (isEmptyElement)
            {
                Read();
                buffer = Array.Empty<byte>();
            }
            else
            {
                ReadStartElement();
                buffer = ReadContentAsBase64();
                ReadEndElement();
            }

            return buffer;
        }

        public virtual byte[] ReadElementContentAsBinHex()
        {
            bool isEmptyElement = IsStartElement() && IsEmptyElement;
            byte[] buffer;

            if (isEmptyElement)
            {
                Read();
                buffer = Array.Empty<byte>();
            }
            else
            {
                ReadStartElement();
                buffer = ReadContentAsBinHex();
                ReadEndElement();
            }

            return buffer;
        }

        public virtual void GetNonAtomizedNames(out string localName, out string namespaceUri)
        {
            localName = LocalName;
            namespaceUri = NamespaceURI;
        }

        public virtual bool TryGetLocalNameAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? localName)
        {
            localName = null;
            return false;
        }

        public virtual bool TryGetNamespaceUriAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? namespaceUri)
        {
            namespaceUri = null;
            return false;
        }

        public virtual bool TryGetValueAsDictionaryString([NotNullWhen(true)] out XmlDictionaryString? value)
        {
            value = null;
            return false;
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

        public virtual bool IsStartArray([NotNullWhen(true)] out Type? type)
        {
            type = null;
            return false;
        }

        public virtual bool TryGetArrayLength(out int count)
        {
            count = 0;
            return false;
        }

        // Boolean
        public virtual bool[] ReadBooleanArray(string localName, string namespaceUri)
        {
            return BooleanArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual bool[] ReadBooleanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return BooleanArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, bool[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsBoolean();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int16
        public virtual short[] ReadInt16Array(string localName, string namespaceUri)
        {
            return Int16ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual short[] ReadInt16Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int16ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                int i = ReadElementContentAsInt();
                if (i < short.MinValue || i > short.MaxValue)
                    XmlExceptionHelper.ThrowConversionOverflow(this, i.ToString(NumberFormatInfo.CurrentInfo), "Int16");
                array[offset + actual] = (short)i;
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int32
        public virtual int[] ReadInt32Array(string localName, string namespaceUri)
        {
            return Int32ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int[] ReadInt32Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int32ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsInt();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Int64
        public virtual long[] ReadInt64Array(string localName, string namespaceUri)
        {
            return Int64ArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual long[] ReadInt64Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return Int64ArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsLong();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Single
        public virtual float[] ReadSingleArray(string localName, string namespaceUri)
        {
            return SingleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual float[] ReadSingleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return SingleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsFloat();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Double
        public virtual double[] ReadDoubleArray(string localName, string namespaceUri)
        {
            return DoubleArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual double[] ReadDoubleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DoubleArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsDouble();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Decimal
        public virtual decimal[] ReadDecimalArray(string localName, string namespaceUri)
        {
            return DecimalArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual decimal[] ReadDecimalArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DecimalArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsDecimal();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // DateTime
        public virtual DateTime[] ReadDateTimeArray(string localName, string namespaceUri)
        {
            return DateTimeArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual DateTime[] ReadDateTimeArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return DateTimeArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, DateTime[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsDateTime();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // Guid
        public virtual Guid[] ReadGuidArray(string localName, string namespaceUri)
        {
            return GuidArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual Guid[] ReadGuidArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return GuidArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, Guid[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsGuid();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        // TimeSpan
        public virtual TimeSpan[] ReadTimeSpanArray(string localName, string namespaceUri)
        {
            return TimeSpanArrayHelperWithString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual TimeSpan[] ReadTimeSpanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            return TimeSpanArrayHelperWithDictionaryString.Instance.ReadArray(this, localName, namespaceUri, Quotas.MaxArrayLength);
        }

        public virtual int ReadArray(string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
        {
            CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && IsStartElement(localName, namespaceUri))
            {
                array[offset + actual] = ReadElementContentAsTimeSpan();
                actual++;
            }
            return actual;
        }

        public virtual int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
        {
            return ReadArray(XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(namespaceUri), array, offset, count);
        }

        private sealed class XmlWrappedReader : XmlDictionaryReader, IXmlLineInfo
        {
            private readonly XmlReader _reader;
            private XmlNamespaceManager? _nsMgr;

            public XmlWrappedReader(XmlReader reader, XmlNamespaceManager? nsMgr)
            {
                _reader = reader;
                _nsMgr = nsMgr;
            }

            public override int AttributeCount
            {
                get
                {
                    return _reader.AttributeCount;
                }
            }

            public override string BaseURI
            {
                get
                {
                    return _reader.BaseURI;
                }
            }

            public override bool CanReadBinaryContent
            {
                get { return _reader.CanReadBinaryContent; }
            }

            public override bool CanReadValueChunk
            {
                get { return _reader.CanReadValueChunk; }
            }

            public override void Close()
            {
                _reader.Dispose();
                _nsMgr = null;
            }

            public override int Depth
            {
                get
                {
                    return _reader.Depth;
                }
            }

            public override bool EOF
            {
                get
                {
                    return _reader.EOF;
                }
            }

            public override string GetAttribute(int index)
            {
                return _reader.GetAttribute(index);
            }

            public override string? GetAttribute(string name)
            {
                return _reader.GetAttribute(name);
            }

            public override string? GetAttribute(string name, string? namespaceUri)
            {
                return _reader.GetAttribute(name, namespaceUri);
            }

            public override bool HasValue
            {
                get
                {
                    return _reader.HasValue;
                }
            }

            public override bool IsDefault
            {
                get
                {
                    return _reader.IsDefault;
                }
            }

            public override bool IsEmptyElement
            {
                get
                {
                    return _reader.IsEmptyElement;
                }
            }

            public override bool IsStartElement(string name)
            {
                return _reader.IsStartElement(name);
            }

            public override bool IsStartElement(string localName, string namespaceUri)
            {
                return _reader.IsStartElement(localName, namespaceUri);
            }

            public override string LocalName
            {
                get
                {
                    return _reader.LocalName;
                }
            }

            public override string? LookupNamespace(string namespaceUri)
            {
                return _reader.LookupNamespace(namespaceUri);
            }

            public override void MoveToAttribute(int index)
            {
                _reader.MoveToAttribute(index);
            }

            public override bool MoveToAttribute(string name)
            {
                return _reader.MoveToAttribute(name);
            }

            public override bool MoveToAttribute(string name, string? namespaceUri)
            {
                return _reader.MoveToAttribute(name, namespaceUri);
            }

            public override bool MoveToElement()
            {
                return _reader.MoveToElement();
            }

            public override bool MoveToFirstAttribute()
            {
                return _reader.MoveToFirstAttribute();
            }

            public override bool MoveToNextAttribute()
            {
                return _reader.MoveToNextAttribute();
            }

            public override string Name
            {
                get
                {
                    return _reader.Name;
                }
            }

            public override string NamespaceURI
            {
                get
                {
                    return _reader.NamespaceURI;
                }
            }

            public override XmlNameTable NameTable
            {
                get
                {
                    return _reader.NameTable;
                }
            }

            public override XmlNodeType NodeType
            {
                get
                {
                    return _reader.NodeType;
                }
            }

            public override string Prefix
            {
                get
                {
                    return _reader.Prefix;
                }
            }


            public override bool Read()
            {
                return _reader.Read();
            }

            public override bool ReadAttributeValue()
            {
                return _reader.ReadAttributeValue();
            }


            public override string ReadInnerXml()
            {
                return _reader.ReadInnerXml();
            }

            public override string ReadOuterXml()
            {
                return _reader.ReadOuterXml();
            }

            public override void ReadStartElement(string name)
            {
                _reader.ReadStartElement(name);
            }

            public override void ReadStartElement(string localName, string namespaceUri)
            {
                _reader.ReadStartElement(localName, namespaceUri);
            }

            public override void ReadEndElement()
            {
                _reader.ReadEndElement();
            }

            public override ReadState ReadState
            {
                get
                {
                    return _reader.ReadState;
                }
            }

            public override void ResolveEntity()
            {
                _reader.ResolveEntity();
            }

            public override string this[int index]
            {
                get
                {
                    return _reader[index];
                }
            }

            public override string? this[string name]
            {
                get
                {
                    return _reader[name];
                }
            }

            public override string? this[string name, string? namespaceUri]
            {
                get
                {
                    return _reader[name, namespaceUri];
                }
            }

            public override string Value
            {
                get
                {
                    return _reader.Value;
                }
            }

            public override string XmlLang
            {
                get
                {
                    return _reader.XmlLang;
                }
            }

            public override XmlSpace XmlSpace
            {
                get
                {
                    return _reader.XmlSpace;
                }
            }

            public override int ReadElementContentAsBase64(byte[] buffer, int offset, int count)
            {
                return _reader.ReadElementContentAsBase64(buffer, offset, count);
            }

            public override int ReadContentAsBase64(byte[] buffer, int offset, int count)
            {
                return _reader.ReadContentAsBase64(buffer, offset, count);
            }

            public override int ReadElementContentAsBinHex(byte[] buffer, int offset, int count)
            {
                return _reader.ReadElementContentAsBinHex(buffer, offset, count);
            }

            public override int ReadContentAsBinHex(byte[] buffer, int offset, int count)
            {
                return _reader.ReadContentAsBinHex(buffer, offset, count);
            }

            public override int ReadValueChunk(char[] chars, int offset, int count)
            {
                return _reader.ReadValueChunk(chars, offset, count);
            }

            public override Type ValueType
            {
                get
                {
                    return _reader.ValueType;
                }
            }

            public override bool ReadContentAsBoolean()
            {
                try
                {
                    return _reader.ReadContentAsBoolean();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Boolean), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Boolean), exception));
                }
            }

            public override DateTime ReadContentAsDateTime()
            {
                return _reader.ReadContentAsDateTime();
            }

            public override decimal ReadContentAsDecimal()
            {
                try
                {
                    return (decimal)_reader.ReadContentAs(typeof(decimal), null);
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Decimal), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Decimal), exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Decimal), exception));
                }
            }

            public override double ReadContentAsDouble()
            {
                try
                {
                    return _reader.ReadContentAsDouble();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Double), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Double), exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Double), exception));
                }
            }

            public override int ReadContentAsInt()
            {
                try
                {
                    return _reader.ReadContentAsInt();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int32), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int32), exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int32), exception));
                }
            }

            public override long ReadContentAsLong()
            {
                try
                {
                    return _reader.ReadContentAsLong();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int64), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int64), exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Int64), exception));
                }
            }

            public override float ReadContentAsFloat()
            {
                try
                {
                    return _reader.ReadContentAsFloat();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Single), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Single), exception));
                }
                catch (OverflowException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(Single), exception));
                }
            }

            public override string ReadContentAsString()
            {
                try
                {
                    return _reader.ReadContentAsString();
                }
                catch (ArgumentException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(String), exception));
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(nameof(String), exception));
                }
            }

            public override object ReadContentAs(Type type, IXmlNamespaceResolver? namespaceResolver)
            {
                return _reader.ReadContentAs(type, namespaceResolver);
            }

            public bool HasLineInfo()
            {
                IXmlLineInfo? lineInfo = _reader as IXmlLineInfo;

                if (lineInfo == null)
                    return false;

                return lineInfo.HasLineInfo();
            }

            public int LineNumber
            {
                get
                {
                    IXmlLineInfo? lineInfo = _reader as IXmlLineInfo;

                    if (lineInfo == null)
                        return 1;

                    return lineInfo.LineNumber;
                }
            }

            public int LinePosition
            {
                get
                {
                    IXmlLineInfo? lineInfo = _reader as IXmlLineInfo;

                    if (lineInfo == null)
                        return 1;

                    return lineInfo.LinePosition;
                }
            }
        }
    }
}
