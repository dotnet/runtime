// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static void HandleMetadataPropertyValue(ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Value for metadata properties cannot be other than string.");
            }

            MetadataPropertyName metadata = state.Current.MetadataProperty;
            string key = reader.GetString();

            if (metadata == MetadataPropertyName.Id)
            {
                state.AddReference(key, GetValueToPreserve(ref state));
            }
            else if (metadata == MetadataPropertyName.Ref)
            {
                state.Current.ReferenceId = key;
            }

            state.Current.ReadMetadataValue = false;
        }

        private static object GetValueToPreserve(ref ReadStack state)
        {
            return state.Current.IsProcessingProperty(ClassType.Dictionary) ? state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) : state.Current.ReturnValue;
        }

        internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (propertyName[0] == '$')
            {
                switch (propertyName.Length)
                {
                    case 3:
                        if (propertyName[1] == 'i' &&
                            propertyName[2] == 'd')
                        {
                            return MetadataPropertyName.Id;
                        }
                        break;

                    case 4:
                        if (propertyName[1] == 'r' &&
                            propertyName[2] == 'e' &&
                            propertyName[3] == 'f')
                        {
                            return MetadataPropertyName.Ref;
                        }
                        break;

                    case 7:
                        // Only Preserved Arrays are allowed to read $values as metadata.
                        if (state.Current.IsPreservedArray &&
                            propertyName[1] == 'v' &&
                            propertyName[2] == 'a' &&
                            propertyName[3] == 'l' &&
                            propertyName[4] == 'u' &&
                            propertyName[5] == 'e' &&
                            propertyName[6] == 's')
                        {
                            return MetadataPropertyName.Values;
                        }
                        break;
                }

                // Fail state.
                // Set PropertyInfo or KeyName to write down the conflicting property name in JsonException.Path
                if (!state.Current.IsProcessingDictionary())
                {
                    JsonPropertyInfo info = JsonPropertyInfo.s_metadataProperty;
                    info.JsonPropertyName = propertyName.ToArray();
                    state.Current.JsonPropertyInfo = info;
                }
                else
                {
                    state.Current.KeyName = reader.GetString();
                }

                throw new JsonException("Properties that start with '$' are not allowed on preserve mode, you must either escape '$' or turn off preserve references.");
            }

            return MetadataPropertyName.NoMetadata;
        }

        private static void HandleReference(JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader)
        {
            object referenceValue = state.ResolveReference(state.Current.ReferenceId);
            if (state.Current.IsProcessingProperty(ClassType.Dictionary))
            {
                ApplyObjectToEnumerable(referenceValue, ref state, setPropertyDirectly: true);
                state.Current.EndProperty();
            }
            else
            {
                state.Current.ReturnValue = referenceValue;
                HandleEndObjectRef(ref state);
            }

            state.Current.ShouldHandleReference = false;
        }

        internal static void SetAsPreserved(ref ReadStackFrame frame)
        {
            bool alreadyPreserving;
            if (frame.IsProcessingProperty(ClassType.Dictionary))
            {
                alreadyPreserving = frame.DictionaryPropertyIsPreserved;
                frame.DictionaryPropertyIsPreserved = true;
            }
            else
            {
                alreadyPreserving = frame.IsPreserved;
                frame.IsPreserved = true;
            }

            if (alreadyPreserving)
            {
                throw new JsonException("Object already defines a reference identifier.");
            }
        }
    }

    internal enum MetadataPropertyName
    {
        NoMetadata,
        Values,
        Id,
        Ref,
    }
}
