// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class DefaultJsonTypeInfoResolver
    {
        internal static MemberAccessor MemberAccessor
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return s_memberAccessor ?? Initialize();
                static MemberAccessor Initialize()
                {
                    MemberAccessor value =
#if NET
                        // if dynamic code isn't supported, fallback to reflection
                        RuntimeFeature.IsDynamicCodeSupported ?
                            new ReflectionEmitCachingMemberAccessor() :
                            new ReflectionMemberAccessor();
#elif NETFRAMEWORK
                            new ReflectionEmitCachingMemberAccessor();
#else
                            new ReflectionMemberAccessor();
#endif
                    return Interlocked.CompareExchange(ref s_memberAccessor, value, null) ?? value;
                }
            }
        }

        internal static void ClearMemberAccessorCaches() => s_memberAccessor?.Clear();
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

            if (typeInfo is { Kind: JsonTypeInfoKind.Object, IsNullable: false })
            {
                NullabilityInfoContext nullabilityCtx = new();

                if (converter.ConstructorIsParameterized)
                {
                    // NB parameter metadata must be populated *before* property metadata
                    // so that properties can be linked to their associated parameters.
                    PopulateParameterInfoValues(typeInfo, nullabilityCtx);
                }

                PopulateProperties(typeInfo, nullabilityCtx);

                typeInfo.ConstructorAttributeProvider = typeInfo.Converter.ConstructorInfo;
            }

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            converter.ConfigureJsonTypeInfoUsingReflection(typeInfo, options);
            return typeInfo;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void PopulateProperties(JsonTypeInfo typeInfo, NullabilityInfoContext nullabilityCtx)
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
                    nullabilityCtx,
                    constructorHasSetsRequiredMembersAttribute,
                    ref state);
            }

            if (state.IsPropertyOrderSpecified)
            {
                typeInfo.PropertyList.SortProperties();
            }
        }

        private const BindingFlags AllInstanceMembers =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly;


        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void AddMembersDeclaredBySuperType(
            JsonTypeInfo typeInfo,
            Type currentType,
            NullabilityInfoContext nullabilityCtx,
            bool constructorHasSetsRequiredMembersAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            Debug.Assert(!typeInfo.IsReadOnly);
            Debug.Assert(currentType.IsAssignableFrom(typeInfo.Type));

            // Compiler adds RequiredMemberAttribute to type if any of the members are marked with 'required' keyword.
            bool shouldCheckMembersForRequiredMemberAttribute =
                !constructorHasSetsRequiredMembersAttribute && currentType.HasRequiredMemberAttribute();

            foreach (PropertyInfo propertyInfo in currentType.GetProperties(AllInstanceMembers))
            {
                // Ignore indexers and virtual properties that have overrides that were [JsonIgnore]d.
                if (propertyInfo.GetIndexParameters().Length > 0 ||
                    PropertyIsOverriddenAndIgnored(propertyInfo, state.IgnoredProperties))
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
                        nullabilityCtx,
                        shouldCheckMembersForRequiredMemberAttribute,
                        hasJsonIncludeAttribute,
                        ref state);
                }
            }

            foreach (FieldInfo fieldInfo in currentType.GetFields(AllInstanceMembers))
            {
                bool hasJsonIncludeAtribute = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;
                if (hasJsonIncludeAtribute || (fieldInfo.IsPublic && typeInfo.Options.IncludeFields))
                {
                    AddMember(
                        typeInfo,
                        typeToConvert: fieldInfo.FieldType,
                        memberInfo: fieldInfo,
                        nullabilityCtx,
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
            NullabilityInfoContext nullabilityCtx,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            JsonPropertyInfo? jsonPropertyInfo = CreatePropertyInfo(typeInfo, typeToConvert, memberInfo, nullabilityCtx, typeInfo.Options, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
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
            NullabilityInfoContext nullabilityCtx,
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
            PopulatePropertyInfo(jsonPropertyInfo, memberInfo, customConverter, ignoreCondition, nullabilityCtx, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
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

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void PopulateParameterInfoValues(JsonTypeInfo typeInfo, NullabilityInfoContext nullabilityCtx)
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
                    DefaultValue = reflectionInfo.GetDefaultValue(),
                    IsNullable = DetermineParameterNullability(reflectionInfo, nullabilityCtx) is not NullabilityState.NotNull,
                };

                jsonParameters[i] = jsonInfo;
            }

            typeInfo.PopulateParameterInfoValues(jsonParameters);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void PopulatePropertyInfo(
            JsonPropertyInfo jsonPropertyInfo,
            MemberInfo memberInfo,
            JsonConverter? customConverter,
            JsonIgnoreCondition? ignoreCondition,
            NullabilityInfoContext nullabilityCtx,
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
            DeterminePropertyNullability(jsonPropertyInfo, memberInfo, nullabilityCtx);

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

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void DeterminePropertyNullability(JsonPropertyInfo propertyInfo, MemberInfo memberInfo, NullabilityInfoContext nullabilityCtx)
        {
            if (!propertyInfo.PropertyTypeCanBeNull)
            {
                return;
            }

            NullabilityInfo nullabilityInfo;
            if (propertyInfo.MemberType is MemberTypes.Property)
            {
                nullabilityInfo = nullabilityCtx.Create((PropertyInfo)memberInfo);
            }
            else
            {
                Debug.Assert(propertyInfo.MemberType is MemberTypes.Field);
                nullabilityInfo = nullabilityCtx.Create((FieldInfo)memberInfo);
            }

            propertyInfo.IsGetNullable = nullabilityInfo.ReadState is not NullabilityState.NotNull;
            propertyInfo.IsSetNullable = nullabilityInfo.WriteState is not NullabilityState.NotNull;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static NullabilityState DetermineParameterNullability(ParameterInfo parameterInfo, NullabilityInfoContext nullabilityCtx)
        {
            if (!parameterInfo.ParameterType.IsNullableType())
            {
                return NullabilityState.NotNull;
            }
#if NET8_0
            // Workaround for https://github.com/dotnet/runtime/issues/92487
            // The fix has been incorporated into .NET 9 (and the polyfilled implementations in netfx).
            // Should be removed once .NET 8 support is dropped.
            if (parameterInfo.GetGenericParameterDefinition() is { ParameterType: { IsGenericParameter: true } typeParam })
            {
                // Step 1. Look for nullable annotations on the type parameter.
                if (GetNullableFlags(typeParam) is byte[] flags)
                {
                    return TranslateByte(flags[0]);
                }

                // Step 2. Look for nullable annotations on the generic method declaration.
                if (typeParam.DeclaringMethod != null && GetNullableContextFlag(typeParam.DeclaringMethod) is byte flag)
                {
                    return TranslateByte(flag);
                }

                // Step 3. Look for nullable annotations on the generic type declaration.
                if (GetNullableContextFlag(typeParam.DeclaringType!) is byte flag2)
                {
                    return TranslateByte(flag2);
                }

                // Default to nullable.
                return NullabilityState.Nullable;

                static byte[]? GetNullableFlags(MemberInfo member)
                {
                    foreach (Attribute attr in member.GetCustomAttributes())
                    {
                        Type attrType = attr.GetType();
                        if (attrType.Namespace == "System.Runtime.CompilerServices" && attrType.Name == "NullableAttribute")
                        {
                            return (byte[])attr.GetType().GetField("NullableFlags")?.GetValue(attr)!;
                        }
                    }

                    return null;
                }

                static byte? GetNullableContextFlag(MemberInfo member)
                {
                    foreach (Attribute attr in member.GetCustomAttributes())
                    {
                        Type attrType = attr.GetType();
                        if (attrType.Namespace == "System.Runtime.CompilerServices" && attrType.Name == "NullableContextAttribute")
                        {
                            return (byte?)attr?.GetType().GetField("Flag")?.GetValue(attr)!;
                        }
                    }

                    return null;
                }

                static NullabilityState TranslateByte(byte b) =>
                    b switch
                    {
                        1 => NullabilityState.NotNull,
                        2 => NullabilityState.Nullable,
                        _ => NullabilityState.Unknown
                    };
            }
#endif
            NullabilityInfo nullability = nullabilityCtx.Create(parameterInfo);
            return nullability.WriteState;
        }
    }
}
