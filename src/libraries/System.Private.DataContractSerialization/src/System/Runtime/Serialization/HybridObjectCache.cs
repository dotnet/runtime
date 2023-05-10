// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Collections.Generic;


namespace System.Runtime.Serialization
{
    internal sealed class HybridObjectCache
    {
        private Dictionary<string, object?>? _objectDictionary;
        private Dictionary<string, object?>? _referencedObjectDictionary;

        internal HybridObjectCache()
        {
        }

        internal void Add(string id, object? obj)
        {
            _objectDictionary ??= new Dictionary<string, object?>();

            if (_objectDictionary.ContainsKey(id))
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.MultipleIdDefinition, id));
            _objectDictionary.Add(id, obj);
        }

        internal void Remove(string id)
        {
            _objectDictionary?.Remove(id);
        }

        internal object? GetObject(string id)
        {
            _referencedObjectDictionary ??= new Dictionary<string, object?>();
            _referencedObjectDictionary.TryAdd(id, null);

            if (_objectDictionary != null)
            {
                object? obj;
                _objectDictionary.TryGetValue(id, out obj);
                return obj;
            }

            return null;
        }

        internal bool IsObjectReferenced(string id)
        {
            if (_referencedObjectDictionary != null)
            {
                return _referencedObjectDictionary.ContainsKey(id);
            }
            return false;
        }
    }
}
