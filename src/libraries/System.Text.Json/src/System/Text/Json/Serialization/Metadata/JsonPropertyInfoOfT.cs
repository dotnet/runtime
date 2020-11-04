// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    /// <typeparamref name="T"/> is the <see cref="JsonConverter{T}.TypeToConvert"/> for either the property's converter,
    /// or a type's converter, if the current instance is a <see cref="JsonClassInfo.PropertyInfoForClassInfo"/>.
    public sealed class JsonPropertyInfo<T> : JsonPropertyInfo
    {
        private static readonly T s_defaultValue = default!;

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

        /// <summary>
        /// todo
        /// </summary>
        public Func<object, T>? Get { get; internal set; }

        /// <summary>
        /// todo
        /// </summary>
        public Action<object, T>? Set { get; internal set; }

        /// <summary>
        /// todo
        /// </summary>
        public JsonConverter<T> Converter { get; internal set; } = null!;

        internal JsonPropertyInfo() { }

        internal override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type? runtimePropertyType,
            ClassType runtimeClassType,
            MemberInfo? memberInfo,
            JsonConverter converter,
            JsonIgnoreCondition? ignoreCondition,
            JsonNumberHandling? parentTypeNumberHandling,
            JsonSerializerOptions options)
        {
            base.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType,
                memberInfo,
                converter,
                ignoreCondition,
                parentTypeNumberHandling,
                options);

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    {
                        bool useNonPublicAccessors = GetAttribute<JsonIncludeAttribute>(propertyInfo) != null;

                        MethodInfo? getMethod = propertyInfo.GetMethod;
                        if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
                        {
                            HasGetter = true;
                            Get = options.MemberAccessorStrategy.CreatePropertyGetter<T>(propertyInfo);
                        }

                        MethodInfo? setMethod = propertyInfo.SetMethod;
                        if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
                        {
                            HasSetter = true;
                            Set = options.MemberAccessorStrategy.CreatePropertySetter<T>(propertyInfo);
                        }

                        break;
                    }

                case FieldInfo fieldInfo:
                    {
                        Debug.Assert(fieldInfo.IsPublic);

                        HasGetter = true;
                        Get = options.MemberAccessorStrategy.CreateFieldGetter<T>(fieldInfo);

                        if (!fieldInfo.IsInitOnly)
                        {
                            HasSetter = true;
                            Set = options.MemberAccessorStrategy.CreateFieldSetter<T>(fieldInfo);
                        }

                        break;
                    }

                default:
                    {
                        IsForClassInfo = true;
                        HasGetter = true;
                        HasSetter = true;

                        break;
                    }
            }

            _converterIsExternalAndPolymorphic = !converter.IsInternalConverter && DeclaredPropertyType != converter.TypeToConvert;
            PropertyTypeCanBeNull = DeclaredPropertyType.CanBeNull();
            _propertyTypeEqualsTypeToConvert = typeof(T) == DeclaredPropertyType;

            GetPolicies(ignoreCondition, parentTypeNumberHandling, defaultValueIsNull: PropertyTypeCanBeNull);
        }

        /// <summary>
        /// todo
        /// </summary>
        public override JsonConverter ConverterBase
        {
            get
            {
                return Converter;
            }
            set
            {
                Debug.Assert(value is JsonConverter<T>);
                Converter = (JsonConverter<T>)value;
            }
        }

        internal override object? GetValueAsObject(object obj)
        {
            if (IsForClassInfo)
            {
                return obj;
            }

            Debug.Assert(HasGetter);
            return Get!(obj);
        }

        internal override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            T value = Get!(obj);

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
                        Debug.Assert(RuntimeClassInfo.Type == DeclaredPropertyType);

                        // Use a late-bound call to EqualityComparer<DeclaredPropertyType>.
                        if (RuntimeClassInfo.GenericMethods.IsDefaultValue(value))
                        {
                            return true;
                        }
                    }
                }
            }

            if (value == null)
            {
                Debug.Assert(PropertyTypeCanBeNull);

                if (Converter.HandleNullOnWrite)
                {
                    // No object, collection, or re-entrancy converter handles null.
                    Debug.Assert(Converter.ClassType == ClassType.Value);

                    if (state.Current.PropertyState < StackFramePropertyState.Name)
                    {
                        state.Current.PropertyState = StackFramePropertyState.Name;
                        writer.WritePropertyNameSection(EscapedNameSection);
                    }

                    int originalDepth = writer.CurrentDepth;
                    Converter.Write(writer, value, Options);
                    if (originalDepth != writer.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterWrite(Converter);
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

                return Converter.TryWrite(writer, value, Options, ref state);
            }
        }

        internal override bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            bool success;
            T value = Get!(obj);

            if (value == null)
            {
                success = true;
            }
            else
            {
                success = Converter.TryWriteDataExtensionProperty(writer, value, Options, ref state);
            }

            return success;
        }

        internal override bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader)
        {
            bool success;

            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !Converter.HandleNullOnRead && !state.IsContinuation)
            {
                if (!PropertyTypeCanBeNull)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Converter.TypeToConvert);
                }

                Debug.Assert(default(T) == null);

                if (!IgnoreDefaultValuesOnRead)
                {
                    T value = default;
                    Set!(obj, value!);
                }

                success = true;
            }
            else if (Converter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
            {
                // CanUseDirectReadOrWrite == false when using streams
                Debug.Assert(!state.IsContinuation);

                if (!isNullToken || !IgnoreDefaultValuesOnRead || !PropertyTypeCanBeNull)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    T fastValue = Converter.Read(ref reader, RuntimePropertyType!, Options);
                    Set!(obj, fastValue!);
                }

                success = true;
            }
            else
            {
                success = true;
                if (!isNullToken || !IgnoreDefaultValuesOnRead || !PropertyTypeCanBeNull || state.IsContinuation)
                {
                    success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out T value);
                    if (success)
                    {
#if !DEBUG
                        if (_converterIsExternalAndPolymorphic)
#endif
                        {
                            if (value != null)
                            {
                                Type typeOfValue = value.GetType();
                                if (!DeclaredPropertyType.IsAssignableFrom(typeOfValue))
                                {
                                    ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(typeOfValue, DeclaredPropertyType);
                                }
                            }
                            else if (!PropertyTypeCanBeNull)
                            {
                                ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(DeclaredPropertyType);
                            }
                        }

                        Set!(obj, value!);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// todo: Fast path for value types (not objects or collections)
        /// todo: for perf, also call this for value converters for non-delegate\codegen case?
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="state"></param>
        /// <param name="obj"></param>
        public void ReadValueAndSetMember(ref Utf8JsonReader reader, ref ReadStack state, object obj)
        {
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !Converter.HandleNull)
            {
                if (!Converter.CanBeNull)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Converter.TypeToConvert);
                }

                Debug.Assert(s_defaultValue == null);

                if (!IgnoreDefaultValuesOnRead)
                {
                    T value = default;
                    Set!(obj, value!);
                }
            }
            else if (!isNullToken || !IgnoreDefaultValuesOnRead || !Converter.CanBeNull)
            {
                // Support JsonPath in exceptions.
                state.Current.JsonPropertyInfo = this;

                // Optimize for value converters by avoiding the extra call to TryRead.
                T value = Converter.Read(ref reader, RuntimePropertyType!, Options);
                Set!(obj, value!);

                state.Current.EndPropertyFast();
            }
        }

        /// <summary>
        /// todo:Supports object and collections; use ReadValueAndSetMember for perf for values.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="state"></param>
        /// <param name="obj"></param>
        public void ReadAndSetMember(ref Utf8JsonReader reader, ref ReadStack state, object obj)
        {
            // Code-gen should not call this for properties with no setter.
            Debug.Assert(ShouldDeserialize);

            Debug.Assert(!Converter.HandleNull);
            Debug.Assert(Converter.CanBeNull);
            Debug.Assert(!state.IsContinuation);

            bool isNullToken = reader.TokenType == JsonTokenType.Null;

            if (isNullToken)
            {
                Debug.Assert(s_defaultValue == null);

                if (!IgnoreDefaultValuesOnRead)
                {
                    T value = default;
                    Set!(obj, value!);
                }
            }
            else
            {
                state.Current.JsonPropertyInfo = this;

                bool success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out T value);
                Debug.Assert(success);
                Set!(obj, value!);

                state.Current.EndPropertyFast();
            }
        }

        internal override bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !Converter.HandleNullOnRead && !state.IsContinuation)
            {
                if (!PropertyTypeCanBeNull)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Converter.TypeToConvert);
                }

                value = default(T);
                success = true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (Converter.CanUseDirectReadOrWrite && state.Current.NumberHandling == null)
                {
                    // CanUseDirectReadOrWrite == false when using streams
                    Debug.Assert(!state.IsContinuation);

                    value = Converter.Read(ref reader, RuntimePropertyType!, Options);
                    success = true;
                }
                else
                {
                    success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out T typedValue);
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

        /// <summary>
        /// todo:Supports object and collections; use WriteValue for perf for values.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="state"></param>
        /// <param name="writer"></param>
        public void Write(in T value, ref WriteStack state, Utf8JsonWriter writer)
        {
            if (value == null)
            {
                Debug.Assert(s_defaultValue == null && Converter.CanBeNull);

                if (!IgnoreDefaultValuesOnWrite)
                {
                    Debug.Assert(!Converter.HandleNull);
                    writer.WriteNullSection(EscapedNameSection);
                }
            }
            else if (IgnoreDefaultValuesOnWrite && Converter._defaultComparer.Equals(s_defaultValue, value))
            {
                Debug.Assert(s_defaultValue != null && !Converter.CanBeNull);
            }
            else
            {
                writer.WritePropertyNameSection(EscapedNameSection);
                state.Current.PolymorphicJsonPropertyInfo = this;
                if (!Converter.TryWrite(writer, value, Options, ref state))
                {
                    throw new InvalidOperationException("todo: converter returned false");
                }

                state.Current.EndPropertyFast();
            }
        }

        /// <summary>
        /// todo: Fast path for value types (not objects or collections)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="state"></param>
        /// <param name="writer"></param>
        public void WriteValue(in T value, ref WriteStack state, Utf8JsonWriter writer)
        {
            if (IgnoreDefaultValuesOnWrite && Converter._defaultComparer.Equals(s_defaultValue, value))
            {
                Debug.Assert(s_defaultValue != null && !Converter.CanBeNull);
            }
            else
            {
                writer.WritePropertyNameSection(EscapedNameSection);
                state.Current.PolymorphicJsonPropertyInfo = this;
                Converter.Write(writer, value, Options);
                state.Current.EndPropertyFast();
            }
        }
    }
}
