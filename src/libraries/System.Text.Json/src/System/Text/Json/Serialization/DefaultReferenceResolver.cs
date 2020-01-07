﻿// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed class DefaultReferenceResolver
    {
        private uint _referenceCount;
        private Dictionary<string, object> _keyObjectMap;
        private Dictionary<object, string> _objectKeyMap;

        public DefaultReferenceResolver(bool isWrite)
        {
            if (isWrite)
            {
                _objectKeyMap = new Dictionary<object, string>(ReferenceEqualsEqualityComparer<object>.Comparer);
            }
            else
            {
                _keyObjectMap = new Dictionary<string, object>();
            }
        }

        public void AddReference(string key, object value)
        {
            if (_keyObjectMap.ContainsKey(key))
            {
                ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(key);
            }

            _keyObjectMap[key] = value;
        }

        public string GetOrAddReference(object value, out bool alreadyExists)
        {
            alreadyExists = _objectKeyMap.TryGetValue(value, out string key);
            if (!alreadyExists)
            {
                key = (++_referenceCount).ToString();
                _objectKeyMap.Add(value, key);
            }

            return key;
        }

        public object ResolveReference(string key)
        {
            if (!_keyObjectMap.TryGetValue(key, out object value))
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(key);
            }

            return value;
        }
    }
}
