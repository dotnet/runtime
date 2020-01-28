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
    internal sealed class JsonPropertyInfo<TConverter> : JsonPropertyInfo
    {
        public Func<object, TConverter>? Get { get; private set; }
        public Action<object, TConverter>? Set { get; private set; }

        public JsonConverter<TConverter> Converter { get; internal set; } = null!;

        public override void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type? runtimePropertyType,
            ClassType runtimeClassType,
            PropertyInfo? propertyInfo,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            base.Initialize(
                parentClassType,
                declaredPropertyType,
                runtimePropertyType,
                runtimeClassType,
                propertyInfo,
                converter,
                options);

            if (propertyInfo != null)
            {
                if (propertyInfo.GetMethod?.IsPublic == true)
                {
                    HasGetter = true;
                    Get = options.MemberAccessorStrategy.CreatePropertyGetter<TConverter>(propertyInfo);
                }

                if (propertyInfo.SetMethod?.IsPublic == true)
                {
                    HasSetter = true;
                    Set = options.MemberAccessorStrategy.CreatePropertySetter<TConverter>(propertyInfo);
                }
            }
            else
            {
                IsPropertyPolicy = true;
                HasGetter = true;
                HasSetter = true;
            }

            GetPolicies();
        }

        public override JsonConverter ConverterBase
        {
            get
            {
                return Converter;
            }
            set
            {
                Debug.Assert(value is JsonConverter<TConverter>);
                Converter = (JsonConverter<TConverter>)value;
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
            TConverter value = Get!(obj);
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
            TConverter value = Get!(obj);

            if (value == null)
            {
                success = true;
            }
            else
            {
                state.Current.PolymorphicJsonPropertyInfo = state.Current.DeclaredJsonPropertyInfo!.RuntimeClassInfo.ElementClassInfo!.PolicyProperty;
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
                    TConverter value = default!;
                    Set!(obj, value);
                }

                success = true;
            }
            else
            {
                // Get the value from the converter and set the property.
                if (Converter.CanUseDirectReadOrWrite)
                {
                    // Optimize for internal converters by avoiding the extra call to TryRead.
                    TConverter fastvalue = Converter.Read(ref reader, RuntimePropertyType!, Options);
                    if (!IgnoreNullValues || (!isNullToken && fastvalue != null))
                    {
                        Set!(obj, fastvalue);
                    }

                    return true;
                }
                else
                {
                    success = Converter.TryRead(ref reader, RuntimePropertyType!, Options, ref state, out TConverter value);
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

        public override void SetValueAsObject(object obj, object? value)
        {
            Debug.Assert(HasSetter);
            TConverter typedValue = (TConverter)value!;

            if (typedValue != null || !IgnoreNullValues)
            {
                Set!(obj, typedValue);
            }
        }
    }
}
