// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;

namespace System.Text.Json
{
    internal delegate ResolvedReferenceHandling ReferenceHandlingStrategy(ref WriteStack state, out string referenceId, out bool writeAsReference, object value);

    internal delegate void WriteStart(ref WriteStackFrame frame, ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, bool writeNull = false, bool writeAsReference = false, string referenceId = null);

    internal delegate void PopReference(ref WriteStack state, bool isCollectionProperty);

    public static partial class JsonSerializer
    {
        internal static ResolvedReferenceHandling PreserveReferencesStrategy(ref WriteStack state, out string referenceId, out bool skip, object value)
        {
            // Avoid emitting metadata to value types.
            Type currentType = state.Current.JsonPropertyInfo?.DeclaredPropertyType ?? state.Current.JsonClassInfo.Type;
            if (currentType.IsValueType)
            {
                referenceId = null;
                skip = false;
                return ResolvedReferenceHandling.None;
            }

            if (skip = state.GetPreservedReference(value, out referenceId))
            {
                return ResolvedReferenceHandling.IsReference;
            }

            return ResolvedReferenceHandling.Preserve;
        }

        internal static ResolvedReferenceHandling IgnoreReferencesStrategy(ref WriteStack state, out string referenceId, out bool skip, object value)
        {
            if (!state.AddStackReference(value))
            {
                //if reference wasn't added to the set, it means it was already there, therefore we should ignore it BUT not remove it from the set in order to keep validating against further references.
                state.Current.KeepReferenceInSet = true;
                skip = true;
                referenceId = default;
                return ResolvedReferenceHandling.Ignore;
            }

            skip = default;
            referenceId = default;
            return ResolvedReferenceHandling.None;
        }

        internal static ResolvedReferenceHandling DefaultOnReferencesStrategy(ref WriteStack state, out string referenceId, out bool skip, object value)
        {
            //params not meant for this code path.
            skip = default;
            referenceId = default;
            return ResolvedReferenceHandling.None;
        }

        internal static void WriteObjectOrArrayStart(ref WriteStackFrame frame, ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, bool writeNull = false, bool writeAsReference = false, string referenceId = null)
        {
            if (frame.JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                WriteObjectOrArrayStart(ref frame, classType, frame.JsonPropertyInfo.EscapedName.Value, writer, writeNull);
            }
            else if (frame.KeyName != null)
            {
                JsonEncodedText propertyName = JsonEncodedText.Encode(frame.KeyName, options.Encoder);
                WriteObjectOrArrayStart(ref frame, classType, propertyName, writer, writeNull);
            }
            else
            {
                Debug.Assert(writeNull == false);

                // Write start without a property name.
                if (classType == ClassType.Object || classType == ClassType.Dictionary)
                {
                    writer.WriteStartObject();
                    frame.StartObjectWritten = true;
                }
                else
                {
                    Debug.Assert(classType == ClassType.Enumerable);
                    writer.WriteStartArray();
                }
            }
        }

        private static void WriteObjectOrArrayStart(ref WriteStackFrame frame, ClassType classType, JsonEncodedText propertyName, Utf8JsonWriter writer, bool writeNull)
        {
            if (writeNull)
            {
                writer.WriteNull(propertyName);
            }
            else if ((classType & (ClassType.Object | ClassType.Dictionary)) != 0)
            {
                writer.WriteStartObject(propertyName);
                frame.StartObjectWritten = true;
            }
            else
            {
                Debug.Assert(classType == ClassType.Enumerable);
                writer.WriteStartArray(propertyName);
            }
        }

        private static void WriteReferenceObjectOrArrayStart(ref WriteStackFrame frame, ClassType classType, Utf8JsonWriter writer, JsonSerializerOptions options, bool writeNull = false, bool writeAsReference = false, string referenceId = null)
        {
            if (frame.JsonPropertyInfo?.EscapedName.HasValue == true)
            {
                WriteReferenceObjectOrArrayStart(ref frame, classType, frame.JsonPropertyInfo.EscapedName.Value, writer, writeNull, writeAsReference, referenceId);
            }
            else if (frame.KeyName != null)
            {
                JsonEncodedText propertyName = JsonEncodedText.Encode(frame.KeyName, options.Encoder);
                WriteReferenceObjectOrArrayStart(ref frame, classType, propertyName, writer, writeNull, writeAsReference, referenceId);
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
                    frame.StartObjectWritten = true;
                }
                else
                {
                    Debug.Assert(classType == ClassType.Enumerable);
                    if (referenceId != null) // wrap array into an object with $id and $values metadata properties.
                    {
                        writer.WriteStartObject();
                        writer.WriteString("$id", referenceId); //it can be WriteString.
                        writer.WritePropertyName("$values");
                        frame.WriteWrappingBraceOnEndCollection = true;
                    }
                    writer.WriteStartArray();
                }
            }
        }

        private static void WriteReferenceObjectOrArrayStart(ref WriteStackFrame frame, ClassType classType, JsonEncodedText propertyName, Utf8JsonWriter writer, bool writeNull, bool writeAsReference, string referenceId)
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
                frame.StartObjectWritten = true;
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
                    frame.WriteWrappingBraceOnEndCollection = true;
                }
                else
                {
                    writer.WriteStartArray(propertyName);
                }
            }
        }

        private static void PopReference(ref WriteStack state, bool isCollectionProperty)
        {
            if (!state.Current.KeepReferenceInSet) // Only remove objects that are the first reference in the stack.
            {
                object value = isCollectionProperty ?
                    (IEnumerable)state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.CurrentValue) :
                    state.Current.CurrentValue;

                state.PopStackReference(value);
            }
        }

        internal static void DefaultPopReference(ref WriteStack state, bool isCollectionProperty)
        {
            //Nothig to do when opting in for the default behavior.
        }
    }
}
