// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace System.Xml
{
    public class XmlDictionary : IXmlDictionary
    {
        private static IXmlDictionary? s_empty;
        private readonly Dictionary<string, XmlDictionaryString> _lookup;
        private XmlDictionaryString[]? _strings;
        private int _nextId;

        public static IXmlDictionary Empty => s_empty ??= new EmptyDictionary();

        public XmlDictionary()
        {
            _lookup = new Dictionary<string, XmlDictionaryString>();
            _strings = null;
            _nextId = 0;
        }

        public XmlDictionary(int capacity)
        {
            _lookup = new Dictionary<string, XmlDictionaryString>(capacity);
            _strings = new XmlDictionaryString[capacity];
            _nextId = 0;
        }

        public virtual XmlDictionaryString Add(string value)
        {
            XmlDictionaryString? str;
            if (!_lookup.TryGetValue(value, out str))
            {
                if (_strings == null)
                {
                    _strings = new XmlDictionaryString[4];
                }
                else if (_nextId == _strings.Length)
                {
                    int newSize = _nextId * 2;
                    if (newSize == 0)
                        newSize = 4;
                    Array.Resize(ref _strings, newSize);
                }
                str = new XmlDictionaryString(this, value, _nextId);
                _strings[_nextId] = str;
                _lookup.Add(value, str);
                _nextId++;
            }
            return str;
        }

        public virtual bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
        {
            return _lookup.TryGetValue(value, out result);
        }

        public virtual bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
        {
            if (key < 0 || key >= _nextId)
            {
                result = null;
                return false;
            }
            result = _strings![key];
            return true;
        }

        public virtual bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Dictionary != this)
            {
                result = null;
                return false;
            }
            result = value;
            return true;
        }

        private sealed class EmptyDictionary : IXmlDictionary
        {
            public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
            {
                result = null;
                return false;
            }

            public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
            {
                result = null;
                return false;
            }

            public bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result)
            {
                result = null;
                return false;
            }
        }
    }
}
