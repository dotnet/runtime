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

            private const string DictionaryTypeRef = "global::System.Collections.Generic.Dictionary";

            private readonly Compilation _compilation;
            private readonly KnownTypeSymbols _knownSymbols;

            // Keeps track of generated context type names
            private readonly HashSet<(string ContextName, string TypeName)> _generatedContextAndTypeNames = new();

#pragma warning disable RS1024 // Compare symbols correctly https://github.com/dotnet/roslyn-analyzers/issues/5804
            private readonly HashSet<ITypeSymbol> _builtInSupportTypes = new(SymbolEqualityComparer.Default);
#pragma warning restore

            private readonly Queue<(ITypeSymbol type, JsonSourceGenerationMode mode, string? typeInfoPropertyName, Location? attributeLocation)> _typesToGenerate = new();
#pragma warning disable RS1024 // Compare symbols correctly https://github.com/dotnet/roslyn-analyzers/issues/5804
            private readonly Dictionary<ITypeSymbol, TypeGenerationSpec> _generatedTypes = new(SymbolEqualityComparer.Default);
#pragma warning restore
            private JsonKnownNamingPolicy _currentContextNamingPolicy;

            private readonly List<Diagnostic> _diagnostics = new();

            public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
            {
                location = location.GetTrimmedLocation();
                Diagnostic diag = Diagnostic.Create(descriptor, location, messageArgs);
                _diagnostics.Add(diag);
            }

            private static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1030",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor DuplicateTypeName { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1031",
                title: new LocalizableResourceString(nameof(SR.DuplicateTypeNameTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DuplicateTypeNameMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor ContextClassesMustBePartial { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1032",
                title: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor MultipleJsonConstructorAttribute { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1033",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonConstructorAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor MultipleJsonExtensionDataAttribute { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1035",
                title: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.MultipleJsonExtensionDataAttributeFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor DataExtensionPropertyInvalid { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1036",
                title: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DataExtensionPropertyInvalidFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor InaccessibleJsonIncludePropertiesNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1038",
                title: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.InaccessibleJsonIncludePropertiesNotSupportedFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor PolymorphismNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1039",
                title: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.FastPathPolymorphismNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: JsonConstants.SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public Parser(Compilation compilation)
            {
                _compilation = compilation;
                _knownSymbols = new KnownTypeSymbols(compilation);

                PopulateBuiltInSupportTypes();
            }

            public SourceGenerationSpec? GetGenerationSpec(IEnumerable<ClassDeclarationSyntax> classDeclarationSyntaxList, CancellationToken cancellationToken)
            {
                Compilation compilation = _compilation;
                INamedTypeSymbol? jsonSerializerContextSymbol = _knownSymbols.JsonSerializerContextType;
                INamedTypeSymbol? jsonSerializableAttributeSymbol = _knownSymbols.JsonSerializableAttributeType;
                INamedTypeSymbol? jsonSourceGenerationOptionsAttributeSymbol = _knownSymbols.JsonSourceGenerationOptionsAttributeType;
                INamedTypeSymbol? jsonConverterOfTSymbol = _knownSymbols.JsonConverterOfTType;

                if (jsonSerializerContextSymbol == null ||
                    jsonSerializableAttributeSymbol == null ||
                    jsonSourceGenerationOptionsAttributeSymbol == null ||
                    jsonConverterOfTSymbol == null)
                {
                    return null;
                }

                List<ContextGenerationSpec>? contextGenSpecList = null;

                foreach (IGrouping<SyntaxTree, ClassDeclarationSyntax> group in classDeclarationSyntaxList.GroupBy(c => c.SyntaxTree))
                {
                    SyntaxTree syntaxTree = group.Key;
                    SemanticModel compilationSemanticModel = compilation.GetSemanticModel(syntaxTree);
                    CompilationUnitSyntax compilationUnitSyntax = (CompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken);

                    foreach (ClassDeclarationSyntax classDeclarationSyntax in group)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Ensure context-scoped metadata caches are empty.
                        Debug.Assert(_typesToGenerate.Count == 0);
                        Debug.Assert(_generatedTypes.Count == 0);

                        if (!DerivesFromJsonSerializerContext(classDeclarationSyntax, jsonSerializerContextSymbol, compilationSemanticModel, cancellationToken))
                        {
                            continue;
                        }

                        JsonSourceGenerationOptionsAttribute? options = null;
                        List<AttributeSyntax>? serializableAttributeList = null;

                        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                        {
                            AttributeSyntax attributeSyntax = attributeListSyntax.Attributes.First();
                            if (compilationSemanticModel.GetSymbolInfo(attributeSyntax, cancellationToken).Symbol is not IMethodSymbol attributeSymbol)
                            {
                                continue;
                            }

                            INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;

                            if (jsonSerializableAttributeSymbol.Equals(attributeContainingTypeSymbol, SymbolEqualityComparer.Default))
                            {
                                (serializableAttributeList ??= new List<AttributeSyntax>()).Add(attributeSyntax);
                            }
                            else if (jsonSourceGenerationOptionsAttributeSymbol.Equals(attributeContainingTypeSymbol, SymbolEqualityComparer.Default))
                            {
                                options = GetSerializerOptions(attributeSyntax);
                            }
                        }

                        if (serializableAttributeList == null)
                        {
                            // No types were indicated with [JsonSerializable]
                            continue;
                        }

                        INamedTypeSymbol? contextTypeSymbol = compilationSemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken);
                        Debug.Assert(contextTypeSymbol != null);

                        Location contextLocation = contextTypeSymbol.Locations.Length > 0 ? contextTypeSymbol.Locations[0] : Location.None;

                        if (!TryGetClassDeclarationList(contextTypeSymbol, out List<string>? classDeclarationList))
                        {
                            // Class or one of its containing types is not partial so we can't add to it.
                            ReportDiagnostic(ContextClassesMustBePartial, contextLocation, new string[] { contextTypeSymbol.Name });
                            continue;
                        }

                        options ??= new JsonSourceGenerationOptionsAttribute();

                        // Set the naming policy for the current context.
                        _currentContextNamingPolicy = options.PropertyNamingPolicy;

                        foreach (AttributeSyntax attribute in serializableAttributeList)
                        {
                            EnqueueRootType(compilationSemanticModel, attribute, options.GenerationMode, cancellationToken);
                        }

                        while (_typesToGenerate.Count > 0)
                        {
                            (ITypeSymbol type, JsonSourceGenerationMode mode, string? typeInfoPropertyName, Location? attributeLocation) = _typesToGenerate.Dequeue();
                            if (!_generatedTypes.ContainsKey(type))
                            {
                                TypeGenerationSpec spec = CreateTypeGenerationSpec(type, mode, typeInfoPropertyName, attributeLocation, contextLocation, contextName: contextTypeSymbol.Name);
                                _generatedTypes.Add(type, spec);
                            }
                        }

                        if (_generatedTypes.Count == 0)
                        {
                            continue;
                        }

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

                        contextGenSpecList ??= new List<ContextGenerationSpec>();
                        contextGenSpecList.Add(contextGenSpec);

                        // Clear the caches of generated metadata between the processing of context classes.
                        _generatedTypes.Clear();
                        _typesToGenerate.Clear();
                    }
                }

                if (contextGenSpecList == null)
                {
                    return null;
                }

                return new SourceGenerationSpec
                {
                    ContextGenerationSpecs = contextGenSpecList.ToImmutableEquatableArray(),
                    Diagnostics = _diagnostics.ToImmutableEquatableArray(),
                };
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

            private void EnqueueRootType(
                SemanticModel compilationSemanticModel,
                AttributeSyntax attributeSyntax,
                JsonSourceGenerationMode generationMode,
                CancellationToken cancellationToken)
            {
                IEnumerable<SyntaxNode> attributeArguments = attributeSyntax.DescendantNodes().Where(node => node is AttributeArgumentSyntax);

                ITypeSymbol? typeSymbol = null;
                string? typeInfoPropertyName = null;

                bool seenFirstArg = false;
                foreach (AttributeArgumentSyntax node in attributeArguments)
                {
                    if (!seenFirstArg)
                    {
                        TypeOfExpressionSyntax? typeNode = node.ChildNodes().Single() as TypeOfExpressionSyntax;
                        if (typeNode != null)
                        {
                            ExpressionSyntax typeNameSyntax = (ExpressionSyntax)typeNode.ChildNodes().Single();
                            typeSymbol = compilationSemanticModel.GetTypeInfo(typeNameSyntax, cancellationToken).ConvertedType;
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

                if (typeSymbol == null)
                {
                    return;
                }

                EnqueueType(typeSymbol, generationMode, typeInfoPropertyName, attributeSyntax.GetLocation());
            }

            private TypeRef EnqueueType(ITypeSymbol type, JsonSourceGenerationMode generationMode, string? typeInfoPropertyName = null, Location? attributeLocation = null)
            {
                // Trim compile-time erased metadata such as tuple labels and NRT annotations.
                type = _compilation.EraseCompileTimeMetadata(type);

                if (_generatedTypes.TryGetValue(type, out TypeGenerationSpec? spec))
                {
                    return spec.TypeRef;
                }

                _typesToGenerate.Enqueue((type, generationMode, typeInfoPropertyName, attributeLocation));
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

            private static JsonSourceGenerationOptionsAttribute? GetSerializerOptions(AttributeSyntax? attributeSyntax)
            {
                if (attributeSyntax == null)
                {
                    return null;
                }

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
                                if (Enum.TryParse<JsonKnownNamingPolicy>(propertyValueStr, out JsonKnownNamingPolicy value))
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

            private TypeGenerationSpec CreateTypeGenerationSpec(ITypeSymbol type, JsonSourceGenerationMode generationMode, string? typeInfoPropertyName, Location? attributeLocation, Location contextLocation, string contextName)
            {
                Location typeLocation = type.GetDiagnosticLocation() ?? attributeLocation ?? contextLocation;

                ClassType classType;
                JsonPrimitiveTypeKind? primitiveTypeKind = GetPrimitiveTypeKind(type);
                TypeRef? collectionKeyType = null;
                TypeRef? collectionValueType = null;
                TypeRef? nullableUnderlyingType = null;
                TypeRef? extensionDataPropertyType = null;
                string? runtimeTypeRef = null;
                List<PropertyGenerationSpec>? propGenSpecList = null;
                ObjectConstructionStrategy constructionStrategy = default;
                bool constructorSetsRequiredMembers = false;
                ParameterGenerationSpec[]? paramGenSpecArray = null;
                List<PropertyInitializerGenerationSpec>? propertyInitializerSpecList = null;
                CollectionType collectionType = CollectionType.NotApplicable;
                JsonNumberHandling? numberHandling = null;
                JsonUnmappedMemberHandling? unmappedMemberHandling = null;
                JsonObjectCreationHandling? preferredPropertyObjectCreationHandling = null;
                string? immutableCollectionFactoryTypeFullName = null;
                bool foundDesignTimeCustomConverter = false;
                string? converterInstantiationLogic = null;
                bool implementsIJsonOnSerialized = false;
                bool implementsIJsonOnSerializing = false;
                bool isPolymorphic = false;
                bool hasTypeFactoryConverter = false;
                bool hasPropertyFactoryConverters = false;

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
                        foundDesignTimeCustomConverter = true;
                        converterInstantiationLogic = GetConverterInstantiationLogic(
                            type,
                            attributeData,
                            forType: true,
                            ref hasTypeFactoryConverter);
                    }

                    if (SymbolEqualityComparer.Default.Equals(attributeType, _knownSymbols.JsonDerivedTypeAttributeType))
                    {
                        Debug.Assert(attributeData.ConstructorArguments.Length > 0);
                        var derivedType = (ITypeSymbol)attributeData.ConstructorArguments[0].Value!;
                        EnqueueType(derivedType, generationMode);

                        if (!isPolymorphic && generationMode == JsonSourceGenerationMode.Serialization)
                        {
                            ReportDiagnostic(PolymorphismNotSupported, typeLocation, new string[] { type.ToDisplayString() });
                        }

                        isPolymorphic = true;
                    }
                }

                if (foundDesignTimeCustomConverter)
                {
                    classType = converterInstantiationLogic != null
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
                    nullableUnderlyingType = EnqueueType(underlyingType, generationMode);
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
                    collectionValueType = EnqueueType(elementType, generationMode);
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

                    collectionValueType = EnqueueType(valueType, generationMode);

                    if (keyType != null)
                    {
                        collectionKeyType = EnqueueType(keyType, generationMode);

                        if (needsRuntimeType)
                        {
                            runtimeTypeRef = GetDictionaryTypeRef(collectionKeyType, collectionValueType);
                        }
                    }
                }
                else
                {
                    bool useDefaultCtorInAnnotatedStructs = type.GetCompatibleGenericBaseType(_knownSymbols.KeyValuePair) is null;

                    if (!TryGetDeserializationConstructor(type, useDefaultCtorInAnnotatedStructs, out IMethodSymbol? constructor))
                    {
                        classType = ClassType.TypeUnsupportedBySourceGen;
                        ReportDiagnostic(MultipleJsonConstructorAttribute, typeLocation, new string[] { type.ToDisplayString() });
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
                                paramGenSpecArray = new ParameterGenerationSpec[paramCount];

                                for (int i = 0; i < paramCount; i++)
                                {
                                    IParameterSymbol parameterInfo = parameters![i];
                                    TypeRef parameterTypeRef = EnqueueType(parameterInfo.Type, generationMode);

                                    paramGenSpecArray[i] = new ParameterGenerationSpec
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
                        paramGenSpecArray ??= Array.Empty<ParameterGenerationSpec>();
                        int nextParameterIndex = paramGenSpecArray.Length;

                        // Walk the type hierarchy starting from the current type up to the base type(s)
                        foreach (INamedTypeSymbol currentType in type.GetSortedTypeHierarchy())
                        {
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

                                PropertyGenerationSpec? spec = GetPropertyGenerationSpec(currentType, propertyInfo.Type, propertyInfo, isVirtual, generationMode);
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

                                PropertyGenerationSpec? spec = GetPropertyGenerationSpec(currentType, fieldInfo.Type, fieldInfo, isVirtual: false, generationMode);
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
                                hasPropertyFactoryConverters |= spec.HasFactoryConverter;

                                if (spec.IsExtensionData)
                                {
                                    if (extensionDataPropertyType != null)
                                    {
                                        ReportDiagnostic(MultipleJsonExtensionDataAttribute, typeLocation, new string[] { type.Name });
                                    }

                                    if (!IsValidDataExtensionPropertyType(memberType))
                                    {
                                        ReportDiagnostic(DataExtensionPropertyInvalid, memberInfo.GetDiagnosticLocation(), new string[] { type.Name, spec.MemberName });
                                    }

                                    extensionDataPropertyType = spec.PropertyType;
                                }

                                if (constructionStrategy is not ObjectConstructionStrategy.NotApplicable && spec.CanUseSetter &&
                                    ((spec.IsRequired && !constructorSetsRequiredMembers) || spec.IsInitOnlySetter))
                                {
                                    ParameterGenerationSpec? matchingConstructorParameter = GetMatchingConstructorParameter(spec, paramGenSpecArray);

                                    if (spec.IsRequired || matchingConstructorParameter is null)
                                    {
                                        constructionStrategy = ObjectConstructionStrategy.ParameterizedConstructor;

                                        var propInitializerSpec = new PropertyInitializerGenerationSpec
                                        {
                                            Property = spec,
                                            MatchesConstructorParameter = matchingConstructorParameter is not null,
                                            ParameterIndex = matchingConstructorParameter?.ParameterIndex ?? nextParameterIndex++,
                                        };

                                        (propertyInitializerSpecList ??= new()).Add(propInitializerSpec);
                                    }
                                }

                                if (spec.HasJsonInclude && (!spec.CanUseGetter || !spec.CanUseSetter || !spec.IsPublic))
                                {
                                    ReportDiagnostic(InaccessibleJsonIncludePropertiesNotSupported, memberInfo.GetDiagnosticLocation(), new string[] { type.Name, spec.MemberName });
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
                typeInfoPropertyName ??= GetTypeInfoPropertyName(type);

                if (classType is ClassType.TypeUnsupportedBySourceGen)
                {
                    ReportDiagnostic(TypeNotSupported, typeLocation, new string[] { typeRef.FullyQualifiedName });
                }

                if (!_generatedContextAndTypeNames.Add((contextName, typeInfoPropertyName)))
                {
                    // The context name/property name combination will result in a conflict in generated types.
                    // Workaround for https://github.com/dotnet/roslyn/issues/54185 by keeping track of the file names we've used.
                    ReportDiagnostic(DuplicateTypeName, attributeLocation ?? contextLocation, new string[] { typeInfoPropertyName });
                    classType = ClassType.TypeUnsupportedBySourceGen;
                }

                return new TypeGenerationSpec
                {
                    TypeRef = typeRef,
                    TypeInfoPropertyName = typeInfoPropertyName,
                    GenerationMode = generationMode,
                    ClassType = classType,
                    PrimitiveTypeKind = primitiveTypeKind,
                    IsPolymorphic = isPolymorphic,
                    NumberHandling = numberHandling,
                    UnmappedMemberHandling = unmappedMemberHandling,
                    PreferredPropertyObjectCreationHandling = preferredPropertyObjectCreationHandling,
                    PropertyGenSpecs = propGenSpecList?.ToImmutableEquatableArray(),
                    PropertyInitializerSpecs = propertyInitializerSpecList?.ToImmutableEquatableArray(),
                    CtorParamGenSpecs = paramGenSpecArray?.ToImmutableEquatableArray(),
                    CollectionType = collectionType,
                    CollectionKeyType = collectionKeyType,
                    CollectionValueType = collectionValueType,
                    ConstructionStrategy = constructionStrategy,
                    ConstructorSetsRequiredParameters = constructorSetsRequiredMembers,
                    NullableUnderlyingType = nullableUnderlyingType,
                    RuntimeTypeRef = runtimeTypeRef,
                    IsValueTuple = type.IsTupleType,
                    ExtensionDataPropertyType = extensionDataPropertyType,
                    ConverterInstantiationLogic = converterInstantiationLogic,
                    ImplementsIJsonOnSerialized = implementsIJsonOnSerialized,
                    ImplementsIJsonOnSerializing = implementsIJsonOnSerializing,
                    ImmutableCollectionFactoryMethod = DetermineImmutableCollectionFactoryMethod(immutableCollectionFactoryTypeFullName),
                    HasTypeFactoryConverter = hasTypeFactoryConverter,
                    HasPropertyFactoryConverters = hasPropertyFactoryConverters,
                };
            }

            private static string GetDictionaryTypeRef(TypeRef keyType, TypeRef valueType)
                => $"{DictionaryTypeRef}<{keyType.FullyQualifiedName}, {valueType.FullyQualifiedName}>";

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

            private static ParameterGenerationSpec? GetMatchingConstructorParameter(PropertyGenerationSpec propSpec, ParameterGenerationSpec[]? paramGenSpecArray)
            {
                return paramGenSpecArray?.FirstOrDefault(MatchesConstructorParameter);

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

            private PropertyGenerationSpec? GetPropertyGenerationSpec(
                INamedTypeSymbol declaringType,
                ITypeSymbol memberType,
                ISymbol memberInfo,
                bool isVirtual,
                JsonSourceGenerationMode generationMode)
            {
                Debug.Assert(memberInfo is IFieldSymbol or IPropertySymbol);

                ProcessMemberCustomAttributes(
                    memberType,
                    memberInfo,
                    out bool hasJsonInclude,
                    out string? jsonPropertyName,
                    out JsonIgnoreCondition? ignoreCondition,
                    out JsonNumberHandling? numberHandling,
                    out JsonObjectCreationHandling? objectCreationHandling,
                    out string? converterInstantiationLogic,
                    out int order,
                    out bool hasFactoryConverter,
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
                string runtimePropertyName = DetermineRuntimePropName(clrName, jsonPropertyName, _currentContextNamingPolicy);
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
                    DeclaringTypeRef = declaringType.GetFullyQualifiedName(),
                    ConverterInstantiationLogic = converterInstantiationLogic,
                    HasFactoryConverter = hasFactoryConverter
                };
            }

            private void ProcessMemberCustomAttributes(
                ITypeSymbol memberCLRType,
                ISymbol memberInfo,
                out bool hasJsonInclude,
                out string? jsonPropertyName,
                out JsonIgnoreCondition? ignoreCondition,
                out JsonNumberHandling? numberHandling,
                out JsonObjectCreationHandling? objectCreationHandling,
                out string? converterInstantiationLogic,
                out int order,
                out bool hasFactoryConverter,
                out bool isExtensionData,
                out bool hasJsonRequiredAttribute)
            {
                Debug.Assert(memberInfo is IFieldSymbol or IPropertySymbol);

                hasJsonInclude = false;
                jsonPropertyName = null;
                ignoreCondition = default;
                numberHandling = default;
                objectCreationHandling = default;
                converterInstantiationLogic = null;
                order = 0;
                isExtensionData = false;
                hasJsonRequiredAttribute = false;

                bool foundDesignTimeCustomConverter = false;
                hasFactoryConverter = false;

                foreach (AttributeData attributeData in memberInfo.GetAttributes())
                {
                    INamedTypeSymbol? attributeType = attributeData.AttributeClass;

                    if (attributeType is null)
                    {
                        continue;
                    }

                    if (!foundDesignTimeCustomConverter && _knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeType))
                    {
                        foundDesignTimeCustomConverter = true;
                        converterInstantiationLogic = GetConverterInstantiationLogic(
                            memberCLRType,
                            attributeData,
                            forType: false,
                            ref hasFactoryConverter);
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

            private string? GetConverterInstantiationLogic(
                ITypeSymbol type, AttributeData attributeData,
                bool forType, // whether for a type or a property
                ref bool hasFactoryConverter)
            {
                Debug.Assert(_knownSymbols.JsonConverterAttributeType.IsAssignableFrom(attributeData.AttributeClass));

                var converterType = (INamedTypeSymbol?)attributeData.ConstructorArguments[0].Value;

                if (converterType == null || !converterType.Constructors.Any(c => c.Parameters.Length == 0) || converterType.IsNestedPrivate())
                {
                    return null;
                }

                if (converterType.GetCompatibleGenericBaseType(_knownSymbols.JsonConverterOfTType) != null)
                {
                    return $"new {converterType.GetFullyQualifiedName()}()";
                }
                else if (_knownSymbols.JsonConverterFactoryType.IsAssignableFrom(converterType))
                {
                    hasFactoryConverter = true;

                    if (forType)
                    {
                        return $"{Emitter.GetConverterFromFactoryMethodName}({OptionsLocalVariableName}, typeof({type.GetFullyQualifiedName()}), new {converterType.GetFullyQualifiedName()}())";
                    }
                    else
                    {
                        return $"{Emitter.GetConverterFromFactoryMethodName}<{type.GetFullyQualifiedName()}>({OptionsLocalVariableName}, new {converterType.GetFullyQualifiedName()}())";
                    }
                }

                return null;
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

            private void PopulateBuiltInSupportTypes()
            {
                HashSet<ITypeSymbol> builtInSupportTypes = _builtInSupportTypes;

                AddTypeIfNotNull(_knownSymbols.ByteArrayType);
                AddTypeIfNotNull(_knownSymbols.TimeSpanType);
                AddTypeIfNotNull(_knownSymbols.DateTimeOffsetType);
                AddTypeIfNotNull(_knownSymbols.DateOnlyType);
                AddTypeIfNotNull(_knownSymbols.TimeOnlyType);
                AddTypeIfNotNull(_knownSymbols.GuidType);
                AddTypeIfNotNull(_knownSymbols.UriType);
                AddTypeIfNotNull(_knownSymbols.VersionType);

                AddTypeIfNotNull(_knownSymbols.JsonArrayType);
                AddTypeIfNotNull(_knownSymbols.JsonElementType);
                AddTypeIfNotNull(_knownSymbols.JsonNodeType);
                AddTypeIfNotNull(_knownSymbols.JsonObjectType);
                AddTypeIfNotNull(_knownSymbols.JsonValueType);
                AddTypeIfNotNull(_knownSymbols.JsonDocumentType);

                void AddTypeIfNotNull(ITypeSymbol? type)
                {
                    if (type != null)
                    {
                        builtInSupportTypes.Add(type);
                    }
                }
            }
        }
    }
}
