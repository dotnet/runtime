// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class DefaultJsonTypeInfoResolver
    {
        internal static MemberAccessor MemberAccessor
        {
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return s_memberAccessor ??=
#if NETCOREAPP
                // if dynamic code isn't supported, fallback to reflection
                RuntimeFeature.IsDynamicCodeSupported ?
                    new ReflectionEmitCachingMemberAccessor() :
                    new ReflectionMemberAccessor();
#elif NETFRAMEWORK
                    new ReflectionEmitCachingMemberAccessor();
#else
                    new ReflectionMemberAccessor();
#endif
            }
        }

        private static MemberAccessor? s_memberAccessor;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo CreateTypeInfoCore(Type type, JsonConverter converter, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, converter, options);

            if (GetNumberHandlingForType(typeInfo.Type) is { } numberHandling)
            {
                typeInfo.NumberHandling = numberHandling;
            }

            if (GetObjectCreationHandlingForType(typeInfo.Type) is { } creationHandling)
            {
                typeInfo.PreferredPropertyObjectCreationHandling = creationHandling;
            }

            if (GetUnmappedMemberHandling(typeInfo.Type) is { } unmappedMemberHandling)
            {
                typeInfo.UnmappedMemberHandling = unmappedMemberHandling;
            }

            typeInfo.PopulatePolymorphismMetadata();
            typeInfo.MapInterfaceTypesToCallbacks();

            Func<object>? createObject = DetermineCreateObjectDelegate(type, converter);
            typeInfo.SetCreateObjectIfCompatible(createObject);
            typeInfo.CreateObjectForExtensionDataProperty = createObject;

            if (typeInfo.Kind is JsonTypeInfoKind.Object)
            {
                PopulateProperties(typeInfo);

                if (converter.ConstructorIsParameterized)
                {
                    PopulateParameterInfoValues(typeInfo);
                }
            }

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            converter.ConfigureJsonTypeInfoUsingReflection(typeInfo, options);
            return typeInfo;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void PopulateProperties(JsonTypeInfo typeInfo)
        {
            Debug.Assert(!typeInfo.IsReadOnly);
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);

            // SetsRequiredMembersAttribute means that all required members are assigned by constructor and therefore there is no enforcement
            bool constructorHasSetsRequiredMembersAttribute =
                typeInfo.Converter.ConstructorInfo?.HasSetsRequiredMembersAttribute() ?? false;

            JsonTypeInfo.PropertyHierarchyResolutionState state = new(typeInfo.Options);

            // Walk the type hierarchy starting from the current type up to the base type(s)
            foreach (Type currentType in typeInfo.Type.GetSortedTypeHierarchy())
            {
                if (currentType == JsonTypeInfo.ObjectType ||
                    currentType == typeof(ValueType))
                {
                    // Don't process any members for typeof(object) or System.ValueType
                    break;
                }

                AddMembersDeclaredBySuperType(
                    typeInfo,
                    currentType,
                    constructorHasSetsRequiredMembersAttribute,
                    ref state);
            }

            if (state.IsPropertyOrderSpecified)
            {
                typeInfo.PropertyList.SortProperties();
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void AddMembersDeclaredBySuperType(
            JsonTypeInfo typeInfo,
            Type currentType,
            bool constructorHasSetsRequiredMembersAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            Debug.Assert(!typeInfo.IsReadOnly);
            Debug.Assert(currentType.IsAssignableFrom(typeInfo.Type));

            const BindingFlags BindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            // Compiler adds RequiredMemberAttribute to type if any of the members are marked with 'required' keyword.
            bool shouldCheckMembersForRequiredMemberAttribute =
                !constructorHasSetsRequiredMembersAttribute && currentType.HasRequiredMemberAttribute();

            foreach (PropertyInfo propertyInfo in currentType.GetProperties(BindingFlags))
            {
                // Ignore:
                // - indexers
                // - virtual properties that have overrides that were [JsonIgnore]d
                // - shadowed properties
                if (propertyInfo.GetIndexParameters().Length > 0 ||
                    PropertyIsOverriddenAndIgnored(propertyInfo, state.IgnoredProperties) ||
                    PropertyIsShadowed(propertyInfo, state.AddedProperties))
                {
                    continue;
                }

                bool hasJsonIncludeAttribute = propertyInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;

                // Only include properties that either have a public getter or a public setter or have the JsonIncludeAttribute set.
                if (propertyInfo.GetMethod?.IsPublic == true ||
                    propertyInfo.SetMethod?.IsPublic == true ||
                    hasJsonIncludeAttribute)
                {
                    AddMember(
                        typeInfo,
                        typeToConvert: propertyInfo.PropertyType,
                        memberInfo: propertyInfo,
                        shouldCheckMembersForRequiredMemberAttribute,
                        hasJsonIncludeAttribute,
                        ref state);
                }
            }

            foreach (FieldInfo fieldInfo in currentType.GetFields(BindingFlags))
            {
                bool hasJsonIncludeAtribute = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;
                if (hasJsonIncludeAtribute || (fieldInfo.IsPublic && typeInfo.Options.IncludeFields))
                {
                    AddMember(
                        typeInfo,
                        typeToConvert: fieldInfo.FieldType,
                        memberInfo: fieldInfo,
                        shouldCheckMembersForRequiredMemberAttribute,
                        hasJsonIncludeAtribute,
                        ref state);
                }
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void AddMember(
            JsonTypeInfo typeInfo,
            Type typeToConvert,
            MemberInfo memberInfo,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            JsonPropertyInfo? jsonPropertyInfo = CreatePropertyInfo(typeInfo, typeToConvert, memberInfo, typeInfo.Options, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
            if (jsonPropertyInfo == null)
            {
                // ignored invalid property
                return;
            }

            Debug.Assert(jsonPropertyInfo.Name != null);
            typeInfo.PropertyList.AddPropertyWithConflictResolution(jsonPropertyInfo, ref state);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonPropertyInfo? CreatePropertyInfo(
            JsonTypeInfo typeInfo,
            Type typeToConvert,
            MemberInfo memberInfo,
            JsonSerializerOptions options,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute)
        {
            JsonIgnoreCondition? ignoreCondition = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>(inherit: false)?.Condition;

            if (JsonTypeInfo.IsInvalidForSerialization(typeToConvert))
            {
                if (ignoreCondition == JsonIgnoreCondition.Always)
                    return null;

                ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(typeToConvert, memberInfo.DeclaringType, memberInfo);
            }

            // Resolve any custom converters on the attribute level.
            JsonConverter? customConverter;
            try
            {
                customConverter = GetCustomConverterForMember(typeToConvert, memberInfo, options);
            }
            catch (InvalidOperationException) when (ignoreCondition == JsonIgnoreCondition.Always)
            {
                // skip property altogether if attribute is invalid and the property is ignored
                return null;
            }

            JsonPropertyInfo jsonPropertyInfo = typeInfo.CreatePropertyUsingReflection(typeToConvert, declaringType: memberInfo.DeclaringType);
            PopulatePropertyInfo(jsonPropertyInfo, memberInfo, customConverter, ignoreCondition, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
            return jsonPropertyInfo;
        }

        private static JsonNumberHandling? GetNumberHandlingForType(Type type)
        {
            JsonNumberHandlingAttribute? numberHandlingAttribute = type.GetUniqueCustomAttribute<JsonNumberHandlingAttribute>(inherit: false);
            return numberHandlingAttribute?.Handling;
        }

        private static JsonObjectCreationHandling? GetObjectCreationHandlingForType(Type type)
        {
            JsonObjectCreationHandlingAttribute? creationHandlingAttribute = type.GetUniqueCustomAttribute<JsonObjectCreationHandlingAttribute>(inherit: false);
            return creationHandlingAttribute?.Handling;
        }

        private static JsonUnmappedMemberHandling? GetUnmappedMemberHandling(Type type)
        {
            JsonUnmappedMemberHandlingAttribute? numberHandlingAttribute = type.GetUniqueCustomAttribute<JsonUnmappedMemberHandlingAttribute>(inherit: false);
            return numberHandlingAttribute?.UnmappedMemberHandling;
        }

        private static bool PropertyIsOverriddenAndIgnored(PropertyInfo propertyInfo, Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            return propertyInfo.IsVirtual() &&
                ignoredMembers?.TryGetValue(propertyInfo.Name, out JsonPropertyInfo? ignoredMember) == true &&
                ignoredMember.IsVirtual &&
                propertyInfo.PropertyType == ignoredMember.PropertyType;
        }

        private static bool PropertyIsShadowed(PropertyInfo propertyInfo, Dictionary<string, (JsonPropertyInfo, int)> addedProperties)
        {
            return addedProperties.TryGetValue(propertyInfo.Name, out (JsonPropertyInfo propertyInfo, int index) other) &&
                propertyInfo.Name == other.propertyInfo.MemberName && propertyInfo.DeclaringType?.IsAssignableFrom(other.propertyInfo.DeclaringType) is true &&
                !other.propertyInfo.IsIgnored;
        }

        private static void PopulateParameterInfoValues(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.Converter.ConstructorInfo != null);
            ParameterInfo[] parameters = typeInfo.Converter.ConstructorInfo.GetParameters();
            int parameterCount = parameters.Length;
            JsonParameterInfoValues[] jsonParameters = new JsonParameterInfoValues[parameterCount];

            for (int i = 0; i < parameterCount; i++)
            {
                ParameterInfo reflectionInfo = parameters[i];

                // Trimmed parameter names are reported as null in CoreCLR or "" in Mono.
                if (string.IsNullOrEmpty(reflectionInfo.Name))
                {
                    Debug.Assert(typeInfo.Converter.ConstructorInfo.DeclaringType != null);
                    ThrowHelper.ThrowNotSupportedException_ConstructorContainsNullParameterNames(typeInfo.Converter.ConstructorInfo.DeclaringType);
                }

                JsonParameterInfoValues jsonInfo = new()
                {
                    Name = reflectionInfo.Name,
                    ParameterType = reflectionInfo.ParameterType,
                    Position = reflectionInfo.Position,
                    HasDefaultValue = reflectionInfo.HasDefaultValue,
                    DefaultValue = reflectionInfo.GetDefaultValue()
                };

                jsonParameters[i] = jsonInfo;
            }

            typeInfo.ParameterInfoValues = jsonParameters;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void PopulatePropertyInfo(
            JsonPropertyInfo jsonPropertyInfo,
            MemberInfo memberInfo,
            JsonConverter? customConverter,
            JsonIgnoreCondition? ignoreCondition,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute)
        {
            Debug.Assert(jsonPropertyInfo.AttributeProvider == null);

            switch (jsonPropertyInfo.AttributeProvider = memberInfo)
            {
                case PropertyInfo propertyInfo:
                    jsonPropertyInfo.MemberName = propertyInfo.Name;
                    jsonPropertyInfo.IsVirtual = propertyInfo.IsVirtual();
                    jsonPropertyInfo.MemberType = MemberTypes.Property;
                    break;
                case FieldInfo fieldInfo:
                    jsonPropertyInfo.MemberName = fieldInfo.Name;
                    jsonPropertyInfo.MemberType = MemberTypes.Field;
                    break;
                default:
                    Debug.Fail("Only FieldInfo and PropertyInfo members are supported.");
                    break;
            }

            jsonPropertyInfo.CustomConverter = customConverter;
            DeterminePropertyPolicies(jsonPropertyInfo, memberInfo);
            DeterminePropertyName(jsonPropertyInfo, memberInfo);
            DeterminePropertyIsRequired(jsonPropertyInfo, memberInfo, shouldCheckForRequiredKeyword);

            if (ignoreCondition != JsonIgnoreCondition.Always)
            {
                jsonPropertyInfo.DetermineReflectionPropertyAccessors(memberInfo, useNonPublicAccessors: hasJsonIncludeAttribute);
            }

            jsonPropertyInfo.IgnoreCondition = ignoreCondition;
            jsonPropertyInfo.IsExtensionData = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(inherit: false) != null;
        }

        private static void DeterminePropertyPolicies(JsonPropertyInfo propertyInfo, MemberInfo memberInfo)
        {
            JsonPropertyOrderAttribute? orderAttr = memberInfo.GetCustomAttribute<JsonPropertyOrderAttribute>(inherit: false);
            propertyInfo.Order = orderAttr?.Order ?? 0;

            JsonNumberHandlingAttribute? numberHandlingAttr = memberInfo.GetCustomAttribute<JsonNumberHandlingAttribute>(inherit: false);
            propertyInfo.NumberHandling = numberHandlingAttr?.Handling;

            JsonObjectCreationHandlingAttribute? objectCreationHandlingAttr = memberInfo.GetCustomAttribute<JsonObjectCreationHandlingAttribute>(inherit: false);
            propertyInfo.ObjectCreationHandling = objectCreationHandlingAttr?.Handling;
        }

        private static void DeterminePropertyName(JsonPropertyInfo propertyInfo, MemberInfo memberInfo)
        {
            JsonPropertyNameAttribute? nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false);
            string? name;
            if (nameAttribute != null)
            {
                name = nameAttribute.Name;
            }
            else if (propertyInfo.Options.PropertyNamingPolicy != null)
            {
                name = propertyInfo.Options.PropertyNamingPolicy.ConvertName(memberInfo.Name);
            }
            else
            {
                name = memberInfo.Name;
            }

            if (name == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(propertyInfo);
            }

            propertyInfo.Name = name;
        }

        private static void DeterminePropertyIsRequired(JsonPropertyInfo propertyInfo, MemberInfo memberInfo, bool shouldCheckForRequiredKeyword)
        {
            propertyInfo.IsRequired =
                memberInfo.GetCustomAttribute<JsonRequiredAttribute>(inherit: false) != null
                || (shouldCheckForRequiredKeyword && memberInfo.HasRequiredMemberAttribute());
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static void DeterminePropertyAccessors<T>(JsonPropertyInfo<T> jsonPropertyInfo, MemberInfo memberInfo, bool useNonPublicAccessors)
        {
            Debug.Assert(memberInfo is FieldInfo or PropertyInfo);

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    MethodInfo? getMethod = propertyInfo.GetMethod;
                    if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
                    {
                        jsonPropertyInfo.Get = MemberAccessor.CreatePropertyGetter<T>(propertyInfo);
                    }

                    MethodInfo? setMethod = propertyInfo.SetMethod;
                    if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
                    {
                        jsonPropertyInfo.Set = MemberAccessor.CreatePropertySetter<T>(propertyInfo);
                    }

                    break;

                case FieldInfo fieldInfo:
                    Debug.Assert(fieldInfo.IsPublic || useNonPublicAccessors);

                    jsonPropertyInfo.Get = MemberAccessor.CreateFieldGetter<T>(fieldInfo);

                    if (!fieldInfo.IsInitOnly)
                    {
                        jsonPropertyInfo.Set = MemberAccessor.CreateFieldSetter<T>(fieldInfo);
                    }

                    break;

                default:
                    Debug.Fail($"Invalid MemberInfo type: {memberInfo.MemberType}");
                    break;
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static Func<object>? DetermineCreateObjectDelegate(Type type, JsonConverter converter)
        {
            ConstructorInfo? defaultCtor = null;

            if (converter.ConstructorInfo != null && !converter.ConstructorIsParameterized)
            {
                // A parameterless constructor has been resolved by the converter
                // (e.g. it might be a non-public ctor with JsonConverterAttribute).
                defaultCtor = converter.ConstructorInfo;
            }

            // Fall back to resolving any public constructors on the type.
            defaultCtor ??= type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

            return MemberAccessor.CreateParameterlessConstructor(type, defaultCtor);
        }
    }
}
