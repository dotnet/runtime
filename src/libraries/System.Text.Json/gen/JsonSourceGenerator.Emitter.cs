// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed partial class Emitter
        {
            // Literals in generated source
            private const string RuntimeCustomConverterFetchingMethodName = "GetRuntimeProvidedCustomConverter";
            private const string OptionsInstanceVariableName = "Options";
            private const string PropInitMethodNameSuffix = "PropInit";
            private const string CtorParamInitMethodNameSuffix = "CtorParamInit";
            private const string SerializeMethodNameSuffix = "Serialize";
            private const string CreateValueInfoMethodName = "CreateValueInfo";
            private const string DefaultOptionsStaticVarName = "s_defaultOptions";
            private const string DefaultContextBackingStaticVarName = "s_defaultContext";
            private const string WriterVarName = "writer";
            private const string ValueVarName = "value";
            private const string JsonSerializerContextName = "JsonSerializerContext";

            private static AssemblyName _assemblyName = typeof(Emitter).Assembly.GetName();
            private static readonly string s_generatedCodeAttributeSource = $@"
[global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{_assemblyName.Name}"", ""{_assemblyName.Version}"")]
";

            // global::fully.qualified.name for referenced types
            private const string ArrayTypeRef = "global::System.Array";
            private const string InvalidOperationExceptionTypeRef = "global::System.InvalidOperationException";
            private const string TypeTypeRef = "global::System.Type";
            private const string UnsafeTypeRef = "global::System.Runtime.CompilerServices.Unsafe";
            private const string NullableTypeRef = "global::System.Nullable";
            private const string EqualityComparerTypeRef = "global::System.Collections.Generic.EqualityComparer";
            private const string IListTypeRef = "global::System.Collections.Generic.IList";
            private const string KeyValuePairTypeRef = "global::System.Collections.Generic.KeyValuePair";
            private const string ListTypeRef = "global::System.Collections.Generic.List";
            private const string DictionaryTypeRef = "global::System.Collections.Generic.Dictionary";
            private const string JsonEncodedTextTypeRef = "global::System.Text.Json.JsonEncodedText";
            private const string JsonNamingPolicyTypeRef = "global::System.Text.Json.JsonNamingPolicy";
            private const string JsonSerializerTypeRef = "global::System.Text.Json.JsonSerializer";
            private const string JsonSerializerOptionsTypeRef = "global::System.Text.Json.JsonSerializerOptions";
            private const string Utf8JsonWriterTypeRef = "global::System.Text.Json.Utf8JsonWriter";
            private const string JsonConverterTypeRef = "global::System.Text.Json.Serialization.JsonConverter";
            private const string JsonConverterFactoryTypeRef = "global::System.Text.Json.Serialization.JsonConverterFactory";
            private const string JsonIgnoreConditionTypeRef = "global::System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonNumberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonNumberHandling";
            private const string JsonSerializerContextTypeRef = "global::System.Text.Json.Serialization.JsonSerializerContext";
            private const string JsonMetadataServicesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonMetadataServices";
            private const string JsonObjectInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues";
            private const string JsonParameterInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonParameterInfoValues";
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";

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

            private readonly GeneratorExecutionContext _executionContext;

            private ContextGenerationSpec _currentContext = null!;

            private readonly SourceGenerationSpec _generationSpec = null!;

            public Emitter(in GeneratorExecutionContext executionContext, SourceGenerationSpec generationSpec)
            {
                _executionContext = executionContext;
                _generationSpec = generationSpec;
            }

            public void Emit()
            {
                foreach (ContextGenerationSpec contextGenerationSpec in _generationSpec.ContextGenerationSpecList)
                {
                    _currentContext = contextGenerationSpec;

                    foreach (TypeGenerationSpec typeGenerationSpec in _currentContext.RootSerializableTypes)
                    {
                        GenerateTypeInfo(typeGenerationSpec);
                    }

                    string contextName = _currentContext.ContextType.Name;

                    // Add root context implementation.
                    AddSource($"{contextName}.g.cs", GetRootJsonContextImplementation(), isRootContextDef: true);

                    // Add GetJsonTypeInfo override implementation.
                    AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation());

                    // Add property name initialization.
                    AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization());
                }
            }

            private void AddSource(string fileName, string source, bool isRootContextDef = false)
            {
                string? generatedCodeAttributeSource = isRootContextDef ? s_generatedCodeAttributeSource : null;

                List<string> declarationList = _currentContext.ContextClassDeclarationList;
                int declarationCount = declarationList.Count;
                Debug.Assert(declarationCount >= 1);

                string @namespace = _currentContext.ContextType.Namespace;
                bool isInGlobalNamespace = @namespace == JsonConstants.GlobalNamespaceValue;

                StringBuilder sb = new("// <auto-generated/>");

                if (!isInGlobalNamespace)
                {
                    sb.Append(@$"

namespace {@namespace}
{{");
                }

                for (int i = 0; i < declarationCount - 1; i++)
                {
                    string declarationSource = $@"
{declarationList[declarationCount - 1 - i]}
{{";
                    sb.Append($@"
{IndentSource(declarationSource, numIndentations: i + 1)}
");
                }

                // Add the core implementation for the derived context class.
                string partialContextImplementation = $@"
{generatedCodeAttributeSource}{declarationList[0]}
{{
    {IndentSource(source, Math.Max(1, declarationCount - 1))}
}}";
                sb.AppendLine(IndentSource(partialContextImplementation, numIndentations: declarationCount));

                // Match curly brace for each containing type.
                for (int i = 0; i < declarationCount - 1; i++)
                {
                    sb.AppendLine(IndentSource("}", numIndentations: declarationCount + i + 1));
                }

                if (!isInGlobalNamespace)
                {
                    sb.AppendLine("}");
                }

                _executionContext.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            private void GenerateTypeInfo(TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec != null);

                HashSet<TypeGenerationSpec> typesWithMetadata = _currentContext.TypesWithMetadataGenerated;

                if (typesWithMetadata.Contains(typeGenerationSpec))
                {
                    return;
                }

                typesWithMetadata.Add(typeGenerationSpec);

                string source;

                switch (typeGenerationSpec.ClassType)
                {
                    case ClassType.KnownType:
                        {
                            source = GenerateForTypeWithKnownConverter(typeGenerationSpec);
                        }
                        break;
                    case ClassType.TypeWithDesignTimeProvidedCustomConverter:
                        {
                            source = GenerateForTypeWithUnknownConverter(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Nullable:
                        {
                            source = GenerateForNullable(typeGenerationSpec);

                            GenerateTypeInfo(typeGenerationSpec.NullableUnderlyingTypeMetadata);
                        }
                        break;
                    case ClassType.Enum:
                        {
                            source = GenerateForEnum(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Enumerable:
                        {
                            source = GenerateForCollection(typeGenerationSpec);

                            GenerateTypeInfo(typeGenerationSpec.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Dictionary:
                        {
                            source = GenerateForCollection(typeGenerationSpec);

                            GenerateTypeInfo(typeGenerationSpec.CollectionKeyTypeMetadata);
                            GenerateTypeInfo(typeGenerationSpec.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Object:
                        {
                            source = GenerateForObject(typeGenerationSpec);

                            foreach (PropertyGenerationSpec spec in typeGenerationSpec.PropertyGenSpecList)
                            {
                                GenerateTypeInfo(spec.TypeGenerationSpec);
                            }

                            if (typeGenerationSpec.ConstructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor)
                            {
                                foreach (ParameterGenerationSpec spec in typeGenerationSpec.CtorParamGenSpecArray!)
                                {
                                    GenerateTypeInfo(spec.TypeGenerationSpec);
                                }
                            }
                        }
                        break;
                    case ClassType.TypeUnsupportedBySourceGen:
                        {
                            _executionContext.ReportDiagnostic(
                                Diagnostic.Create(TypeNotSupported, Location.None, new string[] { typeGenerationSpec.TypeRef }));
                            return;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }

                try
                {
                    AddSource($"{_currentContext.ContextType.Name}.{typeGenerationSpec.TypeInfoPropertyName}.g.cs", source);
                }
                catch (ArgumentException)
                {
                    _executionContext.ReportDiagnostic(Diagnostic.Create(DuplicateTypeName, Location.None, new string[] { typeGenerationSpec.TypeInfoPropertyName }));
                }
            }

            private string GenerateForTypeWithKnownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $@"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, {JsonMetadataServicesTypeRef}.{typeFriendlyName}Converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForTypeWithUnknownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                StringBuilder sb = new();

                // TODO (https://github.com/dotnet/runtime/issues/52218): consider moving this verification source to common helper.
                string metadataInitSource = $@"{JsonConverterTypeRef} converter = {typeMetadata.ConverterInstantiationLogic};
                    {TypeTypeRef} typeToConvert = typeof({typeCompilableName});
                    if (!converter.CanConvert(typeToConvert))
                    {{
                        {TypeTypeRef} underlyingType = {NullableTypeRef}.GetUnderlyingType(typeToConvert);
                        if (underlyingType != null && converter.CanConvert(underlyingType))
                        {{
                            {JsonConverterTypeRef} actualConverter = converter;

                            if (converter is {JsonConverterFactoryTypeRef} converterFactory)
                            {{
                                actualConverter = converterFactory.CreateConverter(underlyingType, {OptionsInstanceVariableName});

                                if (actualConverter == null || actualConverter is {JsonConverterFactoryTypeRef})
                                {{
                                    throw new {InvalidOperationExceptionTypeRef}($""JsonConverterFactory '{{converter}} cannot return a 'null' or 'JsonConverterFactory' value."");
                                }}
                            }}

                            // Allow nullable handling to forward to the underlying type's converter.
                            converter = {JsonMetadataServicesTypeRef}.GetNullableConverter<{typeCompilableName}>(this.{typeFriendlyName});
                        }}
                        else
                        {{
                            throw new {InvalidOperationExceptionTypeRef}($""The converter '{{converter.GetType()}}' is not compatible with the type '{{typeToConvert}}'."");
                        }}
                    }}

                    _{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForNullable(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                TypeGenerationSpec? underlyingTypeMetadata = typeMetadata.NullableUnderlyingTypeMetadata;
                Debug.Assert(underlyingTypeMetadata != null);
                string underlyingTypeCompilableName = underlyingTypeMetadata.TypeRef;
                string underlyingTypeFriendlyName = underlyingTypeMetadata.TypeInfoPropertyName;
                string underlyingTypeInfoNamedArg = underlyingTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                    ? "underlyingTypeInfo: null"
                    : $"underlyingTypeInfo: {underlyingTypeFriendlyName}";

                string metadataInitSource = @$"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}(
                        {OptionsInstanceVariableName},
                        {JsonMetadataServicesTypeRef}.GetNullableConverter<{underlyingTypeCompilableName}>({underlyingTypeInfoNamedArg}));
";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForEnum(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, {JsonMetadataServicesTypeRef}.GetEnumConverter<{typeCompilableName}>({OptionsInstanceVariableName}));";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForCollection(TypeGenerationSpec typeGenerationSpec)
            {
                // Key metadata
                TypeGenerationSpec? collectionKeyTypeMetadata = typeGenerationSpec.CollectionKeyTypeMetadata;
                Debug.Assert(!(typeGenerationSpec.ClassType == ClassType.Dictionary && collectionKeyTypeMetadata == null));
                string? keyTypeCompilableName = collectionKeyTypeMetadata?.TypeRef;
                string? keyTypeReadableName = collectionKeyTypeMetadata?.TypeInfoPropertyName;

                string? keyTypeMetadataPropertyName;
                if (typeGenerationSpec.ClassType != ClassType.Dictionary)
                {
                    keyTypeMetadataPropertyName = "null";
                }
                else
                {
                    keyTypeMetadataPropertyName = collectionKeyTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                        ? "null"
                        : $"this.{keyTypeReadableName}";
                }

                // Value metadata
                TypeGenerationSpec? collectionValueTypeMetadata = typeGenerationSpec.CollectionValueTypeMetadata;
                Debug.Assert(collectionValueTypeMetadata != null);
                string valueTypeCompilableName = collectionValueTypeMetadata.TypeRef;
                string valueTypeReadableName = collectionValueTypeMetadata.TypeInfoPropertyName;

                string valueTypeMetadataPropertyName = collectionValueTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                    ? "null"
                    : $"this.{valueTypeReadableName}";

                string numberHandlingArg = $"{GetNumberHandlingAsStr(typeGenerationSpec.NumberHandling)}";

                string serializeFuncNamedArg;

                string? serializeFuncSource;
                if (!typeGenerationSpec.GenerateSerializationLogic)
                {
                    serializeFuncSource = null;
                    serializeFuncNamedArg = "serializeFunc: null";
                }
                else
                {
                    serializeFuncSource = typeGenerationSpec.ClassType == ClassType.Enumerable
                        ? GenerateFastPathFuncForEnumerable(typeGenerationSpec)
                        : GenerateFastPathFuncForDictionary(typeGenerationSpec);

                    serializeFuncNamedArg = $"serializeFunc: {typeGenerationSpec.FastPathSerializeMethodName}";
                }

                CollectionType collectionType = typeGenerationSpec.CollectionType;

                string typeRef = typeGenerationSpec.TypeRef;
                string createObjectFuncArg = typeGenerationSpec.ConstructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                    ? $"createObjectFunc: () => new {typeRef}()"
                    : "createObjectFunc: null";

                string collectionInfoCreationPrefix = collectionType switch
                {
                    CollectionType.IListOfT => $"{JsonMetadataServicesTypeRef}.CreateIListInfo<",
                    CollectionType.ICollectionOfT => $"{JsonMetadataServicesTypeRef}.CreateICollectionInfo<",
                    CollectionType.StackOfT => $"{JsonMetadataServicesTypeRef}.CreateStackInfo<",
                    CollectionType.QueueOfT => $"{JsonMetadataServicesTypeRef}.CreateQueueInfo<",
                    CollectionType.Stack => $"{JsonMetadataServicesTypeRef}.CreateStackOrQueueInfo<",
                    CollectionType.Queue => $"{JsonMetadataServicesTypeRef}.CreateStackOrQueueInfo<",
                    CollectionType.IEnumerableOfT => $"{JsonMetadataServicesTypeRef}.CreateIEnumerableInfo<",
                    CollectionType.IDictionaryOfTKeyTValue => $"{JsonMetadataServicesTypeRef}.CreateIDictionaryInfo<",
                    _ => $"{JsonMetadataServicesTypeRef}.Create{collectionType}Info<"
                };

                string dictInfoCreationPrefix = $"{collectionInfoCreationPrefix}{typeRef}, {keyTypeCompilableName!}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, {createObjectFuncArg}, {keyTypeMetadataPropertyName!}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg}";
                string enumerableInfoCreationPrefix = $"{collectionInfoCreationPrefix}{typeRef}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, {createObjectFuncArg}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg}";
                string immutableCollectionCreationSuffix = $"createRangeFunc: {typeGenerationSpec.ImmutableCollectionBuilderName}";

                string collectionTypeInfoValue;

                switch (collectionType)
                {
                    case CollectionType.Array:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{valueTypeCompilableName}>({OptionsInstanceVariableName}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})";
                        break;
                    case CollectionType.IEnumerable:
                    case CollectionType.IList:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsInstanceVariableName}, {createObjectFuncArg}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})";
                        break;
                    case CollectionType.Stack:
                    case CollectionType.Queue:
                        string addMethod = collectionType == CollectionType.Stack ? "Push" : "Enqueue";
                        string addFuncNamedArg = $"addFunc: (collection, {ValueVarName}) => collection.{addMethod}({ValueVarName})";
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsInstanceVariableName}, {createObjectFuncArg}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg}, {addFuncNamedArg})";
                        break;
                    case CollectionType.ImmutableEnumerable:
                        collectionTypeInfoValue = $"{enumerableInfoCreationPrefix}, {immutableCollectionCreationSuffix})";
                        break;
                    case CollectionType.IDictionary:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsInstanceVariableName}, {createObjectFuncArg}, {keyTypeMetadataPropertyName!}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})";
                        break;
                    case CollectionType.Dictionary:
                    case CollectionType.IDictionaryOfTKeyTValue:
                    case CollectionType.IReadOnlyDictionary:
                        collectionTypeInfoValue = $"{dictInfoCreationPrefix})";
                        break;
                    case CollectionType.ImmutableDictionary:
                        collectionTypeInfoValue = $"{dictInfoCreationPrefix}, {immutableCollectionCreationSuffix})";
                        break;
                    default:
                        collectionTypeInfoValue = $"{enumerableInfoCreationPrefix})";
                        break;
                }

                string metadataInitSource = @$"_{typeGenerationSpec.TypeInfoPropertyName} = {collectionTypeInfoValue};";

                return GenerateForType(typeGenerationSpec, metadataInitSource, serializeFuncSource);
            }

            private string GenerateFastPathFuncForEnumerable(TypeGenerationSpec typeGenerationSpec)
            {
                TypeGenerationSpec valueTypeGenerationSpec = typeGenerationSpec.CollectionValueTypeMetadata;

                Type elementType = valueTypeGenerationSpec.Type;
                string? writerMethodToCall = GetWriterMethod(elementType);
                
                string iterationLogic;
                string valueToWrite;

                switch (typeGenerationSpec.CollectionType)
                {
                    case CollectionType.Array:
                        iterationLogic = $"for (int i = 0; i < {ValueVarName}.Length; i++)";
                        valueToWrite = $"{ValueVarName}[i]";
                        break;
                    case CollectionType.IListOfT:
                    case CollectionType.List:
                    case CollectionType.IList:
                        iterationLogic = $"for (int i = 0; i < {ValueVarName}.Count; i++)";
                        valueToWrite = $"{ValueVarName}[i]";
                        break;
                    default:
                        const string elementVarName = "element";
                        iterationLogic = $"foreach ({valueTypeGenerationSpec.TypeRef} {elementVarName} in {ValueVarName})";
                        valueToWrite = elementVarName;
                        break;
                };

                if (elementType == _generationSpec.CharType)
                {
                    valueToWrite = $"{valueToWrite}.ToString()";
                }

                string elementSerializationLogic = writerMethodToCall == null
                    ? GetSerializeLogicForNonPrimitiveType(valueTypeGenerationSpec.TypeInfoPropertyName, valueToWrite, valueTypeGenerationSpec.GenerateSerializationLogic)
                    : $"{writerMethodToCall}Value({valueToWrite});";

                string serializationLogic = $@"{WriterVarName}.WriteStartArray();

    {iterationLogic}
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndArray();";

                return GenerateFastPathFuncForType(
                    typeGenerationSpec.FastPathSerializeMethodName,
                    typeGenerationSpec.TypeRef,
                    serializationLogic,
                    typeGenerationSpec.CanBeNull);
            }

            private string GenerateFastPathFuncForDictionary(TypeGenerationSpec typeGenerationSpec)
            {
                TypeGenerationSpec keyTypeGenerationSpec = typeGenerationSpec.CollectionKeyTypeMetadata;
                TypeGenerationSpec valueTypeGenerationSpec = typeGenerationSpec.CollectionValueTypeMetadata;

                Type elementType = valueTypeGenerationSpec.Type;
                string? writerMethodToCall = GetWriterMethod(elementType);
                string elementSerializationLogic;

                const string pairVarName = "pair";
                string keyToWrite = $"{pairVarName}.Key";
                string valueToWrite = $"{pairVarName}.Value";

                if (elementType == _generationSpec.CharType)
                {
                    valueToWrite = $"{valueToWrite}.ToString()";
                }

                if (writerMethodToCall != null)
                {
                    elementSerializationLogic = $"{writerMethodToCall}({keyToWrite}, {valueToWrite});";
                }
                else
                {
                    elementSerializationLogic = $@"{WriterVarName}.WritePropertyName({keyToWrite});
        {GetSerializeLogicForNonPrimitiveType(valueTypeGenerationSpec.TypeInfoPropertyName, valueToWrite, valueTypeGenerationSpec.GenerateSerializationLogic)}";
                }

                string serializationLogic = $@"{WriterVarName}.WriteStartObject();

    foreach ({KeyValuePairTypeRef}<{keyTypeGenerationSpec.TypeRef}, {valueTypeGenerationSpec.TypeRef}> {pairVarName} in {ValueVarName})
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndObject();";

                return GenerateFastPathFuncForType(
                    typeGenerationSpec.FastPathSerializeMethodName,
                    typeGenerationSpec.TypeRef,
                    serializationLogic,
                    typeGenerationSpec.CanBeNull);
            }

            private string GenerateForObject(TypeGenerationSpec typeMetadata)
            {
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                ObjectConstructionStrategy constructionStrategy = typeMetadata.ConstructionStrategy;

                string creatorInvocation = constructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                    ? $"static () => new {typeMetadata.TypeRef}()"
                    : "null";

                string parameterizedCreatorInvocation = constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor
                    ? GetParameterizedCtorInvocationFunc(typeMetadata)
                    : "null";

                string? propMetadataInitFuncSource = null;
                string? ctorParamMetadataInitFuncSource = null;
                string? serializeFuncSource = null;

                string propInitMethodName = "null";
                string ctorParamMetadataInitMethodName = "null";
                string serializeMethodName = "null";

                if (typeMetadata.GenerateMetadata)
                {
                    propMetadataInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata);
                    propInitMethodName = $"{typeFriendlyName}{PropInitMethodNameSuffix}";

                    if (constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        ctorParamMetadataInitFuncSource = GenerateCtorParamMetadataInitFunc(typeMetadata);
                        ctorParamMetadataInitMethodName = $"{typeFriendlyName}{CtorParamInitMethodNameSuffix}";
                    }
                }

                if (typeMetadata.GenerateSerializationLogic)
                {
                    serializeFuncSource = GenerateFastPathFuncForObject(typeMetadata);
                    serializeMethodName = $"{typeFriendlyName}{SerializeMethodNameSuffix}";
                }

                const string ObjectInfoVarName = "objectInfo";
                string genericArg = typeMetadata.TypeRef;

                string objectInfoInitSource = $@"{JsonObjectInfoValuesTypeRef}<{genericArg}> {ObjectInfoVarName} = new {JsonObjectInfoValuesTypeRef}<{genericArg}>()
                {{
                    ObjectCreator = {creatorInvocation},
                    ObjectWithParameterizedConstructorCreator = {parameterizedCreatorInvocation},
                    PropertyMetadataInitializer = {propInitMethodName},
                    ConstructorParameterMetadataInitializer = {ctorParamMetadataInitMethodName},
                    NumberHandling = {GetNumberHandlingAsStr(typeMetadata.NumberHandling)},
                    SerializeHandler = {serializeMethodName}
                }};

                _{typeFriendlyName} = {JsonMetadataServicesTypeRef}.CreateObjectInfo<{typeMetadata.TypeRef}>({OptionsInstanceVariableName}, {ObjectInfoVarName});";

                string additionalSource = @$"{propMetadataInitFuncSource}{serializeFuncSource}{ctorParamMetadataInitFuncSource}";

                return GenerateForType(typeMetadata, objectInfoInitSource, additionalSource);
            }

            private string GeneratePropMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string PropVarName = "properties";
                const string JsonContextVarName = "jsonContext";

                List<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecList!;

                int propCount = properties.Count;

                string propertyArrayInstantiationValue = propCount == 0
                    ? $"{ArrayTypeRef}.Empty<{JsonPropertyInfoTypeRef}>()"
                    : $"new {JsonPropertyInfoTypeRef}[{propCount}]";

                string contextTypeRef = _currentContext.ContextTypeRef;
                string propInitMethodName = $"{typeGenerationSpec.TypeInfoPropertyName}{PropInitMethodNameSuffix}";

                StringBuilder sb = new();

                sb.Append($@"

private static {JsonPropertyInfoTypeRef}[] {propInitMethodName}({JsonSerializerContextTypeRef} context)
{{
    {contextTypeRef} {JsonContextVarName} = ({contextTypeRef})context;
    {JsonSerializerOptionsTypeRef} options = context.Options;

    {JsonPropertyInfoTypeRef}[] {PropVarName} = {propertyArrayInstantiationValue};
");

                for (int i = 0; i < propCount; i++)
                {
                    PropertyGenerationSpec memberMetadata = properties[i];

                    TypeGenerationSpec memberTypeMetadata = memberMetadata.TypeGenerationSpec;

                    string clrPropertyName = memberMetadata.ClrName;

                    string declaringTypeCompilableName = memberMetadata.DeclaringTypeRef;

                    string memberTypeFriendlyName = memberTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                        ? "null"
                        : $"{JsonContextVarName}.{memberTypeMetadata.TypeInfoPropertyName}";

                    string typeTypeInfoNamedArg = $"propertyTypeInfo: {memberTypeFriendlyName}";

                    string jsonPropertyNameNamedArg = memberMetadata.JsonPropertyName != null
                        ? @$"jsonPropertyName: ""{memberMetadata.JsonPropertyName}"""
                        : "jsonPropertyName: null";

                    string getterNamedArg = memberMetadata.CanUseGetter
                        ? $"getter: static (obj) => (({declaringTypeCompilableName})obj).{clrPropertyName}"
                        : "getter: null";

                    string setterNamedArg;
                    if (memberMetadata.CanUseSetter)
                    {
                        string propMutation = typeGenerationSpec.IsValueType
                            ? @$"{UnsafeTypeRef}.Unbox<{declaringTypeCompilableName}>(obj).{clrPropertyName} = value"
                            : $@"(({declaringTypeCompilableName})obj).{clrPropertyName} = value";

                        setterNamedArg = $"setter: static (obj, value) => {propMutation}";
                    }
                    else
                    {
                        setterNamedArg = "setter: null";
                    }

                    JsonIgnoreCondition? ignoreCondition = memberMetadata.DefaultIgnoreCondition;
                    string ignoreConditionNamedArg = ignoreCondition.HasValue
                        ? $"ignoreCondition: {JsonIgnoreConditionTypeRef}.{ignoreCondition.Value}"
                        : "ignoreCondition: null";

                    string converterNamedArg = memberMetadata.ConverterInstantiationLogic == null
                        ? "converter: null"
                        : $"converter: {memberMetadata.ConverterInstantiationLogic}";

                    string memberTypeCompilableName = memberTypeMetadata.TypeRef;

                    sb.Append($@"
    {PropVarName}[{i}] = {JsonMetadataServicesTypeRef}.CreatePropertyInfo<{memberTypeCompilableName}>(
        options,
        isProperty: {ToCSharpKeyword(memberMetadata.IsProperty)},
        isPublic: {ToCSharpKeyword(memberMetadata.IsPublic)},
        isVirtual: {ToCSharpKeyword(memberMetadata.IsVirtual)},
        declaringType: typeof({memberMetadata.DeclaringTypeRef}),
        {typeTypeInfoNamedArg},
        {converterNamedArg},
        {getterNamedArg},
        {setterNamedArg},
        {ignoreConditionNamedArg},
        hasJsonInclude: {ToCSharpKeyword(memberMetadata.HasJsonInclude)},
        numberHandling: {GetNumberHandlingAsStr(memberMetadata.NumberHandling)},
        propertyName: ""{clrPropertyName}"",
        {jsonPropertyNameNamedArg});
    ");
                }

                sb.Append(@$"
    return {PropVarName};
}}");

                return sb.ToString();
            }

            private string GenerateCtorParamMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string parametersVarName = "parameters";
                const string infoVarName = "info";

                ParameterGenerationSpec[] parameters = typeGenerationSpec.CtorParamGenSpecArray;
                int paramCount = parameters.Length;
                Debug.Assert(paramCount > 0);

                StringBuilder sb = new($@"

private static {JsonParameterInfoValuesTypeRef}[] {typeGenerationSpec.TypeInfoPropertyName}{CtorParamInitMethodNameSuffix}()
{{
    {JsonParameterInfoValuesTypeRef}[] {parametersVarName} = new {JsonParameterInfoValuesTypeRef}[{paramCount}];
    {JsonParameterInfoValuesTypeRef} info;
");

                for (int i = 0; i < paramCount; i++)
                {
                    ParameterInfo reflectionInfo = parameters[i].ParameterInfo;

                    sb.Append(@$"
    {infoVarName} = new()
    {{
        Name = ""{reflectionInfo.Name!}"",
        ParameterType = typeof({reflectionInfo.ParameterType.GetCompilableName()}),
        Position = {reflectionInfo.Position},
        HasDefaultValue = {ToCSharpKeyword(reflectionInfo.HasDefaultValue)},
        DefaultValue = {GetParamDefaultValueAsString(reflectionInfo.DefaultValue)}
    }};
    {parametersVarName}[{i}] = {infoVarName};
");
                }

                sb.Append(@$"
    return {parametersVarName};
}}");

                return sb.ToString();
            }

            private string GenerateFastPathFuncForObject(TypeGenerationSpec typeGenSpec)
            {
                JsonSourceGenerationOptionsAttribute options = _currentContext.GenerationOptions;
                string typeRef = typeGenSpec.TypeRef;
                string serializeMethodName = $"{typeGenSpec.TypeInfoPropertyName}{SerializeMethodNameSuffix}";

                if (!typeGenSpec.TryFilterSerializableProps(
                    options,
                    out Dictionary<string, PropertyGenerationSpec>? serializableProperties,
                    out bool castingRequiredForProps))
                {
                    string exceptionMessage = @$"""Invalid serializable-property configuration specified for type '{typeRef}'. For more information, use 'JsonSourceGenerationMode.Serialization'.""";

                    return GenerateFastPathFuncForType(
                        serializeMethodName,
                        typeRef,
                        $@"throw new {InvalidOperationExceptionTypeRef}({exceptionMessage});",
                        canBeNull: false); // Skip null check since we want to throw an exception straightaway.
                }

                StringBuilder sb = new();

                // Begin method logic.
                if (typeGenSpec.ImplementsIJsonOnSerializing)
                {
                    sb.Append($@"((global::{JsonConstants.IJsonOnSerializingFullName}){ValueVarName}).OnSerializing();");
                    sb.Append($@"{Environment.NewLine}    ");
                }

                sb.Append($@"{WriterVarName}.WriteStartObject();");

                // Provide generation logic for each prop.
                Debug.Assert(serializableProperties != null);

                foreach (PropertyGenerationSpec propertyGenSpec in serializableProperties.Values)
                {
                    if (!ShouldIncludePropertyForFastPath(propertyGenSpec, options))
                    {
                        continue;
                    }

                    TypeGenerationSpec propertyTypeSpec = propertyGenSpec.TypeGenerationSpec;

                    string runtimePropName = propertyGenSpec.RuntimePropertyName;

                    // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                    _currentContext.RuntimePropertyNames.Add(runtimePropName);

                    Type propertyType = propertyTypeSpec.Type;
                    string propName = $"{runtimePropName}PropName";
                    string? objectRef = castingRequiredForProps ? $"(({propertyGenSpec.DeclaringTypeRef}){ValueVarName})" : ValueVarName;
                    string propValue = $"{objectRef}.{propertyGenSpec.ClrName}";
                    string methodArgs = $"{propName}, {propValue}";

                    string? methodToCall = GetWriterMethod(propertyType);

                    if (propertyType == _generationSpec.CharType)
                    {
                        methodArgs = $"{methodArgs}.ToString()";
                    }

                    string serializationLogic;

                    if (methodToCall != null)
                    {
                        serializationLogic = $@"
    {methodToCall}({methodArgs});";
                    }
                    else
                    {
                        serializationLogic = $@"
    {WriterVarName}.WritePropertyName({propName});
    {GetSerializeLogicForNonPrimitiveType(propertyTypeSpec.TypeInfoPropertyName, propValue, propertyTypeSpec.GenerateSerializationLogic)}";
                    }

                    JsonIgnoreCondition ignoreCondition = propertyGenSpec.DefaultIgnoreCondition ?? options.DefaultIgnoreCondition;
                    DefaultCheckType defaultCheckType;
                    bool typeCanBeNull = propertyTypeSpec.CanBeNull;

                    switch (ignoreCondition)
                    {
                        case JsonIgnoreCondition.WhenWritingNull:
                            defaultCheckType = typeCanBeNull ? DefaultCheckType.Null : DefaultCheckType.None;
                            break;
                        case JsonIgnoreCondition.WhenWritingDefault:
                            defaultCheckType = typeCanBeNull ? DefaultCheckType.Null : DefaultCheckType.Default;
                            break;
                        default:
                            defaultCheckType = DefaultCheckType.None;
                            break;
                    }

                    sb.Append(WrapSerializationLogicInDefaultCheckIfRequired(serializationLogic, propValue, propertyTypeSpec.TypeRef, defaultCheckType));
                }

                // End method logic.
                sb.Append($@"

    {WriterVarName}.WriteEndObject();");

                if (typeGenSpec.ImplementsIJsonOnSerialized)
                {
                    sb.Append($@"{Environment.NewLine}    ");
                    sb.Append($@"((global::{JsonConstants.IJsonOnSerializedFullName}){ValueVarName}).OnSerialized();");
                };

                return GenerateFastPathFuncForType(serializeMethodName, typeRef, sb.ToString(), typeGenSpec.CanBeNull);
            }

            private static bool ShouldIncludePropertyForFastPath(PropertyGenerationSpec propertyGenSpec, JsonSourceGenerationOptionsAttribute options)
            {
                TypeGenerationSpec propertyTypeSpec = propertyGenSpec.TypeGenerationSpec;

                if (propertyTypeSpec.ClassType == ClassType.TypeUnsupportedBySourceGen || !propertyGenSpec.CanUseGetter)
                {
                    return false;
                }

                if (!propertyGenSpec.IsProperty && !propertyGenSpec.HasJsonInclude && !options.IncludeFields)
                {
                    return false;
                }

                if (propertyGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                {
                    return false;
                }

                if (propertyGenSpec.IsReadOnly)
                {
                    if (propertyGenSpec.IsProperty)
                    {
                        if (options.IgnoreReadOnlyProperties)
                        {
                            return false;
                        }
                    }
                    else if (options.IgnoreReadOnlyFields)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static string GetParameterizedCtorInvocationFunc(TypeGenerationSpec typeGenerationSpec)
            {
                ParameterGenerationSpec[] parameters = typeGenerationSpec.CtorParamGenSpecArray;
                int paramCount = parameters.Length;
                Debug.Assert(paramCount != 0);

                if (paramCount > JsonConstants.MaxParameterCount)
                {
                    return "null";
                }

                const string ArgsVarName = "args";
                int lastIndex = paramCount - 1;

                StringBuilder sb = new($"static ({ArgsVarName}) => new {typeGenerationSpec.TypeRef}(");

                for (int i = 0; i < lastIndex; i++)
                {
                    sb.Append($"{GetParamUnboxing(parameters[i], i)}, ");
                }

                sb.Append($"{GetParamUnboxing(parameters[lastIndex], lastIndex)})");

                return sb.ToString();

                static string GetParamUnboxing(ParameterGenerationSpec spec, int index)
                    => $"({spec.ParameterInfo.ParameterType.GetCompilableName()}){ArgsVarName}[{index}]";
            }

            private string? GetWriterMethod(Type type)
            {
                string? method;
                if (_generationSpec.IsStringBasedType(type))
                {
                    method = $"{WriterVarName}.WriteString";
                }
                else if (type == _generationSpec.BooleanType)
                {
                    method = $"{WriterVarName}.WriteBoolean";
                }
                else if (type == _generationSpec.ByteArrayType)
                {
                    method = $"{WriterVarName}.WriteBase64String";
                }
                else if (type == _generationSpec.CharType)
                {
                    method = $"{WriterVarName}.WriteString";
                }
                else if (_generationSpec.IsNumberType(type))
                {
                    method = $"{WriterVarName}.WriteNumber";
                }
                else
                {
                    method = null;
                }

                return method;
            }

            private string GenerateFastPathFuncForType(string serializeMethodName, string typeInfoTypeRef, string serializationLogic, bool canBeNull)
            {
                return $@"

private static void {serializeMethodName}({Utf8JsonWriterTypeRef} {WriterVarName}, {typeInfoTypeRef} {ValueVarName})
{{
    {GetEarlyNullCheckSource(canBeNull)}
    {serializationLogic}
}}";
            }

            private string GetEarlyNullCheckSource(bool canBeNull)
            {
                return canBeNull
                    ? $@"if ({ValueVarName} == null)
    {{
        {WriterVarName}.WriteNullValue();
        return;
    }}
"
                    : null;
            }

            private string GetSerializeLogicForNonPrimitiveType(string typeInfoPropertyName, string valueToWrite, bool serializationLogicGenerated)
            {
                string typeInfoRef = $"{_currentContext.ContextTypeRef}.Default.{typeInfoPropertyName}";

                if (serializationLogicGenerated)
                {
                    return $"{typeInfoPropertyName}{SerializeMethodNameSuffix}({WriterVarName}, {valueToWrite});";
                }

                return $"{JsonSerializerTypeRef}.Serialize({WriterVarName}, {valueToWrite}, {typeInfoRef});";
            }

            private enum DefaultCheckType
            {
                None,
                Null,
                Default,
            }

            private string WrapSerializationLogicInDefaultCheckIfRequired(string serializationLogic, string propValue, string propTypeRef, DefaultCheckType defaultCheckType)
            {
                string comparisonLogic;

                switch (defaultCheckType)
                {
                    case DefaultCheckType.None:
                        return serializationLogic;
                    case DefaultCheckType.Null:
                        comparisonLogic = $"{propValue} != null";
                        break;
                    case DefaultCheckType.Default:
                        comparisonLogic = $"!{EqualityComparerTypeRef}<{propTypeRef}>.Default.Equals(default, {propValue})";
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                return $@"
    if ({comparisonLogic})
    {{{IndentSource(serializationLogic, numIndentations: 1)}
    }}";
            }

            private string GenerateForType(TypeGenerationSpec typeMetadata, string metadataInitSource, string? additionalSource = null)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                string typeInfoPropertyTypeRef = $"{JsonTypeInfoTypeRef}<{typeCompilableName}>";

                return @$"private {typeInfoPropertyTypeRef} _{typeFriendlyName};
public {typeInfoPropertyTypeRef} {typeFriendlyName}
{{
    get
    {{
        if (_{typeFriendlyName} == null)
        {{
            {WrapWithCheckForCustomConverterIfRequired(metadataInitSource, typeCompilableName, typeFriendlyName, GetNumberHandlingAsStr(typeMetadata.NumberHandling))}
        }}

        return _{typeFriendlyName};
    }}
}}{additionalSource}";
            }

            private string WrapWithCheckForCustomConverterIfRequired(string source, string typeCompilableName, string typeFriendlyName, string numberHandlingNamedArg)
            {
                if (_currentContext.GenerationOptions.IgnoreRuntimeCustomConverters)
                {
                    return source;
                }

                return @$"{JsonConverterTypeRef} customConverter;
                if ({OptionsInstanceVariableName}.Converters.Count > 0 && (customConverter = {RuntimeCustomConverterFetchingMethodName}(typeof({typeCompilableName}))) != null)
                {{
                    _{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, customConverter);
                }}
                else
                {{
                    {IndentSource(source, numIndentations: 1)}
                }}";
            }

            private string GetRootJsonContextImplementation()
            {
                string contextTypeRef = _currentContext.ContextTypeRef;
                string contextTypeName = _currentContext.ContextType.Name;

                StringBuilder sb = new();

                sb.Append(@$"{GetLogicForDefaultSerializerOptionsInit()}

private static {contextTypeRef} {DefaultContextBackingStaticVarName};
public static {contextTypeRef} Default => {DefaultContextBackingStaticVarName} ??= new {contextTypeRef}(new {JsonSerializerOptionsTypeRef}({DefaultOptionsStaticVarName}));

public {contextTypeName}() : base(null, {DefaultOptionsStaticVarName})
{{
}}

public {contextTypeName}({JsonSerializerOptionsTypeRef} options) : base(options, {DefaultOptionsStaticVarName})
{{
}}

{GetFetchLogicForRuntimeSpecifiedCustomConverter()}");

                return sb.ToString();
            }

            private string GetLogicForDefaultSerializerOptionsInit()
            {
                JsonSourceGenerationOptionsAttribute options = _currentContext.GenerationOptions;

                string? namingPolicyInit = options.PropertyNamingPolicy == JsonKnownNamingPolicy.CamelCase
                    ? $@"
            PropertyNamingPolicy = {JsonNamingPolicyTypeRef}.CamelCase"
                    : null;

                return $@"
private static {JsonSerializerOptionsTypeRef} {DefaultOptionsStaticVarName} {{ get; }} = new {JsonSerializerOptionsTypeRef}()
{{
    DefaultIgnoreCondition = {JsonIgnoreConditionTypeRef}.{options.DefaultIgnoreCondition},
    IgnoreReadOnlyFields = {ToCSharpKeyword(options.IgnoreReadOnlyFields)},
    IgnoreReadOnlyProperties = {ToCSharpKeyword(options.IgnoreReadOnlyProperties)},
    IncludeFields = {ToCSharpKeyword(options.IncludeFields)},
    WriteIndented = {ToCSharpKeyword(options.WriteIndented)},{namingPolicyInit}
}};";
            }

            private string GetFetchLogicForRuntimeSpecifiedCustomConverter()
            {
                if (_currentContext.GenerationOptions.IgnoreRuntimeCustomConverters)
                {
                    return "";
                }

                // TODO (https://github.com/dotnet/runtime/issues/52218): use a dictionary if count > ~15.
                return @$"private {JsonConverterTypeRef} {RuntimeCustomConverterFetchingMethodName}({TypeTypeRef} type)
{{
    {IListTypeRef}<{JsonConverterTypeRef}> converters = {OptionsInstanceVariableName}.Converters;

    for (int i = 0; i < converters.Count; i++)
    {{
        {JsonConverterTypeRef} converter = converters[i];

        if (converter.CanConvert(type))
        {{
            if (converter is {JsonConverterFactoryTypeRef} factory)
            {{
                converter = factory.CreateConverter(type, {OptionsInstanceVariableName});
                if (converter == null || converter is {JsonConverterFactoryTypeRef})
                {{
                    throw new {InvalidOperationExceptionTypeRef}($""The converter '{{factory.GetType()}}' cannot return null or a JsonConverterFactory instance."");
                }}
            }}

            return converter;
        }}
    }}

    return null;
}}";
            }

            private string GetGetTypeInfoImplementation()
            {
                StringBuilder sb = new();

                sb.Append(@$"public override {JsonTypeInfoTypeRef} GetTypeInfo({TypeTypeRef} type)
{{");

                HashSet<TypeGenerationSpec> types = new(_currentContext.RootSerializableTypes);
                types.UnionWith(_currentContext.ImplicitlyRegisteredTypes);

                // TODO (https://github.com/dotnet/runtime/issues/52218): Make this Dictionary-lookup-based if root-serializable type count > 64.
                foreach (TypeGenerationSpec metadata in types)
                {
                    if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                    {
                        sb.Append($@"
    if (type == typeof({metadata.TypeRef}))
    {{
        return this.{metadata.TypeInfoPropertyName};
    }}
");
                    }
                }

                sb.Append(@"
    return null!;
}");

                return sb.ToString();
            }

            private string GetPropertyNameInitialization()
            {
                // Ensure metadata for types has already occured.
                Debug.Assert(!(
                    _currentContext.TypesWithMetadataGenerated.Count == 0
                    && _currentContext.RuntimePropertyNames.Count > 0));

                StringBuilder sb = new();

                foreach (string propName in _currentContext.RuntimePropertyNames)
                {
                    sb.Append($@"
private static {JsonEncodedTextTypeRef} {propName}PropName = {JsonEncodedTextTypeRef}.Encode(""{propName}"");");
                }

                return sb.ToString();
            }

            private static string IndentSource(string source, int numIndentations)
            {
                Debug.Assert(numIndentations >= 1);
                return source.Replace(Environment.NewLine, $"{Environment.NewLine}{new string(' ', 4 * numIndentations)}"); // 4 spaces per indentation.
            }

            private static string GetNumberHandlingAsStr(JsonNumberHandling? numberHandling) =>
                 numberHandling.HasValue
                    ? $"({JsonNumberHandlingTypeRef}){(int)numberHandling.Value}"
                    : "default";

            private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";
        }

        private static string ToCSharpKeyword(bool value) => value.ToString().ToLowerInvariant();

        private static string GetParamDefaultValueAsString(object? value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case bool boolVal:
                    return ToCSharpKeyword(boolVal);
                default:
                    return value!.ToString();
            }
        }
    }
}
