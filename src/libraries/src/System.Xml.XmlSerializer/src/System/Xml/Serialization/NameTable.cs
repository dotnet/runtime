// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

namespace System.Xml.Serialization
{
    using System.Collections;
    using System.Xml.Extensions;
    // this[key] api throws KeyNotFoundException
    using Hashtable = System.Collections.InternalHashtable;

    internal class NameKey
    {
        private string _ns;
        private string _name;

        internal NameKey(string name, string ns)
        {
            _name = name;
            _ns = ns;
        }

        public override bool Equals(object other)
        {
            if (!(other is NameKey)) return false;
            NameKey key = (NameKey)other;
            return _name == key._name && _ns == key._ns;
        }

        public override int GetHashCode()
        {
            return (_ns == null ? "<null>".GetHashCode() : _ns.GetHashCode()) ^ (_name == null ? 0 : _name.GetHashCode());
        }
    }
    internal interface INameScope
    {
        object this[string name, string ns] { get; set; }
    }
    internal class NameTable : INameScope
    {
        private Hashtable _table = new Hashtable();


        internal void Add(string name, string ns, object value)
        {
            NameKey key = new NameKey(name, ns);
            _table.Add(key, value);
        }

        internal object this[string name, string ns]
        {
            get
            {
                return _table[new NameKey(name, ns)];
            }
            set
            {
                _table[new NameKey(name, ns)] = value;
            }
        }
        object INameScope.this[string name, string ns]
        {
            get
            {
                return _table[new NameKey(name, ns)];
            }
            set
            {
                _table[new NameKey(name, ns)] = value;
            }
        }

        internal ICollection Values
        {
            get { return _table.Values; }
        }

        internal Array ToArray(Type type)
        {
            Array a = Array.CreateInstance(type, _table.Count);
            _table.Values.CopyTo(a, 0);
            return a;
        }
    }
}

