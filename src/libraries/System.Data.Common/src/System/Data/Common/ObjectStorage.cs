// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Dynamic;

namespace System.Data.Common
{
    internal sealed class ObjectStorage : DataStorage
    {
        private static readonly object s_defaultValue = null;

        private enum Families { DATETIME, NUMBER, STRING, BOOLEAN, ARRAY };

        private object[] _values;
        private readonly bool _implementsIXmlSerializable;

        internal ObjectStorage(DataColumn column, Type type)
        : base(column, type, s_defaultValue, DBNull.Value, typeof(ICloneable).IsAssignableFrom(type), GetStorageType(type))
        {
            _implementsIXmlSerializable = typeof(IXmlSerializable).IsAssignableFrom(type);
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            object valueNo1 = _values[recordNo1];
            object valueNo2 = _values[recordNo2];

            if (valueNo1 == valueNo2)
                return 0;
            if (valueNo1 == null)
                return -1;
            if (valueNo2 == null)
                return 1;

            IComparable icomparable = (valueNo1 as IComparable);
            if (null != icomparable)
            {
                try
                {
                    return icomparable.CompareTo(valueNo2);
                }
                catch (ArgumentException e)
                {
                    ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                }
            }
            return CompareWithFamilies(valueNo1, valueNo2);
        }

        public override int CompareValueTo(int recordNo1, object value)
        {
            object valueNo1 = Get(recordNo1);

            if (valueNo1 is IComparable)
            {
                if (value.GetType() == valueNo1.GetType())
                    return ((IComparable)valueNo1).CompareTo(value);
            }

            if (valueNo1 == value)
                return 0;

            if (valueNo1 == null)
            {
                if (_nullValue == value)
                {
                    return 0;
                }
                return -1;
            }
            if ((_nullValue == value) || (null == value))
            {
                return 1;
            }

            return CompareWithFamilies(valueNo1, value);
        }


        private int CompareTo(object valueNo1, object valueNo2)
        {
            if (valueNo1 == null)
                return -1;
            if (valueNo2 == null)
                return 1;
            if (valueNo1 == valueNo2)
                return 0;
            if (valueNo1 == _nullValue)
                return -1;
            if (valueNo2 == _nullValue)
                return 1;

            if (valueNo1 is IComparable)
            {
                try
                {
                    return ((IComparable)valueNo1).CompareTo(valueNo2);
                }
                catch (ArgumentException e)
                {
                    ExceptionBuilder.TraceExceptionWithoutRethrow(e);
                }
            }
            return CompareWithFamilies(valueNo1, valueNo2);
        }

        private int CompareWithFamilies(object valueNo1, object valueNo2)
        {
            Families Family1 = GetFamily(valueNo1.GetType());
            Families Family2 = GetFamily(valueNo2.GetType());
            if (Family1 < Family2)
                return -1;
            else
                if (Family1 > Family2)
                return 1;
            else
            {
                switch (Family1)
                {
                    case Families.BOOLEAN:
                        valueNo1 = Convert.ToBoolean(valueNo1, FormatProvider);
                        valueNo2 = Convert.ToBoolean(valueNo2, FormatProvider);
                        break;
                    case Families.DATETIME:
                        valueNo1 = Convert.ToDateTime(valueNo1, FormatProvider);
                        valueNo2 = Convert.ToDateTime(valueNo1, FormatProvider);
                        break;
                    case Families.NUMBER:
                        valueNo1 = Convert.ToDouble(valueNo1, FormatProvider);
                        valueNo2 = Convert.ToDouble(valueNo2, FormatProvider);
                        break;
                    case Families.ARRAY:
                        {
                            Array arr1 = (Array)valueNo1;
                            Array arr2 = (Array)valueNo2;
                            if (arr1.Length > arr2.Length)
                                return 1;
                            else if (arr1.Length < arr2.Length)
                                return -1;
                            else
                            { // same number of elements
                                for (int i = 0; i < arr1.Length; i++)
                                {
                                    int c = CompareTo(arr1.GetValue(i), arr2.GetValue(i));
                                    if (c != 0)
                                        return c;
                                }
                            }
                            return 0;
                        }
                    default:
                        valueNo1 = valueNo1.ToString();
                        valueNo2 = valueNo2.ToString();
                        break;
                }
                return ((IComparable)valueNo1).CompareTo(valueNo2);
            }
        }

