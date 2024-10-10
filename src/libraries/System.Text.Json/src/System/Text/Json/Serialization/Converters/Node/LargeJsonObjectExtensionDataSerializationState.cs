// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implements a mitigation for deserializing large JsonObject extension data properties.
    /// Extension data properties use replace semantics when duplicate keys are encountered,
    /// which is an O(n) operation for JsonObject resulting in O(n^2) total deserialization time.
    /// This class mitigates the performance issue by storing the deserialized properties in a
    /// temporary dictionary (which has O(1) updates) and copies them to the destination object
    /// at the end of deserialization.
    /// </summary>
    internal sealed class LargeJsonObjectExtensionDataSerializationState
    {
        public const int LargeObjectThreshold = 25;
        private readonly Dictionary<string, JsonNode?> _tempDictionary;
        public JsonObject Destination { get; }

        public LargeJsonObjectExtensionDataSerializationState(JsonObject destination)
        {
            StringComparer comparer = destination.Options?.PropertyNameCaseInsensitive ?? false
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            Destination = destination;
            _tempDictionary = new(comparer);
        }

        /// <summary>
        /// Stores a deserialized property to the temporary dictionary, using replace semantics.
        /// </summary>
        public void AddProperty(string key, JsonNode? value)
        {
            _tempDictionary[key] = value;
        }

        /// <summary>
        /// Copies the properties from the temporary dictionary to the destination JsonObject.
        /// </summary>
        public void Complete()
        {
            // Because we're only appending values to _tempDictionary, this should preserve JSON ordering.
            foreach (KeyValuePair<string, JsonNode?> kvp in _tempDictionary)
            {
                Destination[kvp.Key] = kvp.Value;
            }
        }
    }
}
