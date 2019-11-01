// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using StringHandle = System.Int64;
using System.Xml;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Xml
{
    public class XmlBinaryReaderSession : IXmlDictionary
    {
        private const int MaxArrayEntries = 2048;

        private XmlDictionaryString[] _strings;
        private Dictionary<int, XmlDictionaryString> _stringDict;

        public XmlBinaryReaderSession()
        {
        }

        public XmlDictionaryString Add(int id, string value)
        {
            if (id < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(id), SR.XmlInvalidID));
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
            XmlDictionaryString xmlString;
            if (TryLookup(id, out xmlString))
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlIDDefined));

            xmlString = new XmlDictionaryString(this, value, id);
            if (id >= MaxArrayEntries)
            {
                if (_stringDict == null)
                    _stringDict = new Dictionary<int, XmlDictionaryString>();

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
                    Array.Copy(_strings, 0, newStrings, 0, _strings.Length);
                    _strings = newStrings;
                }
                _strings[id] = xmlString;
            }
            return xmlString;
        }

        public bool TryLookup(int key, out XmlDictionaryString result)
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

        public bool TryLookup(string value, out XmlDictionaryString result)
        {
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));

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

        public bool TryLookup(XmlDictionaryString value, out XmlDictionaryString result)
        {
            if (value == null)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(value)));
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
                Array.Clear(_strings, 0, _strings.Length);

            if (_stringDict != null)
                _stringDict.Clear();
        }
    }
}
