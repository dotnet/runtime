// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The default ReferenceResolver implementation to handle duplicate object references.
    /// </summary>
    /// <remarks>
    /// It is currently a struct to save one unnecessary allcation while (de)serializing.
    /// If we choose to expose the ReferenceResolver in a future, we may need to create an abstract class/interface and change this type to become a class that inherits from that abstract class/interface.
    /// </remarks>
    internal struct DefaultReferenceResolver
    {
        private uint _referenceCount;
        private Dictionary<string, object>? _keyObjectMap;
        private Dictionary<object, string>? _objectKeyMap;

        public DefaultReferenceResolver(bool writing)
        {
            _referenceCount = default;

            if (writing)
            {
                // Comparer used here to always do a Reference Equality comparison on serialization which is where we use the objects as the TKey in our dictionary.
                _objectKeyMap = new Dictionary<object, string>(ReferenceEqualsEqualityComparer<object>.Comparer);
                _keyObjectMap = null;
            }
            else
            {
                _keyObjectMap = new Dictionary<string, object>();
                _objectKeyMap = null;
            }
        }

        public void AddReferenceOnDeserialize(string key, object value)
        {
            if (!JsonHelpers.TryAdd(_keyObjectMap!, key, value))
            {
                ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(key);
            }
        }

        public bool TryGetOrAddReferenceOnSerialize(object value, out string key)
        {
            if (!_objectKeyMap!.TryGetValue(value, out key!))
            {
                key = (++_referenceCount).ToString();
                _objectKeyMap.Add(value, key);

                return false;
            }

            return true;
        }

        public object ResolveReferenceOnDeserialize(string key)
        {
            if (!_keyObjectMap!.TryGetValue(key, out object? value))
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(key);
            }

            return value;
        }
    }
}
