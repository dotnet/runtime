// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal class JsonObjectProperty
    {
        internal string Name { get; }
        internal JsonNode Value { get; set;  }
        internal JsonObjectProperty Prev { get; set; }
        internal JsonObjectProperty Next { get; set; }

        public JsonObjectProperty(string name, JsonNode value, JsonObjectProperty prev, JsonObjectProperty next)
        {
            Name = name;
            Value = value;
            Prev = prev;
            Next = next;
        }
    }
}
