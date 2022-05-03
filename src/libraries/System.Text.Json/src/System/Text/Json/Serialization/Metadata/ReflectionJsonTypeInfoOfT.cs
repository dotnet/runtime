// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    internal sealed class ReflectionJsonTypeInfo<T> : JsonTypeInfo<T>
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal ReflectionJsonTypeInfo(JsonSerializerOptions options)
            : this(
                  GetConverter(
                    typeof(T),
                    parentClassType: null, // A TypeInfo never has a "parent" class.
                    memberInfo: null, // A TypeInfo never has a "parent" property.
                    options),
                  options)
        {
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal ReflectionJsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
            : base(converter, options)
        {
            NumberHandling = GetNumberHandlingForType(Type);

            if (PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object)
            {
                AddPropertiesAndParametersUsingReflection();
            }

            CreateObject = Options.MemberAccessorStrategy.CreateConstructor(typeof(T));
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Constructor is marked as RequiresUnreferencedCode")]
        internal override void Configure()
        {
            base.Configure();
            PropertyInfoForTypeInfo.ConverterBase.ConfigureJsonTypeInfoUsingReflection(this, Options);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private void AddPropertiesAndParametersUsingReflection()
        {
            Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);

            const BindingFlags bindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Dictionary<string, JsonPropertyInfo>? ignoredMembers = null;
            PropertyInfo[] properties = Type.GetProperties(bindingFlags);
            bool propertyOrderSpecified = false;

            // PropertyCache is not accessed by other threads until the current JsonTypeInfo instance
            //  is finished initializing and added to the cache on JsonSerializerOptions.
            // Default 'capacity' to the common non-polymorphic + property case.
            PropertyCache = new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, capacity: properties.Length);

            // We start from the most derived type.
            Type? currentType = Type;

            while (true)
            {
                foreach (PropertyInfo propertyInfo in properties)
                {
                    bool isVirtual = propertyInfo.IsVirtual();
                    string propertyName = propertyInfo.Name;

                    // Ignore indexers and virtual properties that have overrides that were [JsonIgnore]d.
                    if (propertyInfo.GetIndexParameters().Length > 0 ||
                        PropertyIsOverridenAndIgnored(propertyName, propertyInfo.PropertyType, isVirtual, ignoredMembers))
                    {
                        continue;
                    }

                    // For now we only support public properties (i.e. setter and/or getter is public).
                    if (propertyInfo.GetMethod?.IsPublic == true ||
                        propertyInfo.SetMethod?.IsPublic == true)
                    {
                        CacheMember(
                            currentType,
                            propertyInfo.PropertyType,
                            propertyInfo,
                            isVirtual,
                            NumberHandling,
                            ref propertyOrderSpecified,
                            ref ignoredMembers);
                    }
                    else
                    {
                        if (JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(propertyInfo) != null)
                        {
                            ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(propertyName, currentType);
                        }

                        // Non-public properties should not be included for (de)serialization.
                    }
                }

                foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                {
                    string fieldName = fieldInfo.Name;

                    if (PropertyIsOverridenAndIgnored(fieldName, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
                    {
                        continue;
                    }

                    bool hasJsonInclude = JsonPropertyInfo.GetAttribute<JsonIncludeAttribute>(fieldInfo) != null;

                    if (fieldInfo.IsPublic)
                    {
                        if (hasJsonInclude || Options.IncludeFields)
                        {
                            CacheMember(
                                currentType,
                                fieldInfo.FieldType,
                                fieldInfo,
                                isVirtual: false,
                                NumberHandling,
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

                currentType = currentType.BaseType;
                if (currentType == null)
                {
                    break;
                }

                properties = currentType.GetProperties(bindingFlags);
            };

            if (propertyOrderSpecified)
            {
                PropertyCache.List.Sort((p1, p2) => p1.Value!.Order.CompareTo(p2.Value!.Order));
            }
        }

        private void CacheMember(
            Type declaringType,
            Type memberType,
            MemberInfo memberInfo,
            bool isVirtual,
            JsonNumberHandling? typeNumberHandling,
            ref bool propertyOrderSpecified,
            ref Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            bool hasExtensionAttribute = memberInfo.GetCustomAttribute(typeof(JsonExtensionDataAttribute)) != null;
            if (hasExtensionAttribute && DataExtensionProperty != null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
            }

            JsonPropertyInfo jsonPropertyInfo = AddProperty(memberInfo, memberType, declaringType, isVirtual, Options);
            Debug.Assert(jsonPropertyInfo.Name != null);

            if (hasExtensionAttribute)
            {
                Debug.Assert(DataExtensionProperty == null);
                ValidateAndAssignDataExtensionProperty(jsonPropertyInfo);
                Debug.Assert(DataExtensionProperty != null);
            }
            else
            {
                CacheMember(jsonPropertyInfo, PropertyCache, ref ignoredMembers);
                propertyOrderSpecified |= jsonPropertyInfo.Order != 0;
            }
        }

        private static JsonPropertyInfo AddProperty(
            MemberInfo memberInfo,
            Type memberType,
            Type parentClassType,
            bool isVirtual,
            JsonSerializerOptions options)
        {
            JsonIgnoreCondition? ignoreCondition = JsonPropertyInfo.GetAttribute<JsonIgnoreAttribute>(memberInfo)?.Condition;
            if (ignoreCondition == JsonIgnoreCondition.Always)
            {
                return JsonPropertyInfo.CreateIgnoredPropertyPlaceholder(memberInfo, memberType, isVirtual, options);
            }

            ValidateType(memberType, parentClassType, memberInfo, options);

            JsonConverter converter = GetConverter(
                memberType,
                parentClassType,
                memberInfo,
                options);

            return CreateProperty(
                declaredPropertyType: memberType,
                memberInfo,
                parentClassType,
                isVirtual,
                converter,
                options,
                ignoreCondition);
        }

        private static JsonNumberHandling? GetNumberHandlingForType(Type type)
        {
            var numberHandlingAttribute =
                (JsonNumberHandlingAttribute?)JsonSerializerOptions.GetAttributeThatCanHaveMultiple(type, typeof(JsonNumberHandlingAttribute));

            return numberHandlingAttribute?.Handling;
        }

        // This method gets the runtime information for a given type or property.
        // The runtime information consists of the following:
        // - class type,
        // - element type (if the type is a collection),
        // - the converter (either native or custom), if one exists.
        private static JsonConverter GetConverter(
            Type type,
            Type? parentClassType,
            MemberInfo? memberInfo,
            JsonSerializerOptions options)
        {
            Debug.Assert(type != null);
            Debug.Assert(!IsInvalidForSerialization(type), $"Type `{type.FullName}` should already be validated.");
            return options.GetConverterFromMember(parentClassType, type, memberInfo);
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
            ParameterInfo[] parameters = PropertyInfoForTypeInfo.ConverterBase.ConstructorInfo!.GetParameters();
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

        private void ValidateAndAssignDataExtensionProperty(JsonPropertyInfo jsonPropertyInfo)
        {
            if (!IsValidDataExtensionProperty(jsonPropertyInfo))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(Type, jsonPropertyInfo);
            }

            DataExtensionProperty = jsonPropertyInfo;
        }
    }
}
