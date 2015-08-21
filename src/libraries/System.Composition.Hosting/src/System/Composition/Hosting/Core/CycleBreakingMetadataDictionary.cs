// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Internal;

namespace System.Composition.Hosting.Core
{
    internal class CycleBreakingMetadataDictionary : IDictionary<string, object>
    {
        private readonly Lazy<ExportDescriptor> _exportDescriptor;

        public CycleBreakingMetadataDictionary(Lazy<ExportDescriptor> exportDescriptor)
        {
            _exportDescriptor = exportDescriptor;
        }

        private IDictionary<string, object> ActualMetadata
        {
            get
            {
                if (!_exportDescriptor.IsValueCreated)
                    throw ThrowHelper.NotImplemented_MetadataCycles();

                return _exportDescriptor.Value.Metadata;
            }
        }

        public void Add(string key, object value)
        {
            ActualMetadata.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return ActualMetadata.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return ActualMetadata.Keys; }
        }

        public bool Remove(string key)
        {
            return ActualMetadata.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return ActualMetadata.TryGetValue(key, out value);
        }

        public ICollection<object> Values
        {
            get { return ActualMetadata.Values; }
        }

        public object this[string key]
        {
            get
            {
                return ActualMetadata[key];
            }
            set
            {
                ActualMetadata[key] = value;
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ActualMetadata.Add(item);
        }

        public void Clear()
        {
            ActualMetadata.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ActualMetadata.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ActualMetadata.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return ActualMetadata.Count; }
        }

        public bool IsReadOnly
        {
            get { return ActualMetadata.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ActualMetadata.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ActualMetadata.GetEnumerator();
        }

        Collections.IEnumerator Collections.IEnumerable.GetEnumerator()
        {
            return ((Collections.IEnumerable)ActualMetadata).GetEnumerator();
        }
    }
}
