// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class DefaultJsonTypeInfoResolver
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonTypeInfo CreateCore(Type type, JsonConverter converter, JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, converter, options);
            typeInfo.NumberHandling = GetNumberHandlingForType(typeInfo.Type);
            if (typeInfo.Kind == JsonTypeInfoKind.Object)
            {
                typeInfo.UnmappedMemberHandling = GetUnmappedMemberHandling(typeInfo.Type);
            }

            typeInfo.PopulatePolymorphismMetadata();
            typeInfo.MapInterfaceTypesToCallbacks();

            Func<object>? createObject = JsonSerializerOptions.MemberAccessorStrategy.CreateConstructor(typeInfo.Type);
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

            // Compiler adds RequiredMemberAttribute to type if any of the members is marked with 'required' keyword.
            // SetsRequiredMembersAttribute means that all required members are assigned by constructor and therefore there is no enforcement
            bool shouldCheckMembersForRequiredMemberAttribute =
                typeInfo.Type.HasRequiredMemberAttribute()
                && !(typeInfo.Converter.ConstructorInfo?.HasSetsRequiredMembersAttribute() ?? false);

            JsonTypeInfo.PropertyHierarchyResolutionState state = new();

            // Walk the type hierarchy starting from the current type up to the base type(s)
            foreach (Type currentType in typeInfo.Type.GetSortedTypeHierarchy())
            {
                if (currentType == JsonTypeInfo.ObjectType)
                {
                    // Don't process any members for typeof(object)
                    break;
                }

                AddMembersDeclaredBySuperType(
                    typeInfo,
                    currentType,
                    shouldCheckMembersForRequiredMemberAttribute,
                    ref state);
            }

            if (state.IsPropertyOrderSpecified)
            {
                typeInfo.SortProperties();
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void AddMembersDeclaredBySuperType(
            JsonTypeInfo typeInfo,
            Type currentType,
            bool shouldCheckMembersForRequiredMemberAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            Debug.Assert(!typeInfo.IsReadOnly);
            Debug.Assert(currentType.IsAssignableFrom(typeInfo.Type));

            const BindingFlags BindingFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            PropertyInfo[] properties = currentType.GetProperties(BindingFlags);

            foreach (PropertyInfo propertyInfo in properties)
            {
                string propertyName = propertyInfo.Name;

                // Ignore indexers and virtual properties that have overrides that were [JsonIgnore]d.
                if (propertyInfo.GetIndexParameters().Length > 0 ||
                    PropertyIsOverriddenAndIgnored(propertyName, propertyInfo.PropertyType, propertyInfo.IsVirtual(), state.IgnoredProperties))
                {
                    continue;
                }

                // For now we only support public properties (i.e. setter and/or getter is public).
                if (propertyInfo.GetMethod?.IsPublic == true ||
                    propertyInfo.SetMethod?.IsPublic == true)
                {
                    AddMember(
                        typeInfo,
                        typeToConvert: propertyInfo.PropertyType,
                        memberInfo: propertyInfo,
                        shouldCheckMembersForRequiredMemberAttribute,
                        ref state);
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

                if (PropertyIsOverriddenAndIgnored(fieldName, fieldInfo.FieldType, currentMemberIsVirtual: false, state.IgnoredProperties))
                {
                    continue;
                }

                bool hasJsonInclude = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;

                if (fieldInfo.IsPublic)
                {
                    if (hasJsonInclude || typeInfo.Options.IncludeFields)
                    {
                        AddMember(
                            typeInfo,
                            typeToConvert: fieldInfo.FieldType,
                            memberInfo: fieldInfo,
                            shouldCheckMembersForRequiredMemberAttribute,
                            ref state);
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

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void AddMember(
            JsonTypeInfo typeInfo,
            Type typeToConvert,
            MemberInfo memberInfo,
            bool shouldCheckForRequiredKeyword,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            JsonPropertyInfo? jsonPropertyInfo = CreatePropertyInfo(typeInfo, typeToConvert, memberInfo, typeInfo.Options, shouldCheckForRequiredKeyword);
            if (jsonPropertyInfo == null)
            {
                // ignored invalid property
                return;
            }

            Debug.Assert(jsonPropertyInfo.Name != null);
            typeInfo.AddProperty(jsonPropertyInfo, ref state);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonPropertyInfo? CreatePropertyInfo(
            JsonTypeInfo typeInfo,
            Type typeToConvert,
            MemberInfo memberInfo,
            JsonSerializerOptions options,
            bool shouldCheckForRequiredKeyword)
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
                customConverter = DefaultJsonTypeInfoResolver.GetCustomConverterForMember(typeToConvert, memberInfo, options);
            }
            catch (InvalidOperationException) when (ignoreCondition == JsonIgnoreCondition.Always)
            {
                // skip property altogether if attribute is invalid and the property is ignored
                return null;
            }

            JsonPropertyInfo jsonPropertyInfo = typeInfo.CreatePropertyUsingReflection(typeToConvert);
            jsonPropertyInfo.InitializeUsingMemberReflection(memberInfo, customConverter, ignoreCondition, shouldCheckForRequiredKeyword);
            return jsonPropertyInfo;
        }

        private static JsonNumberHandling? GetNumberHandlingForType(Type type)
        {
            JsonNumberHandlingAttribute? numberHandlingAttribute = type.GetUniqueCustomAttribute<JsonNumberHandlingAttribute>(inherit: false);
            return numberHandlingAttribute?.Handling;
        }

        private static JsonUnmappedMemberHandling? GetUnmappedMemberHandling(Type type)
        {
            JsonUnmappedMemberHandlingAttribute? numberHandlingAttribute = type.GetUniqueCustomAttribute<JsonUnmappedMemberHandlingAttribute>(inherit: false);
            return numberHandlingAttribute?.UnmappedMemberHandling;
        }

        private static bool PropertyIsOverriddenAndIgnored(
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
    }
}
