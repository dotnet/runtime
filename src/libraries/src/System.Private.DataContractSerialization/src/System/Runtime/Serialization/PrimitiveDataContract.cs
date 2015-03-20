// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Security;


namespace System.Runtime.Serialization
{
    internal abstract class PrimitiveDataContract : DataContract
    {
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - holds instance of CriticalHelper which keeps state that is cached statically for serialization. 
        ///            Static fields are marked SecurityCritical or readonly to prevent
        ///            data from being modified or leaked to other components in appdomain.
        /// </SecurityNote>
        private PrimitiveDataContractCriticalHelper _helper;

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        protected PrimitiveDataContract(Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(new PrimitiveDataContractCriticalHelper(type, name, ns))
        {
            _helper = base.Helper as PrimitiveDataContractCriticalHelper;
        }

        static internal PrimitiveDataContract GetPrimitiveDataContract(Type type)
        {
            return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
        }

        static internal PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
        {
            return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
        }

        internal abstract string WriteMethodName { get; }
        internal abstract string ReadMethodName { get; }

        internal override XmlDictionaryString TopLevelElementNamespace
        {
            /// <SecurityNote>
            /// Critical - for consistency with base class
            /// Safe - for consistency with base class
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            { return DictionaryGlobals.SerializationNamespace; }
            /// <SecurityNote>
            /// Critical - for consistency with base class
            /// </SecurityNote>
            [SecurityCritical]
            set
            { }
        }

        internal override bool CanContainReferences
        {
            get { return false; }
        }

        internal override bool IsPrimitive
        {
            get { return true; }
        }

        internal override bool IsBuiltInDataContract
        {
            get
            {
                return true;
            }
        }

        internal MethodInfo XmlFormatWriterMethod
        {
            /// <SecurityNote>
            /// Critical - fetches the critical XmlFormatWriterMethod property
            /// Safe - XmlFormatWriterMethod only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
                if (_helper.XmlFormatWriterMethod == null)
                {
                    if (UnderlyingType.GetTypeInfo().IsValueType)
                        _helper.XmlFormatWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    else
                        _helper.XmlFormatWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                }
                return _helper.XmlFormatWriterMethod;
            }
        }

