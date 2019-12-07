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
        public object CurrentValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // The current IEnumerable or IDictionary.
        public IEnumerator CollectionEnumerator;
        // Note all bools are kept together for packing:
        public bool PopStackOnEndCollection;

        // The current object.
        public bool PopStackOnEndObject;
        public bool StartObjectWritten;
        public bool MoveToNextProperty;

        public bool WriteWrappingBraceOnEndCollection;
        public bool KeepReferenceInSet;

        // The current property.
        public int PropertyEnumeratorIndex;
        public ExtensionDataWriteStatus ExtensionDataStatus;
        public JsonPropertyInfo JsonPropertyInfo;

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
                WriteObjectOrArrayStart(classType, JsonPropertyInfo.EscapedName.Value, writer, writeNull);
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

        public void WriteReferenceObjectOrArrayStart(ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, bool writeNull = false, bool writeAsReference = false, string referenceId = null)
        {
            if (JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                WriteReferenceObjectOrArrayStart(classType, JsonPropertyInfo.EscapedName.Value, writer, writeNull, writeAsReference, referenceId);
            }
            else if (KeyName != null)
            {
                JsonEncodedText propertyName = JsonEncodedText.Encode(KeyName, options.Encoder);
                WriteReferenceObjectOrArrayStart(classType, propertyName, writer, writeNull, writeAsReference, referenceId);
            }
            else
            {
                Debug.Assert(writeNull == false);
                // Write start without a property name.
                if (writeAsReference)
                {
                    writer.WriteStartObject();
                    writer.WriteString("$ref", referenceId);
                    writer.WriteEndObject();
                }
                else if (classType == ClassType.Object || classType == ClassType.Dictionary)
                {
                    writer.WriteStartObject();
                    if (referenceId != null)
                    {
                        writer.WriteString("$id", referenceId);
                    }
                    StartObjectWritten = true;
                }
                else
                {
                    Debug.Assert(classType == ClassType.Enumerable);
                    if (referenceId != null) // wrap array into an object with $id and $values metadata properties.
                    {
                        writer.WriteStartObject();
                        writer.WriteString("$id", referenceId); //it can be WriteString.
                        writer.WritePropertyName("$values");
                        WriteWrappingBraceOnEndCollection = true;
                    }
                    writer.WriteStartArray();
                }
            }
        }

        private void WriteReferenceObjectOrArrayStart(ClassType classType, JsonEncodedText propertyName, Utf8JsonWriter writer, bool writeNull, bool writeAsReference, string referenceId)
        {
            if (writeNull)
            {
                writer.WriteNull(propertyName);
            }
            else if (writeAsReference) //is a reference? write { "$ref": "1" } regardless of the type.
            {
                writer.WriteStartObject(propertyName);
                writer.WriteString("$ref", referenceId.ToString());
                writer.WriteEndObject();
            }
            else if ((classType & (ClassType.Object | ClassType.Dictionary)) != 0)
            {
                writer.WriteStartObject(propertyName);
                StartObjectWritten = true;
                if (referenceId != null)
                {
                    writer.WriteString("$id", referenceId);
                }
            }
            else
            {
                Debug.Assert(classType == ClassType.Enumerable);
                if (referenceId != null) // new reference? wrap array into an object with $id and $values metadata properties
                {
                    writer.WriteStartObject(propertyName);
                    writer.WriteString("$id", referenceId); //it can be WriteString.
                    writer.WritePropertyName("$values");
                    writer.WriteStartArray();
                    WriteWrappingBraceOnEndCollection = true;
                }
                else
                {
                    writer.WriteStartArray(propertyName);
                }
            }
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
