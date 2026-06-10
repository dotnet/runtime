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
            [RequiresUnreferencedCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return global::System.Text.Json.Serialization.Metadata.MemberAccessor.Instance;
            }
        }

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

            if (GetUnmappedMemberHandling(typeInfo.Type) is { } unmappedMemberHandling
                && typeInfo.Kind is JsonTypeInfoKind.Object)
            {
                typeInfo.UnmappedMemberHandling = unmappedMemberHandling;
            }

            PopulatePolymorphismMetadata(typeInfo);

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

            if (typeInfo.Kind is JsonTypeInfoKind.Union)
            {
                PopulateUnionMetadata(typeInfo);
            }

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            converter.ConfigureJsonTypeInfoUsingReflection(typeInfo, options);
            return typeInfo;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static void PopulatePolymorphismMetadata(JsonTypeInfo typeInfo)
        {
            Debug.Assert(!typeInfo.IsReadOnly);

            JsonPolymorphismOptions? options = JsonPolymorphismOptions.CreateFromAttributeDeclarations(typeInfo.Type, out JsonPolymorphicAttribute? polymorphicAttribute);

            if (options is not null)
            {
                int originalDerivedTypeCount = options.DerivedTypes.Count;
                ResolveOpenGenericDerivedTypes(typeInfo.Type, options.DerivedTypes);

                // If closed-derived filtering removed every entry from a non-empty
                // registration AND the user did not explicitly declare [JsonPolymorphic],
                // demote the current specialization to non-polymorphic rather than
                // emitting empty polymorphism options that would throw downstream. The
                // typical scenario is a generic base def carrying closed [JsonDerivedType]
                // attributes that only apply to specific specializations (e.g. Cat :
                // Animal<int> and Dog : Animal<string> both attached to Animal<T>) --
                // for an unrelated Animal<bool>, neither survives filtering and the user
                // expects the type to serialize as plain base. Open-derived failures
                // throw eagerly inside ResolveOpenGenericDerivedTypes, so we only reach
                // this branch when the emptying was caused by silent closed-derived
                // filtering. When [JsonPolymorphic] IS present the user has explicitly
                // opted in, so we honor that intent and let the downstream "no derived
                // types specified" error surface.
                if (polymorphicAttribute is null && originalDerivedTypeCount > 0 && options.DerivedTypes.Count == 0)
                {
                    options = null;
                }
            }

            if (options is not null)
            {
                typeInfo.SetPolymorphismOptions(options);
            }

            if (typeInfo.Kind is not JsonTypeInfoKind.Union)
            {
                if (polymorphicAttribute?.TypeClassifier is Type classifierFactoryType)
                {
                    if (!typeof(JsonTypeClassifierFactory).IsAssignableFrom(classifierFactoryType))
                    {
                        ThrowHelper.ThrowInvalidOperationException_TypeClassifierMustDeriveFromJsonTypeClassifierFactory(classifierFactoryType, typeInfo.Type);
                    }

                    typeInfo.TypeClassifierFactory = (JsonTypeClassifierFactory)Activator.CreateInstance(classifierFactoryType)!;
                }

                if (typeInfo.PolymorphismOptions is not null &&
                    (typeInfo.TypeClassifierFactory is not null || typeInfo.Options.TypeClassifiers.Count > 0))
                {
                    typeInfo.TypeClassifierResolutionPending = true;
                }
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static void ResolveOpenGenericDerivedTypes(Type baseType, IList<JsonDerivedType> derivedTypes)
        {
            Type? baseTypeDefinition = null;
            Type[]? baseTypeArgs = null;

            // Iterate backwards so RemoveAt(i) for filtered entries doesn't disturb the
            // remaining iteration indices.
            for (int i = derivedTypes.Count - 1; i >= 0; i--)
            {
                JsonDerivedType entry = derivedTypes[i];

                if (entry.DerivedType is null)
                {
                    // entry.DerivedType is annotated non-nullable, but the public
                    // JsonDerivedType constructors do not validate the argument, so a
                    // [JsonDerivedType(derivedType: null)] attribute (or an explicit
                    // null) yields an entry with DerivedType == null. The downstream
                    // validation in PolymorphicTypeResolver will surface a friendly
                    // diagnostic; skip silently here so the explicit error wins.
                    continue;
                }

                if (!entry.DerivedType.IsGenericTypeDefinition)
                {
                    // Closed derived type. If the base type is generic and the closed derived
                    // type isn't assignable to the current base specialization, drop it
                    // silently so the same generic base def can carry [JsonDerivedType]
                    // attributes that only apply to specific specializations (e.g. closed
                    // Cat : Animal<int> and Dog : Animal<string> both attached to Animal<T>).
                    // We deliberately do not throw here: the attribute is declared on the
                    // open generic definition, so it cannot statically know which closed
                    // base it will be paired with at runtime. Closed derived types on a
                    // non-generic base continue to flow through to PolymorphicTypeResolver,
                    // which throws if they aren't assignable -- that's a true misregistration.
                    if (baseType.IsGenericType && !baseType.IsAssignableFrom(entry.DerivedType))
                    {
                        derivedTypes.RemoveAt(i);
                    }

                    continue;
                }

                if (!baseType.IsGenericType)
                {
                    ThrowHelper.ThrowInvalidOperationException_OpenGenericDerivedTypeCouldNotBeResolved(
                        baseType, entry.DerivedType, SR.Polymorphism_OpenGeneric_Reason_NotAssignable);
                }

                baseTypeDefinition ??= baseType.GetGenericTypeDefinition();
                baseTypeArgs ??= baseType.GetGenericArguments();

                if (!TryResolveOpenGenericDerivedType(
                        entry.DerivedType, baseTypeDefinition, baseTypeArgs,
                        out Type? resolvedType, out string? failureReason))
                {
                    ThrowHelper.ThrowInvalidOperationException_OpenGenericDerivedTypeCouldNotBeResolved(
                        baseType, entry.DerivedType, failureReason!);
                }

                derivedTypes[i] = new JsonDerivedType(resolvedType!, entry.TypeDiscriminator);
            }
        }

        /// <summary>
        /// Reflection-side resolver: closes <paramref name="openDerivedType"/> against the
        /// constructed base type identified by (<paramref name="baseTypeDefinition"/>,
        /// <paramref name="baseTypeArgs"/>) via structural unification.
        ///
        /// IMPORTANT: This implementation MIRRORS the source-gen resolver
        /// <c>JsonSourceGenerator.Parser.TryResolveOpenGenericDerivedType</c> in
        /// gen/JsonSourceGenerator.Parser.cs. Both implementations -- the structural
        /// unbound pre-check, the per-ancestor unification, and the ambiguity
        /// detection -- must be kept in lockstep so that reflection and source-gen
        /// produce the same closed type for the same registration. Any algorithmic
        /// change here must be applied in the source-gen mirror as well.
        ///
        /// Known intentional asymmetry with the source-gen mirror: source-gen rejects a
        /// managed value type (e.g. a struct containing reference fields) supplied for a
        /// <c>where T : unmanaged</c> constraint because emitting such a closed type would
        /// produce a C# compile error (CS8377). The reflection resolver, by contrast,
        /// delegates constraint validation to <see cref="Type.MakeGenericType"/>, which only
        /// enforces the underlying value-type part of the constraint at runtime (the
        /// <c>unmanaged</c> modreq is not surfaced through standard reflection metadata).
        /// As a result, reflection accepts managed structs for <c>unmanaged</c>-constrained
        /// derived types where source-gen rejects them. This divergence cannot be bridged
        /// without emitting invalid C# code on the source-gen side.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static bool TryResolveOpenGenericDerivedType(
            Type openDerivedType,
            Type baseTypeDefinition,
            Type[] baseTypeArgs,
            out Type? closedDerivedType,
            out string? failureReason)
        {
            closedDerivedType = null;
            failureReason = null;

            // Find every ancestor of the open derived type whose generic type definition matches
            // the base type definition. For classes there is at most one such ancestor, but for
            // interfaces a derived type can implement the same interface definition multiple times
            // with different type arguments (e.g. Derived<T> : IBase<T>, IBase<List<T>>).
            List<Type> matchingBases = new();
            foreach (Type match in openDerivedType.GetMatchingGenericBaseTypes(baseTypeDefinition))
            {
                matchingBases.Add(match);
            }

            if (matchingBases.Count == 0)
            {
                failureReason = SR.Polymorphism_OpenGeneric_Reason_NotAssignable;
                return false;
            }

            // The full set of generic parameters we must bind includes the parameters of the
            // derived type itself plus any parameters declared by enclosing generic types
            // (e.g. Outer<T>.Derived needs T bound from the outer class).
            // Type.GetGenericArguments() on an open generic type returns this complete set.
            Type[] requiredParams = openDerivedType.GetGenericArguments();

            // Structural unbound pre-check: every required parameter must appear at least once
            // somewhere in some matching ancestor's type arguments. If a parameter never appears
            // at all, no closed base could ever bind it -- the derived definition is malformed
            // regardless of which closed base it is registered against.
            HashSet<Type> referencedParams = new();
            foreach (Type mb in matchingBases)
            {
                foreach (Type arg in mb.GetGenericArguments())
                {
                    CollectReferencedParameters(arg, referencedParams);
                }
            }
            foreach (Type required in requiredParams)
            {
                if (!referencedParams.Contains(required))
                {
                    failureReason = SR.Format(SR.Polymorphism_OpenGeneric_Reason_UnboundParameter, required.Name);
                    return false;
                }
            }

            Type[]? successfulArgs = null;
            int successCount = 0;

            foreach (Type matchingBase in matchingBases)
            {
                Type[] matchingBaseArgs = matchingBase.GetGenericArguments();
                Debug.Assert(matchingBaseArgs.Length == baseTypeArgs.Length,
                    "matchingBase and baseTypeArgs share the same generic type definition, so arity must match.");

                var substitution = new Dictionary<Type, Type>(requiredParams.Length);
                bool unified = true;
                for (int i = 0; i < matchingBaseArgs.Length; i++)
                {
                    if (!matchingBaseArgs[i].TryUnifyWith(baseTypeArgs[i], substitution))
                    {
                        unified = false;
                        break;
                    }
                }

                if (!unified)
                {
                    continue;
                }

                // Unification succeeded for every position. Every required parameter of the
                // derived type definition must be bound by this ancestor; otherwise the
                // resulting closed type would have unbound type arguments (an unspeakable type).
                // A sibling ancestor may still bind this parameter, so failure here is not fatal.
                Type[] closedArgs = new Type[requiredParams.Length];
                bool allBound = true;
                for (int i = 0; i < requiredParams.Length; i++)
                {
                    if (!substitution.TryGetValue(requiredParams[i], out Type? boundArg))
                    {
                        allBound = false;
                        break;
                    }

                    closedArgs[i] = boundArg;
                }

                if (!allBound)
                {
                    continue;
                }

                successCount++;
                if (successCount == 1)
                {
                    successfulArgs = closedArgs;
                }
                else
                {
                    failureReason = SR.Polymorphism_OpenGeneric_Reason_AmbiguousMatch;
                    return false;
                }
            }

            if (successCount == 0 || successfulArgs is null)
            {
                failureReason = SR.Polymorphism_OpenGeneric_Reason_UnificationFailed;
                return false;
            }

            try
            {
                closedDerivedType = openDerivedType.MakeGenericType(successfulArgs);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or TypeLoadException)
            {
                // Constraint violation or load failure (e.g. unmanaged constraint, which is
                // not observable via the standard reflection constraint metadata). We use a
                // structured reason rather than ex.Message so that the outer template — which
                // appends its own trailing period — never produces a double period.
                failureReason = SR.Polymorphism_OpenGeneric_Reason_ConstraintViolation;
                return false;
            }
        }

        private static void CollectReferencedParameters(Type pattern, HashSet<Type> set)
        {
            if (pattern.IsGenericParameter)
            {
                set.Add(pattern);
                return;
            }

            if (pattern.HasElementType)
            {
                CollectReferencedParameters(pattern.GetElementType()!, set);
                return;
            }

            if (pattern.IsGenericType)
            {
                foreach (Type arg in pattern.GetGenericArguments())
                {
                    CollectReferencedParameters(arg, set);
                }
            }
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

            // Resolve the type-level JsonNamingPolicyAttribute once for the entire type.
            JsonNamingPolicy? typeNamingPolicy = typeInfo.Type.GetUniqueCustomAttribute<JsonNamingPolicyAttribute>(inherit: false)?.NamingPolicy;

            // Resolve type-level [JsonIgnore] once per type, rather than per-member.
            JsonIgnoreCondition? typeIgnoreCondition = typeInfo.Type.GetUniqueCustomAttribute<JsonIgnoreAttribute>(inherit: false)?.Condition;
            if (typeIgnoreCondition == JsonIgnoreCondition.Always)
            {
                ThrowHelper.ThrowInvalidOperationException(SR.DefaultIgnoreConditionInvalid);
            }

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
                    typeNamingPolicy,
                    nullabilityCtx,
                    typeIgnoreCondition,
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
            JsonNamingPolicy? typeNamingPolicy,
            NullabilityInfoContext nullabilityCtx,
            JsonIgnoreCondition? typeIgnoreCondition,
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
                        typeNamingPolicy,
                        nullabilityCtx,
                        typeIgnoreCondition,
                        shouldCheckMembersForRequiredMemberAttribute,
                        hasJsonIncludeAttribute,
                        ref state);
                }
            }

            foreach (FieldInfo fieldInfo in currentType.GetFields(AllInstanceMembers))
            {
                bool hasJsonIncludeAttribute = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;
                if (hasJsonIncludeAttribute || (fieldInfo.IsPublic && typeInfo.Options.IncludeFields))
                {
                    AddMember(
                        typeInfo,
                        typeToConvert: fieldInfo.FieldType,
                        memberInfo: fieldInfo,
                        typeNamingPolicy,
                        nullabilityCtx,
                        typeIgnoreCondition,
                        shouldCheckMembersForRequiredMemberAttribute,
                        hasJsonIncludeAttribute,
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
            JsonNamingPolicy? typeNamingPolicy,
            NullabilityInfoContext nullabilityCtx,
            JsonIgnoreCondition? typeIgnoreCondition,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute,
            ref JsonTypeInfo.PropertyHierarchyResolutionState state)
        {
            JsonPropertyInfo? jsonPropertyInfo = CreatePropertyInfo(typeInfo, typeToConvert, memberInfo, typeNamingPolicy, nullabilityCtx, typeIgnoreCondition, typeInfo.Options, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
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
            JsonNamingPolicy? typeNamingPolicy,
            NullabilityInfoContext nullabilityCtx,
            JsonIgnoreCondition? typeIgnoreCondition,
            JsonSerializerOptions options,
            bool shouldCheckForRequiredKeyword,
            bool hasJsonIncludeAttribute)
        {
            JsonIgnoreCondition? ignoreCondition = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>(inherit: false)?.Condition;

            // Fall back to the type-level [JsonIgnore] if no member-level attribute is specified.
            if (ignoreCondition is null && typeIgnoreCondition is not null)
            {
                // WhenWritingNull is invalid for non-nullable value types; treat as Never in that case
                // so that the type-level annotation still overrides the global JSO DefaultIgnoreCondition.
                ignoreCondition = typeIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && !typeToConvert.IsNullableType()
                    ? JsonIgnoreCondition.Never
                    : typeIgnoreCondition;
            }

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
            PopulatePropertyInfo(jsonPropertyInfo, memberInfo, customConverter, ignoreCondition, nullabilityCtx, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute, typeNamingPolicy);
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

            // Count non-out parameters - out parameters don't receive values from JSON.
            int nonOutParameterCount = 0;
            foreach (ParameterInfo param in parameters)
            {
                if (!param.IsOut)
                {
                    nonOutParameterCount++;
                }
            }

            JsonParameterInfoValues[] jsonParameters = new JsonParameterInfoValues[nonOutParameterCount];

            int jsonParamIndex = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo reflectionInfo = parameters[i];

                // Skip out parameters - they don't receive values from JSON deserialization.
                if (reflectionInfo.IsOut)
                {
                    continue;
                }

                // Trimmed parameter names are reported as null in CoreCLR or "" in Mono.
                if (string.IsNullOrEmpty(reflectionInfo.Name))
                {
                    Debug.Assert(typeInfo.Converter.ConstructorInfo.DeclaringType != null);
                    ThrowHelper.ThrowNotSupportedException_ConstructorContainsNullParameterNames(typeInfo.Converter.ConstructorInfo.DeclaringType);
                }

                // For byref parameters (in/ref), use the underlying element type.
                Type parameterType = reflectionInfo.ParameterType;
                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType()!;
                }

                JsonParameterInfoValues jsonInfo = new()
                {
                    Name = reflectionInfo.Name,
                    ParameterType = parameterType,
                    Position = jsonParamIndex, // Use the position in the args array, not the constructor parameter index
                    HasDefaultValue = reflectionInfo.HasDefaultValue,
                    DefaultValue = reflectionInfo.GetDefaultValue(),
                    IsNullable = DetermineParameterNullability(reflectionInfo, nullabilityCtx) is not NullabilityState.NotNull,
                };

                jsonParameters[jsonParamIndex] = jsonInfo;
                jsonParamIndex++;
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
            bool hasJsonIncludeAttribute,
            JsonNamingPolicy? typeNamingPolicy)
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
            DeterminePropertyName(jsonPropertyInfo, memberInfo, typeNamingPolicy);
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

        private static void DeterminePropertyName(JsonPropertyInfo propertyInfo, MemberInfo memberInfo, JsonNamingPolicy? typeNamingPolicy)
        {
            JsonPropertyNameAttribute? nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false);
            string? name;
            if (nameAttribute != null)
            {
                name = nameAttribute.Name;
            }
            else
            {
                JsonNamingPolicy? effectivePolicy = memberInfo.GetCustomAttribute<JsonNamingPolicyAttribute>(inherit: false)?.NamingPolicy
                    ?? typeNamingPolicy
                    ?? propertyInfo.Options.PropertyNamingPolicy;

                name = effectivePolicy is not null
                    ? effectivePolicy.ConvertName(memberInfo.Name)
                    : memberInfo.Name;
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
                // (e.g. it might be a non-public ctor with JsonConstructorAttribute).
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
                    foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                    {
                        Type attrType = attr.AttributeType;
                        if (attrType.Name == "NullableAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                        {
                            foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                            {
                                switch (ctorArg.Value)
                                {
                                    case byte flag:
                                        return [flag];
                                    case byte[] flags:
                                        return flags;
                                }
                            }
                        }
                    }

                    return null;
                }

                static byte? GetNullableContextFlag(MemberInfo member)
                {
                    foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                    {
                        Type attrType = attr.AttributeType;
                        if (attrType.Name == "NullableContextAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                        {
                            foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                            {
                                if (ctorArg.Value is byte flag)
                                {
                                    return flag;
                                }
                            }
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
