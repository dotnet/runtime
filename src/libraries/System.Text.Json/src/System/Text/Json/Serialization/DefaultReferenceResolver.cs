// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed class DefaultReferenceResolver
    {
        private uint _referenceCount;
        private Dictionary<object, object> _referenceMapper;

        public DefaultReferenceResolver()
        {
            // On deserialization: key is TKey.
            // On serialization: value is TKey.
            _referenceMapper = new Dictionary<object, object>();
        }

        // Used on deserialization.
        public void AddReference(string key, object value)
        {
            if (!_referenceMapper.TryAdd(key, value))
            {
                throw new JsonException($"Duplicated $id \"{key}\" found while preserving reference.");
            }
        }

        // Used on serialization.
        public string GetReference(object value)
        {
            object key;

            if (!_referenceMapper.TryGetValue(value, out key))
            {
                key = (++_referenceCount).ToString();
                _referenceMapper.Add(value, key);
            }

            return (string) key;
        }

        // Used on serialization.
        public bool IsReferenced(object value)
        {
            return _referenceMapper.ContainsKey(value);
        }

        // Used on deserialization.
        public object ResolveReference(string key)
        {
            if (!_referenceMapper.TryGetValue(key, out object value))
            {
                throw new JsonException("Reference not found.");
            }

            return value;
        }
    }
}
