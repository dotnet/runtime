// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization
{
    using System.Xml;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal sealed class NameKey
    {
        private readonly string? _ns;
        private readonly string? _name;

        internal NameKey(string? name, string? ns)
        {
            _name = name;
            _ns = ns;
        }

        public override bool Equals([NotNullWhen(true)] object? other)
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
        object? this[string? name, string? ns] { get; set; }
    }
    internal sealed class NameTable : INameScope
    {
        private readonly Dictionary<NameKey, object?> _table = new Dictionary<NameKey, object?>();

        internal void Add(XmlQualifiedName qname, object value)
        {
            Add(qname.Name, qname.Namespace, value);
        }

        internal void Add(string? name, string? ns, object value)
        {
            NameKey key = new NameKey(name, ns);
            _table.Add(key, value);
        }

        internal object? this[XmlQualifiedName qname]
        {
            get
            {
                object? obj;
                return _table.TryGetValue(new NameKey(qname.Name, qname.Namespace), out obj) ? obj : null;
            }
            set
            {
                _table[new NameKey(qname.Name, qname.Namespace)] = value;
            }
        }
        internal object? this[string? name, string? ns]
        {
            get
            {
                object? obj;
                return _table.TryGetValue(new NameKey(name, ns), out obj) ? obj : null;
            }
            set
            {
                _table[new NameKey(name, ns)] = value;
            }
        }
        object? INameScope.this[string? name, string? ns]
        {
            get
            {
                object? obj;
                _table.TryGetValue(new NameKey(name, ns), out obj);
                return obj;
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
            ((ICollection)_table.Values).CopyTo(a, 0);
            return a;
        }
    }
}