        public override void Copy(int recordNo1, int recordNo2)
        {
            _values[recordNo2] = _values[recordNo1];
        }

        public override object Get(int recordNo)
        {
            object value = _values[recordNo];
            if (null != value)
            {
                return value;
            }
            return _nullValue;
        }

        private Families GetFamily(Type dataType)
        {
            switch (Type.GetTypeCode(dataType))
            {
                case TypeCode.Boolean: return Families.BOOLEAN;
                case TypeCode.Char: return Families.STRING;
                case TypeCode.SByte: return Families.STRING;
                case TypeCode.Byte: return Families.STRING;
                case TypeCode.Int16: return Families.NUMBER;
                case TypeCode.UInt16: return Families.NUMBER;
                case TypeCode.Int32: return Families.NUMBER;
                case TypeCode.UInt32: return Families.NUMBER;
                case TypeCode.Int64: return Families.NUMBER;
                case TypeCode.UInt64: return Families.NUMBER;
                case TypeCode.Single: return Families.NUMBER;
                case TypeCode.Double: return Families.NUMBER;
                case TypeCode.Decimal: return Families.NUMBER;
                case TypeCode.DateTime: return Families.DATETIME;
                case TypeCode.String: return Families.STRING;
                default:
                    if (typeof(TimeSpan) == dataType)
                    {
                        return Families.DATETIME;
                    }
                    else if (dataType.IsArray)
                    {
                        return Families.ARRAY;
                    }
                    else
                    {
                        return Families.STRING;
                    }
            }
        }

        public override bool IsNull(int record)
        {
            return (null == _values[record]);
        }

        public override void Set(int recordNo, object value)
        {
            Debug.Assert(null != value, "null value");
            if (_nullValue == value)
            {
                _values[recordNo] = null;
            }
            else if (_dataType == typeof(object) || _dataType.IsInstanceOfType(value))
            {
                _values[recordNo] = value;
            }
            else
            {
                Type valType = value.GetType();
                if (_dataType == typeof(Guid) && valType == typeof(string))
                {
                    _values[recordNo] = new Guid((string)value);
                }
                else if (_dataType == typeof(byte[]))
                {
                    if (valType == typeof(bool))
                    {
                        _values[recordNo] = BitConverter.GetBytes((bool)value);
                    }
                    else if (valType == typeof(char))
                    {
                        _values[recordNo] = BitConverter.GetBytes((char)value);
                    }
                    else if (valType == typeof(short))
                    {
                        _values[recordNo] = BitConverter.GetBytes((short)value);
                    }
                    else if (valType == typeof(int))
                    {
                        _values[recordNo] = BitConverter.GetBytes((int)value);
                    }
                    else if (valType == typeof(long))
                    {
                        _values[recordNo] = BitConverter.GetBytes((long)value);
                    }
                    else if (valType == typeof(ushort))
                    {
                        _values[recordNo] = BitConverter.GetBytes((ushort)value);
                    }
                    else if (valType == typeof(uint))
                    {
                        _values[recordNo] = BitConverter.GetBytes((uint)value);
                    }
                    else if (valType == typeof(ulong))
                    {
                        _values[recordNo] = BitConverter.GetBytes((ulong)value);
                    }
                    else if (valType == typeof(float))
                    {
                        _values[recordNo] = BitConverter.GetBytes((float)value);
                    }
                    else if (valType == typeof(double))
                    {
                        _values[recordNo] = BitConverter.GetBytes((double)value);
                    }
                    else
                    {
                        throw ExceptionBuilder.StorageSetFailed();
                    }
                }
                else
                {
                    throw ExceptionBuilder.StorageSetFailed();
                }
            }
        }

