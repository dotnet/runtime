// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text.Json.Serialization;
using System.Text.Json.SourceGeneration.Reflection;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed class Parser
        {
            private const string SystemTextJsonNamespace = "System.Text.Json";

            private const string JsonConverterAttributeFullName = "System.Text.Json.Serialization.JsonConverterAttribute";

            private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";

            private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";

            private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";

            private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";

            private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";

            private readonly GeneratorExecutionContext _executionContext;

            private readonly MetadataLoadContextInternal _metadataLoadContext;

            private readonly Type _ienumerableType;
            private readonly Type _listOfTType;
            private readonly Type _dictionaryType;

            private readonly Type _booleanType;
            private readonly Type _byteArrayType;
            private readonly Type _charType;
            private readonly Type _dateTimeType;
            private readonly Type _dateTimeOffsetType;
            private readonly Type _guidType;
            private readonly Type _nullableOfTType;
            private readonly Type _stringType;
            private readonly Type _uriType;
            private readonly Type _versionType;

            private readonly HashSet<Type> _numberTypes = new();

            private readonly HashSet<Type> _knownTypes = new();

            /// <summary>
            /// Type information for member types in input object graphs.
            /// </summary>
            private readonly Dictionary<Type, TypeGenerationSpec> _typeGenerationSpecCache = new();

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

                _ienumerableType = _metadataLoadContext.Resolve(typeof(IEnumerable));
                _listOfTType = _metadataLoadContext.Resolve(typeof(List<>));
                _dictionaryType = _metadataLoadContext.Resolve(typeof(Dictionary<,>));

                _booleanType = _metadataLoadContext.Resolve(typeof(bool));
                _byteArrayType = _metadataLoadContext.Resolve(typeof(byte[]));
                _charType = _metadataLoadContext.Resolve(typeof(char));
                _dateTimeType = _metadataLoadContext.Resolve(typeof(DateTime));
                _dateTimeOffsetType = _metadataLoadContext.Resolve(typeof(DateTimeOffset));
                _guidType = _metadataLoadContext.Resolve(typeof(Guid));
                _nullableOfTType = _metadataLoadContext.Resolve(typeof(Nullable<>));
                _stringType = _metadataLoadContext.Resolve(typeof(string));
                _uriType = _metadataLoadContext.Resolve(typeof(Uri));
                _versionType = _metadataLoadContext.Resolve(typeof(Version));

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

                    foreach(AttributeSyntax attribute in serializableAttributeList)
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

                    contextGenSpecList ??= new List<ContextGenerationSpec>();
                    contextGenSpecList.Add(contextGenSpec);

                    // Clear the cache of generated metadata between the processing of context classes.
                    _typeGenerationSpecCache.Clear();
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

            private static bool TryGetClassDeclarationList(INamedTypeSymbol typeSymbol, [NotNullWhenAttribute(true)] out List<string>? classDeclarationList)
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
                typeMetadata = new();
                _typeGenerationSpecCache[type] = typeMetadata;

                ClassType classType;
                Type? collectionKeyType = null;
                Type? collectionValueType = null;
                Type? nullableUnderlyingType = null;
                List<PropertyGenerationSpec>? propertiesMetadata = null;
                CollectionType collectionType = CollectionType.NotApplicable;
                ObjectConstructionStrategy constructionStrategy = default;
                JsonNumberHandling? numberHandling = null;

                bool foundDesignTimeCustomConverter = false;
                string? converterInstatiationLogic = null;

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
                else if (type.IsNullableValueType(_nullableOfTType, out nullableUnderlyingType))
                {
                    Debug.Assert(nullableUnderlyingType != null);
                    classType = ClassType.Nullable;
                }
                else if (type.IsEnum)
                {
                    classType = ClassType.Enum;
                }
                else if (_ienumerableType.IsAssignableFrom(type))
                {
                    // Only T[], List<T>, and Dictionary<Tkey, TValue> are supported.

                    if (type.IsArray)
                    {
                        classType = ClassType.Enumerable;
                        collectionType = CollectionType.Array;
                        collectionValueType = type.GetElementType();
                    }
                    else if (!type.IsGenericType)
                    {
                        classType = ClassType.TypeUnsupportedBySourceGen;
                    }
                    else
                    {
                        Type genericTypeDef = type.GetGenericTypeDefinition();
                        Type[] genericTypeArgs = type.GetGenericArguments();

                        if (genericTypeDef == _listOfTType)
                        {
                            classType = ClassType.Enumerable;
                            collectionType = CollectionType.List;
                            collectionValueType = genericTypeArgs[0];
                        }
                        else if (genericTypeDef == _dictionaryType)
                        {
                            classType = ClassType.Dictionary;
                            collectionType = CollectionType.Dictionary;
                            collectionKeyType = genericTypeArgs[0];
                            collectionValueType = genericTypeArgs[1];
                        }
                        else
                        {
                            classType = ClassType.TypeUnsupportedBySourceGen;
                        }
                    }
                }
                else
                {
                    classType = ClassType.Object;

                    if (type.GetConstructor(Type.EmptyTypes) != null && !type.IsAbstract && !type.IsInterface)
                    {
                        constructionStrategy = ObjectConstructionStrategy.ParameterlessConstructor;
                    }

                    for (Type? currentType = type; currentType != null; currentType = currentType.BaseType)
                    {
                        const BindingFlags bindingFlags =
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly;

                        foreach (PropertyInfo propertyInfo in currentType.GetProperties(bindingFlags))
                        {
                            PropertyGenerationSpec metadata = GetPropertyGenerationSpec(propertyInfo, generationMode);

                            // Ignore indexers.
                            if (propertyInfo.GetIndexParameters().Length > 0)
                            {
                                continue;
                            }

                            if (metadata.CanUseGetter || metadata.CanUseSetter)
                            {
                                (propertiesMetadata ??= new()).Add(metadata);
                            }
                        }

                        foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                        {
                            PropertyGenerationSpec metadata = GetPropertyGenerationSpec(fieldInfo, generationMode);

                            if (metadata.CanUseGetter || metadata.CanUseSetter)
                            {
                                (propertiesMetadata ??= new()).Add(metadata);
                            }
                        }
                    }
                }

                typeMetadata.Initialize(
                    generationMode,
                    typeRef: type.GetUniqueCompilableTypeName(),
                    typeInfoPropertyName: type.GetFriendlyTypeName(),
                    type,
                    classType,
                    isValueType: type.IsValueType,
                    numberHandling,
                    propertiesMetadata,
                    collectionType,
                    collectionKeyTypeMetadata: collectionKeyType != null ? GetOrAddTypeGenerationSpec(collectionKeyType, generationMode) : null,
                    collectionValueTypeMetadata: collectionValueType != null ? GetOrAddTypeGenerationSpec(collectionValueType, generationMode) : null,
                    constructionStrategy,
                    nullableUnderlyingTypeMetadata: nullableUnderlyingType != null ? GetOrAddTypeGenerationSpec(nullableUnderlyingType, generationMode) : null,
                    converterInstatiationLogic);

                return typeMetadata;
            }

            private PropertyGenerationSpec GetPropertyGenerationSpec(MemberInfo memberInfo, JsonSourceGenerationMode generationMode)
            {
                IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(memberInfo);

                bool hasJsonInclude = false;
                JsonIgnoreCondition? ignoreCondition = null;
                JsonNumberHandling? numberHandling = null;
                string? jsonPropertyName = null;

                bool foundDesignTimeCustomConverter = false;
                string? converterInstantiationLogic = null;

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
                            default:
                                break;
                        }
                    }
                }

                Type memberCLRType;
                bool isReadOnly;
                bool canUseGetter;
                bool canUseSetter;
                bool getterIsVirtual = false;
                bool setterIsVirtual = false;

                switch (memberInfo)
                {
                    case PropertyInfo propertyInfo:
                        {
                            MethodInfo setMethod = propertyInfo.SetMethod;
                            memberCLRType = propertyInfo.PropertyType;
                            isReadOnly = setMethod == null;
                            canUseGetter = PropertyAccessorCanBeReferenced(propertyInfo.GetMethod, hasJsonInclude);
                            canUseSetter = PropertyAccessorCanBeReferenced(setMethod, hasJsonInclude) && !setMethod.IsInitOnly();
                            getterIsVirtual = propertyInfo.GetMethod?.IsVirtual == true;
                            setterIsVirtual = propertyInfo.SetMethod?.IsVirtual == true;
                        }
                        break;
                    case FieldInfo fieldInfo:
                        {
                            Debug.Assert(fieldInfo.IsPublic);
                            memberCLRType = fieldInfo.FieldType;
                            isReadOnly = fieldInfo.IsInitOnly;
                            canUseGetter = true;
                            canUseSetter = !isReadOnly;
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                return new PropertyGenerationSpec
                {
                    ClrName = memberInfo.Name,
                    IsProperty = memberInfo.MemberType == MemberTypes.Property,
                    JsonPropertyName = jsonPropertyName,
                    IsReadOnly = isReadOnly,
                    CanUseGetter = canUseGetter,
                    CanUseSetter = canUseSetter,
                    GetterIsVirtual = getterIsVirtual,
                    SetterIsVirtual = setterIsVirtual,
                    DefaultIgnoreCondition = ignoreCondition,
                    NumberHandling = numberHandling,
                    HasJsonInclude = hasJsonInclude,
                    TypeGenerationSpec = GetOrAddTypeGenerationSpec(memberCLRType, generationMode),
                    DeclaringTypeRef = $"global::{memberInfo.DeclaringType.GetUniqueCompilableTypeName()}",
                    ConverterInstantiationLogic = converterInstantiationLogic
                };
            }

            private static bool PropertyAccessorCanBeReferenced(MethodInfo? memberAccessor, bool hasJsonInclude) =>
                (memberAccessor != null && !memberAccessor.IsPrivate) && (memberAccessor.IsPublic || hasJsonInclude);

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

                return $"new {converterType.GetUniqueCompilableTypeName()}()";
            }

            private void PopulateNumberTypes()
            {
                Debug.Assert(_numberTypes != null);
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(byte)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(decimal)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(double)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(short)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(sbyte)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(int)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(long)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(float)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(ushort)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(uint)));
                _numberTypes.Add(_metadataLoadContext.Resolve(typeof(ulong)));
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
                _knownTypes.Add(_metadataLoadContext.Resolve(typeof(object)));
                _knownTypes.Add(_stringType);

                // System.Private.Uri may not be loaded in input compilation.
                if (_uriType != null)
                {
                    _knownTypes.Add(_uriType);
                }

                _knownTypes.Add(_metadataLoadContext.Resolve(typeof(Version)));
            }
        }
    }
}
