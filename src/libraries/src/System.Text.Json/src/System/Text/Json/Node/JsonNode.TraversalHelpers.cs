// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///   The base class that represents a single node within a mutable JSON document.
    /// </summary>
    public abstract partial class JsonNode
    {
        private static void AddToParent(
            KeyValuePair<string, JsonNode> nodePair,
            ref Stack<KeyValuePair<string, JsonNode>> currentNodes,
            ref JsonNode toReturn,
            DuplicatePropertyNameHandlingStrategy duplicatePropertyNameHandling = DuplicatePropertyNameHandlingStrategy.Replace)
        {
            if (currentNodes.TryPeek(out KeyValuePair<string, JsonNode> parentPair))
            {
                // Parent needs to be JsonObject or JsonArray
                Debug.Assert(parentPair.Value is JsonObject || parentPair.Value is JsonArray);

                if (parentPair.Value is JsonObject jsonObject)
                {
                    Debug.Assert(nodePair.Key != null);

                    // Handle duplicate properties accordingly to duplicatePropertyNameHandling:

                    if (duplicatePropertyNameHandling == DuplicatePropertyNameHandlingStrategy.Replace)
                    {
                        jsonObject[nodePair.Key] = nodePair.Value;
                    }
                    else if (jsonObject._dictionary.ContainsKey(nodePair.Key))
                    {
                        if (duplicatePropertyNameHandling == DuplicatePropertyNameHandlingStrategy.Error)
                            throw new ArgumentException(SR.JsonObjectDuplicateKey);

                        Debug.Assert(duplicatePropertyNameHandling == DuplicatePropertyNameHandlingStrategy.Ignore);
                    }
                    else
                    {
                        jsonObject.Add(nodePair);
                    }
                }
                else if (parentPair.Value is JsonArray jsonArray)
                {
                    Debug.Assert(nodePair.Key == null);
                    jsonArray.Add(nodePair.Value);
                }
            }
            else
            {
                toReturn = nodePair.Value;
            }
        }

        private struct RecursionStackFrame
        {
            public string PropertyName { get; set; }
            public JsonNode PropertyValue { get; set; }
            public JsonValueKind ValueKind { get; set; } // to retrieve ValueKind when PropertyValue is null

            public RecursionStackFrame(string propertyName, JsonNode propertyValue, JsonValueKind valueKind)
            {
                PropertyName = propertyName;
                PropertyValue = propertyValue;
                ValueKind = valueKind;
            }

            public RecursionStackFrame(string propertyName, JsonNode propertyValue) : this(propertyName, propertyValue, propertyValue.ValueKind)
            {
            }
        }
    }
}
