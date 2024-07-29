// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        // The source generator requires NRT and init-only property support.
        private const LanguageVersion MinimumSupportedLanguageVersion = LanguageVersion.CSharp9;

        private sealed class Parser
        {
            private const string SystemTextJsonNamespace = "System.Text.Json";
            private const string JsonExtensionDataAttributeFullName = "System.Text.Json.Serialization.JsonExtensionDataAttribute";
            private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
            private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";
            private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";
            private const string JsonObjectCreationHandlingAttributeFullName = "System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute";
            private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
            private const string JsonPropertyOrderAttributeFullName = "System.Text.Json.Serialization.JsonPropertyOrderAttribute";
            private const string JsonRequiredAttributeFullName = "System.Text.Json.Serialization.JsonRequiredAttribute";

            internal const string JsonSerializableAttributeFullName = "System.Text.Json.Serialization.JsonSerializableAttribute";

            private readonly KnownTypeSymbols _knownSymbols;
            private readonly bool _compilationContainsCoreJsonTypes;

            // Keeps track of generated context type names
            private readonly HashSet<(string ContextName, string TypeName)> _generatedContextAndTypeNames = new();

            private readonly HashSet<ITypeSymbol> _builtInSupportTypes;
            private readonly Queue<TypeToGenerate> _typesToGenerate = new();
#pragma warning disable RS1024 // Compare symbols correctly https://github.com/dotnet/roslyn-analyzers/issues/5804
            private readonly Dictionary<ITypeSymbol, TypeGenerationSpec> _generatedTypes = new(SymbolEqualityComparer.Default);
#pragma warning restore

            public List<DiagnosticInfo> Diagnostics { get; } = new();
            private Location? _contextClassLocation;

            public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
            {
                Debug.Assert(_contextClassLocation != null);

                if (location is null || !_knownSymbols.Compilation.ContainsLocation(location))
                {
                    // If location is null or is a location outside of the current compilation, fall back to the location of the context class.
                    location = _contextClassLocation;
                }

                Diagnostics.Add(DiagnosticInfo.Create(descriptor, location, messageArgs));
            }

            public Parser(KnownTypeSymbols knownSymbols)
            {
                _knownSymbols = knownSymbols;
                _compilationContainsCoreJsonTypes =
                    knownSymbols.JsonSerializerContextType != null &&
                    knownSymbols.JsonSerializableAttributeType != null &&
                    knownSymbols.JsonSourceGenerationOptionsAttributeType != null &&
                    knownSymbols.JsonConverterType != null;

                _builtInSupportTypes = (knownSymbols.BuiltInSupportTypes ??= CreateBuiltInSupportTypeSet(knownSymbols));
            }

            public ContextGenerationSpec? ParseContextGenerationSpec(ClassDeclarationSyntax contextClassDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                if (!_compilationContainsCoreJsonTypes)
                {
                    return null;
                }

                Debug.Assert(_knownSymbols.JsonSerializerContextType != null);

                // Ensure context-scoped metadata caches are empty.
                Debug.Assert(_typesToGenerate.Count == 0);
                Debug.Assert(_generatedTypes.Count == 0);
                Debug.Assert(_contextClassLocation is null);

                INamedTypeSymbol? contextTypeSymbol = semanticModel.GetDeclaredSymbol(contextClassDeclaration, cancellationToken);
                Debug.Assert(contextTypeSymbol != null);

                _contextClassLocation = contextTypeSymbol.GetLocation();
                Debug.Assert(_contextClassLocation is not null);

                if (!_knownSymbols.JsonSerializerContextType.IsAssignableFrom(contextTypeSymbol))
                {
                    ReportDiagnostic(DiagnosticDescriptors.JsonSerializableAttributeOnNonContextType, _contextClassLocation, contextTypeSymbol.ToDisplayString());
                    return null;
                }

                ParseJsonSerializerContextAttributes(contextTypeSymbol,
                    out List<TypeToGenerate>? rootSerializableTypes,
                    out SourceGenerationOptionsSpec? options);

                if (rootSerializableTypes is null)
                {
                    // No types were annotated with JsonSerializableAttribute.
                    // Can only be reached if a [JsonSerializable(null)] declaration has been made.
                    // Do not emit a diagnostic since a NRT warning will also be emitted.
                    return null;
                }

                Debug.Assert(rootSerializableTypes.Count > 0);

                LanguageVersion? langVersion = _knownSymbols.Compilation.GetLanguageVersion();
                if (langVersion is null or < MinimumSupportedLanguageVersion)
                {
                    // Unsupported lang version should be the first (and only) diagnostic emitted by the generator.
                    ReportDiagnostic(DiagnosticDescriptors.JsonUnsupportedLanguageVersion, _contextClassLocation, langVersion?.ToDisplayString(), MinimumSupportedLanguageVersion.ToDisplayString());
                    return null;
                }

                if (!TryGetNestedTypeDeclarations(contextClassDeclaration, semanticModel, cancellationToken, out List<string>? classDeclarationList))
                {
                    // Class or one of its containing types is not partial so we can't add to it.
                    ReportDiagnostic(DiagnosticDescriptors.ContextClassesMustBePartial, _contextClassLocation, contextTypeSymbol.Name);
                    return null;
                }

                // Enqueue attribute data for spec generation
                foreach (TypeToGenerate rootSerializableType in rootSerializableTypes)
                {
                    _typesToGenerate.Enqueue(rootSerializableType);
                }

                // Walk the transitive type graph generating specs for every encountered type.
                while (_typesToGenerate.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TypeToGenerate typeToGenerate = _typesToGenerate.Dequeue();
                    if (!_generatedTypes.ContainsKey(typeToGenerate.Type))
                    {
                        TypeGenerationSpec spec = ParseTypeGenerationSpec(typeToGenerate, contextTypeSymbol, options);
                        _generatedTypes.Add(typeToGenerate.Type, spec);
                    }
                }

                Debug.Assert(_generatedTypes.Count > 0);

                ContextGenerationSpec contextGenSpec = new()
                {
                    ContextType = new(contextTypeSymbol),
                    GeneratedTypes = _generatedTypes.Values.OrderBy(t => t.TypeRef.FullyQualifiedName).ToImmutableEquatableArray(),
                    Namespace = contextTypeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
                    ContextClassDeclarations = classDeclarationList.ToImmutableEquatableArray(),
                    GeneratedOptionsSpec = options,
                };

                // Clear the caches of generated metadata between the processing of context classes.
                _generatedTypes.Clear();
                _typesToGenerate.Clear();
                _contextClassLocation = null;
                return contextGenSpec;
            }

            private static bool TryGetNestedTypeDeclarations(ClassDeclarationSyntax contextClassSyntax, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out List<string>? typeDeclarations)
            {
                typeDeclarations = null;

                for (TypeDeclarationSyntax? currentType = contextClassSyntax; currentType != null; currentType = currentType.Parent as TypeDeclarationSyntax)
                {
                    StringBuilder stringBuilder = new();
                    bool isPartialType = false;

                    foreach (SyntaxToken modifier in currentType.Modifiers)
                    {
                        stringBuilder.Append(modifier.Text);
                        stringBuilder.Append(' ');
                        isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
                    }

                    if (!isPartialType)
                    {
                        typeDeclarations = null;
                        return false;
                    }

                    stringBuilder.Append(currentType.GetTypeKindKeyword());
                    stringBuilder.Append(' ');

                    INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(currentType, cancellationToken);
                    Debug.Assert(typeSymbol != null);

                    string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    stringBuilder.Append(typeName);

                    (typeDeclarations ??= new()).Add(stringBuilder.ToString());
                }

                Debug.Assert(typeDeclarations?.Count > 0);
                return true;
            }

            private TypeRef EnqueueType(ITypeSymbol type, JsonSourceGenerationMode? generationMode)
            {
                // Trim compile-time erased metadata such as tuple labels and NRT annotations.
                type = _knownSymbols.Compilation.EraseCompileTimeMetadata(type);

                if (_generatedTypes.TryGetValue(type, out TypeGenerationSpec? spec))
                {
                    return spec.TypeRef;
                }

                _typesToGenerate.Enqueue(new TypeToGenerate
                {
                    Type = type,
                    Mode = generationMode,
                    TypeInfoPropertyName = null,
                    Location = type.GetLocation(),
                    AttributeLocation = null,
                });

                return new TypeRef(type);
            }

            private void ParseJsonSerializerContextAttributes(
                INamedTypeSymbol contextClassSymbol,
                out List<TypeToGenerate>? rootSerializableTypes,
                out SourceGenerationOptionsSpec? options)
            {
                Debug.Assert(_knownSymbols.JsonSerializableAttributeType != null);
                Debug.Assert(_knownSymbols.JsonSourceGenerationOptionsAttributeType != null);

                rootSerializableTypes = null;
                options = null;

                foreach (AttributeData attributeData in contextClassSymbol.GetAttributes())
                {
                    INamedTypeSymbol? attributeClass = attributeData.AttributeClass;

                    if (SymbolEqualityComparer.Default.Equals(attributeClass, _knownSymbols.JsonSerializableAttributeType))
                    {
                        TypeToGenerate? typeToGenerate = ParseJsonSerializableAttribute(attributeData);
                        if (typeToGenerate is null)
                        {
                            continue;
                        }

                        (rootSerializableTypes ??= new()).Add(typeToGenerate.Value);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeClass, _knownSymbols.JsonSourceGenerationOptionsAttributeType))
                    {
                        options = ParseJsonSourceGenerationOptionsAttribute(contextClassSymbol, attributeData);
                    }
                }
            }

            private SourceGenerationOptionsSpec ParseJsonSourceGenerationOptionsAttribute(INamedTypeSymbol contextType, AttributeData attributeData)
            {
                JsonSourceGenerationMode? generationMode = null;
                List<TypeRef>? converters = null;
                JsonSerializerDefaults? defaults = null;
                bool? allowOutOfOrderMetadataProperties = null;
                bool? allowTrailingCommas = null;
                int? defaultBufferSize = null;
                JsonIgnoreCondition? defaultIgnoreCondition = null;
                JsonKnownNamingPolicy? dictionaryKeyPolicy = null;
                bool? respectNullableAnnotations = null;
                bool? ignoreReadOnlyFields = null;
                bool? respectRequiredConstructorParameters = null;
                bool? ignoreReadOnlyProperties = null;
                bool? includeFields = null;
                int? maxDepth = null;
                string? newLine = null;
                JsonNumberHandling? numberHandling = null;
                JsonObjectCreationHandling? preferredObjectCreationHandling = null;
                bool? propertyNameCaseInsensitive = null;
                JsonKnownNamingPolicy? propertyNamingPolicy = null;
                JsonCommentHandling? readCommentHandling = null;
                JsonUnknownTypeHandling? unknownTypeHandling = null;
                JsonUnmappedMemberHandling? unmappedMemberHandling = null;
                bool? useStringEnumConverter = null;
                bool? writeIndented = null;
                char? indentCharacter = null;
                int? indentSize = null;

                if (attributeData.ConstructorArguments.Length > 0)
                {
                    Debug.Assert(attributeData.ConstructorArguments.Length == 1 & attributeData.ConstructorArguments[0].Type?.Name is nameof(JsonSerializerDefaults));
                    defaults = (JsonSerializerDefaults)attributeData.ConstructorArguments[0].Value!;
                }

                foreach (KeyValuePair<string, TypedConstant> namedArg in attributeData.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(JsonSourceGenerationOptionsAttribute.AllowOutOfOrderMetadataProperties):
                            allowOutOfOrderMetadataProperties = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.AllowTrailingCommas):
                            allowTrailingCommas = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.Converters):
                            converters = new List<TypeRef>();
                            foreach (TypedConstant element in namedArg.Value.Values)
                            {
                                var converterType = (ITypeSymbol?)element.Value;
                                TypeRef? typeRef = GetConverterTypeFromAttribute(contextType, converterType, contextType, attributeData);
                                if (typeRef != null)
                                {
                                    converters.Add(typeRef);
                                }
                            }

                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.DefaultBufferSize):
                            defaultBufferSize = (int)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.DefaultIgnoreCondition):
                            defaultIgnoreCondition = (JsonIgnoreCondition)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.DictionaryKeyPolicy):
                            dictionaryKeyPolicy = (JsonKnownNamingPolicy)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.RespectNullableAnnotations):
                            respectNullableAnnotations = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.RespectRequiredConstructorParameters):
                            respectRequiredConstructorParameters = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.IgnoreReadOnlyFields):
                            ignoreReadOnlyFields = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.IgnoreReadOnlyProperties):
                            ignoreReadOnlyProperties = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.IncludeFields):
                            includeFields = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.MaxDepth):
                            maxDepth = (int)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.NewLine):
                            newLine = (string)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.NumberHandling):
                            numberHandling = (JsonNumberHandling)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.PreferredObjectCreationHandling):
                            preferredObjectCreationHandling = (JsonObjectCreationHandling)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.PropertyNameCaseInsensitive):
                            propertyNameCaseInsensitive = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.PropertyNamingPolicy):
                            propertyNamingPolicy = (JsonKnownNamingPolicy)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.ReadCommentHandling):
                            readCommentHandling = (JsonCommentHandling)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.UnknownTypeHandling):
                            unknownTypeHandling = (JsonUnknownTypeHandling)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.UnmappedMemberHandling):
                            unmappedMemberHandling = (JsonUnmappedMemberHandling)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.UseStringEnumConverter):
                            useStringEnumConverter = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.WriteIndented):
                            writeIndented = (bool)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.IndentCharacter):
                            indentCharacter = (char)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.IndentSize):
                            indentSize = (int)namedArg.Value.Value!;
                            break;

                        case nameof(JsonSourceGenerationOptionsAttribute.GenerationMode):
                            generationMode = (JsonSourceGenerationMode)namedArg.Value.Value!;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                }

                return new SourceGenerationOptionsSpec
                {
                    GenerationMode = generationMode,
                    Defaults = defaults,
                    AllowOutOfOrderMetadataProperties = allowOutOfOrderMetadataProperties,
                    AllowTrailingCommas = allowTrailingCommas,
                    DefaultBufferSize = defaultBufferSize,
                    Converters = converters?.ToImmutableEquatableArray(),
                    DefaultIgnoreCondition = defaultIgnoreCondition,
                    DictionaryKeyPolicy = dictionaryKeyPolicy,
                    RespectNullableAnnotations = respectNullableAnnotations,
                    RespectRequiredConstructorParameters = respectRequiredConstructorParameters,
                    IgnoreReadOnlyFields = ignoreReadOnlyFields,
                    IgnoreReadOnlyProperties = ignoreReadOnlyProperties,
                    IncludeFields = includeFields,
                    MaxDepth = maxDepth,
                    NewLine = newLine,
                    NumberHandling = numberHandling,
                    PreferredObjectCreationHandling = preferredObjectCreationHandling,
                    PropertyNameCaseInsensitive = propertyNameCaseInsensitive,
                    PropertyNamingPolicy = propertyNamingPolicy,
                    ReadCommentHandling = readCommentHandling,
                    UnknownTypeHandling = unknownTypeHandling,
                    UnmappedMemberHandling = unmappedMemberHandling,
                    UseStringEnumConverter = useStringEnumConverter,
                    WriteIndented = writeIndented,
                    IndentCharacter = indentCharacter,
                    IndentSize = indentSize,
                };
            }

            private TypeToGenerate? ParseJsonSerializableAttribute(AttributeData attributeData)
            {
                Debug.Assert(attributeData.ConstructorArguments.Length == 1);
                var typeSymbol = (ITypeSymbol?)attributeData.ConstructorArguments[0].Value;
                if (typeSymbol is null)
                {
                    return null;
                }

                JsonSourceGenerationMode? generationMode = null;
                string? typeInfoPropertyName = null;

                foreach (KeyValuePair<string, TypedConstant> namedArg in attributeData.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(JsonSerializableAttribute.TypeInfoPropertyName):
                            typeInfoPropertyName = (string)namedArg.Value.Value!;
                            break;
                        case nameof(JsonSerializableAttribute.GenerationMode):
                            generationMode = (JsonSourceGenerationMode)namedArg.Value.Value!;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                Location? location = typeSymbol.GetLocation();
                Location? attributeLocation = attributeData.GetLocation();
                Debug.Assert(attributeLocation != null);

                if (location is null || !_knownSymbols.Compilation.ContainsLocation(location))
                {
                    // For symbols located outside the compilation, fall back to attribute location instead.
                    location = attributeLocation;
                }

                return new TypeToGenerate
                {
                    Type = _knownSymbols.Compilation.EraseCompileTimeMetadata(typeSymbol),
                    Mode = generationMode,
                    TypeInfoPropertyName = typeInfoPropertyName,
                    Location = location,
                    AttributeLocation = attributeLocation,
                };
            }

            private TypeGenerationSpec ParseTypeGenerationSpec(in TypeToGenerate typeToGenerate, INamedTypeSymbol contextType, SourceGenerationOptionsSpec? options)
            {
                Debug.Assert(IsSymbolAccessibleWithin(typeToGenerate.Type, within: contextType), "should not generate metadata for inaccessible types.");

                ITypeSymbol type = typeToGenerate.Type;

                ClassType classType;
                JsonPrimitiveTypeKind? primitiveTypeKind = GetPrimitiveTypeKind(type);
                TypeRef? collectionKeyType = null;
                TypeRef? collectionValueType = null;
                TypeRef? nullableUnderlyingType = null;
                bool hasExtensionDataProperty = false;
                TypeRef? runtimeTypeRef = null;
                List<PropertyGenerationSpec>? propertySpecs = null;
                List<int>? fastPathPropertyIndices = null;
                ObjectConstructionStrategy constructionStrategy = default;
                bool constructorSetsRequiredMembers = false;
                ParameterGenerationSpec[]? ctorParamSpecs = null;
                List<PropertyInitializerGenerationSpec>? propertyInitializerSpecs = null;
                CollectionType collectionType = CollectionType.NotApplicable;
                string? immutableCollectionFactoryTypeFullName = null;
                bool implementsIJsonOnSerialized = false;
                bool implementsIJsonOnSerializing = false;

                ProcessTypeCustomAttributes(typeToGenerate, contextType,
                    out JsonNumberHandling? numberHandling,
                    out JsonUnmappedMemberHandling? unmappedMemberHandling,
                    out JsonObjectCreationHandling? preferredPropertyObjectCreationHandling,
                    out bool foundJsonConverterAttribute,
                    out TypeRef? customConverterType,
                    out bool isPolymorphic);

                if (type is INamedTypeSymbol { IsUnboundGenericType: true } or IErrorTypeSymbol)
                {
                    classType = ClassType.TypeUnsupportedBySourceGen;
                }
                else if (foundJsonConverterAttribute)
                {
                    classType = customConverterType != null
                        ? ClassType.TypeWithDesignTimeProvidedCustomConverter
                        : ClassType.TypeUnsupportedBySourceGen;
                }
                else if (IsBuiltInSupportType(type))
                {
                    classType = ClassType.BuiltInSupportType;
                }
                else if (IsUnsupportedType(type))
                {
                    classType = ClassType.UnsupportedType;
                }
                else if (type.IsNullableValueType(out ITypeSymbol? underlyingType))
                {
                    classType = ClassType.Nullable;
                    nullableUnderlyingType = EnqueueType(underlyingType, typeToGenerate.Mode);
                }
                else if (type.TypeKind is TypeKind.Enum)
                {
                    if (options?.UseStringEnumConverter == true)
                    {
                        Debug.Assert(_knownSymbols.JsonStringEnumConverterOfTType != null);
                        INamedTypeSymbol converterSymbol = _knownSymbols.JsonStringEnumConverterOfTType.Construct(type);

                        customConverterType = new TypeRef(converterSymbol);
                        classType = ClassType.TypeWithDesignTimeProvidedCustomConverter;
                    }
                    else
                    {
                        classType = ClassType.Enum;
                    }
                }
                else if (TryResolveCollectionType(type,
                    out ITypeSymbol? valueType,
                    out ITypeSymbol? keyType,
                    out collectionType,
                    out immutableCollectionFactoryTypeFullName,
                    out bool needsRuntimeType))
                {
                    if (!IsSymbolAccessibleWithin(valueType, within: contextType) ||
                        (keyType != null && !IsSymbolAccessibleWithin(keyType, within: contextType)))
                    {
                        classType = ClassType.UnsupportedType;
                        immutableCollectionFactoryTypeFullName = null;
                        collectionType = default;
                    }
                    else
                    {
                        if (type.CanUseDefaultConstructorForDeserialization(out IMethodSymbol? defaultCtor))
                        {
                            constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                            constructorSetsRequiredMembers = defaultCtor?.ContainsAttribute(_knownSymbols.SetsRequiredMembersAttributeType) == true;
                        }

                        classType = keyType != null ? ClassType.Dictionary : ClassType.Enumerable;
                        collectionValueType = EnqueueType(valueType, typeToGenerate.Mode);

                        if (keyType != null)
                        {
                            collectionKeyType = EnqueueType(keyType, typeToGenerate.Mode);

                            if (needsRuntimeType)
                            {
                                runtimeTypeRef = GetDictionaryTypeRef(keyType, valueType);
                            }
                        }
                    }
                }
                else
                {
                    bool useDefaultCtorInAnnotatedStructs = type.GetCompatibleGenericBaseType(_knownSymbols.KeyValuePair) is null;

                    if (!TryGetDeserializationConstructor(type, useDefaultCtorInAnnotatedStructs, out IMethodSymbol? constructor))
                    {
                        ReportDiagnostic(DiagnosticDescriptors.MultipleJsonConstructorAttribute, typeToGenerate.Location, type.ToDisplayString());
                    }
                    else if (constructor != null && !IsSymbolAccessibleWithin(constructor, within: contextType))
                    {
                        ReportDiagnostic(DiagnosticDescriptors.JsonConstructorInaccessible, typeToGenerate.Location, type.ToDisplayString());
                        constructor = null;
                    }

                    classType = ClassType.Object;

                    implementsIJsonOnSerializing = _knownSymbols.IJsonOnSerializingType.IsAssignableFrom(type);
                    implementsIJsonOnSerialized = _knownSymbols.IJsonOnSerializedType.IsAssignableFrom(type);

                    ctorParamSpecs = ParseConstructorParameters(typeToGenerate, constructor, out constructionStrategy, out constructorSetsRequiredMembers);
                    propertySpecs = ParsePropertyGenerationSpecs(contextType, typeToGenerate, options, out hasExtensionDataProperty, out fastPathPropertyIndices);
                    propertyInitializerSpecs = ParsePropertyInitializers(ctorParamSpecs, propertySpecs, constructorSetsRequiredMembers, ref constructionStrategy);
                }

                var typeRef = new TypeRef(type);
                string typeInfoPropertyName = typeToGenerate.TypeInfoPropertyName ?? GetTypeInfoPropertyName(type);

                if (classType is ClassType.TypeUnsupportedBySourceGen)
                {
                    ReportDiagnostic(DiagnosticDescriptors.TypeNotSupported, typeToGenerate.AttributeLocation ?? typeToGenerate.Location, type.ToDisplayString());
                }

                if (!_generatedContextAndTypeNames.Add((contextType.Name, typeInfoPropertyName)))
                {
                    // The context name/property name combination will result in a conflict in generated types.
                    // Workaround for https://github.com/dotnet/roslyn/issues/54185 by keeping track of the file names we've used.
                    ReportDiagnostic(DiagnosticDescriptors.DuplicateTypeName, typeToGenerate.AttributeLocation ?? _contextClassLocation, typeInfoPropertyName);
                    classType = ClassType.TypeUnsupportedBySourceGen;
                }

                return new TypeGenerationSpec
                {
                    TypeRef = typeRef,
                    TypeInfoPropertyName = typeInfoPropertyName,
                    GenerationMode = typeToGenerate.Mode ?? options?.GenerationMode ?? JsonSourceGenerationMode.Default,
                    ClassType = classType,
                    PrimitiveTypeKind = primitiveTypeKind,
                    IsPolymorphic = isPolymorphic,
                    NumberHandling = numberHandling,
                    UnmappedMemberHandling = unmappedMemberHandling,
                    PreferredPropertyObjectCreationHandling = preferredPropertyObjectCreationHandling,
                    PropertyGenSpecs = propertySpecs?.ToImmutableEquatableArray() ?? ImmutableEquatableArray<PropertyGenerationSpec>.Empty,
                    FastPathPropertyIndices = fastPathPropertyIndices?.ToImmutableEquatableArray(),
                    PropertyInitializerSpecs = propertyInitializerSpecs?.ToImmutableEquatableArray() ?? ImmutableEquatableArray<PropertyInitializerGenerationSpec>.Empty,
                    CtorParamGenSpecs = ctorParamSpecs?.ToImmutableEquatableArray() ?? ImmutableEquatableArray<ParameterGenerationSpec>.Empty,
                    CollectionType = collectionType,
                    CollectionKeyType = collectionKeyType,
                    CollectionValueType = collectionValueType,
                    ConstructionStrategy = constructionStrategy,
                    ConstructorSetsRequiredParameters = constructorSetsRequiredMembers,
                    NullableUnderlyingType = nullableUnderlyingType,
                    RuntimeTypeRef = runtimeTypeRef,
                    IsValueTuple = type.IsTupleType,
                    HasExtensionDataPropertyType = hasExtensionDataProperty,
                    ConverterType = customConverterType,
                    ImplementsIJsonOnSerialized = implementsIJsonOnSerialized,
                    ImplementsIJsonOnSerializing = implementsIJsonOnSerializing,
                    ImmutableCollectionFactoryMethod = DetermineImmutableCollectionFactoryMethod(immutableCollectionFactoryTypeFullName),
                };
            }

            private void ProcessTypeCustomAttributes(
                in TypeToGenerate typeToGenerate,
                INamedTypeSymbol contextType,
                out JsonNumberHandling? numberHandling,
                out JsonUnmappedMemberHandling? unmappedMemberHandling,
                out JsonObjectCreationHandling? objectCreationHandling,
                out bool foundJsonConverterAttribute,
                out TypeRef? customConverterType,
                out bool isPolymorphic)
            {
                numberHandling = null;
                unmappedMemberHandling = null;
                objectCreationHandling = null;
                customConverterType = null;
                foundJsonConverterAttribute = false;
                isPolymorphic = false;

                foreach (AttributeData attributeData in typeToGenerate.Type.GetAttributes())
                {
                    INamedTypeSymbol? attributeType = attributeData.AttributeClass;

                    if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonNumberHandlingAttributeType))
                    {
                        numberHandling = (JsonNumberHandling)attributeData.ConstructorArguments[0].Value!;
                        continue;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonUnmappedMemberHandlingAttributeType))
                    {
                        unmappedMemberHandling = (JsonUnmappedMemberHandling)attributeData.ConstructorArguments[0].Value!;
                        continue;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonObjectCreationHandlingAttributeType))
                    {
                        objectCreationHandling = (JsonObjectCreationHandling)attributeData.ConstructorArguments[0].Value!;
                        continue;
                    }
                    else if (!foundJsonConverterAttribute && _knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeType))
                    {
                        customConverterType = GetConverterTypeFromJsonConverterAttribute(contextType, typeToGenerate.Type, attributeData);
                        foundJsonConverterAttribute = true;
                    }

                    if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonDerivedTypeAttributeType))
                    {
                        Debug.Assert(attributeData.ConstructorArguments.Length > 0);
                        var derivedType = (ITypeSymbol)attributeData.ConstructorArguments[0].Value!;
                        EnqueueType(derivedType, typeToGenerate.Mode);

                        if (!isPolymorphic && typeToGenerate.Mode == JsonSourceGenerationMode.Serialization)
                        {
                            ReportDiagnostic(DiagnosticDescriptors.PolymorphismNotSupported, typeToGenerate.Location, typeToGenerate.Type.ToDisplayString());
                        }

                        isPolymorphic = true;
                    }
                }
            }

            private bool TryResolveCollectionType(
                ITypeSymbol type,
                [NotNullWhen(true)] out ITypeSymbol? valueType,
                out ITypeSymbol? keyType,
                out CollectionType collectionType,
                out string? immutableCollectionFactoryTypeFullName,
                out bool needsRuntimeType)
            {
                INamedTypeSymbol? actualTypeToConvert;
                valueType = null;
                keyType = null;
                collectionType = default;
                immutableCollectionFactoryTypeFullName = null;
                needsRuntimeType = false;

                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _knownSymbols.MemoryType))
                {
                    Debug.Assert(!SymbolEqualityComparer.Default.Equals(type, _knownSymbols.MemoryByteType));
                    valueType = ((INamedTypeSymbol)type).TypeArguments[0];
                    collectionType = CollectionType.MemoryOfT;
                    return true;
                }

                if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _knownSymbols.ReadOnlyMemoryType))
                {
                    Debug.Assert(!SymbolEqualityComparer.Default.Equals(type, _knownSymbols.ReadOnlyMemoryByteType));
                    valueType = ((INamedTypeSymbol)type).TypeArguments[0];
                    collectionType = CollectionType.ReadOnlyMemoryOfT;
                    return true;
                }

                // IAsyncEnumerable<T> takes precedence over IEnumerable.
                if (type.GetCompatibleGenericBaseType(_knownSymbols.IAsyncEnumerableOfTType) is INamedTypeSymbol iAsyncEnumerableType)
                {
                    valueType = iAsyncEnumerableType.TypeArguments[0];
                    collectionType = CollectionType.IAsyncEnumerableOfT;
                    return true;
                }

                if (!_knownSymbols.IEnumerableType.IsAssignableFrom(type))
                {
                    // Type is not IEnumerable and therefore not a collection type
                    return false;
                }

                if (type is IArrayTypeSymbol arraySymbol)
                {
                    Debug.Assert(arraySymbol.Rank == 1, "multi-dimensional arrays should have been handled earlier.");
                    collectionType = CollectionType.Array;
                    valueType = arraySymbol.ElementType;
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.KeyedCollectionType)) != null)
                {
                    collectionType = CollectionType.ICollectionOfT;
                    valueType = actualTypeToConvert.TypeArguments[1];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ListOfTType)) != null)
                {
                    collectionType = CollectionType.List;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.DictionaryOfTKeyTValueType)) != null)
                {
                    collectionType = CollectionType.Dictionary;
                    keyType = actualTypeToConvert.TypeArguments[0];
                    valueType = actualTypeToConvert.TypeArguments[1];
                }
                else if (_knownSymbols.IsImmutableDictionaryType(type, out immutableCollectionFactoryTypeFullName))
                {
                    collectionType = CollectionType.ImmutableDictionary;
                    ImmutableArray<ITypeSymbol> genericArgs = ((INamedTypeSymbol)type).TypeArguments;
                    keyType = genericArgs[0];
                    valueType = genericArgs[1];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IDictionaryOfTKeyTValueType)) != null)
                {
                    collectionType = CollectionType.IDictionaryOfTKeyTValue;
                    keyType = actualTypeToConvert.TypeArguments[0];
                    valueType = actualTypeToConvert.TypeArguments[1];
                    needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IReadonlyDictionaryOfTKeyTValueType)) != null)
                {
                    collectionType = CollectionType.IReadOnlyDictionary;
                    keyType = actualTypeToConvert.TypeArguments[0];
                    valueType = actualTypeToConvert.TypeArguments[1];
                    needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                }
                else if (_knownSymbols.IsImmutableEnumerableType(type, out immutableCollectionFactoryTypeFullName))
                {
                    collectionType = CollectionType.ImmutableEnumerable;
                    valueType = ((INamedTypeSymbol)type).TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IListOfTType)) != null)
                {
                    collectionType = CollectionType.IListOfT;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ISetOfTType)) != null)
                {
                    collectionType = CollectionType.ISet;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ICollectionOfTType)) != null)
                {
                    collectionType = CollectionType.ICollectionOfT;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.StackOfTType)) != null)
                {
                    collectionType = CollectionType.StackOfT;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.QueueOfTType)) != null)
                {
                    collectionType = CollectionType.QueueOfT;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ConcurrentStackType)) != null)
                {
                    collectionType = CollectionType.ConcurrentStack;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ConcurrentQueueType)) != null)
                {
                    collectionType = CollectionType.ConcurrentQueue;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IEnumerableOfTType)) != null)
                {
                    collectionType = CollectionType.IEnumerableOfT;
                    valueType = actualTypeToConvert.TypeArguments[0];
                }
                else if (_knownSymbols.IDictionaryType.IsAssignableFrom(type))
                {
                    collectionType = CollectionType.IDictionary;
                    keyType = _knownSymbols.StringType;
                    valueType = _knownSymbols.ObjectType;
                    needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                }
                else if (_knownSymbols.IListType.IsAssignableFrom(type))
                {
                    collectionType = CollectionType.IList;
                    valueType = _knownSymbols.ObjectType;
                }
                else if (_knownSymbols.StackType.IsAssignableFrom(type))
                {
                    collectionType = CollectionType.Stack;
                    valueType = _knownSymbols.ObjectType;
                }
                else if (_knownSymbols.QueueType.IsAssignableFrom(type))
                {
                    collectionType = CollectionType.Queue;
                    valueType = _knownSymbols.ObjectType;
                }
                else
                {
                    collectionType = CollectionType.IEnumerable;
                    valueType = _knownSymbols.ObjectType;
                }

                return true;
            }

            private TypeRef? GetDictionaryTypeRef(ITypeSymbol keyType, ITypeSymbol valueType)
            {
                INamedTypeSymbol? dictionary = _knownSymbols.DictionaryOfTKeyTValueType?.Construct(keyType, valueType);
                return dictionary is null ? null : new TypeRef(dictionary);
            }

            private List<PropertyGenerationSpec> ParsePropertyGenerationSpecs(
                INamedTypeSymbol contextType,
                in TypeToGenerate typeToGenerate,
                SourceGenerationOptionsSpec? options,
                out bool hasExtensionDataProperty,
                out List<int>? fastPathPropertyIndices)
            {
                Location? typeLocation = typeToGenerate.Location;
                List<PropertyGenerationSpec> properties = new();
                PropertyHierarchyResolutionState state = new(options);
                hasExtensionDataProperty = false;

                // Walk the type hierarchy starting from the current type up to the base type(s)
                foreach (INamedTypeSymbol currentType in typeToGenerate.Type.GetSortedTypeHierarchy())
                {
                    var declaringTypeRef = new TypeRef(currentType);
                    ImmutableArray<ISymbol> members = currentType.GetMembers();

                    foreach (IPropertySymbol propertyInfo in members.OfType<IPropertySymbol>())
                    {
                        // Skip if:
                        if (
                            // property is static or an indexer
                            propertyInfo.IsStatic || propertyInfo.Parameters.Length > 0 ||
                            // It is overridden by a derived property
                            PropertyIsOverriddenAndIgnored(propertyInfo, state.IgnoredMembers))
                        {
                            continue;
                        }

                        AddMember(
                            declaringTypeRef,
                            memberType: propertyInfo.Type,
                            memberInfo: propertyInfo,
                            typeToGenerate.Mode,
                            ref state,
                            ref hasExtensionDataProperty);
                    }

                    foreach (IFieldSymbol fieldInfo in members.OfType<IFieldSymbol>())
                    {
                        // Skip if :
                        if (
                            // it is a static field, constant
                            fieldInfo.IsStatic || fieldInfo.IsConst ||
                            // it is a compiler-generated backing field
                            fieldInfo.AssociatedSymbol != null ||
                            // symbol represents an explicitly named tuple element
                            fieldInfo.IsExplicitlyNamedTupleElement)
                        {
                            continue;
                        }

                        AddMember(
                            declaringTypeRef,
                            memberType: fieldInfo.Type,
                            memberInfo: fieldInfo,
                            typeToGenerate.Mode,
                            ref state,
                            ref hasExtensionDataProperty);
                    }
                }

                if (state.IsPropertyOrderSpecified)
                {
                    state.Properties.StableSortByKey(i => properties[i].Order);
                }

                fastPathPropertyIndices = state.HasInvalidConfigurationForFastPath ? null : state.Properties;
                return properties;

                void AddMember(
                    TypeRef declaringTypeRef,
                    ITypeSymbol memberType,
                    ISymbol memberInfo,
                    JsonSourceGenerationMode? generationMode,
                    ref PropertyHierarchyResolutionState state,
                    ref bool hasExtensionDataProperty)
                {
                    PropertyGenerationSpec? propertySpec = ParsePropertyGenerationSpec(
                        contextType,
                        declaringTypeRef,
                        typeLocation,
                        memberType,
                        memberInfo,
                        ref hasExtensionDataProperty,
                        generationMode,
                        options);

                    if (propertySpec is null)
                    {
                        // ignored invalid property
                        return;
                    }

                    AddPropertyWithConflictResolution(propertySpec, memberInfo, propertyIndex: properties.Count, ref state);
                    properties.Add(propertySpec);

                    // In case of JsonInclude fail if either:
                    // 1. the getter is not accessible by the source generator or
                    // 2. neither getter or setter methods are public.
                    if (propertySpec.HasJsonInclude && (!propertySpec.CanUseGetter || !propertySpec.IsPublic))
                    {
                        state.HasInvalidConfigurationForFastPath = true;
                    }
                }

                bool PropertyIsOverriddenAndIgnored(IPropertySymbol property, Dictionary<string, ISymbol>? ignoredMembers)
                {
                    return property.IsVirtual() &&
                        ignoredMembers?.TryGetValue(property.Name, out ISymbol? ignoredMember) == true &&
                        ignoredMember.IsVirtual() &&
                        SymbolEqualityComparer.Default.Equals(property.Type, ignoredMember.GetMemberType());
                }
            }

            private ref struct PropertyHierarchyResolutionState(SourceGenerationOptionsSpec? options)
            {
                public readonly List<int> Properties = new();
                public Dictionary<string, (PropertyGenerationSpec, ISymbol, int index)> AddedProperties = new(options?.PropertyNameCaseInsensitive == true ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                public Dictionary<string, ISymbol>? IgnoredMembers;
                public bool IsPropertyOrderSpecified;
                public bool HasInvalidConfigurationForFastPath;
            }

            /// <summary>
            /// Runs the property name conflict resolution algorithm for the fast-path serializer
            /// using the compile-time specified naming policies. Note that the metadata serializer
            /// does not consult these results since its own run-time configuration can be different.
            /// Instead the same algorithm is executed at run-time within the JsonTypeInfo infrastructure.
            /// </summary>
            private static void AddPropertyWithConflictResolution(
                PropertyGenerationSpec propertySpec,
                ISymbol memberInfo,
                int propertyIndex,
                ref PropertyHierarchyResolutionState state)
            {
                // Algorithm should be kept in sync with the runtime equivalent in JsonTypeInfo.cs
                string memberName = propertySpec.MemberName;

                if (state.AddedProperties.TryAdd(propertySpec.EffectiveJsonPropertyName, (propertySpec, memberInfo, state.Properties.Count)))
                {
                    state.Properties.Add(propertyIndex);
                    state.IsPropertyOrderSpecified |= propertySpec.Order != 0;
                }
                else
                {
                    // The JsonPropertyNameAttribute or naming policy resulted in a collision.
                    (PropertyGenerationSpec other, ISymbol otherSymbol, int index) = state.AddedProperties[propertySpec.EffectiveJsonPropertyName];

                    if (other.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                    {
                        // Overwrite previously cached property since it has [JsonIgnore].
                        state.AddedProperties[propertySpec.EffectiveJsonPropertyName] = (propertySpec, memberInfo, index);
                        state.Properties[index] = propertyIndex;
                        state.IsPropertyOrderSpecified |= propertySpec.Order != 0;
                    }
                    else
                    {
                        bool ignoreCurrentProperty =
                            // Does the current property have `JsonIgnoreAttribute`?
                            propertySpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always ||
                            // Is the current property hidden by the previously cached property
                            // (with `new` keyword, or by overriding)?
                            memberInfo.IsOverriddenOrShadowedBy(otherSymbol) ||
                            // Was a property with the same CLR name ignored? That property hid the current property,
                            // thus, if it was ignored, the current property should be ignored too.
                            (state.IgnoredMembers?.TryGetValue(memberName, out ISymbol? ignored) == true && memberInfo.IsOverriddenOrShadowedBy(ignored));

                        if (!ignoreCurrentProperty)
                        {
                            // Property name conflict cannot be reconciled
                            // Signal the fast-path generator to emit a throwing stub method.
                            state.HasInvalidConfigurationForFastPath = true;
                        }
                    }
                }

                if (propertySpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                {
                    (state.IgnoredMembers ??= new())[memberName] = memberInfo;
                }
            }

            private bool IsValidDataExtensionPropertyType(ITypeSymbol type)
            {
                if (SymbolEqualityComparer.Default.Equals(type, _knownSymbols.JsonObjectType))
                {
                    return true;
                }

                INamedTypeSymbol? actualDictionaryType = type.GetCompatibleGenericBaseType(_knownSymbols.IDictionaryOfTKeyTValueType);
                if (actualDictionaryType == null)
                {
                    return false;
                }

                return SymbolEqualityComparer.Default.Equals(actualDictionaryType.TypeArguments[0], _knownSymbols.StringType) &&
                        (SymbolEqualityComparer.Default.Equals(actualDictionaryType.TypeArguments[1], _knownSymbols.ObjectType) ||
                         SymbolEqualityComparer.Default.Equals(actualDictionaryType.TypeArguments[1], _knownSymbols.JsonElementType));
            }

            private PropertyGenerationSpec? ParsePropertyGenerationSpec(
                INamedTypeSymbol contextType,
                TypeRef declaringType,
                Location? typeLocation,
                ITypeSymbol memberType,
                ISymbol memberInfo,
                ref bool typeHasExtensionDataProperty,
                JsonSourceGenerationMode? generationMode,
                SourceGenerationOptionsSpec? options)
            {
                Debug.Assert(memberInfo is IFieldSymbol or IPropertySymbol);

                ProcessMemberCustomAttributes(
                    contextType,
                    memberInfo,
                    out bool hasJsonInclude,
                    out string? jsonPropertyName,
                    out JsonIgnoreCondition? ignoreCondition,
                    out JsonNumberHandling? numberHandling,
                    out JsonObjectCreationHandling? objectCreationHandling,
                    out TypeRef? converterType,
                    out int order,
                    out bool isExtensionData,
                    out bool hasJsonRequiredAttribute);

                ProcessMember(
                    contextType,
                    memberInfo,
                    hasJsonInclude,
                    out bool isReadOnly,
                    out bool isAccessible,
                    out bool isRequired,
                    out bool canUseGetter,
                    out bool canUseSetter,
                    out bool hasJsonIncludeButIsInaccessible,
                    out bool setterIsInitOnly,
                    out bool isGetterNonNullable,
                    out bool isSetterNonNullable);

                if (hasJsonIncludeButIsInaccessible)
                {
                    ReportDiagnostic(DiagnosticDescriptors.InaccessibleJsonIncludePropertiesNotSupported, memberInfo.GetLocation(), declaringType.Name, memberInfo.Name);
                }

                if (isExtensionData)
                {
                    if (typeHasExtensionDataProperty)
                    {
                        ReportDiagnostic(DiagnosticDescriptors.MultipleJsonExtensionDataAttribute, typeLocation, declaringType.Name);
                    }

                    if (!IsValidDataExtensionPropertyType(memberType))
                    {
                        ReportDiagnostic(DiagnosticDescriptors.DataExtensionPropertyInvalid, memberInfo.GetLocation(), declaringType.Name, memberInfo.Name);
                    }

                    typeHasExtensionDataProperty = true;
                }

                if ((!canUseGetter && !canUseSetter && !hasJsonIncludeButIsInaccessible) ||
                    !IsSymbolAccessibleWithin(memberType, within: contextType))
                {
                    // Skip the member if either of the two conditions hold
                    // 1. Member has no accessible getters or setters (but is not marked with JsonIncludeAttribute since we need to throw a runtime exception) OR
                    // 2. The member type is not accessible within the generated context.
                    return null;
                }

                string effectiveJsonPropertyName = DetermineEffectiveJsonPropertyName(memberInfo.Name, jsonPropertyName, options);
                string propertyNameFieldName = DeterminePropertyNameFieldName(effectiveJsonPropertyName);

                // Enqueue the property type for generation, unless the member is ignored.
                TypeRef propertyTypeRef = ignoreCondition != JsonIgnoreCondition.Always
                    ? EnqueueType(memberType, generationMode)
                    : new TypeRef(memberType);

                return new PropertyGenerationSpec
                {
                    NameSpecifiedInSourceCode = memberInfo.MemberNameNeedsAtSign() ? "@" + memberInfo.Name : memberInfo.Name,
                    MemberName = memberInfo.Name,
                    IsProperty = memberInfo is IPropertySymbol,
                    IsPublic = isAccessible,
                    IsVirtual = memberInfo.IsVirtual(),
                    JsonPropertyName = jsonPropertyName,
                    EffectiveJsonPropertyName = effectiveJsonPropertyName,
                    PropertyNameFieldName = propertyNameFieldName,
                    IsReadOnly = isReadOnly,
                    IsRequired = isRequired,
                    HasJsonRequiredAttribute = hasJsonRequiredAttribute,
                    IsInitOnlySetter = setterIsInitOnly,
                    CanUseGetter = canUseGetter,
                    CanUseSetter = canUseSetter,
                    DefaultIgnoreCondition = ignoreCondition,
                    NumberHandling = numberHandling,
                    ObjectCreationHandling = objectCreationHandling,
                    Order = order,
                    HasJsonInclude = hasJsonInclude,
                    IsExtensionData = isExtensionData,
                    PropertyType = propertyTypeRef,
                    DeclaringType = declaringType,
                    ConverterType = converterType,
                    IsGetterNonNullableAnnotation = isGetterNonNullable,
                    IsSetterNonNullableAnnotation = isSetterNonNullable,
                };
            }

            private void ProcessMemberCustomAttributes(
                INamedTypeSymbol contextType,
                ISymbol memberInfo,
                out bool hasJsonInclude,
                out string? jsonPropertyName,
                out JsonIgnoreCondition? ignoreCondition,
                out JsonNumberHandling? numberHandling,
                out JsonObjectCreationHandling? objectCreationHandling,
                out TypeRef? converterType,
                out int order,
                out bool isExtensionData,
                out bool hasJsonRequiredAttribute)
            {
                Debug.Assert(memberInfo is IFieldSymbol or IPropertySymbol);

                hasJsonInclude = false;
                jsonPropertyName = null;
                ignoreCondition = default;
                numberHandling = default;
                objectCreationHandling = default;
                converterType = null;
                order = 0;
                isExtensionData = false;
                hasJsonRequiredAttribute = false;

                foreach (AttributeData attributeData in memberInfo.GetAttributes())
                {
                    INamedTypeSymbol? attributeType = attributeData.AttributeClass;

                    if (attributeType is null)
                    {
                        continue;
                    }

                    if (converterType is null && _knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeType))
                    {
                        converterType = GetConverterTypeFromJsonConverterAttribute(contextType, memberInfo, attributeData);
                    }
                    else if (attributeType.ContainingAssembly.Name == SystemTextJsonNamespace)
                    {
                        switch (attributeType.ToDisplayString())
                        {
                            case JsonIgnoreAttributeFullName:
                                {
                                    ImmutableArray<KeyValuePair<string, TypedConstant>> namedArgs = attributeData.NamedArguments;

                                    if (namedArgs.Length == 0)
                                    {
                                        ignoreCondition = JsonIgnoreCondition.Always;
                                    }
                                    else if (namedArgs.Length == 1 &&
                                        namedArgs[0].Value.Type?.ToDisplayString() == JsonIgnoreConditionFullName)
                                    {
                                        ignoreCondition = (JsonIgnoreCondition)namedArgs[0].Value.Value!;
                                    }
                                }
                                break;
                            case JsonIncludeAttributeFullName:
                                {
                                    hasJsonInclude = true;
                                }
                                break;
                            case JsonNumberHandlingAttributeFullName:
                                {
                                    ImmutableArray<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                                    numberHandling = (JsonNumberHandling)ctorArgs[0].Value!;
                                }
                                break;
                            case JsonObjectCreationHandlingAttributeFullName:
                                {
                                    ImmutableArray<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                                    objectCreationHandling = (JsonObjectCreationHandling)ctorArgs[0].Value!;
                                }
                                break;
                            case JsonPropertyNameAttributeFullName:
                                {
                                    ImmutableArray<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                                    jsonPropertyName = (string)ctorArgs[0].Value!;
                                    // Null check here is done at runtime within JsonSerializer.
                                }
                                break;
                            case JsonPropertyOrderAttributeFullName:
                                {
                                    ImmutableArray<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                                    order = (int)ctorArgs[0].Value!;
                                }
                                break;
                            case JsonExtensionDataAttributeFullName:
                                {
                                    isExtensionData = true;
                                }
                                break;
                            case JsonRequiredAttributeFullName:
                                {
                                    hasJsonRequiredAttribute = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            private void ProcessMember(
                INamedTypeSymbol contextType,
                ISymbol memberInfo,
                bool hasJsonInclude,
                out bool isReadOnly,
                out bool isAccessible,
                out bool isRequired,
                out bool canUseGetter,
                out bool canUseSetter,
                out bool hasJsonIncludeButIsInaccessible,
                out bool isSetterInitOnly,
                out bool isGetterNonNullable,
                out bool isSetterNonNullable)
            {
                isAccessible = false;
                isReadOnly = false;
                isRequired = false;
                canUseGetter = false;
                canUseSetter = false;
                hasJsonIncludeButIsInaccessible = false;
                isSetterInitOnly = false;
                isGetterNonNullable = false;
                isSetterNonNullable = false;

                switch (memberInfo)
                {
                    case IPropertySymbol propertyInfo:
#if ROSLYN4_4_OR_GREATER
                        isRequired = propertyInfo.IsRequired;
#endif
                        if (propertyInfo.GetMethod is { } getMethod)
                        {
                            if (getMethod.DeclaredAccessibility is Accessibility.Public)
                            {
                                isAccessible = true;
                                canUseGetter = true;
                            }
                            else if (IsSymbolAccessibleWithin(getMethod, within: contextType))
                            {
                                isAccessible = true;
                                canUseGetter = hasJsonInclude;
                            }
                            else
                            {
                                hasJsonIncludeButIsInaccessible = hasJsonInclude;
                            }
                        }

                        if (propertyInfo.SetMethod is { } setMethod)
                        {
                            isSetterInitOnly = setMethod.IsInitOnly;

                            if (setMethod.DeclaredAccessibility is Accessibility.Public)
                            {
                                isAccessible = true;
                                canUseSetter = true;
                            }
                            else if (IsSymbolAccessibleWithin(setMethod, within: contextType))
                            {
                                isAccessible = true;
                                canUseSetter = hasJsonInclude;
                            }
                            else
                            {
                                hasJsonIncludeButIsInaccessible = hasJsonInclude;
                            }
                        }
                        else
                        {
                            isReadOnly = true;
                        }

                        propertyInfo.ResolveNullabilityAnnotations(out isGetterNonNullable, out isSetterNonNullable);
                        break;
                    case IFieldSymbol fieldInfo:
                        isReadOnly = fieldInfo.IsReadOnly;
#if ROSLYN4_4_OR_GREATER
                        isRequired = fieldInfo.IsRequired;
#endif
                        if (fieldInfo.DeclaredAccessibility is Accessibility.Public)
                        {
                            isAccessible = true;
                            canUseGetter = true;
                            canUseSetter = !isReadOnly;
                        }
                        else if (IsSymbolAccessibleWithin(fieldInfo, within: contextType))
                        {
                            isAccessible = true;
                            canUseGetter = hasJsonInclude;
                            canUseSetter = hasJsonInclude && !isReadOnly;
                        }
                        else
                        {
                            hasJsonIncludeButIsInaccessible = hasJsonInclude;
                        }

                        fieldInfo.ResolveNullabilityAnnotations(out isGetterNonNullable, out isSetterNonNullable);
                        break;
                    default:
                        Debug.Fail("Method given an invalid symbol type.");
                        break;
                }
            }

            private ParameterGenerationSpec[]? ParseConstructorParameters(
                in TypeToGenerate typeToGenerate,
                IMethodSymbol? constructor,
                out ObjectConstructionStrategy constructionStrategy,
                out bool constructorSetsRequiredMembers)
            {
                ITypeSymbol type = typeToGenerate.Type;

                if ((constructor is null && !type.IsValueType) || type.IsAbstract)
                {
                    constructionStrategy = ObjectConstructionStrategy.NotApplicable;
                    constructorSetsRequiredMembers = false;
                    return null;
                }

                ParameterGenerationSpec[] constructorParameters;
                int paramCount = constructor?.Parameters.Length ?? 0;
                constructorSetsRequiredMembers = constructor?.ContainsAttribute(_knownSymbols.SetsRequiredMembersAttributeType) == true;

                if (paramCount == 0)
                {
                    constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                    constructorParameters = Array.Empty<ParameterGenerationSpec>();
                }
                else
                {
                    Debug.Assert(constructor != null);

                    constructionStrategy = ObjectConstructionStrategy.ParameterizedConstructor;
                    constructorParameters = new ParameterGenerationSpec[paramCount];

                    for (int i = 0; i < paramCount; i++)
                    {
                        IParameterSymbol parameterInfo = constructor.Parameters[i];
                        TypeRef parameterTypeRef = EnqueueType(parameterInfo.Type, typeToGenerate.Mode);

                        constructorParameters[i] = new ParameterGenerationSpec
                        {
                            ParameterType = parameterTypeRef,
                            Name = parameterInfo.Name,
                            HasDefaultValue = parameterInfo.HasExplicitDefaultValue,
                            DefaultValue = parameterInfo.HasExplicitDefaultValue ? parameterInfo.ExplicitDefaultValue : null,
                            ParameterIndex = i,
                            IsNullable = parameterInfo.IsNullable(),
                        };
                    }
                }

                return constructorParameters;
            }

            private List<PropertyInitializerGenerationSpec>? ParsePropertyInitializers(
                ParameterGenerationSpec[]? constructorParameters,
                List<PropertyGenerationSpec>? properties,
                bool constructorSetsRequiredMembers,
                ref ObjectConstructionStrategy constructionStrategy)
            {
                if (constructionStrategy is ObjectConstructionStrategy.NotApplicable || properties is null)
                {
                    return null;
                }

                HashSet<string>? memberInitializerNames = null;
                List<PropertyInitializerGenerationSpec>? propertyInitializers = null;
                int paramCount = constructorParameters?.Length ?? 0;

                // Determine potential init-only or required properties that need to be part of the constructor delegate signature.
                foreach (PropertyGenerationSpec property in properties)
                {
                    if (!property.CanUseSetter)
                    {
                        continue;
                    }

                    if ((property.IsRequired && !constructorSetsRequiredMembers) || property.IsInitOnlySetter)
                    {
                        if (!(memberInitializerNames ??= new()).Add(property.MemberName))
                        {
                            // We've already added another member initializer with the same name to our spec list.
                            // Duplicates can occur here because the provided list of properties includes shadowed members.
                            // This is because we generate metadata for *all* members, including shadowed or ignored ones,
                            // since we need to re-run the deduplication algorithm taking run-time configuration into account.
                            // This is a simple deduplication that keeps the first result for each member name --
                            // this should be fine since the properties are listed from most derived to least derived order,
                            // so the second instance of a member name is always shadowed by the first.
                            continue;
                        }

                        ParameterGenerationSpec? matchingConstructorParameter = GetMatchingConstructorParameter(property, constructorParameters);

                        if (property.IsRequired || matchingConstructorParameter is null)
                        {
                            constructionStrategy = ObjectConstructionStrategy.ParameterizedConstructor;

                            var propertyInitializer = new PropertyInitializerGenerationSpec
                            {
                                Name = property.MemberName,
                                ParameterType = property.PropertyType,
                                MatchesConstructorParameter = matchingConstructorParameter is not null,
                                ParameterIndex = matchingConstructorParameter?.ParameterIndex ?? paramCount++,
                            };

                            (propertyInitializers ??= new()).Add(propertyInitializer);
                        }

                        static ParameterGenerationSpec? GetMatchingConstructorParameter(PropertyGenerationSpec propSpec, ParameterGenerationSpec[]? paramGenSpecs)
                        {
                            return paramGenSpecs?.FirstOrDefault(MatchesConstructorParameter);

                            bool MatchesConstructorParameter(ParameterGenerationSpec paramSpec)
                                => propSpec.MemberName.Equals(paramSpec.Name, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }

                return propertyInitializers;
            }

            private TypeRef? GetConverterTypeFromJsonConverterAttribute(INamedTypeSymbol contextType, ISymbol declaringSymbol, AttributeData attributeData)
            {
                Debug.Assert(_knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeData.AttributeClass));

                if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, _knownSymbols.JsonConverterAttributeType))
                {
                    ReportDiagnostic(DiagnosticDescriptors.DerivedJsonConverterAttributesNotSupported, attributeData.GetLocation(), attributeData.AttributeClass!.ToDisplayString());
                    return null;
                }

                Debug.Assert(attributeData.ConstructorArguments.Length == 1 && attributeData.ConstructorArguments[0].Value is null or ITypeSymbol);
                var converterType = (ITypeSymbol?)attributeData.ConstructorArguments[0].Value;
                return GetConverterTypeFromAttribute(contextType, converterType, declaringSymbol, attributeData);
            }

            private TypeRef? GetConverterTypeFromAttribute(INamedTypeSymbol contextType, ITypeSymbol? converterType, ISymbol declaringSymbol, AttributeData attributeData)
            {
                if (converterType is not INamedTypeSymbol namedConverterType ||
                    !_knownSymbols.JsonConverterType.IsAssignableFrom(namedConverterType) ||
                    !namedConverterType.Constructors.Any(c => c.Parameters.Length == 0 && IsSymbolAccessibleWithin(c, within: contextType)))
                {
                    ReportDiagnostic(DiagnosticDescriptors.JsonConverterAttributeInvalidType, attributeData.GetLocation(), converterType?.ToDisplayString() ?? "null", declaringSymbol.ToDisplayString());
                    return null;
                }

                if (_knownSymbols.JsonStringEnumConverterType.IsAssignableFrom(converterType))
                {
                    ReportDiagnostic(DiagnosticDescriptors.JsonStringEnumConverterNotSupportedInAot, attributeData.GetLocation(), declaringSymbol.ToDisplayString());
                }

                return new TypeRef(converterType);
            }

            private static string DetermineEffectiveJsonPropertyName(string propertyName, string? jsonPropertyName, SourceGenerationOptionsSpec? options)
            {
                if (jsonPropertyName != null)
                {
                    return jsonPropertyName;
                }

                JsonNamingPolicy? instance = options?.GetEffectivePropertyNamingPolicy() switch
                {
                    JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
                    JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                    JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
                    JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
                    JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
                    _ => null,
                };

                return instance?.ConvertName(propertyName) ?? propertyName;
            }

            private static string? DetermineImmutableCollectionFactoryMethod(string? immutableCollectionFactoryTypeFullName)
            {
                return immutableCollectionFactoryTypeFullName is not null ? $"global::{immutableCollectionFactoryTypeFullName}.CreateRange" : null;
            }

            private static string DeterminePropertyNameFieldName(string effectiveJsonPropertyName)
            {
                const string PropName = "PropName_";

                // Use a different prefix to avoid possible collisions with "PropName_" in
                // the rare case there is a C# property in a hex format.
                const string EncodedPropName = "EncodedPropName_";

                if (SyntaxFacts.IsValidIdentifier(effectiveJsonPropertyName))
                {
                    return PropName + effectiveJsonPropertyName;
                }

                // Encode the string to a byte[] and then convert to hexadecimal.
                // To make the generated code more readable, we could use a different strategy in the future
                // such as including the full class name + the CLR property name when there are duplicates,
                // but that will create unnecessary JsonEncodedText properties.
                byte[] utf8Json = Encoding.UTF8.GetBytes(effectiveJsonPropertyName);

                StringBuilder sb = new StringBuilder(
                    EncodedPropName,
                    capacity: EncodedPropName.Length + utf8Json.Length * 2);

                for (int i = 0; i < utf8Json.Length; i++)
                {
                    sb.Append(utf8Json[i].ToString("X2")); // X2 is hex format
                }

                return sb.ToString();
            }

            private JsonPrimitiveTypeKind? GetPrimitiveTypeKind(ITypeSymbol type)
            {
                if (type.IsNumberType())
                {
                    return JsonPrimitiveTypeKind.Number;
                }

                if (type.SpecialType is SpecialType.System_Boolean)
                {
                    return JsonPrimitiveTypeKind.Boolean;
                }

                if (type.SpecialType is SpecialType.System_Char)
                {
                    return JsonPrimitiveTypeKind.Char;
                }

                SymbolEqualityComparer cmp = SymbolEqualityComparer.Default;

                if (type.SpecialType is SpecialType.System_String or SpecialType.System_DateTime ||
                     cmp.Equals(type, _knownSymbols.DateTimeOffsetType) || cmp.Equals(type, _knownSymbols.GuidType))
                {
                    return JsonPrimitiveTypeKind.String;
                }

                if (cmp.Equals(type, _knownSymbols.ByteArrayType))
                {
                    return JsonPrimitiveTypeKind.ByteArray;
                }

                return null;
            }

            private static string GetTypeInfoPropertyName(ITypeSymbol type)
            {
                if (type is IArrayTypeSymbol arrayType)
                {
                    int rank = arrayType.Rank;
                    string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                    return GetTypeInfoPropertyName(arrayType.ElementType) + suffix;
                }

                if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
                {
                    return type.Name;
                }

                StringBuilder sb = new();

                string name = namedType.Name;

                sb.Append(name);

                if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
                {
                    foreach (ITypeSymbol genericArg in typeArgsInScope)
                    {
                        sb.Append(GetTypeInfoPropertyName(genericArg));
                    }
                }

                return sb.ToString();
            }

            private bool TryGetDeserializationConstructor(
                ITypeSymbol type,
                bool useDefaultCtorInAnnotatedStructs,
                out IMethodSymbol? deserializationCtor)
            {
                IMethodSymbol? ctorWithAttribute = null;
                IMethodSymbol? publicParameterlessCtor = null;
                IMethodSymbol? lonePublicCtor = null;

                if (type is not INamedTypeSymbol namedType)
                {
                    deserializationCtor = null;
                    return false;
                }

                IMethodSymbol[] publicCtors = namedType.GetExplicitlyDeclaredInstanceConstructors().Where(ctor => ctor.DeclaredAccessibility is Accessibility.Public).ToArray();

                if (publicCtors.Length == 1)
                {
                    lonePublicCtor = publicCtors[0];
                }

                foreach (IMethodSymbol constructor in publicCtors)
                {
                    if (constructor.ContainsAttribute(_knownSymbols.JsonConstructorAttributeType))
                    {
                        if (ctorWithAttribute != null)
                        {
                            deserializationCtor = null;
                            return false;
                        }

                        ctorWithAttribute = constructor;
                    }
                    else if (constructor.Parameters.Length == 0)
                    {
                        publicParameterlessCtor = constructor;
                    }
                }

                // Search for non-public ctors with [JsonConstructor].
                foreach (IMethodSymbol constructor in namedType.GetExplicitlyDeclaredInstanceConstructors().Where(ctor => ctor.DeclaredAccessibility is not Accessibility.Public))
                {
                    if (constructor.ContainsAttribute(_knownSymbols.JsonConstructorAttributeType))
                    {
                        if (ctorWithAttribute != null)
                        {
                            deserializationCtor = null;
                            return false;
                        }

                        ctorWithAttribute = constructor;
                    }
                }

                // Structs will use default constructor if attribute isn't used.
                if (useDefaultCtorInAnnotatedStructs && type.IsValueType && ctorWithAttribute == null)
                {
                    deserializationCtor = null;
                    return true;
                }

                deserializationCtor = ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
                return true;
            }

            private bool IsSymbolAccessibleWithin(ISymbol symbol, INamedTypeSymbol within)
                => _knownSymbols.Compilation.IsSymbolAccessibleWithin(symbol, within);

            private bool IsUnsupportedType(ITypeSymbol type)
            {
                return
                    SymbolEqualityComparer.Default.Equals(_knownSymbols.SerializationInfoType, type) ||
                    SymbolEqualityComparer.Default.Equals(_knownSymbols.IntPtrType, type) ||
                    SymbolEqualityComparer.Default.Equals(_knownSymbols.UIntPtrType, type) ||
                    _knownSymbols.MemberInfoType.IsAssignableFrom(type) ||
                    _knownSymbols.DelegateType.IsAssignableFrom(type) ||
                    type is IArrayTypeSymbol { Rank: > 1 };
            }

            private bool IsBuiltInSupportType(ITypeSymbol type)
            {
                return type.SpecialType is
                        SpecialType.System_Boolean or
                        SpecialType.System_Char or
                        SpecialType.System_DateTime or
                        SpecialType.System_String or
                        SpecialType.System_Object ||

                    type.IsNumberType() ||
                    _builtInSupportTypes.Contains(type);
            }

            private static HashSet<ITypeSymbol> CreateBuiltInSupportTypeSet(KnownTypeSymbols knownSymbols)
            {
#pragma warning disable RS1024 // Compare symbols correctly https://github.com/dotnet/roslyn-analyzers/issues/5804
                HashSet<ITypeSymbol> builtInSupportTypes = new(SymbolEqualityComparer.Default);
#pragma warning restore

                AddTypeIfNotNull(knownSymbols.ByteArrayType);
                AddTypeIfNotNull(knownSymbols.MemoryByteType);
                AddTypeIfNotNull(knownSymbols.ReadOnlyMemoryByteType);
                AddTypeIfNotNull(knownSymbols.TimeSpanType);
                AddTypeIfNotNull(knownSymbols.DateTimeOffsetType);
                AddTypeIfNotNull(knownSymbols.DateOnlyType);
                AddTypeIfNotNull(knownSymbols.TimeOnlyType);
                AddTypeIfNotNull(knownSymbols.Int128Type);
                AddTypeIfNotNull(knownSymbols.UInt128Type);
                AddTypeIfNotNull(knownSymbols.HalfType);
                AddTypeIfNotNull(knownSymbols.GuidType);
                AddTypeIfNotNull(knownSymbols.UriType);
                AddTypeIfNotNull(knownSymbols.VersionType);

                AddTypeIfNotNull(knownSymbols.JsonArrayType);
                AddTypeIfNotNull(knownSymbols.JsonElementType);
                AddTypeIfNotNull(knownSymbols.JsonNodeType);
                AddTypeIfNotNull(knownSymbols.JsonObjectType);
                AddTypeIfNotNull(knownSymbols.JsonValueType);
                AddTypeIfNotNull(knownSymbols.JsonDocumentType);

                return builtInSupportTypes;

                void AddTypeIfNotNull(ITypeSymbol? type)
                {
                    if (type != null)
                    {
                        builtInSupportTypes.Add(type);
                    }
                }
            }

            private readonly struct TypeToGenerate
            {
                public required ITypeSymbol Type { get; init; }
                public required JsonSourceGenerationMode? Mode { get; init; }
                public required string? TypeInfoPropertyName { get; init; }
                public required Location? Location { get; init; }
                public required Location? AttributeLocation { get; init; }
            }
        }
    }
}
