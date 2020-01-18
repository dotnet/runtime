// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal struct WriteStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated.
        public object? CurrentValue;
        public JsonClassInfo? JsonClassInfo;

        // Support Dictionary keys.
        public string? KeyName;

        // The current IEnumerable or IDictionary.
        public IEnumerator? CollectionEnumerator;
        // Note all bools are kept together for packing:
        public bool PopStackOnEndCollection;

        // The current object.
        public bool PopStackOnEndObject;
        public bool StartObjectWritten;
        public bool MoveToNextProperty;

        public bool WriteWrappingBraceOnEndPreservedArray;

        // The current property.
        public int PropertyEnumeratorIndex;
        public ExtensionDataWriteStatus ExtensionDataStatus;
        public JsonPropertyInfo? JsonPropertyInfo;

        // Pre-encoded metadata properties.
        private static readonly JsonEncodedText s_metadataId = JsonEncodedText.Encode("$id", encoder: null);
        private static readonly JsonEncodedText s_metadataRef = JsonEncodedText.Encode("$ref", encoder: null);
        private static readonly JsonEncodedText s_metadataValues = JsonEncodedText.Encode("$values", encoder: null);

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if ((JsonClassInfo.ClassType & (ClassType.Value | ClassType.Enumerable | ClassType.Dictionary)) != 0)
            {
                JsonPropertyInfo = JsonClassInfo.PolicyProperty;
            }
        }

        public void WriteObjectOrArrayStart(ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, bool writeNull = false)
        {
            if (JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                WriteObjectOrArrayStart(classType, JsonPropertyInfo.EscapedName!.Value, writer, writeNull);
            }
            else if (KeyName != null)
            {
                JsonEncodedText propertyName = JsonEncodedText.Encode(KeyName, options.Encoder);
                WriteObjectOrArrayStart(classType, propertyName, writer, writeNull);
            }
            else
            {
                Debug.Assert(writeNull == false);

                // Write start without a property name.
                if (classType == ClassType.Object || classType == ClassType.Dictionary)
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
            else if ((classType & (ClassType.Object | ClassType.Dictionary)) != 0)
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

        public void WritePreservedObjectOrArrayStart(ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, string referenceId)
        {
            if (JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                writer.WriteStartObject(JsonPropertyInfo.EscapedName!.Value);
            }
            else if (KeyName != null)
            {
                writer.WriteStartObject(KeyName);
            }
            else
            {
                writer.WriteStartObject();
            }


            writer.WriteString(s_metadataId, referenceId);

            if ((classType & (ClassType.Object | ClassType.Dictionary)) != 0)
            {
                StartObjectWritten = true;
            }
            else
            {
                // Wrap array into an object with $id and $values metadata properties.
                Debug.Assert(classType == ClassType.Enumerable);
                writer.WriteStartArray(s_metadataValues);
                WriteWrappingBraceOnEndPreservedArray = true;
            }
        }

        public void WriteReferenceObject(Utf8JsonWriter writer, JsonSerializerOptions options, string referenceId)
        {
            if (JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                writer.WriteStartObject(JsonPropertyInfo.EscapedName!.Value);
            }
            else if (KeyName != null)
            {
                writer.WriteStartObject(KeyName);
            }
            else
            {
                writer.WriteStartObject();
            }

            writer.WriteString(s_metadataRef, referenceId);
            writer.WriteEndObject();
        }

        public void Reset()
        {
            CurrentValue = null;
            CollectionEnumerator = null;
            ExtensionDataStatus = ExtensionDataWriteStatus.NotStarted;
            JsonClassInfo = null;
            PropertyEnumeratorIndex = 0;
            PopStackOnEndCollection = false;
            PopStackOnEndObject = false;
            StartObjectWritten = false;

            EndProperty();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndProperty()
        {
            JsonPropertyInfo = null;
            KeyName = null;
            MoveToNextProperty = false;
            WriteWrappingBraceOnEndPreservedArray = false;
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

        // AggressiveInlining used although a large method it is only called from one location and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextProperty()
        {
            EndProperty();

            Debug.Assert(JsonClassInfo != null && JsonClassInfo.PropertyCacheArray != null);
            int maxPropertyIndex = JsonClassInfo.PropertyCacheArray.Length;

            ++PropertyEnumeratorIndex;
            if (PropertyEnumeratorIndex >= maxPropertyIndex)
            {
                if (PropertyEnumeratorIndex > maxPropertyIndex)
                {
                    ExtensionDataStatus = ExtensionDataWriteStatus.Finished;
                }
                else if (JsonClassInfo.DataExtensionProperty != null)
                {
                    ExtensionDataStatus = ExtensionDataWriteStatus.Writing;
                }
            }
        }
    }
}
