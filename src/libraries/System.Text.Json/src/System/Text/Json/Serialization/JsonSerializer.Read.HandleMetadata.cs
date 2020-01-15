// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static void HandleMetadataPropertyValue(ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowJsonException_MetadataValueWasNotString();
            }

            MetadataPropertyName metadata = state.Current.MetadataProperty;
            string key = reader.GetString()!;

            if (metadata == MetadataPropertyName.Id)
            {
                state.ReferenceResolver.AddReference(key, GetValueToPreserve(ref state));
            }
            else if (metadata == MetadataPropertyName.Ref)
            {
                state.Current.ReferenceId = key;
            }

            state.Current.ReadMetadataValue = false;
        }

        private static object GetValueToPreserve(ref ReadStack state)
        {
            return state.Current.IsProcessingProperty(ClassType.Dictionary) ?
                state.Current.JsonPropertyInfo!.GetValueAsObject(state.Current.ReturnValue)! : state.Current.ReturnValue!;
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

                ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingSign(propertyName, ref state, in reader);
            }

            return MetadataPropertyName.NoMetadata;
        }

        private static void HandleReference(ref ReadStack state)
        {
            object referenceValue = state.ReferenceResolver.ResolveReference(state.Current.ReferenceId);
            if (state.Current.IsProcessingProperty(ClassType.Dictionary))
            {
                ApplyObjectToEnumerable(referenceValue, ref state, setPropertyDirectly: true);
                state.Current.EndProperty();
            }
            else
            {
                state.Current.ReturnValue = referenceValue;
                HandleEndObject(ref state);
            }

            state.Current.ShouldHandleReference = false;
        }

        private static JsonPropertyInfo GetValuesFromJsonPreservedReference(ref ReadStackFrame current)
        {
            Debug.Assert(current.JsonClassInfo!.PropertyCacheArray![0] == current.JsonClassInfo.PropertyCache!["Values"]);
            return current.JsonClassInfo.PropertyCacheArray[0];
        }
    }
}
