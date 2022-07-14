// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    internal sealed class ReflectionJsonTypeInfo<T> : JsonTypeInfo<T>
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal ReflectionJsonTypeInfo(JsonSerializerOptions options)
            : this(DefaultJsonTypeInfoResolver.GetConverterForType(typeof(T), options), options)
        {
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal ReflectionJsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
            : base(converter, options)
        {
            NumberHandling = GetNumberHandlingForType(Type);
            PolymorphismOptions = JsonPolymorphismOptions.CreateFromAttributeDeclarations(Type);
            MapInterfaceTypesToCallbacks();

            if (PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object)
            {
                AddPropertiesAndParametersUsingReflection();
            }

            Func<object>? createObject = Options.MemberAccessorStrategy.CreateConstructor(typeof(T));
            if (converter.UsesDefaultConstructor)
            {
                SetCreateObject(createObject);
            }

            CreateObjectForExtensionDataProperty = createObject;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked as RequiresUnreferencedCode")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The ctor is marked RequiresDynamicCode.")]
        internal override void Configure()
        {
            base.Configure();
            Converter.ConfigureJsonTypeInfoUsingReflection(this, Options);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private void AddPropertiesAndParametersUsingReflection()
        {
            Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);

            const BindingFlags BindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Dictionary<string, JsonPropertyInfo>? ignoredMembers = null;
            bool propertyOrderSpecified = false;

            // Walk through the inheritance hierarchy, starting from the most derived type upward.
            for (Type? currentType = Type; currentType != null; currentType = currentType.BaseType)
            {
                PropertyInfo[] properties = currentType.GetProperties(BindingFlags);

                // PropertyCache is not accessed by other threads until the current JsonTypeInfo instance
                // is finished initializing and added to the cache in JsonSerializerOptions.
                // Default 'capacity' to the common non-polymorphic + property case.
                PropertyCache ??= CreatePropertyCache(capacity: properties.Length);

                foreach (PropertyInfo propertyInfo in properties)
                {
                    string propertyName = propertyInfo.Name;

                    // Ignore indexers and virtual properties that have overrides that were [JsonIgnore]d.
                    if (propertyInfo.GetIndexParameters().Length > 0 ||
                        PropertyIsOverridenAndIgnored(propertyName, propertyInfo.PropertyType, propertyInfo.IsVirtual(), ignoredMembers))
                    {
                        continue;
                    }

                    // For now we only support public properties (i.e. setter and/or getter is public).
                    if (propertyInfo.GetMethod?.IsPublic == true ||
                        propertyInfo.SetMethod?.IsPublic == true)
                    {
                        CacheMember(
                            typeToConvert: propertyInfo.PropertyType,
                            memberInfo: propertyInfo,
                            ref propertyOrderSpecified,
                            ref ignoredMembers);
                    }
                    else
                    {
                        if (propertyInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null)
                        {
                            ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(propertyName, currentType);
                        }

                        // Non-public properties should not be included for (de)serialization.
                    }
                }

                foreach (FieldInfo fieldInfo in currentType.GetFields(BindingFlags))
                {
                    string fieldName = fieldInfo.Name;

                    if (PropertyIsOverridenAndIgnored(fieldName, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
                    {
                        continue;
                    }

                    bool hasJsonInclude = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;

                    if (fieldInfo.IsPublic)
                    {
                        if (hasJsonInclude || Options.IncludeFields)
                        {
                            CacheMember(
                                typeToConvert: fieldInfo.FieldType,
                                memberInfo: fieldInfo,
                                ref propertyOrderSpecified,
                                ref ignoredMembers);
                        }
                    }
                    else
                    {
                        if (hasJsonInclude)
                        {
                            ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(fieldName, currentType);
                        }

                        // Non-public fields should not be included for (de)serialization.
                    }
                }
            }

            Debug.Assert(PropertyCache != null);

            if (propertyOrderSpecified)
            {
                PropertyCache.List.StableSortByKey(static p => p.Value.Order);
            }
        }

        private void CacheMember(
            Type typeToConvert,
            MemberInfo memberInfo,
            ref bool propertyOrderSpecified,
            ref Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            JsonPropertyInfo? jsonPropertyInfo = AddProperty(typeToConvert, memberInfo, Options);
            if (jsonPropertyInfo == null)
            {
                // ignored invalid property
                return;
            }

            Debug.Assert(jsonPropertyInfo.Name != null);
            Debug.Assert(PropertyCache != null);
            CacheMember(jsonPropertyInfo, PropertyCache, ref ignoredMembers);
            propertyOrderSpecified |= jsonPropertyInfo.Order != 0;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked as RequiresUnreferencedCode")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The ctor is marked RequiresDynamicCode.")]
        private JsonPropertyInfo? AddProperty(
            Type typeToConvert,
            MemberInfo memberInfo,
            JsonSerializerOptions options)
        {
            JsonIgnoreCondition? ignoreCondition = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>(inherit: false)?.Condition;

            if (IsInvalidForSerialization(typeToConvert))
            {
                if (ignoreCondition == JsonIgnoreCondition.Always)
                    return null;

                ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(typeToConvert, memberInfo.DeclaringType, memberInfo);
            }

            JsonConverter? customConverter;
            JsonConverter converter;

            try
            {
                converter = DefaultJsonTypeInfoResolver.GetConverterForMember(
                    typeToConvert,
                    memberInfo,
                    options,
                    out customConverter);
            }
            catch (InvalidOperationException) when (ignoreCondition == JsonIgnoreCondition.Always)
            {
                return null;
            }

            JsonPropertyInfo jsonPropertyInfo = CreatePropertyUsingReflection(typeToConvert, converter);
            jsonPropertyInfo.InitializeUsingMemberReflection(memberInfo, customConverter, ignoreCondition);
            return jsonPropertyInfo;
        }

        private static JsonNumberHandling? GetNumberHandlingForType(Type type)
        {
            JsonNumberHandlingAttribute? numberHandlingAttribute = type.GetUniqueCustomAttribute<JsonNumberHandlingAttribute>(inherit: false);
            return numberHandlingAttribute?.Handling;
        }

        private static bool PropertyIsOverridenAndIgnored(
            string currentMemberName,
            Type currentMemberType,
            bool currentMemberIsVirtual,
            Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            if (ignoredMembers == null || !ignoredMembers.TryGetValue(currentMemberName, out JsonPropertyInfo? ignoredMember))
            {
                return false;
            }

            return currentMemberType == ignoredMember.PropertyType &&
                currentMemberIsVirtual &&
                ignoredMember.IsVirtual;
        }

        internal override JsonParameterInfoValues[] GetParameterInfoValues()
        {
            ParameterInfo[] parameters = Converter.ConstructorInfo!.GetParameters();
            return GetParameterInfoArray(parameters);
        }

        private static JsonParameterInfoValues[] GetParameterInfoArray(ParameterInfo[] parameters)
        {
            int parameterCount = parameters.Length;
            JsonParameterInfoValues[] jsonParameters = new JsonParameterInfoValues[parameterCount];

            for (int i = 0; i < parameterCount; i++)
            {
                ParameterInfo reflectionInfo = parameters[i];

                JsonParameterInfoValues jsonInfo = new()
                {
                    Name = reflectionInfo.Name!,
                    ParameterType = reflectionInfo.ParameterType,
                    Position = reflectionInfo.Position,
                    HasDefaultValue = reflectionInfo.HasDefaultValue,
                    DefaultValue = reflectionInfo.GetDefaultValue()
                };

                jsonParameters[i] = jsonInfo;
            }

            return jsonParameters;
        }
    }
}