        public override void SetCapacity(int capacity)
        {
            object[] newValues = new object[capacity];
            if (_values != null)
            {
                Array.Copy(_values, 0, newValues, 0, Math.Min(capacity, _values.Length));
            }
            _values = newValues;
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object ConvertXmlToObject(string s)
        {
            Type type = _dataType; // real type of objects in this column

            if (type == typeof(byte[]))
            {
                return Convert.FromBase64String(s);
            }
            if (type == typeof(Type))
            {
                return Type.GetType(s);
            }
            if (type == typeof(Guid))
            {
                return (new Guid(s));
            }
            if (type == typeof(Uri))
            {
                return (new Uri(s));
            }


            if (_implementsIXmlSerializable)
            {
                object Obj = System.Activator.CreateInstance(_dataType, true);
                StringReader strReader = new StringReader(s);
                using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
                {
                    ((IXmlSerializable)Obj).ReadXml(xmlTextReader);
                }
                return Obj;
            }

            StringReader strreader = new StringReader(s);
            XmlSerializer deserializerWithOutRootAttribute = ObjectStorage.GetXmlSerializer(type);
            return (deserializerWithOutRootAttribute.Deserialize(strreader));
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
        {
            object retValue = null;
            bool isBaseCLRType = false;
            bool legacyUDT = false; // in 1.0 and 1.1 we used to call ToString on CDT obj. so if we have the same case
            // we need to handle the case when we have column type as object.
            if (null == xmlAttrib)
            { // this means type implements IXmlSerializable
                Type type = null;
                string typeName = xmlReader.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS);
                if (typeName == null || typeName.Length == 0)
                { // No CDT polumorphism
                    string xsdTypeName = xmlReader.GetAttribute(Keywords.TYPE, Keywords.XSINS); // this xsd type: Base type polymorphism
                    if (null != xsdTypeName && xsdTypeName.Length > 0)
                    {
                        string[] _typename = xsdTypeName.Split(':');
                        if (_typename.Length == 2)
                        { // split will return aray of size 1 if ":" is not there
                            if (xmlReader.LookupNamespace(_typename[0]) == Keywords.XSDNS)
                            {
                                xsdTypeName = _typename[1]; // trim the prefix and just continue with
                            }
                        } // for other case, let say we have two ':' in type, the we throws (as old behavior)
                        type = XSDSchema.XsdtoClr(xsdTypeName);
                        isBaseCLRType = true;
                    }
                    else if (_dataType == typeof(object))
                    {// there is no Keywords.MSD_INSTANCETYPE and no Keywords.TYPE
                        legacyUDT = true;             // see if our type is object
                    }
                }

                if (legacyUDT)
                { // if Everett UDT, just read it and return string
                    retValue = xmlReader.ReadString();
                }
                else
                {
                    if (typeName == Keywords.TYPEINSTANCE)
                    {
                        retValue = Type.GetType(xmlReader.ReadString());
                        xmlReader.Read(); // need to move to next node
                    }
                    else
                    {
                        if (null == type)
                        {
                            type = (typeName == null) ? _dataType : DataStorage.GetType(typeName);
                        }

                        if (type == typeof(char) || type == typeof(Guid))
                        { //msdata:char and msdata:guid imply base types.
                            isBaseCLRType = true;
                        }

                        if (type == typeof(object))
                            throw ExceptionBuilder.CanNotDeserializeObjectType();
                        if (!isBaseCLRType)
                        {
                            retValue = System.Activator.CreateInstance(type, true);
                            Debug.Assert(xmlReader is DataTextReader, "Invalid DataTextReader is being passed to customer");
                            ((IXmlSerializable)retValue).ReadXml(xmlReader);
                        }
                        else
                        {  // Process Base CLR type
                           // for Element Node, if it is Empty, ReadString does not move to End Element; we need to move it
                            if (type == typeof(string) && xmlReader.NodeType == XmlNodeType.Element && xmlReader.IsEmptyElement)
                            {
                                retValue = string.Empty;
                            }
                            else
                            {
                                retValue = xmlReader.ReadString();
                                if (type != typeof(byte[]))
                                {
                                    retValue = SqlConvert.ChangeTypeForXML(retValue, type);
                                }
                                else
                                {
                                    retValue = Convert.FromBase64String(retValue.ToString());
                                }
                            }
                            xmlReader.Read();
                        }
                    }
                }
            }
            else
            {
                XmlSerializer deserializerWithRootAttribute = ObjectStorage.GetXmlSerializer(_dataType, xmlAttrib);
                retValue = deserializerWithRootAttribute.Deserialize(xmlReader);
            }
            return retValue;
        }

        public override string ConvertObjectToXml(object value)
        {
            if ((value == null) || (value == _nullValue))// this case won't happen,  this is added in case if code in xml saver changes
                return string.Empty;

            Type type = _dataType;
            if (type == typeof(byte[]) || (type == typeof(object) && (value is byte[])))
            {
                return Convert.ToBase64String((byte[])value);
            }
            if ((type == typeof(Type)) || ((type == typeof(object)) && (value is Type)))
            {
                return ((Type)value).AssemblyQualifiedName;
            }

            if (!IsTypeCustomType(value.GetType()))
            { // Guid and Type had TypeCode.Object
                return (string)SqlConvert.ChangeTypeForXML(value, typeof(string));
            }

            if (Type.GetTypeCode(value.GetType()) != TypeCode.Object)
            {
                return value.ToString();
            }

            StringWriter strwriter = new StringWriter(FormatProvider);
            if (_implementsIXmlSerializable)
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
                {
                    ((IXmlSerializable)value).WriteXml(xmlTextWriter);
                }
                return (strwriter.ToString());
            }

            XmlSerializer serializerWithOutRootAttribute = ObjectStorage.GetXmlSerializer(value.GetType());
            serializerWithOutRootAttribute.Serialize(strwriter, value);
            return strwriter.ToString();
        }

