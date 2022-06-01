// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal abstract class PrimitiveDataContract : DataContract
    {
        internal static readonly PrimitiveDataContract NullContract = new NullPrimitiveDataContract();

        private readonly PrimitiveDataContractCriticalHelper _helper;

        protected PrimitiveDataContract(
            [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
            Type type, XmlDictionaryString name, XmlDictionaryString ns) : base(new PrimitiveDataContractCriticalHelper(type, name, ns))
        {
            _helper = (base.Helper as PrimitiveDataContractCriticalHelper)!;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static PrimitiveDataContract? GetPrimitiveDataContract(Type type)
        {
            return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static PrimitiveDataContract? GetPrimitiveDataContract(string name, string ns)
        {
            return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
        }

        internal abstract string WriteMethodName { get; }
        internal abstract string ReadMethodName { get; }

        public override XmlDictionaryString? TopLevelElementNamespace
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
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatWriterMethod == null)
                {
                    if (UnderlyingType.IsValueType)
                        _helper.XmlFormatWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) })!;
                    else
                        _helper.XmlFormatWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType, typeof(XmlDictionaryString), typeof(XmlDictionaryString) })!;
                }
                return _helper.XmlFormatWriterMethod;
            }
        }

        internal MethodInfo XmlFormatContentWriterMethod
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                if (_helper.XmlFormatContentWriterMethod == null)
                {
                    if (UnderlyingType.IsValueType)
                        _helper.XmlFormatContentWriterMethod = typeof(XmlWriterDelegator).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { UnderlyingType })!;
                    else
                        _helper.XmlFormatContentWriterMethod = typeof(XmlObjectSerializerWriteContext).GetMethod(WriteMethodName, Globals.ScanAllMembers, new Type[] { typeof(XmlWriterDelegator), UnderlyingType })!;
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
                    _helper.XmlFormatReaderMethod = typeof(XmlReaderDelegator).GetMethod(ReadMethodName, Globals.ScanAllMembers)!;
                }
                return _helper.XmlFormatReaderMethod;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext? context)
        {
            xmlWriter.WriteAnyType(obj);
        }

        protected static object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
        {
            context.AddNewObject(obj);
            return obj;
        }

        protected static bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
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

        private sealed class PrimitiveDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private MethodInfo? _xmlFormatWriterMethod;
            private MethodInfo? _xmlFormatContentWriterMethod;
            private MethodInfo? _xmlFormatReaderMethod;

            internal PrimitiveDataContractCriticalHelper(
                [DynamicallyAccessedMembers(ClassDataContract.DataContractPreserveMemberTypes)]
                Type type,
                XmlDictionaryString name, XmlDictionaryString ns) : base(type)
            {
                SetDataContractName(name, ns);
            }

            internal MethodInfo? XmlFormatWriterMethod
            {
                get { return _xmlFormatWriterMethod; }
                set { _xmlFormatWriterMethod = value; }
            }

            internal MethodInfo? XmlFormatContentWriterMethod
            {
                get { return _xmlFormatContentWriterMethod; }
                set { _xmlFormatContentWriterMethod = value; }
            }

            internal MethodInfo? XmlFormatReaderMethod
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteChar((char)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsChar()
                : HandleReadValue(reader.ReadElementContentAsChar(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteChar((char)obj!, name, ns);
        }
    }

    internal sealed class AsmxCharDataContract : CharDataContract
    {
        internal AsmxCharDataContract() : base(DictionaryGlobals.CharLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

    internal sealed class BooleanDataContract : PrimitiveDataContract
    {
        public BooleanDataContract() : base(typeof(bool), DictionaryGlobals.BooleanLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteBoolean"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsBoolean"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteBoolean((bool)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsBoolean()
                : HandleReadValue(reader.ReadElementContentAsBoolean(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteBoolean((bool)obj!, name, ns);
        }
    }

    internal sealed class SignedByteDataContract : PrimitiveDataContract
    {
        public SignedByteDataContract() : base(typeof(sbyte), DictionaryGlobals.SignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteSignedByte"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsSignedByte"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteSignedByte((sbyte)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsSignedByte()
                : HandleReadValue(reader.ReadElementContentAsSignedByte(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteSignedByte((sbyte)obj!, name, ns);
        }
    }

    internal sealed class UnsignedByteDataContract : PrimitiveDataContract
    {
        public UnsignedByteDataContract() : base(typeof(byte), DictionaryGlobals.UnsignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedByte"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedByte"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteUnsignedByte((byte)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedByte()
                : HandleReadValue(reader.ReadElementContentAsUnsignedByte(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteUnsignedByte((byte)obj!, name, ns);
        }
    }

    internal sealed class ShortDataContract : PrimitiveDataContract
    {
        public ShortDataContract() : base(typeof(short), DictionaryGlobals.ShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteShort"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsShort"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteShort((short)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsShort()
                : HandleReadValue(reader.ReadElementContentAsShort(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteShort((short)obj!, name, ns);
        }
    }

    internal sealed class UnsignedShortDataContract : PrimitiveDataContract
    {
        public UnsignedShortDataContract() : base(typeof(ushort), DictionaryGlobals.UnsignedShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedShort"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedShort"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteUnsignedShort((ushort)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedShort()
                : HandleReadValue(reader.ReadElementContentAsUnsignedShort(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteUnsignedShort((ushort)obj!, name, ns);
        }
    }

    internal sealed class NullPrimitiveDataContract : PrimitiveDataContract
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This warns because the call to Base has the type annotated with DynamicallyAccessedMembers so it warns" +
            "when looking into the methods of NullPrimitiveDataContract which are annotated with RequiresUnreferencedCodeAttribute. " +
            "Because this just represents null, we suppress.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers",
            Justification = "This warns because the call to Base has the type annotated with DynamicallyAccessedMembers so it warns" +
            "when looking into the methods of NullPrimitiveDataContract which are annotated with DynamicallyAccessedMembersAttribute. " +
            "Because this just represents null, we suppress.")]
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            throw new NotImplementedException();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            throw new NotImplementedException();
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class IntDataContract : PrimitiveDataContract
    {
        public IntDataContract() : base(typeof(int), DictionaryGlobals.IntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteInt"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsInt"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteInt((int)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsInt()
                : HandleReadValue(reader.ReadElementContentAsInt(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteInt((int)obj!, name, ns);
        }
    }

    internal sealed class UnsignedIntDataContract : PrimitiveDataContract
    {
        public UnsignedIntDataContract() : base(typeof(uint), DictionaryGlobals.UnsignedIntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedInt"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedInt"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteUnsignedInt((uint)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedInt()
                : HandleReadValue(reader.ReadElementContentAsUnsignedInt(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteUnsignedInt((uint)obj!, name, ns);
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteLong((long)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsLong()
                : HandleReadValue(reader.ReadElementContentAsLong(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteLong((long)obj!, name, ns);
        }
    }

    internal sealed class IntegerDataContract : LongDataContract
    {
        internal IntegerDataContract() : base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class PositiveIntegerDataContract : LongDataContract
    {
        internal PositiveIntegerDataContract() : base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NegativeIntegerDataContract : LongDataContract
    {
        internal NegativeIntegerDataContract() : base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NonPositiveIntegerDataContract : LongDataContract
    {
        internal NonPositiveIntegerDataContract() : base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NonNegativeIntegerDataContract : LongDataContract
    {
        internal NonNegativeIntegerDataContract() : base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class UnsignedLongDataContract : PrimitiveDataContract
    {
        public UnsignedLongDataContract() : base(typeof(ulong), DictionaryGlobals.UnsignedLongLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUnsignedLong"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUnsignedLong"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteUnsignedLong((ulong)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsUnsignedLong()
                : HandleReadValue(reader.ReadElementContentAsUnsignedLong(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteUnsignedLong((ulong)obj!, name, ns);
        }
    }

    internal sealed class FloatDataContract : PrimitiveDataContract
    {
        public FloatDataContract() : base(typeof(float), DictionaryGlobals.FloatLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteFloat"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsFloat"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteFloat((float)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsFloat()
                : HandleReadValue(reader.ReadElementContentAsFloat(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteFloat((float)obj!, name, ns);
        }
    }

    internal sealed class DoubleDataContract : PrimitiveDataContract
    {
        public DoubleDataContract() : base(typeof(double), DictionaryGlobals.DoubleLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDouble"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDouble"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteDouble((double)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsDouble()
                : HandleReadValue(reader.ReadElementContentAsDouble(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteDouble((double)obj!, name, ns);
        }
    }

    internal sealed class DecimalDataContract : PrimitiveDataContract
    {
        public DecimalDataContract() : base(typeof(decimal), DictionaryGlobals.DecimalLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDecimal"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDecimal"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteDecimal((decimal)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsDecimal()
                : HandleReadValue(reader.ReadElementContentAsDecimal(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteDecimal((decimal)obj!, name, ns);
        }
    }

    internal sealed class DateTimeDataContract : PrimitiveDataContract
    {
        public DateTimeDataContract() : base(typeof(DateTime), DictionaryGlobals.DateTimeLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteDateTime"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsDateTime"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteDateTime((DateTime)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsDateTime()
                : HandleReadValue(reader.ReadElementContentAsDateTime(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteDateTime((DateTime)obj!, name, ns);
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteString((string)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            context.WriteString(xmlWriter, (string?)obj, name, ns);
        }
    }

    internal sealed class TimeDataContract : StringDataContract
    {
        internal TimeDataContract() : base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class DateDataContract : StringDataContract
    {
        internal DateDataContract() : base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class HexBinaryDataContract : StringDataContract
    {
        internal HexBinaryDataContract() : base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class GYearMonthDataContract : StringDataContract
    {
        internal GYearMonthDataContract() : base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class GYearDataContract : StringDataContract
    {
        internal GYearDataContract() : base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class GMonthDayDataContract : StringDataContract
    {
        internal GMonthDayDataContract() : base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class GDayDataContract : StringDataContract
    {
        internal GDayDataContract() : base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class GMonthDataContract : StringDataContract
    {
        internal GMonthDataContract() : base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NormalizedStringDataContract : StringDataContract
    {
        internal NormalizedStringDataContract() : base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class TokenDataContract : StringDataContract
    {
        internal TokenDataContract() : base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class LanguageDataContract : StringDataContract
    {
        internal LanguageDataContract() : base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NameDataContract : StringDataContract
    {
        internal NameDataContract() : base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NCNameDataContract : StringDataContract
    {
        internal NCNameDataContract() : base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class IDDataContract : StringDataContract
    {
        internal IDDataContract() : base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class IDREFDataContract : StringDataContract
    {
        internal IDREFDataContract() : base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class IDREFSDataContract : StringDataContract
    {
        internal IDREFSDataContract() : base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class ENTITYDataContract : StringDataContract
    {
        internal ENTITYDataContract() : base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class ENTITIESDataContract : StringDataContract
    {
        internal ENTITIESDataContract() : base(DictionaryGlobals.ENTITIESLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NMTOKENDataContract : StringDataContract
    {
        internal NMTOKENDataContract() : base(DictionaryGlobals.NMTOKENLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class NMTOKENSDataContract : StringDataContract
    {
        internal NMTOKENSDataContract() : base(DictionaryGlobals.NMTOKENSLocalName, DictionaryGlobals.SchemaNamespace) { }
    }

    internal sealed class ByteArrayDataContract : PrimitiveDataContract
    {
        public ByteArrayDataContract() : base(typeof(byte[]), DictionaryGlobals.ByteArrayLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteBase64"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsBase64"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteBase64((byte[])obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteStartElement(name, ns);
            xmlWriter.WriteBase64((byte[]?)obj);
            xmlWriter.WriteEndElement();
        }
    }

    internal sealed class ObjectDataContract : PrimitiveDataContract
    {
        public ObjectDataContract() : base(typeof(object), DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteAnyType"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsAnyType"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            // write nothing
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            object obj;
            if (XmlReaderDelegator.IsEmptyElement)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteTimeSpan((TimeSpan)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsTimeSpan()
                : HandleReadValue(reader.ReadElementContentAsTimeSpan(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator writer, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            writer.WriteTimeSpan((TimeSpan)obj!, name, ns);
        }
    }

    internal sealed class XsDurationDataContract : TimeSpanDataContract
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteGuid((Guid)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
        {
            return (context == null) ? reader.ReadElementContentAsGuid()
                : HandleReadValue(reader.ReadElementContentAsGuid(), context);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator xmlWriter, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            xmlWriter.WriteGuid((Guid)obj!, name, ns);
        }
    }

    internal sealed class AsmxGuidDataContract : GuidDataContract
    {
        internal AsmxGuidDataContract() : base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace) { }
    }

    internal sealed class UriDataContract : PrimitiveDataContract
    {
        public UriDataContract() : base(typeof(Uri), DictionaryGlobals.UriLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override string WriteMethodName { get { return "WriteUri"; } }
        internal override string ReadMethodName { get { return "ReadElementContentAsUri"; } }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteUri((Uri)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator writer, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            writer.WriteUri((Uri?)obj, name, ns);
        }
    }

    internal sealed class QNameDataContract : PrimitiveDataContract
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext? context)
        {
            writer.WriteQName((XmlQualifiedName)obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext? context)
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteXmlElement(XmlWriterDelegator writer, object? obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString? ns)
        {
            context.WriteQName(writer, (XmlQualifiedName?)obj, name, ns);
        }

        internal override void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString? ns)
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
