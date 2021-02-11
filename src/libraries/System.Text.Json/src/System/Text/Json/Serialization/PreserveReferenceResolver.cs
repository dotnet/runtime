// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The default ReferenceResolver implementation to handle duplicate object references.
    /// </summary>
    internal sealed class PreserveReferenceResolver : ReferenceResolver
    {
        private uint _referenceCount;
        private readonly Dictionary<string, object>? _referenceIdToObjectMap;
        private readonly Dictionary<object, string>? _objectToReferenceIdMap;

        public PreserveReferenceResolver(bool writing)
        {
            if (writing)
            {
                // Comparer used here does a reference equality comparison on serialization, which is where we use the objects as the dictionary keys.
                _objectToReferenceIdMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
            }
            else
            {
                _referenceIdToObjectMap = new Dictionary<string, object>();
            }
        }

        public override void AddReference(string referenceId, object value)
        {
            Debug.Assert(_referenceIdToObjectMap != null);

            if (!JsonHelpers.TryAdd(_referenceIdToObjectMap, referenceId, value))
            {
                ThrowHelper.ThrowJsonException_MetadataDuplicateIdFound(referenceId);
            }
        }

        public override string GetReference(object value, out bool alreadyExists)
        {
            Debug.Assert(_objectToReferenceIdMap != null);

            if (_objectToReferenceIdMap.TryGetValue(value, out string? referenceId))
            {
                alreadyExists = true;
            }
            else
            {
                _referenceCount++;
                referenceId = _referenceCount.ToString();
                _objectToReferenceIdMap.Add(value, referenceId);
                alreadyExists = false;
            }

            return referenceId;
        }

        public override object ResolveReference(string referenceId)
        {
            Debug.Assert(_referenceIdToObjectMap != null);

            if (!_referenceIdToObjectMap.TryGetValue(referenceId, out object? value))
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(referenceId);
            }

            return value;
        }
    }
}
