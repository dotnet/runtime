// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace System.Xml
{
    public class XmlDictionaryString
    {
        internal const int MinKey = 0;
        internal const int MaxKey = int.MaxValue / 4;

        private readonly IXmlDictionary _dictionary;
        private readonly string _value;
        private readonly int _key;
        private byte[]? _buffer;
        private static readonly EmptyStringDictionary s_emptyStringDictionary = new EmptyStringDictionary();

        public XmlDictionaryString(IXmlDictionary dictionary, string value, int key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(value);

            if (key < MinKey || key > MaxKey)
                throw new ArgumentOutOfRangeException(nameof(key), SR.Format(SR.ValueMustBeInRange, MinKey, MaxKey));
            _dictionary = dictionary;
            _value = value;
            _key = key;
        }

        [return: NotNullIfNotNull(nameof(s))]
        internal static string? GetString(XmlDictionaryString? s)
        {
            if (s == null)
                return null;
            return s.Value;
        }

        public static XmlDictionaryString Empty
        {
            get
            {
                return s_emptyStringDictionary.EmptyString;
            }
        }

        public IXmlDictionary Dictionary
        {
            get
            {
                return _dictionary;
            }
        }

        public int Key
        {
            get
            {
                return _key;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }

        internal byte[] ToUTF8()
        {
            return _buffer ??= System.Text.Encoding.UTF8.GetBytes(_value);
        }

        public override string ToString()
        {
            return _value;
        }

        private sealed class EmptyStringDictionary : IXmlDictionary
        {
            private readonly XmlDictionaryString _empty;

            public EmptyStringDictionary()
            {
                _empty = new XmlDictionaryString(this, string.Empty, 0);
            }

            public XmlDictionaryString EmptyString
            {
                get
                {
                    return _empty;
                }
            }

            public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
            {
                ArgumentNullException.ThrowIfNull(value);

                if (value.Length == 0)
                {
                    result = _empty;
                    return true;
                }
                result = null;
                return false;
            }

            public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
            {
                if (key == 0)
                {
                    result = _empty;
                    return true;
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
        }
    }
}
