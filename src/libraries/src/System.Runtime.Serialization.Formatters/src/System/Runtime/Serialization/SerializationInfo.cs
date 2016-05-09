// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.Serialization
{
    public sealed class SerializationInfo
    {
        private const int defaultSize = 4;

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
        internal bool IsFullTypeNameSetExplicit;
        internal bool IsAssemblyNameSetExplicit;

        [CLSCompliant(false)]
        public SerializationInfo(Type type, IFormatterConverter converter)
        {
            if ((object)type == null)
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

            _names = new String[defaultSize];
            _values = new object[defaultSize];
            _types = new Type[defaultSize];

            _nameToIndex = new Dictionary<string, int>();

            _converter = converter;
        }

        public string FullTypeName
        {
            get
            {
                return _rootTypeName;
            }
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
            get
            {
                return _rootTypeAssemblyName;
            }
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

        public int MemberCount
        {
            get
            {
                return _count;
            }
        }

        public Type ObjectType
        {
            get
            {
                return _rootType;
            }
        }

        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(_names, _values, _types, _count);
        }

        private void ExpandArrays()
        {
            int newSize;
            Debug.Assert(_names.Length == _count);

            newSize = (_count * 2);

            //
            // In the pathological case, we may wrap
            //
            if (newSize < _count)
            {
                if (int.MaxValue > _count)
                {
                    newSize = int.MaxValue;
                }
            }

            //
            // Allocate more space and copy the data
            //
            string[] newMembers = new string[newSize];
            object[] newData = new object[newSize];
            Type[] newTypes = new Type[newSize];

            Array.Copy(_names, newMembers, _count);
            Array.Copy(_values, newData, _count);
            Array.Copy(_types, newTypes, _count);

            //
            // Assign the new arrys back to the member vars.
            //
            _names = newMembers;
            _values = newData;
            _types = newTypes;
        }

        #region AddValue
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
        #endregion

        internal void AddValueInternal(string name, object value, Type type)
        {
            if (_nameToIndex.ContainsKey(name))
            {
                throw new SerializationException(SR.Serialization_SameNameTwice);
            }
            _nameToIndex.Add(name, _count);

            //
            // If we need to expand the arrays, do so.
            //
            if (_count >= _names.Length)
            {
                ExpandArrays();
            }

            //
            // Add the data and then advance the counter.
            //
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
            if (_nameToIndex.TryGetValue(name, out index))
            {
                return index;
            }
            return -1;
        }

        /*==================================GetElement==================================
        **Action: Use FindElement to get the location of a particular member and then return
        **        the value of the element at that location.  The type of the member is
        **        returned in the foundType field.
        **Returns: The value of the element at the position associated with name.
        **Arguments: name -- the name of the element to find.
        **           foundType -- the type of the element associated with the given name.
        **Exceptions: None.  FindElement does null checking and throws for elements not 
        **            found.
        ==============================================================================*/
        private object GetElement(string name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                throw new SerializationException(SR.Format(SR.Serialization_NotFound, name));
            }

            Debug.Assert(index < _values.Length);
            Debug.Assert(index < _types.Length);

            foundType = _types[index];
            Debug.Assert(foundType != null);
            return _values[index];
        }

        #region GetValue
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

            Debug.Assert(_converter != null);

            return _converter.Convert(value, type);
        }

        public bool GetBoolean(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(bool)))
            {
                return (bool)value;
            }
            return _converter.ToBoolean(value);
        }

        public char GetChar(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(char)))
            {
                return (char)value;
            }
            return _converter.ToChar(value);
        }

        [CLSCompliant(false)]
        public sbyte GetSByte(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(sbyte)))
            {
                return (sbyte)value;
            }
            return _converter.ToSByte(value);
        }

        public byte GetByte(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(byte)))
            {
                return (byte)value;
            }
            return _converter.ToByte(value);
        }

        public short GetInt16(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(short)))
            {
                return (short)value;
            }
            return _converter.ToInt16(value);
        }

        [CLSCompliant(false)]
        public ushort GetUInt16(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(ushort)))
            {
                return (ushort)value;
            }
            return _converter.ToUInt16(value);
        }

        public int GetInt32(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(int)))
            {
                return (int)value;
            }
            return _converter.ToInt32(value);
        }

        [CLSCompliant(false)]
        public uint GetUInt32(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(uint)))
            {
                return (uint)value;
            }
            return _converter.ToUInt32(value);
        }

        public long GetInt64(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(long)))
            {
                return (long)value;
            }
            return _converter.ToInt64(value);
        }

        [CLSCompliant(false)]
        public ulong GetUInt64(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(ulong)))
            {
                return (ulong)value;
            }
            return _converter.ToUInt64(value);
        }

        public float GetSingle(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(float)))
            {
                return (float)value;
            }
            return _converter.ToSingle(value);
        }


        public double GetDouble(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(double)))
            {
                return (double)value;
            }
            return _converter.ToDouble(value);
        }

        public decimal GetDecimal(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(decimal)))
            {
                return (decimal)value;
            }
            return _converter.ToDecimal(value);
        }

        public DateTime GetDateTime(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(DateTime)))
            {
                return (DateTime)value;
            }
            return _converter.ToDateTime(value);
        }

        public string GetString(string name)
        {
            Type foundType;
            object value;

            value = GetElement(name, out foundType);
            if (ReferenceEquals(foundType, typeof(String)) || value == null)
            {
                return (string)value;
            }
            return _converter.ToString(value);
        }
        #endregion
    }
}