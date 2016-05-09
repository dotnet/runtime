// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    public struct SerializationEntry
    {
        private readonly Type _type;
        private readonly object _value;
        private readonly string _name;

        public object Value => _value;      
        public string Name => _name;    
        public Type ObjectType => _type;

        internal SerializationEntry(string entryName, object entryValue, Type entryType)
        {
            _value = entryValue;
            _name = entryName;
            _type = entryType;
        }
    }

    //
    // A simple enumerator over the values stored in the SerializationInfo.
    // This does not snapshot the values, it just keeps pointers to the 
    // member variables of the SerializationInfo that created it.
    //
    public sealed class SerializationInfoEnumerator : IEnumerator
    {
        private readonly string[] _names;
        private readonly object[] _values;
        private readonly Type[] _types;
        private readonly int _count;
        private int _current;
        private bool _hasCurrent;

        internal SerializationInfoEnumerator(string[] names, object[] values, Type[] types, int count)
        {
            Debug.Assert(names != null);
            Debug.Assert(values != null);
            Debug.Assert(types != null);
            Debug.Assert(count >= 0);
            Debug.Assert(names.Length >= count);
            Debug.Assert(values.Length >= count);
            Debug.Assert(types.Length >= count);

            _names = names;
            _values = values;
            _types = types;
            //The MoveNext semantic is much easier if we enforce that [0..m_numItems] are valid entries
            //in the enumerator, hence we subtract 1.
            _count = count - 1;
            _current = -1;
            _hasCurrent = false;
        }

        public bool MoveNext()
        {
            if (_current < _count)
            {
                _current++;
                _hasCurrent = true;
            }
            else
            {
                _hasCurrent = false;
            }
            return _hasCurrent;
        }

        /// <internalonly/>
        object IEnumerator.Current
        {
            get
            {
                if (_hasCurrent == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return new SerializationEntry(_names[_current], _values[_current], _types[_current]);
            }
        }

        public SerializationEntry Current
        {
            get
            {
                if (_hasCurrent == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return (new SerializationEntry(_names[_current], _values[_current], _types[_current]));
            }
        }

        public void Reset()
        {
            _current = -1;
            _hasCurrent = false;
        }

        public string Name
        {
            get
            {
                if (_hasCurrent == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _names[_current];
            }
        }
        public object Value
        {
            get
            {
                if (_hasCurrent == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _values[_current];
            }
        }
        public Type ObjectType
        {
            get
            {
                if (_hasCurrent == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _types[_current];
            }
        }
    }
}