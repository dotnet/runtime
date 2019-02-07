// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public readonly struct JsonProperty
    {
        public JsonElement Value { get; }

        internal JsonProperty(JsonElement value)
        {
            Value = value;
        }

        public string Name => Value.GetPropertyName();

        public override string ToString()
        {
            return Value.GetPropertyRawText();
        }
    }
}
