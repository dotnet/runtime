// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    public readonly struct SerializationEntry
    {
        private readonly string _name;
        private readonly object? _value;
        private readonly Type _type;

        internal SerializationEntry(string entryName, object? entryValue, Type entryType)
        {
            _name = entryName;
            _value = entryValue;
            _type = entryType;
        }

        public object? Value => _value;
        public string Name => _name;
        public Type ObjectType => _type;
    }

    public sealed class SerializationInfoEnumerator : IEnumerator
    {
        private readonly string[] _members;
        private readonly object?[] _data;
        private readonly Type[] _types;
        private readonly int _numItems;
        private int _currItem;
        private bool _current;

        internal SerializationInfoEnumerator(string[] members, object?[] info, Type[] types, int numItems)
        {
            Debug.Assert(members != null, "[SerializationInfoEnumerator.ctor]members!=null");
            Debug.Assert(info != null, "[SerializationInfoEnumerator.ctor]info!=null");
            Debug.Assert(types != null, "[SerializationInfoEnumerator.ctor]types!=null");
            Debug.Assert(numItems >= 0, "[SerializationInfoEnumerator.ctor]numItems>=0");
            Debug.Assert(members.Length >= numItems, "[SerializationInfoEnumerator.ctor]members.Length>=numItems");
            Debug.Assert(info.Length >= numItems, "[SerializationInfoEnumerator.ctor]info.Length>=numItems");
            Debug.Assert(types.Length >= numItems, "[SerializationInfoEnumerator.ctor]types.Length>=numItems");

            _members = members;
            _data = info;
            _types = types;

            //The MoveNext semantic is much easier if we enforce that [0..m_numItems] are valid entries
            //in the enumerator, hence we subtract 1.
            _numItems = numItems - 1;
            _currItem = -1;
            _current = false;
        }

        public bool MoveNext()
        {
            if (_currItem < _numItems)
            {
                _currItem++;
                _current = true;
            }
            else
            {
                _current = false;
            }

            return _current;
        }

        object? IEnumerator.Current => Current; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268

        public SerializationEntry Current
        {
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return new SerializationEntry(_members[_currItem], _data[_currItem], _types[_currItem]);
            }
        }

        public void Reset()
        {
            _currItem = -1;
            _current = false;
        }

        public string Name
        {
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _members[_currItem];
            }
        }
        public object? Value
        {
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _data[_currItem];
            }
        }
        public Type ObjectType
        {
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
                return _types[_currItem];
            }
        }
    }
}
