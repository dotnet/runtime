// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Globalization;
using System.Collections.Generic;


namespace System.Runtime.Serialization
{
#if USE_REFEMIT || NET_NATIVE
    public class XmlReaderDelegator
#else
    internal class XmlReaderDelegator
#endif
    {
        protected XmlReader reader;
        protected XmlDictionaryReader dictionaryReader;
        protected bool isEndOfEmptyElement = false;

        public XmlReaderDelegator(XmlReader reader)
        {
            XmlObjectSerializer.CheckNull(reader, nameof(reader));
            this.reader = reader;
            this.dictionaryReader = reader as XmlDictionaryReader;
        }

        internal XmlReader UnderlyingReader
        {
            get { return reader; }
        }

        internal int AttributeCount
        {
            get { return isEndOfEmptyElement ? 0 : reader.AttributeCount; }
        }

        internal string GetAttribute(string name)
        {
            return isEndOfEmptyElement ? null : reader.GetAttribute(name);
        }

        internal string GetAttribute(string name, string namespaceUri)
        {
            return isEndOfEmptyElement ? null : reader.GetAttribute(name, namespaceUri);
        }

        internal string GetAttribute(int i)
        {
            if (isEndOfEmptyElement)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(i), SR.Format(SR.XmlElementAttributes)));
            return reader.GetAttribute(i);
        }

        internal bool IsEmptyElement
        {
            get { return false; }
        }

        internal bool IsNamespaceURI(string ns)
        {
            if (dictionaryReader == null)
                return ns == reader.NamespaceURI;
            else
                return dictionaryReader.IsNamespaceUri(ns);
        }

        internal bool IsLocalName(string localName)
        {
            if (dictionaryReader == null)
                return localName == reader.LocalName;
            else
                return dictionaryReader.IsLocalName(localName);
        }

        internal bool IsNamespaceUri(XmlDictionaryString ns)
        {
            if (dictionaryReader == null)
                return ns.Value == reader.NamespaceURI;
            else
                return dictionaryReader.IsNamespaceUri(ns);
        }

        internal bool IsLocalName(XmlDictionaryString localName)
        {
            if (dictionaryReader == null)
                return localName.Value == reader.LocalName;
            else
                return dictionaryReader.IsLocalName(localName);
        }

        internal int IndexOfLocalName(XmlDictionaryString[] localNames, XmlDictionaryString ns)
        {
            if (dictionaryReader != null)
                return dictionaryReader.IndexOfLocalName(localNames, ns);

            if (reader.NamespaceURI == ns.Value)
            {
                string localName = this.LocalName;
                for (int i = 0; i < localNames.Length; i++)
                {
                    if (localName == localNames[i].Value)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

#if USE_REFEMIT        
        public bool IsStartElement()
#else
        internal bool IsStartElement()
#endif
        {
            return !isEndOfEmptyElement && reader.IsStartElement();
        }

        internal bool IsStartElement(string localname, string ns)
        {
            return !isEndOfEmptyElement && reader.IsStartElement(localname, ns);
        }

#if USE_REFEMIT        
        public bool IsStartElement(XmlDictionaryString localname, XmlDictionaryString ns)
#else
        internal bool IsStartElement(XmlDictionaryString localname, XmlDictionaryString ns)
#endif
        {
            if (dictionaryReader == null)
                return !isEndOfEmptyElement && reader.IsStartElement(localname.Value, ns.Value);
            else
                return !isEndOfEmptyElement && dictionaryReader.IsStartElement(localname, ns);
        }

        internal bool MoveToAttribute(string name)
        {
            return isEndOfEmptyElement ? false : reader.MoveToAttribute(name);
        }

        internal bool MoveToAttribute(string name, string ns)
        {
            return isEndOfEmptyElement ? false : reader.MoveToAttribute(name, ns);
        }

        internal void MoveToAttribute(int i)
        {
            if (isEndOfEmptyElement)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(i), SR.Format(SR.XmlElementAttributes)));
            reader.MoveToAttribute(i);
        }

        internal bool MoveToElement()
        {
            return isEndOfEmptyElement ? false : reader.MoveToElement();
        }

        internal bool MoveToFirstAttribute()
        {
            return isEndOfEmptyElement ? false : reader.MoveToFirstAttribute();
        }

        internal bool MoveToNextAttribute()
        {
            return isEndOfEmptyElement ? false : reader.MoveToNextAttribute();
        }

#if USE_REFEMIT        
        public XmlNodeType NodeType
#else
        internal XmlNodeType NodeType
#endif
        {
            get { return isEndOfEmptyElement ? XmlNodeType.EndElement : reader.NodeType; }
        }

        internal bool Read()
        {
            //reader.MoveToFirstAttribute();
            //if (NodeType == XmlNodeType.Attribute)
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
                return reader.Read();
            if (isEndOfEmptyElement)
            {
                isEndOfEmptyElement = false;
                return reader.Read();
            }
            isEndOfEmptyElement = true;
            return true;
        }

        internal XmlNodeType MoveToContent()
        {
            if (isEndOfEmptyElement)
                return XmlNodeType.EndElement;

            return reader.MoveToContent();
        }

        internal bool ReadAttributeValue()
        {
            return isEndOfEmptyElement ? false : reader.ReadAttributeValue();
        }

#if USE_REFEMIT        
        public void ReadEndElement()
#else
        internal void ReadEndElement()
#endif
        {
            if (isEndOfEmptyElement)
                Read();
            else
                reader.ReadEndElement();
        }

        private void ThrowConversionException(string value, string type)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(XmlObjectSerializer.TryAddLineInfo(this, SR.Format(SR.XmlInvalidConversion, value, type))));
        }

        private void ThrowNotAtElement()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.XmlStartElementExpected, "EndElement")));
        }

