// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    /// <typeparamref name="T"/> is the <see cref="JsonConverter{T}.TypeToConvert"/> for either the property's converter,
    /// or a type's converter, if the current instance is a <see cref="JsonTypeInfo.PropertyInfoForTypeInfo"/>.
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

            VerifyMutable();

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

            VerifyMutable();

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

        internal override object? DefaultValue => default(T);
        internal override bool PropertyTypeCanBeNull => default(T) is null;

        internal JsonConverter<T> TypedEffectiveConverter { get; private set; } = null!;

        private protected override void DetermineMemberAccessors(MemberInfo memberInfo)
        {
            Debug.Assert(memberInfo is FieldInfo or PropertyInfo);

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    {
                        bool useNonPublicAccessors = propertyInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;

                        MethodInfo? getMethod = propertyInfo.GetMethod;
                        if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
                        {
                            Get = Options.MemberAccessorStrategy.CreatePropertyGetter<T>(propertyInfo);
                        }

                        MethodInfo? setMethod = propertyInfo.SetMethod;
                        if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
                        {
                            Set = Options.MemberAccessorStrategy.CreatePropertySetter<T>(propertyInfo);
                        }

                        break;
                    }

                case FieldInfo fieldInfo:
                    {
                        Debug.Assert(fieldInfo.IsPublic);

                        Get = Options.MemberAccessorStrategy.CreateFieldGetter<T>(fieldInfo);

                        if (!fieldInfo.IsInitOnly)
                        {
                            Set = Options.MemberAccessorStrategy.CreateFieldSetter<T>(fieldInfo);
                        }

                        break;
                    }

                default:
                    {
                        Debug.Fail($"Invalid MemberInfo type: {memberInfo.MemberType}");
                        break;
                    }
            }
        }

        internal JsonPropertyInfo(JsonPropertyInfoValues<T> propertyInfo, JsonSerializerOptions options)
            : this(propertyInfo.DeclaringType, declaringTypeInfo: null, options)
        {
            string? name;

            // Property name settings.
            if (propertyInfo.JsonPropertyName != null)
            {
                name = propertyInfo.JsonPropertyName;
            }
            else if (options.PropertyNamingPolicy == null)
            {
                name = propertyInfo.PropertyName;
            }
            else
            {
                name = options.PropertyNamingPolicy.ConvertName(propertyInfo.PropertyName);
            }

            // Compat: We need to do validation before we assign Name so that we get InvalidOperationException rather than ArgumentNullException
            if (name == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(this);
            }

            Name = name;
            MemberName = propertyInfo.PropertyName;
            MemberType = propertyInfo.IsProperty ? MemberTypes.Property : MemberTypes.Field;
            SrcGen_IsPublic = propertyInfo.IsPublic;
            SrcGen_HasJsonInclude = propertyInfo.HasJsonInclude;
            IsExtensionData = propertyInfo.IsExtensionData;
            DefaultConverterForType = propertyInfo.PropertyTypeInfo?.Converter as JsonConverter<T>;
            CustomConverter = propertyInfo.Converter;

            IgnoreCondition = propertyInfo.IgnoreCondition;
            if (IgnoreCondition != JsonIgnoreCondition.Always)
            {
                Get = propertyInfo.Getter!;
                Set = propertyInfo.Setter;
            }

            JsonTypeInfo = propertyInfo.PropertyTypeInfo;
            NumberHandling = propertyInfo.NumberHandling;
        }

        private protected override void DetermineEffectiveConverter()
        {
            JsonConverter? customConverter = CustomConverter;
            if (customConverter != null)
            {
                customConverter = Options.ExpandFactoryConverter(customConverter, PropertyType);
                JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(customConverter, PropertyType);
            }

            JsonConverter converter = customConverter ?? DefaultConverterForType ?? Options.GetOrAddJsonTypeInfo(PropertyType, configured: false).Converter;
            TypedEffectiveConverter = converter is JsonConverter<T> typedConv ? typedConv : converter.CreateCastingConverter<T>();
            ConverterStrategy = TypedEffectiveConverter.ConverterStrategy;
        }

        internal override JsonConverter EffectiveConverter => TypedEffectiveConverter;

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
                    if (EqualityComparer<T>.Default.Equals(default, value))
                    {
                        return true;
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

        private protected override Func<object, object?, bool> GetShouldSerializeForIgnoreCondition(JsonIgnoreCondition ignoreCondition)
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
                        return ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeEqualsTypeToConvert;
                    }
                default:
                    Debug.Fail($"Unknown value of JsonIgnoreCondition '{ignoreCondition}'");
                    return null!;
            }
        }

        private static bool ShouldSerializeIgnoreConditionAlways(object obj, object? value) => false;
        private static bool ShouldSerializeIgnoreConditionNever(object obj, object? value) => true;
        private static bool ShouldSerializeIgnoreConditionWhenWritingDefaultPropertyTypeEqualsTypeToConvert(object obj, object? value)
        {
            if (value == null)
            {
                return false;
            }

            T typedValue = (T)value;
            return !EqualityComparer<T>.Default.Equals(default, typedValue);
        }
    }
}
