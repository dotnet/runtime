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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed class Parser
        {
            private const string SystemTextJsonNamespace = "System.Text.Json";
            private const string JsonConverterAttributeFullName = "System.Text.Json.Serialization.JsonConverterAttribute";
            private const string JsonElementFullName = "System.Text.Json.JsonElement";
            private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
            private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";
            private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";
            private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";

            private const string JsonPropertyOrderAttributeFullName = "System.Text.Json.Serialization.JsonPropertyOrderAttribute";

            private readonly GeneratorExecutionContext _executionContext;
            private readonly MetadataLoadContextInternal _metadataLoadContext;

            private readonly Type _ilistOfTType;
            private readonly Type _icollectionOfTType;
            private readonly Type _ienumerableType;
            private readonly Type _ienumerableOfTType;

            private readonly Type? _listOfTType;
            private readonly Type? _dictionaryType;
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

            private readonly Type _booleanType;
            private readonly Type _charType;
            private readonly Type _dateTimeType;
            private readonly Type _nullableOfTType;
            private readonly Type _objectType;
            private readonly Type _stringType;

            private readonly Type? _dateTimeOffsetType;
            private readonly Type? _byteArrayType;
            private readonly Type? _guidType;
            private readonly Type? _uriType;
            private readonly Type? _versionType;
            private readonly Type? _jsonElementType;

            private readonly HashSet<Type> _numberTypes = new();
            private readonly HashSet<Type> _knownTypes = new();

            /// <summary>
            /// Type information for member types in input object graphs.
            /// </summary>
            private readonly Dictionary<Type, TypeGenerationSpec> _typeGenerationSpecCache = new();

            private readonly HashSet<TypeGenerationSpec> _nullableTypeGenerationSpecCache = new();

            private JsonKnownNamingPolicy _currentContextNamingPolicy;

            private static DiagnosticDescriptor ContextClassesMustBePartial { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1032",
                title: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.ContextClassesMustBePartialMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            public Parser(in GeneratorExecutionContext executionContext)
            {
                _executionContext = executionContext;
                _metadataLoadContext = new MetadataLoadContextInternal(executionContext.Compilation);

                _ilistOfTType = ResolveType(SpecialType.System_Collections_Generic_IList_T);
                _icollectionOfTType = ResolveType(SpecialType.System_Collections_Generic_ICollection_T);
                _ienumerableOfTType = ResolveType(SpecialType.System_Collections_Generic_IEnumerable_T);
                _ienumerableType = ResolveType(SpecialType.System_Collections_IEnumerable);

                _listOfTType = ResolveType(typeof(List<>).FullName!);
                _dictionaryType = ResolveType(typeof(Dictionary<,>).FullName!);
                _idictionaryOfTKeyTValueType = ResolveType(typeof(IDictionary<,>).FullName!);
                _ireadonlyDictionaryType = ResolveType(typeof(IReadOnlyDictionary<,>).FullName!);
                _isetType = ResolveType(typeof(ISet<>).FullName!);
                _stackOfTType = ResolveType(typeof(Stack<>).FullName!);
                _queueOfTType = ResolveType(typeof(Queue<>).FullName!);
                _concurrentStackType = ResolveType(typeof(ConcurrentStack<>).FullName!);
                _concurrentQueueType = ResolveType(typeof(ConcurrentQueue<>).FullName!);
                _idictionaryType = ResolveType(typeof(IDictionary).FullName!);
                _ilistType = ResolveType(typeof(IList).FullName!);
                _stackType = ResolveType(typeof(Stack).FullName!);
                _queueType = ResolveType(typeof(Queue).FullName!);

                _booleanType = ResolveType(SpecialType.System_Boolean);
                _charType = ResolveType(SpecialType.System_Char);
                _dateTimeType = ResolveType(SpecialType.System_DateTime);
                _nullableOfTType = ResolveType(SpecialType.System_Nullable_T);
                _objectType = ResolveType(SpecialType.System_Object);
                _stringType = ResolveType(SpecialType.System_String);

                _dateTimeOffsetType = ResolveType(typeof(DateTimeOffset).FullName!);
                _byteArrayType = ResolveType(typeof(byte[]).FullName!);
                _guidType = ResolveType(typeof(Guid).FullName!);
                _uriType = ResolveType(typeof(Uri).FullName!);
                _versionType = ResolveType(typeof(Version).FullName!);
                _jsonElementType = ResolveType(JsonElementFullName);

                PopulateKnownTypes();
            }

            public SourceGenerationSpec? GetGenerationSpec(List<ClassDeclarationSyntax> classDeclarationSyntaxList)
            {
                Compilation compilation = _executionContext.Compilation;
                INamedTypeSymbol jsonSerializerContextSymbol = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSerializerContext");
                INamedTypeSymbol jsonSerializableAttributeSymbol = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSerializableAttribute");
                INamedTypeSymbol jsonSourceGenerationOptionsAttributeSymbol = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute");

                if (jsonSerializerContextSymbol == null || jsonSerializableAttributeSymbol == null || jsonSourceGenerationOptionsAttributeSymbol == null)
                {
                    return null;
                }

                List<ContextGenerationSpec>? contextGenSpecList = null;

                foreach (ClassDeclarationSyntax classDeclarationSyntax in classDeclarationSyntaxList)
                {
                    CompilationUnitSyntax compilationUnitSyntax = classDeclarationSyntax.FirstAncestorOrSelf<CompilationUnitSyntax>();
                    SemanticModel compilationSemanticModel = compilation.GetSemanticModel(compilationUnitSyntax.SyntaxTree);

                    if (!DerivesFromJsonSerializerContext(classDeclarationSyntax, jsonSerializerContextSymbol, compilationSemanticModel))
                    {
                        continue;
                    }

                    JsonSourceGenerationOptionsAttribute? options = null;
                    List<AttributeSyntax>? serializableAttributeList = null;

                    foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
                    {
                        AttributeSyntax attributeSyntax = attributeListSyntax.Attributes.First();
                        IMethodSymbol attributeSymbol = compilationSemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                        if (attributeSymbol == null)
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

                    INamedTypeSymbol contextTypeSymbol = (INamedTypeSymbol)compilationSemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                    Debug.Assert(contextTypeSymbol != null);

                    if (!TryGetClassDeclarationList(contextTypeSymbol, out List<string> classDeclarationList))
                    {
                        // Class or one of its containing types is not partial so we can't add to it.
                        _executionContext.ReportDiagnostic(Diagnostic.Create(ContextClassesMustBePartial, Location.None, new string[] { contextTypeSymbol.Name }));
                        continue;
                    }

                    ContextGenerationSpec contextGenSpec = new()
                    {
                        GenerationOptions = options ?? new JsonSourceGenerationOptionsAttribute(),
                        ContextType = contextTypeSymbol.AsType(_metadataLoadContext),
                        ContextClassDeclarationList = classDeclarationList
                    };

                    // Set the naming policy for the current context.
                    _currentContextNamingPolicy = contextGenSpec.GenerationOptions.PropertyNamingPolicy;

                    foreach (AttributeSyntax attribute in serializableAttributeList)
                    {
                        TypeGenerationSpec? metadata = GetRootSerializableType(compilationSemanticModel, attribute, contextGenSpec.GenerationOptions.GenerationMode);
                        if (metadata != null)
                        {
                            contextGenSpec.RootSerializableTypes.Add(metadata);
                        }
                    }

                    if (contextGenSpec.RootSerializableTypes.Count == 0)
                    {
                        continue;
                    }

                    contextGenSpec.NullableUnderlyingTypes.UnionWith(_nullableTypeGenerationSpecCache);

                    contextGenSpecList ??= new List<ContextGenerationSpec>();
                    contextGenSpecList.Add(contextGenSpec);

                    // Clear the cache of generated metadata between the processing of context classes.
                    _typeGenerationSpecCache.Clear();
                    _nullableTypeGenerationSpecCache.Clear();
                }

                if (contextGenSpecList == null)
                {
                    return null;
                }

                return new SourceGenerationSpec
                {
                    ContextGenerationSpecList = contextGenSpecList,
                    BooleanType = _booleanType,
                    ByteArrayType = _byteArrayType,
                    CharType = _charType,
                    DateTimeType = _dateTimeType,
                    DateTimeOffsetType = _dateTimeOffsetType,
                    GuidType = _guidType,
                    StringType = _stringType,
                    NumberTypes = _numberTypes,
                };
            }

            // Returns true if a given type derives directly from JsonSerializerContext.
            private bool DerivesFromJsonSerializerContext(
                ClassDeclarationSyntax classDeclarationSyntax,
                INamedTypeSymbol jsonSerializerContextSymbol,
                SemanticModel compilationSemanticModel)
            {
                SeparatedSyntaxList<BaseTypeSyntax>? baseTypeSyntaxList = classDeclarationSyntax.BaseList?.Types;
                if (baseTypeSyntaxList == null)
                {
                    return false;
                }

                INamedTypeSymbol? match = null;

                foreach (BaseTypeSyntax baseTypeSyntax in baseTypeSyntaxList)
                {
                    INamedTypeSymbol? candidate = compilationSemanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol as INamedTypeSymbol;
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
                        declarationElements[tokenCount + 1] = currentSymbol.Name;

                        (classDeclarationList ??= new List<string>()).Add(string.Join(" ", declarationElements));
                    }

                    currentSymbol = currentSymbol.ContainingType;
                }

                Debug.Assert(classDeclarationList.Count > 0);
                return true;
            }

            private TypeGenerationSpec? GetRootSerializableType(
                SemanticModel compilationSemanticModel,
                AttributeSyntax attributeSyntax,
                JsonSourceGenerationMode generationMode)
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
                            typeSymbol = compilationSemanticModel.GetTypeInfo(typeNameSyntax).ConvertedType;
                        }

                        seenFirstArg = true;
                    }
                    else
                    {
                        IEnumerable<SyntaxNode> childNodes = node.ChildNodes();

                        NameEqualsSyntax? propertyNameNode = childNodes.First() as NameEqualsSyntax;
                        Debug.Assert(propertyNameNode != null);

                        SyntaxNode? propertyValueNode = childNodes.ElementAtOrDefault(1);
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
                    return null;
                }

                Type type = typeSymbol.AsType(_metadataLoadContext);
                if (type.Namespace == "<global namespace>")
                {
                    // typeof() reference where the type's name isn't fully qualified.
                    // The compilation is not valid and the user needs to fix their code.
                    // The compiler will notify the user so we don't have to.
                    return null;
                }

                TypeGenerationSpec typeGenerationSpec = GetOrAddTypeGenerationSpec(type, generationMode);

                if (typeInfoPropertyName != null)
                {
                    typeGenerationSpec.TypeInfoPropertyName = typeInfoPropertyName;
                }

                if (generationMode != default)
                {
                    typeGenerationSpec.GenerationMode = generationMode;
                }

                return typeGenerationSpec;
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

                    SyntaxNode? propertyValueNode = childNodes.ElementAtOrDefault(1);
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
                        case nameof(JsonSourceGenerationOptionsAttribute.IgnoreRuntimeCustomConverters):
                            {
                                if (bool.TryParse(propertyValueStr, out bool value))
                                {
                                    options.IgnoreRuntimeCustomConverters = value;
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

            private TypeGenerationSpec GetOrAddTypeGenerationSpec(Type type, JsonSourceGenerationMode generationMode)
            {
                if (_typeGenerationSpecCache.TryGetValue(type, out TypeGenerationSpec? typeMetadata))
                {
                    return typeMetadata!;
                }

                // Add metadata to cache now to prevent stack overflow when the same type is found somewhere else in the object graph.
                typeMetadata = new TypeGenerationSpec();
                _typeGenerationSpecCache[type] = typeMetadata;

                ClassType classType;
                Type? collectionKeyType = null;
                Type? collectionValueType = null;
                TypeGenerationSpec? nullableUnderlyingTypeGenSpec = null;
                List<PropertyGenerationSpec>? propGenSpecList = null;
                ObjectConstructionStrategy constructionStrategy = default;
                CollectionType collectionType = CollectionType.NotApplicable;
                JsonNumberHandling? numberHandling = null;
                bool foundDesignTimeCustomConverter = false;
                string? converterInstatiationLogic = null;
                bool implementsIJsonOnSerialized = false;
                bool implementsIJsonOnSerializing = false;

                IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(type);
                foreach (CustomAttributeData attributeData in attributeDataList)
                {
                    Type attributeType = attributeData.AttributeType;
                    if (attributeType.FullName == "System.Text.Json.Serialization.JsonNumberHandlingAttribute")
                    {
                        IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                        numberHandling = (JsonNumberHandling)ctorArgs[0].Value;
                        continue;
                    }
                    else if (!foundDesignTimeCustomConverter && attributeType.GetCompatibleBaseClass(JsonConverterAttributeFullName) != null)
                    {
                        foundDesignTimeCustomConverter = true;
                        converterInstatiationLogic = GetConverterInstantiationLogic(attributeData);
                    }
                }

                if (type.Name.StartsWith("StackWrapper"))
                {
                }

                if (type.GetConstructor(Type.EmptyTypes) != null && !type.IsAbstract && !type.IsInterface)
                {
                    constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                }

                if (foundDesignTimeCustomConverter)
                {
                    classType = converterInstatiationLogic != null
                        ? ClassType.TypeWithDesignTimeProvidedCustomConverter
                        : ClassType.TypeUnsupportedBySourceGen;
                }
                else if (_knownTypes.Contains(type))
                {
                    classType = ClassType.KnownType;
                }
                else if (type.IsNullableValueType(_nullableOfTType, out Type? nullableUnderlyingType))
                {
                    Debug.Assert(nullableUnderlyingType != null);
                    classType = ClassType.Nullable;
                    nullableUnderlyingTypeGenSpec = GetOrAddTypeGenerationSpec(nullableUnderlyingType, generationMode);
                    _nullableTypeGenerationSpecCache.Add(nullableUnderlyingTypeGenSpec);
                }
                else if (type.IsEnum)
                {
                    classType = ClassType.Enum;
                }
                else if (_ienumerableType.IsAssignableFrom(type))
                {
                    Type actualTypeToConvert;

                    if (type.IsArray)
                    {
                        classType = type.GetArrayRank() > 1
                            ? ClassType.TypeUnsupportedBySourceGen // Multi-dimentional arrays are not supported in STJ.
                            : ClassType.Enumerable;
                        collectionType = CollectionType.Array;
                        collectionValueType = type.GetElementType();
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _listOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.List;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _dictionaryType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.Dictionary;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        collectionKeyType = genericArgs[0];
                        collectionValueType = genericArgs[1];
                    }
                    else if (type.IsImmutableDictionaryType(sourceGenType: true))
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.ImmutableDictionary;

                        Type[] genericArgs = type.GetGenericArguments();
                        collectionKeyType = genericArgs[0];
                        collectionValueType = genericArgs[1];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_idictionaryOfTKeyTValueType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionaryOfTKeyTValue;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        collectionKeyType = genericArgs[0];
                        collectionValueType = genericArgs[1];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ireadonlyDictionaryType)) != null)
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IReadOnlyDictionary;

                        Type[] genericArgs = actualTypeToConvert.GetGenericArguments();
                        collectionKeyType = genericArgs[0];
                        collectionValueType = genericArgs[1];
                    }
                    else if (type.IsImmutableEnumerableType(sourceGenType: true))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ImmutableEnumerable;
                        collectionValueType = type.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ilistOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IListOfT;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_isetType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ISet;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_icollectionOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ICollectionOfT;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _stackOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.StackOfT;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = GetCompatibleGenericBaseClass(type, _queueOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.QueueOfT;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseClass(_concurrentStackType, _objectType, sourceGenType: true)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentStack;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericBaseClass(_concurrentQueueType, _objectType, sourceGenType: true)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.ConcurrentQueue;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if ((actualTypeToConvert = type.GetCompatibleGenericInterface(_ienumerableOfTType)) != null)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerableOfT;
                        collectionValueType = actualTypeToConvert.GetGenericArguments()[0];
                    }
                    else if (_idictionaryType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Dictionary;
                        collectionType = CollectionType.IDictionary;
                        collectionKeyType = _stringType;
                        collectionValueType = _objectType;
                    }
                    else if (_ilistType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IList;
                        collectionValueType = _objectType;
                    }
                    else if (_stackType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Stack;
                        collectionValueType = _objectType;
                    }
                    else if (_queueType.IsAssignableFrom(type))
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Queue;
                        collectionValueType = _objectType;
                    }
                    else
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.IEnumerable;
                        collectionValueType = _objectType;
                    }
                }
                else
                {
                    classType = ClassType.Object;

                    // GetInterface() is currently not implemented, so we use GetInterfaces().
                    IEnumerable<string> interfaces = type.GetInterfaces().Select(interfaceType => interfaceType.FullName!);
                    implementsIJsonOnSerialized = interfaces.FirstOrDefault(interfaceName => interfaceName == IJsonOnSerializedFullName) != null;
                    implementsIJsonOnSerializing = interfaces.FirstOrDefault(interfaceName => interfaceName == IJsonOnSerializingFullName) != null;

                    propGenSpecList = new List<PropertyGenerationSpec>();
                    Dictionary<string, PropertyGenerationSpec>? ignoredMembers = null;

                    const BindingFlags bindingFlags =
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly;

                    bool propertyOrderSpecified = false;

                    for (Type? currentType = type; currentType != null; currentType = currentType.BaseType)
                    {
                        foreach (PropertyInfo propertyInfo in currentType.GetProperties(bindingFlags))
                        {
                            bool isVirtual = propertyInfo.IsVirtual();

                            if (propertyInfo.GetIndexParameters().Length > 0 ||
                                PropertyIsOverridenAndIgnored(propertyInfo.Name, propertyInfo.PropertyType, isVirtual, ignoredMembers))
                            {
                                continue;
                            }

                            PropertyGenerationSpec spec = GetPropertyGenerationSpec(propertyInfo, isVirtual, generationMode);
                            CacheMember(spec, ref propGenSpecList, ref ignoredMembers);
                            propertyOrderSpecified |= spec.Order != 0;
                        }

                        foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                        {
                            if (PropertyIsOverridenAndIgnored(fieldInfo.Name, fieldInfo.FieldType, currentMemberIsVirtual: false, ignoredMembers))
                            {
                                continue;
                            }

                            PropertyGenerationSpec spec = GetPropertyGenerationSpec(fieldInfo, isVirtual: false, generationMode);
                            CacheMember(spec, ref propGenSpecList, ref ignoredMembers);
                            propertyOrderSpecified |= spec.Order != 0;
                        }
                    }

                    if (propertyOrderSpecified)
                    {
                        propGenSpecList.Sort((p1, p2) => p1.Order.CompareTo(p2.Order));
                    }
                }

                typeMetadata.Initialize(
                    generationMode,
                    type,
                    classType,
                    numberHandling,
                    propGenSpecList,
                    collectionType,
                    collectionKeyTypeMetadata: collectionKeyType != null ? GetOrAddTypeGenerationSpec(collectionKeyType, generationMode) : null,
                    collectionValueTypeMetadata: collectionValueType != null ? GetOrAddTypeGenerationSpec(collectionValueType, generationMode) : null,
                    constructionStrategy,
                    nullableUnderlyingTypeMetadata: nullableUnderlyingTypeGenSpec,
                    converterInstatiationLogic,
                    implementsIJsonOnSerialized,
                    implementsIJsonOnSerializing);

                return typeMetadata;
            }

            private Type GetCompatibleGenericBaseClass(Type type, Type baseType)
                => type.GetCompatibleGenericBaseClass(baseType, _objectType);

            private void CacheMember(
                PropertyGenerationSpec propGenSpec,
                ref List<PropertyGenerationSpec> propGenSpecList,
                ref Dictionary<string, PropertyGenerationSpec> ignoredMembers)
            {
                propGenSpecList.Add(propGenSpec);

                if (propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                {
                    ignoredMembers ??= new Dictionary<string, PropertyGenerationSpec>();
                    ignoredMembers.Add(propGenSpec.ClrName, propGenSpec);
                }
            }

            private static bool PropertyIsOverridenAndIgnored(
                string currentMemberName,
                Type currentMemberType,
                bool currentMemberIsVirtual,
                Dictionary<string, PropertyGenerationSpec>? ignoredMembers)
            {
                if (ignoredMembers == null || !ignoredMembers.TryGetValue(currentMemberName, out PropertyGenerationSpec? ignoredMember))
                {
                    return false;
                }

                return currentMemberType == ignoredMember.TypeGenerationSpec.Type &&
                    currentMemberIsVirtual &&
                    ignoredMember.IsVirtual;
            }

            private PropertyGenerationSpec GetPropertyGenerationSpec(MemberInfo memberInfo, bool isVirtual, JsonSourceGenerationMode generationMode)
            {
                IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(memberInfo);

                bool hasJsonInclude = false;
                JsonIgnoreCondition? ignoreCondition = null;
                JsonNumberHandling? numberHandling = null;
                string? jsonPropertyName = null;

                bool foundDesignTimeCustomConverter = false;
                string? converterInstantiationLogic = null;
                int order = 0;

                foreach (CustomAttributeData attributeData in attributeDataList)
                {
                    Type attributeType = attributeData.AttributeType;

                    if (!foundDesignTimeCustomConverter && attributeType.GetCompatibleBaseClass(JsonConverterAttributeFullName) != null)
                    {
                        foundDesignTimeCustomConverter = true;
                        converterInstantiationLogic = GetConverterInstantiationLogic(attributeData);
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
                                        ignoreCondition = (JsonIgnoreCondition)namedArgs[0].TypedValue.Value;
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
                                    numberHandling = (JsonNumberHandling)ctorArgs[0].Value;
                                }
                                break;
                            case JsonPropertyNameAttributeFullName:
                                {
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                                    jsonPropertyName = (string)ctorArgs[0].Value;
                                    // Null check here is done at runtime within JsonSerializer.
                                }
                                break;
                            case JsonPropertyOrderAttributeFullName:
                                {
                                    IList<CustomAttributeTypedArgument> ctorArgs = attributeData.ConstructorArguments;
                                    order = (int)ctorArgs[0].Value;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                Type memberCLRType;
                bool isReadOnly;
                bool isPublic = false;
                bool canUseGetter = false;
                bool canUseSetter = false;
                bool getterIsVirtual = false;
                bool setterIsVirtual = false;

                switch (memberInfo)
                {
                    case PropertyInfo propertyInfo:
                        {
                            memberCLRType = propertyInfo.PropertyType;

                            MethodInfo? getMethod = propertyInfo.GetMethod;
                            MethodInfo? setMethod = propertyInfo.SetMethod;

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

                                if (setMethod.IsPublic)
                                {
                                    isPublic = true;
                                    canUseSetter = !setMethod.IsInitOnly();
                                }
                                else if (setMethod.IsAssembly)
                                {
                                    canUseSetter = hasJsonInclude && !setMethod.IsInitOnly();
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
                            memberCLRType = fieldInfo.FieldType;
                            isPublic = fieldInfo.IsPublic;
                            isReadOnly = fieldInfo.IsInitOnly;

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

                string clrName = memberInfo.Name;

                return new PropertyGenerationSpec
                {
                    ClrName = clrName,
                    IsProperty = memberInfo.MemberType == MemberTypes.Property,
                    IsPublic = isPublic,
                    IsVirtual = isVirtual,
                    JsonPropertyName = jsonPropertyName,
                    RuntimePropertyName = DetermineRuntimePropName(clrName, jsonPropertyName, _currentContextNamingPolicy),
                    IsReadOnly = isReadOnly,
                    CanUseGetter = canUseGetter,
                    CanUseSetter = canUseSetter,
                    GetterIsVirtual = getterIsVirtual,
                    SetterIsVirtual = setterIsVirtual,
                    DefaultIgnoreCondition = ignoreCondition,
                    NumberHandling = numberHandling,
                    Order = order,
                    HasJsonInclude = hasJsonInclude,
                    TypeGenerationSpec = GetOrAddTypeGenerationSpec(memberCLRType, generationMode),
                    DeclaringTypeRef = memberInfo.DeclaringType.GetCompilableName(),
                    ConverterInstantiationLogic = converterInstantiationLogic
                };
            }

            private static bool PropertyAccessorCanBeReferenced(MethodInfo? accessor)
                => accessor != null && (accessor.IsPublic || accessor.IsAssembly);

            private string? GetConverterInstantiationLogic(CustomAttributeData attributeData)
            {
                if (attributeData.AttributeType.FullName != JsonConverterAttributeFullName)
                {
                    return null;
                }

                ITypeSymbol converterTypeSymbol = (ITypeSymbol)attributeData.ConstructorArguments[0].Value;
                Type converterType = converterTypeSymbol.AsType(_metadataLoadContext);

                if (converterType == null || converterType.GetConstructor(Type.EmptyTypes) == null || converterType.IsNestedPrivate)
                {
                    return null;
                }

                return $"new {converterType.GetCompilableName()}()";
            }

            private static string DetermineRuntimePropName(string clrPropName, string? jsonPropName, JsonKnownNamingPolicy namingPolicy)
            {
                string runtimePropName;

                if (jsonPropName != null)
                {
                    runtimePropName = jsonPropName;
                }
                else if (namingPolicy == JsonKnownNamingPolicy.CamelCase)
                {
                    runtimePropName = JsonNamingPolicy.CamelCase.ConvertName(clrPropName);
                }
                else
                {
                    runtimePropName = clrPropName;
                }

                return runtimePropName;
            }

            private void PopulateNumberTypes()
            {
                Debug.Assert(_numberTypes != null);
                _numberTypes.Add(ResolveType(SpecialType.System_Byte));
                _numberTypes.Add(ResolveType(SpecialType.System_Decimal));
                _numberTypes.Add(ResolveType(SpecialType.System_Double));
                _numberTypes.Add(ResolveType(SpecialType.System_Int16));
                _numberTypes.Add(ResolveType(SpecialType.System_SByte));
                _numberTypes.Add(ResolveType(SpecialType.System_Int32));
                _numberTypes.Add(ResolveType(SpecialType.System_Int64));
                _numberTypes.Add(ResolveType(SpecialType.System_Single));
                _numberTypes.Add(ResolveType(SpecialType.System_UInt64));
                _numberTypes.Add(ResolveType(SpecialType.System_UInt32));
                _numberTypes.Add(ResolveType(SpecialType.System_UInt64));
            }

            private void PopulateKnownTypes()
            {
                PopulateNumberTypes();
                Debug.Assert(_knownTypes != null);
                Debug.Assert(_numberTypes != null);

                _knownTypes.UnionWith(_numberTypes);
                _knownTypes.Add(_booleanType);
                _knownTypes.Add(_byteArrayType);
                _knownTypes.Add(_charType);
                _knownTypes.Add(_dateTimeType);
                _knownTypes.Add(_dateTimeOffsetType);
                _knownTypes.Add(_guidType);
                _knownTypes.Add(_objectType);
                _knownTypes.Add(_stringType);
                _knownTypes.Add(_uriType);
                _knownTypes.Add(_versionType);
                _knownTypes.Add(_jsonElementType);
            }

            private Type ResolveType(string fullyQualifiedMetadataName)
            {
                INamedTypeSymbol? typeSymbol = _executionContext.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
                return typeSymbol.AsType(_metadataLoadContext);
            }

            private Type ResolveType(SpecialType specialType)
            {
                INamedTypeSymbol? typeSymbol = _executionContext.Compilation.GetSpecialType(specialType);
                return typeSymbol.AsType(_metadataLoadContext);
            }
        }
    }
}