#if USE_REFEMIT
        public virtual char ReadElementContentAsChar()
#else
        internal virtual char ReadElementContentAsChar()
#endif
        {
            return ToChar(ReadElementContentAsInt());
        }

        private char ToChar(int value)
        {
            if (value < char.MinValue || value > char.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Char");
            }
            return (char)value;
        }

#if USE_REFEMIT
        public string ReadElementContentAsString()
#else
        internal string ReadElementContentAsString()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsString();
        }

        internal string ReadContentAsString()
        {
            return isEndOfEmptyElement ? String.Empty : reader.ReadContentAsString();
        }

#if USE_REFEMIT
        public bool ReadElementContentAsBoolean()
#else
        internal bool ReadElementContentAsBoolean()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsBoolean();
        }

        internal bool ReadContentAsBoolean()
        {
            if (isEndOfEmptyElement)
                ThrowConversionException(string.Empty, "Boolean");

            return reader.ReadContentAsBoolean();
        }

#if USE_REFEMIT
        public float ReadElementContentAsFloat()
#else
        internal float ReadElementContentAsFloat()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsFloat();
        }


#if USE_REFEMIT
        public double ReadElementContentAsDouble()
#else
        internal double ReadElementContentAsDouble()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsDouble();
        }


#if USE_REFEMIT
        public decimal ReadElementContentAsDecimal()
#else
        internal decimal ReadElementContentAsDecimal()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsDecimal();
        }


#if USE_REFEMIT
        public virtual byte[] ReadElementContentAsBase64()
#else
        internal virtual byte[] ReadElementContentAsBase64()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            if (dictionaryReader == null)
            {
                return ReadContentAsBase64(reader.ReadElementContentAsString());
            }
            else
            {
                return dictionaryReader.ReadElementContentAsBase64();
            }
        }

        internal byte[] ReadContentAsBase64(string str)
        {
            if (str == null)
                return null;
            str = str.Trim();
            if (str.Length == 0)
                return Array.Empty<byte>();

            try
            {
                return Convert.FromBase64String(str);
            }
            catch (ArgumentException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "byte[]", exception));
            }
            catch (FormatException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "byte[]", exception));
            }
        }

#if USE_REFEMIT
        public virtual DateTime ReadElementContentAsDateTime()
#else
        internal virtual DateTime ReadElementContentAsDateTime()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return XmlConvert.ToDateTime(reader.ReadElementContentAsString(), XmlDateTimeSerializationMode.RoundtripKind);
        }


#if USE_REFEMIT
        public int ReadElementContentAsInt()
#else
        internal int ReadElementContentAsInt()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsInt();
        }

        internal int ReadContentAsInt()
        {
            if (isEndOfEmptyElement)
                ThrowConversionException(string.Empty, "Int32");

            return reader.ReadContentAsInt();
        }

#if USE_REFEMIT
        public long ReadElementContentAsLong()