        public override void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib)
        {
            if (null == xmlAttrib)
            { // implements IXmlSerializable
                Debug.Assert(xmlWriter is DataTextWriter, "Invalid DataTextWriter is being passed to customer");
                ((IXmlSerializable)value).WriteXml(xmlWriter);
            }
            else
            {
                XmlSerializer serializerWithRootAttribute = ObjectStorage.GetXmlSerializer(value.GetType(), xmlAttrib);
                serializerWithRootAttribute.Serialize(xmlWriter, value);
            }
        }

        protected override object GetEmptyStorage(int recordCount)
        {
            return new object[recordCount];
        }

        protected override void CopyValue(int record, object store, BitArray nullbits, int storeIndex)
        {
            object[] typedStore = (object[])store;
            typedStore[storeIndex] = _values[record];
            bool isNull = IsNull(record);
            nullbits.Set(storeIndex, isNull);

            if (!isNull && (typedStore[storeIndex] is DateTime))
            {
                DateTime dt = (DateTime)typedStore[storeIndex];
                if (dt.Kind == DateTimeKind.Local)
                {
                    typedStore[storeIndex] = DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Local);
                }
            }
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (object[])store;
            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] is DateTime)
                {
                    DateTime dt = (DateTime)_values[i];
                    if (dt.Kind == DateTimeKind.Local)
                    {
                        _values[i] = (DateTime.SpecifyKind(dt, DateTimeKind.Utc)).ToLocalTime();
                    }
                }
            }
        }

        private static readonly object s_tempAssemblyCacheLock = new object();
        private static Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> s_tempAssemblyCache;
        private static readonly XmlSerializerFactory s_serializerFactory = new XmlSerializerFactory();

        /// <summary>
        /// throw an InvalidOperationException if type implements IDynamicMetaObjectProvider and not IXmlSerializable
        /// because XmlSerializerFactory will only serialize the type's declared properties, not its dynamic properties
        /// </summary>
        /// <param name="type">type to test for IDynamicMetaObjectProvider</param>
        /// <exception cref="InvalidOperationException">DataSet will not serialize types that implement IDynamicMetaObjectProvider but do not also implement IXmlSerializable</exception>
        /// <remarks>IDynamicMetaObjectProvider was introduced in .NET Framework V4.0 into System.Core</remarks>
        internal static void VerifyIDynamicMetaObjectProvider(Type type)
        {
            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(type) &&
               !typeof(IXmlSerializable).IsAssignableFrom(type))
            {
                throw ADP.InvalidOperation(SR.Xml_DynamicWithoutXmlSerializable);
            }
        }

        internal static XmlSerializer GetXmlSerializer(Type type)
        {
            // prevent writing an instance which implements IDynamicMetaObjectProvider and not IXmlSerializable
            // the check here prevents the instance data from being written
            VerifyIDynamicMetaObjectProvider(type);

            // use factory which caches XmlSerializer as necessary
            XmlSerializer serializer = s_serializerFactory.CreateSerializer(type);
            return serializer;
        }

        internal static XmlSerializer GetXmlSerializer(Type type, XmlRootAttribute attribute)
        {
            XmlSerializer serializer = null;
            KeyValuePair<Type, XmlRootAttribute> key = new KeyValuePair<Type, XmlRootAttribute>(type, attribute);

            // _tempAssemblyCache is a readonly instance, lock on write to copy & add then replace the original instance.
            Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> cache = s_tempAssemblyCache;
            if ((null == cache) || !cache.TryGetValue(key, out serializer))
            {   // not in cache, try again with lock because it may need to grow
                lock (s_tempAssemblyCacheLock)
                {
                    cache = s_tempAssemblyCache;
                    if ((null == cache) || !cache.TryGetValue(key, out serializer))
                    {
                        // prevent writing an instance which implements IDynamicMetaObjectProvider and not IXmlSerializable
                        // the check here prevents the instance data from being written
                        VerifyIDynamicMetaObjectProvider(type);

                        // if still not in cache, make cache larger and add new XmlSerializer
                        if (null != cache)
                        {   // create larger cache, because dictionary is not reader/writer safe
                            // copy cache so that all readers don't take lock - only potential new writers
                            // same logic used by DbConnectionFactory
                            Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> tmp =
                                new Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer>(
                                    1 + cache.Count, TempAssemblyComparer.s_default);
                            foreach (KeyValuePair<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer> entry in cache)
                            {   // copy contents from old cache to new cache
                                tmp.Add(entry.Key, entry.Value);
                            }
                            cache = tmp;
                        }
                        else
                        {   // initial creation of cache
                            cache = new Dictionary<KeyValuePair<Type, XmlRootAttribute>, XmlSerializer>(
                                TempAssemblyComparer.s_default);
                        }

                        // attribute is modifiable - but usuage from XmlSaver & XmlDataLoader & XmlDiffLoader 
                        // the instances are not modified, but to be safe - copy the XmlRootAttribute before caching
                        key = new KeyValuePair<Type, XmlRootAttribute>(type, new XmlRootAttribute());
                        key.Value.ElementName = attribute.ElementName;
                        key.Value.Namespace = attribute.Namespace;
                        key.Value.DataType = attribute.DataType;
                        key.Value.IsNullable = attribute.IsNullable;

                        serializer = s_serializerFactory.CreateSerializer(type, attribute);
                        cache.Add(key, serializer);
                        s_tempAssemblyCache = cache;
                    }
                }
            }
            return serializer;
        }

        private class TempAssemblyComparer : IEqualityComparer<KeyValuePair<Type, XmlRootAttribute>>
        {
            internal static readonly IEqualityComparer<KeyValuePair<Type, XmlRootAttribute>> s_default = new TempAssemblyComparer();

            private TempAssemblyComparer() { }

            public bool Equals(KeyValuePair<Type, XmlRootAttribute> x, KeyValuePair<Type, XmlRootAttribute> y)
            {
                return ((x.Key == y.Key) &&                                         // same type
                        (((x.Value == null) && (y.Value == null)) ||                // XmlRootAttribute both null
                         ((x.Value != null) && (y.Value != null) &&                 // XmlRootAttribute both with value
                          (x.Value.ElementName == y.Value.ElementName) &&           // all attribute elements are equal
                          (x.Value.Namespace == y.Value.Namespace) &&
                          (x.Value.DataType == y.Value.DataType) &&
                          (x.Value.IsNullable == y.Value.IsNullable))));
            }

            public int GetHashCode(KeyValuePair<Type, XmlRootAttribute> obj)
            {
                return unchecked(obj.Key.GetHashCode() + obj.Value.ElementName.GetHashCode());
            }
        }
    }
}

