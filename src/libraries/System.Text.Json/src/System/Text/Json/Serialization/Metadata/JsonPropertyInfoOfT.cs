// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    /// <typeparamref name="T"/> is the <see cref="JsonConverter{T}.TypeToConvert"/> for either the property's converter,
    /// or a type's converter, if the current instance is a <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/>.
    internal sealed class JsonPropertyInfo<T> : JsonPropertyInfo
    {
        /// <summary>
        /// Returns true if the property's converter is external (a user's custom converter)
        /// and the type to convert is not the same as the declared property type (polymorphic).
        /// Used to determine whether to perform additional validation on the value returned by the
        /// converter on deserialization.
        /// </summary>
        private bool _converterIsExternalAndPolymorphic;

        // Since a converter's TypeToConvert (which is the T value in this type) can be different than
        // the property's type, we track that and whether the property type can be null.
        private bool _propertyTypeEqualsTypeToConvert;

        private Func<object, T>? _typedGet;
        private Action<object, T>? _typedSet;

        internal JsonPropertyInfo(JsonTypeInfo? parentTypeInfo) : base(parentTypeInfo)
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

            CheckMutable();

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

            CheckMutable();

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
                _typedSet = ((obj, value) => untypedSet(obj, (T)value!));
                _untypedSet = untypedSet;
            }
        }

        internal override object? DefaultValue => default(T);

        internal JsonConverter<T> TypedEffectiveConverter { get; private set; } = null!;

        internal override void Initialize(
            Type declaringType,
            Type declaredPropertyType,
            ConverterStrategy converterStrategy,
            MemberInfo? memberInfo,
            bool isVirtual,
            JsonConverter converter,
            JsonIgnoreCondition? ignoreCondition,
            JsonSerializerOptions options,
            JsonTypeInfo? jsonTypeInfo = null,
            bool isUserDefinedProperty = false)
        {
            Debug.Assert(converter != null);

            PropertyType = declaredPropertyType;
            _propertyTypeEqualsTypeToConvert = typeof(T) == declaredPropertyType;
            PropertyTypeCanBeNull = PropertyType.CanBeNull();
            ConverterStrategy = converterStrategy;
            if (jsonTypeInfo != null)
            {
                JsonTypeInfo = jsonTypeInfo;
            }

            DefaultConverterForType = converter;
            Options = options;
            DeclaringType = declaringType;
            MemberInfo = memberInfo;
            IsVirtual = isVirtual;
            IgnoreCondition = ignoreCondition;
            IsIgnored = ignoreCondition == JsonIgnoreCondition.Always;

            if (memberInfo != null)
            {
                if (!IsIgnored)
                {
                    switch (memberInfo)
                    {
                        case PropertyInfo propertyInfo:
                            {
                                bool useNonPublicAccessors = GetAttribute<JsonIncludeAttribute>(propertyInfo) != null;

                                MethodInfo? getMethod = propertyInfo.GetMethod;
                                if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
                                {
                                    Get = options.MemberAccessorStrategy.CreatePropertyGetter<T>(propertyInfo);
                                }

                                MethodInfo? setMethod = propertyInfo.SetMethod;
                                if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
                                {
                                    Set = options.MemberAccessorStrategy.CreatePropertySetter<T>(propertyInfo);
                                }

                                MemberType = MemberTypes.Property;

                                break;
                            }

                        case FieldInfo fieldInfo:
                            {
                                Debug.Assert(fieldInfo.IsPublic);

                                Get = options.MemberAccessorStrategy.CreateFieldGetter<T>(fieldInfo);

                                if (!fieldInfo.IsInitOnly)
                                {
                                    Set = options.MemberAccessorStrategy.CreateFieldSetter<T>(fieldInfo);
                                }

                                MemberType = MemberTypes.Field;

                                break;
                            }

                        default:
                            {
                                Debug.Fail($"Invalid memberInfo type: {memberInfo.GetType().FullName}");
                                break;
                            }
                    }
                }

                GetPolicies();
            }
            else if (!isUserDefinedProperty)
            {
                IsForTypeInfo = true;
            }

            if (IgnoreCondition != null)
            {
                _shouldSerialize = GetShouldSerializeForIgnoreCondition(IgnoreCondition.Value);
            }
        }

        internal void InitializeForSourceGen(JsonSerializerOptions options, JsonPropertyInfoValues<T> propertyInfo)
        {
            Options = options;
            ClrName = propertyInfo.PropertyName;

            string name;

            // Property name settings.
            if (propertyInfo.JsonPropertyName != null)
            {
                name = propertyInfo.JsonPropertyName;
            }
            else if (options.PropertyNamingPolicy == null)
            {
                name = ClrName;
            }
            else
            {
                name = options.PropertyNamingPolicy.ConvertName(ClrName);
            }

            // Compat: We need to do validation before we assign Name so that we get InvalidOperationException rather than ArgumentNullException
            if (name == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(DeclaringType, this);
            }

            Name = name;

            SrcGen_IsPublic = propertyInfo.IsPublic;
            SrcGen_HasJsonInclude = propertyInfo.HasJsonInclude;
            SrcGen_IsExtensionData = propertyInfo.IsExtensionData;
            PropertyType = typeof(T);
            _propertyTypeEqualsTypeToConvert = true;
            PropertyTypeCanBeNull = PropertyType.CanBeNull();

            JsonTypeInfo? propertyTypeInfo = propertyInfo.PropertyTypeInfo;
            Type declaringType = propertyInfo.DeclaringType;

            JsonConverter<T>? typedCustomConverter = propertyInfo.Converter;
            CustomConverter = typedCustomConverter;

            JsonConverter<T>? typedNonCustomConverter = propertyTypeInfo?.Converter as JsonConverter<T>;
            DefaultConverterForType = typedNonCustomConverter;

            IsIgnored = propertyInfo.IgnoreCondition == JsonIgnoreCondition.Always;
            if (!IsIgnored)
            {
                Get = propertyInfo.Getter!;
                Set = propertyInfo.Setter;
            }

            JsonTypeInfo = propertyTypeInfo;
            DeclaringType = declaringType;
            IgnoreCondition = propertyInfo.IgnoreCondition;
            MemberType = propertyInfo.IsProperty ? MemberTypes.Property : MemberTypes.Field;
            NumberHandling = propertyInfo.NumberHandling;

            if (IgnoreCondition != null)
            {
                _shouldSerialize = GetShouldSerializeForIgnoreCondition(IgnoreCondition.Value);
            }
        }

        internal override void Configure()
        {
            base.Configure();

            if (!IsForTypeInfo && !IsIgnored)
            {
                _converterIsExternalAndPolymorphic = !EffectiveConverter.IsInternalConverter && PropertyType != EffectiveConverter.TypeToConvert;
            }
        }

        internal override void DetermineEffectiveConverter()
        {
            JsonConverter? customConverter = CustomConverter;
            if (customConverter != null)
            {
                customConverter = Options.ExpandFactoryConverter(customConverter, PropertyType);
                JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(customConverter, PropertyType);
            }

            JsonConverter effectiveConverter = customConverter ?? DefaultConverterForType ?? Options.GetConverterFromTypeInfo(PropertyType);
            if (effectiveConverter.TypeToConvert == PropertyType)
            {
                EffectiveConverter = effectiveConverter;
            }
            else
            {
                EffectiveConverter = effectiveConverter.CreateCastingConverter<T>();
            }
        }

        internal override JsonConverter EffectiveConverter
        {
            get
            {
                return TypedEffectiveConverter;
            }
            set
            {
                TypedEffectiveConverter = (JsonConverter<T>)value;
            }
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
                !TypedEffectiveConverter.IsValueType &&
#endif
                Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                value is not null &&
                !state.IsContinuation &&
                // .NET types that are serialized as JSON primitive values don't need to be tracked for cycle detection e.g: string.
                ConverterStrategy != ConverterStrategy.Value &&
                state.ReferenceResolver.ContainsReferenceForCycleDetection(value))
            {
                // If a reference cycle is detected, treat value as null.
                value = default!;
                Debug.Assert(value == null);
            }

            if (IgnoreDefaultValuesOnWrite)
            {
                // If value is null, it is a reference type or nullable<T>.
                if (value == null)
                {
                    return true;
                }

                if (!PropertyTypeCanBeNull)
                {
                    if (_propertyTypeEqualsTypeToConvert)
                    {
                        // The converter and property types are the same, so we can use T for EqualityComparer<>.
                        if (EqualityComparer<T>.Default.Equals(default, value))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        Debug.Assert(JsonTypeInfo.Type == PropertyType);

                        // Use a late-bound call to EqualityComparer<DeclaredPropertyType>.
                        if (JsonTypeInfo.DefaultValueHolder.IsDefaultValue(value))
                        {
                            return true;
                        }
                    }
                }
            }

            if (ShouldSerialize?.Invoke(obj, value) == false)
            {
                // We return true here.
                // False means that there is not enough data.
                return true;
            }

            if (value == null)
            {
                Debug.Assert(PropertyTypeCanBeNull);

                if (TypedEffectiveConverter.HandleNullOnWrite)
                {
                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;
                        writer.WritePropertyNameSection(EscapedNameSection);
                    }

                    int originalDepth = writer.CurrentDepth;
                    TypedEffectiveConverter.Write(writer, value, Options);
                    if (originalDepth != writer.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterWrite(TypedEffectiveConverter);
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

                return TypedEffectiveConverter.TryWrite(writer, value, Options, ref state);
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
                success = TypedEffectiveConverter.TryWriteDataExtensionProperty(writer, value, Options, ref state);
            }

            return success;
        }

        internal override bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader)
        {
            bool success;

            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !TypedEffectiveConverter.HandleNullOnRead && !state.IsContinuation)
            {
                if (!PropertyTypeCanBeNull)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypedEffectiveConverter.TypeToConvert);
                }

                Debug.Assert(default(T) == null);

                if (!IgnoreDefaultValuesOnRead)
                {
                    T? value = default;
                    Set!(obj, value!);
                }

                success = true;
            }
            else if (TypedEffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // CanUseDirectReadOrWrite == false when using streams
                Debug.Assert(!state.IsContinuation);

                if (!isNullToken || !IgnoreDefaultValuesOnRead || !PropertyTypeCanBeNull)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    T? fastValue = TypedEffectiveConverter.Read(ref reader, PropertyType, Options);
                    Set!(obj, fastValue!);
                }

                success = true;
            }
            else
            {
                success = true;
                if (!isNullToken || !IgnoreDefaultValuesOnRead || !PropertyTypeCanBeNull || state.IsContinuation)
                {
                    success = TypedEffectiveConverter.TryRead(ref reader, PropertyType, Options, ref state, out T? value);
                    if (success)
                    {
#if !DEBUG
                        if (_converterIsExternalAndPolymorphic)
#endif
                        {
                            if (value != null)
                            {
                                Type typeOfValue = value.GetType();
                                if (!PropertyType.IsAssignableFrom(typeOfValue))
                                {
                                    ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(typeOfValue, PropertyType);
                                }
                            }
                            else if (!PropertyTypeCanBeNull)
                            {
                                ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(PropertyType);
                            }
                        }

                        Set!(obj, value!);
                    }
                }
            }

            return success;
        }

        internal override bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !TypedEffectiveConverter.HandleNullOnRead && !state.IsContinuation)
            {
                if (!PropertyTypeCanBeNull)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(TypedEffectiveConverter.TypeToConvert);
                }

                value = default(T);
                success = true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (TypedEffectiveConverter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
                {
                    // CanUseDirectReadOrWrite == false when using streams
                    Debug.Assert(!state.IsContinuation);

                    value = TypedEffectiveConverter.Read(ref reader, PropertyType, Options);
                    success = true;
                }
                else
                {
                    success = TypedEffectiveConverter.TryRead(ref reader, PropertyType, Options, ref state, out T? typedValue);
                    value = typedValue;
                }
            }

            return success;
        }

        internal override void SetExtensionDictionaryAsObject(object obj, object? extensionDict)
        {
            Debug.Assert(HasSetter);
            T typedValue = (T)extensionDict!;
            Set!(obj, typedValue);
        }

        private Func<object, object?, bool> GetShouldSerializeForIgnoreCondition(JsonIgnoreCondition ignoreCondition)
        {
            switch (ignoreCondition)
            {
                case JsonIgnoreCondition.Always: return ShouldSerializeIgnoreConditionAlways;
                case JsonIgnoreCondition.Never: return ShouldSerializeIgnoreConditionNever;
                case JsonIgnoreCondition.WhenWritingNull:
                    if (!PropertyTypeCanBeNull)
                    {
                        return ShouldSerializeIgnoreConditionNever;
                    }

                    goto case JsonIgnoreCondition.WhenWritingDefault;
                case JsonIgnoreCondition.WhenWritingDefault:
                    {
                        if (_propertyTypeEqualsTypeToConvert)
                        {
                            return ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeEqualsTypeToConvert;
                        }
                        else
                        {
                            return ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeNotEqualsTypeToConvert;
                        }
                    }
                default:
                    Debug.Fail($"Unknown value of JsonIgnoreCondition '{ignoreCondition}'");
                    return null!;
            }
        }

        internal static bool ShouldSerializeIgnoreConditionAlways(object obj, object? value) => false;
        internal static bool ShouldSerializeIgnoreConditionNever(object obj, object? value) => true;
        internal static bool ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeEqualsTypeToConvert(object obj, object? value)
        {
            if (value == null)
            {
                return false;
            }

            T typedValue = (T)value;
            return !EqualityComparer<T>.Default.Equals(default, typedValue);
        }

        internal bool ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeNotEqualsTypeToConvert(object obj, object? value)
        {
            if (value == null)
            {
                return false;
            }

            Debug.Assert(JsonTypeInfo.Type == PropertyType);
            return !JsonTypeInfo.DefaultValueHolder.IsDefaultValue(value);
        }
    }
}
