// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal struct WriteStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated.
        public object CurrentValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // The current IEnumerable or IDictionary.
        public IEnumerator CollectionEnumerator;
        // Note all bools are kept together for packing:
        public bool PopStackOnEndCollection;
        public bool IsIDictionaryConstructible;
        public bool IsIDictionaryConstructibleProperty;

        // The current object.
        public bool PopStackOnEndObject;
        public bool StartObjectWritten;
        public bool MoveToNextProperty;

        // The current property.
        public bool PropertyEnumeratorActive;
        public ExtensionDataWriteStatus ExtensionDataStatus;
        public IEnumerator PropertyEnumerator;
        public JsonPropertyInfo JsonPropertyInfo;

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if (JsonClassInfo.ClassType == ClassType.Value || JsonClassInfo.ClassType == ClassType.Enumerable || JsonClassInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo = JsonClassInfo.PolicyProperty;
            }
            else if (JsonClassInfo.ClassType == ClassType.IDictionaryConstructible)
            {
                JsonPropertyInfo = JsonClassInfo.PolicyProperty;
                IsIDictionaryConstructible = true;
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
            EndObject();
        }

        public void EndObject()
        {
            CollectionEnumerator = null;
            ExtensionDataStatus = ExtensionDataWriteStatus.NotStarted;
            IsIDictionaryConstructible = false;
            JsonClassInfo = null;
            PropertyEnumerator = null;
            PropertyEnumeratorActive = false;
            PopStackOnEndCollection = false;
            PopStackOnEndObject = false;
            StartObjectWritten = false;
            EndProperty();
        }

        public void EndProperty()
        {
            IsIDictionaryConstructibleProperty = false;
            JsonPropertyInfo = null;
            KeyName = null;
            MoveToNextProperty = false;
        }

        public void EndDictionary()
        {
            CollectionEnumerator = null;
            PopStackOnEndCollection = false;
        }

        public void EndArray()
        {
            CollectionEnumerator = null;
            PopStackOnEndCollection = false;
        }

        public void NextProperty()
        {
            EndProperty();

            if (PropertyEnumeratorActive)
            {
                if (PropertyEnumerator.MoveNext())
                {
                    PropertyEnumeratorActive = true;
                }
                else
                {
                    PropertyEnumeratorActive = false;
                    ExtensionDataStatus = ExtensionDataWriteStatus.Writing;
                }
            }
            else
            {
                ExtensionDataStatus = ExtensionDataWriteStatus.Finished;
            }
        }
    }
}
