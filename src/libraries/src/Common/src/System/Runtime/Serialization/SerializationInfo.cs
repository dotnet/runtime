// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

// TODO: Put back asserts once Debug.Assert is available

namespace System.Runtime.Serialization
{
    public sealed class SerializationInfo
    {
        private const int DefaultSize = 4;

        // Even though we have a dictionary, we're still keeping all the arrays around for back-compat. 
        // Otherwise we may run into potentially breaking behaviors like GetEnumerator() not returning entries in the same order they were added.
        private string[] _names;
        private object[] _values;
        private Type[] _types;
        private int _count;
        private Dictionary<string, int> _nameToIndex;

        private IFormatterConverter _converter;
        private string _rootTypeName;
        private string _rootTypeAssemblyName;
        private Type _rootType;

        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            _rootType = type;
            _rootTypeName = type.FullName;
            _rootTypeAssemblyName = type.GetTypeInfo().Module.Assembly.FullName;

            _names = new string[DefaultSize];
            _values = new object[DefaultSize];
            _types = new Type[DefaultSize];
            _nameToIndex = new Dictionary<string, int>();
            _converter = converter;
        }

        public string FullTypeName
        {
            get { return _rootTypeName; }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _rootTypeName = value;
                IsFullTypeNameSetExplicit = true;
            }
        }

        public string AssemblyName
        {
            get { return _rootTypeAssemblyName; }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _rootTypeAssemblyName = value;
                IsAssemblyNameSetExplicit = true;
            }
        }

        public bool IsFullTypeNameSetExplicit { get; private set; }

        public bool IsAssemblyNameSetExplicit { get; private set; }

        public void SetType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!ReferenceEquals(_rootType, type))
            {
                _rootType = type;
                _rootTypeName = type.FullName;
                _rootTypeAssemblyName = type.GetTypeInfo().Module.Assembly.FullName;
                IsFullTypeNameSetExplicit = false;
                IsAssemblyNameSetExplicit = false;
            }
        }

        public int MemberCount => _count;

        public Type ObjectType => _rootType;

        public SerializationInfoEnumerator GetEnumerator() => new SerializationInfoEnumerator(_names, _values, _types, _count);

        private void ExpandArrays()
        {
            int newSize;
            // Debug.Assert(_names.Length == _count);

            newSize = (_count * 2);

            // In the pathological case, we may wrap
            if (newSize < _count)
            {
                if (int.MaxValue > _count)
                {
                    newSize = int.MaxValue;
                }
            }

            // Allocate more space and copy the data

            string[] newMembers = new string[newSize];
            Array.Copy(_names, 0, newMembers, 0, _count);
            _names = newMembers;

            object[] newData = new object[newSize];
            Array.Copy(_values, 0, newData, 0, _count);
            _values = newData;

            Type[] newTypes = new Type[newSize];
            Array.Copy(_types, 0, newTypes, 0, _count);
            _types = newTypes;
        }

        internal void UpdateValue(string name, object value, Type type)
        {
            //Debug.Assert(null != name, "[SerializationInfo.UpdateValue]name!=null");
            //Debug.Assert(null != value, "[SerializationInfo.UpdateValue]value!=null");
            //Debug.Assert(null != type, "[SerializationInfo.UpdateValue]type!=null");

            int index = FindElement(name);
            if (index < 0)
            {
                AddValueInternal(name, value, type);
            }
            else
            {
                _values[index] = value;
                _types[index] = type;
            }
        }

        public void AddValue(string name, object value, Type type)
        {
            if (null == name)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            AddValueInternal(name, value, type);
        }

        public void AddValue(string name, object value)
        {
            if (null == value)
            {
                AddValue(name, value, typeof(object));
            }
            else
            {
                AddValue(name, value, value.GetType());
            }
        }

        public void AddValue(string name, bool value)
        {
            AddValue(name, value, typeof(bool));
        }

        public void AddValue(string name, char value)
        {
            AddValue(name, value, typeof(char));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, sbyte value)
        {
            AddValue(name, value, typeof(sbyte));
        }

        public void AddValue(string name, byte value)
        {
            AddValue(name, value, typeof(byte));
        }

        public void AddValue(string name, short value)
        {
            AddValue(name, value, typeof(short));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, ushort value)
        {
            AddValue(name, value, typeof(ushort));
        }

        public void AddValue(string name, int value)
        {
            AddValue(name, value, typeof(int));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, uint value)
        {
            AddValue(name, value, typeof(uint));
        }

        public void AddValue(string name, long value)
        {
            AddValue(name, value, typeof(long));
        }

        [CLSCompliant(false)]
        public void AddValue(string name, ulong value)
        {
            AddValue(name, value, typeof(ulong));
        }

        public void AddValue(string name, float value)
        {
            AddValue(name, value, typeof(float));
        }

        public void AddValue(string name, double value)
        {
            AddValue(name, value, typeof(double));
        }

        public void AddValue(string name, decimal value)
        {
            AddValue(name, value, typeof(decimal));
        }

        public void AddValue(string name, DateTime value)
        {
            AddValue(name, value, typeof(DateTime));
        }

        internal void AddValueInternal(string name, object value, Type type)
        {
            if (_nameToIndex.ContainsKey(name))
            {
                throw new SerializationException("Cannot add the same member twice to a SerializationInfo object."); // TODO: SR.Serialization_SameNameTwice);
            }
            _nameToIndex.Add(name, _count);

            // If we need to expand the arrays, do so.
            if (_count >= _names.Length)
            {
                ExpandArrays();
            }

            // Add the data and then advance the counter.
            _names[_count] = name;
            _values[_count] = value;
            _types[_count] = type;
            _count++;
        }

        private int FindElement(string name)
        {
            if (null == name)
            {
                throw new ArgumentNullException(nameof(name));
            }

            int index;
            return _nameToIndex.TryGetValue(name, out index) ? index : -1;
        }

        private object GetElement(string name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                throw new SerializationException(string.Format("Member '{0}' was not found.", name)); // TODO: SR.Format(SR.Serialization_NotFound, name));
            }

            //Debug.Assert(index < _values.Length);
            //Debug.Assert(index < _types.Length);

            foundType = _types[index];
            //Debug.Assert(foundType != null);
            return _values[index];
        }

        private object GetElementNoThrow(string name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                foundType = null;
                return null;
            }

            //Debug.Assert(index < _values.Length, "[SerializationInfo.GetElement]index<m_data.Length");
            //Debug.Assert(index < _types.Length, "[SerializationInfo.GetElement]index<m_types.Length");

            foundType = _types[index];
            //Debug.Assert(foundType != null, "[SerializationInfo.GetElement]foundType!=null");
            return _values[index];
        }

        public object GetValue(string name, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, type) || type.GetTypeInfo().IsAssignableFrom(foundType.GetTypeInfo()) || value == null)
            {
                return value;
            }

            //Debug.Assert(_converter != null);
            return _converter.Convert(value, type);
        }

        internal object GetValueNoThrow(string name, Type type)
        {
            //Debug.Assert(type != null, "[SerializationInfo.GetValue]type ==null");

            Type foundType;
            object value = GetElementNoThrow(name, out foundType);
            if (value == null)
                return null;

            if (ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
            {
                return value;
            }

            //Debug.Assert(_converter != null, "[SerializationInfo.GetValue]m_converter!=null");
            return _converter.Convert(value, type);
        }

        public bool GetBoolean(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(bool)) ? (bool)value : _converter.ToBoolean(value);
        }

        public char GetChar(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(char)) ? (char)value : _converter.ToChar(value);
        }

        [CLSCompliant(false)]
        public sbyte GetSByte(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(sbyte)) ? (sbyte)value : _converter.ToSByte(value);
        }

        public byte GetByte(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(byte)) ? (byte)value : _converter.ToByte(value);
        }

        public short GetInt16(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(short)) ? (short)value : _converter.ToInt16(value);
        }

        [CLSCompliant(false)]
        public ushort GetUInt16(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(ushort)) ? (ushort)value : _converter.ToUInt16(value);
        }

        public int GetInt32(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(int)) ? (int)value : _converter.ToInt32(value);
        }

        [CLSCompliant(false)]
        public uint GetUInt32(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(uint)) ? (uint)value : _converter.ToUInt32(value);
        }

        public long GetInt64(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(long)) ? (long)value : _converter.ToInt64(value);
        }

        [CLSCompliant(false)]
        public ulong GetUInt64(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(ulong)) ? (ulong)value : _converter.ToUInt64(value);
        }

        public float GetSingle(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(float)) ? (float)value : _converter.ToSingle(value);
        }


        public double GetDouble(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(double)) ? (double)value : _converter.ToDouble(value);
        }

        public decimal GetDecimal(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(decimal)) ? (decimal)value : _converter.ToDecimal(value);
        }

        public DateTime GetDateTime(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(DateTime)) ? (DateTime)value : _converter.ToDateTime(value);
        }

        public string GetString(string name)
        {
            Type foundType;
            object value = GetElement(name, out foundType);
            return ReferenceEquals(foundType, typeof(string)) || value == null ? (string)value : _converter.ToString(value);
        }
    }
}
