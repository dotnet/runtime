// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    internal struct WriteStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated.
        public object CurrentValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

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
        }

        public void WriteObjectOrArrayStart(ClassType classType, Utf8JsonWriter writer, bool writeNull = false)
        {
            if (JsonPropertyInfo?._escapedName != null)
            {
                WriteObjectOrArrayStart(classType, JsonPropertyInfo?._escapedName, writer, writeNull);
            }
            else if (KeyName != null)
            {
                byte[] pooledKey = null;
                byte[] utf8Key = Encoding.UTF8.GetBytes(KeyName);
                int length = JsonWriterHelper.GetMaxEscapedLength(utf8Key.Length, 0);

                Span<byte> escapedKey = length <= JsonConstants.StackallocThreshold ?
                    stackalloc byte[length] :
                    (pooledKey = ArrayPool<byte>.Shared.Rent(length));

                JsonWriterHelper.EscapeString(utf8Key, escapedKey, 0, out int written);
                Span<byte> propertyName = escapedKey.Slice(0, written);

                WriteObjectOrArrayStart(classType, propertyName, writer, writeNull);

                if (pooledKey != null)
                {
                    ArrayPool<byte>.Shared.Return(pooledKey);
                }
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

        private void WriteObjectOrArrayStart(ClassType classType, ReadOnlySpan<byte> propertyName, Utf8JsonWriter writer, bool writeNull)
        {
            if (writeNull)
            {
                writer.WriteNull(propertyName);
            }
            else if (classType == ClassType.Object || classType == ClassType.Dictionary)
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
            PopStackOnEndObject = false;
            PopStackOnEnd = false;
            StartObjectWritten = false;
        }

        public void EndObject()
        {
            PropertyIndex = 0;
            PopStackOnEndObject = false;
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
