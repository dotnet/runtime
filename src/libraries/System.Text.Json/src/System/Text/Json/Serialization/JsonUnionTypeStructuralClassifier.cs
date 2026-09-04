// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
#if NET
using System.Runtime.CompilerServices;
#endif
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Classifies JSON payloads into union case types by comparing their JSON value types
    /// and, for JSON objects, their property names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default union classification distinguishes cases that use different JSON token kinds,
    /// such as a string and an array. This classifier adds structural classification for .NET
    /// POCOs configured to serialize as JSON objects.
    /// </para>
    /// <para>
    /// To classify an object, the classifier starts with every POCO case as a candidate. For each
    /// property name at the level of the current JSON object, it eliminates candidates that do not
    /// define a matching property. Property values and nested content are not examined, and
    /// property order does not affect the result. Name matching honors
    /// <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/>.
    /// </para>
    /// <para>
    /// After reading the object, candidates missing a required property are eliminated. Missing
    /// optional properties have no effect. A property name that is not defined by any case
    /// eliminates only cases configured with <see cref="JsonUnmappedMemberHandling.Disallow"/>.
    /// </para>
    /// <para>
    /// The classifier selects the case when exactly one candidate remains. Classification fails
    /// when no candidates or multiple candidates remain. Configurations containing a case that can
    /// never be selected uniquely are rejected when the classifier is created.
    /// </para>
    /// <para>
    /// This classifier supports union types only. Nested union cases, polymorphic cases, and
    /// reference-preserving deserialization are not supported.
    /// </para>
    /// </remarks>
    public class JsonUnionTypeStructuralClassifier : JsonTypeClassifierFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonUnionTypeStructuralClassifier"/> class.
        /// </summary>
        public JsonUnionTypeStructuralClassifier()
        {
        }

        /// <inheritdoc/>
        public override bool CanClassify(JsonTypeClassifierContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return context.Kind is JsonTypeClassifierKind.Union;
        }

        /// <inheritdoc/>
        public override JsonTypeClassifier CreateJsonClassifier(
            JsonTypeClassifierContext context,
            JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(options);

            if (context.Kind is not JsonTypeClassifierKind.Union)
            {
                ThrowHelper.ThrowInvalidOperationException_UnionTypeStructuralClassifierOnlyForUnions(context.DeclaringType);
            }

            if (options.ReferenceHandlingStrategy is JsonKnownReferenceHandler.Preserve)
            {
                ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierPreserveReferencesNotSupported(context.DeclaringType);
            }

            StructuralClassifier classifier = BuildStructuralClassifier(context.DeclaringType, context.UnionCases, options);
            return classifier.Classify;
        }

        private static StructuralClassifier BuildStructuralClassifier(
            Type unionType,
            IReadOnlyList<JsonUnionCaseInfo> unionCases,
            JsonSerializerOptions options)
        {
            // POCO object cases expose JsonPropertyInfo metadata through JsonTypeInfoKind.Object.
            // A non-POCO JSON object case advertises the Object shape without such metadata.
            Dictionary<JsonValueType, Type> shapeBasedCases = new();
            List<PocoObjectCase> pocoObjectCaseList = [];
            int requiredPropertyCount = 0;

            foreach (JsonUnionCaseInfo unionCase in unionCases)
            {
                AddCase(
                    unionType,
                    unionCase.CaseType,
                    options,
                    shapeBasedCases,
                    pocoObjectCaseList,
                    ref requiredPropertyCount);
            }

            shapeBasedCases.TryGetValue(JsonValueType.Object, out Type? nonPocoJsonObjectCaseType);
            if (nonPocoJsonObjectCaseType is not null &&
                pocoObjectCaseList is { Count: > 0 })
            {
                ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierAmbiguousCases(
                    unionType,
                    pocoObjectCaseList[0].CaseType,
                    nonPocoJsonObjectCaseType,
                    JsonValueType.Object);
            }

            ValidatePocoObjectCases(unionType, pocoObjectCaseList);

            PocoObjectCase[] pocoObjectCases = [..pocoObjectCaseList];
            Dictionary<byte[], List<PocoPropertyClassifierInfo>> pocoPropertyIndex = new(JsonHelpers.ByteArrayOrdinalComparer.Instance);
            Dictionary<string, List<PocoPropertyClassifierInfo>>? caseInsensitivePocoPropertyIndex = null;

            // Keep the ordinal and case-insensitive indexes separate so exact matches can be
            // processed first when differently-cased names overlap.
            foreach (PocoObjectCase pocoObjectCase in pocoObjectCases)
            {
                foreach (PocoPropertyClassifierInfo property in pocoObjectCase.Properties)
                {
                    AddPocoPropertyInfo(pocoPropertyIndex, property.NameAsUtf8Bytes, property);

                    if (!property.IsCaseSensitive)
                    {
                        caseInsensitivePocoPropertyIndex ??= new(StringComparer.OrdinalIgnoreCase);
                        AddPocoPropertyInfo(caseInsensitivePocoPropertyIndex, property.Name, property);
                    }
                }
            }

            return new(
                shapeBasedCases,
                pocoObjectCases,
                pocoPropertyIndex,
                caseInsensitivePocoPropertyIndex,
                requiredPropertyCount);

            static void AddPocoPropertyInfo<TKey>(
                Dictionary<TKey, List<PocoPropertyClassifierInfo>> index,
                TKey key,
                PocoPropertyClassifierInfo propertyInfo)
                where TKey : notnull
            {
                if (!index.TryGetValue(key, out List<PocoPropertyClassifierInfo>? matches))
                {
                    matches = [];
                    index.Add(key, matches);
                }

                matches.Add(propertyInfo);
            }
        }

        private static void AddCase(
            Type unionType,
            Type caseType,
            JsonSerializerOptions options,
            Dictionary<JsonValueType, Type> shapeBasedCases,
            List<PocoObjectCase> pocoObjectCases,
            ref int requiredPropertyCount)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(caseType);
            if (typeInfo is { IsNullable: true, ElementTypeInfo: JsonTypeInfo elementTypeInfo })
            {
                typeInfo = elementTypeInfo;
            }

            if (typeInfo.Kind is JsonTypeInfoKind.Union)
            {
                ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierCaseNotSupported(
                    unionType,
                    caseType);
            }

            if (typeInfo.PolymorphismOptions is not null)
            {
                ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierCaseNotSupported(
                    unionType,
                    caseType);
            }

            JsonNumberHandling numberHandling = typeInfo.NumberHandling ?? options.NumberHandling;
            JsonValueType valueTypes = typeInfo.Converter.GetSupportedJsonValueTypes(numberHandling);

            bool isPocoObjectCase = typeInfo is
            {
                Kind: JsonTypeInfoKind.Object,
                Converter.IsInternalConverter: true,
            };

            ReadOnlySpan<JsonValueType> supportedValueTypes =
            [
                JsonValueType.Object,
                JsonValueType.Array,
                JsonValueType.String,
                JsonValueType.Number,
                JsonValueType.Boolean,
            ];

            // Token kind is the only discriminator for non-object shapes, so only one case can
            // claim each kind. POCO object cases instead participate in property matching.
            Debug.Assert((valueTypes &
                (JsonValueType.Object |
                 JsonValueType.Array |
                 JsonValueType.String |
                 JsonValueType.Number |
                 JsonValueType.Boolean)) is not 0);

            foreach (JsonValueType valueType in supportedValueTypes)
            {
                if ((valueTypes & valueType) is 0)
                {
                    continue;
                }

                if (valueType is JsonValueType.Object && isPocoObjectCase)
                {
                    pocoObjectCases.Add(BuildPocoObjectCase(
                        caseType,
                        typeInfo,
                        options,
                        pocoObjectCases.Count,
                        ref requiredPropertyCount));
                }
                else if (!shapeBasedCases.TryAdd(valueType, caseType))
                {
                    Type conflictingCaseType = shapeBasedCases[valueType];
                    ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierAmbiguousCases(
                        unionType,
                        conflictingCaseType,
                        caseType,
                        valueType);
                }
            }
        }

        private static PocoObjectCase BuildPocoObjectCase(
            Type caseType,
            JsonTypeInfo typeInfo,
            JsonSerializerOptions options,
            int pocoCaseIndex,
            ref int requiredPropertyCount)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);

            bool caseInsensitive = options.PropertyNameCaseInsensitive;
            List<PocoPropertyClassifierInfo> properties = new(typeInfo.Properties.Count);
            int requiredCount = 0;
            bool hasExtensionData = false;

            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                if (property.IsExtensionData)
                {
                    hasExtensionData = true;
                    continue;
                }

                int requiredPropertyIndex = property.IsRequired ? requiredPropertyCount++ : -1;
                properties.Add(new(
                    property.Name,
                    property.IsRequired,
                    isCaseSensitive: !caseInsensitive,
                    pocoCaseIndex,
                    requiredPropertyIndex));

                if (property.IsRequired)
                {
                    requiredCount++;
                }
            }

            JsonUnmappedMemberHandling unmappedMemberHandling = typeInfo.UnmappedMemberHandling ??
                (hasExtensionData ? JsonUnmappedMemberHandling.Skip : options.UnmappedMemberHandling);

            return new(
                caseType,
                [..properties],
                unmappedMemberHandling is JsonUnmappedMemberHandling.Disallow,
                requiredCount);
        }

        private static void ValidatePocoObjectCases(
            Type unionType,
            List<PocoObjectCase> pocoObjectCases)
        {
            // Reject a case when every object it can accept also satisfies another case, since it
            // can never be selected uniquely. This quadratic check runs only during construction
            // and is bounded by the union cases declared on the type.
            for (int i = 0; i < pocoObjectCases.Count; i++)
            {
                PocoObjectCase pocoObjectCase = pocoObjectCases[i];

                for (int j = 0; j < pocoObjectCases.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    PocoObjectCase other = pocoObjectCases[j];
                    if (pocoObjectCase.IsShadowedBy(other))
                    {
                        ThrowHelper.ThrowNotSupportedException_UnionTypeStructuralClassifierUnreachableObjectCase(
                            unionType,
                            pocoObjectCase.CaseType,
                            other.CaseType);
                    }
                }
            }

        }

        /// <summary>Classifies JSON values using precomputed union case metadata.</summary>
        private sealed class StructuralClassifier
        {
            // PocoObjectCaseState is 8 bytes, so this caps the stack-allocated buffer at 128 bytes.
            private const int InlinePocoObjectCaseCount = 16;

            private readonly Dictionary<byte[], List<PocoPropertyClassifierInfo>> _pocoPropertyIndex;
            private readonly Dictionary<string, List<PocoPropertyClassifierInfo>>? _caseInsensitivePocoPropertyIndex;
            private readonly int[] _disallowUnmappedPropertiesPocoCaseIndices;
            private readonly Dictionary<JsonValueType, Type> _shapeBasedCases;
            private readonly PocoObjectCase[] _pocoObjectCases;
            private readonly int _requiredPropertyCount;

            public StructuralClassifier(
                Dictionary<JsonValueType, Type> shapeBasedCases,
                PocoObjectCase[] pocoObjectCases,
                Dictionary<byte[], List<PocoPropertyClassifierInfo>> pocoPropertyIndex,
                Dictionary<string, List<PocoPropertyClassifierInfo>>? caseInsensitivePocoPropertyIndex,
                int requiredPropertyCount)
            {
                _shapeBasedCases = shapeBasedCases;
                _pocoObjectCases = pocoObjectCases;
                _pocoPropertyIndex = pocoPropertyIndex;
                _caseInsensitivePocoPropertyIndex = caseInsensitivePocoPropertyIndex;
                _requiredPropertyCount = requiredPropertyCount;

                List<int>? disallowUnmappedPropertiesPocoCaseIndices = null;
                for (int i = 0; i < pocoObjectCases.Length; i++)
                {
                    if (pocoObjectCases[i].DisallowUnmappedProperties)
                    {
                        (disallowUnmappedPropertiesPocoCaseIndices ??= []).Add(i);
                    }
                }

                _disallowUnmappedPropertiesPocoCaseIndices = disallowUnmappedPropertiesPocoCaseIndices?.ToArray() ?? [];
            }

            public Type? Classify(ref Utf8JsonReader reader)
            {
                JsonValueType valueType = reader.TokenType switch
                {
                    JsonTokenType.StartObject => JsonValueType.Object,
                    JsonTokenType.StartArray => JsonValueType.Array,
                    JsonTokenType.String => JsonValueType.String,
                    JsonTokenType.Number => JsonValueType.Number,
                    JsonTokenType.True or JsonTokenType.False => JsonValueType.Boolean,
                    _ => JsonValueType.None,
                };

                if (valueType is JsonValueType.Object && _pocoObjectCases is { Length: > 0 })
                {
                    return ClassifyJsonObject(ref reader);
                }

                _shapeBasedCases.TryGetValue(valueType, out Type? caseType);
                return caseType;
            }

            private Type? ClassifyJsonObject(ref Utf8JsonReader reader)
            {
                Debug.Assert(reader.TokenType is JsonTokenType.StartObject);
                Debug.Assert(_pocoObjectCases.Length is > 0);

                // Begin with every POCO object case as a candidate and eliminate cases using property
                // names only. After the complete object is scanned, required properties and
                // uniqueness determine the result.
                PocoObjectCaseState[]? rentedPocoCaseStates = null;
                try
                {
                    scoped Span<PocoObjectCaseState> pocoCaseStates =
                        _pocoObjectCases.Length <= InlinePocoObjectCaseCount
                            ? stackalloc PocoObjectCaseState[InlinePocoObjectCaseCount]
                            : (rentedPocoCaseStates = ArrayPool<PocoObjectCaseState>.Shared.Rent(_pocoObjectCases.Length));
                    pocoCaseStates = pocoCaseStates.Slice(0, _pocoObjectCases.Length);
                    pocoCaseStates.Clear();

                    scoped Span<ulong> pocoCaseCandidateBuffer = stackalloc ulong[ValueBitArray.ScratchBufferSize];
                    ValueBitArray isPocoCaseCandidate = new(
                        _pocoObjectCases.Length,
                        pocoCaseCandidateBuffer,
                        initialWordValue: ulong.MaxValue);

                    scoped Span<ulong> pocoCaseMatchesCurrentPropertyBuffer =
                        stackalloc ulong[ValueBitArray.ScratchBufferSize];
                    ValueBitArray pocoCaseMatchesCurrentProperty =
                        new(_pocoObjectCases.Length, pocoCaseMatchesCurrentPropertyBuffer);

                    scoped Span<ulong> requiredPropertySeenBuffer = stackalloc ulong[ValueBitArray.ScratchBufferSize];
                    ValueBitArray isRequiredPropertySeen =
                        new(_requiredPropertyCount, requiredPropertySeenBuffer);
                    int i = 0;

                    while (true)
                    {
                        reader.ReadWithVerify();
                        Debug.Assert(reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.EndObject);
                        if (reader.TokenType is JsonTokenType.EndObject)
                        {
                            return SelectJsonObjectCase(
                                _pocoObjectCases,
                                pocoCaseStates,
                                isPocoCaseCandidate);
                        }

                        ReadNextProperty(
                            ref reader,
                            propertyIndex: i++,
                            pocoCaseStates,
                            isPocoCaseCandidate,
                            pocoCaseMatchesCurrentProperty,
                            isRequiredPropertySeen);
                    }
                }
                finally
                {
                    if (rentedPocoCaseStates is not null)
                    {
#if NET
                        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<PocoObjectCaseState>());
#endif
                        ArrayPool<PocoObjectCaseState>.Shared.Return(
                            rentedPocoCaseStates,
                            clearArray: false);
                    }
                }

                static Type? SelectJsonObjectCase(
                    PocoObjectCase[] pocoObjectCases,
                    scoped ReadOnlySpan<PocoObjectCaseState> pocoCaseStates,
                    scoped ValueBitArray isPocoCaseCandidate)
                {
                    Type? selectedType = null;

                    for (int i = 0; i < pocoObjectCases.Length; i++)
                    {
                        PocoObjectCase pocoObjectCase = pocoObjectCases[i];
                        PocoObjectCaseState pocoCaseState = pocoCaseStates[i];
                        if (!isPocoCaseCandidate[i] ||
                            pocoCaseState.RequiredSeen < pocoObjectCase.RequiredCount)
                        {
                            continue;
                        }

                        if (selectedType is not null)
                        {
                            return null;
                        }

                        selectedType = pocoObjectCase.CaseType;
                    }

                    return selectedType;
                }
            }

            private void ReadNextProperty(
                scoped ref Utf8JsonReader reader,
                int propertyIndex,
                scoped Span<PocoObjectCaseState> pocoCaseStates,
                scoped ValueBitArray isPocoCaseCandidate,
                scoped ValueBitArray pocoCaseMatchesCurrentProperty,
                scoped ValueBitArray isRequiredPropertySeen)
            {
                byte[]? rentedPropertyName = null;
                try
                {
                    scoped Span<byte> buffer = default;
                    scoped ReadOnlySpan<byte> propertyName;

                    if (reader is { HasValueSequence: false, ValueIsEscaped: false })
                    {
                        propertyName = reader.ValueSpan;
                    }
                    else
                    {
                        int bufferLength = reader.ValueLength;
                        buffer = bufferLength is <= JsonConstants.StackallocByteThreshold
                            ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                            : (rentedPropertyName = ArrayPool<byte>.Shared.Rent(bufferLength));

                        int bytesWritten = reader.CopyString(buffer);
                        propertyName = buffer.Slice(0, bytesWritten);
                    }

                    reader.ReadWithVerify();

                    pocoCaseMatchesCurrentProperty.Clear();
                    RecordPropertyMatches(
                        propertyName,
                        propertyIndex,
                        pocoCaseStates,
                        isPocoCaseCandidate,
                        pocoCaseMatchesCurrentProperty,
                        isRequiredPropertySeen);

                    reader.SkipWithVerify();
                }
                finally
                {
                    if (rentedPropertyName is not null)
                    {
                        ArrayPool<byte>.Shared.Return(rentedPropertyName);
                    }
                }
            }

            private void RecordPropertyMatches(
                scoped ReadOnlySpan<byte> propertyName,
                int propertyIndex,
                scoped Span<PocoObjectCaseState> pocoCaseStates,
                scoped ValueBitArray isPocoCaseCandidate,
                scoped ValueBitArray pocoCaseMatchesCurrentProperty,
                scoped ValueBitArray isRequiredPropertySeen)
            {
                // A name known to any POCO case retains only the POCO cases that declare it; an
                // unknown name retains only POCO cases that permit unmapped properties.
                Debug.Assert(pocoCaseMatchesCurrentProperty.IsEmpty);
                bool isKnownPocoProperty = false;

                if (_pocoPropertyIndex.TryLookupUtf8Key(
                    propertyName,
                    out List<PocoPropertyClassifierInfo>? propertyMatches))
                {
                    isKnownPocoProperty = true;

                    foreach (PocoPropertyClassifierInfo propertyInfo in propertyMatches)
                    {
                        RecordPropertyMatch(
                            propertyInfo,
                            propertyIndex,
                            pocoCaseStates,
                            isPocoCaseCandidate,
                            pocoCaseMatchesCurrentProperty,
                            isRequiredPropertySeen);
                    }
                }

                if (_caseInsensitivePocoPropertyIndex is { } caseInsensitivePocoPropertyIndex &&
                    caseInsensitivePocoPropertyIndex.TryLookupUtf8Key(
                        propertyName,
                        out List<PocoPropertyClassifierInfo>? caseInsensitivePropertyMatches))
                {
                    isKnownPocoProperty = true;

                    foreach (PocoPropertyClassifierInfo propertyInfo in caseInsensitivePropertyMatches)
                    {
                        RecordPropertyMatch(
                            propertyInfo,
                            propertyIndex,
                            pocoCaseStates,
                            isPocoCaseCandidate,
                            pocoCaseMatchesCurrentProperty,
                            isRequiredPropertySeen);
                    }
                }

                if (isKnownPocoProperty)
                {
                    // An exact or case-insensitive lookup recognized the name, so retain only
                    // cases whose configured property metadata matched it.
                    isPocoCaseCandidate.IntersectWith(pocoCaseMatchesCurrentProperty);
                }
                else
                {
                    // No POCO case recognizes the name, so it eliminates only cases that reject
                    // unmapped properties.
                    foreach (int pocoCaseIndex in _disallowUnmappedPropertiesPocoCaseIndices)
                    {
                        isPocoCaseCandidate[pocoCaseIndex] = false;
                    }
                }

                static void RecordPropertyMatch(
                    PocoPropertyClassifierInfo propertyInfo,
                    int propertyIndex,
                    scoped Span<PocoObjectCaseState> pocoCaseStates,
                    scoped ValueBitArray isPocoCaseCandidate,
                    scoped ValueBitArray pocoCaseMatchesCurrentProperty,
                    scoped ValueBitArray isRequiredPropertySeen)
                {
                    ref PocoObjectCaseState pocoCaseState = ref pocoCaseStates[propertyInfo.PocoCaseIndex];
                    pocoCaseMatchesCurrentProperty[propertyInfo.PocoCaseIndex] = true;

                    // Exact matches take precedence over case-insensitive matches, and one JSON
                    // property can contribute to at most one declared property per POCO object case.
                    if (!isPocoCaseCandidate[propertyInfo.PocoCaseIndex] ||
                        pocoCaseState.LastMatchedPropertyIndex == propertyIndex)
                    {
                        return;
                    }

                    pocoCaseState.LastMatchedPropertyIndex = propertyIndex;
                    // RequiredPropertyIndex is unique per declared property, so duplicate JSON
                    // names cannot satisfy the same requirement more than once.
                    int requiredPropertyIndex = propertyInfo.RequiredPropertyIndex;
                    if (propertyInfo.IsRequired &&
                        !isRequiredPropertySeen[requiredPropertyIndex])
                    {
                        isRequiredPropertySeen[requiredPropertyIndex] = true;
                        pocoCaseState.RequiredSeen++;
                    }
                }
            }
        }

        /// <summary>Describes the POCO property contract used to classify a union case.</summary>
        private sealed class PocoObjectCase(
            Type caseType,
            PocoPropertyClassifierInfo[] properties,
            bool disallowUnmappedProperties,
            int requiredCount)
        {
            public Type CaseType { get; } = caseType;
            public bool DisallowUnmappedProperties { get; } = disallowUnmappedProperties;
            public PocoPropertyClassifierInfo[] Properties { get; } = properties;
            public int RequiredCount { get; } = requiredCount;

            public bool IsShadowedBy(PocoObjectCase other)
            {
                // This case is shadowed when every payload it accepts is also accepted by the
                // other case: the other case must be at least as permissive about unknown names,
                // recognize every property-name spelling accepted by this case, and require no
                // property that this case does not itself require.
                if (!DisallowUnmappedProperties && other.DisallowUnmappedProperties)
                {
                    return false;
                }

                foreach (PocoPropertyClassifierInfo property in Properties)
                {
                    bool otherHasEquivalentProperty = false;
                    foreach (PocoPropertyClassifierInfo otherProperty in other.Properties)
                    {
                        if (property.IsPropertyNameEquivalent(otherProperty))
                        {
                            otherHasEquivalentProperty = true;
                            break;
                        }
                    }

                    if (!otherHasEquivalentProperty)
                    {
                        return false;
                    }
                }

                foreach (PocoPropertyClassifierInfo otherProperty in other.Properties)
                {
                    if (!otherProperty.IsRequired)
                    {
                        continue;
                    }

                    bool requiredPropertyIsGuaranteedByThisCase = false;
                    foreach (PocoPropertyClassifierInfo property in Properties)
                    {
                        if (property.IsRequired && property.IsPropertyNameEquivalent(otherProperty))
                        {
                            requiredPropertyIsGuaranteedByThisCase = true;
                            break;
                        }
                    }

                    if (!requiredPropertyIsGuaranteedByThisCase)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Describes how a JSON property name contributes to a POCO object case.</summary>
        private sealed class PocoPropertyClassifierInfo(
            string name,
            bool isRequired,
            bool isCaseSensitive,
            int pocoCaseIndex,
            int requiredPropertyIndex)
        {
            public int PocoCaseIndex { get; } = pocoCaseIndex;
            public bool IsCaseSensitive { get; } = isCaseSensitive;
            public bool IsRequired { get; } = isRequired;
            public string Name { get; } = name;
            public byte[] NameAsUtf8Bytes { get; } = Encoding.UTF8.GetBytes(name);
            public int RequiredPropertyIndex { get; } = requiredPropertyIndex;

            public bool IsPropertyNameEquivalent(PocoPropertyClassifierInfo other)
            {
                Debug.Assert(IsCaseSensitive == other.IsCaseSensitive);

                return string.Equals(
                    Name,
                    other.Name,
                    IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Tracks per-case matching state while scanning a POCO JSON object.</summary>
        private struct PocoObjectCaseState
        {
            public int RequiredSeen;

            /// <summary>
            /// Gets or sets the zero-based index of the last matched JSON property, or -1 if none matched.
            /// </summary>
            public int LastMatchedPropertyIndex
            {
                readonly get => field - 1;
                set
                {
                    Debug.Assert(value >= 0);
                    field = value + 1;
                }
            }
        }
    }
}
