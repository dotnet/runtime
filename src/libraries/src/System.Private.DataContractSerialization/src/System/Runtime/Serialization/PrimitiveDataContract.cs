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
#if uapaot
    public abstract class PrimitiveDataContract : DataContract
#else
    internal abstract class PrimitiveDataContract : DataContract
#endif
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

#if uapaot
    public class CharDataContract : PrimitiveDataContract
#else
    internal class CharDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class AsmxCharDataContract : CharDataContract
#else
    internal class AsmxCharDataContract : CharDataContract
#endif
    {
        internal AsmxCharDataContract() : base(DictionaryGlobals.CharLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

#if uapaot
    public class BooleanDataContract : PrimitiveDataContract
#else
    internal class BooleanDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class SignedByteDataContract : PrimitiveDataContract
#else
    internal class SignedByteDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class UnsignedByteDataContract : PrimitiveDataContract
#else
    internal class UnsignedByteDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class ShortDataContract : PrimitiveDataContract
#else
    internal class ShortDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class UnsignedShortDataContract : PrimitiveDataContract
#else
    internal class UnsignedShortDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class IntDataContract : PrimitiveDataContract
#else
    internal class IntDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class UnsignedIntDataContract : PrimitiveDataContract
#else
    internal class UnsignedIntDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class LongDataContract : PrimitiveDataContract
#else
    internal class LongDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class IntegerDataContract : LongDataContract
#else
    internal class IntegerDataContract : LongDataContract
#endif
    {
        internal IntegerDataContract() : base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class PositiveIntegerDataContract : LongDataContract
#else
    internal class PositiveIntegerDataContract : LongDataContract
#endif
    {
        internal PositiveIntegerDataContract() : base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NegativeIntegerDataContract : LongDataContract
#else
    internal class NegativeIntegerDataContract : LongDataContract
#endif
    {
        internal NegativeIntegerDataContract() : base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NonPositiveIntegerDataContract : LongDataContract
#else
    internal class NonPositiveIntegerDataContract : LongDataContract
#endif
    {
        internal NonPositiveIntegerDataContract() : base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NonNegativeIntegerDataContract : LongDataContract
#else
    internal class NonNegativeIntegerDataContract : LongDataContract
#endif
    {
        internal NonNegativeIntegerDataContract() : base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class UnsignedLongDataContract : PrimitiveDataContract
#else
    internal class UnsignedLongDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class FloatDataContract : PrimitiveDataContract
#else
    internal class FloatDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class DoubleDataContract : PrimitiveDataContract
#else
    internal class DoubleDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class DecimalDataContract : PrimitiveDataContract
#else
    internal class DecimalDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class DateTimeDataContract : PrimitiveDataContract
#else
    internal class DateTimeDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class StringDataContract : PrimitiveDataContract
#else
    internal class StringDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class TimeDataContract : StringDataContract
#else
    internal class TimeDataContract : StringDataContract
#endif
    {
        internal TimeDataContract() : base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class DateDataContract : StringDataContract
#else
    internal class DateDataContract : StringDataContract
#endif
    {
        internal DateDataContract() : base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal class HexBinaryDataContract : StringDataContract
    {
        internal HexBinaryDataContract() : base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GYearMonthDataContract : StringDataContract
#else
    internal class GYearMonthDataContract : StringDataContract
#endif
    {
        internal GYearMonthDataContract() : base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GYearDataContract : StringDataContract
#else
    internal class GYearDataContract : StringDataContract
#endif
    {
        internal GYearDataContract() : base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GMonthDayDataContract : StringDataContract
#else
    internal class GMonthDayDataContract : StringDataContract
#endif
    {
        internal GMonthDayDataContract() : base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GDayDataContract : StringDataContract
#else
    internal class GDayDataContract : StringDataContract
#endif
    {
        internal GDayDataContract() : base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GMonthDataContract : StringDataContract
#else
    internal class GMonthDataContract : StringDataContract
#endif
    {
        internal GMonthDataContract() : base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NormalizedStringDataContract : StringDataContract
#else
    internal class NormalizedStringDataContract : StringDataContract
#endif
    {
        internal NormalizedStringDataContract() : base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class TokenDataContract : StringDataContract
#else
    internal class TokenDataContract : StringDataContract
#endif
    {
        internal TokenDataContract() : base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class LanguageDataContract : StringDataContract
#else
    internal class LanguageDataContract : StringDataContract
#endif
    {
        internal LanguageDataContract() : base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NameDataContract : StringDataContract
#else
    internal class NameDataContract : StringDataContract
#endif
    {
        internal NameDataContract() : base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NCNameDataContract : StringDataContract
#else
    internal class NCNameDataContract : StringDataContract
#endif
    {
        internal NCNameDataContract() : base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class IDDataContract : StringDataContract
#else
    internal class IDDataContract : StringDataContract
#endif
    {
        internal IDDataContract() : base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class IDREFDataContract : StringDataContract
#else
    internal class IDREFDataContract : StringDataContract
#endif
    {
        internal IDREFDataContract() : base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class IDREFSDataContract : StringDataContract
#else
    internal class IDREFSDataContract : StringDataContract
#endif
    {
        internal IDREFSDataContract() : base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class ENTITYDataContract : StringDataContract
#else
    internal class ENTITYDataContract : StringDataContract
#endif
    {
        internal ENTITYDataContract() : base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class ENTITIESDataContract : StringDataContract
#else
    internal class ENTITIESDataContract : StringDataContract
#endif
    {
        internal ENTITIESDataContract() : base(DictionaryGlobals.ENTITIESLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NMTOKENDataContract : StringDataContract
#else
    internal class NMTOKENDataContract : StringDataContract
#endif
    {
        internal NMTOKENDataContract() : base(DictionaryGlobals.NMTOKENLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class NMTOKENSDataContract : StringDataContract
#else
    internal class NMTOKENSDataContract : StringDataContract
#endif
    {
        internal NMTOKENSDataContract() : base(DictionaryGlobals.NMTOKENSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class ByteArrayDataContract : PrimitiveDataContract
#else
    internal class ByteArrayDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class ObjectDataContract : PrimitiveDataContract
#else
    internal class ObjectDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class TimeSpanDataContract : PrimitiveDataContract
#else
    internal class TimeSpanDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class XsDurationDataContract : TimeSpanDataContract
#else
    internal class XsDurationDataContract : TimeSpanDataContract
#endif
    {
        public XsDurationDataContract() : base(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

#if uapaot
    public class GuidDataContract : PrimitiveDataContract
#else
    internal class GuidDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class AsmxGuidDataContract : GuidDataContract
#else
    internal class AsmxGuidDataContract : GuidDataContract
#endif
    {
        internal AsmxGuidDataContract() : base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

#if uapaot
    public class UriDataContract : PrimitiveDataContract
#else
    internal class UriDataContract : PrimitiveDataContract
#endif
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

#if uapaot
    public class QNameDataContract : PrimitiveDataContract
#else
    internal class QNameDataContract : PrimitiveDataContract
#endif
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
