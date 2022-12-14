// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters
{
    internal abstract class JsonAdvancedConverter<T> : JsonResumableConverter<T>
    {
        internal virtual bool IsJsonDictionaryConverter => false;

        internal virtual bool OnWriteResume(Utf8JsonWriter writer, T dictionary, JsonSerializerOptions options, ref WriteStack state)
        {
            Debug.Fail("We should have overriden it if we got here");
            return true;
        }
    }

    /// <summary>
    /// Base class for object, enumerable and dictionary converters
    /// </summary>
    internal abstract class JsonAdvancedConverter<T, IntermediateType> : JsonAdvancedConverter<T>
    {
        private protected virtual bool TryCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out IntermediateType? obj)
        {
            // TODO: generalize exceptions
            if (jsonTypeInfo.CreateObject == null)
            {
                // The contract model was not able to produce a default constructor for two possible reasons:
                // 1. Either the declared collection type is abstract and cannot be instantiated.
                // 2. The collection type does not specify a default constructor.
                if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
                {
                    ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(TypeToConvert, ref reader, ref state);
                }
            }

            obj = (IntermediateType)jsonTypeInfo.CreateObject();

            if (IsReadOnly(obj))
            {
                // TODO: not possible for objects, might be a good idea to still generalize exception
                ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
            }

            return true;
        }

        private protected bool TryGetOrCreateObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, [NotNullWhen(true)] out IntermediateType? obj)
        {
            Debug.Assert((!state.SupportContinuation && !state.Current.CanContainMetadata) || state.Current.ObjectState < StackFrameObjectState.CreatedObject,
                $"SupportsContinuation: {state.SupportContinuation}, CanContainMetadata: {state.Current.CanContainMetadata}, ObjectState: {state.Current.ObjectState}");
            Debug.Assert(!state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Ref), "References should already be resolved");

            if (TryCreateObject(ref reader, jsonTypeInfo, ref state, out obj))
            {
                if (state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Id))
                {
                    Debug.Assert(state.ReferenceId != null);
                    Debug.Assert(jsonTypeInfo.Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve);
                    state.ReferenceResolver.AddReference(state.ReferenceId, obj);
                    state.ReferenceId = null;
                }

                return true;
            }

            return false;
        }

        private protected virtual bool IsReadOnly(object obj) => false;

        private protected abstract bool TryPopulate(ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state, ref IntermediateType obj);

        private protected virtual bool TryConvert(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo, scoped ref ReadStack state, IntermediateType obj, out T value)
        {
            // TODO: consider removing this method
            // Unbox
            Debug.Assert(obj != null);
            value = (T)(object)obj;
            return true;
        }
    }
}
