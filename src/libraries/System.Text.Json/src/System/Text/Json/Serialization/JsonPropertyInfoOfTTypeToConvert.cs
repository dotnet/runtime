// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    /// <summary>
    /// Represents a strongly-typed property to prevent boxing and to create a direct delegate to the getter\setter.
    /// </summary>
    internal sealed class JsonPropertyInfo<TTypeToConvert> : JsonPropertyInfo
    {
        public Func<object, TTypeToConvert>? Get { get; private set; }
        public Action<object, TTypeToConvert>? Set { get; private set; }

        public JsonConverter<TTypeToConvert> Converter { get; internal set; } = null!;

        public override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type? runtimePropertyType,
            ClassType runtimeClassType,
            PropertyInfo? propertyInfo,
            JsonConverter converter,
            JsonIgnoreCondition? ignoreCondition,
            JsonSerializerOptions options)
        {
            base.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType,
                propertyInfo,
                converter,
                ignoreCondition,
                options);

            if (propertyInfo != null)
            {
                bool useNonPublicAccessors = GetAttribute<JsonIncludeAttribute>(propertyInfo) != null;

                MethodInfo? getMethod = propertyInfo.GetMethod;
                if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
                {
                    HasGetter = true;
                    Get = options.MemberAccessorStrategy.CreatePropertyGetter<TTypeToConvert>(propertyInfo);
                }

                MethodInfo? setMethod = propertyInfo.SetMethod;
                if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
                {
                    HasSetter = true;
                    Set = options.MemberAccessorStrategy.CreatePropertySetter<TTypeToConvert>(propertyInfo);
                }
            }
            else
            {
                IsPropertyPolicy = true;
                HasGetter = true;
                HasSetter = true;
            }

            GetPolicies(ignoreCondition);
        }

        public override JsonConverter ConverterBase
        {
            get
            {
                return Converter;
            }
            set
            {
                Debug.Assert(value is JsonConverter<TTypeToConvert>);
                Converter = (JsonConverter<TTypeToConvert>)value;
            }
        }

        public override object? GetValueAsObject(object obj)
        {
            if (IsPropertyPolicy)
            {
                return obj;
            }

            Debug.Assert(HasGetter);
            return Get!(obj);
        }

        public override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(EscapedName.HasValue);

            bool success;
            TTypeToConvert value = Get!(obj);
            if (value == null)
            {
                if (!IgnoreNullValues)
                {
                    writer.WriteNull(EscapedName.Value);
                }

                success = true;
            }
            else
            {
                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    state.Current.PropertyState = StackFramePropertyState.Name;
                    writer.WritePropertyName(EscapedName.Value);
                }

                success = Converter.TryWrite(writer, value, Options, ref state);
            }

            return success;
        }

        public override bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer)
        {
            bool success;
            TTypeToConvert value = Get!(obj);

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

        public override bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !Converter.HandleNullValue && !state.IsContinuation)
            {
                if (!IgnoreNullValues)
                {
                    TTypeToConvert value = default;
                    Set!(obj, value!);
                }

                success = true;
            }
            else
            {
                // Get the value from the converter and set the property.
                if (Converter.CanUseDirectReadOrWrite)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    TTypeToConvert fastvalue = Converter.Read(ref reader, RuntimePropertyType!, Options);
                    if (!IgnoreNullValues || (!isNullToken && fastvalue != null))
                    {
                        Set!(obj, fastvalue);
                    }

                    return true;
                }
                else
                {
                    success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out TTypeToConvert value);
                    if (success)
                    {
                        if (!IgnoreNullValues || (!isNullToken && value != null))
                        {
                            Set!(obj, value);
                        }
                    }
                }
            }

            return success;
        }

        public override bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            bool success;
            bool isNullToken = reader.TokenType == JsonTokenType.Null;
            if (isNullToken && !Converter.HandleNullValue && !state.IsContinuation)
            {
                value = default(TTypeToConvert)!;
                success = true;
            }
            else
            {
                // Optimize for internal converters by avoiding the extra call to TryRead.
                if (Converter.CanUseDirectReadOrWrite)
                {
                    value = Converter.Read(ref reader, RuntimePropertyType!, Options);
                    return true;
                }
                else
                {
                    success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out TTypeToConvert typedValue);
                    value = typedValue;
                }
            }

            return success;
        }

        public override void SetValueAsObject(object obj, object? value)
        {
            Debug.Assert(HasSetter);
            TTypeToConvert typedValue = (TTypeToConvert)value!;

            if (typedValue != null || !IgnoreNullValues)
            {
                Set!(obj, typedValue);
            }
        }
    }
}