#else
        internal long ReadElementContentAsLong()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            return reader.ReadElementContentAsLong();
        }


#if USE_REFEMIT
        public short ReadElementContentAsShort()
#else
        internal short ReadElementContentAsShort()
#endif
        {
            return ToShort(ReadElementContentAsInt());
        }

        private short ToShort(int value)
        {
            if (value < short.MinValue || value > short.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Int16");
            }
            return (short)value;
        }

#if USE_REFEMIT
        public byte ReadElementContentAsUnsignedByte()
#else
        internal byte ReadElementContentAsUnsignedByte()
#endif
        {
            return ToByte(ReadElementContentAsInt());
        }

        private byte ToByte(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Byte");
            }
            return (byte)value;
        }

#if USE_REFEMIT
        [CLSCompliant(false)]
        public SByte ReadElementContentAsSignedByte()
#else
        internal SByte ReadElementContentAsSignedByte()
#endif
        {
            return ToSByte(ReadElementContentAsInt());
        }

        private SByte ToSByte(int value)
        {
            if (value < SByte.MinValue || value > SByte.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "SByte");
            }
            return (SByte)value;
        }

#if USE_REFEMIT
        [CLSCompliant(false)]
        public UInt32 ReadElementContentAsUnsignedInt()
#else
        internal UInt32 ReadElementContentAsUnsignedInt()
#endif
        {
            return ToUInt32(ReadElementContentAsLong());
        }

        private UInt32 ToUInt32(long value)
        {
            if (value < UInt32.MinValue || value > UInt32.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "UInt32");
            }
            return (UInt32)value;
        }

#if USE_REFEMIT
        [CLSCompliant(false)]
        public virtual UInt64 ReadElementContentAsUnsignedLong()
#else
        internal virtual UInt64 ReadElementContentAsUnsignedLong()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            string str = reader.ReadElementContentAsString();

            if (str == null || str.Length == 0)
                ThrowConversionException(string.Empty, "UInt64");

            return XmlConverter.ToUInt64(str);
        }


#if USE_REFEMIT
        [CLSCompliant(false)]
        public UInt16 ReadElementContentAsUnsignedShort()
#else
        internal UInt16 ReadElementContentAsUnsignedShort()
#endif
        {
            return ToUInt16(ReadElementContentAsInt());
        }


        private UInt16 ToUInt16(int value)
        {
            if (value < UInt16.MinValue || value > UInt16.MaxValue)
            {
                ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "UInt16");
            }
            return (UInt16)value;
        }

#if USE_REFEMIT
        public TimeSpan ReadElementContentAsTimeSpan()
#else
        internal TimeSpan ReadElementContentAsTimeSpan()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            string str = reader.ReadElementContentAsString();
            return XmlConverter.ToTimeSpan(str);
        }

#if USE_REFEMIT
        public Guid ReadElementContentAsGuid()
#else
        internal Guid ReadElementContentAsGuid()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            string str = reader.ReadElementContentAsString();
            try
            {
                return new Guid(str);
            }
            catch (ArgumentException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "Guid", exception));
            }
            catch (FormatException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "Guid", exception));
            }
            catch (OverflowException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "Guid", exception));
            }
        }


#if USE_REFEMIT
        public Uri ReadElementContentAsUri()
#else
        internal Uri ReadElementContentAsUri()
#endif
        {
            if (isEndOfEmptyElement)
                ThrowNotAtElement();

            string str = ReadElementContentAsString();
            try
            {
                return new Uri(str, UriKind.RelativeOrAbsolute);
            }
            catch (ArgumentException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "Uri", exception));
            }
            catch (FormatException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(str, "Uri", exception));
            }
        }


#if USE_REFEMIT
        public XmlQualifiedName ReadElementContentAsQName()
#else
        internal XmlQualifiedName ReadElementContentAsQName()
