// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Globalization;


namespace System.Runtime.Serialization
{
#if USE_REFEMIT || uapaot
    public class XmlWriterDelegator
#else
    internal class XmlWriterDelegator
#endif
    {
        protected XmlWriter writer;
        protected XmlDictionaryWriter dictionaryWriter;
        internal int depth;
        private int _prefixes;

        public XmlWriterDelegator(XmlWriter writer)
        {
            XmlObjectSerializer.CheckNull(writer, nameof(writer));
            this.writer = writer;
            this.dictionaryWriter = writer as XmlDictionaryWriter;
        }

        internal XmlWriter Writer
        {
            get { return writer; }
        }

        internal void Flush()
        {
            writer.Flush();
        }

        internal string LookupPrefix(string ns)
        {
            return writer.LookupPrefix(ns);
        }

        private void WriteEndAttribute()
        {
            writer.WriteEndAttribute();
        }

#if USE_REFEMIT        
        public void WriteEndElement()
#else                
        internal void WriteEndElement()
#endif                
        {
            writer.WriteEndElement();
            depth--;
        }

        internal void WriteRaw(char[] buffer, int index, int count)
        {
            writer.WriteRaw(buffer, index, count);
        }

        internal void WriteRaw(string data)
        {
            writer.WriteRaw(data);
        }


        internal void WriteXmlnsAttribute(XmlDictionaryString ns)
        {
            if (dictionaryWriter != null)
            {
                if (ns != null)
                    dictionaryWriter.WriteXmlnsAttribute(null, ns);
            }
            else
                WriteXmlnsAttribute(ns.Value);
        }

        internal void WriteXmlnsAttribute(string ns)
        {
            if (ns != null)
            {
                if (ns.Length == 0)
                    writer.WriteAttributeString("xmlns", String.Empty, null, ns);
                else
                {
                    if (dictionaryWriter != null)
                        dictionaryWriter.WriteXmlnsAttribute(null, ns);
                    else
                    {
                        string prefix = writer.LookupPrefix(ns);
                        if (prefix == null)
                        {
                            prefix = String.Format(CultureInfo.InvariantCulture, "d{0}p{1}", depth, _prefixes);
                            _prefixes++;
                            writer.WriteAttributeString("xmlns", prefix, null, ns);
                        }
                    }
                }
            }
        }

        internal void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
        {
            if (dictionaryWriter != null)
            {
                dictionaryWriter.WriteXmlnsAttribute(prefix, ns);
            }
            else
            {
                writer.WriteAttributeString("xmlns", prefix, null, ns.Value);
            }
        }

        private void WriteStartAttribute(string prefix, string localName, string ns)
        {
            writer.WriteStartAttribute(prefix, localName, ns);
        }

        private void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (dictionaryWriter != null)
                dictionaryWriter.WriteStartAttribute(prefix, localName, namespaceUri);
            else
                writer.WriteStartAttribute(prefix,
                    (localName == null ? null : localName.Value),
                    (namespaceUri == null ? null : namespaceUri.Value));
        }

        internal void WriteAttributeString(string prefix, string localName, string ns, string value)
        {
            WriteStartAttribute(prefix, localName, ns);
            WriteAttributeStringValue(value);
            WriteEndAttribute();
        }

        internal void WriteAttributeString(string prefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, string value)
        {
            WriteStartAttribute(prefix, attrName, attrNs);
            WriteAttributeStringValue(value);
            WriteEndAttribute();
        }

        private void WriteAttributeStringValue(string value)
        {
            writer.WriteValue(value);
        }

        internal void WriteAttributeString(string prefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, XmlDictionaryString value)
        {
            WriteStartAttribute(prefix, attrName, attrNs);
            WriteAttributeStringValue(value);
            WriteEndAttribute();
        }

        private void WriteAttributeStringValue(XmlDictionaryString value)
        {
            if (dictionaryWriter == null)
                writer.WriteString(value.Value);
            else
                dictionaryWriter.WriteString(value);
        }

        internal void WriteAttributeInt(string prefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, int value)
        {
            WriteStartAttribute(prefix, attrName, attrNs);
            WriteAttributeIntValue(value);
            WriteEndAttribute();
        }

        private void WriteAttributeIntValue(int value)
        {
            writer.WriteValue(value);
        }

        internal void WriteAttributeBool(string prefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, bool value)
        {
            WriteStartAttribute(prefix, attrName, attrNs);
            WriteAttributeBoolValue(value);
            WriteEndAttribute();
        }

        private void WriteAttributeBoolValue(bool value)
        {
            writer.WriteValue(value);
        }

