// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed class Parser
        {
            private const string SystemTextJsonNamespace = "System.Text.Json";
            private const string JsonConstructorAttributeFullName = "System.Text.Json.Serialization.JsonConstructorAttribute";
            private const string JsonExtensionDataAttributeFullName = "System.Text.Json.Serialization.JsonExtensionDataAttribute";
            private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
            private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";
            private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";
            private const string JsonObjectCreationHandlingAttributeFullName = "System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute";
            private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
            private const string JsonPropertyOrderAttributeFullName = "System.Text.Json.Serialization.JsonPropertyOrderAttribute";
            private const string JsonRequiredAttributeFullName = "System.Text.Json.Serialization.JsonRequiredAttribute";
            private const string SetsRequiredMembersAttributeFullName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";

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

            public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
            {
                Diagnostics.Add(new DiagnosticInfo
                {
                    Descriptor = descriptor,
                    Location = location.GetTrimmedLocation(),
                    MessageArgs = messageArgs ?? Array.Empty<object?>(),
                });
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

                if (!DerivesFromJsonSerializerContext(contextClassDeclaration, _knownSymbols.JsonSerializerContextType, semanticModel, cancellationToken))
                {
                    return null;
                }

                if (!TryParseJsonSerializerContextAttributes(
                    contextClassDeclaration,
                    semanticModel,
                    cancellationToken,
                    out List<TypeToGenerate>? rootSerializableTypes,
                    out JsonSourceGenerationOptionsAttribute? options))
                {
                    // Context does not specify any source gen attributes.
                    return null;
                }

                if (rootSerializableTypes is null)
                {
                    // No types were indicated with [JsonSerializable]
                    return null;
                }

                INamedTypeSymbol? contextTypeSymbol = semanticModel.GetDeclaredSymbol(contextClassDeclaration, cancellationToken);
                Debug.Assert(contextTypeSymbol != null);

                Location contextLocation = contextClassDeclaration.GetLocation();
                if (!TryGetClassDeclarationList(contextTypeSymbol, out List<string>? classDeclarationList))
                {
                    // Class or one of its containing types is not partial so we can't add to it.
                    ReportDiagnostic(DiagnosticDescriptors.ContextClassesMustBePartial, contextLocation, new string[] { contextTypeSymbol.Name });
                    return null;
                }

                options ??= new JsonSourceGenerationOptionsAttribute();

                // Enqueue attribute data for spec generation
                foreach (TypeToGenerate rootSerializableType in rootSerializableTypes)
                {
                    EnqueueType(rootSerializableType.Type, rootSerializableType.Mode, rootSerializableType.TypeInfoPropertyName, rootSerializableType.AttributeLocation);
                }

                // Walk the transitive type graph generating specs for every encountered type.
                while (_typesToGenerate.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TypeToGenerate typeToGenerate = _typesToGenerate.Dequeue();
                    if (!_generatedTypes.ContainsKey(typeToGenerate.Type))
                    {
                        TypeGenerationSpec spec = ParseTypeGenerationSpec(typeToGenerate, contextName: contextTypeSymbol.Name, contextLocation, options);
                        _generatedTypes.Add(typeToGenerate.Type, spec);
                    }
                }

                Debug.Assert(_generatedTypes.Count > 0);

                ContextGenerationSpec contextGenSpec = new()
                {
                    ContextType = new(contextTypeSymbol),
                    GeneratedTypes = _generatedTypes.Values.OrderBy(t => t.TypeRef.FullyQualifiedName).ToImmutableEquatableArray(),
                    Namespace = contextTypeSymbol.ContainingNamespace.ToDisplayString(),
                    ContextClassDeclarations = classDeclarationList.ToImmutableEquatableArray(),
                    DefaultIgnoreCondition = options.DefaultIgnoreCondition,
                    IgnoreReadOnlyFields = options.IgnoreReadOnlyFields,
                    IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties,
                    IncludeFields = options.IncludeFields,
                    PropertyNamingPolicy = options.PropertyNamingPolicy,
                    WriteIndented = options.WriteIndented,
                };

                // Clear the caches of generated metadata between the processing of context classes.
                _generatedTypes.Clear();
                _typesToGenerate.Clear();
                return contextGenSpec;
            }

            // Returns true if a given type derives directly from JsonSerializerContext.
            private static bool DerivesFromJsonSerializerContext(
                ClassDeclarationSyntax classDeclarationSyntax,
                INamedTypeSymbol jsonSerializerContextSymbol,
                SemanticModel compilationSemanticModel,
                CancellationToken cancellationToken)
            {
                SeparatedSyntaxList<BaseTypeSyntax>? baseTypeSyntaxList = classDeclarationSyntax.BaseList?.Types;
                if (baseTypeSyntaxList == null)
                {
                    return false;
                }

                INamedTypeSymbol? match = null;

                foreach (BaseTypeSyntax baseTypeSyntax in baseTypeSyntaxList)
                {
                    INamedTypeSymbol? candidate = compilationSemanticModel.GetSymbolInfo(baseTypeSyntax.Type, cancellationToken).Symbol as INamedTypeSymbol;
                    if (candidate != null && jsonSerializerContextSymbol.Equals(candidate, SymbolEqualityComparer.Default))
                    {
                        match = candidate;
                        break;
                    }
                }

                return match != null;
            }

            private static bool TryGetClassDeclarationList(INamedTypeSymbol typeSymbol, [NotNullWhen(true)] out List<string>? classDeclarationList)
            {
                INamedTypeSymbol currentSymbol = typeSymbol;
                classDeclarationList = null;

                while (currentSymbol != null)
                {
                    ClassDeclarationSyntax? classDeclarationSyntax = currentSymbol.DeclaringSyntaxReferences.First().GetSyntax() as ClassDeclarationSyntax;

                    if (classDeclarationSyntax != null)
                    {
                        SyntaxTokenList tokenList = classDeclarationSyntax.Modifiers;
                        int tokenCount = tokenList.Count;

                        bool isPartial = false;

                        string[] declarationElements = new string[tokenCount + 2];

                        for (int i = 0; i < tokenCount; i++)
                        {
                            SyntaxToken token = tokenList[i];
                            declarationElements[i] = token.Text;

                            if (token.IsKind(SyntaxKind.PartialKeyword))
                            {
                                isPartial = true;
                            }
                        }

                        if (!isPartial)
                        {
                            classDeclarationList = null;
                            return false;
                        }

                        declarationElements[tokenCount] = "class";
                        declarationElements[tokenCount + 1] = GetClassDeclarationName(currentSymbol);

                        (classDeclarationList ??= new List<string>()).Add(string.Join(" ", declarationElements));
                    }

                    currentSymbol = currentSymbol.ContainingType;
                }

                Debug.Assert(classDeclarationList?.Count > 0);
                return true;
            }

            private static string GetClassDeclarationName(INamedTypeSymbol typeSymbol)
            {
                if (typeSymbol.TypeArguments.Length == 0)
                {
                    return typeSymbol.Name;
                }

                StringBuilder sb = new StringBuilder();

                sb.Append(typeSymbol.Name);
                sb.Append('<');

                bool first = true;
                foreach (ITypeSymbol typeArg in typeSymbol.TypeArguments)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        first = false;
                    }

                    sb.Append(typeArg.Name);
                }

                sb.Append('>');

                return sb.ToString();
            }

            private TypeRef EnqueueType(ITypeSymbol type, JsonSourceGenerationMode? generationMode, string? typeInfoPropertyName = null, Location? attributeLocation = null)
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
                    TypeInfoPropertyName = typeInfoPropertyName,
                    AttributeLocation = attributeLocation,
                });

                return new TypeRef(type);
            }

            private static JsonSourceGenerationMode? GetJsonSourceGenerationModeEnumVal(SyntaxNode propertyValueMode)
            {
                IEnumerable<string> enumTokens = propertyValueMode
                    .DescendantTokens()
                    .Where(token => IsValidEnumIdentifier(token.ValueText))
                    .Select(token => token.ValueText);
                string enumAsStr = string.Join(",", enumTokens);

                if (Enum.TryParse(enumAsStr, out JsonSourceGenerationMode value))
                {
                    return value;
                }

                return null;

                static bool IsValidEnumIdentifier(string token) => token != nameof(JsonSourceGenerationMode) && token != "." && token != "|";
            }

            private bool TryParseJsonSerializerContextAttributes(
                ClassDeclarationSyntax classDeclarationSyntax,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                out List<TypeToGenerate>? rootSerializableTypes,
                out JsonSourceGenerationOptionsAttribute? options)
            {
                Debug.Assert(_knownSymbols.JsonSerializableAttributeType != null);
                Debug.Assert(_knownSymbols.JsonSourceGenerationOptionsAttributeType != null);

                bool foundSourceGenAttributes = false;
                rootSerializableTypes = null;
                options = null;

                foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                {
                    AttributeSyntax attributeSyntax = attributeListSyntax.Attributes.First();
                    if (semanticModel.GetSymbolInfo(attributeSyntax, cancellationToken).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;

                    if (_knownSymbols.JsonSerializableAttributeType.Equals(attributeContainingTypeSymbol, SymbolEqualityComparer.Default))
                    {
                        foundSourceGenAttributes = true;
                        TypeToGenerate? typeToGenerate = ParseJsonSerializableAttribute(semanticModel, attributeSyntax, cancellationToken);
                        if (typeToGenerate is null)
                        {
                            continue;
                        }

                        (rootSerializableTypes ??= new()).Add(typeToGenerate.Value);
                    }
                    else if (_knownSymbols.JsonSourceGenerationOptionsAttributeType.Equals(attributeContainingTypeSymbol, SymbolEqualityComparer.Default))
                    {
                        foundSourceGenAttributes = true;
                        options = ParseJsonSourceGenerationOptionsAttribute(attributeSyntax);
                    }
                }

                return foundSourceGenAttributes;
            }

            private static JsonSourceGenerationOptionsAttribute ParseJsonSourceGenerationOptionsAttribute(AttributeSyntax attributeSyntax)
            {
                IEnumerable<SyntaxNode> attributeArguments = attributeSyntax.DescendantNodes().Where(node => node is AttributeArgumentSyntax);

                JsonSourceGenerationOptionsAttribute options = new();

                foreach (AttributeArgumentSyntax node in attributeArguments)
                {
                    IEnumerable<SyntaxNode> childNodes = node.ChildNodes();

                    NameEqualsSyntax? propertyNameNode = childNodes.First() as NameEqualsSyntax;
                    Debug.Assert(propertyNameNode != null);

                    SyntaxNode propertyValueNode = childNodes.ElementAt(1);
                    string propertyValueStr = propertyValueNode.GetLastToken().ValueText;

                    switch (propertyNameNode.Name.Identifier.ValueText)
                    {
                        case nameof(JsonSourceGenerationOptionsAttribute.DefaultIgnoreCondition):
                            {
                                if (Enum.TryParse(propertyValueStr, out JsonIgnoreCondition value))
                                {
                                    options.DefaultIgnoreCondition = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.IgnoreReadOnlyFields):
                            {
                                if (bool.TryParse(propertyValueStr, out bool value))
                                {
                                    options.IgnoreReadOnlyFields = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.IgnoreReadOnlyProperties):
                            {
                                if (bool.TryParse(propertyValueStr, out bool value))
                                {
                                    options.IgnoreReadOnlyProperties = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.IncludeFields):
                            {
                                if (bool.TryParse(propertyValueStr, out bool value))
                                {
                                    options.IncludeFields = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.PropertyNamingPolicy):
                            {
                                if (Enum.TryParse(propertyValueStr, out JsonKnownNamingPolicy value))
                                {
                                    options.PropertyNamingPolicy = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.WriteIndented):
                            {
                                if (bool.TryParse(propertyValueStr, out bool value))
                                {
                                    options.WriteIndented = value;
                                }
                            }
                            break;
                        case nameof(JsonSourceGenerationOptionsAttribute.GenerationMode):
                            {
                                JsonSourceGenerationMode? mode = GetJsonSourceGenerationModeEnumVal(propertyValueNode);
                                if (mode.HasValue)
                                {
                                    options.GenerationMode = mode.Value;
                                }
                            }
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                return options;
            }

            private static TypeToGenerate? ParseJsonSerializableAttribute(SemanticModel semanticModel, AttributeSyntax attributeSyntax, CancellationToken cancellationToken)
            {
                IEnumerable<SyntaxNode> attributeArguments = attributeSyntax.DescendantNodes().Where(node => node is AttributeArgumentSyntax);

                ITypeSymbol? typeSymbol = null;
                string? typeInfoPropertyName = null;
                JsonSourceGenerationMode? generationMode = null;

                bool seenFirstArg = false;
                foreach (AttributeArgumentSyntax node in attributeArguments)
                {
                    if (!seenFirstArg)
                    {
                        TypeOfExpressionSyntax? typeNode = node.ChildNodes().Single() as TypeOfExpressionSyntax;
                        if (typeNode != null)
                        {
                            ExpressionSyntax typeNameSyntax = (ExpressionSyntax)typeNode.ChildNodes().Single();
                            typeSymbol = semanticModel.GetTypeInfo(typeNameSyntax, cancellationToken).ConvertedType;
                        }

                        seenFirstArg = true;
                    }
                    else
                    {
                        IEnumerable<SyntaxNode> childNodes = node.ChildNodes();

                        NameEqualsSyntax? propertyNameNode = childNodes.First() as NameEqualsSyntax;
                        Debug.Assert(propertyNameNode != null);

                        SyntaxNode propertyValueNode = childNodes.ElementAt(1);
                        string optionName = propertyNameNode.Name.Identifier.ValueText;

                        if (optionName == nameof(JsonSerializableAttribute.TypeInfoPropertyName))
                        {
                            typeInfoPropertyName = propertyValueNode.GetFirstToken().ValueText;
                        }
                        else if (optionName == nameof(JsonSerializableAttribute.GenerationMode))
                        {
                            JsonSourceGenerationMode? mode = GetJsonSourceGenerationModeEnumVal(propertyValueNode);
                            if (mode.HasValue)
                            {
                                generationMode = mode.Value;
                            }
                        }
                    }
                }

                if (typeSymbol is null)
                {
                    return null;
                }

                return new TypeToGenerate
                {
                    Type = typeSymbol,
                    Mode = generationMode,
                    TypeInfoPropertyName = typeInfoPropertyName,
                    AttributeLocation = attributeSyntax.GetLocation(),
                };
            }

            private TypeGenerationSpec ParseTypeGenerationSpec(TypeToGenerate typeToGenerate, string contextName, Location contextLocation, JsonSourceGenerationOptionsAttribute options)
            {
                ITypeSymbol type = typeToGenerate.Type;
                Location typeLocation = type.GetDiagnosticLocation() ?? typeToGenerate.AttributeLocation ?? contextLocation;

                ClassType classType;
                JsonPrimitiveTypeKind? primitiveTypeKind = GetPrimitiveTypeKind(type);
                TypeRef? collectionKeyType = null;
                TypeRef? collectionValueType = null;
                TypeRef? nullableUnderlyingType = null;
                TypeRef? extensionDataPropertyType = null;
                TypeRef? runtimeTypeRef = null;
                List<PropertyGenerationSpec>? propGenSpecList = null;
                ObjectConstructionStrategy constructionStrategy = default;
                bool constructorSetsRequiredMembers = false;
                ParameterGenerationSpec[]? paramGenSpecs = null;
                List<PropertyInitializerGenerationSpec>? propertyInitializers = null;
                CollectionType collectionType = CollectionType.NotApplicable;
                JsonNumberHandling? numberHandling = null;
                JsonUnmappedMemberHandling? unmappedMemberHandling = null;
                JsonObjectCreationHandling? preferredPropertyObjectCreationHandling = null;
                string? immutableCollectionFactoryTypeFullName = null;
                bool foundDesignTimeCustomConverter = false;
                TypeRef? converterType = null;
                bool implementsIJsonOnSerialized = false;
                bool implementsIJsonOnSerializing = false;
                bool isPolymorphic = false;

                IList<AttributeData> attributeDataList = type.GetAttributes();
                foreach (AttributeData attributeData in attributeDataList)
                {
                    INamedTypeSymbol? attributeType = attributeData.AttributeClass;

                    if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonNumberHandlingAttributeType))
                    {
                        IList<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                        numberHandling = (JsonNumberHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonUnmappedMemberHandlingAttributeType))
                    {
                        IList<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                        unmappedMemberHandling = (JsonUnmappedMemberHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonObjectCreationHandlingAttributeType))
                    {
                        IList<TypedConstant> ctorArgs = attributeData.ConstructorArguments;
                        preferredPropertyObjectCreationHandling = (JsonObjectCreationHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (!foundDesignTimeCustomConverter && _knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeType))
                    {
                        converterType = GetConverterTypeFromAttribute(attributeData);
                        foundDesignTimeCustomConverter = true;
                    }

                    if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonDerivedTypeAttributeType))
                    {
                        Debug.Assert(attributeData.ConstructorArguments.Length > 0);
                        var derivedType = (ITypeSymbol)attributeData.ConstructorArguments[0].Value!;
                        EnqueueType(derivedType, typeToGenerate.Mode);

                        if (!isPolymorphic && typeToGenerate.Mode == JsonSourceGenerationMode.Serialization)
                        {
                            ReportDiagnostic(DiagnosticDescriptors.PolymorphismNotSupported, typeLocation, new string[] { type.ToDisplayString() });
                        }

                        isPolymorphic = true;
                    }
                }

                if (foundDesignTimeCustomConverter)
                {
                    classType = converterType != null
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
                    classType = ClassType.Enum;
                }
                else if (type.GetCompatibleGenericBaseType(_knownSymbols.IAsyncEnumerableOfTType) is INamedTypeSymbol iasyncEnumerableType)
                {
                    if (type.CanUseDefaultConstructorForDeserialization(out IMethodSymbol? defaultCtor))
                    {
                        constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                        constructorSetsRequiredMembers = defaultCtor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                    }

                    ITypeSymbol elementType = iasyncEnumerableType.TypeArguments[0];
                    collectionValueType = EnqueueType(elementType, typeToGenerate.Mode);
                    collectionType = CollectionType.IAsyncEnumerableOfT;
                    classType = ClassType.Enumerable;
                }
                else if (_knownSymbols.IEnumerableType.IsAssignableFrom(type))
                {
                    if (type.CanUseDefaultConstructorForDeserialization(out IMethodSymbol? defaultCtor))
                    {
                        constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                        constructorSetsRequiredMembers = defaultCtor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                    }

                    INamedTypeSymbol? actualTypeToConvert;
                    ITypeSymbol? keyType = null;
                    ITypeSymbol valueType;
                    bool needsRuntimeType = false;

                    if (type is IArrayTypeSymbol arraySymbol)
                    {
                        Debug.Assert(arraySymbol.Rank == 1, "multi-dimensional arrays should have been handled earlier.");
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Array;
                        valueType = arraySymbol.ElementType;
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ListOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.List;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.DictionaryOfTKeyTValueType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.Dictionary;

                        keyType = actualTypeToConvert.TypeArguments[0];
                        valueType = actualTypeToConvert.TypeArguments[1];
                    }
                    else if (_knownSymbols.IsImmutableDictionaryType(type, out immutableCollectionFactoryTypeFullName))
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.ImmutableDictionary;

                        ImmutableArray<ITypeSymbol> genericArgs = ((INamedTypeSymbol)type).TypeArguments;
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IDictionaryOfTKeyTValueType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionaryOfTKeyTValue;

                        keyType = actualTypeToConvert.TypeArguments[0];
                        valueType = actualTypeToConvert.TypeArguments[1];

                        needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IReadonlyDictionaryOfTKeyTValueType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IReadOnlyDictionary;

                        keyType = actualTypeToConvert.TypeArguments[0];
                        valueType = actualTypeToConvert.TypeArguments[1];

                        needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                    }
                    else if (_knownSymbols.IsImmutableEnumerableType(type, out immutableCollectionFactoryTypeFullName))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ImmutableEnumerable;
                        valueType = ((INamedTypeSymbol)type).TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IListOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IListOfT;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ISetOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ISet;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ICollectionOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ICollectionOfT;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.StackOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.StackOfT;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.QueueOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.QueueOfT;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ConcurrentStackType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentStack;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.ConcurrentQueueType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentQueue;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseType(_knownSymbols.IEnumerableOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerableOfT;
                        valueType = actualTypeToConvert.TypeArguments[0];
                    }
                    else if (_knownSymbols.IDictionaryType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionary;
                        keyType = _knownSymbols.StringType;
                        valueType = _knownSymbols.ObjectType;

                        needsRuntimeType = SymbolEqualityComparer.Default.Equals(type, actualTypeToConvert);
                    }
                    else if (_knownSymbols.IListType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IList;
                        valueType = _knownSymbols.ObjectType;
                    }
                    else if (_knownSymbols.StackType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Stack;
                        valueType = _knownSymbols.ObjectType;
                    }
                    else if (_knownSymbols.QueueType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Queue;
                        valueType = _knownSymbols.ObjectType;
                    }
                    else
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerable;
                        valueType = _knownSymbols.ObjectType;
                    }

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
                else
                {
                    bool useDefaultCtorInAnnotatedStructs = type.GetCompatibleGenericBaseType(_knownSymbols.KeyValuePair) is null;

                    if (!TryGetDeserializationConstructor(type, useDefaultCtorInAnnotatedStructs, out IMethodSymbol? constructor))
                    {
                        classType = ClassType.TypeUnsupportedBySourceGen;
                        ReportDiagnostic(DiagnosticDescriptors.MultipleJsonConstructorAttribute, typeLocation, new string[] { type.ToDisplayString() });
                    }
                    else
                    {
                        classType = ClassType.Object;

                        if ((constructor != null || type.IsValueType) && !type.IsAbstract)
                        {
                            constructorSetsRequiredMembers = constructor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                            ImmutableArray<IParameterSymbol> parameters = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
                            int paramCount = parameters.Length;

                            if (paramCount == 0)
                            {
                                constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                            }
                            else
                            {
                                constructionStrategy = ObjectConstructionStrategy.ParameterizedConstructor;
                                paramGenSpecs = new ParameterGenerationSpec[paramCount];

                                for (int i = 0; i < paramCount; i++)
                                {
                                    IParameterSymbol parameterInfo = parameters![i];
                                    TypeRef parameterTypeRef = EnqueueType(parameterInfo.Type, typeToGenerate.Mode);

                                    paramGenSpecs[i] = new ParameterGenerationSpec
                                    {
                                        ParameterType = parameterTypeRef,
                                        Name = parameterInfo.Name!,
                                        HasDefaultValue = parameterInfo.HasExplicitDefaultValue,
                                        DefaultValue = parameterInfo.HasExplicitDefaultValue ? parameterInfo.ExplicitDefaultValue : null,
                                        ParameterIndex = i
                                    };
                                }
                            }
                        }

                        IEnumerable<string> interfaces = type.AllInterfaces.Select(interfaceType => interfaceType.ToDisplayString());
                        implementsIJsonOnSerialized = interfaces.FirstOrDefault(interfaceName => interfaceName == JsonConstants.IJsonOnSerializedFullName) != null;
                        implementsIJsonOnSerializing = interfaces.FirstOrDefault(interfaceName => interfaceName == JsonConstants.IJsonOnSerializingFullName) != null;

                        propGenSpecList = new List<PropertyGenerationSpec>();
                        Dictionary<string, ISymbol>? ignoredMembers = null;

                        bool propertyOrderSpecified = false;
                        paramGenSpecs ??= Array.Empty<ParameterGenerationSpec>();
                        int nextParameterIndex = paramGenSpecs.Length;

                        // Walk the type hierarchy starting from the current type up to the base type(s)
                        foreach (INamedTypeSymbol currentType in type.GetSortedTypeHierarchy())
                        {
                            var declaringTypeRef = new TypeRef(currentType);

                            foreach (IPropertySymbol propertyInfo in currentType.GetMembers().OfType<IPropertySymbol>())
                            {
                                bool isVirtual = propertyInfo.IsVirtual();

                                // Skip if:
                                if (
                                    // property is static or an indexer
                                    propertyInfo.IsStatic || propertyInfo.Parameters.Length > 0 ||
                                    // It is overridden by a derived member
                                    PropertyIsOverriddenAndIgnored(propertyInfo, propertyInfo.Type, isVirtual, ignoredMembers))
                                {
                                    continue;
                                }

                                PropertyGenerationSpec? spec = ParsePropertyGenerationSpec(declaringTypeRef, propertyInfo.Type, propertyInfo, isVirtual, typeToGenerate.Mode, options);
                                if (spec is null)
                                {
                                    continue;
                                }

                                CacheMemberHelper(propertyInfo.Type, propertyInfo, spec);
                            }

                            foreach (IFieldSymbol fieldInfo in currentType.GetMembers().OfType<IFieldSymbol>())
                            {
                                // Skip if :
                                if (
                                    // it is a static field, constant
                                    fieldInfo.IsStatic || fieldInfo.IsConst ||
                                    // it is a compiler-generated backing field
                                    fieldInfo.AssociatedSymbol != null ||
                                    // symbol represents an explicitly named tuple element
                                    fieldInfo.IsExplicitlyNamedTupleElement ||
                                    // It is overridden by a derived member
                                    PropertyIsOverriddenAndIgnored(fieldInfo, fieldInfo.Type, currentMemberIsVirtual: false, ignoredMembers))
                                {
                                    continue;
                                }

                                PropertyGenerationSpec? spec = ParsePropertyGenerationSpec(declaringTypeRef, fieldInfo.Type, fieldInfo, isVirtual: false, typeToGenerate.Mode, options);
                                if (spec is null)
                                {
                                    continue;
                                }

                                CacheMemberHelper(fieldInfo.Type, fieldInfo, spec);
                            }

                            void CacheMemberHelper(ITypeSymbol memberType, ISymbol memberInfo, PropertyGenerationSpec spec)
                            {
                                CacheMember(memberInfo, spec, ref propGenSpecList, ref ignoredMembers);

                                propertyOrderSpecified |= spec.Order != 0;

                                if (spec.IsExtensionData)
                                {
                                    if (extensionDataPropertyType != null)
                                    {
                                        ReportDiagnostic(DiagnosticDescriptors.MultipleJsonExtensionDataAttribute, typeLocation, new string[] { type.Name });
                                    }

                                    if (!IsValidDataExtensionPropertyType(memberType))
                                    {
                                        ReportDiagnostic(DiagnosticDescriptors.DataExtensionPropertyInvalid, memberInfo.GetDiagnosticLocation(), new string[] { type.Name, spec.MemberName });
                                    }

                                    extensionDataPropertyType = spec.PropertyType;
                                }

                                if (constructionStrategy is not ObjectConstructionStrategy.NotApplicable && spec.CanUseSetter &&
                                    ((spec.IsRequired && !constructorSetsRequiredMembers) || spec.IsInitOnlySetter))
                                {
                                    ParameterGenerationSpec? matchingConstructorParameter = GetMatchingConstructorParameter(spec, paramGenSpecs);

                                    if (spec.IsRequired || matchingConstructorParameter is null)
                                    {
                                        constructionStrategy = ObjectConstructionStrategy.ParameterizedConstructor;

                                        var propInitializerSpec = new PropertyInitializerGenerationSpec
                                        {
                                            Name = spec.MemberName,
                                            ParameterType = spec.PropertyType,
                                            MatchesConstructorParameter = matchingConstructorParameter is not null,
                                            ParameterIndex = matchingConstructorParameter?.ParameterIndex ?? nextParameterIndex++,
                                        };

                                        (propertyInitializers ??= new()).Add(propInitializerSpec);
                                    }
                                }

                                if (spec.HasJsonInclude && (!spec.CanUseGetter || !spec.CanUseSetter || !spec.IsPublic))
                                {
                                    ReportDiagnostic(DiagnosticDescriptors.InaccessibleJsonIncludePropertiesNotSupported, memberInfo.GetDiagnosticLocation(), new string[] { type.Name, spec.MemberName });
                                }
                            }
                        }

                        if (propertyOrderSpecified)
                        {
                            propGenSpecList.StableSortByKey(p => p.Order);
                        }
                    }
                }

                var typeRef = new TypeRef(type);
                string typeInfoPropertyName = typeToGenerate.TypeInfoPropertyName ?? GetTypeInfoPropertyName(type);

                if (classType is ClassType.TypeUnsupportedBySourceGen)
                {
                    ReportDiagnostic(DiagnosticDescriptors.TypeNotSupported, typeLocation, new string[] { typeRef.FullyQualifiedName });
                }

                if (!_generatedContextAndTypeNames.Add((contextName, typeInfoPropertyName)))
                {
                    // The context name/property name combination will result in a conflict in generated types.
                    // Workaround for https://github.com/dotnet/roslyn/issues/54185 by keeping track of the file names we've used.
                    ReportDiagnostic(DiagnosticDescriptors.DuplicateTypeName, typeToGenerate.AttributeLocation ?? contextLocation, new string[] { typeInfoPropertyName });
                    classType = ClassType.TypeUnsupportedBySourceGen;
                }

                return new TypeGenerationSpec
                {
                    TypeRef = typeRef,
                    TypeInfoPropertyName = typeInfoPropertyName,
                    GenerationMode = typeToGenerate.Mode ?? options.GenerationMode,
                    ClassType = classType,
                    PrimitiveTypeKind = primitiveTypeKind,
                    IsPolymorphic = isPolymorphic,
                    NumberHandling = numberHandling,
                    UnmappedMemberHandling = unmappedMemberHandling,
                    PreferredPropertyObjectCreationHandling = preferredPropertyObjectCreationHandling,
                    PropertyGenSpecs = propGenSpecList?.ToImmutableEquatableArray(),
                    PropertyInitializerSpecs = propertyInitializers?.ToImmutableEquatableArray(),
                    CtorParamGenSpecs = paramGenSpecs?.ToImmutableEquatableArray(),
                    CollectionType = collectionType,
                    CollectionKeyType = collectionKeyType,
                    CollectionValueType = collectionValueType,
                    ConstructionStrategy = constructionStrategy,
                    ConstructorSetsRequiredParameters = constructorSetsRequiredMembers,
                    NullableUnderlyingType = nullableUnderlyingType,
                    RuntimeTypeRef = runtimeTypeRef,
                    IsValueTuple = type.IsTupleType,
                    ExtensionDataPropertyType = extensionDataPropertyType,
                    ConverterType = converterType,
                    ImplementsIJsonOnSerialized = implementsIJsonOnSerialized,
                    ImplementsIJsonOnSerializing = implementsIJsonOnSerializing,
                    ImmutableCollectionFactoryMethod = DetermineImmutableCollectionFactoryMethod(immutableCollectionFactoryTypeFullName),
                };
            }

            private TypeRef? GetDictionaryTypeRef(ITypeSymbol keyType, ITypeSymbol valueType)
            {
                INamedTypeSymbol? dictionary = _knownSymbols.DictionaryOfTKeyTValueType?.Construct(keyType, valueType);
                return dictionary is null ? null : new TypeRef(dictionary);
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

            private static void CacheMember(
                ISymbol memberInfo,
                PropertyGenerationSpec propGenSpec,
                ref List<PropertyGenerationSpec> propGenSpecList,
                ref Dictionary<string, ISymbol>? ignoredMembers)
            {
                propGenSpecList.Add(propGenSpec);

                if (propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                {
                    ignoredMembers ??= new();
                    ignoredMembers.Add(propGenSpec.MemberName, memberInfo);
                }
            }

            private static ParameterGenerationSpec? GetMatchingConstructorParameter(PropertyGenerationSpec propSpec, ParameterGenerationSpec[]? paramGenSpecs)
            {
                return paramGenSpecs?.FirstOrDefault(MatchesConstructorParameter);

                bool MatchesConstructorParameter(ParameterGenerationSpec paramSpec)
                    => propSpec.MemberName.Equals(paramSpec.Name, StringComparison.OrdinalIgnoreCase);
            }

            private static bool PropertyIsOverriddenAndIgnored(
                ISymbol memberInfo,
                ITypeSymbol currentMemberType,
                bool currentMemberIsVirtual,
                Dictionary<string, ISymbol>? ignoredMembers)
            {
                if (ignoredMembers == null || !ignoredMembers.TryGetValue(memberInfo.Name, out ISymbol? ignoredMember))
                {
                    return false;
                }

                return SymbolEqualityComparer.Default.Equals(currentMemberType, ignoredMember.GetMemberType()) &&
                    currentMemberIsVirtual &&
                    ignoredMember.IsVirtual();
            }

            private PropertyGenerationSpec? ParsePropertyGenerationSpec(
                TypeRef declaringType,
                ITypeSymbol memberType,
                ISymbol memberInfo,
                bool isVirtual,
                JsonSourceGenerationMode? generationMode,
                JsonSourceGenerationOptionsAttribute options)
            {
                Debug.Assert(memberInfo is IFieldSymbol or IPropertySymbol);

                ProcessMemberCustomAttributes(
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
                    memberInfo,
                    hasJsonInclude,
                    out bool isReadOnly,
                    out bool isPublic,
                    out bool isRequired,
                    out bool canUseGetter,
                    out bool canUseSetter,
                    out bool setterIsInitOnly);

                if (!isPublic && !memberType.IsPublic())
                {
                    return null;
                }

                bool needsAtSign = memberInfo.MemberNameNeedsAtSign();

                string clrName = memberInfo.Name;
                string runtimePropertyName = DetermineRuntimePropName(clrName, jsonPropertyName, options.PropertyNamingPolicy);
                string propertyNameVarName = DeterminePropNameIdentifier(runtimePropertyName);

                return new PropertyGenerationSpec
                {
                    NameSpecifiedInSourceCode = needsAtSign ? "@" + memberInfo.Name : memberInfo.Name,
                    MemberName = memberInfo.Name,
                    IsProperty = memberInfo is IPropertySymbol,
                    IsPublic = isPublic,
                    IsVirtual = isVirtual,
                    JsonPropertyName = jsonPropertyName,
                    RuntimePropertyName = runtimePropertyName,
                    PropertyNameVarName = propertyNameVarName,
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
                    PropertyType = EnqueueType(memberType, generationMode),
                    DeclaringType = declaringType,
                    ConverterType = converterType,
                };
            }

            private void ProcessMemberCustomAttributes(
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
                        converterType = GetConverterTypeFromAttribute(attributeData);
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

            private static void ProcessMember(
                ISymbol memberInfo,
                bool hasJsonInclude,
                out bool isReadOnly,
                out bool isPublic,
                out bool isRequired,
                out bool canUseGetter,
                out bool canUseSetter,
                out bool setterIsInitOnly)
            {
                isPublic = false;
                isRequired = false;
                canUseGetter = false;
                canUseSetter = false;
                setterIsInitOnly = false;

                switch (memberInfo)
                {
                    case IPropertySymbol propertyInfo:
                        {
                            IMethodSymbol? getMethod = propertyInfo.GetMethod;
                            IMethodSymbol? setMethod = propertyInfo.SetMethod;
#if ROSLYN4_4_OR_GREATER
                            isRequired = propertyInfo.IsRequired;
#endif

                            if (getMethod != null)
                            {
                                if (getMethod.DeclaredAccessibility is Accessibility.Public)
                                {
                                    isPublic = true;
                                    canUseGetter = true;
                                }
                                else if (getMethod.DeclaredAccessibility is Accessibility.Internal)
                                {
                                    canUseGetter = hasJsonInclude;
                                }
                            }

                            if (setMethod != null)
                            {
                                isReadOnly = false;
                                setterIsInitOnly = setMethod.IsInitOnly;

                                if (setMethod.DeclaredAccessibility is Accessibility.Public)
                                {
                                    isPublic = true;
                                    canUseSetter = true;
                                }
                                else if (setMethod.DeclaredAccessibility is Accessibility.Internal)
                                {
                                    canUseSetter = hasJsonInclude;
                                }
                            }
                            else
                            {
                                isReadOnly = true;
                            }
                        }
                        break;
                    case IFieldSymbol fieldInfo:
                        {
                            isPublic = fieldInfo.DeclaredAccessibility is Accessibility.Public;
                            isReadOnly = fieldInfo.IsReadOnly;
#if ROSLYN4_4_OR_GREATER
                            isRequired = fieldInfo.IsRequired;
#endif
                            if (fieldInfo.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Protected))
                            {
                                canUseGetter = true;
                                canUseSetter = !isReadOnly;
                            }
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            private static bool PropertyAccessorCanBeReferenced(MethodInfo? accessor)
                => accessor != null && (accessor.IsPublic || accessor.IsAssembly);

            private TypeRef? GetConverterTypeFromAttribute(AttributeData attributeData)
            {
                Debug.Assert(_knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeData.AttributeClass));
                var converterType = (INamedTypeSymbol?)attributeData.ConstructorArguments[0].Value;

                if (converterType == null ||
                    !_knownSymbols.JsonConverterType.IsAssignableFrom(converterType) ||
                    !converterType.Constructors.Any(c => c.Parameters.Length == 0) ||
                    converterType.IsNestedPrivate())
                {
                    return null;
                }

                return new TypeRef(converterType);
            }

            private static string DetermineRuntimePropName(string clrPropName, string? jsonPropName, JsonKnownNamingPolicy namingPolicy)
            {
                string runtimePropName;

                if (jsonPropName != null)
                {
                    runtimePropName = jsonPropName;
                }
                else
                {
                    JsonNamingPolicy? instance = namingPolicy switch
                    {
                        JsonKnownNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
                        JsonKnownNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                        JsonKnownNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
                        JsonKnownNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
                        JsonKnownNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
                        _ => null,
                    };

                    runtimePropName = instance?.ConvertName(clrPropName) ?? clrPropName;
                }

                return runtimePropName;
            }

            private static string? DetermineImmutableCollectionFactoryMethod(string? immutableCollectionFactoryTypeFullName)
            {
                return immutableCollectionFactoryTypeFullName is not null ? $"global::{immutableCollectionFactoryTypeFullName}.CreateRange" : null;
            }

            private static string DeterminePropNameIdentifier(string runtimePropName)
            {
                const string PropName = "PropName_";

                // Use a different prefix to avoid possible collisions with "PropName_" in
                // the rare case there is a C# property in a hex format.
                const string EncodedPropName = "EncodedPropName_";

                if (SyntaxFacts.IsValidIdentifier(runtimePropName))
                {
                    return PropName + runtimePropName;
                }

                // Encode the string to a byte[] and then convert to hexadecimal.
                // To make the generated code more readable, we could use a different strategy in the future
                // such as including the full class name + the CLR property name when there are duplicates,
                // but that will create unnecessary JsonEncodedText properties.
                byte[] utf8Json = Encoding.UTF8.GetBytes(runtimePropName);

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

                foreach (ITypeSymbol genericArg in namedType.GetAllTypeArgumentsInScope())
                {
                    sb.Append(GetTypeInfoPropertyName(genericArg));
                }

                return sb.ToString();
            }

            private static bool TryGetDeserializationConstructor(
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
                    if (constructor.ContainsAttribute(JsonConstructorAttributeFullName))
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

                // For correctness, throw if multiple ctors have [JsonConstructor], even if one or more are non-public.
                IMethodSymbol? dummyCtorWithAttribute = ctorWithAttribute;

                foreach (IMethodSymbol constructor in namedType.GetExplicitlyDeclaredInstanceConstructors().Where(ctor => ctor.DeclaredAccessibility is not Accessibility.Public))
                {
                    if (constructor.ContainsAttribute(JsonConstructorAttributeFullName))
                    {
                        if (dummyCtorWithAttribute != null)
                        {
                            deserializationCtor = null;
                            return false;
                        }

                        dummyCtorWithAttribute = constructor;
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
                AddTypeIfNotNull(knownSymbols.TimeSpanType);
                AddTypeIfNotNull(knownSymbols.DateTimeOffsetType);
                AddTypeIfNotNull(knownSymbols.DateOnlyType);
                AddTypeIfNotNull(knownSymbols.TimeOnlyType);
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
                public JsonSourceGenerationMode? Mode { get; init; }
                public string? TypeInfoPropertyName { get; init; }
                public Location? AttributeLocation { get; init; }
            }
        }
    }
}
