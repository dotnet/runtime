// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StringHandle = System.Int64;
using System.Xml;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    public class XmlBinaryReaderSession : IXmlDictionary
    {
        private const int MaxArrayEntries = 2048;

        private XmlDictionaryString[]? _strings;
        private Dictionary<int, XmlDictionaryString>? _stringDict;

        public XmlBinaryReaderSession()
        {
        }

        public XmlDictionaryString Add(int id, string value)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), SR.XmlInvalidID);
            ArgumentNullException.ThrowIfNull(value);
            XmlDictionaryString? xmlString;
            if (TryLookup(id, out _))
                throw new InvalidOperationException(SR.XmlIDDefined);

            xmlString = new XmlDictionaryString(this, value, id);
            if (id >= MaxArrayEntries)
            {
                _stringDict ??= new Dictionary<int, XmlDictionaryString>();

                _stringDict.Add(id, xmlString);
            }
            else
            {
                if (_strings == null)
                {
                    _strings = new XmlDictionaryString[Math.Max(id + 1, 16)];
                }
                else if (id >= _strings.Length)
                {
                    XmlDictionaryString[] newStrings = new XmlDictionaryString[Math.Min(Math.Max(id + 1, _strings.Length * 2), MaxArrayEntries)];
                    Array.Copy(_strings, newStrings, _strings.Length);
                    _strings = newStrings;
                }
                _strings[id] = xmlString;
            }
            return xmlString;
        }

        public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
        {
            if (_strings != null && key >= 0 && key < _strings.Length)
            {
                result = _strings[key];
                return result != null;
            }
            else if (key >= MaxArrayEntries)
            {
                if (_stringDict != null)
                    return _stringDict.TryGetValue(key, out result);
            }
            result = null;
            return false;
        }

        public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (_strings != null)
            {
                for (int i = 0; i < _strings.Length; i++)
                {
                    XmlDictionaryString s = _strings[i];
                    if (s != null && s.Value == value)
                    {
                        result = s;
                        return true;
                    }
                }
            }

            if (_stringDict != null)
            {
                foreach (XmlDictionaryString s in _stringDict.Values)
                {
                    if (s.Value == value)
                    {
                        result = s;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result)
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

        public void Clear()
        {
            if (_strings != null)
                Array.Clear(_strings);

            _stringDict?.Clear();
        }
    }
}
