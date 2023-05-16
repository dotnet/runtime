// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal sealed class JsonPropertyInfo<T> : JsonPropertyInfo
    {
        private Func<object, T>? _typedGet;
        private Action<object, T>? _typedSet;

        internal JsonPropertyInfo(Type declaringType, JsonTypeInfo? declaringTypeInfo, JsonSerializerOptions options)
            : base(declaringType, propertyType: typeof(T), declaringTypeInfo, options)
        {
        }

        internal new Func<object, T>? Get
        {
            get => _typedGet;
            set => SetGetter(value);
        }

        internal new Action<object, T>? Set
        {
            get => _typedSet;
            set => SetSetter(value);
        }

        private protected override void SetGetter(Delegate? getter)
        {
            Debug.Assert(getter is null or Func<object, object?> or Func<object, T>);
            Debug.Assert(!IsConfigured);

            if (getter is null)
            {
                _typedGet = null;
                _untypedGet = null;
            }
            else if (getter is Func<object, T> typedGetter)
            {
                _typedGet = typedGetter;
                _untypedGet = getter is Func<object, object?> untypedGet ? untypedGet : obj => typedGetter(obj);
            }
            else
            {
                Func<object, object?> untypedGet = (Func<object, object?>)getter;
                _typedGet = (obj => (T)untypedGet(obj)!);
                _untypedGet = untypedGet;
            }
        }

        private protected override void SetSetter(Delegate? setter)
        {
            Debug.Assert(setter is null or Action<object, object?> or Action<object, T>);
            Debug.Assert(!IsConfigured);

            if (setter is null)
            {
                _typedSet = null;
                _untypedSet = null;
            }
            else if (setter is Action<object, T> typedSetter)
            {
                _typedSet = typedSetter;
                _untypedSet = setter is Action<object, object?> untypedSet ? untypedSet : (obj, value) => typedSetter(obj, (T)value!);
            }
            else
            {
                Action<object, object?> untypedSet = (Action<object, object?>)setter;
                _typedSet = ((obj, value) => untypedSet(obj, value));
                _untypedSet = untypedSet;
            }
        }

        internal new Func<object, T?, bool>? ShouldSerialize
        {
            get => _shouldSerializeTyped;
            set => SetShouldSerialize(value);
        }

        private Func<object, T?, bool>? _shouldSerializeTyped;

        private protected override void SetShouldSerialize(Delegate? predicate)
        {
            Debug.Assert(predicate is null or Func<object, object?, bool> or Func<object, T?, bool>);
            Debug.Assert(!IsConfigured);

            if (predicate is null)
            {
                _shouldSerializeTyped = null;
                _shouldSerialize = null;
            }
            else if (predicate is Func<object, T?, bool> typedPredicate)
            {
                _shouldSerializeTyped = typedPredicate;
                _shouldSerialize = typedPredicate is Func<object, object?, bool> untypedPredicate ? untypedPredicate : (obj, value) => typedPredicate(obj, (T?)value);
            }
            else
            {
                Func<object, object?, bool> untypedPredicate = (Func<object, object?, bool>)predicate;
                _shouldSerializeTyped = (obj, value) => untypedPredicate(obj, value);
                _shouldSerialize = untypedPredicate;
            }
        }

        internal override object? DefaultValue => default(T);
        internal override bool PropertyTypeCanBeNull => default(T) is null;
        internal override JsonParameterInfo CreateJsonParameterInfo(JsonParameterInfoValues parameterInfoValues)
            => new JsonParameterInfo<T>(parameterInfoValues, this);

        internal new JsonConverter<T> EffectiveConverter
        {
            get
            {
                Debug.Assert(_typedEffectiveConverter != null);
                return _typedEffectiveConverter;
            }
        }

        private JsonConverter<T>? _typedEffectiveConverter;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal override void DetermineReflectionPropertyAccessors(MemberInfo memberInfo)
            => DefaultJsonTypeInfoResolver.DeterminePropertyAccessors<T>(this, memberInfo);
        private protected override void DetermineEffectiveConverter(JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo is JsonTypeInfo<T>);

            JsonConverter<T> converter =
                Options.ExpandConverterFactory(CustomConverter, PropertyType) // Expand any property-level custom converters.
                ?.CreateCastingConverter<T>()                                 // Cast to JsonConverter<T>, potentially with wrapping.
                ?? ((JsonTypeInfo<T>)jsonTypeInfo).EffectiveConverter;        // Fall back to the effective converter for the type.

            _effectiveConverter = converter;
            _typedEffectiveConverter = converter;
        }

        internal override object? GetValueAsObject(object obj)
        {
            if (IsForTypeInfo)
            {
                return obj;
            }

            Debug.Assert(HasGetter);
            return Get!(obj);
        }

        internal override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            T value = Get!(obj);

            if (
#if NETCOREAPP
                !typeof(T).IsValueType && // treated as a constant by recent versions of the JIT.
#else
                !EffectiveConverter.IsValueType &&
#endif
                Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                value is not null &&
                !state.IsContinuation &&
                // .NET types that are serialized as JSON primitive values don't need to be tracked for cycle detection e.g: string.
                EffectiveConverter.ConverterStrategy != ConverterStrategy.Value &&
                state.ReferenceResolver.ContainsReferenceForCycleDetection(value))
            {
                // If a reference cycle is detected, treat value as null.
                value = default!;
                Debug.Assert(value == null);
            }

            if (IgnoreDefaultValuesOnWrite)
            {
                // Fast path `ShouldSerialize` check when using JsonIgnoreCondition.WhenWritingNull/Default configuration
                if (IsDefaultValue(value))
                {
                    return true;
                }
            }
            else if (ShouldSerialize?.Invoke(obj, value) == false)
            {
                // We return true here.
                // False means that there is not enough data.
                return true;
            }

            if (value is null)
            {
                Debug.Assert(PropertyTypeCanBeNull);

                if (EffectiveConverter.HandleNullOnWrite)
                {
                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;
                        writer.WritePropertyNameSection(EscapedNameSection);
                    }

                    int originalDepth = writer.CurrentDepth;
                    EffectiveConverter.Write(writer, value, Options);
                    if (originalDepth != writer.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterWrite(EffectiveConverter);
                    }
                }
                else
                {
                    writer.WriteNullSection(EscapedNameSection);
                }

                return true;
            }
            else
            {
                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    state.Current.PropertyState = StackFramePropertyState.Name;
                    writer.WritePropertyNameSection(EscapedNameSection);
                }

                return EffectiveConverter.TryWrite(writer, value, Options, ref state);
            }
        }

        internal override bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            bool success;
            T value = Get!(obj);

            if (ShouldSerialize?.Invoke(obj, value) == false)
            {
                // We return true here.
                // False means that there is not enough data.
                return true;
            }

            if (value == null)
            {
                success = true;
            }
            else
            {
                success = EffectiveConverter.TryWriteDataExtensionProperty(writer, value, Options, ref state);
            }

            return success;
        }

        internal override bool ReadJsonAndSetMember(object obj, scoped ref ReadStack state, ref Utf8JsonReader reader)
        {
            bool success;

            bool isNullToken = reader.TokenType == JsonTokenType.Null;

            if (isNullToken && !EffectiveConverter.HandleNullOnRead && !state.IsContinuation)
            {
                if (default(T) is not null || !CanDeserialize)
                {
                    if (default(T) is null)
                    {
                        Debug.Assert(CanDeserialize || EffectiveObjectCreationHandling == JsonObjectCreationHandling.Populate);
                        ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(EffectiveConverter.TypeToConvert);
                    }

                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.TypeToConvert);
                }

                if (!IgnoreNullTokensOnRead)
                {
                    T? value = default;
                    Set!(obj, value!);
                }

                success = true;
                state.Current.MarkRequiredPropertyAsRead(this);
            }
            else if (EffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // CanUseDirectReadOrWrite == false when using streams
                Debug.Assert(!state.IsContinuation);
                Debug.Assert(EffectiveObjectCreationHandling != JsonObjectCreationHandling.Populate, "Populating should not be possible for simple types");

                if (!isNullToken || !IgnoreNullTokensOnRead || default(T) is not null)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    T? fastValue = EffectiveConverter.Read(ref reader, PropertyType, Options);
                    Set!(obj, fastValue!);
                }

                success = true;
                state.Current.MarkRequiredPropertyAsRead(this);
            }
            else
            {
                success = true;
                if (!isNullToken || !IgnoreNullTokensOnRead || default(T) is not null || state.IsContinuation)
                {
                    state.Current.ReturnValue = obj;

                    success = EffectiveConverter.TryRead(ref reader, PropertyType, Options, ref state, out T? value, out bool populatedValue);
                    if (success)
                    {
                        if (typeof(T).IsValueType || !populatedValue)
                        {
                            // note: populatedValue value may be different than when CreationHandling is Populate
                            //       i.e. when initial value of property is null

                            // We cannot do reader.Skip early because converter decides if populating will happen or not
                            if (CanDeserialize)
                            {
                                Set!(obj, value!);
                            }
                        }

                        state.Current.MarkRequiredPropertyAsRead(this);
                    }
                }
            }

            return success;
        }

        internal override bool ReadJsonAsObject(scoped ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !EffectiveConverter.HandleNullOnRead && !state.IsContinuation)
            {
                if (default(T) is not null)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.TypeToConvert);
                }

                value = default(T);
                success = true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (EffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
                {
                    // CanUseDirectReadOrWrite == false when using streams
                    Debug.Assert(!state.IsContinuation);

                    value = EffectiveConverter.Read(ref reader, PropertyType, Options);
                    success = true;
                }
                else
                {
                    success = EffectiveConverter.TryRead(ref reader, PropertyType, Options, ref state, out T? typedValue, out _);
                    value = typedValue;
                }
            }

            return success;
        }

        private protected override void ConfigureIgnoreCondition(JsonIgnoreCondition? ignoreCondition)
        {
            switch (ignoreCondition)
            {
                case null:
                    break;

                case JsonIgnoreCondition.Never:
                    ShouldSerialize = ShouldSerializeIgnoreConditionNever;
                    break;

                case JsonIgnoreCondition.Always:
                    ShouldSerialize = ShouldSerializeIgnoreConditionAlways;
                    break;

                case JsonIgnoreCondition.WhenWritingNull:
                    if (PropertyTypeCanBeNull)
                    {
                        ShouldSerialize = ShouldSerializeIgnoreWhenWritingDefault;
                        IgnoreDefaultValuesOnWrite = true;
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(MemberName!, DeclaringType);
                    }
                    break;

                case JsonIgnoreCondition.WhenWritingDefault:
                    ShouldSerialize = ShouldSerializeIgnoreWhenWritingDefault;
                    IgnoreDefaultValuesOnWrite = true;
                    break;

                default:
                    Debug.Fail($"Unknown value of JsonIgnoreCondition '{ignoreCondition}'");
                    break;
            }

            static bool ShouldSerializeIgnoreConditionNever(object _, T? value) => true;
            static bool ShouldSerializeIgnoreConditionAlways(object _, T? value) => false;
            static bool ShouldSerializeIgnoreWhenWritingDefault(object _, T? value)
            {
                return default(T) is null ? value is not null : !EqualityComparer<T>.Default.Equals(default, value);
            }
        }

        private static bool IsDefaultValue(T? value)
        {
            return default(T) is null ? value is null : EqualityComparer<T>.Default.Equals(default, value);
        }
    }
}
