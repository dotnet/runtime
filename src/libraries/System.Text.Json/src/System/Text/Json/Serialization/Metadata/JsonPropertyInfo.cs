// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a property or field.
    /// </summary>
    [DebuggerDisplay("MemberInfo={MemberInfo}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class JsonPropertyInfo
    {
        internal static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

        private JsonTypeInfo? _runtimeTypeInfo;

        internal ConverterStrategy ConverterStrategy;

        internal abstract JsonConverter ConverterBase { get; set; }

        internal JsonPropertyInfo()
        {
        }

        internal static JsonPropertyInfo GetPropertyPlaceholder()
        {
            JsonPropertyInfo info = new JsonPropertyInfo<object>();

            Debug.Assert(!info.IsForTypeInfo);
            Debug.Assert(!info.ShouldDeserialize);
            Debug.Assert(!info.ShouldSerialize);

            info.NameAsString = string.Empty;

            return info;
        }

        // Create a property that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        internal static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(MemberInfo memberInfo, JsonSerializerOptions options)
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

        internal Type DeclaredPropertyType { get; set; } = null!;

        internal virtual void GetPolicies(JsonIgnoreCondition? ignoreCondition, JsonNumberHandling? declaringTypeNumberHandling)
        {
            if (IsForTypeInfo)
            {
                Debug.Assert(MemberInfo == null);
                DetermineNumberHandlingForTypeInfo(declaringTypeNumberHandling);
            }
            else
            {
                Debug.Assert(MemberInfo != null);
                DetermineSerializationCapabilities(ignoreCondition);
                DeterminePropertyName();
                DetermineIgnoreCondition(ignoreCondition);

                JsonNumberHandlingAttribute? attribute = GetAttribute<JsonNumberHandlingAttribute>(MemberInfo);
                DetermineNumberHandlingForProperty(attribute?.Handling, declaringTypeNumberHandling);
            }
        }

        private void DeterminePropertyName()
        {
            Debug.Assert(MemberInfo != null);

            JsonPropertyNameAttribute? nameAttribute = GetAttribute<JsonPropertyNameAttribute>(MemberInfo);
            if (nameAttribute != null)
            {
                string name = nameAttribute.Name;
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(DeclaringType, this);
                }

                NameAsString = name;
            }
            else if (Options.PropertyNamingPolicy != null)
            {
                string name = Options.PropertyNamingPolicy.ConvertName(MemberInfo.Name);
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(DeclaringType, this);
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

        internal void DetermineSerializationCapabilities(JsonIgnoreCondition? ignoreCondition)
        {
            Debug.Assert(MemberType == MemberTypes.Property || MemberType == MemberTypes.Field);

            if ((ConverterStrategy & (ConverterStrategy.Enumerable | ConverterStrategy.Dictionary)) == 0)
            {
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                // Three possible values for ignoreCondition:
                // null = JsonIgnore was not placed on this property, global IgnoreReadOnlyProperties/Fields wins
                // WhenNull = only ignore when null, global IgnoreReadOnlyProperties/Fields loses
                // Never = never ignore (always include), global IgnoreReadOnlyProperties/Fields loses
                bool serializeReadOnlyProperty = ignoreCondition != null || (MemberType == MemberTypes.Property
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

        internal void DetermineIgnoreCondition(JsonIgnoreCondition? ignoreCondition)
        {
            if (ignoreCondition != null)
            {
                // This is not true for CodeGen scenarios since we do not cache this as of yet.
                // Debug.Assert(MemberInfo != null);
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                if (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
                else if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
                {
                    if (PropertyTypeCanBeNull)
                    {
                        IgnoreDefaultValuesOnWrite = true;
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(ClrName!, DeclaringType);
                    }
                }
            }
#pragma warning disable SYSLIB0020 // JsonSerializerOptions.IgnoreNullValues is obsolete
            else if (Options.IgnoreNullValues)
            {
                Debug.Assert(Options.DefaultIgnoreCondition == JsonIgnoreCondition.Never);
                if (PropertyTypeCanBeNull)
                {
                    IgnoreDefaultValuesOnRead = true;
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                if (PropertyTypeCanBeNull)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                IgnoreDefaultValuesOnWrite = true;
            }
#pragma warning restore SYSLIB0020
        }

        internal void DetermineNumberHandlingForTypeInfo(JsonNumberHandling? numberHandling)
        {
            if (numberHandling != null && numberHandling != JsonNumberHandling.Strict && !ConverterBase.IsInternalConverter)
            {
                ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
            }

            if (NumberHandingIsApplicable())
            {
                // This logic is to honor JsonNumberHandlingAttribute placed on
                // custom collections e.g. public class MyNumberList : List<int>.

                // Priority 1: Get handling from the type (parent type in this case is the type itself).
                NumberHandling = numberHandling;

                // Priority 2: Get handling from JsonSerializerOptions instance.
                if (!NumberHandling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    NumberHandling = Options.NumberHandling;
                }
            }
        }

        internal void DetermineNumberHandlingForProperty(
            JsonNumberHandling? propertyNumberHandling,
            JsonNumberHandling? declaringTypeNumberHandling)
        {
            bool numberHandlingIsApplicable = NumberHandingIsApplicable();

            if (numberHandlingIsApplicable)
            {
                // Priority 1: Get handling from attribute on property/field, or its parent class type.
                JsonNumberHandling? handling = propertyNumberHandling ?? declaringTypeNumberHandling;

                // Priority 2: Get handling from JsonSerializerOptions instance.
                if (!handling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    handling = Options.NumberHandling;
                }

                NumberHandling = handling;
            }
            else if (propertyNumberHandling.HasValue && propertyNumberHandling != JsonNumberHandling.Strict)
            {
                ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
            }
        }

        private bool NumberHandingIsApplicable()
        {
            if (ConverterBase.IsInternalConverterForNumberType)
            {
                return true;
            }

            if (!ConverterBase.IsInternalConverter ||
                ((ConverterStrategy.Enumerable | ConverterStrategy.Dictionary) & ConverterStrategy) == 0)
            {
                return false;
            }

            Type? elementType = ConverterBase.ElementType;
            Debug.Assert(elementType != null);

            elementType = Nullable.GetUnderlyingType(elementType) ?? elementType;

            if (elementType == typeof(byte) ||
                elementType == typeof(decimal) ||
                elementType == typeof(double) ||
                elementType == typeof(short) ||
                elementType == typeof(int) ||
                elementType == typeof(long) ||
                elementType == typeof(sbyte) ||
                elementType == typeof(float) ||
                elementType == typeof(ushort) ||
                elementType == typeof(uint) ||
                elementType == typeof(ulong) ||
                elementType == JsonTypeInfo.ObjectType)
            {
                return true;
            }

            return false;
        }

        internal static TAttribute? GetAttribute<TAttribute>(MemberInfo memberInfo) where TAttribute : Attribute
        {
            return (TAttribute?)memberInfo.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        internal abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);
        internal abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

        internal abstract object? GetValueAsObject(object obj);

        internal bool HasGetter { get; set; }
        internal bool HasSetter { get; set; }

        internal virtual void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type? runtimePropertyType,
            ConverterStrategy runtimeClassType,
            MemberInfo? memberInfo,
            JsonConverter converter,
            JsonIgnoreCondition? ignoreCondition,
            JsonNumberHandling? parentTypeNumberHandling,
            JsonSerializerOptions options)
        {
            Debug.Assert(converter != null);

            ClrName = memberInfo?.Name;
            DeclaringType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            ConverterStrategy = runtimeClassType;
            MemberInfo = memberInfo;
            ConverterBase = converter;
            Options = options;
        }

        internal abstract void InitializeForTypeInfo(
            Type declaredType,
            JsonTypeInfo runtimeTypeInfo,
            JsonConverter converter,
            JsonSerializerOptions options);

        internal bool IgnoreDefaultValuesOnRead { get; private set; }
        internal bool IgnoreDefaultValuesOnWrite { get; private set; }

        /// <summary>
        /// True if the corresponding cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> is this instance.
        /// </summary>
        internal bool IsForTypeInfo { get; set; }

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
        internal string NameAsString { get; set; } = null!;

        /// <summary>
        /// Utf8 version of NameAsString.
        /// </summary>
        internal byte[] NameAsUtf8Bytes { get; set; } = null!;

        /// <summary>
        /// The escaped name passed to the writer.
        /// </summary>
        internal byte[] EscapedNameSection { get; set; } = null!;

        internal JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        internal bool ReadJsonAndAddExtensionProperty(
            object obj,
            ref ReadStack state,
            ref Utf8JsonReader reader)
        {
            object propValue = GetValueAsObject(obj)!;

            if (propValue is IDictionary<string, object?> dictionaryObjectValue)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    // A null JSON value is treated as a null object reference.
                    dictionaryObjectValue[state.Current.JsonPropertyNameAsString!] = null;
                }
                else
                {
                    JsonConverter<object> converter = (JsonConverter<object>)GetDictionaryValueConverter(JsonTypeInfo.ObjectType);
                    object value = converter.Read(ref reader, JsonTypeInfo.ObjectType, Options)!;
                    dictionaryObjectValue[state.Current.JsonPropertyNameAsString!] = value;
                }
            }
            else if (propValue is IDictionary<string, JsonElement> dictionaryElementValue)
            {
                Type elementType = typeof(JsonElement);
                JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)GetDictionaryValueConverter(elementType);
                JsonElement value = converter.Read(ref reader, elementType, Options);
                dictionaryElementValue[state.Current.JsonPropertyNameAsString!] = value;
            }
            else
            {
                // Avoid a type reference to JsonObject and its converter to support trimming.
                Debug.Assert(propValue is Nodes.JsonObject);
                ConverterBase.ReadElementAndSetProperty(propValue, state.Current.JsonPropertyNameAsString!, ref reader, Options, ref state);
            }

            return true;

            JsonConverter GetDictionaryValueConverter(Type dictionaryValueType)
            {
                JsonConverter converter;
                JsonTypeInfo? dictionaryValueInfo = RuntimeTypeInfo.ElementTypeInfo;
                if (dictionaryValueInfo != null)
                {
                    // Fast path when there is a generic type such as Dictionary<,>.
                    converter = dictionaryValueInfo.PropertyInfoForTypeInfo.ConverterBase;
                }
                else
                {
                    // Slower path for non-generic types that implement IDictionary<,>.
                    // It is possible to cache this converter on JsonTypeInfo if we assume the property value
                    // will always be the same type for all instances.
                    converter = Options.GetConverterInternal(dictionaryValueType);
                }

                Debug.Assert(converter != null);
                return converter;
            }
        }

        internal abstract bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader);

        internal abstract bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value);

        internal bool ReadJsonExtensionDataValue(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            Debug.Assert(this == state.Current.JsonTypeInfo.DataExtensionProperty);

            if (RuntimeTypeInfo.ElementType == JsonTypeInfo.ObjectType && reader.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)Options.GetConverterInternal(typeof(JsonElement));
            if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out JsonElement jsonElement))
            {
                // JsonElement is a struct that must be read in full.
                value = null;
                return false;
            }

            value = jsonElement;
            return true;
        }

        internal Type DeclaringType { get; set; } = null!;

        internal MemberInfo? MemberInfo { get; private set; }

        internal JsonTypeInfo RuntimeTypeInfo
        {
            get
            {
                if (_runtimeTypeInfo == null)
                {
                    _runtimeTypeInfo = Options.GetOrAddClass(RuntimePropertyType!);
                }

                return _runtimeTypeInfo;
            }
            set
            {
                // Used by JsonMetadataServices.
                Debug.Assert(_runtimeTypeInfo == null);
                _runtimeTypeInfo = value;
            }
        }

        internal Type? RuntimePropertyType { get; set; }

        internal abstract void SetExtensionDictionaryAsObject(object obj, object? extensionDict);

        internal bool ShouldSerialize { get; set; }

        internal bool ShouldDeserialize { get; set; }

        internal bool IsIgnored { get; set; }

        internal JsonNumberHandling? NumberHandling { get; set; }

        //  Whether the property type can be null.
        internal bool PropertyTypeCanBeNull { get; set; }

        internal JsonIgnoreCondition? IgnoreCondition { get; set; }

        internal MemberTypes MemberType { get; set; } // TODO: with some refactoring, we should be able to remove this.

        internal string? ClrName { get; set; }
    }
}
