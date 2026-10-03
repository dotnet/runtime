// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal sealed class JsonPropertyInfo<T> : JsonPropertyInfo
    {
        private Func<object, T>? _typedGet;
        private Action<object, T>? _typedSet;

        // Whether the compiled Get/Set delegates supplied for this property understand
        // 'obj' being a StrongBox<TDeclaringType> wrapper (used internally for source-generated
        // struct types so their members can be mutated without Unsafe.Unbox). This starts out
        // unknown ('false') and is determined lazily, the first time 'obj' is actually a
        // StrongBox<TDeclaringType>, by InvokeGetter/InvokeSetter below. Delegates compiled by a
        // source generator version that predates StrongBox-based struct accessors (or a resolver
        // modifier written against that historical "obj is a boxed TDeclaringType" contract) still
        // expect a plain boxed TDeclaringType, so we detect that case, cache it, and bridge
        // into/out of the box via a one-time unbox/rebox instead of permanently breaking those
        // delegates.
        private bool _getterRequiresLegacyUnbox;
        private bool _setterRequiresLegacyUnbox;

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
                return;
            }

            Func<object, T> rawGetter = getter is Func<object, T> typedGetter
                ? typedGetter
                : (obj => (T)((Func<object, object?>)getter)(obj)!);

            // _untypedGet keeps the exact delegate instance the caller supplied so that the
            // public JsonPropertyInfo.Get getter/setter pair (below in the base class) preserves
            // reference identity, e.g. 'propertyInfo.Get = someDelegate; Assert.Same(someDelegate,
            // propertyInfo.Get)'. Only _typedGet (used internally by GetValueAsObject and by
            // serialization/deserialization) is routed through InvokeGetter for StrongBox
            // compatibility; the untyped accessor is bridged separately through
            // JsonPropertyInfo.GetValueAsObject wherever internal code needs the compatibility
            // handling (e.g. TryGetPrePopulatedValue).
            _typedGet = obj => InvokeGetter(rawGetter, obj);
            _untypedGet = getter is Func<object, object?> untypedGetter ? untypedGetter : obj => rawGetter(obj);
        }

        private protected override void SetSetter(Delegate? setter)
        {
            Debug.Assert(setter is null or Action<object, object?> or Action<object, T>);
            Debug.Assert(!IsConfigured);

            if (setter is null)
            {
                _typedSet = null;
                _untypedSet = null;
                return;
            }

            Action<object, T> rawSetter = setter is Action<object, T> typedSetter
                ? typedSetter
                : (obj, value) => ((Action<object, object?>)setter)(obj, value);

            // See the identity-preservation comment in SetGetter above: _untypedSet keeps the
            // exact delegate instance the caller supplied whenever that shape is untyped, and
            // internal StrongBox-compatibility handling is applied via _typedSet (used by
            // GetValueAsObject/SetValueAsObject wherever internal code needs it) instead.
            _typedSet = (obj, value) => InvokeSetter(rawSetter, obj, value);
            _untypedSet = setter is Action<object, object?> untypedSetter ? untypedSetter : (obj, value) => rawSetter(obj, (T)value!);
        }

        // See the comment on _getterRequiresLegacyUnbox for background.
        private T InvokeGetter(Func<object, T> rawGetter, object obj)
        {
            if (_getterRequiresLegacyUnbox)
            {
                return rawGetter(((IStrongBox)obj).Value!);
            }

            if (obj is not IStrongBox strongBox)
            {
                // The overwhelmingly common case: 'obj' is either a reference-type instance or a
                // plain boxed value type (e.g. reflection-based accessors always pass this shape).
                return rawGetter(obj);
            }

            try
            {
                // The common source-generated struct case: the compiled getter was itself
                // generated to understand StrongBox<TDeclaringType>.
                return rawGetter(obj);
            }
            catch (InvalidCastException)
            {
                // 'rawGetter' does not understand StrongBox<TDeclaringType> and expects a plain
                // boxed TDeclaringType instead. Remember this so future calls skip straight to the
                // compatible path below.
                _getterRequiresLegacyUnbox = true;
                return rawGetter(strongBox.Value!);
            }
        }

        // See the comment on _setterRequiresLegacyUnbox for background.
        private void InvokeSetter(Action<object, T> rawSetter, object obj, T value)
        {
            if (_setterRequiresLegacyUnbox)
            {
                SetViaLegacyUnbox(rawSetter, (IStrongBox)obj, value);
                return;
            }

            if (obj is not IStrongBox strongBox)
            {
                rawSetter(obj, value);
                return;
            }

            try
            {
                rawSetter(obj, value);
            }
            catch (InvalidCastException)
            {
                _setterRequiresLegacyUnbox = true;
                SetViaLegacyUnbox(rawSetter, strongBox, value);
            }
        }

        private static void SetViaLegacyUnbox(Action<object, T> rawSetter, IStrongBox strongBox, T value)
        {
            // 'rawSetter' can only mutate a genuine boxed TDeclaringType in place (that's the only
            // reason it predates StrongBox<TDeclaringType> support), so box/rebox once here to
            // bridge into and out of the StrongBox<TDeclaringType> that the runtime already
            // maintains for tracking mutations to source-generated struct types.
            object boxed = strongBox.Value!;
            rawSetter(boxed, value);
            strongBox.Value = boxed;
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
        internal override void AddJsonParameterInfo(JsonParameterInfoValues parameterInfoValues)
        {
            Debug.Assert(!IsConfigured);
            Debug.Assert(AssociatedParameter is null);

            AssociatedParameter = new JsonParameterInfo<T>(parameterInfoValues, this);
            // Overwrite the nullability annotation of property setter with the parameter.
            _isSetNullable = parameterInfoValues.IsNullable;

            if (Options.RespectRequiredConstructorParameters)
            {
                // If the property has been associated with a non-optional parameter, mark it as required.
                _isRequired |= AssociatedParameter.IsRequiredParameter;
            }
        }

        internal new JsonConverter<T> EffectiveConverter
        {
            get
            {
                Debug.Assert(_typedEffectiveConverter is not null);
                return _typedEffectiveConverter;
            }
        }

        private JsonConverter<T>? _typedEffectiveConverter;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal override void DetermineReflectionPropertyAccessors(MemberInfo memberInfo, bool useNonPublicAccessors)
            => DefaultJsonTypeInfoResolver.DeterminePropertyAccessors<T>(this, memberInfo, useNonPublicAccessors);

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

        internal override void SetValueAsObject(object obj, object? value)
        {
            Debug.Assert(HasSetter);
            Set!(obj, (T)value!);
        }

        internal override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            T value = Get!(obj);

            if (
#if NET
                !typeof(T).IsValueType && // treated as a constant by recent versions of the JIT.
#else
                !EffectiveConverter.IsValueType &&
#endif
                Options.ReferenceHandlingStrategy == JsonKnownReferenceHandler.IgnoreCycles &&
                value is not null &&
                !state.IsContinuation &&
                // .NET types that are serialized as JSON primitive values don't need to be tracked for cycle detection e.g: string.
                EffectiveConverter.ConverterStrategy != ConverterStrategy.Value &&
                state.ReferenceResolver.ContainsReferenceForCycleDetection(value))
            {
                // If a reference cycle is detected, treat value as null.
                value = default!;
                Debug.Assert(value is null);
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

                if (!IsGetNullable && Options.RespectNullableAnnotations)
                {
                    ThrowHelper.ThrowJsonException_PropertyGetterDisallowNull(Name, state.Current.JsonTypeInfo.Type);
                }

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

            if (value is null)
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
                        ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(EffectiveConverter.Type);
                    }

                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.Type);
                }

                if (!IgnoreNullTokensOnRead)
                {
                    if (!IsSetNullable && Options.RespectNullableAnnotations)
                    {
                        ThrowHelper.ThrowJsonException_PropertySetterDisallowNull(Name, state.Current.JsonTypeInfo.Type);
                    }

                    T? value = default;
                    Set!(obj, value!);
                }

                success = true;
                state.Current.MarkPropertyAsRead(this);
            }
            else if (EffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling is null)
            {
                // CanUseDirectReadOrWrite == false when using streams
                Debug.Assert(!state.IsContinuation);
                Debug.Assert(EffectiveObjectCreationHandling != JsonObjectCreationHandling.Populate, "Populating should not be possible for simple types");

                if (!isNullToken || !IgnoreNullTokensOnRead || default(T) is not null)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    T? fastValue = EffectiveConverter.Read(ref reader, PropertyType, Options);

                    if (fastValue is null && !IsSetNullable && Options.RespectNullableAnnotations)
                    {
                        ThrowHelper.ThrowJsonException_PropertySetterDisallowNull(Name, state.Current.JsonTypeInfo.Type);
                    }

                    Set!(obj, fastValue!);
                }

                success = true;
                state.Current.MarkPropertyAsRead(this);
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
                                if (value is null && !IsSetNullable && Options.RespectNullableAnnotations)
                                {
                                    ThrowHelper.ThrowJsonException_PropertySetterDisallowNull(Name, state.Current.JsonTypeInfo.Type);
                                }

                                Set!(obj, value!);
                            }
                        }

                        state.Current.MarkPropertyAsRead(this);
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
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.Type);
                }

                value = default(T);
                success = true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (EffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling is null)
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

                case JsonIgnoreCondition.WhenWriting:
                    ShouldSerialize = ShouldSerializeIgnoreConditionAlways;
                    break;

                case JsonIgnoreCondition.WhenReading:
                    Set = null;
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