#endif
        {
            Read();
            XmlQualifiedName obj = ReadContentAsQName();
            ReadEndElement();
            return obj;
        }

        internal virtual XmlQualifiedName ReadContentAsQName()
        {
            return ParseQualifiedName(ReadContentAsString());
        }

        private XmlQualifiedName ParseQualifiedName(string str)
        {
            string name, ns, prefix;
            if (str == null || str.Length == 0)
                name = ns = String.Empty;
            else
                XmlObjectSerializerReadContext.ParseQualifiedName(str, this, out name, out ns, out prefix);
            return new XmlQualifiedName(name, ns);
        }

        private void CheckExpectedArrayLength(XmlObjectSerializerReadContext context, int arrayLength)
        {
            context.IncrementItemCount(arrayLength);
        }

        protected int GetArrayLengthQuota(XmlObjectSerializerReadContext context)
        {
            return Math.Min(context.RemainingItemCount, int.MaxValue);
        }

        private void CheckActualArrayLength(int expectedLength, int actualLength, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
        {
            if (expectedLength != actualLength)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayExceededSizeAttribute, expectedLength, itemName.Value, itemNamespace.Value)));
        }

#if USE_REFEMIT
        public bool TryReadBooleanArray(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadBooleanArray(XmlObjectSerializerReadContext context,
#endif
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out bool[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new bool[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = BooleanArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public virtual bool TryReadDateTimeArray(XmlObjectSerializerReadContext context,
#else
        internal virtual bool TryReadDateTimeArray(XmlObjectSerializerReadContext context,
#endif
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out DateTime[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new DateTime[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = DateTimeArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public bool TryReadDecimalArray(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadDecimalArray(XmlObjectSerializerReadContext context,
#endif
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out decimal[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new decimal[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = DecimalArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public bool TryReadInt32Array(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadInt32Array(XmlObjectSerializerReadContext context,
#endif
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out int[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new int[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = Int32ArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public bool TryReadInt64Array(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadInt64Array(XmlObjectSerializerReadContext context,
#endif
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out long[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new long[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = Int64ArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public bool TryReadSingleArray(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadSingleArray(XmlObjectSerializerReadContext context,
#endif
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out float[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new float[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = SingleArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

#if USE_REFEMIT
        public bool TryReadDoubleArray(XmlObjectSerializerReadContext context,
#else
        internal bool TryReadDoubleArray(XmlObjectSerializerReadContext context,
#endif   
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out double[] array)
        {
            if (dictionaryReader == null)
            {
                array = null;
                return false;
            }

            if (arrayLength != -1)
            {
                CheckExpectedArrayLength(context, arrayLength);
                array = new double[arrayLength];
                int read = 0, offset = 0;
                while ((read = dictionaryReader.ReadArray(itemName, itemNamespace, array, offset, arrayLength - offset)) > 0)
                {
                    offset += read;
                }
                CheckActualArrayLength(arrayLength, offset, itemName, itemNamespace);
            }
            else
            {
                array = DoubleArrayHelperWithDictionaryString.Instance.ReadArray(
                    dictionaryReader, itemName, itemNamespace, GetArrayLengthQuota(context));
                context.IncrementItemCount(array.Length);
            }
            return true;
        }

        internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return (reader is IXmlNamespaceResolver) ? ((IXmlNamespaceResolver)reader).GetNamespacesInScope(scope) : null;
        }

        // IXmlLineInfo members
        internal bool HasLineInfo()
        {
            IXmlLineInfo iXmlLineInfo = reader as IXmlLineInfo;
            return (iXmlLineInfo == null) ? false : iXmlLineInfo.HasLineInfo();
        }

        internal int LineNumber
        {
            get
            {
                IXmlLineInfo iXmlLineInfo = reader as IXmlLineInfo;
                return (iXmlLineInfo == null) ? 0 : iXmlLineInfo.LineNumber;
            }
        }

        internal int LinePosition
        {
            get
            {
                IXmlLineInfo iXmlLineInfo = reader as IXmlLineInfo;
                return (iXmlLineInfo == null) ? 0 : iXmlLineInfo.LinePosition;
            }
        }

        // IXmlTextParser members

        // delegating properties and methods
        internal string Name { get { return reader.Name; } }
        internal string LocalName { get { return reader.LocalName; } }
        internal string NamespaceURI { get { return reader.NamespaceURI; } }
        internal string Value { get { return reader.Value; } }
        internal Type ValueType { get { return reader.ValueType; } }
        internal int Depth { get { return reader.Depth; } }
        internal string LookupNamespace(string prefix) { return reader.LookupNamespace(prefix); }
        internal bool EOF { get { return reader.EOF; } }

        internal void Skip()
        {
            reader.Skip();
            isEndOfEmptyElement = false;
        }
    }
}

