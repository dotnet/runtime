// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics.Contracts;

namespace System.Runtime.Serialization
{
    //
    // The tuple returned by SerializationInfoEnumerator.Current.
    //
    [System.Runtime.InteropServices.ComVisible(true)]
    internal struct SerializationEntry
    {
        private Type _type;
        private Object _value;
        private String _name;

        public Object Value
        {
            get
            {
                return _value;
            }
        }

        public String Name
        {
            get
            {
                return _name;
            }
        }

        public Type ObjectType
        {
            get
            {
                return _type;
            }
        }

        internal SerializationEntry(String entryName, Object entryValue, Type entryType)
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
    [System.Runtime.InteropServices.ComVisible(true)]
    internal sealed class SerializationInfoEnumerator : IEnumerator
    {
        private String[] _members;
        private Object[] _data;
        private Type[] _types;
        private int _numItems;
        private int _currItem;
        private bool _current;

        internal SerializationInfoEnumerator(String[] members, Object[] info, Type[] types, int numItems)
        {
            Contract.Assert(members != null, "[SerializationInfoEnumerator.ctor]members!=null");
            Contract.Assert(info != null, "[SerializationInfoEnumerator.ctor]info!=null");
            Contract.Assert(types != null, "[SerializationInfoEnumerator.ctor]types!=null");
            Contract.Assert(numItems >= 0, "[SerializationInfoEnumerator.ctor]numItems>=0");
            Contract.Assert(members.Length >= numItems, "[SerializationInfoEnumerator.ctor]members.Length>=numItems");
            Contract.Assert(info.Length >= numItems, "[SerializationInfoEnumerator.ctor]info.Length>=numItems");
            Contract.Assert(types.Length >= numItems, "[SerializationInfoEnumerator.ctor]types.Length>=numItems");

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

        /// <internalonly/>
        Object IEnumerator.Current
        { //Actually returns a SerializationEntry
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(/*Environment.GetResourceString(*/"InvalidOperation_EnumOpCantHappen"/*)*/);
                }
                return (Object)(new SerializationEntry(_members[_currItem], _data[_currItem], _types[_currItem]));
            }
        }

        public SerializationEntry Current
        { //Actually returns a SerializationEntry
            get
            {
                if (_current == false)
                {
                    throw new InvalidOperationException(/*Environment.GetResourceString(*/"InvalidOperation_EnumOpCantHappen"/*)*/);
                }
                return (new SerializationEntry(_members[_currItem], _data[_currItem], _types[_currItem]));
            }
        }

        public void Reset()
        {
            _currItem = -1;
            _current = false;
        }
    }
}
