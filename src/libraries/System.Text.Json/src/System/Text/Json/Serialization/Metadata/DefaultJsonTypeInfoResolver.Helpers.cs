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
                ResolveOpenGenericDerivedTypes(typeInfo.Type, options.DerivedTypes);
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

            for (int i = 0; i < derivedTypes.Count; i++)
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
                    // Closed (non-open) derived type. If the base type is generic the same
                    // [JsonDerivedType] attribute lives on the open base definition and is
                    // shared across every closed specialization; a closed derived type
                    // necessarily pins a specific specialization of the base. Reject at
                    // metadata-resolution time so a registration cannot silently work for
                    // one specialization and break for another.
                    //
                    // Closed derived types on a NON-generic base continue to flow through
                    // to the PolymorphicTypeResolver assignability check (which validates
                    // and throws on true misregistration).
                    if (baseType.IsGenericType)
                    {
                        ThrowHelper.ThrowInvalidOperationException_OpenGenericDerivedTypeCouldNotBeResolved(
                            baseType, entry.DerivedType, SR.Polymorphism_OpenGeneric_Reason_ClosedDerivedOnGenericBase);
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
        /// Reflection-side resolver: validates that <paramref name="openDerivedType"/> applies
        /// universally to every specialization of the open generic
        /// <paramref name="baseTypeDefinition"/>, and (when it does) produces the closed
        /// derived type for the specific closure identified by
        /// <paramref name="baseTypeArgs"/>.
        ///
        /// "Universal" means: there is a single canonical substitution mapping each derived
        /// type parameter to a base type parameter that simultaneously satisfies every
        /// matching ancestor of the derived type, with every derived constraint implied by
        /// (i.e. weaker-than-or-equal-to) the constraints on the corresponding base parameter.
        /// Registrations that pin a particular specialization (e.g. <c>Derived&lt;T&gt; : Base&lt;T, int&gt;</c>)
        /// are rejected: such registrations would silently work for one base specialization
        /// and break for another, which we treat as a misregistration regardless of which
        /// specialization is currently being constructed.
        ///
        /// IMPORTANT: This implementation MIRRORS the source-gen resolver
        /// <c>RoslynExtensions.TryResolveOpenGenericDerivedType</c> in
        /// gen/Helpers/RoslynExtensions.cs. Both implementations -- the per-ancestor
        /// unification, the canonical-substitution consistency check, and the
        /// constraint-subsumption rules -- must be kept in lockstep so that reflection and
        /// source-gen produce the same closed type for the same registration.
        ///
        /// Known intentional asymmetry with the source-gen mirror: standard reflection does
        /// not surface the <c>unmanaged</c> generic constraint (it is encoded as a modreq),
        /// so reflection's constraint-subsumption check cannot enforce that a derived
        /// <c>unmanaged</c> constraint is matched by a base <c>unmanaged</c> constraint.
        /// Source-gen, which inspects Roslyn's constraint metadata directly, can and does
        /// enforce this. Reflection therefore falls back on <see cref="Type.MakeGenericType"/>
        /// to surface any remaining constraint violations at closure time.
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

            // Every ancestor of the open derived type whose generic type definition matches
            // the base type definition. For classes there is at most one such ancestor; for
            // interfaces a derived type can implement the same interface definition multiple
            // times with different type arguments (e.g. Derived<T> : IBase<T>, IBase<List<T>>).
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

            // The complete set of derived parameters that must be bound (enclosing + leaf
            // flattened: Type.GetGenericArguments() on a nested generic returns enclosing+leaf).
            Type[] requiredParams = openDerivedType.GetGenericArguments();
            Type[] baseParams = baseTypeDefinition.GetGenericArguments();
            var baseParamSet = new HashSet<Type>(baseParams);

            // Per-ancestor independent substitutions; the universal answer must be a single
            // canonical substitution agreed upon by every ancestor.
            Dictionary<Type, Type>? canonical = null;

            foreach (Type ancestor in matchingBases)
            {
                var substitution = new Dictionary<Type, Type>(requiredParams.Length);
                if (!ancestor.TryUnifyWith(baseTypeDefinition, substitution))
                {
                    // Some position pins a concrete type (e.g. Base<T, int>) or a constructed
                    // pattern (e.g. Base<List<T>>) that cannot match a free base parameter.
                    failureReason = SR.Polymorphism_OpenGeneric_Reason_NonUniversalPinning;
                    return false;
                }

                foreach (Type p in requiredParams)
                {
                    if (!substitution.TryGetValue(p, out Type? mapped))
                    {
                        // E.g. D<U1, U2> : IBase<U1> -- U2 is not bound by this ancestor.
                        failureReason = SR.Format(SR.Polymorphism_OpenGeneric_Reason_UnboundParameter, p.Name);
                        return false;
                    }

                    if (!mapped.IsGenericParameter || !baseParamSet.Contains(mapped))
                    {
                        // Substitution value isn't one of the base's own type parameters --
                        // happens when a derived ancestor binds a parameter to a non-parameter
                        // structural target. Treated as non-universal.
                        failureReason = SR.Polymorphism_OpenGeneric_Reason_NonUniversalPinning;
                        return false;
                    }
                }

                if (canonical is null)
                {
                    canonical = substitution;
                }
                else if (!SubstitutionsEqual(canonical, substitution))
                {
                    // Two ancestors agree on independent bindings but produce different
                    // (derived -> base) mappings, e.g. D<U1, U2> : IBase<U1, U2>, IBase<U2, U1>.
                    // There is no single canonical answer for an arbitrary base closure.
                    failureReason = SR.Polymorphism_OpenGeneric_Reason_AmbiguousMatch;
                    return false;
                }
            }

            Debug.Assert(canonical is not null);

            // Constraint equivalence: every derived parameter's constraints must exactly
            // match the constraints on the mapped base parameter (after substitution) so
            // that any valid closure of the base also yields a valid closure of the derived.
            // See ReflectionExtensions.AreConstraintsEquivalent for the rationale behind
            // exact match (vs one-sided subsumption).
            foreach (Type derivedParam in requiredParams)
            {
                Type mappedBaseParam = canonical[derivedParam];
                if (!ReflectionExtensions.AreConstraintsEquivalent(derivedParam, mappedBaseParam, canonical))
                {
                    failureReason = SR.Format(SR.Polymorphism_OpenGeneric_Reason_ConstraintMismatch,
                        derivedParam.Name, mappedBaseParam.Name);
                    return false;
                }
            }

            // Closure construction: substitute the canonical mapping then specialize each
            // base parameter slot to the actual closed-base type argument.
            var baseParamPosition = new Dictionary<Type, int>(baseParams.Length);
            for (int i = 0; i < baseParams.Length; i++)
            {
                baseParamPosition[baseParams[i]] = i;
            }

            Type[] closedArgs = new Type[requiredParams.Length];
            for (int i = 0; i < requiredParams.Length; i++)
            {
                Type mappedBaseParam = canonical[requiredParams[i]];
                closedArgs[i] = baseTypeArgs[baseParamPosition[mappedBaseParam]];
            }

            try
            {
                closedDerivedType = openDerivedType.MakeGenericType(closedArgs);
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or TypeLoadException)
            {
                // Defense in depth: MakeGenericType can still throw for constraints the
                // subsumption check above cannot observe (e.g. derived `unmanaged`
                // constraint, which is not surfaced by standard reflection metadata).
                // Use a structured reason rather than ex.Message so the outer template --
                // which appends its own trailing period -- never produces a double period.
                failureReason = SR.Polymorphism_OpenGeneric_Reason_ConstraintViolation;
                return false;
            }
        }

        private static bool SubstitutionsEqual(Dictionary<Type, Type> a, Dictionary<Type, Type> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (KeyValuePair<Type, Type> kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out Type? other) || other != kvp.Value)
                {
                    return false;
                }
            }

            return true;
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
