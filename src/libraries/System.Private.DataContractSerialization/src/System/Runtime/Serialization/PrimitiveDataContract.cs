// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal abstract class PrimitiveDataContract : DataContract
    {
        internal static readonly PrimitiveDataContract NullContract = new NullPrimitiveDataContract();

        private PrimitiveDataContractCriticalHelper _helper;

        protected PrimitiveDataContract(Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(new PrimitiveDataContractCriticalHelper(type, name, ns))
        {
            _helper = base.Helper as PrimitiveDataContractCriticalHelper;
        }

        internal static PrimitiveDataContract GetPrimitiveDataContract(Type type)
        {
            return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
        }

        internal static PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
        {
            return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
        }

        internal abstract string WriteMethodName { get; }
        internal abstract string ReadMethodName { get; }

        public override XmlDictionaryString TopLevelElementNamespace
        {
            get
            { return DictionaryGlobals.SerializationNamespace; }

            set
            { }
        }

        internal override bool CanContainReferences => false;

        internal override bool IsPrimitive => true;

        public override bool IsBuiltInDataContract => true;

        internal MethodInfo XmlFormatWriterMethod
        {
            get
            {
                if (_helper.XmlFormatWriterMethod == null)
                {
                    if (UnderlyingType.IsValueType)
                        _helper.XmlFormatWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                    else
                        _helper.XmlFormatWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) });
                }
                return _helper.XmlFormatWriterMethod;
            }
        }

        internal MethodInfo XmlFormatContentWriterMethod
        {
            get
            {
                if (_helper.XmlFormatContentWriterMethod == null)
                {
                    if (UnderlyingType.IsValueType)
                        _helper.XmlFormatContentWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType });
                    else
                        _helper.XmlFormatContentWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType });
                }
                return _helper.XmlFormatContentWriterMethod;
            }
        }

        internal MethodInfo XmlFormatReaderMethod
        {
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
        public CharDataContract() : this(DictionaryGlobals.CharLocalName, DictionaryGlobals.SerializationNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteChar((char)obj, name, ns);
        }
    }

    internal class AsmxCharDataContract : CharDataContract
    {
        internal AsmxCharDataContract() : base(DictionaryGlobals.CharLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

    internal class BooleanDataContract : PrimitiveDataContract
    {
        public BooleanDataContract() : base(typeof(bool), DictionaryGlobals.BooleanLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteBoolean((bool)obj, name, ns);
        }
    }

    internal class SignedByteDataContract : PrimitiveDataContract
    {
        public SignedByteDataContract() : base(typeof(sbyte), DictionaryGlobals.SignedByteLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteSignedByte((sbyte)obj, name, ns);
        }
    }

    internal class UnsignedByteDataContract : PrimitiveDataContract
    {
        public UnsignedByteDataContract() : base(typeof(byte), DictionaryGlobals.UnsignedByteLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteUnsignedByte((byte)obj, name, ns);
        }
    }

    internal class ShortDataContract : PrimitiveDataContract
    {
        public ShortDataContract() : base(typeof(short), DictionaryGlobals.ShortLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteShort((short)obj, name, ns);
        }
    }

    internal class UnsignedShortDataContract : PrimitiveDataContract
    {
        public UnsignedShortDataContract() : base(typeof(ushort), DictionaryGlobals.UnsignedShortLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteUnsignedShort((ushort)obj, name, ns);
        }
    }

    internal class NullPrimitiveDataContract : PrimitiveDataContract
    {
        public NullPrimitiveDataContract() : base(typeof(NullPrimitiveDataContract), DictionaryGlobals.EmptyString, DictionaryGlobals.EmptyString)
        {

        }

        internal override string ReadMethodName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override string WriteMethodName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
        {
            throw new NotImplementedException();
        }

        public override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
        {
            throw new NotImplementedException();
        }

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            throw new NotImplementedException();
        }
    }

    internal class IntDataContract : PrimitiveDataContract
    {
        public IntDataContract() : base(typeof(int), DictionaryGlobals.IntLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteInt((int)obj, name, ns);
        }
    }

    internal class UnsignedIntDataContract : PrimitiveDataContract
    {
        public UnsignedIntDataContract() : base(typeof(uint), DictionaryGlobals.UnsignedIntLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteUnsignedInt((uint)obj, name, ns);
        }
    }

    internal class LongDataContract : PrimitiveDataContract
    {
        public LongDataContract() : this(DictionaryGlobals.LongLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteLong((long)obj, name, ns);
        }
    }

    internal class IntegerDataContract : LongDataContract
    {
        internal IntegerDataContract() : base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class PositiveIntegerDataContract : LongDataContract
    {
        internal PositiveIntegerDataContract() : base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NegativeIntegerDataContract : LongDataContract
    {
        internal NegativeIntegerDataContract() : base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NonPositiveIntegerDataContract : LongDataContract
    {
        internal NonPositiveIntegerDataContract() : base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NonNegativeIntegerDataContract : LongDataContract
    {
        internal NonNegativeIntegerDataContract() : base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class UnsignedLongDataContract : PrimitiveDataContract
    {
        public UnsignedLongDataContract() : base(typeof(ulong), DictionaryGlobals.UnsignedLongLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteUnsignedLong((ulong)obj, name, ns);
        }
    }

    internal class FloatDataContract : PrimitiveDataContract
    {
        public FloatDataContract() : base(typeof(float), DictionaryGlobals.FloatLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteFloat((float)obj, name, ns);
        }
    }

    internal class DoubleDataContract : PrimitiveDataContract
    {
        public DoubleDataContract() : base(typeof(double), DictionaryGlobals.DoubleLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteDouble((double)obj, name, ns);
        }
    }

    internal class DecimalDataContract : PrimitiveDataContract
    {
        public DecimalDataContract() : base(typeof(decimal), DictionaryGlobals.DecimalLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteDecimal((decimal)obj, name, ns);
        }
    }

    internal class DateTimeDataContract : PrimitiveDataContract
    {
        public DateTimeDataContract() : base(typeof(DateTime), DictionaryGlobals.DateTimeLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteDateTime((DateTime)obj, name, ns);
        }
    }

    internal class StringDataContract : PrimitiveDataContract
    {
        public StringDataContract() : this(DictionaryGlobals.StringLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            context.WriteString(xmlWriter, (string)obj, name, ns);
        }
    }

    internal class TimeDataContract : StringDataContract
    {
        internal TimeDataContract() : base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class DateDataContract : StringDataContract
    {
        internal DateDataContract() : base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class HexBinaryDataContract : StringDataContract
    {
        internal HexBinaryDataContract() : base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GYearMonthDataContract : StringDataContract
    {
        internal GYearMonthDataContract() : base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GYearDataContract : StringDataContract
    {
        internal GYearDataContract() : base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GMonthDayDataContract : StringDataContract
    {
        internal GMonthDayDataContract() : base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GDayDataContract : StringDataContract
    {
        internal GDayDataContract() : base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GMonthDataContract : StringDataContract
    {
        internal GMonthDataContract() : base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NormalizedStringDataContract : StringDataContract
    {
        internal NormalizedStringDataContract() : base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class TokenDataContract : StringDataContract
    {
        internal TokenDataContract() : base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class LanguageDataContract : StringDataContract
    {
        internal LanguageDataContract() : base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NameDataContract : StringDataContract
    {
        internal NameDataContract() : base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NCNameDataContract : StringDataContract
    {
        internal NCNameDataContract() : base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class IDDataContract : StringDataContract
    {
        internal IDDataContract() : base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class IDREFDataContract : StringDataContract
    {
        internal IDREFDataContract() : base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class IDREFSDataContract : StringDataContract
    {
        internal IDREFSDataContract() : base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class ENTITYDataContract : StringDataContract
    {
        internal ENTITYDataContract() : base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class ENTITIESDataContract : StringDataContract
    {
        internal ENTITIESDataContract() : base(DictionaryGlobals.ENTITIESLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NMTOKENDataContract : StringDataContract
    {
        internal NMTOKENDataContract() : base(DictionaryGlobals.NMTOKENLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class NMTOKENSDataContract : StringDataContract
    {
        internal NMTOKENSDataContract() : base(DictionaryGlobals.NMTOKENSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class ByteArrayDataContract : PrimitiveDataContract
    {
        public ByteArrayDataContract() : base(typeof(byte[]), DictionaryGlobals.ByteArrayLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteStartElement(name, ns);
            xmlWriter.WriteBase64((byte[])obj);
            xmlWriter.WriteEndElement();
        }
    }

    internal class ObjectDataContract : PrimitiveDataContract
    {
        public ObjectDataContract() : base(typeof(object), DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace)
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
        public TimeSpanDataContract() : this(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SerializationNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            writer.WriteTimeSpan((TimeSpan)obj, name, ns);
        }
    }

    internal class XsDurationDataContract : TimeSpanDataContract
    {
        public XsDurationDataContract() : base(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class GuidDataContract : PrimitiveDataContract
    {
        public GuidDataContract() : this(DictionaryGlobals.GuidLocalName, DictionaryGlobals.SerializationNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            xmlWriter.WriteGuid((Guid)obj, name, ns);
        }
    }

    internal class AsmxGuidDataContract : GuidDataContract
    {
        internal AsmxGuidDataContract() : base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

    internal class UriDataContract : PrimitiveDataContract
    {
        public UriDataContract() : base(typeof(Uri), DictionaryGlobals.UriLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            writer.WriteUri((Uri)obj, name, ns);
        }
    }

    internal class QNameDataContract : PrimitiveDataContract
    {
        public QNameDataContract() : base(typeof(XmlQualifiedName), DictionaryGlobals.QNameLocalName, DictionaryGlobals.SchemaNamespace)
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

        public override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
        {
            context.WriteQName(writer, (XmlQualifiedName)obj, name, ns);
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