        internal void WriteAttributeQualifiedName(string attrPrefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, string name, string ns)
        {
            WriteXmlnsAttribute(ns);
            WriteStartAttribute(attrPrefix, attrName, attrNs);
            WriteAttributeQualifiedNameValue(name, ns);
            WriteEndAttribute();
        }

        private void WriteAttributeQualifiedNameValue(string name, string ns)
        {
            writer.WriteQualifiedName(name, ns);
        }

        internal void WriteAttributeQualifiedName(string attrPrefix, XmlDictionaryString attrName, XmlDictionaryString attrNs, XmlDictionaryString name, XmlDictionaryString ns)
        {
            WriteXmlnsAttribute(ns);
            WriteStartAttribute(attrPrefix, attrName, attrNs);
            WriteAttributeQualifiedNameValue(name, ns);
            WriteEndAttribute();
        }

        private void WriteAttributeQualifiedNameValue(XmlDictionaryString name, XmlDictionaryString ns)
        {
            if (dictionaryWriter == null)
                writer.WriteQualifiedName(name.Value, ns.Value);
            else
                dictionaryWriter.WriteQualifiedName(name, ns);
        }

        internal void WriteStartElement(string localName, string ns)
        {
            WriteStartElement(null, localName, ns);
        }

        internal virtual void WriteStartElement(string prefix, string localName, string ns)
        {
            writer.WriteStartElement(prefix, localName, ns);
            depth++;
            _prefixes = 1;
        }

#if USE_REFEMIT
        public void WriteStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
#else
        internal void WriteStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
#endif
        {
            WriteStartElement(null, localName, namespaceUri);
        }

