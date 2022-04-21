﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    public partial class JsonConverter
    {
        /// <summary>
        /// Initializes the state for polymorphic cases and returns the appropriate derived converter.
        /// </summary>
        internal JsonConverter? ResolvePolymorphicConverter(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options, ref ReadStack state)
        {
            Debug.Assert(!IsValueType);
            Debug.Assert(CanHaveMetadata);
            Debug.Assert(state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Type));
            Debug.Assert(state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted);
            Debug.Assert(jsonTypeInfo.PolymorphicTypeResolver?.UsesTypeDiscriminators == true);

            JsonConverter? polymorphicConverter = null;

            switch (state.Current.PolymorphicSerializationState)
            {
                case PolymorphicSerializationState.None:
                    Debug.Assert(!state.IsContinuation);
                    Debug.Assert(state.PolymorphicTypeDiscriminator != null);

                    PolymorphicTypeResolver resolver = jsonTypeInfo.PolymorphicTypeResolver;
                    if (resolver.TryGetDerivedJsonTypeInfo(state.PolymorphicTypeDiscriminator, out JsonTypeInfo? resolvedType))
                    {
                        Debug.Assert(TypeToConvert.IsAssignableFrom(resolvedType.Type));

                        polymorphicConverter = state.InitializePolymorphicReEntry(resolvedType);
                        if (!polymorphicConverter.CanHaveMetadata)
                        {
                            ThrowHelper.ThrowNotSupportedException_DerivedConverterDoesNotSupportMetadata(resolvedType.Type);
                        }
                    }
                    else
                    {
                        state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryNotFound;
                    }

                    state.PolymorphicTypeDiscriminator = null;
                    break;

                case PolymorphicSerializationState.PolymorphicReEntrySuspended:
                    polymorphicConverter = state.ResumePolymorphicReEntry();
                    Debug.Assert(TypeToConvert.IsAssignableFrom(polymorphicConverter.TypeToConvert));
                    break;

                case PolymorphicSerializationState.PolymorphicReEntryNotFound:
                    Debug.Assert(state.Current.PolymorphicJsonTypeInfo is null);
                    break;

                default:
                    Debug.Fail("Unexpected PolymorphicSerializationState.");
                    break;
            }

            return polymorphicConverter;
        }

        /// <summary>
        /// Initializes the state for polymorphic cases and returns the appropriate derived converter.
        /// </summary>
        internal JsonConverter? ResolvePolymorphicConverter(object value, JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Assert(!IsValueType);
            Debug.Assert(value != null && TypeToConvert.IsAssignableFrom(value.GetType()));
            Debug.Assert(CanBePolymorphic || jsonTypeInfo.PolymorphicTypeResolver != null);
            Debug.Assert(state.PolymorphicTypeDiscriminator is null);

            JsonConverter? polymorphicConverter = null;

            switch (state.Current.PolymorphicSerializationState)
            {
                case PolymorphicSerializationState.None:
                    Debug.Assert(!state.IsContinuation);

                    Type runtimeType = value.GetType();

                    if (jsonTypeInfo.PolymorphicTypeResolver is PolymorphicTypeResolver resolver)
                    {
                        Debug.Assert(CanHaveMetadata);

                        if (resolver.TryGetDerivedJsonTypeInfo(runtimeType, out JsonTypeInfo? derivedJsonTypeInfo, out string? typeDiscriminatorId))
                        {
                            polymorphicConverter = state.Current.InitializePolymorphicReEntry(derivedJsonTypeInfo);

                            if (typeDiscriminatorId is not null)
                            {
                                if (!polymorphicConverter.CanHaveMetadata)
                                {
                                    ThrowHelper.ThrowNotSupportedException_DerivedConverterDoesNotSupportMetadata(derivedJsonTypeInfo.Type);
                                }

                                state.PolymorphicTypeDiscriminator = typeDiscriminatorId;
                            }
                        }
                        else
                        {
                            state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryNotFound;
                        }
                    }
                    else
                    {
                        Debug.Assert(CanBePolymorphic);

                        if (runtimeType != TypeToConvert)
                        {
                            polymorphicConverter = state.Current.InitializePolymorphicReEntry(runtimeType, options);
                        }
                        else
                        {
                            state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryNotFound;
                        }
                    }
                    break;

                case PolymorphicSerializationState.PolymorphicReEntrySuspended:
                    Debug.Assert(state.IsContinuation);
                    polymorphicConverter = state.Current.ResumePolymorphicReEntry();
                    Debug.Assert(TypeToConvert.IsAssignableFrom(polymorphicConverter.TypeToConvert));
                    break;

                case PolymorphicSerializationState.PolymorphicReEntryNotFound:
                    Debug.Assert(state.IsContinuation);
                    break;

                default:
                    Debug.Fail("Unexpected PolymorphicSerializationState.");
                    break;
            }

            return polymorphicConverter;
        }

        internal bool TryHandleSerializedObjectReference(Utf8JsonWriter writer, object value, JsonSerializerOptions options, JsonConverter? polymorphicConverter, ref WriteStack state)
        {
            Debug.Assert(!IsValueType);
            Debug.Assert(!state.IsContinuation);
            Debug.Assert(value != null);

            switch (options.ReferenceHandlingStrategy)
            {
                case ReferenceHandlingStrategy.IgnoreCycles:
                    ReferenceResolver resolver = state.ReferenceResolver;
                    if (resolver.ContainsReferenceForCycleDetection(value))
                    {
                        writer.WriteNullValue();
                        return true;
                    }

                    resolver.PushReferenceForCycleDetection(value);
                    // WriteStack reuses root-level stackframes for its children as a performance optimization;
                    // we want to avoid writing any data for the root-level object to avoid corrupting the stack.
                    // This is fine since popping the root object at the end of serialization is not essential.
                    state.Current.IsPushedReferenceForCycleDetection = state.CurrentDepth > 0;
                    break;

                case ReferenceHandlingStrategy.Preserve:
                    bool canHaveIdMetata = polymorphicConverter?.CanHaveMetadata ?? CanHaveMetadata;
                    if (canHaveIdMetata && JsonSerializer.TryGetReferenceForValue(value, ref state, writer))
                    {
                        // We found a repeating reference and wrote the relevant metadata; serialization complete.
                        return true;
                    }
                    break;

                default:
                    Debug.Fail("Unexpected ReferenceHandlingStrategy.");
                    break;
            }

            return false;
        }
    }
}
