// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed class Parser
        {
            private const string SystemTextJsonNamespace = "System.Text.Json";
            private const string JsonConverterAttributeFullName = "System.Text.Json.Serialization.JsonConverterAttribute";
            private const string JsonConverterFactoryFullName = "System.Text.Json.Serialization.JsonConverterFactory";
            private const string JsonConverterOfTFullName = "System.Text.Json.Serialization.JsonConverter`1";
            private const string JsonArrayFullName = "System.Text.Json.Nodes.JsonArray";
            private const string JsonDerivedTypeAttributeFullName = "System.Text.Json.Serialization.JsonDerivedTypeAttribute";
            private const string JsonElementFullName = "System.Text.Json.JsonElement";
            private const string JsonExtensionDataAttributeFullName = "System.Text.Json.Serialization.JsonExtensionDataAttribute";
            private const string JsonNodeFullName = "System.Text.Json.Nodes.JsonNode";
            private const string JsonObjectFullName = "System.Text.Json.Nodes.JsonObject";
            private const string JsonValueFullName = "System.Text.Json.Nodes.JsonValue";
            private const string JsonDocumentFullName = "System.Text.Json.JsonDocument";
            private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
            private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";
            private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";
            private const string JsonObjectCreationHandlingAttributeFullName = "System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute";
            private const string JsonUnmappedMemberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonUnmappedMemberHandlingAttribute";
            private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
            private const string JsonPropertyOrderAttributeFullName = "System.Text.Json.Serialization.JsonPropertyOrderAttribute";
            private const string JsonRequiredAttributeFullName = "System.Text.Json.Serialization.JsonRequiredAttribute";
            private const string JsonSerializerContextFullName = "System.Text.Json.Serialization.JsonSerializerContext";
            private const string JsonSourceGenerationOptionsAttributeFullName = "System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute";
            private const string SetsRequiredMembersAttributeFullName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";

            internal const string JsonSerializableAttributeFullName = "System.Text.Json.Serialization.JsonSerializableAttribute";

            private const string DateOnlyFullName = "System.DateOnly";
            private const string TimeOnlyFullName = "System.TimeOnly";
            private const string IAsyncEnumerableFullName = "System.Collections.Generic.IAsyncEnumerable`1";

            private const string DictionaryTypeRef = "global::System.Collections.Generic.Dictionary";

            private readonly Compilation _compilation;
            private readonly MetadataLoadContextInternal _metadataLoadContext;

            private readonly Type _ilistOfTType;
            private readonly Type _icollectionOfTType;
            private readonly Type _ienumerableType;
            private readonly Type _ienumerableOfTType;

            private readonly Type? _listOfTType;
            private readonly Type? _dictionaryType;
            private readonly Type? _iasyncEnumerableOfTType;
            private readonly Type? _idictionaryOfTKeyTValueType;
            private readonly Type? _ireadonlyDictionaryType;
            private readonly Type? _isetType;
            private readonly Type? _stackOfTType;
            private readonly Type? _queueOfTType;
            private readonly Type? _concurrentStackType;
            private readonly Type? _concurrentQueueType;
            private readonly Type? _idictionaryType;
            private readonly Type? _ilistType;
            private readonly Type? _stackType;
            private readonly Type? _queueType;
            private readonly Type? _keyValuePair;

            private readonly Type _booleanType;
            private readonly Type _charType;
            private readonly Type _dateTimeType;
            private readonly Type _nullableOfTType;
            private readonly Type _objectType;
            private readonly Type _stringType;

            private readonly Type? _timeSpanType;
            private readonly Type? _dateTimeOffsetType;
            private readonly Type? _dateOnlyType;
            private readonly Type? _timeOnlyType;
            private readonly Type? _byteArrayType;
            private readonly Type? _guidType;
            private readonly Type? _uriType;
            private readonly Type? _versionType;
            private readonly Type? _jsonArrayType;
            private readonly Type? _jsonElementType;
            private readonly Type? _jsonNodeType;
            private readonly Type? _jsonObjectType;
            private readonly Type? _jsonValueType;
            private readonly Type? _jsonDocumentType;

            // Unsupported types
            private readonly Type? _delegateType;
            private readonly Type? _memberInfoType;
            private readonly Type? _serializationInfoType;
            private readonly Type? _intPtrType;
            private readonly Type? _uIntPtrType;

            // Needed for converter validation
            private readonly Type? _jsonConverterOfTType;

            // Keeps track of generated context type names
            private readonly HashSet<(string ContextName, string TypeName)> _generatedContextAndTypeNames = new();

            private readonly HashSet<Type> _numberTypes = new();
            private readonly HashSet<Type> _knownTypes = new();
            private readonly HashSet<Type> _knownUnsupportedTypes = new();

            private readonly Queue<(Type type, JsonSourceGenerationMode mode, string? typeInfoPropertyName, Location? attributeLocation)> _typesToGenerate = new();
            private readonly Dictionary<Type, TypeGenerationSpec> _generatedTypes = new();
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
                _metadataLoadContext = new MetadataLoadContextInternal(_compilation);

                _ilistOfTType = _metadataLoadContext.Resolve(SpecialType.System_Collections_Generic_IList_T);
                _icollectionOfTType = _metadataLoadContext.Resolve(SpecialType.System_Collections_Generic_ICollection_T);
                _ienumerableOfTType = _metadataLoadContext.Resolve(SpecialType.System_Collections_Generic_IEnumerable_T);
                _ienumerableType = _metadataLoadContext.Resolve(SpecialType.System_Collections_IEnumerable);

                _listOfTType = _metadataLoadContext.Resolve(typeof(List<>));
                _dictionaryType = _metadataLoadContext.Resolve(typeof(Dictionary<,>));
                _iasyncEnumerableOfTType = _metadataLoadContext.Resolve(IAsyncEnumerableFullName);
                _idictionaryOfTKeyTValueType = _metadataLoadContext.Resolve(typeof(IDictionary<,>));
                _ireadonlyDictionaryType = _metadataLoadContext.Resolve(typeof(IReadOnlyDictionary<,>));
                _isetType = _metadataLoadContext.Resolve(typeof(ISet<>));
                _stackOfTType = _metadataLoadContext.Resolve(typeof(Stack<>));
                _queueOfTType = _metadataLoadContext.Resolve(typeof(Queue<>));
                _concurrentStackType = _metadataLoadContext.Resolve(typeof(ConcurrentStack<>));
                _concurrentQueueType = _metadataLoadContext.Resolve(typeof(ConcurrentQueue<>));
                _idictionaryType = _metadataLoadContext.Resolve(typeof(IDictionary));
                _ilistType = _metadataLoadContext.Resolve(typeof(IList));
                _stackType = _metadataLoadContext.Resolve(typeof(Stack));
                _queueType = _metadataLoadContext.Resolve(typeof(Queue));
                _keyValuePair = _metadataLoadContext.Resolve(typeof(KeyValuePair<,>));

                _booleanType = _metadataLoadContext.Resolve(SpecialType.System_Boolean);
                _charType = _metadataLoadContext.Resolve(SpecialType.System_Char);
                _timeSpanType = _metadataLoadContext.Resolve(typeof(TimeSpan));
                _dateTimeType = _metadataLoadContext.Resolve(SpecialType.System_DateTime);
                _nullableOfTType = _metadataLoadContext.Resolve(SpecialType.System_Nullable_T);
                _objectType = _metadataLoadContext.Resolve(SpecialType.System_Object);
                _stringType = _metadataLoadContext.Resolve(SpecialType.System_String);

                _dateTimeOffsetType = _metadataLoadContext.Resolve(typeof(DateTimeOffset));
                _byteArrayType = _metadataLoadContext.Resolve(SpecialType.System_Byte).MakeArrayType();
                _guidType = _metadataLoadContext.Resolve(typeof(Guid));
                _uriType = _metadataLoadContext.Resolve(typeof(Uri));
                _versionType = _metadataLoadContext.Resolve(typeof(Version));
                _jsonArrayType = _metadataLoadContext.Resolve(JsonArrayFullName);
                _jsonElementType = _metadataLoadContext.Resolve(JsonElementFullName);
                _jsonNodeType = _metadataLoadContext.Resolve(JsonNodeFullName);
                _jsonObjectType = _metadataLoadContext.Resolve(JsonObjectFullName);
                _jsonValueType = _metadataLoadContext.Resolve(JsonValueFullName);
                _jsonDocumentType = _metadataLoadContext.Resolve(JsonDocumentFullName);
                _dateOnlyType = _metadataLoadContext.Resolve(DateOnlyFullName);
                _timeOnlyType = _metadataLoadContext.Resolve(TimeOnlyFullName);

                // Unsupported types.
                _delegateType = _metadataLoadContext.Resolve(SpecialType.System_Delegate);
                _memberInfoType = _metadataLoadContext.Resolve(typeof(MemberInfo));
                _serializationInfoType = _metadataLoadContext.Resolve(typeof(Runtime.Serialization.SerializationInfo));
                _intPtrType = _metadataLoadContext.Resolve(typeof(IntPtr));
                _uIntPtrType = _metadataLoadContext.Resolve(typeof(UIntPtr));

                _jsonConverterOfTType = _metadataLoadContext.Resolve(JsonConverterOfTFullName);

                PopulateKnownTypes();
            }

            public SourceGenerationSpec? GetGenerationSpec(IEnumerable<ClassDeclarationSyntax> classDeclarationSyntaxList, CancellationToken cancellationToken)
            {
                Compilation compilation = _compilation;
                INamedTypeSymbol? jsonSerializerContextSymbol = compilation.GetBestTypeByMetadataName(JsonSerializerContextFullName);
                INamedTypeSymbol? jsonSerializableAttributeSymbol = compilation.GetBestTypeByMetadataName(JsonSerializableAttributeFullName);
                INamedTypeSymbol? jsonSourceGenerationOptionsAttributeSymbol = compilation.GetBestTypeByMetadataName(JsonSourceGenerationOptionsAttributeFullName);
                INamedTypeSymbol? jsonConverterOfTAttributeSymbol = compilation.GetBestTypeByMetadataName(JsonConverterOfTFullName);

                if (jsonSerializerContextSymbol == null ||
                    jsonSerializableAttributeSymbol == null ||
                    jsonSourceGenerationOptionsAttributeSymbol == null ||
                    jsonConverterOfTAttributeSymbol == null)
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
                            (Type type, JsonSourceGenerationMode mode, string? typeInfoPropertyName, Location? attributeLocation) = _typesToGenerate.Dequeue();
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

                        Type contextType = contextTypeSymbol.AsType(_metadataLoadContext);
                        ContextGenerationSpec contextGenSpec = new()
                        {
                            ContextType = new(contextType),
                            GeneratedTypes = _generatedTypes.Values.ToImmutableEquatableArray(),
                            Namespace = contextType.Namespace,
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

                Type type = typeSymbol.AsType(_metadataLoadContext);
                EnqueueType(type, generationMode, typeInfoPropertyName, attributeSyntax.GetLocation());
            }

            private TypeRef EnqueueType(Type type, JsonSourceGenerationMode generationMode, string? typeInfoPropertyName = null, Location? attributeLocation = null)
            {
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

                if (Enum.TryParse<JsonSourceGenerationMode>(enumAsStr, out JsonSourceGenerationMode value))
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
                                if (Enum.TryParse<JsonIgnoreCondition>(propertyValueStr, out JsonIgnoreCondition value))
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

            private TypeGenerationSpec CreateTypeGenerationSpec(Type type, JsonSourceGenerationMode generationMode, string? typeInfoPropertyName, Location? attributeLocation, Location contextLocation, string contextName)
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
                bool foundDesignTimeCustomConverter = false;
                string? converterInstantiationLogic = null;
                bool implementsIJsonOnSerialized = false;
                bool implementsIJsonOnSerializing = false;
                bool isPolymorphic = false;
                bool hasTypeFactoryConverter = false;
                bool hasPropertyFactoryConverters = false;
                bool canContainNullableReferenceAnnotations = type.CanContainNullableReferenceTypeAnnotations();

                IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(type);
                foreach (CustomAttributeData attributeData in attributeDataList)
                {
                    Type attributeType = attributeData.AttributeType;
                    string? attributeTypeFullName = attributeType.FullName;

                    if (attributeTypeFullName == JsonNumberHandlingAttributeFullName)
                    {
                        IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                        numberHandling = (JsonNumberHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (attributeTypeFullName == JsonUnmappedMemberHandlingAttributeFullName)
                    {
                        IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                        unmappedMemberHandling = (JsonUnmappedMemberHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (attributeTypeFullName == JsonObjectCreationHandlingAttributeFullName)
                    {
                        IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                        preferredPropertyObjectCreationHandling = (JsonObjectCreationHandling)ctorArgs[0].Value!;
                        continue;
                    }
                    else if (!foundDesignTimeCustomConverter && attributeType.GetCompatibleBaseClass(JsonConverterAttributeFullName) != null)
                    {
                        foundDesignTimeCustomConverter = true;
                        converterInstantiationLogic = GetConverterInstantiationLogic(
                            type,
                            attributeData,
                            forType: true,
                            ref hasTypeFactoryConverter);
                    }

                    if (attributeTypeFullName == JsonDerivedTypeAttributeFullName)
                    {
                        Debug.Assert(attributeData.ConstructorArguments.Count > 0);
                        ITypeSymbol derivedTypeSymbol = (ITypeSymbol)attributeData.ConstructorArguments[0].Value!;
                        Type derivedType = derivedTypeSymbol.AsType(_metadataLoadContext);
                        EnqueueType(derivedType, generationMode);

                        if (!isPolymorphic && generationMode == JsonSourceGenerationMode.Serialization)
                        {
                            ReportDiagnostic(PolymorphismNotSupported, typeLocation, new string[] { type.FullName! });
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
                else if (_knownTypes.Contains(type))
                {
                    classType = ClassType.KnownType;
                }
                else if (
                    _knownUnsupportedTypes.Contains(type) ||
                    _memberInfoType?.IsAssignableFrom(type) == true ||
                    _delegateType?.IsAssignableFrom(type) == true ||
                    (type.IsArray && type.GetArrayRank() > 1))
                {
                    classType = ClassType.KnownUnsupportedType;
                }
                else if (type.IsNullableValueType(_nullableOfTType, out Type? underlyingType))
                {
                    Debug.Assert(underlyingType != null);
                    classType = ClassType.Nullable;
                    nullableUnderlyingType = EnqueueType(underlyingType, generationMode);
                }
                else if (type.IsEnum)
                {
                    classType = ClassType.Enum;
                }
                else if (type.GetCompatibleGenericInterface(_iasyncEnumerableOfTType) is Type iasyncEnumerableType)
                {
                    if (type.CanUseDefaultConstructorForDeserialization(out ConstructorInfo? defaultCtor))
                    {
                        constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                        constructorSetsRequiredMembers = defaultCtor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                    }

                    Type elementType = iasyncEnumerableType.GetGenericArguments()[0];
                    collectionValueType = EnqueueType(elementType, generationMode);
                    collectionType = CollectionType.IAsyncEnumerableOfT;
                    classType = ClassType.Enumerable;
                }
                else if (_ienumerableType.IsAssignableFrom(type))
                {
                    if (type.CanUseDefaultConstructorForDeserialization(out ConstructorInfo? defaultCtor))
                    {
                        constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                        constructorSetsRequiredMembers = defaultCtor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                    }

                    Type? actualTypeToConvert;
                    Type? keyType = null;
                    Type valueType;
                    bool needsRuntimeType = false;

                    if (type.IsArray)
                    {
                        Debug.Assert(type.GetArrayRank() == 1, "multi-dimensional arrays should have been handled earlier.");
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Array;
                        valueType = type.GetElementType()!;
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _listOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.List;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _dictionaryType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.Dictionary;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];
                    }
                    else if (type.IsImmutableDictionaryType(sourceGenType: true))
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.ImmutableDictionary;

                        Type[] genericArgs = type.GetGenericArguments();
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_idictionaryOfTKeyTValueType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionaryOfTKeyTValue;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];

                        needsRuntimeType = type == actualTypeToConvert;
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ireadonlyDictionaryType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IReadOnlyDictionary;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        keyType = genericArgs[0];
                        valueType = genericArgs[1];

                        needsRuntimeType = type == actualTypeToConvert;
                    }
                    else if (type.IsImmutableEnumerableType(sourceGenType: true))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ImmutableEnumerable;
                        valueType = type.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ilistOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IListOfT;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_isetType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ISet;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_icollectionOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ICollectionOfT;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _stackOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.StackOfT;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _queueOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.QueueOfT;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseClass(_concurrentStackType, sourceGenType: true)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentStack;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseClass(_concurrentQueueType, sourceGenType: true)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentQueue;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ienumerableOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerableOfT;
                        valueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if (_idictionaryType?.IsAssignableFrom(type) == true)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionary;
                        keyType = _stringType;
                        valueType = _objectType;

                        needsRuntimeType = type == actualTypeToConvert;
                    }
                    else if (_ilistType?.IsAssignableFrom(type) == true)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IList;
                        valueType = _objectType;
                    }
                    else if (_stackType?.IsAssignableFrom(type) == true)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Stack;
                        valueType = _objectType;
                    }
                    else if (_queueType?.IsAssignableFrom(type) == true)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Queue;
                        valueType = _objectType;
                    }
                    else
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerable;
                        valueType = _objectType;
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
                    bool useDefaultCtorInAnnotatedStructs = !type.IsKeyValuePair(_keyValuePair);

                    if (!type.TryGetDeserializationConstructor(useDefaultCtorInAnnotatedStructs, out ConstructorInfo? constructor))
                    {
                        classType = ClassType.TypeUnsupportedBySourceGen;
                        ReportDiagnostic(MultipleJsonConstructorAttribute, typeLocation, new string[] { $"{type}" });
                    }
                    else
                    {
                        classType = ClassType.Object;

                        if ((constructor != null || type.IsValueType) && !type.IsAbstract)
                        {
                            constructorSetsRequiredMembers = constructor?.ContainsAttribute(SetsRequiredMembersAttributeFullName) == true;
                            ParameterInfo[]? parameters = constructor?.GetParameters();
                            int paramCount = parameters?.Length ?? 0;

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
                                    ParameterInfo parameterInfo = parameters![i];
                                    TypeRef parameterTypeRef = EnqueueType(parameterInfo.ParameterType, generationMode);

                                    paramGenSpecArray[i] = new ParameterGenerationSpec()
                                    {
                                        ParameterType = parameterTypeRef,
                                        Name = parameterInfo.Name!,
                                        HasDefaultValue = parameterInfo.HasDefaultValue,
                                        DefaultValue = parameterInfo.GetDefaultValue(),
                                        ParameterIndex = i
                                    };
                                }
                            }
                        }

                        // GetInterface() is currently not implemented, so we use GetInterfaces().
                        IEnumerable<string> interfaces = type.GetInterfaces().Select(interfaceType => interfaceType.FullName!);
                        implementsIJsonOnSerialized = interfaces.FirstOrDefault(interfaceName => interfaceName == JsonConstants.IJsonOnSerializedFullName) != null;
                        implementsIJsonOnSerializing = interfaces.FirstOrDefault(interfaceName => interfaceName == JsonConstants.IJsonOnSerializingFullName) != null;

                        propGenSpecList = new List<PropertyGenerationSpec>();
                        Dictionary<string, MemberInfo>? ignoredMembers = null;

                        const BindingFlags bindingFlags =
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly;

                        bool propertyOrderSpecified = false;
                        paramGenSpecArray ??= Array.Empty<ParameterGenerationSpec>();
                        int nextParameterIndex = paramGenSpecArray.Length;

                        // Walk the type hierarchy starting from the current type up to the base type(s)
                        foreach (Type currentType in type.GetSortedTypeHierarchy())
                        {
                            foreach (PropertyInfo propertyInfo in currentType.GetProperties(bindingFlags))
                            {
                                bool isVirtual = propertyInfo.IsVirtual();

                                if (propertyInfo.GetIndexParameters().Length > 0 ||
                                    PropertyIsOverriddenAndIgnored(propertyInfo, propertyInfo.PropertyType, isVirtual, ignoredMembers))
                                {
                                    continue;
                                }

                                PropertyGenerationSpec? spec = GetPropertyGenerationSpec(propertyInfo.PropertyType, propertyInfo, isVirtual, generationMode);
                                if (spec is null)
                                {
                                    continue;
                                }

                                CacheMemberHelper(propertyInfo.PropertyType, propertyInfo, spec);
                            }

                            foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                            {
                                if (PropertyIsOverriddenAndIgnored(fieldInfo, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
                                {
                                    continue;
                                }

                                PropertyGenerationSpec? spec = GetPropertyGenerationSpec(fieldInfo.FieldType, fieldInfo, isVirtual: false, generationMode);
                                if (spec is null)
                                {
                                    continue;
                                }

                                CacheMemberHelper(fieldInfo.FieldType, fieldInfo, spec);
                            }

                            void CacheMemberHelper(Type memberType, MemberInfo memberInfo, PropertyGenerationSpec spec)
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
                typeInfoPropertyName ??= type.GetTypeInfoPropertyName();

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
                    ExtensionDataPropertyType = extensionDataPropertyType,
                    ConverterInstantiationLogic = converterInstantiationLogic,
                    ImplementsIJsonOnSerialized = implementsIJsonOnSerialized,
                    ImplementsIJsonOnSerializing = implementsIJsonOnSerializing,
                    ImmutableCollectionBuilderName = DetermineImmutableCollectionBuilderName(type, collectionType),
                    HasTypeFactoryConverter = hasTypeFactoryConverter,
                    HasPropertyFactoryConverters = hasPropertyFactoryConverters,
                };
            }

            private static string GetDictionaryTypeRef(TypeRef keyType, TypeRef valueType)
                => $"{DictionaryTypeRef}<{keyType.FullyQualifiedName}, {valueType.FullyQualifiedName}>";

            private bool IsValidDataExtensionPropertyType(Type type)
            {
                if (type == _jsonObjectType)
                {
                    return true;
                }

                Type? actualDictionaryType = type.GetCompatibleGenericInterface(_idictionaryOfTKeyTValueType);
                if (actualDictionaryType == null)
                {
                    return false;
                }

                Type[] genericArguments = actualDictionaryType.GetGenericArguments();
                return genericArguments[0] == _stringType && (genericArguments[1] == _objectType || genericArguments[1] == _jsonElementType);
            }

            private static Type? GetCompatibleGenericBaseClass(Type type, Type? baseType)
                => type.GetCompatibleGenericBaseClass(baseType);

            private static void CacheMember(
                MemberInfo memberInfo,
                PropertyGenerationSpec propGenSpec,
                ref List<PropertyGenerationSpec> propGenSpecList,
                ref Dictionary<string, MemberInfo>? ignoredMembers)
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
                MemberInfo memberInfo,
                Type currentMemberType,
                bool currentMemberIsVirtual,
                Dictionary<string, MemberInfo>? ignoredMembers)
            {
                if (ignoredMembers == null || !ignoredMembers.TryGetValue(memberInfo.Name, out MemberInfo? ignoredMember))
                {
                    return false;
                }

                return currentMemberType == ignoredMember.GetMemberType() &&
                    currentMemberIsVirtual &&
                    ignoredMember.IsVirtual();
            }

            private PropertyGenerationSpec? GetPropertyGenerationSpec(
                Type memberType,
                MemberInfo memberInfo,
                bool isVirtual,
                JsonSourceGenerationMode generationMode)
            {
                Debug.Assert(memberInfo.DeclaringType != null);
                IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(memberInfo);

                ProcessMemberCustomAttributes(
                    attributeDataList,
                    memberType,
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
                    out bool getterIsVirtual,
                    out bool setterIsVirtual,
                    out bool setterIsInitOnly);

                if (!isPublic && !memberType.IsPublic)
                {
                    return null;
                }

                bool needsAtSign = memberInfo switch
                {
                    PropertyInfoWrapper prop => prop.NeedsAtSign,
                    FieldInfoWrapper field => field.NeedsAtSign,
                    _ => false
                };

                string clrName = memberInfo.Name;
                string runtimePropertyName = DetermineRuntimePropName(clrName, jsonPropertyName, _currentContextNamingPolicy);
                string propertyNameVarName = DeterminePropNameIdentifier(runtimePropertyName);

                return new PropertyGenerationSpec
                {
                    NameSpecifiedInSourceCode = needsAtSign ? "@" + memberInfo.Name : memberInfo.Name,
                    MemberName = memberInfo.Name,
                    IsProperty = memberInfo.MemberType == MemberTypes.Property,
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
                    GetterIsVirtual = getterIsVirtual,
                    SetterIsVirtual = setterIsVirtual,
                    DefaultIgnoreCondition = ignoreCondition,
                    NumberHandling = numberHandling,
                    ObjectCreationHandling = objectCreationHandling,
                    Order = order,
                    HasJsonInclude = hasJsonInclude,
                    IsExtensionData = isExtensionData,
                    PropertyType = EnqueueType(memberType, generationMode),
                    DeclaringTypeRef = memberInfo.DeclaringType.GetCompilableName(),
                    ConverterInstantiationLogic = converterInstantiationLogic,
                    HasFactoryConverter = hasFactoryConverter
                };
            }

            private void ProcessMemberCustomAttributes(
                IList<CustomAttributeData> attributeDataList,
                Type memberCLRType,
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

                foreach (CustomAttributeData attributeData in attributeDataList)
                {
                    Type attributeType = attributeData.AttributeType;

                    if (!foundDesignTimeCustomConverter && attributeType.GetCompatibleBaseClass(JsonConverterAttributeFullName) != null)
                    {
                        foundDesignTimeCustomConverter = true;
                        converterInstantiationLogic = GetConverterInstantiationLogic(
                            memberCLRType,
                            attributeData,
                            forType: false,
                            ref hasFactoryConverter);
                    }
                    else if (attributeType.Assembly.FullName == SystemTextJsonNamespace)
                    {
                        switch (attributeData.AttributeType.FullName)
                        {
                            case JsonIgnoreAttributeFullName:
                                {
                                    IList<CustomAttributeNamedArgument> namedArgs = attributeData.NamedArguments;

                                    if (namedArgs.Count == 0)
                                    {
                                        ignoreCondition = JsonIgnoreCondition.Always;
                                    }
                                    else if (namedArgs.Count == 1 &&
                                        namedArgs[0].MemberInfo.MemberType == MemberTypes.Property &&
                                        ((PropertyInfo)namedArgs[0].MemberInfo).PropertyType.FullName == JsonIgnoreConditionFullName)
                                    {
                                        ignoreCondition = (JsonIgnoreCondition)namedArgs[0].TypedValue.Value!;
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
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                                    numberHandling = (JsonNumberHandling)ctorArgs[0].Value!;
                                }
                                break;
                            case JsonObjectCreationHandlingAttributeFullName:
                                {
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                                    objectCreationHandling = (JsonObjectCreationHandling)ctorArgs[0].Value!;
                                }
                                break;
                            case JsonPropertyNameAttributeFullName:
                                {
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                                    jsonPropertyName = (string)ctorArgs[0].Value!;
                                    // Null check here is done at runtime within JsonSerializer.
                                }
                                break;
                            case JsonPropertyOrderAttributeFullName:
                                {
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
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
                MemberInfo memberInfo,
                bool hasJsonInclude,
                out bool isReadOnly,
                out bool isPublic,
                out bool isRequired,
                out bool canUseGetter,
                out bool canUseSetter,
                out bool getterIsVirtual,
                out bool setterIsVirtual,
                out bool setterIsInitOnly)
            {
                isPublic = false;
                isRequired = false;
                canUseGetter = false;
                canUseSetter = false;
                getterIsVirtual = false;
                setterIsVirtual = false;
                setterIsInitOnly = false;

                switch (memberInfo)
                {
                    case PropertyInfo propertyInfo:
                        {
                            MethodInfo? getMethod = propertyInfo.GetMethod;
                            MethodInfo? setMethod = propertyInfo.SetMethod;
                            isRequired = propertyInfo.IsRequired();

                            if (getMethod != null)
                            {
                                if (getMethod.IsPublic)
                                {
                                    isPublic = true;
                                    canUseGetter = true;
                                }
                                else if (getMethod.IsAssembly)
                                {
                                    canUseGetter = hasJsonInclude;
                                }

                                getterIsVirtual = getMethod.IsVirtual;
                            }

                            if (setMethod != null)
                            {
                                isReadOnly = false;
                                setterIsInitOnly = setMethod.IsInitOnly();

                                if (setMethod.IsPublic)
                                {
                                    isPublic = true;
                                    canUseSetter = true;
                                }
                                else if (setMethod.IsAssembly)
                                {
                                    canUseSetter = hasJsonInclude;
                                }

                                setterIsVirtual = setMethod.IsVirtual;
                            }
                            else
                            {
                                isReadOnly = true;
                            }
                        }
                        break;
                    case FieldInfo fieldInfo:
                        {
                            isPublic = fieldInfo.IsPublic;
                            isReadOnly = fieldInfo.IsInitOnly;
                            isRequired = fieldInfo.IsRequired();

                            if (!fieldInfo.IsPrivate && !fieldInfo.IsFamily)
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
                Type type, CustomAttributeData attributeData,
                bool forType, // whether for a type or a property
                ref bool hasFactoryConverter)
            {
                if (attributeData.AttributeType.FullName != JsonConverterAttributeFullName)
                {
                    return null;
                }

                ITypeSymbol converterTypeSymbol = (ITypeSymbol)attributeData.ConstructorArguments[0].Value!;
                Type? converterType = converterTypeSymbol.AsType(_metadataLoadContext);

                if (converterType == null || converterType.GetConstructor(Type.EmptyTypes) == null || converterType.IsNestedPrivate)
                {
                    return null;
                }

                if (converterType.GetCompatibleGenericBaseClass(_jsonConverterOfTType) != null)
                {
                    return $"new {converterType.GetCompilableName()}()";
                }
                else if (converterType.GetCompatibleBaseClass(JsonConverterFactoryFullName) != null)
                {
                    hasFactoryConverter = true;

                    if (forType)
                    {
                        return $"{Emitter.GetConverterFromFactoryMethodName}({OptionsLocalVariableName}, typeof({type.GetCompilableName()}), new {converterType.GetCompilableName()}())";
                    }
                    else
                    {
                        return $"{Emitter.GetConverterFromFactoryMethodName}<{type.GetCompilableName()}>({OptionsLocalVariableName}, new {converterType.GetCompilableName()}())";
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

            private static string? DetermineImmutableCollectionBuilderName(Type type, CollectionType collectionType)
            {
                string? builderName;

                if (collectionType == CollectionType.ImmutableDictionary)
                {
                    builderName = type.GetImmutableDictionaryConstructingTypeName(sourceGenType: true);
                }
                else if (collectionType == CollectionType.ImmutableEnumerable)
                {
                    builderName = type.GetImmutableEnumerableConstructingTypeName(sourceGenType: true);
                }
                else
                {
                    return null;
                }

                Debug.Assert(builderName != null);
                return $"global::{builderName}.{ReflectionExtensions.CreateRangeMethodName}";
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

            private JsonPrimitiveTypeKind? GetPrimitiveTypeKind(Type type)
            {
                if (_numberTypes.Contains(type))
                    return JsonPrimitiveTypeKind.Number;

                if (type == _stringType || type == _dateTimeType || type == _dateTimeOffsetType || type == _guidType)
                {
                    return JsonPrimitiveTypeKind.String;
                }

                if (type == _booleanType)
                {
                    return JsonPrimitiveTypeKind.Boolean;
                }

                if (type == _byteArrayType)
                {
                    return JsonPrimitiveTypeKind.ByteArray;
                }

                if (type == _charType)
                {
                    return JsonPrimitiveTypeKind.Char;
                }

                return null;
            }

            private void PopulateNumberTypes()
            {
                Debug.Assert(_numberTypes != null);
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Byte));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Decimal));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Double));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Int16));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_SByte));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Int32));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Int64));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_Single));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_UInt16));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_UInt32));
                _numberTypes.Add(_metadataLoadContext.Resolve(SpecialType.System_UInt64));
            }

            private void PopulateKnownTypes()
            {
                PopulateNumberTypes();

                Debug.Assert(_knownTypes != null);
                Debug.Assert(_numberTypes != null);
                Debug.Assert(_knownUnsupportedTypes != null);

                _knownTypes.UnionWith(_numberTypes);
                _knownTypes.Add(_booleanType);
                _knownTypes.Add(_charType);
                _knownTypes.Add(_dateTimeType);
                _knownTypes.Add(_objectType);
                _knownTypes.Add(_stringType);

                AddTypeIfNotNull(_knownTypes, _byteArrayType);
                AddTypeIfNotNull(_knownTypes, _timeSpanType);
                AddTypeIfNotNull(_knownTypes, _dateTimeOffsetType);
                AddTypeIfNotNull(_knownTypes, _dateOnlyType);
                AddTypeIfNotNull(_knownTypes, _timeOnlyType);
                AddTypeIfNotNull(_knownTypes, _guidType);
                AddTypeIfNotNull(_knownTypes, _uriType);
                AddTypeIfNotNull(_knownTypes, _versionType);
                AddTypeIfNotNull(_knownTypes, _jsonArrayType);
                AddTypeIfNotNull(_knownTypes, _jsonElementType);
                AddTypeIfNotNull(_knownTypes, _jsonNodeType);
                AddTypeIfNotNull(_knownTypes, _jsonObjectType);
                AddTypeIfNotNull(_knownTypes, _jsonValueType);
                AddTypeIfNotNull(_knownTypes, _jsonDocumentType);

                AddTypeIfNotNull(_knownUnsupportedTypes, _serializationInfoType);
                AddTypeIfNotNull(_knownUnsupportedTypes, _intPtrType);
                AddTypeIfNotNull(_knownUnsupportedTypes, _uIntPtrType);

                static void AddTypeIfNotNull(HashSet<Type> types, Type? type)
                {
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }
            }
        }
    }
}