        internal void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (dictionaryWriter != null)
                dictionaryWriter.WriteStartElement(prefix, localName, namespaceUri);
            else
                writer.WriteStartElement(prefix, (localName == null ? null : localName.Value), (namespaceUri == null ? null : namespaceUri.Value));
            depth++;
            _prefixes = 1;
        }

        internal void WriteStartElementPrimitive(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
        {
            if (dictionaryWriter != null)
                dictionaryWriter.WriteStartElement(null, localName, namespaceUri);
            else
                writer.WriteStartElement(null, (localName == null ? null : localName.Value), (namespaceUri == null ? null : namespaceUri.Value));
        }

        internal void WriteEndElementPrimitive()
        {
            writer.WriteEndElement();
        }

        internal WriteState WriteState
        {
            get { return writer.WriteState; }
        }

        internal string XmlLang
        {
            get { return writer.XmlLang; }
        }

        internal XmlSpace XmlSpace
        {
            get { return writer.XmlSpace; }
        }

#if USE_REFEMIT
        public void WriteNamespaceDecl(XmlDictionaryString ns)
#else
        internal void WriteNamespaceDecl(XmlDictionaryString ns)
#endif
        {
            WriteXmlnsAttribute(ns);
        }

        private Exception CreateInvalidPrimitiveTypeException(Type type)
        {
            return new InvalidDataContractException(SR.Format(SR.InvalidPrimitiveType, DataContract.GetClrTypeFullName(type)));
        }

        internal void WriteAnyType(object value)
        {
            WriteAnyType(value, value.GetType());
        }

        internal void WriteAnyType(object value, Type valueType)
        {
            bool handled = true;
            switch (valueType.GetTypeCode())
            {
                case TypeCode.Boolean:
                    WriteBoolean((bool)value);
                    break;
                case TypeCode.Char:
                    WriteChar((char)value);
                    break;
                case TypeCode.Byte:
                    WriteUnsignedByte((byte)value);
                    break;
                case TypeCode.Int16:
                    WriteShort((short)value);
                    break;
                case TypeCode.Int32:
                    WriteInt((int)value);
                    break;
                case TypeCode.Int64:
                    WriteLong((long)value);
                    break;
                case TypeCode.Single:
                    WriteFloat((float)value);
                    break;
                case TypeCode.Double:
                    WriteDouble((double)value);
                    break;
                case TypeCode.Decimal:
                    WriteDecimal((decimal)value);
                    break;
                case TypeCode.DateTime:
                    WriteDateTime((DateTime)value);
                    break;
                case TypeCode.String:
                    WriteString((string)value);
                    break;
                case TypeCode.SByte:
                    WriteSignedByte((sbyte)value);
                    break;
                case TypeCode.UInt16:
                    WriteUnsignedShort((ushort)value);
                    break;
                case TypeCode.UInt32:
                    WriteUnsignedInt((uint)value);
                    break;
                case TypeCode.UInt64:
                    WriteUnsignedLong((ulong)value);
                    break;
                case TypeCode.Empty:
                case TypeCode.Object:
                default:
                    if (valueType == Globals.TypeOfByteArray)
                        WriteBase64((byte[])value);
                    else if (valueType == Globals.TypeOfObject)
                    {
                        //Write Nothing
                    }
                    else if (valueType == Globals.TypeOfTimeSpan)
                        WriteTimeSpan((TimeSpan)value);
                    else if (valueType == Globals.TypeOfGuid)
                        WriteGuid((Guid)value);
                    else if (valueType == Globals.TypeOfUri)
                        WriteUri((Uri)value);
                    else if (valueType == Globals.TypeOfXmlQualifiedName)
                        WriteQName((XmlQualifiedName)value);
                    else
                        handled = false;
                    break;
            }
            if (!handled)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidPrimitiveTypeException(valueType));
        }

        internal void WriteExtensionData(IDataNode dataNode)
        {
            bool handled = true;
            Type valueType = dataNode.DataType;
            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.Boolean:
                    WriteBoolean(((DataNode<bool>)dataNode).GetValue());
                    break;
                case TypeCode.Char:
                    WriteChar(((DataNode<char>)dataNode).GetValue());
                    break;
                case TypeCode.Byte:
                    WriteUnsignedByte(((DataNode<byte>)dataNode).GetValue());
                    break;
                case TypeCode.Int16:
                    WriteShort(((DataNode<short>)dataNode).GetValue());
                    break;
                case TypeCode.Int32:
                    WriteInt(((DataNode<int>)dataNode).GetValue());
                    break;
                case TypeCode.Int64:
                    WriteLong(((DataNode<long>)dataNode).GetValue());
                    break;
                case TypeCode.Single:
                    WriteFloat(((DataNode<float>)dataNode).GetValue());
                    break;
                case TypeCode.Double:
                    WriteDouble(((DataNode<double>)dataNode).GetValue());
                    break;
                case TypeCode.Decimal:
                    WriteDecimal(((DataNode<decimal>)dataNode).GetValue());
                    break;
                case TypeCode.DateTime:
                    WriteDateTime(((DataNode<DateTime>)dataNode).GetValue());
                    break;
                case TypeCode.String:
                    WriteString(((DataNode<string>)dataNode).GetValue());
                    break;
                case TypeCode.SByte:
                    WriteSignedByte(((DataNode<sbyte>)dataNode).GetValue());
                    break;
                case TypeCode.UInt16:
                    WriteUnsignedShort(((DataNode<ushort>)dataNode).GetValue());
                    break;
                case TypeCode.UInt32:
                    WriteUnsignedInt(((DataNode<uint>)dataNode).GetValue());
                    break;
                case TypeCode.UInt64:
                    WriteUnsignedLong(((DataNode<ulong>)dataNode).GetValue());
                    break;
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Object:
                default:
                    if (valueType == Globals.TypeOfByteArray)
                        WriteBase64(((DataNode<byte[]>)dataNode).GetValue());
                    else if (valueType == Globals.TypeOfObject)
                    {
                        object obj = dataNode.Value;
                        if (obj != null)
                            WriteAnyType(obj);
                    }
                    else if (valueType == Globals.TypeOfTimeSpan)
                        WriteTimeSpan(((DataNode<TimeSpan>)dataNode).GetValue());
                    else if (valueType == Globals.TypeOfGuid)
                        WriteGuid(((DataNode<Guid>)dataNode).GetValue());
                    else if (valueType == Globals.TypeOfUri)
                        WriteUri(((DataNode<Uri>)dataNode).GetValue());
                    else if (valueType == Globals.TypeOfXmlQualifiedName)
                        WriteQName(((DataNode<XmlQualifiedName>)dataNode).GetValue());
                    else
                        handled = false;
                    break;
            }

            if (!handled)
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateInvalidPrimitiveTypeException(valueType));
            }
        }

        internal void WriteString(string value)
        {
            writer.WriteValue(value);
        }

        internal virtual void WriteBoolean(bool value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteBoolean(bool value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteBoolean(bool value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteBoolean(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteDateTime(DateTime value)
        {
            WriteString(XmlConvert.ToString(value, XmlDateTimeSerializationMode.RoundtripKind));
        }

#if USE_REFEMIT
        public void WriteDateTime(DateTime value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteDateTime(DateTime value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteDateTime(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteDecimal(decimal value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteDecimal(decimal value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteDecimal(decimal value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteDecimal(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteDouble(double value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteDouble(double value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteDouble(double value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteDouble(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteInt(int value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteInt(int value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteInt(int value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteInt(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteLong(long value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteLong(long value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteLong(long value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteLong(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteFloat(float value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteFloat(float value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteFloat(float value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteFloat(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteBase64(byte[] bytes)
        {
            if (bytes == null)
                return;

            writer.WriteBase64(bytes, 0, bytes.Length);
        }

        internal virtual void WriteShort(short value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteShort(short value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteShort(short value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteShort(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteUnsignedByte(byte value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        public void WriteUnsignedByte(byte value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteUnsignedByte(byte value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteUnsignedByte(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteSignedByte(sbyte value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        [CLSCompliant(false)]
        public void WriteSignedByte(sbyte value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteSignedByte(sbyte value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteSignedByte(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteUnsignedInt(uint value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        [CLSCompliant(false)]
        public void WriteUnsignedInt(uint value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteUnsignedInt(uint value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteUnsignedInt(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteUnsignedLong(ulong value)
        {
            writer.WriteRaw(XmlConvert.ToString(value));
        }
#if USE_REFEMIT
        [CLSCompliant(false)]
        public void WriteUnsignedLong(ulong value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteUnsignedLong(ulong value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteUnsignedLong(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteUnsignedShort(ushort value)
        {
            writer.WriteValue(value);
        }
#if USE_REFEMIT
        [CLSCompliant(false)]
        public void WriteUnsignedShort(ushort value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteUnsignedShort(ushort value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteUnsignedShort(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteChar(char value)
        {
            writer.WriteValue((int)value);
        }
#if USE_REFEMIT
        public void WriteChar(char value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteChar(char value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteChar(value);
            WriteEndElementPrimitive();
        }

        internal void WriteTimeSpan(TimeSpan value)
        {
            writer.WriteRaw(XmlConvert.ToString(value));
        }

        internal void WriteTimeSpan(char value, XmlDictionaryString name, XmlDictionaryString ns)
        {
            WriteStartElementPrimitive(name, ns);
            writer.WriteRaw(XmlConvert.ToString(value));
            WriteEndElementPrimitive();
        }

#if USE_REFEMIT
        public void WriteTimeSpan(TimeSpan value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteTimeSpan(TimeSpan value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteTimeSpan(value);
            WriteEndElementPrimitive();
        }

        internal void WriteGuid(Guid value)
        {
            writer.WriteRaw(value.ToString());
        }
#if USE_REFEMIT
        public void WriteGuid(Guid value, XmlDictionaryString name, XmlDictionaryString ns)
#else
        internal void WriteGuid(Guid value, XmlDictionaryString name, XmlDictionaryString ns)
#endif
        {
            WriteStartElementPrimitive(name, ns);
            WriteGuid(value);
            WriteEndElementPrimitive();
        }

        internal void WriteUri(Uri value)
        {
            writer.WriteString(value.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped));
        }

        internal void WriteUri(Uri value, XmlDictionaryString name, XmlDictionaryString ns)
        {
            WriteStartElementPrimitive(name, ns);
            WriteUri(value);
            WriteEndElementPrimitive();
        }

        internal virtual void WriteQName(XmlQualifiedName value)
        {
            if (value != XmlQualifiedName.Empty)
            {
                WriteXmlnsAttribute(value.Namespace);
                WriteQualifiedName(value.Name, value.Namespace);
            }
        }

        internal void WriteQualifiedName(string localName, string ns)
        {
            writer.WriteQualifiedName(localName, ns);
        }

        internal void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString ns)
        {
            if (dictionaryWriter == null)
                writer.WriteQualifiedName(localName.Value, ns.Value);
            else
                dictionaryWriter.WriteQualifiedName(localName, ns);
        }

#if USE_REFEMIT
        public void WriteBooleanArray(bool[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteBooleanArray(bool[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteBoolean(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteDateTimeArray(DateTime[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteDateTimeArray(DateTime[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteDateTime(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteDecimalArray(decimal[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteDecimalArray(decimal[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteDecimal(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteInt32Array(int[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteInt32Array(int[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteInt(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteInt64Array(long[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteInt64Array(long[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteLong(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteSingleArray(float[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteSingleArray(float[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteFloat(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }

#if USE_REFEMIT
        public void WriteDoubleArray(double[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#else
        internal void WriteDoubleArray(double[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
#endif
        {
            if (dictionaryWriter == null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    WriteDouble(value[i], itemName, itemNamespace);
                }
            }
            else
            {
                dictionaryWriter.WriteArray(null, itemName, itemNamespace, value, 0, value.Length);
            }
        }
    }
}