        internal MethodInfo XmlFormatContentWriterMethod
        {
            /// <SecurityNote>
            /// Critical - fetches the critical XmlFormatContentWriterMethod property
            /// Safe - XmlFormatContentWriterMethod only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
                if (_helper.XmlFormatContentWriterMethod == null)
                {
                    if (UnderlyingType.GetTypeInfo().IsValueType)
                        _helper.XmlFormatContentWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType });
                    else
                        _helper.XmlFormatContentWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType });
                }
                return _helper.XmlFormatContentWriterMethod;
            }
        }

        internal MethodInfo XmlFormatReaderMethod
        {
            /// <SecurityNote>
            /// Critical - fetches the critical XmlFormatReaderMethod property
            /// Safe - XmlFormatReaderMethod only needs to be protected for write; initialized in getter if null
            /// </SecurityNote>
            [SecuritySafeCritical]
            get
            {
                if (_helper.XmlFormatReaderMethod == null)
                {
                    _helper.XmlFormatReaderMethod = typeof(XmlReaderDelegator).GetMethod(ReadMethodName, Globals.ScanAllMembers);
                }
                return _helper.XmlFormatReaderMethod;
            }
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            xmlWriter.WriteAnyType(obj);
        }

        protected object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
        {
            context.AddNewObject(obj);
            return obj;
        }

        protected bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
        {
            Attributes attributes = new Attributes();
            attributes.Read(reader);
            if (attributes.Ref != Globals.NewObjectId)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotDeserializeRefAtTopLevel, attributes.Ref)));
            if (attributes.XsiNil)
            {
                reader.Skip();
                return true;
            }
            return false;
        }
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - holds all state used for for (de)serializing primitives.
        ///            since the data is cached statically, we lock down access to it.
        /// </SecurityNote>
        private class PrimitiveDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private MethodInfo _xmlFormatWriterMethod;
            private MethodInfo _xmlFormatContentWriterMethod;
            private MethodInfo _xmlFormatReaderMethod;

            internal PrimitiveDataContractCriticalHelper(Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(type)
            {
                SetDataContractName(name, ns);
            }

            internal MethodInfo XmlFormatWriterMethod
            {
                get { return _xmlFormatWriterMethod; }
                set { _xmlFormatWriterMethod = value; }
            }

            internal MethodInfo XmlFormatContentWriterMethod
            {
                get { return _xmlFormatContentWriterMethod; }
                set { _xmlFormatContentWriterMethod = value; }
            }

            internal MethodInfo XmlFormatReaderMethod
            {
                get { return _xmlFormatReaderMethod; }
                set { _xmlFormatReaderMethod = value; }
            }
        }
    }

    internal class CharDataContract : PrimitiveDataContract
    {
        internal CharDataContract() : this(DictionaryGlobals.CharLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal CharDataContract(XmlDictionaryString name, XmlDictionaryString ns) : base(typeof(char), name, ns)
        {
        }

        internal override string WriteMethodName { get { return "WriteChar"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsChar"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteChar((char)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsChar()
                : HandleReadValue(reader.ReadElementContentAsChar(), context);
        }
    }


    internal class BooleanDataContract : PrimitiveDataContract
    {
        internal BooleanDataContract() : base(typeof(bool), DictionaryGlobals.BooleanLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteBoolean"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsBoolean"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteBoolean((bool)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsBoolean()
                : HandleReadValue(reader.ReadElementContentAsBoolean(), context);
        }
    }

    internal class SignedByteDataContract : PrimitiveDataContract
    {
        internal SignedByteDataContract() : base(typeof(sbyte), DictionaryGlobals.SignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteSignedByte"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsSignedByte"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteSignedByte((sbyte)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsSignedByte()
                : HandleReadValue(reader.ReadElementContentAsSignedByte(), context);
        }
    }

    internal class UnsignedByteDataContract : PrimitiveDataContract
    {
        internal UnsignedByteDataContract() : base(typeof(byte), DictionaryGlobals.UnsignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedByte"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedByte"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteUnsignedByte((byte)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedByte()
                : HandleReadValue(reader.ReadElementContentAsUnsignedByte(), context);
        }
    }

    internal class ShortDataContract : PrimitiveDataContract
    {
        internal ShortDataContract() : base(typeof(short), DictionaryGlobals.ShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteShort"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsShort"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteShort((short)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsShort()
                : HandleReadValue(reader.ReadElementContentAsShort(), context);
        }
    }

    internal class UnsignedShortDataContract : PrimitiveDataContract
    {
        internal UnsignedShortDataContract() : base(typeof(ushort), DictionaryGlobals.UnsignedShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedShort"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedShort"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteUnsignedShort((ushort)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedShort()
                : HandleReadValue(reader.ReadElementContentAsUnsignedShort(), context);
        }
    }

    internal class IntDataContract : PrimitiveDataContract
    {
        internal IntDataContract() : base(typeof(int), DictionaryGlobals.IntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteInt"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsInt"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteInt((int)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsInt()
                : HandleReadValue(reader.ReadElementContentAsInt(), context);
        }
    }

    internal class UnsignedIntDataContract : PrimitiveDataContract
    {
        internal UnsignedIntDataContract() : base(typeof(uint), DictionaryGlobals.UnsignedIntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedInt"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedInt"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteUnsignedInt((uint)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedInt()
                : HandleReadValue(reader.ReadElementContentAsUnsignedInt(), context);
        }
    }

    internal class LongDataContract : PrimitiveDataContract
    {
        internal LongDataContract() : this(DictionaryGlobals.LongLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal LongDataContract(XmlDictionaryString name, XmlDictionaryString ns) : base(typeof(long), name, ns)
        {
        }

        internal override string WriteMethodName { get { return "WriteLong"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsLong"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteLong((long)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsLong()
                : HandleReadValue(reader.ReadElementContentAsLong(), context);
        }
    }
    internal class UnsignedLongDataContract : PrimitiveDataContract
    {
        internal UnsignedLongDataContract() : base(typeof(ulong), DictionaryGlobals.UnsignedLongLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedLong"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedLong"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteUnsignedLong((ulong)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedLong()
                : HandleReadValue(reader.ReadElementContentAsUnsignedLong(), context);
        }
    }

    internal class FloatDataContract : PrimitiveDataContract
    {
        internal FloatDataContract() : base(typeof(float), DictionaryGlobals.FloatLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteFloat"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsFloat"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteFloat((float)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsFloat()
                : HandleReadValue(reader.ReadElementContentAsFloat(), context);
        }
    }

    internal class DoubleDataContract : PrimitiveDataContract
    {
        internal DoubleDataContract() : base(typeof(double), DictionaryGlobals.DoubleLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDouble"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDouble"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteDouble((double)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsDouble()
                : HandleReadValue(reader.ReadElementContentAsDouble(), context);
        }
    }

    internal class DecimalDataContract : PrimitiveDataContract
    {
        internal DecimalDataContract() : base(typeof(decimal), DictionaryGlobals.DecimalLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDecimal"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDecimal"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteDecimal((decimal)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsDecimal()
                : HandleReadValue(reader.ReadElementContentAsDecimal(), context);
        }
    }

    internal class DateTimeDataContract : PrimitiveDataContract
    {
        internal DateTimeDataContract() : base(typeof(DateTime), DictionaryGlobals.DateTimeLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDateTime"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDateTime"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteDateTime((DateTime)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsDateTime()
                : HandleReadValue(reader.ReadElementContentAsDateTime(), context);
        }
    }

    internal class StringDataContract : PrimitiveDataContract
    {
        internal StringDataContract() : this(DictionaryGlobals.StringLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal StringDataContract(XmlDictionaryString name, XmlDictionaryString ns) : base(typeof(string), name, ns)
        {
        }

        internal override string WriteMethodName { get { return "WriteString"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsString"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteString((string)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(reader) ? null : reader.ReadElementContentAsString();
            }
            else
            {
                return HandleReadValue(reader.ReadElementContentAsString(), context);
            }
        }
    }
    internal class HexBinaryDataContract : StringDataContract
    {
        internal HexBinaryDataContract() : base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class ByteArrayDataContract : PrimitiveDataContract
    {
        internal ByteArrayDataContract() : base(typeof(byte[]), DictionaryGlobals.ByteArrayLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteBase64"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsBase64"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteBase64((byte[])obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(reader) ? null : reader.ReadElementContentAsBase64();
            }
            else
            {
                return HandleReadValue(reader.ReadElementContentAsBase64(), context);
            }
        }
    }

    internal class ObjectDataContract : PrimitiveDataContract
    {
        internal ObjectDataContract() : base(typeof(object), DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteAnyType"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsAnyType"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            // write nothing
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            object obj;
            if (reader.IsEmptyElement)
            {
                reader.Skip();
                obj = new object();
            }
            else
            {
                string localName = reader.LocalName;
                string ns = reader.NamespaceURI;
                reader.Read();
                try
                {
                    reader.ReadEndElement();
                    obj = new object();
                }
                catch (XmlException xes)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlForObjectCannotHaveContent, localName, ns), xes));
                }
            }
            return (context == null) ? obj : HandleReadValue(obj, context);
        }

        internal override bool CanContainReferences
        {
            get { return true; }
        }

        internal override bool IsPrimitive
        {
            get { return false; }
        }
    }

    internal class TimeSpanDataContract : PrimitiveDataContract
    {
        internal TimeSpanDataContract() : this(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal TimeSpanDataContract(XmlDictionaryString name, XmlDictionaryString ns) : base(typeof(TimeSpan), name, ns)
        {
        }

        internal override string WriteMethodName { get { return "WriteTimeSpan"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsTimeSpan"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteTimeSpan((TimeSpan)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsTimeSpan()
                : HandleReadValue(reader.ReadElementContentAsTimeSpan(), context);
        }
    }

    internal class GuidDataContract : PrimitiveDataContract
    {
        internal GuidDataContract() : this(DictionaryGlobals.GuidLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal GuidDataContract(XmlDictionaryString name, XmlDictionaryString ns) : base(typeof(Guid), name, ns)
        {
        }

        internal override string WriteMethodName { get { return "WriteGuid"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsGuid"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteGuid((Guid)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            return (context == null) ? reader.ReadElementContentAsGuid()
                : HandleReadValue(reader.ReadElementContentAsGuid(), context);
        }
    }


    internal class UriDataContract : PrimitiveDataContract
    {
        internal UriDataContract() : base(typeof(Uri), DictionaryGlobals.UriLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUri"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUri"; } }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteUri((Uri)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(reader) ? null : reader.ReadElementContentAsUri();
            }
            else
            {
                return HandleReadValue(reader.ReadElementContentAsUri(), context);
            }
        }
    }

    internal class QNameDataContract : PrimitiveDataContract
    {
        internal QNameDataContract() : base(typeof(XmlQualifiedName), DictionaryGlobals.QNameLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteQName"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsQName"; } }

        internal override bool IsPrimitive
        {
            get { return false; }
        }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            writer.WriteQName((XmlQualifiedName)obj);
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(reader) ? null : reader.ReadElementContentAsQName();
            }
            else
            {
                return HandleReadValue(reader.ReadElementContentAsQName(), context);
            }
        }

        internal override void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns)
        {
            if (object.ReferenceEquals(ns, DictionaryGlobals.SerializationNamespace))
                writer.WriteStartElement(Globals.SerPrefix, name, ns);
            else if (ns != null && ns.Value != null && ns.Value.Length > 0)
                writer.WriteStartElement(Globals.ElementPrefix, name, ns);
            else
                writer.WriteStartElement(name, ns);
        }
    }
}
