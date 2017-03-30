// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Data.Common
{
    internal sealed class SqlUdtStorage : DataStorage
    {
        private object[] _values;
        private readonly bool _implementsIXmlSerializable = false;
        private readonly bool _implementsIComparable = false;

        private static readonly Dictionary<Type, object> s_typeToNull = new Dictionary<Type, object>();

        public SqlUdtStorage(DataColumn column, Type type)
        : this(column, type, GetStaticNullForUdtType(type))
        {
        }

        private SqlUdtStorage(DataColumn column, Type type, object nullValue)
        : base(column, type, nullValue, nullValue, typeof(ICloneable).IsAssignableFrom(type), GetStorageType(type))
        {
            _implementsIXmlSerializable = typeof(IXmlSerializable).IsAssignableFrom(type);
            _implementsIComparable = typeof(IComparable).IsAssignableFrom(type);
        }

        // to support oracle types and other INUllable types that have static Null as field
        internal static object GetStaticNullForUdtType(Type type)
        {
            object value;
            if (!s_typeToNull.TryGetValue(type, out value))
            {
                System.Reflection.PropertyInfo propInfo = type.GetProperty("Null", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (propInfo != null)
                    value = propInfo.GetValue(null, null);
                else
                {
                    System.Reflection.FieldInfo fieldInfo = type.GetField("Null", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (fieldInfo != null)
                    {
                        value = fieldInfo.GetValue(null);
                    }
                    else
                    {
                        throw ExceptionBuilder.INullableUDTwithoutStaticNull(type.AssemblyQualifiedName);
                    }
                }
                lock (s_typeToNull)
                {
                    //if(50 < TypeToNull.Count) {
                    //    TypeToNull.Clear();
                    //}
                    s_typeToNull[type] = value;
                }
            }
            return value;
        }

        public override bool IsNull(int record)
        {
            return (((INullable)_values[record]).IsNull);
        }

        public override object Aggregate(int[] records, AggregateType kind)
        {
            throw ExceptionBuilder.AggregateException(kind, _dataType);
        }

        public override int Compare(int recordNo1, int recordNo2)
        {
            return (CompareValueTo(recordNo1, _values[recordNo2]));
        }

        public override int CompareValueTo(int recordNo1, object value)
        {
            if (DBNull.Value == value)
            {
                // it is not meaningful compare UDT with DBNull.Value
                value = _nullValue;
            }
            if (_implementsIComparable)
            {
                IComparable comparable = (IComparable)_values[recordNo1];
                return comparable.CompareTo(value);
            }
            else if (_nullValue == value)
            {
                INullable nullableValue = (INullable)_values[recordNo1];
                return nullableValue.IsNull ? 0 : 1; // left may be null, right is null
            }

            throw ExceptionBuilder.IComparableNotImplemented(_dataType.AssemblyQualifiedName);
        }

        public override void Copy(int recordNo1, int recordNo2)
        {
            CopyBits(recordNo1, recordNo2);
            _values[recordNo2] = _values[recordNo1];
        }

        public override object Get(int recordNo)
        {
            return (_values[recordNo]);
        }

        public override void Set(int recordNo, object value)
        {
            if (DBNull.Value == value)
            {
                _values[recordNo] = _nullValue;
                SetNullBit(recordNo, true);
            }
            else if (null == value)
            {
                if (_isValueType)
                {
                    throw ExceptionBuilder.StorageSetFailed();
                }
                else
                {
                    _values[recordNo] = _nullValue;
                    SetNullBit(recordNo, true);
                }
            }
            else if (!_dataType.IsInstanceOfType(value))
            {
                throw ExceptionBuilder.StorageSetFailed();
            }
            else
            {
                // do not clone the value
                _values[recordNo] = value;
                SetNullBit(recordNo, false);
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
            base.SetCapacity(capacity);
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object ConvertXmlToObject(string s)
        {
            if (_implementsIXmlSerializable)
            {
                object Obj = System.Activator.CreateInstance(_dataType, true);

                string tempStr = string.Concat("<col>", s, "</col>"); // this is done since you can give fragmet to reader
                StringReader strReader = new StringReader(tempStr);

                using (XmlTextReader xmlTextReader = new XmlTextReader(strReader))
                {
                    ((IXmlSerializable)Obj).ReadXml(xmlTextReader);
                }
                return Obj;
            }

            StringReader strreader = new StringReader(s);
            XmlSerializer deserializerWithOutRootAttribute = ObjectStorage.GetXmlSerializer(_dataType);
            return (deserializerWithOutRootAttribute.Deserialize(strreader));
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute xmlAttrib)
        {
            if (null == xmlAttrib)
            {
                string typeName = xmlReader.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.MSDNS);
                if (typeName == null)
                {
                    string xsdTypeName = xmlReader.GetAttribute(Keywords.MSD_INSTANCETYPE, Keywords.XSINS); // this xsd type
                    if (null != xsdTypeName)
                    {
                        typeName = XSDSchema.XsdtoClr(xsdTypeName).FullName;
                    }
                }
                Type type = (typeName == null) ? _dataType : Type.GetType(typeName);
                object Obj = System.Activator.CreateInstance(type, true);
                Debug.Assert(xmlReader is DataTextReader, "Invalid DataTextReader is being passed to customer");
                ((IXmlSerializable)Obj).ReadXml(xmlReader);
                return Obj;
            }
            else
            {
                XmlSerializer deserializerWithRootAttribute = ObjectStorage.GetXmlSerializer(_dataType, xmlAttrib);
                return (deserializerWithRootAttribute.Deserialize(xmlReader));
            }
        }


        public override string ConvertObjectToXml(object value)
        {
            StringWriter strwriter = new StringWriter(FormatProvider);
            if (_implementsIXmlSerializable)
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(strwriter))
                {
                    ((IXmlSerializable)value).WriteXml(xmlTextWriter);
                }
            }
            else
            {
                XmlSerializer serializerWithOutRootAttribute = ObjectStorage.GetXmlSerializer(value.GetType());
                serializerWithOutRootAttribute.Serialize(strwriter, value);
            }
            return (strwriter.ToString());
        }

        public override void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute xmlAttrib)
        {
            if (null == xmlAttrib)
            {
                Debug.Assert(xmlWriter is DataTextWriter, "Invalid DataTextWriter is being passed to customer");
                ((IXmlSerializable)value).WriteXml(xmlWriter);
            }
            else
            {
                // we support polymorphism only for types that implements IXmlSerializable.
                // Assumption: value is the same type as DataType

                XmlSerializer serializerWithRootAttribute = ObjectStorage.GetXmlSerializer(_dataType, xmlAttrib);
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
            nullbits.Set(storeIndex, IsNull(record));
        }

        protected override void SetStorage(object store, BitArray nullbits)
        {
            _values = (object[])store;
            //SetNullStorage(nullbits);
        }
    }
}
