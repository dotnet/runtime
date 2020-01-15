// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    /// Our ReferenceResolver implementation to handle references.
    /// </summary>
    /// <remarks>
    /// It is currently a struct to save one unnecessary allcation while (de)serializing.
    /// If we choose to expose the ReferenceResolver in a future, we may need to create an abstract class and change this type to become a class that inherits from such abstract.
    /// </remarks>
    internal struct DefaultReferenceResolver
    {
        private uint _referenceCount;
        private Dictionary<string, object>? _keyObjectMap;
        private Dictionary<object, string>? _objectKeyMap;

        public DefaultReferenceResolver(bool isWrite)
        {
            _referenceCount = default;

            if (isWrite)
            {
                _objectKeyMap = new Dictionary<object, string>(ReferenceEqualsEqualityComparer<object>.Comparer);
                _keyObjectMap = null;
            }
            else
            {
                _keyObjectMap = new Dictionary<string, object>();
                _objectKeyMap = null;
            }
        }

        public void AddReference(string key, object value)
        {
            if (!JsonHelpers.TryAdd(_keyObjectMap!, key, value))
            {
                ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(key);
            }
        }

        public string GetOrAddReference(object value, out bool alreadyExists)
        {
            alreadyExists = _objectKeyMap!.TryGetValue(value, out string? key);
            if (!alreadyExists)
            {
                key = (++_referenceCount).ToString();
                _objectKeyMap.Add(value, key);
            }

            return key!;
        }

        public object ResolveReference(string key)
        {
            if (!_keyObjectMap!.TryGetValue(key, out object? value))
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(key);
            }

            return value!;
        }
    }
}
