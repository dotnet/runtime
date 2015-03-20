// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

namespace System.Xml.Serialization
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
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.Threading;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    using System.Xml.Extensions;
    using Hashtable = System.Collections.Generic.Dictionary<object, object>;
    using DictionaryEntry = System.Collections.Generic.KeyValuePair<object, object>;
    using XmlSchema = System.ServiceModel.Dispatcher.XmlSchemaConstants;
    using XmlDeserializationEvents = System.Object;

    /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader"]/*' />
    ///<internalonly/>
    public abstract class XmlSerializationReader : XmlSerializationGeneratedCode
    {
        private XmlReader _r;
        private XmlDeserializationEvents _events;
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



        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.InitIDs"]/*' />
        protected abstract void InitIDs();

        // this method must be called before any generated deserialization methods are called
        internal void Init(XmlReader r, XmlDeserializationEvents events, string encodingStyle, TempAssembly tempAssembly)
        {
            _events = events;
            _r = r;
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

        /// <include file='doc\XmlSerializationWriter.uex' path='docs/doc[@for="XmlSerializationWriter.DecodeName"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.Reader"]/*' />
        protected XmlReader Reader
        {
            get
            {
                return _r;
            }
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReaderCount"]/*' />
        protected int ReaderCount
        {
            get
            {
                return 0;
            }
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.GetXsiType"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadTypedPrimitive"]/*' />
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
                else
                    value = ReadXmlNodes(elementCanBeType);
            }
            else
                value = ReadXmlNodes(elementCanBeType);
            return value;
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadTypedNull"]/*' />
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
                else
                    value = null;
            }
            else
                value = null;
            return value;
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.IsXmlnsAttribute"]/*' />
        protected bool IsXmlnsAttribute(string name)
        {
            if (!name.StartsWith("xmlns", StringComparison.Ordinal)) return false;
            if (name.Length == 5) return true;
            return name[5] == ':';
        }


        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.IsReturnValue"]/*' />
        protected bool IsReturnValue
        {
            // value only valid for soap 1.1
            get { return _isReturnValue && !_soap12; }
            set { _isReturnValue = value; }
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadNull"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.GetNullAttr"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadNullableString"]/*' />
        protected string ReadNullableString()
        {
            if (ReadNull()) return null;
            return _r.ReadElementString();
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadNullableQualifiedName"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected XmlQualifiedName ReadNullableQualifiedName()
        {
            if (ReadNull()) return null;
            return ReadElementQualifiedName();
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadElementQualifiedName"]/*' />
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


        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CollapseWhitespace"]/*' />
        protected string CollapseWhitespace(string value)
        {
            if (value == null)
                return null;
            return value.Trim();
        }


        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToByteArrayBase64"]/*' />
        protected static byte[] ToByteArrayBase64(string value)
        {
            return XmlCustomFormatter.ToByteArrayBase64(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToByteArrayBase641"]/*' />
        protected byte[] ToByteArrayBase64(bool isNull)
        {
            if (isNull)
            {
                return null;
            }
            return ReadByteArray(true); //means use Base64
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToByteArrayHex"]/*' />
        protected static byte[] ToByteArrayHex(string value)
        {
            return XmlCustomFormatter.ToByteArrayHex(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToByteArrayHex1"]/*' />
        protected byte[] ToByteArrayHex(bool isNull)
        {
            if (isNull)
            {
                return null;
            }
            return ReadByteArray(false); //means use BinHex
        }





        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToDateTime"]/*' />
        protected static DateTime ToDateTime(string value)
        {
            return XmlCustomFormatter.ToDateTime(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToDate"]/*' />
        protected static DateTime ToDate(string value)
        {
            return XmlCustomFormatter.ToDate(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToTime"]/*' />
        protected static DateTime ToTime(string value)
        {
            return XmlCustomFormatter.ToTime(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToChar"]/*' />
        protected static char ToChar(string value)
        {
            return XmlCustomFormatter.ToChar(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToEnum"]/*' />
        protected static long ToEnum(string value, IDictionary h, string typeName)
        {
            return XmlCustomFormatter.ToEnum(value, h, typeName, true);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToXmlName"]/*' />
        protected static string ToXmlName(string value)
        {
            return XmlCustomFormatter.ToXmlName(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToXmlNCName"]/*' />
        protected static string ToXmlNCName(string value)
        {
            return XmlCustomFormatter.ToXmlNCName(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToXmlNmToken"]/*' />
        protected static string ToXmlNmToken(string value)
        {
            return XmlCustomFormatter.ToXmlNmToken(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToXmlNmTokens"]/*' />
        protected static string ToXmlNmTokens(string value)
        {
            return XmlCustomFormatter.ToXmlNmTokens(value);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ToXmlQualifiedName"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.UnknownNode"]/*' />
        protected void UnknownNode(object o)
        {
            UnknownNode(o, null);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.UnknownNode1"]/*' />
        protected void UnknownNode(object o, string qnames)
        {
            if (_r.NodeType == XmlNodeType.None || _r.NodeType == XmlNodeType.Whitespace)
            {
                _r.Read();
                return;
            }
            if (_r.NodeType == XmlNodeType.EndElement)
                return;
            if (_r.NodeType == XmlNodeType.Attribute)
            {
                return;
            }
            else
            {
                _r.Skip();
                return;
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateUnknownTypeException"]/*' />
        protected Exception CreateUnknownTypeException(XmlQualifiedName type)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownType, type.Name, type.Namespace, CurrentTag()));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateReadOnlyCollectionException"]/*' />
        protected Exception CreateReadOnlyCollectionException(string name)
        {
            return new InvalidOperationException(SR.Format(SR.XmlReadOnlyCollection, name));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateAbstractTypeException"]/*' />
        protected Exception CreateAbstractTypeException(string name, string ns)
        {
            return new InvalidOperationException(SR.Format(SR.XmlAbstractType, name, ns, CurrentTag()));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateInaccessibleConstructorException"]/*' />
        protected Exception CreateInaccessibleConstructorException(string typeName)
        {
            return new InvalidOperationException(SR.Format(SR.XmlConstructorInaccessible, typeName));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateCtorHasSecurityException"]/*' />
        protected Exception CreateCtorHasSecurityException(string typeName)
        {
            return new InvalidOperationException(SR.Format(SR.XmlConstructorHasSecurityAttributes, typeName));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateUnknownNodeException"]/*' />
        protected Exception CreateUnknownNodeException()
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownNode, CurrentTag()));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateUnknownConstantException"]/*' />
        protected Exception CreateUnknownConstantException(string value, Type enumType)
        {
            return new InvalidOperationException(SR.Format(SR.XmlUnknownConstant, value, enumType.Name));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateInvalidCastException"]/*' />
        protected Exception CreateInvalidCastException(Type type, object value)
        {
            return CreateInvalidCastException(type, value, null);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateInvalidCastException1"]/*' />
        protected Exception CreateInvalidCastException(Type type, object value, string id)
        {
            if (value == null)
                return new InvalidCastException(SR.Format(SR.XmlInvalidNullCast, type.FullName));
            else if (id == null)
                return new InvalidCastException(SR.Format(SR.XmlInvalidCast, value.GetType().FullName, type.FullName));
            else
                return new InvalidCastException(SR.Format(SR.XmlInvalidCastWithId, value.GetType().FullName, type.FullName, id));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateBadDerivationException"]/*' />
        protected Exception CreateBadDerivationException(string xsdDerived, string nsDerived, string xsdBase, string nsBase, string clrDerived, string clrBase)
        {
            return new InvalidOperationException(SR.Format(SR.XmlSerializableBadDerivation, xsdDerived, nsDerived, xsdBase, nsBase, clrDerived, clrBase));
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CreateMissingIXmlSerializableType"]/*' />
        protected Exception CreateMissingIXmlSerializableType(string name, string ns, string clrType)
        {
            return new InvalidOperationException(SR.Format(SR.XmlSerializableMissingClrType, name, ns, typeof(XmlIncludeAttribute).Name, clrType));
            //XmlSerializableMissingClrType= Type '{0}' from namespace '{1}' doesnot have corresponding IXmlSerializable type. Please consider adding {2} to '{3}'.
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.EnsureArrayIndex"]/*' />
        protected Array EnsureArrayIndex(Array a, int index, Type elementType)
        {
            if (a == null) return Array.CreateInstance(elementType, 32);
            if (index < a.Length) return a;
            Array b = Array.CreateInstance(elementType, a.Length * 2);
            Array.Copy(a, b, index);
            return b;
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ShrinkArray"]/*' />
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

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadString"]/*' />
        protected string ReadString(string value)
        {
            return ReadString(value, false);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadString1"]/*' />
        protected string ReadString(string value, bool trim)
        {
            string str = _r.ReadString();
            if (str != null && trim)
                str = str.Trim();
            if (value == null || value.Length == 0)
                return str;
            return value + str;
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadSerializable"]/*' />
        protected IXmlSerializable ReadSerializable(IXmlSerializable serializable)
        {
            return ReadSerializable(serializable, false);
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadSerializable"]/*' />
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













        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.InitCallbacks"]/*' />
        protected abstract void InitCallbacks();








        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.ReadEndElement"]/*' />
        protected void ReadEndElement()
        {
            while (_r.NodeType == XmlNodeType.Whitespace) _r.Skip();
            if (_r.NodeType == XmlNodeType.None) _r.Skip();
            else _r.ReadEndElement();
        }

        private object ReadXmlNodes(bool elementCanBeType)
        {
            string elemLocalName = Reader.LocalName;
            string elemNs = Reader.NamespaceURI;
            string xsiTypeName = null;
            string xsiTypeNs = null;
            int skippableNodeCount = 0;

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
                // To support new Object().
                // <anyType xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" />
                skippableNodeCount--;
            }

            // If the node is referenced (or in case of paramStyle = bare) and if xsi:type is not
            // specified then the element name is used as the type name. Reveal this to the user
            // by adding an extra attribute node "xsi:type" with value as the element name.
            if (elementCanBeType && xsiTypeName == null)
            {
                xsiTypeName = elemLocalName;
                xsiTypeNs = elemNs;
                skippableNodeCount--;
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
                throw CodeGenerator.NotSupported("XLinq");
            }


            if (skippableNodeCount >= 0)
                return new object();

            throw CodeGenerator.NotSupported("XLinq");
        }

        /// <include file='doc\XmlSerializationReader.uex' path='docs/doc[@for="XmlSerializationReader.CheckReaderCount"]/*' />
        protected void CheckReaderCount(ref int whileIterations, ref int readerCount)
        {
        }
    }
}
