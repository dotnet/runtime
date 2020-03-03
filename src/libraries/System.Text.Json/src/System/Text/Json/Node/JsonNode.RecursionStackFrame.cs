// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    ///   The base class that represents a single node within a mutable JSON document.
    /// </summary>
    public abstract partial class JsonNode
    {
        private readonly struct RecursionStackFrame
        {
            public string? PropertyName { get; }
            public JsonNode? PropertyValue { get; }
            public JsonValueKind ValueKind { get; } // to retrieve ValueKind when PropertyValue is null

            public RecursionStackFrame(string? propertyName, JsonNode? propertyValue, JsonValueKind valueKind)
            {
                PropertyName = propertyName;
                PropertyValue = propertyValue;
                ValueKind = valueKind;
            }

            public RecursionStackFrame(string? propertyName, JsonNode propertyValue) : this(propertyName, propertyValue, propertyValue.ValueKind)
            {
            }
        }
    }
}
