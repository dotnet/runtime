// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json
{
    internal struct WriteStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated.
        public object CurrentValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // Whether the current object can be constructed with IDictionary.
        public bool IsIDictionaryConstructible;
        public bool IsIDictionaryConstructibleProperty;

        // The current enumerator for the IEnumerable or IDictionary.
        public IEnumerator Enumerator;

        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;

        // The current property.
        public int PropertyIndex;

        // Has the Start tag been written.
        public bool StartObjectWritten;

        // Pop the stack when the current array or dictionary is done.
        public bool PopStackOnEnd;

        // Pop the stack when the current object is done.
        public bool PopStackOnEndObject;

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if (JsonClassInfo.ClassType == ClassType.Value || JsonClassInfo.ClassType == ClassType.Enumerable || JsonClassInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
            }
            else if (JsonClassInfo.ClassType == ClassType.IDictionaryConstructible)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
                IsIDictionaryConstructible = true;
            }
            else if (JsonClassInfo.ClassType == ClassType.KeyValuePair)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyPropertyOfKeyValuePair();
                // Advance to the next property, since the first one is the KeyValuePair type itself,
                // not its first property (Key or Value).
                PropertyIndex++;
            }
        }

        public void WriteObjectOrArrayStart(ClassType classType, Utf8JsonWriter writer, bool writeNull = false)
        {
            if (JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                WriteObjectOrArrayStart(classType, JsonPropertyInfo.EscapedName.Value, writer, writeNull);
            }
            else if (KeyName != null)
            {
                JsonEncodedText propertyName = JsonEncodedText.Encode(KeyName);
                WriteObjectOrArrayStart(classType, propertyName, writer, writeNull);
            }
            else
            {
                Debug.Assert(writeNull == false);

                // Write start without a property name.
                if (classType == ClassType.Object || classType == ClassType.Dictionary || classType == ClassType.IDictionaryConstructible)
                {
                    writer.WriteStartObject();
                    StartObjectWritten = true;
                }
                else
                {
                    Debug.Assert(classType == ClassType.Enumerable);
                    writer.WriteStartArray();
                }
            }
        }

        private void WriteObjectOrArrayStart(ClassType classType, JsonEncodedText propertyName, Utf8JsonWriter writer, bool writeNull)
        {
            if (writeNull)
            {
                writer.WriteNull(propertyName);
            }
            else if (classType == ClassType.Object ||
                classType == ClassType.Dictionary ||
                classType == ClassType.IDictionaryConstructible)
            {
                writer.WriteStartObject(propertyName);
                StartObjectWritten = true;
            }
            else
            {
                Debug.Assert(classType == ClassType.Enumerable);
                writer.WriteStartArray(propertyName);
            }
        }

        public void Reset()
        {
            CurrentValue = null;
            Enumerator = null;
            KeyName = null;
            JsonClassInfo = null;
            JsonPropertyInfo = null;
            PropertyIndex = 0;
            IsIDictionaryConstructible = false;
            PopStackOnEndObject = false;
            PopStackOnEnd = false;
            StartObjectWritten = false;
        }

        public void EndObject()
        {
            PropertyIndex = 0;
            PopStackOnEndObject = false;
            IsIDictionaryConstructibleProperty = false;
            JsonPropertyInfo = null;
        }

        public void EndDictionary()
        {
            Enumerator = null;
            PopStackOnEnd = false;
        }

        public void EndArray()
        {
            Enumerator = null;
            PopStackOnEnd = false;
            JsonPropertyInfo = null;
        }

        public void NextProperty()
        {
            JsonPropertyInfo = null;
            PropertyIndex++;
        }
    }
}
