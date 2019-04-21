// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Text.Json.Serialization
{
    internal struct WriteStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated
        internal object CurrentValue;
        internal JsonClassInfo JsonClassInfo;

        internal IEnumerator Enumerator;

        // Current property values
        internal JsonPropertyInfo JsonPropertyInfo;

        // The current property.
        internal int PropertyIndex;

        // Has the Start tag been written
        internal bool StartObjectWritten;

        internal bool PopStackOnEndArray;
        internal bool PopStackOnEndObject;

        internal void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if (JsonClassInfo.ClassType == ClassType.Value || JsonClassInfo.ClassType == ClassType.Enumerable || JsonClassInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
            }
        }

        internal void Reset()
        {
            CurrentValue = null;
            JsonClassInfo = null;
            StartObjectWritten = false;
            EndObject();
            EndArray();
        }

        internal void EndObject()
        {
            PropertyIndex = 0;
            PopStackOnEndObject = false;
            EndProperty();
        }

        internal void EndDictionary()
        {
            Enumerator = null;
            EndProperty();
        }

        internal void EndArray()
        {
            Enumerator = null;
            PopStackOnEndArray = false;
            EndProperty();
        }

        internal void EndProperty()
        {
            JsonPropertyInfo = null;
        }

        internal void NextProperty()
        {
            JsonPropertyInfo = null;
            PropertyIndex++;
        }
    }
}
