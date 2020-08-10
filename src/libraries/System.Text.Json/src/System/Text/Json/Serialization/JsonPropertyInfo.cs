// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("MemberInfo={MemberInfo}")]
    internal abstract class JsonPropertyInfo
    {
        public static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

        private JsonClassInfo? _runtimeClassInfo;

        public ClassType ClassType;

        public abstract JsonConverter ConverterBase { get; set; }

        public static JsonPropertyInfo GetPropertyPlaceholder()
        {
            JsonPropertyInfo info = new JsonPropertyInfo<object>();

            Debug.Assert(!info.IsForClassInfo);
            Debug.Assert(!info.ShouldDeserialize);
            Debug.Assert(!info.ShouldSerialize);

            info.NameAsString = string.Empty;

            return info;
        }

        // Create a property that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(MemberInfo memberInfo, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfo<sbyte>();
            jsonPropertyInfo.Options = options;
            jsonPropertyInfo.MemberInfo = memberInfo;
            jsonPropertyInfo.DeterminePropertyName();
            jsonPropertyInfo.IsIgnored = true;

            Debug.Assert(!jsonPropertyInfo.ShouldDeserialize);
            Debug.Assert(!jsonPropertyInfo.ShouldSerialize);

            return jsonPropertyInfo;
        }

        public Type DeclaredPropertyType { get; private set; } = null!;

        public virtual void GetPolicies(JsonIgnoreCondition? ignoreCondition, JsonNumberHandling? parentTypeNumberHandling, bool defaultValueIsNull)
        {
            DetermineSerializationCapabilities(ignoreCondition);
            DeterminePropertyName();
            DetermineIgnoreCondition(ignoreCondition, defaultValueIsNull);
            DetermineNumberHandling(parentTypeNumberHandling);
        }

        private void DeterminePropertyName()
        {
            if (MemberInfo == null)
            {
                return;
            }

            JsonPropertyNameAttribute? nameAttribute = GetAttribute<JsonPropertyNameAttribute>(MemberInfo);
            if (nameAttribute != null)
            {
                string name = nameAttribute.Name;
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(ParentClassType, this);
                }

                NameAsString = name;
            }
            else if (Options.PropertyNamingPolicy != null)
            {
                string name = Options.PropertyNamingPolicy.ConvertName(MemberInfo.Name);
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(ParentClassType, this);
                }

                NameAsString = name;
            }
            else
            {
                NameAsString = MemberInfo.Name;
            }

            Debug.Assert(NameAsString != null);

            NameAsUtf8Bytes = Encoding.UTF8.GetBytes(NameAsString);
            EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(NameAsUtf8Bytes, Options.Encoder);
        }

        private void DetermineSerializationCapabilities(JsonIgnoreCondition? ignoreCondition)
        {
            if ((ClassType & (ClassType.Enumerable | ClassType.Dictionary)) == 0)
            {
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                // Three possible values for ignoreCondition:
                // null = JsonIgnore was not placed on this property, global IgnoreReadOnlyProperties/Fields wins
                // WhenNull = only ignore when null, global IgnoreReadOnlyProperties/Fields loses
                // Never = never ignore (always include), global IgnoreReadOnlyProperties/Fields loses
                bool serializeReadOnlyProperty = ignoreCondition != null || (MemberInfo is PropertyInfo
                    ? !Options.IgnoreReadOnlyProperties
                    : !Options.IgnoreReadOnlyFields);

                // We serialize if there is a getter + not ignoring readonly properties.
                ShouldSerialize = HasGetter && (HasSetter || serializeReadOnlyProperty);

                // We deserialize if there is a setter.
                ShouldDeserialize = HasSetter;
            }
            else
            {
                if (HasGetter)
                {
                    Debug.Assert(ConverterBase != null);

                    ShouldSerialize = true;

                    if (HasSetter)
                    {
                        ShouldDeserialize = true;
                    }
                }
            }
        }

        private void DetermineIgnoreCondition(JsonIgnoreCondition? ignoreCondition, bool defaultValueIsNull)
        {
            if (ignoreCondition != null)
            {
                Debug.Assert(MemberInfo != null);
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                if (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
                else if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
                {
                    if (defaultValueIsNull)
                    {
                        IgnoreDefaultValuesOnWrite = true;
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(this);
                    }
                }
            }
#pragma warning disable CS0618 // IgnoreNullValues is obsolete
            else if (Options.IgnoreNullValues)
            {
                Debug.Assert(Options.DefaultIgnoreCondition == JsonIgnoreCondition.Never);
                if (defaultValueIsNull)
                {
                    IgnoreDefaultValuesOnRead = true;
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                if (defaultValueIsNull)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                IgnoreDefaultValuesOnWrite = true;
            }
#pragma warning restore CS0618 // IgnoreNullValues is obsolete
        }

        private void DetermineNumberHandling(JsonNumberHandling? parentTypeNumberHandling)
        {
            if (IsForClassInfo)
            {
                if (parentTypeNumberHandling != null && !ConverterBase.IsInternalConverter)
                {
                    ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
                }

                // Priority 1: Get handling from the type (parent type in this case is the type itself).
                NumberHandling = parentTypeNumberHandling;

                // Priority 2: Get handling from JsonSerializerOptions instance.
                if (!NumberHandling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    NumberHandling = Options.NumberHandling;
                }
            }
            else
            {
                JsonNumberHandling? handling = null;

                // Priority 1: Get handling from attribute on property or field.
                if (MemberInfo != null)
                {
                    JsonNumberHandlingAttribute? attribute = GetAttribute<JsonNumberHandlingAttribute>(MemberInfo);

                    if (attribute != null &&
                        !ConverterBase.IsInternalConverterForNumberType &&
                        ((ClassType.Enumerable | ClassType.Dictionary) & ClassType) == 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
                    }

                    handling = attribute?.Handling;
                }

                // Priority 2: Get handling from attribute on parent class type.
                handling ??= parentTypeNumberHandling;

                // Priority 3: Get handling from JsonSerializerOptions instance.
                if (!handling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    handling = Options.NumberHandling;
                }

                NumberHandling = handling;
            }
        }

        public static TAttribute? GetAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute
        {
            return (TAttribute?)memberInfo.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        public abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);
        public abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

        public abstract object? GetValueAsObject(object obj);

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public virtual void Initialize(
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
            Debug.Assert(converter != null);

            ParentClassType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            ClassType = runtimeClassType;
            MemberInfo = memberInfo;
            ConverterBase = converter;
            Options = options;
        }

        public bool IgnoreDefaultValuesOnRead { get; private set; }
        public bool IgnoreDefaultValuesOnWrite { get; private set; }

        /// <summary>
        /// True if the corresponding cref="JsonClassInfo.PropertyInfoForClassInfo"/> is this instance.
        /// </summary>
        public bool IsForClassInfo { get; protected set; }

        // There are 3 copies of the property name:
        // 1) NameAsString. The unescaped property name.
        // 2) NameAsUtf8Bytes. The Utf8 version of NameAsString. Used during during deserialization for property lookup.
        // 3) EscapedNameSection. The escaped verson of NameAsUtf8Bytes plus the wrapping quotes and a trailing colon. Used during serialization.

        /// <summary>
        /// The unescaped name of the property.
        /// Is either the actual CLR property name,
        /// the value specified in JsonPropertyNameAttribute,
        /// or the value returned from PropertyNamingPolicy(clrPropertyName).
        /// </summary>
        public string NameAsString { get; private set; } = null!;

        /// <summary>
        /// Utf8 version of NameAsString.
        /// </summary>
        public byte[] NameAsUtf8Bytes = null!;

        /// <summary>
        /// The escaped name passed to the writer.
        /// </summary>
        public byte[] EscapedNameSection = null!;

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        public bool ReadJsonAndAddExtensionProperty(object obj, ref ReadStack state, ref Utf8JsonReader reader)
        {
            object propValue = GetValueAsObject(obj)!;

            if (propValue is IDictionary<string, object?> dictionaryObject)
            {
                // Handle case where extension property is System.Object-based.

                if (reader.TokenType == JsonTokenType.Null)
                {
                    // A null JSON value is treated as a null object reference.
                    dictionaryObject[state.Current.JsonPropertyNameAsString!] = null;
                }
                else
                {
                    JsonConverter<object> converter = (JsonConverter<object>)Options.GetConverter(JsonClassInfo.ObjectType);

                    if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out object? value))
                    {
                        return false;
                    }

                    dictionaryObject[state.Current.JsonPropertyNameAsString!] = value;
                }
            }
            else
            {
                // Handle case where extension property is JsonElement-based.

                Debug.Assert(propValue is IDictionary<string, JsonElement>);
                IDictionary<string, JsonElement> dictionaryJsonElement = (IDictionary<string, JsonElement>)propValue;

                JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)Options.GetConverter(typeof(JsonElement));

                if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out JsonElement value))
                {
                    return false;
                }

                dictionaryJsonElement[state.Current.JsonPropertyNameAsString!] = value;
            }

            return true;
        }

        public abstract bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader);

        public abstract bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value);

        public bool ReadJsonExtensionDataValue(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            Debug.Assert(this == state.Current.JsonClassInfo.DataExtensionProperty);

            if (RuntimeClassInfo.ElementType == JsonClassInfo.ObjectType && reader.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)Options.GetConverter(typeof(JsonElement));
            if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out JsonElement jsonElement))
            {
                // JsonElement is a struct that must be read in full.
                value = null;
                return false;
            }

            value = jsonElement;
            return true;
        }

        public Type ParentClassType { get; private set; } = null!;

        public MemberInfo? MemberInfo { get; private set; }

        public JsonClassInfo RuntimeClassInfo
        {
            get
            {
                if (_runtimeClassInfo == null)
                {
                    _runtimeClassInfo = Options.GetOrAddClass(RuntimePropertyType!);
                }

                return _runtimeClassInfo;
            }
        }

        public Type? RuntimePropertyType { get; private set; }

        public abstract void SetExtensionDictionaryAsObject(object obj, object? extensionDict);

        public bool ShouldSerialize { get; private set; }
        public bool ShouldDeserialize { get; private set; }
        public bool IsIgnored { get; private set; }

        public JsonNumberHandling? NumberHandling { get; private set; }
    }
}
