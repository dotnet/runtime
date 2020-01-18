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
            Debug.Assert(state.Current.JsonClassInfo!.Options.ReferenceHandling.ShouldReadPreservedReferences());

            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
            }

            MetadataPropertyName metadata = state.Current.LastSeenMetadataProperty;
            string key = reader.GetString()!;
            Debug.Assert(metadata == MetadataPropertyName.Id || metadata == MetadataPropertyName.Ref);

            if (metadata == MetadataPropertyName.Id)
            {
                // Special case for dictionary properties since those do not push into the ReadStack.
                // There is no need to check for enumerables since those will always be wrapped into JsonPreservableArrayReference<T> which turns enumerables into objects.
                object value = state.Current.IsProcessingProperty(ClassType.Dictionary) ?
                    state.Current.JsonPropertyInfo!.GetValueAsObject(state.Current.ReturnValue)! :
                    state.Current.ReturnValue!;

                state.ReferenceResolver.AddReferenceOnDeserialize(key, value);
            }
            else if (metadata == MetadataPropertyName.Ref)
            {
                state.Current.ReferenceId = key;
            }
        }

        private static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(state.Current.JsonClassInfo!.Options.ReferenceHandling.ShouldReadPreservedReferences());

            if (state.Current.ReferenceId != null)
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
            }

            if (propertyName.Length > 0 && propertyName[0] == '$')
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
                        // Only enumerables wrapped in JsonPreservableArrayReference<T> are allowed to understand $values as metadata.
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

                ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, in reader);
            }

            return MetadataPropertyName.NoMetadata;
        }

        private static void HandleReference(ref ReadStack state)
        {
            Debug.Assert(state.Current.JsonClassInfo!.Options.ReferenceHandling.ShouldReadPreservedReferences());

            object referenceValue = state.ReferenceResolver.ResolveReferenceOnDeserialize(state.Current.ReferenceId!);
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

            // Set back to null to no longer treat subsequent objects as references.
            state.Current.ReferenceId = null;
        }

        internal static JsonPropertyInfo GetValuesPropertyInfoFromJsonPreservableArrayRef(ref ReadStackFrame current)
        {
            Debug.Assert(current.JsonClassInfo!.Options.ReferenceHandling.ShouldReadPreservedReferences());
            Debug.Assert(current.JsonClassInfo.Type.GetGenericTypeDefinition() == typeof(JsonPreservableArrayReference<>));

            JsonPropertyInfo info = current.JsonClassInfo.PropertyCacheArray![0];

            Debug.Assert(info == current.JsonClassInfo.PropertyCache!["Values"]);
            Debug.Assert(info.ClassType == ClassType.Enumerable);

            return info;
        }
    }
}
