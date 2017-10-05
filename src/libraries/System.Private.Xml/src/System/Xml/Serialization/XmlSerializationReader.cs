// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System.IO;
    using System;
    using System.Security;
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.ComponentModel;
    using System.Globalization;
    using System.Diagnostics;
    using System.Threading;
    using System.Configuration;
    using System.Xml.Serialization.Configuration;

    ///<internalonly/>
#if XMLSERIALIZERGENERATOR
    internal abstract class XmlSerializationReader : XmlSerializationGeneratedCode
#else
    public abstract class XmlSerializationReader : XmlSerializationGeneratedCode
#endif
    {
        private XmlReader _r;
        private XmlCountingReader _countingReader;
        private XmlDocument _d;
        private Hashtable _callbacks;
        private Hashtable _types;
        private Hashtable _typesReverse;
        private XmlDeserializationEvents _events;
        private Hashtable _targets;
        private Hashtable _referencedTargets;
        private ArrayList _targetsWithoutIds;
        private ArrayList _fixups;
        private ArrayList _collectionFixups;
        private bool _soap12;
        private bool _isReturnValue;
        private bool _decodeName = true;

        private string _schemaNsID;
        private string _schemaNs1999ID;
        private string _schemaNs2000ID;
        private string _schemaNonXsdTypesNsID;
        private string _instanceNsID;
        private string _instanceNs2000ID;
        private string _instanceNs1999ID;
        private string _soapNsID;
        private string _soap12NsID;
        private string _schemaID;
        private string _wsdlNsID;
        private string _wsdlArrayTypeID;
        private string _nullID;
        private string _nilID;
        private string _typeID;
        private string _arrayTypeID;
        private string _itemTypeID;
        private string _arraySizeID;
        private string _arrayID;
        private string _urTypeID;
        private string _stringID;
        private string _intID;
        private string _booleanID;
        private string _shortID;
        private string _longID;
        private string _floatID;
        private string _doubleID;
        private string _decimalID;
        private string _dateTimeID;
        private string _qnameID;
        private string _dateID;
        private string _timeID;
        private string _hexBinaryID;
        private string _base64BinaryID;
        private string _base64ID;
        private string _unsignedByteID;
        private string _byteID;
        private string _unsignedShortID;
        private string _unsignedIntID;
        private string _unsignedLongID;
        private string _oldDecimalID;
        private string _oldTimeInstantID;

        private string _anyURIID;
        private string _durationID;
        private string _ENTITYID;
        private string _ENTITIESID;
        private string _gDayID;
        private string _gMonthID;
        private string _gMonthDayID;
        private string _gYearID;
        private string _gYearMonthID;
        private string _IDID;
        private string _IDREFID;
        private string _IDREFSID;
        private string _integerID;
        private string _languageID;
        private string _nameID;
        private string _NCNameID;
        private string _NMTOKENID;
        private string _NMTOKENSID;
        private string _negativeIntegerID;
        private string _nonPositiveIntegerID;
        private string _nonNegativeIntegerID;
        private string _normalizedStringID;
        private string _NOTATIONID;
        private string _positiveIntegerID;
        private string _tokenID;

        private string _charID;
        private string _guidID;
        private string _timeSpanID;

        private static bool s_checkDeserializeAdvances;

        protected abstract void InitIDs();

#if FEATURE_SERIALIZATION_UAPAOT
        // this method must be called before any generated deserialization methods are called
        internal void Init(XmlReader r, XmlDeserializationEvents events, string encodingStyle)
        {
            _events = events;
            _r = r;
            _soap12 = (encodingStyle == Soap12.Encoding);

            _schemaNsID = r.NameTable.Add(XmlSchema.Namespace);
            _schemaNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema");
            _schemaNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema");
            _schemaNonXsdTypesNsID = r.NameTable.Add(UrtTypes.Namespace);
            _instanceNsID = r.NameTable.Add(XmlSchema.InstanceNamespace);
            _instanceNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema-instance");
            _instanceNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema-instance");
            _soapNsID = r.NameTable.Add(Soap.Encoding);
            _soap12NsID = r.NameTable.Add(Soap12.Encoding);
            _schemaID = r.NameTable.Add("schema");
            _wsdlNsID = r.NameTable.Add(Wsdl.Namespace);
            _wsdlArrayTypeID = r.NameTable.Add(Wsdl.ArrayType);
            _nullID = r.NameTable.Add("null");
            _nilID = r.NameTable.Add("nil");
            _typeID = r.NameTable.Add("type");
            _arrayTypeID = r.NameTable.Add("arrayType");
            _itemTypeID = r.NameTable.Add("itemType");
            _arraySizeID = r.NameTable.Add("arraySize");
            _arrayID = r.NameTable.Add("Array");
            _urTypeID = r.NameTable.Add(Soap.UrType);
            InitIDs();
        }
#endif

        // this method must be called before any generated deserialization methods are called
        internal void Init(XmlReader r, XmlDeserializationEvents events, string encodingStyle, TempAssembly tempAssembly)
        {
            _events = events;
            if (s_checkDeserializeAdvances)
            {
                _countingReader = new XmlCountingReader(r);
                _r = _countingReader;
            }
            else
                _r = r;
            _d = null;
            _soap12 = (encodingStyle == Soap12.Encoding);
            Init(tempAssembly);

            _schemaNsID = r.NameTable.Add(XmlSchema.Namespace);
            _schemaNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema");
            _schemaNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema");
            _schemaNonXsdTypesNsID = r.NameTable.Add(UrtTypes.Namespace);
            _instanceNsID = r.NameTable.Add(XmlSchema.InstanceNamespace);
            _instanceNs2000ID = r.NameTable.Add("http://www.w3.org/2000/10/XMLSchema-instance");
            _instanceNs1999ID = r.NameTable.Add("http://www.w3.org/1999/XMLSchema-instance");
            _soapNsID = r.NameTable.Add(Soap.Encoding);
            _soap12NsID = r.NameTable.Add(Soap12.Encoding);
            _schemaID = r.NameTable.Add("schema");
            _wsdlNsID = r.NameTable.Add(Wsdl.Namespace);
            _wsdlArrayTypeID = r.NameTable.Add(Wsdl.ArrayType);
            _nullID = r.NameTable.Add("null");
            _nilID = r.NameTable.Add("nil");
            _typeID = r.NameTable.Add("type");
            _arrayTypeID = r.NameTable.Add("arrayType");
            _itemTypeID = r.NameTable.Add("itemType");
            _arraySizeID = r.NameTable.Add("arraySize");
            _arrayID = r.NameTable.Add("Array");
            _urTypeID = r.NameTable.Add(Soap.UrType);
            InitIDs();
        }

#if !XMLSERIALIZERGENERATOR
        protected bool DecodeName
        {
            get
            {
                return _decodeName;
            }
            set
            {
                _decodeName = value;
            }
        }

        protected XmlReader Reader
        {
            get
            {
                return _r;
            }
        }

        protected int ReaderCount
        {
            get
            {
                return s_checkDeserializeAdvances ? _countingReader.AdvanceCount : 0;
            }
        }

        protected XmlDocument Document
        {
            get
            {
                if (_d == null)
                {
                    _d = new XmlDocument(_r.NameTable);
                    _d.SetBaseURI(_r.BaseURI);
                }
                return _d;
            }
        }

        ///<internalonly/>
        protected static Assembly ResolveDynamicAssembly(string assemblyFullName)
        {
            return DynamicAssemblies.Get(assemblyFullName);
        }

        private void InitPrimitiveIDs()
        {
            if (_tokenID != null) return;
            object ns = _r.NameTable.Add(XmlSchema.Namespace);
            object ns2 = _r.NameTable.Add(UrtTypes.Namespace);

            _stringID = _r.NameTable.Add("string");
            _intID = _r.NameTable.Add("int");
            _booleanID = _r.NameTable.Add("boolean");
            _shortID = _r.NameTable.Add("short");
            _longID = _r.NameTable.Add("long");
            _floatID = _r.NameTable.Add("float");
            _doubleID = _r.NameTable.Add("double");
            _decimalID = _r.NameTable.Add("decimal");
            _dateTimeID = _r.NameTable.Add("dateTime");
            _qnameID = _r.NameTable.Add("QName");
            _dateID = _r.NameTable.Add("date");
            _timeID = _r.NameTable.Add("time");
            _hexBinaryID = _r.NameTable.Add("hexBinary");
            _base64BinaryID = _r.NameTable.Add("base64Binary");
            _unsignedByteID = _r.NameTable.Add("unsignedByte");
            _byteID = _r.NameTable.Add("byte");
            _unsignedShortID = _r.NameTable.Add("unsignedShort");
            _unsignedIntID = _r.NameTable.Add("unsignedInt");
            _unsignedLongID = _r.NameTable.Add("unsignedLong");
            _oldDecimalID = _r.NameTable.Add("decimal");
            _oldTimeInstantID = _r.NameTable.Add("timeInstant");
            _charID = _r.NameTable.Add("char");
            _guidID = _r.NameTable.Add("guid");
            _timeSpanID = _r.NameTable.Add("TimeSpan");
            _base64ID = _r.NameTable.Add("base64");

            _anyURIID = _r.NameTable.Add("anyURI");
            _durationID = _r.NameTable.Add("duration");
            _ENTITYID = _r.NameTable.Add("ENTITY");
            _ENTITIESID = _r.NameTable.Add("ENTITIES");
            _gDayID = _r.NameTable.Add("gDay");
            _gMonthID = _r.NameTable.Add("gMonth");
            _gMonthDayID = _r.NameTable.Add("gMonthDay");
            _gYearID = _r.NameTable.Add("gYear");
            _gYearMonthID = _r.NameTable.Add("gYearMonth");
            _IDID = _r.NameTable.Add("ID");
            _IDREFID = _r.NameTable.Add("IDREF");
            _IDREFSID = _r.NameTable.Add("IDREFS");
            _integerID = _r.NameTable.Add("integer");
            _languageID = _r.NameTable.Add("language");
            _nameID = _r.NameTable.Add("Name");
            _NCNameID = _r.NameTable.Add("NCName");
            _NMTOKENID = _r.NameTable.Add("NMTOKEN");
            _NMTOKENSID = _r.NameTable.Add("NMTOKENS");
            _negativeIntegerID = _r.NameTable.Add("negativeInteger");
            _nonNegativeIntegerID = _r.NameTable.Add("nonNegativeInteger");
            _nonPositiveIntegerID = _r.NameTable.Add("nonPositiveInteger");
            _normalizedStringID = _r.NameTable.Add("normalizedString");
            _NOTATIONID = _r.NameTable.Add("NOTATION");
            _positiveIntegerID = _r.NameTable.Add("positiveInteger");
            _tokenID = _r.NameTable.Add("token");
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected XmlQualifiedName GetXsiType()
        {
            string type = _r.GetAttribute(_typeID, _instanceNsID);
            if (type == null)
            {
                type = _r.GetAttribute(_typeID, _instanceNs2000ID);
                if (type == null)
                {
                    type = _r.GetAttribute(_typeID, _instanceNs1999ID);
                    if (type == null)
                        return null;
                }
            }
            return ToXmlQualifiedName(type, false);
        }

        // throwOnUnknown flag controls whether this method throws an exception or just returns 
        // null if typeName.Namespace is unknown. the method still throws if typeName.Namespace
        // is recognized but typeName.Name isn't.
        private Type GetPrimitiveType(XmlQualifiedName typeName, bool throwOnUnknown)
        {
            InitPrimitiveIDs();

            if ((object)typeName.Namespace == (object)_schemaNsID || (object)typeName.Namespace == (object)_soapNsID || (object)typeName.Namespace == (object)_soap12NsID)
            {
                if ((object)typeName.Name == (object)_stringID ||
                    (object)typeName.Name == (object)_anyURIID ||
                    (object)typeName.Name == (object)_durationID ||
                    (object)typeName.Name == (object)_ENTITYID ||
                    (object)typeName.Name == (object)_ENTITIESID ||
                    (object)typeName.Name == (object)_gDayID ||
                    (object)typeName.Name == (object)_gMonthID ||
                    (object)typeName.Name == (object)_gMonthDayID ||
                    (object)typeName.Name == (object)_gYearID ||
                    (object)typeName.Name == (object)_gYearMonthID ||
                    (object)typeName.Name == (object)_IDID ||
                    (object)typeName.Name == (object)_IDREFID ||
                    (object)typeName.Name == (object)_IDREFSID ||
                    (object)typeName.Name == (object)_integerID ||
                    (object)typeName.Name == (object)_languageID ||
                    (object)typeName.Name == (object)_nameID ||
                    (object)typeName.Name == (object)_NCNameID ||
                    (object)typeName.Name == (object)_NMTOKENID ||
                    (object)typeName.Name == (object)_NMTOKENSID ||
                    (object)typeName.Name == (object)_negativeIntegerID ||
                    (object)typeName.Name == (object)_nonPositiveIntegerID ||
                    (object)typeName.Name == (object)_nonNegativeIntegerID ||
                    (object)typeName.Name == (object)_normalizedStringID ||
                    (object)typeName.Name == (object)_NOTATIONID ||
                    (object)typeName.Name == (object)_positiveIntegerID ||
                    (object)typeName.Name == (object)_tokenID)
                    return typeof(string);
                else if ((object)typeName.Name == (object)_intID)
                    return typeof(int);
                else if ((object)typeName.Name == (object)_booleanID)
                    return typeof(bool);
                else if ((object)typeName.Name == (object)_shortID)
                    return typeof(short);
                else if ((object)typeName.Name == (object)_longID)
                    return typeof(long);
                else if ((object)typeName.Name == (object)_floatID)
                    return typeof(float);
                else if ((object)typeName.Name == (object)_doubleID)
                    return typeof(double);
                else if ((object)typeName.Name == (object)_decimalID)
                    return typeof(decimal);
                else if ((object)typeName.Name == (object)_dateTimeID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_qnameID)
                    return typeof(XmlQualifiedName);
                else if ((object)typeName.Name == (object)_dateID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_timeID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_hexBinaryID)
                    return typeof(byte[]);
                else if ((object)typeName.Name == (object)_base64BinaryID)
                    return typeof(byte[]);
                else if ((object)typeName.Name == (object)_unsignedByteID)
                    return typeof(byte);
                else if ((object)typeName.Name == (object)_byteID)
                    return typeof(SByte);
                else if ((object)typeName.Name == (object)_unsignedShortID)
                    return typeof(UInt16);
                else if ((object)typeName.Name == (object)_unsignedIntID)
                    return typeof(UInt32);
                else if ((object)typeName.Name == (object)_unsignedLongID)
                    return typeof(UInt64);
                else
                    throw CreateUnknownTypeException(typeName);
            }
            else if ((object)typeName.Namespace == (object)_schemaNs2000ID || (object)typeName.Namespace == (object)_schemaNs1999ID)
            {
                if ((object)typeName.Name == (object)_stringID ||
                    (object)typeName.Name == (object)_anyURIID ||
                    (object)typeName.Name == (object)_durationID ||
                    (object)typeName.Name == (object)_ENTITYID ||
                    (object)typeName.Name == (object)_ENTITIESID ||
                    (object)typeName.Name == (object)_gDayID ||
                    (object)typeName.Name == (object)_gMonthID ||
                    (object)typeName.Name == (object)_gMonthDayID ||
                    (object)typeName.Name == (object)_gYearID ||
                    (object)typeName.Name == (object)_gYearMonthID ||
                    (object)typeName.Name == (object)_IDID ||
                    (object)typeName.Name == (object)_IDREFID ||
                    (object)typeName.Name == (object)_IDREFSID ||
                    (object)typeName.Name == (object)_integerID ||
                    (object)typeName.Name == (object)_languageID ||
                    (object)typeName.Name == (object)_nameID ||
                    (object)typeName.Name == (object)_NCNameID ||
                    (object)typeName.Name == (object)_NMTOKENID ||
                    (object)typeName.Name == (object)_NMTOKENSID ||
                    (object)typeName.Name == (object)_negativeIntegerID ||
                    (object)typeName.Name == (object)_nonPositiveIntegerID ||
                    (object)typeName.Name == (object)_nonNegativeIntegerID ||
                    (object)typeName.Name == (object)_normalizedStringID ||
                    (object)typeName.Name == (object)_NOTATIONID ||
                    (object)typeName.Name == (object)_positiveIntegerID ||
                    (object)typeName.Name == (object)_tokenID)
                    return typeof(string);
                else if ((object)typeName.Name == (object)_intID)
                    return typeof(int);
                else if ((object)typeName.Name == (object)_booleanID)
                    return typeof(bool);
                else if ((object)typeName.Name == (object)_shortID)
                    return typeof(short);
                else if ((object)typeName.Name == (object)_longID)
                    return typeof(long);
                else if ((object)typeName.Name == (object)_floatID)
                    return typeof(float);
                else if ((object)typeName.Name == (object)_doubleID)
                    return typeof(double);
                else if ((object)typeName.Name == (object)_oldDecimalID)
                    return typeof(decimal);
                else if ((object)typeName.Name == (object)_oldTimeInstantID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_qnameID)
                    return typeof(XmlQualifiedName);
                else if ((object)typeName.Name == (object)_dateID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_timeID)
                    return typeof(DateTime);
                else if ((object)typeName.Name == (object)_hexBinaryID)
                    return typeof(byte[]);
                else if ((object)typeName.Name == (object)_byteID)
                    return typeof(SByte);
                else if ((object)typeName.Name == (object)_unsignedShortID)
                    return typeof(UInt16);
                else if ((object)typeName.Name == (object)_unsignedIntID)
                    return typeof(UInt32);
                else if ((object)typeName.Name == (object)_unsignedLongID)
                    return typeof(UInt64);
                else
                    throw CreateUnknownTypeException(typeName);
            }
            else if ((object)typeName.Namespace == (object)_schemaNonXsdTypesNsID)
            {
                if ((object)typeName.Name == (object)_charID)
                    return typeof(char);
                else if ((object)typeName.Name == (object)_guidID)
                    return typeof(Guid);
                else
                    throw CreateUnknownTypeException(typeName);
            }
            else if (throwOnUnknown)
                throw CreateUnknownTypeException(typeName);
            else
                return null;
        }

        private bool IsPrimitiveNamespace(string ns)
        {
            return (object)ns == (object)_schemaNsID ||
                   (object)ns == (object)_schemaNonXsdTypesNsID ||
                   (object)ns == (object)_soapNsID ||
                   (object)ns == (object)_soap12NsID ||
                   (object)ns == (object)_schemaNs2000ID ||
                   (object)ns == (object)_schemaNs1999ID;
        }

        private string ReadStringValue()
        {
            if (_r.IsEmptyElement)
            {
                _r.Skip();
                return string.Empty;
            }
            _r.ReadStartElement();
            string retVal = _r.ReadString();
            ReadEndElement();
            return retVal;
        }

        private XmlQualifiedName ReadXmlQualifiedName()
        {
            string s;
            bool isEmpty = false;
            if (_r.IsEmptyElement)
            {
                s = string.Empty;
                isEmpty = true;
            }
            else
            {
                _r.ReadStartElement();
                s = _r.ReadString();
            }

            XmlQualifiedName retVal = ToXmlQualifiedName(s);
            if (isEmpty)
                _r.Skip();
            else
                ReadEndElement();
            return retVal;
        }

        private byte[] ReadByteArray(bool isBase64)
        {
            ArrayList list = new ArrayList();
            const int MAX_ALLOC_SIZE = 64 * 1024;
            int currentSize = 1024;
            byte[] buffer;
            int bytes = -1;
            int offset = 0;
            int total = 0;
            buffer = new byte[currentSize];
            list.Add(buffer);
            while (bytes != 0)
            {
                if (offset == buffer.Length)
                {
                    currentSize = Math.Min(currentSize * 2, MAX_ALLOC_SIZE);
                    buffer = new byte[currentSize];
                    offset = 0;
                    list.Add(buffer);
                }
                if (isBase64)
                {
                    bytes = _r.ReadElementContentAsBase64(buffer, offset, buffer.Length - offset);
                }
                else
                {
                    bytes = _r.ReadElementContentAsBinHex(buffer, offset, buffer.Length - offset);
                }
                offset += bytes;
                total += bytes;
            }

            byte[] result = new byte[total];
            offset = 0;
            foreach (byte[] block in list)
            {
                currentSize = Math.Min(block.Length, total);
                if (currentSize > 0)
                {
                    Buffer.BlockCopy(block, 0, result, offset, currentSize);
                    offset += currentSize;
                    total -= currentSize;
                }
            }
            list.Clear();
            return result;
        }

        protected object ReadTypedPrimitive(XmlQualifiedName type)
        {
            return ReadTypedPrimitive(type, false);
        }

        private object ReadTypedPrimitive(XmlQualifiedName type, bool elementCanBeType)
        {
            InitPrimitiveIDs();
            object value = null;
            if (!IsPrimitiveNamespace(type.Namespace) || (object)type.Name == (object)_urTypeID)
                return ReadXmlNodes(elementCanBeType);

            if ((object)type.Namespace == (object)_schemaNsID || (object)type.Namespace == (object)_soapNsID || (object)type.Namespace == (object)_soap12NsID)
            {
                if ((object)type.Name == (object)_stringID ||
                    (object)type.Name == (object)_normalizedStringID)
                    value = ReadStringValue();
                else if ((object)type.Name == (object)_anyURIID ||
                    (object)type.Name == (object)_durationID ||
                    (object)type.Name == (object)_ENTITYID ||
                    (object)type.Name == (object)_ENTITIESID ||
                    (object)type.Name == (object)_gDayID ||
                    (object)type.Name == (object)_gMonthID ||
                    (object)type.Name == (object)_gMonthDayID ||
                    (object)type.Name == (object)_gYearID ||
                    (object)type.Name == (object)_gYearMonthID ||
                    (object)type.Name == (object)_IDID ||
                    (object)type.Name == (object)_IDREFID ||
                    (object)type.Name == (object)_IDREFSID ||
                    (object)type.Name == (object)_integerID ||
                    (object)type.Name == (object)_languageID ||
                    (object)type.Name == (object)_nameID ||
                    (object)type.Name == (object)_NCNameID ||
                    (object)type.Name == (object)_NMTOKENID ||
                    (object)type.Name == (object)_NMTOKENSID ||
                    (object)type.Name == (object)_negativeIntegerID ||
                    (object)type.Name == (object)_nonPositiveIntegerID ||
                    (object)type.Name == (object)_nonNegativeIntegerID ||
                    (object)type.Name == (object)_NOTATIONID ||
                    (object)type.Name == (object)_positiveIntegerID ||
                    (object)type.Name == (object)_tokenID)
                    value = CollapseWhitespace(ReadStringValue());
                else if ((object)type.Name == (object)_intID)
                    value = XmlConvert.ToInt32(ReadStringValue());
                else if ((object)type.Name == (object)_booleanID)
                    value = XmlConvert.ToBoolean(ReadStringValue());
                else if ((object)type.Name == (object)_shortID)
                    value = XmlConvert.ToInt16(ReadStringValue());
                else if ((object)type.Name == (object)_longID)
                    value = XmlConvert.ToInt64(ReadStringValue());
                else if ((object)type.Name == (object)_floatID)
                    value = XmlConvert.ToSingle(ReadStringValue());
                else if ((object)type.Name == (object)_doubleID)
                    value = XmlConvert.ToDouble(ReadStringValue());
                else if ((object)type.Name == (object)_decimalID)
                    value = XmlConvert.ToDecimal(ReadStringValue());
                else if ((object)type.Name == (object)_dateTimeID)
                    value = ToDateTime(ReadStringValue());
                else if ((object)type.Name == (object)_qnameID)
                    value = ReadXmlQualifiedName();
                else if ((object)type.Name == (object)_dateID)
                    value = ToDate(ReadStringValue());
                else if ((object)type.Name == (object)_timeID)
                    value = ToTime(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedByteID)
                    value = XmlConvert.ToByte(ReadStringValue());
                else if ((object)type.Name == (object)_byteID)
                    value = XmlConvert.ToSByte(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedShortID)
                    value = XmlConvert.ToUInt16(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedIntID)
                    value = XmlConvert.ToUInt32(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedLongID)
                    value = XmlConvert.ToUInt64(ReadStringValue());
                else if ((object)type.Name == (object)_hexBinaryID)
                    value = ToByteArrayHex(false);
                else if ((object)type.Name == (object)_base64BinaryID)
                    value = ToByteArrayBase64(false);
                else if ((object)type.Name == (object)_base64ID && ((object)type.Namespace == (object)_soapNsID || (object)type.Namespace == (object)_soap12NsID))
                    value = ToByteArrayBase64(false);
                else
                    value = ReadXmlNodes(elementCanBeType);
            }
            else if ((object)type.Namespace == (object)_schemaNs2000ID || (object)type.Namespace == (object)_schemaNs1999ID)
            {
                if ((object)type.Name == (object)_stringID ||
                    (object)type.Name == (object)_normalizedStringID)
                    value = ReadStringValue();
                else if ((object)type.Name == (object)_anyURIID ||
                    (object)type.Name == (object)_anyURIID ||
                    (object)type.Name == (object)_durationID ||
                    (object)type.Name == (object)_ENTITYID ||
                    (object)type.Name == (object)_ENTITIESID ||
                    (object)type.Name == (object)_gDayID ||
                    (object)type.Name == (object)_gMonthID ||
                    (object)type.Name == (object)_gMonthDayID ||
                    (object)type.Name == (object)_gYearID ||
                    (object)type.Name == (object)_gYearMonthID ||
                    (object)type.Name == (object)_IDID ||
                    (object)type.Name == (object)_IDREFID ||
                    (object)type.Name == (object)_IDREFSID ||
                    (object)type.Name == (object)_integerID ||
                    (object)type.Name == (object)_languageID ||
                    (object)type.Name == (object)_nameID ||
                    (object)type.Name == (object)_NCNameID ||
                    (object)type.Name == (object)_NMTOKENID ||
                    (object)type.Name == (object)_NMTOKENSID ||
                    (object)type.Name == (object)_negativeIntegerID ||
                    (object)type.Name == (object)_nonPositiveIntegerID ||
                    (object)type.Name == (object)_nonNegativeIntegerID ||
                    (object)type.Name == (object)_NOTATIONID ||
                    (object)type.Name == (object)_positiveIntegerID ||
                    (object)type.Name == (object)_tokenID)
                    value = CollapseWhitespace(ReadStringValue());
                else if ((object)type.Name == (object)_intID)
                    value = XmlConvert.ToInt32(ReadStringValue());
                else if ((object)type.Name == (object)_booleanID)
                    value = XmlConvert.ToBoolean(ReadStringValue());
                else if ((object)type.Name == (object)_shortID)
                    value = XmlConvert.ToInt16(ReadStringValue());
                else if ((object)type.Name == (object)_longID)
                    value = XmlConvert.ToInt64(ReadStringValue());
                else if ((object)type.Name == (object)_floatID)
                    value = XmlConvert.ToSingle(ReadStringValue());
                else if ((object)type.Name == (object)_doubleID)
                    value = XmlConvert.ToDouble(ReadStringValue());
                else if ((object)type.Name == (object)_oldDecimalID)
                    value = XmlConvert.ToDecimal(ReadStringValue());
                else if ((object)type.Name == (object)_oldTimeInstantID)
                    value = ToDateTime(ReadStringValue());
                else if ((object)type.Name == (object)_qnameID)
                    value = ReadXmlQualifiedName();
                else if ((object)type.Name == (object)_dateID)
                    value = ToDate(ReadStringValue());
                else if ((object)type.Name == (object)_timeID)
                    value = ToTime(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedByteID)
                    value = XmlConvert.ToByte(ReadStringValue());
                else if ((object)type.Name == (object)_byteID)
                    value = XmlConvert.ToSByte(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedShortID)
                    value = XmlConvert.ToUInt16(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedIntID)
                    value = XmlConvert.ToUInt32(ReadStringValue());
                else if ((object)type.Name == (object)_unsignedLongID)
                    value = XmlConvert.ToUInt64(ReadStringValue());
                else
                    value = ReadXmlNodes(elementCanBeType);
            }
            else if ((object)type.Namespace == (object)_schemaNonXsdTypesNsID)
            {
                if ((object)type.Name == (object)_charID)
                    value = ToChar(ReadStringValue());
                else if ((object)type.Name == (object)_guidID)
                    value = new Guid(CollapseWhitespace(ReadStringValue()));
                else if ((object)type.Name == (object)_timeSpanID)
                    value = XmlConvert.ToTimeSpan(ReadStringValue());
                else
                    value = ReadXmlNodes(elementCanBeType);
            }
            else
                value = ReadXmlNodes(elementCanBeType);
            return value;
        }

        protected object ReadTypedNull(XmlQualifiedName type)
        {
            InitPrimitiveIDs();
            object value = null;
            if (!IsPrimitiveNamespace(type.Namespace) || (object)type.Name == (object)_urTypeID)
            {
                return null;
            }

            if ((object)type.Namespace == (object)_schemaNsID || (object)type.Namespace == (object)_soapNsID || (object)type.Namespace == (object)_soap12NsID)
            {
                if ((object)type.Name == (object)_stringID ||
                    (object)type.Name == (object)_anyURIID ||
                    (object)type.Name == (object)_durationID ||
                    (object)type.Name == (object)_ENTITYID ||
                    (object)type.Name == (object)_ENTITIESID ||
                    (object)type.Name == (object)_gDayID ||
                    (object)type.Name == (object)_gMonthID ||
                    (object)type.Name == (object)_gMonthDayID ||
                    (object)type.Name == (object)_gYearID ||
                    (object)type.Name == (object)_gYearMonthID ||
                    (object)type.Name == (object)_IDID ||
                    (object)type.Name == (object)_IDREFID ||
                    (object)type.Name == (object)_IDREFSID ||
                    (object)type.Name == (object)_integerID ||
                    (object)type.Name == (object)_languageID ||
                    (object)type.Name == (object)_nameID ||
                    (object)type.Name == (object)_NCNameID ||
                    (object)type.Name == (object)_NMTOKENID ||
                    (object)type.Name == (object)_NMTOKENSID ||
                    (object)type.Name == (object)_negativeIntegerID ||
                    (object)type.Name == (object)_nonPositiveIntegerID ||
                    (object)type.Name == (object)_nonNegativeIntegerID ||
                    (object)type.Name == (object)_normalizedStringID ||
                    (object)type.Name == (object)_NOTATIONID ||
                    (object)type.Name == (object)_positiveIntegerID ||
                    (object)type.Name == (object)_tokenID)
                    value = null;
                else if ((object)type.Name == (object)_intID)
                {
                    value = default(Nullable<int>);
                }
                else if ((object)type.Name == (object)_booleanID)
                    value = default(Nullable<bool>);
                else if ((object)type.Name == (object)_shortID)
                    value = default(Nullable<Int16>);
                else if ((object)type.Name == (object)_longID)
                    value = default(Nullable<long>);
                else if ((object)type.Name == (object)_floatID)
                    value = default(Nullable<float>);
                else if ((object)type.Name == (object)_doubleID)
                    value = default(Nullable<double>);
                else if ((object)type.Name == (object)_decimalID)
                    value = default(Nullable<decimal>);
                else if ((object)type.Name == (object)_dateTimeID)
                    value = default(Nullable<DateTime>);
                else if ((object)type.Name == (object)_qnameID)
                    value = null;
                else if ((object)type.Name == (object)_dateID)
                    value = default(Nullable<DateTime>);
                else if ((object)type.Name == (object)_timeID)
                    value = default(Nullable<DateTime>);
                else if ((object)type.Name == (object)_unsignedByteID)
                    value = default(Nullable<byte>);
                else if ((object)type.Name == (object)_byteID)
                    value = default(Nullable<SByte>);
                else if ((object)type.Name == (object)_unsignedShortID)
                    value = default(Nullable<UInt16>);
                else if ((object)type.Name == (object)_unsignedIntID)
                    value = default(Nullable<UInt32>);
                else if ((object)type.Name == (object)_unsignedLongID)
                    value = default(Nullable<UInt64>);
                else if ((object)type.Name == (object)_hexBinaryID)
                    value = null;
                else if ((object)type.Name == (object)_base64BinaryID)
                    value = null;
                else if ((object)type.Name == (object)_base64ID && ((object)type.Namespace == (object)_soapNsID || (object)type.Namespace == (object)_soap12NsID))
                    value = null;
                else
                    value = null;
            }
            else if ((object)type.Namespace == (object)_schemaNonXsdTypesNsID)
            {
                if ((object)type.Name == (object)_charID)
                    value = default(Nullable<char>);
                else if ((object)type.Name == (object)_guidID)
                    value = default(Nullable<Guid>);
                else if ((object)type.Name == (object)_timeSpanID)
                    value = default(Nullable<TimeSpan>);
                else
                    value = null;
            }
            else
                value = null;
            return value;
        }

        protected bool IsXmlnsAttribute(string name)
        {
            if (!name.StartsWith("xmlns", StringComparison.Ordinal)) return false;
            if (name.Length == 5) return true;
            return name[5] == ':';
        }

        protected void ParseWsdlArrayType(XmlAttribute attr)
        {
            if ((object)attr.LocalName == (object)_wsdlArrayTypeID && (object)attr.NamespaceURI == (object)_wsdlNsID)
            {
                int colon = attr.Value.LastIndexOf(':');
                if (colon < 0)
                {
                    attr.Value = _r.LookupNamespace("") + ":" + attr.Value;
                }
                else
                {
                    attr.Value = _r.LookupNamespace(attr.Value.Substring(0, colon)) + ":" +
                        attr.Value.Substring(colon + 1);
                }
            }
            return;
        }

        protected bool IsReturnValue
        {
            // value only valid for soap 1.1
            get { return _isReturnValue && !_soap12; }
            set { _isReturnValue = value; }
        }

        protected bool ReadNull()
        {
            if (!GetNullAttr()) return false;
            if (_r.IsEmptyElement)
            {
                _r.Skip();
                return true;
            }
            _r.ReadStartElement();
            int whileIterations = 0;
            int readerCount = ReaderCount;
            while (_r.NodeType != XmlNodeType.EndElement)
            {
                UnknownNode(null);
                CheckReaderCount(ref whileIterations, ref readerCount);
            }
            ReadEndElement();
            return true;
        }

        protected bool GetNullAttr()
        {
            string isNull = _r.GetAttribute(_nilID, _instanceNsID);
            if (isNull == null)
                isNull = _r.GetAttribute(_nullID, _instanceNsID);
            if (isNull == null)
            {
                isNull = _r.GetAttribute(_nullID, _instanceNs2000ID);
                if (isNull == null)
                    isNull = _r.GetAttribute(_nullID, _instanceNs1999ID);
            }
            if (isNull == null || !XmlConvert.ToBoolean(isNull)) return false;
            return true;
        }

        protected string ReadNullableString()
        {
            if (ReadNull()) return null;
            return _r.ReadElementString();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected XmlQualifiedName ReadNullableQualifiedName()
        {
            if (ReadNull()) return null;
            return ReadElementQualifiedName();
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected XmlQualifiedName ReadElementQualifiedName()
        {
            if (_r.IsEmptyElement)
            {
                XmlQualifiedName empty = new XmlQualifiedName(string.Empty, _r.LookupNamespace(""));
                _r.Skip();
                return empty;
            }
            XmlQualifiedName qname = ToXmlQualifiedName(CollapseWhitespace(_r.ReadString()));
            _r.ReadEndElement();
            return qname;
        }

        protected XmlDocument ReadXmlDocument(bool wrapped)
        {
            XmlNode n = ReadXmlNode(wrapped);
            if (n == null)
                return null;
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.ImportNode(n, true));
            return doc;
        }

        protected string CollapseWhitespace(string value)
        {
            if (value == null)
                return null;
            return value.Trim();
        }

        protected XmlNode ReadXmlNode(bool wrapped)
        {
            XmlNode node = null;
            if (wrapped)
            {
                if (ReadNull()) return null;
                _r.ReadStartElement();
                _r.MoveToContent();
                if (_r.NodeType != XmlNodeType.EndElement)
                    node = Document.ReadNode(_r);
                int whileIterations = 0;
                int readerCount = ReaderCount;
                while (_r.NodeType != XmlNodeType.EndElement)
                {
                    UnknownNode(null);
                    CheckReaderCount(ref whileIterations, ref readerCount);
                }
                _r.ReadEndElement();
            }
            else
            {
                node = Document.ReadNode(_r);
            }
            return node;
        }

        protected static byte[] ToByteArrayBase64(string value)
        {
            return XmlCustomFormatter.ToByteArrayBase64(value);
        }

        protected byte[] ToByteArrayBase64(bool isNull)
        {
            if (isNull)
            {
                return null;
            }
            return ReadByteArray(true); //means use Base64
        }

        protected static byte[] ToByteArrayHex(string value)
        {
            return XmlCustomFormatter.ToByteArrayHex(value);
        }

        protected byte[] ToByteArrayHex(bool isNull)
        {
            if (isNull)
            {
                return null;
            }
            return ReadByteArray(false); //means use BinHex
        }

        protected int GetArrayLength(string name, string ns)
        {
            if (GetNullAttr()) return 0;
            string arrayType = _r.GetAttribute(_arrayTypeID, _soapNsID);
            SoapArrayInfo arrayInfo = ParseArrayType(arrayType);
            if (arrayInfo.dimensions != 1) throw new InvalidOperationException(SR.Format(SR.XmlInvalidArrayDimentions, CurrentTag()));
            XmlQualifiedName qname = ToXmlQualifiedName(arrayInfo.qname, false);
            if (qname.Name != name) throw new InvalidOperationException(SR.Format(SR.XmlInvalidArrayTypeName, qname.Name, name, CurrentTag()));
            if (qname.Namespace != ns) throw new InvalidOperationException(SR.Format(SR.XmlInvalidArrayTypeNamespace, qname.Namespace, ns, CurrentTag()));
            return arrayInfo.length;
        }

        private struct SoapArrayInfo
        {
            public string qname;
            public int dimensions;
            public int length;
            public int jaggedDimensions;
        }

        private SoapArrayInfo ParseArrayType(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(SR.Format(SR.XmlMissingArrayType, CurrentTag()));
            }

            if (value.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.XmlEmptyArrayType, CurrentTag()), nameof(value));
            }

            char[] chars = value.ToCharArray();
            int charsLength = chars.Length;

            SoapArrayInfo soapArrayInfo = new SoapArrayInfo();

            // Parse backwards to get length first, then optional dimensions, then qname.
            int pos = charsLength - 1;

            // Must end with ]
            if (chars[pos] != ']')
            {
                throw new ArgumentException(SR.XmlInvalidArraySyntax, nameof(value));
            }
            pos--;

            // Find [
            while (pos != -1 && chars[pos] != '[')
            {
                if (chars[pos] == ',')
                    throw new ArgumentException(SR.Format(SR.XmlInvalidArrayDimentions, CurrentTag()), nameof(value));
                pos--;
            }
            if (pos == -1)
            {
                throw new ArgumentException(SR.XmlMismatchedArrayBrackets, nameof(value));
            }

            int len = charsLength - pos - 2;
            if (len > 0)
            {
                string lengthString = new String(chars, pos + 1, len);
                try
                {
                    soapArrayInfo.length = Int32.Parse(lengthString, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    if (e is OutOfMemoryException)
                    {
                        throw;
                    }
                    throw new ArgumentException(SR.Format(SR.XmlInvalidArrayLength, lengthString), nameof(value));
                }
            }
            else
            {
                soapArrayInfo.length = -1;
            }

            pos--;

            soapArrayInfo.jaggedDimensions = 0;
            while (pos != -1 && chars[pos] == ']')
            {
                pos--;
                if (pos < 0)
                    throw new ArgumentException(SR.XmlMismatchedArrayBrackets, nameof(value));
                if (chars[pos] == ',')
                    throw new ArgumentException(SR.Format(SR.XmlInvalidArrayDimentions, CurrentTag()), nameof(value));
                else if (chars[pos] != '[')
                    throw new ArgumentException(SR.XmlInvalidArraySyntax, nameof(value));
                pos--;
                soapArrayInfo.jaggedDimensions++;
            }

            soapArrayInfo.dimensions = 1;

            // everything else is qname - validation of qnames?
            soapArrayInfo.qname = new String(chars, 0, pos + 1);
            return soapArrayInfo;
        }

        private SoapArrayInfo ParseSoap12ArrayType(string itemType, string arraySize)
        {
            SoapArrayInfo soapArrayInfo = new SoapArrayInfo();

            if (itemType != null && itemType.Length > 0)
                soapArrayInfo.qname = itemType;
            else
                soapArrayInfo.qname = "";

            string[] dimensions;
            if (arraySize != null && arraySize.Length > 0)
                dimensions = arraySize.Split(null);
            else
                dimensions = new string[0];

            soapArrayInfo.dimensions = 0;
            soapArrayInfo.length = -1;
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (dimensions[i].Length > 0)
                {
                    if (dimensions[i] == "*")
                    {
                        soapArrayInfo.dimensions++;
                    }
                    else
                    {
                        try
                        {
                            soapArrayInfo.length = Int32.Parse(dimensions[i], CultureInfo.InvariantCulture);
                            soapArrayInfo.dimensions++;
                        }
                        catch (Exception e)
                        {
                            if (e is OutOfMemoryException)
                            {
                                throw;
                            }
                            throw new ArgumentException(SR.Format(SR.XmlInvalidArrayLength, dimensions[i]), "value");
                        }
                    }
                }
            }
            if (soapArrayInfo.dimensions == 0)
                soapArrayInfo.dimensions = 1; // default is 1D even if no arraySize is specified

            return soapArrayInfo;
        }

        protected static DateTime ToDateTime(string value)
        {
            return XmlCustomFormatter.ToDateTime(value);
        }

        protected static DateTime ToDate(string value)
        {
            return XmlCustomFormatter.ToDate(value);
        }

        protected static DateTime ToTime(string value)
        {
            return XmlCustomFormatter.ToTime(value);
        }

        protected static char ToChar(string value)
        {
            return XmlCustomFormatter.ToChar(value);
        }

        protected static long ToEnum(string value, Hashtable h, string typeName)
        {
            return XmlCustomFormatter.ToEnum(value, h, typeName, true);
        }

        protected static string ToXmlName(string value)
        {
            return XmlCustomFormatter.ToXmlName(value);
        }

        protected static string ToXmlNCName(string value)
        {
            return XmlCustomFormatter.ToXmlNCName(value);
        }

        protected static string ToXmlNmToken(string value)
        {
            return XmlCustomFormatter.ToXmlNmToken(value);
        }

        protected static string ToXmlNmTokens(string value)
        {
            return XmlCustomFormatter.ToXmlNmTokens(value);
        }

        protected XmlQualifiedName ToXmlQualifiedName(string value)
        {
            return ToXmlQualifiedName(value, DecodeName);
        }

        internal XmlQualifiedName ToXmlQualifiedName(string value, bool decodeName)
        {
            int colon = value == null ? -1 : value.LastIndexOf(':');
            string prefix = colon < 0 ? null : value.Substring(0, colon);
            string localName = value.Substring(colon + 1);

            if (decodeName)
            {
                prefix = XmlConvert.DecodeName(prefix);
                localName = XmlConvert.DecodeName(localName);
            }
            if (prefix == null || prefix.Length == 0)
            {
                return new XmlQualifiedName(_r.NameTable.Add(value), _r.LookupNamespace(String.Empty));
            }
            else
            {
                string ns = _r.LookupNamespace(prefix);
                if (ns == null)
                {
                    // Namespace prefix '{0}' is not defined.
                    throw new InvalidOperationException(SR.Format(SR.XmlUndefinedAlias, prefix));
                }
                return new XmlQualifiedName(_r.NameTable.Add(localName), ns);
            }
        }
        protected void UnknownAttribute(object o, XmlAttribute attr)
        {
            UnknownAttribute(o, attr, null);
        }

        protected void UnknownAttribute(object o, XmlAttribute attr, string qnames)
        {
            if (_events.OnUnknownAttribute != null)
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlAttributeEventArgs e = new XmlAttributeEventArgs(attr, lineNumber, linePosition, o, qnames);
                _events.OnUnknownAttribute(_events.sender, e);
            }
        }

        protected void UnknownElement(object o, XmlElement elem)
        {
            UnknownElement(o, elem, null);
        }

        protected void UnknownElement(object o, XmlElement elem, string qnames)
        {
            if (_events.OnUnknownElement != null)
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlElementEventArgs e = new XmlElementEventArgs(elem, lineNumber, linePosition, o, qnames);
                _events.OnUnknownElement(_events.sender, e);
            }
        }

        protected void UnknownNode(object o)
        {
            UnknownNode(o, null);
        }

        protected void UnknownNode(object o, string qnames)
        {
            if (_r.NodeType == XmlNodeType.None || _r.NodeType == XmlNodeType.Whitespace)
            {
                _r.Read();
                return;
            }
            if (_r.NodeType == XmlNodeType.EndElement)
                return;
            if (_events.OnUnknownNode != null)
            {
                UnknownNode(Document.ReadNode(_r), o, qnames);
            }
            else if (_r.NodeType == XmlNodeType.Attribute && _events.OnUnknownAttribute == null)
            {
                return;
            }
            else if (_r.NodeType == XmlNodeType.Element && _events.OnUnknownElement == null)
            {
                _r.Skip();
                return;
            }
            else
            {
                UnknownNode(Document.ReadNode(_r), o, qnames);
            }
        }

        private void UnknownNode(XmlNode unknownNode, object o, string qnames)
        {
            if (unknownNode == null)
                return;
            if (unknownNode.NodeType != XmlNodeType.None && unknownNode.NodeType != XmlNodeType.Whitespace && _events.OnUnknownNode != null)
            {
                int lineNumber, linePosition;
                GetCurrentPosition(out lineNumber, out linePosition);
                XmlNodeEventArgs e = new XmlNodeEventArgs(unknownNode, lineNumber, linePosition, o);
                _events.OnUnknownNode(_events.sender, e);
            }
            if (unknownNode.NodeType == XmlNodeType.Attribute)
            {
                UnknownAttribute(o, (XmlAttribute)unknownNode, qnames);
            }
            else if (unknownNode.NodeType == XmlNodeType.Element)
            {
                UnknownElement(o, (XmlElement)unknownNode, qnames);
            }
        }

        private void GetCurrentPosition(out int lineNumber, out int linePosition)
        {
            if (Reader is IXmlLineInfo)
            {
                IXmlLineInfo lineInfo = (IXmlLineInfo)Reader;
                lineNumber = lineInfo.LineNumber;
                linePosition = lineInfo.LinePosition;
            }
            else
                lineNumber = linePosition = -1;
        }

        protected void UnreferencedObject(string id, object o)
        {
            if (_events.OnUnreferencedObject != null)
            {
                UnreferencedObjectEventArgs e = new UnreferencedObjectEventArgs(o, id);
                _events.OnUnreferencedObject(_events.sender, e);
            }
        }

        private string CurrentTag()
        {
            switch (_r.NodeType)
            {
                case XmlNodeType.Element:
                    return "<" + _r.LocalName + " xmlns='" + _r.NamespaceURI + "'>";
                case XmlNodeType.EndElement:
                    return ">";
                case XmlNodeType.Text:
                    return _r.Value;
                case XmlNodeType.CDATA:
                    return "CDATA";
                case XmlNodeType.Comment:
                    return "<--";
                case XmlNodeType.ProcessingInstruction:
                    return "<?";
                default:
                    return "(unknown)";
            }
        }

        protected Exception CreateUnknownTypeException(XmlQualifiedName type)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownType, type.Name, type.Namespace, CurrentTag()));
        }

        protected Exception CreateReadOnlyCollectionException(string name)
        {
            return new InvalidOperationException(SR.Format(SR.XmlReadOnlyCollection, name));
        }

        protected Exception CreateAbstractTypeException(string name, string ns)
        {
            return new InvalidOperationException(SR.Format(SR.XmlAbstractType, name, ns, CurrentTag()));
        }

        protected Exception CreateInaccessibleConstructorException(string typeName)
        {
            return new InvalidOperationException(SR.Format(SR.XmlConstructorInaccessible, typeName));
        }

        protected Exception CreateCtorHasSecurityException(string typeName)
        {
            return new InvalidOperationException(SR.Format(SR.XmlConstructorHasSecurityAttributes, typeName));
        }

        protected Exception CreateUnknownNodeException()
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownNode, CurrentTag()));
        }

        protected Exception CreateUnknownConstantException(string value, Type enumType)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, value, enumType.Name));
        }

        protected Exception CreateInvalidCastException(Type type, object value)
        {
            return CreateInvalidCastException(type, value, null);
        }

        protected Exception CreateInvalidCastException(Type type, object value, string id)
        {
            if (value == null)
                return new InvalidCastException(SR.Format(SR.XmlInvalidNullCast, type.FullName));
            else if (id == null)
                return new InvalidCastException(SR.Format(SR.XmlInvalidCast, value.GetType().FullName, type.FullName));
            else
                return new InvalidCastException(SR.Format(SR.XmlInvalidCastWithId, value.GetType().FullName, type.FullName, id));
        }

        protected Exception CreateBadDerivationException(string xsdDerived, string nsDerived, string xsdBase, string nsBase, string clrDerived, string clrBase)
        {
            return new InvalidOperationException(SR.Format(SR.XmlSerializableBadDerivation, xsdDerived, nsDerived, xsdBase, nsBase, clrDerived, clrBase));
        }

        protected Exception CreateMissingIXmlSerializableType(string name, string ns, string clrType)
        {
            return new InvalidOperationException(SR.Format(SR.XmlSerializableMissingClrType, name, ns, typeof(XmlIncludeAttribute).Name, clrType));
            //XmlSerializableMissingClrType= Type '{0}' from namespace '{1}' doesnot have corresponding IXmlSerializable type. Please consider adding {2} to '{3}'.
        }

        protected Array EnsureArrayIndex(Array a, int index, Type elementType)
        {
            if (a == null) return Array.CreateInstance(elementType, 32);
            if (index < a.Length) return a;
            Array b = Array.CreateInstance(elementType, a.Length * 2);
            Array.Copy(a, b, index);
            return b;
        }

        protected Array ShrinkArray(Array a, int length, Type elementType, bool isNullable)
        {
            if (a == null)
            {
                if (isNullable) return null;
                return Array.CreateInstance(elementType, 0);
            }
            if (a.Length == length) return a;
            Array b = Array.CreateInstance(elementType, length);
            Array.Copy(a, b, length);
            return b;
        }
        // This is copied from Core's XmlReader.ReadString, as it is not exposed in the Contract.
        protected virtual string ReadString()
        {
            if (Reader.ReadState != ReadState.Interactive)
            {
                return string.Empty;
            }
            Reader.MoveToElement();
            if (Reader.NodeType == XmlNodeType.Element)
            {
                if (Reader.IsEmptyElement)
                {
                    return string.Empty;
                }
                else if (!Reader.Read())
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
                if (Reader.NodeType == XmlNodeType.EndElement)
                {
                    return string.Empty;
                }
            }
            string result = string.Empty;
            while (IsTextualNode(Reader.NodeType))
            {
                result += Reader.Value;
                if (!Reader.Read())
                {
                    break;
                }
            }
            return result;
        }

        // 0x6018
        private static uint s_isTextualNodeBitmap = (1 << (int)XmlNodeType.Text) | (1 << (int)XmlNodeType.CDATA) | (1 << (int)XmlNodeType.Whitespace) | (1 << (int)XmlNodeType.SignificantWhitespace);

        private static bool IsTextualNode(XmlNodeType nodeType)
        {
            return 0 != (s_isTextualNodeBitmap & (1 << (int)nodeType));
        }

        protected string ReadString(string value)
        {
            return ReadString(value, false);
        }

        protected string ReadString(string value, bool trim)
        {
            string str = _r.ReadString();
            if (str != null && trim)
                str = str.Trim();
            if (value == null || value.Length == 0)
                return str;
            return value + str;
        }

        protected IXmlSerializable ReadSerializable(IXmlSerializable serializable)
        {
            return ReadSerializable(serializable, false);
        }

        protected IXmlSerializable ReadSerializable(IXmlSerializable serializable, bool wrappedAny)
        {
            string name = null;
            string ns = null;

            if (wrappedAny)
            {
                name = _r.LocalName;
                ns = _r.NamespaceURI;
                _r.Read();
                _r.MoveToContent();
            }
            serializable.ReadXml(_r);

            if (wrappedAny)
            {
                while (_r.NodeType == XmlNodeType.Whitespace) _r.Skip();
                if (_r.NodeType == XmlNodeType.None) _r.Skip();
                if (_r.NodeType == XmlNodeType.EndElement && _r.LocalName == name && _r.NamespaceURI == ns)
                {
                    Reader.Read();
                }
            }
            return serializable;
        }

        protected bool ReadReference(out string fixupReference)
        {
            string href = _soap12 ? _r.GetAttribute("ref", Soap12.Encoding) : _r.GetAttribute("href");
            if (href == null)
            {
                fixupReference = null;
                return false;
            }
            if (!_soap12)
            {
                // soap 1.1 href starts with '#'; soap 1.2 ref does not
                if (!href.StartsWith("#", StringComparison.Ordinal)) throw new InvalidOperationException(SR.Format(SR.XmlMissingHref, href));
                fixupReference = href.Substring(1);
            }
            else
                fixupReference = href;

            if (_r.IsEmptyElement)
            {
                _r.Skip();
            }
            else
            {
                _r.ReadStartElement();
                ReadEndElement();
            }
            return true;
        }

        protected void AddTarget(string id, object o)
        {
            if (id == null)
            {
                if (_targetsWithoutIds == null)
                    _targetsWithoutIds = new ArrayList();
                if (o != null)
                    _targetsWithoutIds.Add(o);
            }
            else
            {
                if (_targets == null) _targets = new Hashtable();
                if (!_targets.Contains(id))
                    _targets.Add(id, o);
            }
        }



        protected void AddFixup(Fixup fixup)
        {
            if (_fixups == null) _fixups = new ArrayList();
            _fixups.Add(fixup);
        }

        protected void AddFixup(CollectionFixup fixup)
        {
            if (_collectionFixups == null) _collectionFixups = new ArrayList();
            _collectionFixups.Add(fixup);
        }

        protected object GetTarget(string id)
        {
            object target = _targets != null ? _targets[id] : null;
            if (target == null)
            {
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidHref, id));
            }
            Referenced(target);
            return target;
        }

        protected void Referenced(object o)
        {
            if (o == null) return;
            if (_referencedTargets == null) _referencedTargets = new Hashtable();
            _referencedTargets[o] = o;
        }

        private void HandleUnreferencedObjects()
        {
            if (_targets != null)
            {
                foreach (DictionaryEntry target in _targets)
                {
                    if (_referencedTargets == null || !_referencedTargets.Contains(target.Value))
                    {
                        UnreferencedObject((string)target.Key, target.Value);
                    }
                }
            }
            if (_targetsWithoutIds != null)
            {
                foreach (object o in _targetsWithoutIds)
                {
                    if (_referencedTargets == null || !_referencedTargets.Contains(o))
                    {
                        UnreferencedObject(null, o);
                    }
                }
            }
        }

        private void DoFixups()
        {
            if (_fixups == null) return;
            for (int i = 0; i < _fixups.Count; i++)
            {
                Fixup fixup = (Fixup)_fixups[i];
                fixup.Callback(fixup);
            }

            if (_collectionFixups == null) return;
            for (int i = 0; i < _collectionFixups.Count; i++)
            {
                CollectionFixup collectionFixup = (CollectionFixup)_collectionFixups[i];
                collectionFixup.Callback(collectionFixup.Collection, collectionFixup.CollectionItems);
            }
        }

        protected void FixupArrayRefs(object fixup)
        {
            Fixup f = (Fixup)fixup;
            Array array = (Array)f.Source;
            for (int i = 0; i < array.Length; i++)
            {
                string id = f.Ids[i];
                if (id == null) continue;
                object o = GetTarget(id);
                try
                {
                    array.SetValue(o, i);
                }
                catch (InvalidCastException)
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlInvalidArrayRef, id, o.GetType().FullName, i.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        private object ReadArray(string typeName, string typeNs)
        {
            SoapArrayInfo arrayInfo;
            Type fallbackElementType = null;
            if (_soap12)
            {
                string itemType = _r.GetAttribute(_itemTypeID, _soap12NsID);
                string arraySize = _r.GetAttribute(_arraySizeID, _soap12NsID);
                Type arrayType = (Type)_types[new XmlQualifiedName(typeName, typeNs)];
                // no indication that this is an array?
                if (itemType == null && arraySize == null && (arrayType == null || !arrayType.IsArray))
                    return null;

                arrayInfo = ParseSoap12ArrayType(itemType, arraySize);
                if (arrayType != null)
                    fallbackElementType = TypeScope.GetArrayElementType(arrayType, null);
            }
            else
            {
                string arrayType = _r.GetAttribute(_arrayTypeID, _soapNsID);
                if (arrayType == null)
                    return null;

                arrayInfo = ParseArrayType(arrayType);
            }

            if (arrayInfo.dimensions != 1) throw new InvalidOperationException(SR.Format(SR.XmlInvalidArrayDimentions, CurrentTag()));

            // NOTE: don't use the array size that is specified since an evil client might pass
            // a number larger than the actual number of items in an attempt to harm the server.

            XmlQualifiedName qname;
            bool isPrimitive;
            Type elementType = null;
            XmlQualifiedName urTypeName = new XmlQualifiedName(_urTypeID, _schemaNsID);
            if (arrayInfo.qname.Length > 0)
            {
                qname = ToXmlQualifiedName(arrayInfo.qname, false);
                elementType = (Type)_types[qname];
            }
            else
                qname = urTypeName;

            // try again if the best we could come up with was object
            if (_soap12 && elementType == typeof(object))
                elementType = null;

            if (elementType == null)
            {
                if (!_soap12)
                {
                    elementType = GetPrimitiveType(qname, true);
                    isPrimitive = true;
                }
                else
                {
                    // try it as a primitive
                    if (qname != urTypeName)
                        elementType = GetPrimitiveType(qname, false);
                    if (elementType != null)
                    {
                        isPrimitive = true;
                    }
                    else
                    {
                        // still nothing: go with fallback type or object
                        if (fallbackElementType == null)
                        {
                            elementType = typeof(object);
                            isPrimitive = false;
                        }
                        else
                        {
                            elementType = fallbackElementType;
                            XmlQualifiedName newQname = (XmlQualifiedName)_typesReverse[elementType];
                            if (newQname == null)
                            {
                                newQname = XmlSerializationWriter.GetPrimitiveTypeNameInternal(elementType);
                                isPrimitive = true;
                            }
                            else
                                isPrimitive = elementType.IsPrimitive;
                            if (newQname != null) qname = newQname;
                        }
                    }
                }
            }
            else
                isPrimitive = elementType.IsPrimitive;

            if (!_soap12 && arrayInfo.jaggedDimensions > 0)
            {
                for (int i = 0; i < arrayInfo.jaggedDimensions; i++)
                    elementType = elementType.MakeArrayType();
            }

            if (_r.IsEmptyElement)
            {
                _r.Skip();
                return Array.CreateInstance(elementType, 0);
            }

            _r.ReadStartElement();
            _r.MoveToContent();

            int arrayLength = 0;
            Array array = null;

            if (elementType.IsValueType)
            {
                if (!isPrimitive && !elementType.IsEnum)
                {
                    throw new NotSupportedException(SR.Format(SR.XmlRpcArrayOfValueTypes, elementType.FullName));
                }
                // CONSIDER, erikc, we could have specialized read functions here
                // for primitives, which would avoid boxing.
                int whileIterations = 0;
                int readerCount = ReaderCount;
                while (_r.NodeType != XmlNodeType.EndElement)
                {
                    array = EnsureArrayIndex(array, arrayLength, elementType);
                    array.SetValue(ReadReferencedElement(qname.Name, qname.Namespace), arrayLength);
                    arrayLength++;
                    _r.MoveToContent();
                    CheckReaderCount(ref whileIterations, ref readerCount);
                }
                array = ShrinkArray(array, arrayLength, elementType, false);
            }
            else
            {
                string type;
                string typens;
                string[] ids = null;
                int idsLength = 0;

                int whileIterations = 0;
                int readerCount = ReaderCount;
                while (_r.NodeType != XmlNodeType.EndElement)
                {
                    array = EnsureArrayIndex(array, arrayLength, elementType);
                    ids = (string[])EnsureArrayIndex(ids, idsLength, typeof(string));
                    // CONSIDER: i'm not sure it's correct to let item name take precedence over arrayType
                    if (_r.NamespaceURI.Length != 0)
                    {
                        type = _r.LocalName;
                        if ((object)_r.NamespaceURI == (object)_soapNsID)
                            typens = XmlSchema.Namespace;
                        else
                            typens = _r.NamespaceURI;
                    }
                    else
                    {
                        type = qname.Name;
                        typens = qname.Namespace;
                    }
                    array.SetValue(ReadReferencingElement(type, typens, out ids[idsLength]), arrayLength);
                    arrayLength++;
                    idsLength++;
                    // CONSIDER, erikc, sparse arrays, perhaps?
                    _r.MoveToContent();
                    CheckReaderCount(ref whileIterations, ref readerCount);
                }

                // special case for soap 1.2: try to get a better fit than object[] when no metadata is known
                // this applies in the doc/enc/bare case
                if (_soap12 && elementType == typeof(object))
                {
                    Type itemType = null;
                    for (int i = 0; i < arrayLength; i++)
                    {
                        object currItem = array.GetValue(i);
                        if (currItem != null)
                        {
                            Type currItemType = currItem.GetType();
                            if (currItemType.IsValueType)
                            {
                                itemType = null;
                                break;
                            }
                            if (itemType == null || currItemType.IsAssignableFrom(itemType))
                            {
                                itemType = currItemType;
                            }
                            else if (!itemType.IsAssignableFrom(currItemType))
                            {
                                itemType = null;
                                break;
                            }
                        }
                    }
                    if (itemType != null)
                        elementType = itemType;
                }
                ids = (string[])ShrinkArray(ids, idsLength, typeof(string), false);
                array = ShrinkArray(array, arrayLength, elementType, false);
                Fixup fixupArray = new Fixup(array, new XmlSerializationFixupCallback(this.FixupArrayRefs), ids);
                AddFixup(fixupArray);
            }

            // CONSIDER, erikc, check to see if the specified array length was right, perhaps?

            ReadEndElement();
            return array;
        }

        protected abstract void InitCallbacks();

        protected void ReadReferencedElements()
        {
            _r.MoveToContent();
            string dummy;
            int whileIterations = 0;
            int readerCount = ReaderCount;
            while (_r.NodeType != XmlNodeType.EndElement && _r.NodeType != XmlNodeType.None)
            {
                ReadReferencingElement(null, null, true, out dummy);
                _r.MoveToContent();
                CheckReaderCount(ref whileIterations, ref readerCount);
            }
            DoFixups();

            HandleUnreferencedObjects();
        }

        protected object ReadReferencedElement()
        {
            return ReadReferencedElement(null, null);
        }

        protected object ReadReferencedElement(string name, string ns)
        {
            string dummy;
            return ReadReferencingElement(name, ns, out dummy);
        }

        protected object ReadReferencingElement(out string fixupReference)
        {
            return ReadReferencingElement(null, null, out fixupReference);
        }

        protected object ReadReferencingElement(string name, string ns, out string fixupReference)
        {
            return ReadReferencingElement(name, ns, false, out fixupReference);
        }

        protected object ReadReferencingElement(string name, string ns, bool elementCanBeType, out string fixupReference)
        {
            object o = null;
            EnsureCallbackTables();

            _r.MoveToContent();

            if (ReadReference(out fixupReference)) return null;

            if (ReadNull()) return null;

            string id = _soap12 ? _r.GetAttribute("id", Soap12.Encoding) : _r.GetAttribute("id", null);

            if ((o = ReadArray(name, ns)) == null)
            {
                XmlQualifiedName typeId = GetXsiType();
                if (typeId == null)
                {
                    if (name == null)
                        typeId = new XmlQualifiedName(_r.NameTable.Add(_r.LocalName), _r.NameTable.Add(_r.NamespaceURI));
                    else
                        typeId = new XmlQualifiedName(_r.NameTable.Add(name), _r.NameTable.Add(ns));
                }
                XmlSerializationReadCallback callback = (XmlSerializationReadCallback)_callbacks[typeId];
                if (callback != null)
                {
                    o = callback();
                }
                else
                    o = ReadTypedPrimitive(typeId, elementCanBeType);
            }

            AddTarget(id, o);

            return o;
        }

        internal void EnsureCallbackTables()
        {
            if (_callbacks == null)
            {
                _callbacks = new Hashtable();
                _types = new Hashtable();
                XmlQualifiedName urType = new XmlQualifiedName(_urTypeID, _r.NameTable.Add(XmlSchema.Namespace));
                _types.Add(urType, typeof(object));
                _typesReverse = new Hashtable();
                _typesReverse.Add(typeof(object), urType);
                InitCallbacks();
            }
        }

        protected void AddReadCallback(string name, string ns, Type type, XmlSerializationReadCallback read)
        {
            XmlQualifiedName typeName = new XmlQualifiedName(_r.NameTable.Add(name), _r.NameTable.Add(ns));
            _callbacks[typeName] = read;
            _types[typeName] = type;
            _typesReverse[type] = typeName;
        }

        protected void ReadEndElement()
        {
            while (_r.NodeType == XmlNodeType.Whitespace) _r.Skip();
            if (_r.NodeType == XmlNodeType.None) _r.Skip();
            else _r.ReadEndElement();
        }

        private object ReadXmlNodes(bool elementCanBeType)
        {
            ArrayList xmlNodeList = new ArrayList();
            string elemLocalName = Reader.LocalName;
            string elemNs = Reader.NamespaceURI;
            string elemName = Reader.Name;
            string xsiTypeName = null;
            string xsiTypeNs = null;
            int skippableNodeCount = 0;
            int lineNumber = -1, linePosition = -1;
            XmlNode unknownNode = null;
            if (Reader.NodeType == XmlNodeType.Attribute)
            {
                XmlAttribute attr = Document.CreateAttribute(elemName, elemNs);
                attr.Value = Reader.Value;
                unknownNode = attr;
            }
            else
                unknownNode = Document.CreateElement(elemName, elemNs);
            GetCurrentPosition(out lineNumber, out linePosition);
            XmlElement unknownElement = unknownNode as XmlElement;

            while (Reader.MoveToNextAttribute())
            {
                if (IsXmlnsAttribute(Reader.Name) || (Reader.Name == "id" && (!_soap12 || Reader.NamespaceURI == Soap12.Encoding)))
                    skippableNodeCount++;
                if ((object)Reader.LocalName == (object)_typeID &&
                     ((object)Reader.NamespaceURI == (object)_instanceNsID ||
                       (object)Reader.NamespaceURI == (object)_instanceNs2000ID ||
                       (object)Reader.NamespaceURI == (object)_instanceNs1999ID
                     )
                   )
                {
                    string value = Reader.Value;
                    int colon = value.LastIndexOf(':');
                    xsiTypeName = (colon >= 0) ? value.Substring(colon + 1) : value;
                    xsiTypeNs = Reader.LookupNamespace((colon >= 0) ? value.Substring(0, colon) : "");
                }
                XmlAttribute xmlAttribute = (XmlAttribute)Document.ReadNode(_r);
                xmlNodeList.Add(xmlAttribute);
                if (unknownElement != null) unknownElement.SetAttributeNode(xmlAttribute);
            }

            // If the node is referenced (or in case of paramStyle = bare) and if xsi:type is not
            // specified then the element name is used as the type name. Reveal this to the user
            // by adding an extra attribute node "xsi:type" with value as the element name.
            if (elementCanBeType && xsiTypeName == null)
            {
                xsiTypeName = elemLocalName;
                xsiTypeNs = elemNs;
                XmlAttribute xsiTypeAttribute = Document.CreateAttribute(_typeID, _instanceNsID);
                xsiTypeAttribute.Value = elemName;
                xmlNodeList.Add(xsiTypeAttribute);
            }
            if (xsiTypeName == Soap.UrType &&
                ((object)xsiTypeNs == (object)_schemaNsID ||
                  (object)xsiTypeNs == (object)_schemaNs1999ID ||
                  (object)xsiTypeNs == (object)_schemaNs2000ID
                )
               )
                skippableNodeCount++;


            Reader.MoveToElement();
            if (Reader.IsEmptyElement)
            {
                Reader.Skip();
            }
            else
            {
                Reader.ReadStartElement();
                Reader.MoveToContent();
                int whileIterations = 0;
                int readerCount = ReaderCount;
                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement)
                {
                    XmlNode xmlNode = Document.ReadNode(_r);
                    xmlNodeList.Add(xmlNode);
                    if (unknownElement != null) unknownElement.AppendChild(xmlNode);
                    Reader.MoveToContent();
                    CheckReaderCount(ref whileIterations, ref readerCount);
                }
                ReadEndElement();
            }


            if (xmlNodeList.Count <= skippableNodeCount)
                return new object();

            XmlNode[] childNodes = (XmlNode[])xmlNodeList.ToArray(typeof(XmlNode));

            UnknownNode(unknownNode, null, null);
            return childNodes;
        }

        protected void CheckReaderCount(ref int whileIterations, ref int readerCount)
        {
            if (s_checkDeserializeAdvances)
            {
                whileIterations++;
                if ((whileIterations & 0x80) == 0x80)
                {
                    if (readerCount == ReaderCount)
                        throw new InvalidOperationException(SR.XmlInternalErrorReaderAdvance);
                    readerCount = ReaderCount;
                }
            }
        }

        ///<internalonly/>
        protected class Fixup
        {
            private XmlSerializationFixupCallback _callback;
            private object _source;
            private string[] _ids;

            public Fixup(object o, XmlSerializationFixupCallback callback, int count)
                : this(o, callback, new string[count])
            {
            }

            public Fixup(object o, XmlSerializationFixupCallback callback, string[] ids)
            {
                _callback = callback;
                this.Source = o;
                _ids = ids;
            }

            public XmlSerializationFixupCallback Callback
            {
                get { return _callback; }
            }

            public object Source
            {
                get { return _source; }
                set { _source = value; }
            }

            public string[] Ids
            {
                get { return _ids; }
            }
        }

        protected class CollectionFixup
        {
            private XmlSerializationCollectionFixupCallback _callback;
            private object _collection;
            private object _collectionItems;

            public CollectionFixup(object collection, XmlSerializationCollectionFixupCallback callback, object collectionItems)
            {
                _callback = callback;
                _collection = collection;
                _collectionItems = collectionItems;
            }

            public XmlSerializationCollectionFixupCallback Callback
            {
                get { return _callback; }
            }

            public object Collection
            {
                get { return _collection; }
            }

            public object CollectionItems
            {
                get { return _collectionItems; }
            }
        }
#endif
    }

#if !XMLSERIALIZERGENERATOR
    ///<internalonly/>
    public delegate void XmlSerializationFixupCallback(object fixup);


    ///<internalonly/>
    public delegate void XmlSerializationCollectionFixupCallback(object collection, object collectionItems);

    ///<internalonly/>
    public delegate object XmlSerializationReadCallback();
#endif
}
