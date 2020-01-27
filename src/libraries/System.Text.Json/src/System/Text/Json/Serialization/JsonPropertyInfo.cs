// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("PropertyInfo={PropertyInfo}")]
    internal abstract class JsonPropertyInfo
    {
        public static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

        private JsonClassInfo? _runtimeClassInfo;

        public ClassType ClassType;

        public abstract JsonConverter ConverterBase { get; set; }

        public static JsonPropertyInfo GetPropertyPlaceholder()
        {
            JsonPropertyInfo info = new JsonPropertyInfo<object>();
            info.IsPropertyPolicy = false;
            info.ShouldDeserialize = false;
            info.ShouldSerialize = false;
            return info;
        }

        // Create a property that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(PropertyInfo propertyInfo, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfo<sbyte>();
            jsonPropertyInfo.Options = options;
            jsonPropertyInfo.PropertyInfo = propertyInfo;
            jsonPropertyInfo.DeterminePropertyName();

            Debug.Assert(!jsonPropertyInfo.ShouldDeserialize);
            Debug.Assert(!jsonPropertyInfo.ShouldSerialize);

            return jsonPropertyInfo;
        }

        public Type DeclaredPropertyType { get; private set; } = null!;

        private void DeterminePropertyName()
        {
            if (PropertyInfo == null)
            {
                return;
            }

            JsonPropertyNameAttribute? nameAttribute = GetAttribute<JsonPropertyNameAttribute>(PropertyInfo);
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
                string name = Options.PropertyNamingPolicy.ConvertName(PropertyInfo.Name);
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(ParentClassType, this);
                }

                NameAsString = name;
            }
            else
            {
                NameAsString = PropertyInfo.Name;
            }

            Debug.Assert(NameAsString != null);

            // At this point propertyName is valid UTF16, so just call the simple UTF16->UTF8 encoder.
            Name = Encoding.UTF8.GetBytes(NameAsString);

            // Cache the escaped property name.
            EscapedName = JsonEncodedText.Encode(Name, Options.Encoder);

            ulong key = JsonClassInfo.GetKey(Name);
            PropertyNameKey = key;
        }

        private void DetermineSerializationCapabilities()
        {
            if ((ClassType & (ClassType.Enumerable | ClassType.Dictionary)) == 0)
            {
                // We serialize if there is a getter + not ignoring readonly properties.
                ShouldSerialize = HasGetter && (HasSetter || !Options.IgnoreReadOnlyProperties);

                // We deserialize if there is a setter.
                ShouldDeserialize = HasSetter;
            }
            else
            {
                if (HasGetter)
                {
                    if (ConverterBase == null)
                    {
                        ThrowCollectionNotSupportedException();
                    }

                    ShouldSerialize = true;

                    if (HasSetter)
                    {
                        ShouldDeserialize = true;
                    }
                }
            }
        }

        // The escaped name passed to the writer.
        // Use a field here (not a property) to avoid value semantics.
        public JsonEncodedText? EscapedName;

        public static TAttribute? GetAttribute<TAttribute>(PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return (TAttribute?)propertyInfo.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        public abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);
        public abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

        public virtual void GetPolicies()
        {
            DetermineSerializationCapabilities();
            DeterminePropertyName();
            IgnoreNullValues = Options.IgnoreNullValues;
        }

        public abstract object? GetValueAsObject(object obj);

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public virtual void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type? runtimePropertyType,
            ClassType runtimeClassType,
            PropertyInfo? propertyInfo,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            ParentClassType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            ClassType = runtimeClassType;
            PropertyInfo = propertyInfo;
            ConverterBase = converter;
            Options = options;
        }

        public bool IgnoreNullValues { get; private set; }

        public bool IsPropertyPolicy { get; protected set; }

        // The name from a Json value. This is cached for performance on first deserialize.
        public byte[]? JsonPropertyName { get; set; }

        // The name of the property with any casing policy or the name specified from JsonPropertyNameAttribute.
        public byte[]? Name { get; private set; }
        public string? NameAsString { get; set; }

        // Key for fast property name lookup.
        public ulong PropertyNameKey { get; set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; } = null!; // initialized in Init method

        public bool ReadJsonAndAddExtensionProperty(object obj, ref ReadStack state, ref Utf8JsonReader reader)
        {
            object propValue = GetValueAsObject(obj)!;
            IDictionary<string, object?>? dictionaryObject = propValue as IDictionary<string, object?>;

            if (dictionaryObject != null && reader.TokenType == JsonTokenType.Null)
            {
                // A null JSON value is treated as a null object reference.
                dictionaryObject[state.Current.KeyName!] = null;
            }
            else
            {
                JsonConverter<JsonElement> converter = JsonSerializerOptions.GetJsonElementConverter();
                if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out JsonElement jsonElement))
                {
                    // No need to set a partial object here since JsonElement is a struct that must be read in full.
                    return false;
                }

                if (dictionaryObject != null)
                {
                    dictionaryObject[state.Current.KeyName!] = jsonElement;
                }
                else
                {
                    IDictionary<string, JsonElement> dictionaryJsonElement = (IDictionary<string, JsonElement>)propValue;
                    dictionaryJsonElement[state.Current.KeyName!] = jsonElement;
                }
            }

            return true;
        }

        public abstract bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader);

        public Type ParentClassType { get; private set; } = null!;

        public PropertyInfo? PropertyInfo { get; private set; }

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

        public Type? RuntimePropertyType { get; private set; } = null;

        public abstract void SetValueAsObject(object obj, object? value);

        public bool ShouldSerialize { get; private set; }
        public bool ShouldDeserialize { get; private set; }

        public void ThrowCollectionNotSupportedException()
        {
            throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(RuntimePropertyType!, ParentClassType, PropertyInfo);
        }
    }
}
