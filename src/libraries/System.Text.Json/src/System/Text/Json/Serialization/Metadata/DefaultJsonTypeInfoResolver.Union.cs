// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class DefaultJsonTypeInfoResolver
    {
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static void PopulateUnionMetadata(JsonTypeInfo typeInfo)
        {
            Debug.Assert(!typeInfo.IsReadOnly);
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Union);

            Type unionType = typeInfo.Type;

            Type builderType = typeof(UnionMetadataBuilder<>).MakeGenericType(unionType);
            var builder = (UnionMetadataBuilder)Activator.CreateInstance(builderType, nonPublic: true)!;
            builder.Build(typeInfo);
        }

        private abstract class UnionMetadataBuilder
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            public abstract void Build(JsonTypeInfo typeInfo);
        }

        private sealed class UnionMetadataBuilder<TUnion> : UnionMetadataBuilder
        {
            private readonly List<UnionCaseEntry> _caseEntries = new();
            private readonly Dictionary<Type, UnionCaseEntry> _entryByCaseType = new();

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            public override void Build(JsonTypeInfo typeInfo)
            {
                JsonTypeInfo<TUnion> typeInfoOfT = (JsonTypeInfo<TUnion>)typeInfo;

                PopulateUnionCases(typeInfoOfT);
                if (_caseEntries.Count == 0)
                {
                    // No discoverable case constructors, return early and leave the type info with an empty
                    // case list and null delegates for potential user-side contract fix-up.
                    return;
                }

                PopulateUnionTypeClassifier(typeInfoOfT); // Must happen after union case population.
                PopulateUnionDelegates(typeInfoOfT);
            }

            private static void PopulateUnionTypeClassifier(JsonTypeInfo<TUnion> typeInfo)
            {
                Debug.Assert(typeInfo.TypeClassifier is null,
                    "PopulateTypeClassifier is only invoked from the built-in resolver, before any contract customization. " +
                    "TypeClassifier must therefore not be set yet.");

                JsonUnionAttribute? attr = typeof(TUnion).GetCustomAttribute<JsonUnionAttribute>();
                if (attr?.TypeClassifier is { } attrClassifierType)
                {
                    if (!typeof(JsonTypeClassifierFactory).IsAssignableFrom(attrClassifierType))
                    {
                        ThrowHelper.ThrowInvalidOperationException_TypeClassifierMustDeriveFromJsonTypeClassifierFactory(attrClassifierType, typeof(TUnion));
                    }

                    typeInfo.TypeClassifierFactory = (JsonTypeClassifierFactory)Activator.CreateInstance(attrClassifierType)!;
                }

                // Resolution is deferred to first read of
                // JsonTypeInfo.TypeClassifier — at that point the typeInfo is in the
                // per-options cache, so re-entrant lookups for the union type itself find the
                // partial typeInfo instead of recursing into a fresh resolution.
                typeInfo.TypeClassifierResolutionPending = true;
            }

            /// <summary>
            /// Walks the union type's public single-parameter constructors and populates
            /// <see cref="JsonTypeInfo.UnionCases"/> in declaration order. When the same case
            /// type appears across multiple constructors, the entry is added once, but the
            /// <see cref="JsonUnionCaseInfo.IsNullable"/> flag is the OR across all matching
            /// constructors so that any nullable-accepting overload is selected for the case.
            /// </summary>
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            private void PopulateUnionCases(JsonTypeInfo<TUnion> typeInfo)
            {
                Debug.Assert(typeInfo.UnionCases.Count == 0,
                    "PopulateUnionCases is only invoked from the built-in resolver, before any contract customization. " +
                    "UnionCases must therefore not be populated yet.");

                NullabilityInfoContext nullabilityCtx = new();
                IList<JsonUnionCaseInfo> unionCases = typeInfo.UnionCases;
                Dictionary<Type, int> indexByCaseType = new();

                foreach (ConstructorInfo ctor in typeof(TUnion).GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    if (parameters.Length != 1 ||
                        !TryGetCaseType(parameters[0], nullabilityCtx, out Type? paramType, out bool acceptsNull) ||
                        paramType.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
                    {
                        continue;
                    }

                    if (_entryByCaseType.TryGetValue(paramType, out UnionCaseEntry? entry))
                    {
                        // Reachable when a value-type case has both `Foo(T)` and `Foo(T?)` ctor overloads.
                        Debug.Assert(paramType.IsValueType);

                        if (acceptsNull && !entry.CaseInfo.IsNullable)
                        {
                            int index = indexByCaseType[paramType];
                            entry = CreateUnionCaseEntry(paramType, ctor, isNullable: true);
                            _entryByCaseType[paramType] = entry;
                            _caseEntries[index] = entry;
                            unionCases[index] = entry.CaseInfo;
                        }
                    }
                    else
                    {
                        entry = CreateUnionCaseEntry(paramType, ctor, acceptsNull);
                        _entryByCaseType.Add(paramType, entry);
                        indexByCaseType.Add(paramType, _caseEntries.Count);
                        _caseEntries.Add(entry);
                        unionCases.Add(entry.CaseInfo);
                    }
                }
            }

            /// <summary>
            /// Builds the convention-based <see cref="JsonTypeInfo.UnionDeconstructor"/> and
            /// <see cref="JsonTypeInfo.UnionConstructor"/> delegates.
            /// </summary>
            /// <remarks>
            /// Convention-based discovery gets the union value from a public instance
            /// <c>object Value</c> property. If the property is absent, the deconstructor is left
            /// null and the user must populate it via contract customization.
            /// </remarks>
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            private void PopulateUnionDelegates(JsonTypeInfo<TUnion> typeInfo)
            {
                Debug.Assert(_caseEntries.Count > 0);

                PropertyInfo? valueProperty = GetUnionValueProperty();
                if (valueProperty is null)
                {
                    return;
                }

                // Topologically sort the (declaration-ordered) UnionCases — most-derived first —
                // so the nearest-ancestor walk in the deconstructor and constructor delegates
                // hits the most-specific declared case before any of its bases.
                UnionCaseEntry[] orderedCases = BuildTopologicallySortedCaseEntries();
                ConcurrentDictionary<Type, UnionCaseEntry?> caseIndex = CreateCaseIndex(orderedCases);
                UnionCaseEntry? nullableCase = null;
                foreach (UnionCaseEntry entry in orderedCases)
                {
                    if (entry.CaseInfo.IsNullable)
                    {
                        nullableCase = entry;
                        break;
                    }
                }

                PopulateUnionDeconstructor(typeInfo, orderedCases, caseIndex, valueProperty, nullableCase);
                PopulateUnionConstructor(typeInfo, orderedCases, caseIndex);
            }

            private UnionCaseEntry[] BuildTopologicallySortedCaseEntries()
            {
                Type[] caseTypes = new Type[_caseEntries.Count];
                for (int i = 0; i < _caseEntries.Count; i++)
                {
                    caseTypes[i] = _caseEntries[i].CaseType;
                }

                Type[] orderedCaseTypes = SortTypesByInheritanceHierarchy(caseTypes, mostDerivedTypesFirst: true);
                UnionCaseEntry[] orderedCases = new UnionCaseEntry[orderedCaseTypes.Length];
                for (int i = 0; i < orderedCaseTypes.Length; i++)
                {
                    orderedCases[i] = _entryByCaseType[orderedCaseTypes[i]];
                }

                return orderedCases;
            }

            private static ConcurrentDictionary<Type, UnionCaseEntry?> CreateCaseIndex(UnionCaseEntry[] orderedCases)
            {
                var caseIndex = new ConcurrentDictionary<Type, UnionCaseEntry?>();
                foreach (UnionCaseEntry entry in orderedCases)
                {
                    caseIndex[entry.CaseType] = entry;
                }

                return caseIndex;
            }

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            private static void PopulateUnionDeconstructor(
                JsonTypeInfo<TUnion> typeInfo,
                UnionCaseEntry[] orderedCases,
                ConcurrentDictionary<Type, UnionCaseEntry?> caseIndex,
                PropertyInfo valueProperty,
                UnionCaseEntry? nullableCase)
            {
                Debug.Assert(typeInfo.UnionDeconstructor is null);

                Func<TUnion, object?> valueAccessor = MemberAccessor.CreatePropertyGetter<TUnion, object?>(valueProperty);
                UnionTryGetValueAccessor<TUnion>? chainedTryGetValue = PopulateTryGetValueMethod();

                typeInfo.UnionDeconstructor = (TUnion union) =>
                {
                    // For reference-type unions union may be null -- treat as the canonical null state.
                    if (!typeof(TUnion).IsValueType && (object?)union is null)
                    {
                        return (null, null);
                    }

                    // Primary path: when the union declares 'bool TryGetValue(out CaseType)'
                    // overloads, defer to the chained accessor which mirrors the C# compiler's
                    // pattern-matching lowering (overloads are tried most-derived-first; first
                    // true wins). A false return falls through to the default ResolveUnionCase
                    // path below so unions with partial TryGetValue coverage still dispatch the
                    // remaining cases by runtime type.
                    if (chainedTryGetValue is not null && chainedTryGetValue(union, out Type? matchedCaseType, out object? matchedValue))
                    {
                        return (matchedCaseType, matchedValue);
                    }

                    object? value = valueAccessor(union);
                    if (value is null)
                    {
                        if (nullableCase is null)
                        {
                            ThrowHelper.ThrowJsonException_UnionDoesNotAcceptNull(typeof(TUnion));
                        }

                        return (nullableCase.CaseType, null);
                    }

                    Type runtimeType = value.GetType();
                    UnionCaseEntry? entry = ResolveUnionCase(caseIndex, orderedCases, runtimeType);
                    if (entry is null)
                    {
                        ThrowHelper.ThrowJsonException_UnionRuntimeTypeNotMatchedToCase(typeof(TUnion), runtimeType);
                    }

                    return (entry.CaseType, value);
                };

                // Discovers public instance 'bool TryGetValue(out CaseType)' overloads on TUnion
                // matching declared case types and folds them into a single chained accessor
                // delegate. C# pattern matching for [Union] types lowers 'v is CaseType' to a
                // call to such overloads when present, so the reflection deconstructor must
                // honor the same convention to stay consistent with the source generator.
                [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
                [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
                UnionTryGetValueAccessor<TUnion>? PopulateTryGetValueMethod()
                {
                    KeyValuePair<Type, MethodInfo>[]? entries = PopulateTryGetValueMethods();
                    return entries is null
                        ? null
                        : MemberAccessor.Instance.CreateUnionTryGetValueAccessor<TUnion>(entries);

                    // Filter by the literal "TryGetValue" name in the reflection query: it cuts
                    // the candidate set down to the overloads we actually care about and also
                    // gives the IL trimmer a static signal that this method name is reflected
                    // over, so it can root the matching overloads instead of every public
                    // instance method on TUnion. The discovered overloads are then ordered
                    // most-derived-first via topological sort so when multiple of them can match
                    // the same instance the nearest declared case wins (mirrors the C#
                    // compiler's pattern-matching lowering on union types).
                    [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
                    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
                    KeyValuePair<Type, MethodInfo>[]? PopulateTryGetValueMethods()
                    {
                        Dictionary<Type, MethodInfo>? methodsByCaseType = null;
                        foreach (MemberInfo member in typeof(TUnion).GetMember("TryGetValue", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance))
                        {
                            MethodInfo method = (MethodInfo)member;
                            if (method.ReturnType != typeof(bool) || method.IsGenericMethodDefinition)
                            {
                                continue;
                            }

                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length != 1)
                            {
                                continue;
                            }

                            ParameterInfo parameter = parameters[0];
                            if (!parameter.IsOut || !parameter.ParameterType.IsByRef)
                            {
                                continue;
                            }

                            Type caseType = parameter.ParameterType.GetElementType()!;
                            if (!caseIndex.ContainsKey(caseType))
                            {
                                continue;
                            }

                            // First overload per case type wins; ignore later duplicates.
                            (methodsByCaseType ??= new()).TryAdd(caseType, method);
                        }

                        if (methodsByCaseType is null)
                        {
                            return null;
                        }

                        Type[] orderedCaseTypes = SortTypesByInheritanceHierarchy(
                            new List<Type>(methodsByCaseType.Keys).ToArray(),
                            mostDerivedTypesFirst: true);

                        KeyValuePair<Type, MethodInfo>[] orderedEntries = new KeyValuePair<Type, MethodInfo>[orderedCaseTypes.Length];
                        for (int i = 0; i < orderedCaseTypes.Length; i++)
                        {
                            Type caseType = orderedCaseTypes[i];
                            orderedEntries[i] = new KeyValuePair<Type, MethodInfo>(caseType, methodsByCaseType[caseType]);
                        }

                        return orderedEntries;
                    }
                }
            }

            private void PopulateUnionConstructor(
                JsonTypeInfo<TUnion> typeInfo,
                UnionCaseEntry[] orderedCases,
                ConcurrentDictionary<Type, UnionCaseEntry?> caseIndex)
            {
                Debug.Assert(typeInfo.UnionConstructor is null,
                    "PopulateUnionConstructor is only invoked from the built-in resolver, before any contract customization.");

                Func<object?, TUnion>? nullConstructor = null;
                foreach (UnionCaseEntry entry in _caseEntries)
                {
                    if (entry.CaseInfo.IsNullable)
                    {
                        nullConstructor = entry.Constructor;
                        break;
                    }
                }

                typeInfo.UnionConstructor = (Type caseType, object? value) =>
                {
                    if (value is null)
                    {
                        if (nullConstructor is null)
                        {
                            ThrowHelper.ThrowJsonException_UnionDoesNotAcceptNull(typeof(TUnion));
                        }

                        return nullConstructor(null);
                    }

                    UnionCaseEntry? entry = ResolveUnionCase(caseIndex, orderedCases, caseType);
                    if (entry is null)
                    {
                        ThrowHelper.ThrowJsonException_UnionRuntimeTypeNotMatchedToCase(typeof(TUnion), caseType);
                    }

                    return entry.Constructor(value);
                };
            }

            private static UnionCaseEntry? ResolveUnionCase(
                ConcurrentDictionary<Type, UnionCaseEntry?> caseIndex,
                UnionCaseEntry[] orderedCases,
                Type runtimeType)
            {
                if (caseIndex.TryGetValue(runtimeType, out UnionCaseEntry? cached))
                {
                    return cached;
                }

                // orderedCases is topologically sorted (most-derived first), so the first
                // ancestor match is also the nearest one.
                UnionCaseEntry? found = null;
                foreach (UnionCaseEntry entry in orderedCases)
                {
                    if (entry.CaseType.IsAssignableFrom(runtimeType))
                    {
                        found = entry;
                        break;
                    }
                }

                caseIndex[runtimeType] = found;
                return found;
            }

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            private static PropertyInfo? GetUnionValueProperty()
            {
                PropertyInfo? valueProperty = typeof(TUnion).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                return valueProperty is { PropertyType: { } propertyType } &&
                    propertyType == typeof(object) &&
                    valueProperty.GetMethod is { IsPublic: true } &&
                    valueProperty.GetIndexParameters().Length == 0
                    ? valueProperty
                    : null;
            }

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            private static UnionCaseEntry CreateUnionCaseEntry(Type caseType, ConstructorInfo constructorInfo, bool isNullable)
            {
                Func<object?, TUnion> constructor = MemberAccessor.CreateSingleParameterConstructor<TUnion>(constructorInfo);
                return new UnionCaseEntry(caseType, constructor, isNullable);
            }

            private sealed class UnionCaseEntry
            {
                public UnionCaseEntry(Type caseType, Func<object?, TUnion> constructor, bool isNullable)
                {
                    CaseType = caseType;
                    Constructor = constructor;
                    CaseInfo = new JsonUnionCaseInfo(caseType) { IsNullable = isNullable };
                }

                public Type CaseType { get; }
                public JsonUnionCaseInfo CaseInfo { get; }
                public Func<object?, TUnion> Constructor { get; }
            }
        }

        private static Type[] SortTypesByInheritanceHierarchy(Type[] types, bool mostDerivedTypesFirst)
        {
            if (types.Length <= 1)
            {
                return types;
            }

            Type root = typeof(void);
            Debug.Assert(Array.IndexOf(types, root) < 0);

            // Use typeof(void) as a synthetic root: it cannot be a case type and is not
            // in a subtype relationship with any valid case type.
            Type[] sortedTypesWithRoot = JsonHelpers.TraverseGraphWithTopologicalSort(root, GetInheritanceRelatedTypes);
            Debug.Assert(sortedTypesWithRoot.Length == types.Length + 1);
            Debug.Assert(sortedTypesWithRoot[0] == root);

            Type[] sortedTypes = new Type[types.Length];
            Array.Copy(sortedTypesWithRoot, sourceIndex: 1, sortedTypes, destinationIndex: 0, sortedTypes.Length);

            return sortedTypes;

            ICollection<Type> GetInheritanceRelatedTypes(Type type)
            {
                if (type == root)
                {
                    Type[] rootChildren = new Type[types.Length];
                    for (int i = 0; i < rootChildren.Length; i++)
                    {
                        // TraverseGraphWithTopologicalSort writes childless nodes from the
                        // end of the result, so enumerate root children in reverse to
                        // preserve the input order for unrelated types.
                        rootChildren[i] = types[rootChildren.Length - i - 1];
                    }

                    return rootChildren;
                }

                List<Type>? relatedTypes = null;
                foreach (Type candidate in types)
                {
                    bool isRelatedType = mostDerivedTypesFirst
                        ? candidate.IsAssignableFrom(type)
                        : type.IsAssignableFrom(candidate);

                    if (candidate != type && isRelatedType)
                    {
                        (relatedTypes ??= new()).Add(candidate);
                    }
                }

                return relatedTypes ?? (ICollection<Type>)Array.Empty<Type>();
            }
        }

        private static bool TryGetCaseType(
            ParameterInfo parameter,
            NullabilityInfoContext nullabilityCtx,
            [NotNullWhen(true)] out Type? caseType,
            out bool acceptsNull)
        {
            acceptsNull = false;
            Type parameterType = parameter.ParameterType;
            if (parameterType.IsByRef)
            {
                caseType = null;
                return false;
            }

            caseType = parameterType;
            if (Nullable.GetUnderlyingType(caseType) is Type underlying)
            {
                caseType = underlying;
            }

            if (parameterType.IsNullableType())
            {
                NullabilityInfo nullability = nullabilityCtx.Create(parameter);
                acceptsNull = nullability.WriteState is not NullabilityState.NotNull;
            }

            return true;
        }
    }
}
