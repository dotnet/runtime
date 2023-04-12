// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private const string OptionsLocalVariableName = "options";

        private sealed partial class Emitter
        {
            // Literals in generated source
            private const string CreateValueInfoMethodName = "CreateValueInfo";
            private const string CtorParamInitMethodNameSuffix = "CtorParamInit";
            private const string DefaultOptionsStaticVarName = "s_defaultOptions";
            private const string DefaultContextBackingStaticVarName = "s_defaultContext";
            internal const string GetConverterFromFactoryMethodName = "GetConverterFromFactory";
            private const string OriginatingResolverPropertyName = "OriginatingResolver";
            private const string InfoVarName = "info";
            private const string PropertyInfoVarName = "propertyInfo";
            internal const string JsonContextVarName = "jsonContext";
            private const string NumberHandlingPropName = "NumberHandling";
            private const string UnmappedMemberHandlingPropName = "UnmappedMemberHandling";
            private const string ObjectCreatorPropName = "ObjectCreator";
            private const string OptionsInstanceVariableName = "Options";
            private const string JsonTypeInfoReturnValueLocalVariableName = "jsonTypeInfo";
            private const string PropInitMethodNameSuffix = "PropInit";
            private const string RuntimeCustomConverterFetchingMethodName = "GetRuntimeProvidedCustomConverter";
            private const string SerializeHandlerPropName = "SerializeHandler";
            private const string ValueVarName = "value";
            private const string WriterVarName = "writer";

            private static readonly AssemblyName s_assemblyName = typeof(Emitter).Assembly.GetName();
            private static readonly string s_generatedCodeAttributeSource = $@"
[global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]
";

            // global::fully.qualified.name for referenced types
            private const string ArrayTypeRef = "global::System.Array";
            private const string InvalidOperationExceptionTypeRef = "global::System.InvalidOperationException";
            private const string TypeTypeRef = "global::System.Type";
            private const string UnsafeTypeRef = "global::System.Runtime.CompilerServices.Unsafe";
            private const string EqualityComparerTypeRef = "global::System.Collections.Generic.EqualityComparer";
            private const string IListTypeRef = "global::System.Collections.Generic.IList";
            private const string KeyValuePairTypeRef = "global::System.Collections.Generic.KeyValuePair";
            private const string JsonEncodedTextTypeRef = "global::System.Text.Json.JsonEncodedText";
            private const string JsonNamingPolicyTypeRef = "global::System.Text.Json.JsonNamingPolicy";
            private const string JsonSerializerTypeRef = "global::System.Text.Json.JsonSerializer";
            private const string JsonSerializerOptionsTypeRef = "global::System.Text.Json.JsonSerializerOptions";
            private const string JsonSerializerContextTypeRef = "global::System.Text.Json.Serialization.JsonSerializerContext";
            private const string Utf8JsonWriterTypeRef = "global::System.Text.Json.Utf8JsonWriter";
            private const string JsonConverterTypeRef = "global::System.Text.Json.Serialization.JsonConverter";
            private const string JsonConverterFactoryTypeRef = "global::System.Text.Json.Serialization.JsonConverterFactory";
            private const string JsonCollectionInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues";
            private const string JsonIgnoreConditionTypeRef = "global::System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonNumberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonNumberHandling";
            private const string JsonUnmappedMemberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonUnmappedMemberHandling";
            private const string JsonMetadataServicesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonMetadataServices";
            private const string JsonObjectInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues";
            private const string JsonParameterInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonParameterInfoValues";
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonPropertyInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";
            private const string JsonTypeInfoResolverTypeRef = "global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver";

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

            private readonly JsonSourceGenerationContext _sourceGenerationContext;

            private ContextGenerationSpec _currentContext = null!;

            private readonly SourceGenerationSpec _generationSpec;

            private readonly HashSet<string> _emittedPropertyFileNames = new();

            private bool _generateGetConverterMethodForTypes;
            private bool _generateGetConverterMethodForProperties;

            public Emitter(in JsonSourceGenerationContext sourceGenerationContext, SourceGenerationSpec generationSpec)
            {
                _sourceGenerationContext = sourceGenerationContext;
                _generationSpec = generationSpec;
            }

            public void Emit()
            {
                foreach (ContextGenerationSpec contextGenerationSpec in _generationSpec.ContextGenerationSpecList)
                {
                    _currentContext = contextGenerationSpec;
                    _generateGetConverterMethodForTypes = false;
                    _generateGetConverterMethodForProperties = false;

                    foreach (TypeGenerationSpec typeGenerationSpec in _currentContext.RootSerializableTypes)
                    {
                        GenerateTypeInfo(typeGenerationSpec);
                    }

                    foreach (TypeGenerationSpec typeGenerationSpec in _currentContext.ImplicitlyRegisteredTypes)
                    {
                        GenerateTypeInfo(typeGenerationSpec);
                    }

                    string contextName = _currentContext.ContextType.Name;

                    // Add root context implementation.
                    AddSource(
                        $"{contextName}.g.cs",
                        GetRootJsonContextImplementation(),
                        isRootContextDef: true);

                    // Add GetJsonTypeInfo override implementation.
                    AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation(), interfaceImplementation: JsonTypeInfoResolverTypeRef);

                    // Add property name initialization.
                    AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization());
                }
            }

            private void AddSource(string fileName, string source, bool isRootContextDef = false, string? interfaceImplementation = null)
            {
                string? generatedCodeAttributeSource = isRootContextDef ? s_generatedCodeAttributeSource : null;

                List<string> declarationList = _currentContext.ContextClassDeclarationList;
                int declarationCount = declarationList.Count;
                Debug.Assert(declarationCount >= 1);

                string? @namespace = _currentContext.ContextType.Namespace;
                bool isInGlobalNamespace = @namespace == JsonConstants.GlobalNamespaceValue;

                StringBuilder sb = new("""
// <auto-generated/>

#nullable enable annotations
#nullable disable warnings

// Suppress warnings about [Obsolete] member usage in generated code.
#pragma warning disable CS0612, CS0618


""");
                int indentation = 0;

                if (!isInGlobalNamespace)
                {
                    sb.AppendLine($$"""
namespace {{@namespace}}
{
""");
                    indentation++;
                }

                for (int i = 0; i < declarationCount - 1; i++)
                {
                    string declarationSource = $$"""
{{declarationList[declarationCount - 1 - i]}}
{
""";
                    sb.AppendLine(IndentSource(declarationSource, numIndentations: indentation++));
                }

                // Add the core implementation for the derived context class.
                string partialContextImplementation = $$"""
{{generatedCodeAttributeSource}}{{declarationList[0]}}{{(interfaceImplementation is null ? "" : ": " + interfaceImplementation)}}
{
{{IndentSource(source, 1)}}
}
""";
                sb.AppendLine(IndentSource(partialContextImplementation, numIndentations: indentation--));

                // Match curly braces for each containing type/namespace.
                for (; indentation >= 0; indentation--)
                {
                    sb.AppendLine(IndentSource("}", numIndentations: indentation));
                }

                _sourceGenerationContext.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
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
                            Debug.Assert(typeGenerationSpec.NullableUnderlyingTypeMetadata != null);

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
                            Debug.Assert(typeGenerationSpec.CollectionValueTypeMetadata != null);

                            source = GenerateForCollection(typeGenerationSpec);
                            GenerateTypeInfo(typeGenerationSpec.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Dictionary:
                        {
                            Debug.Assert(typeGenerationSpec.CollectionKeyTypeMetadata != null);
                            Debug.Assert(typeGenerationSpec.CollectionValueTypeMetadata != null);

                            source = GenerateForCollection(typeGenerationSpec);
                            GenerateTypeInfo(typeGenerationSpec.CollectionKeyTypeMetadata);
                            GenerateTypeInfo(typeGenerationSpec.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Object:
                        {
                            Debug.Assert(typeGenerationSpec.PropertyGenSpecList != null);

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

                            TypeGenerationSpec? extPropTypeSpec = typeGenerationSpec.ExtensionDataPropertyTypeSpec;
                            if (extPropTypeSpec != null)
                            {
                                GenerateTypeInfo(extPropTypeSpec);
                            }
                        }
                        break;
                    case ClassType.KnownUnsupportedType:
                        {
                            source = GenerateForUnsupportedType(typeGenerationSpec);
                        }
                        break;
                    case ClassType.TypeUnsupportedBySourceGen:
                        {
                            Location location = typeGenerationSpec.Type.GetDiagnosticLocation() ?? typeGenerationSpec.AttributeLocation ?? _currentContext.Location;
                            _sourceGenerationContext.ReportDiagnostic(
                                Diagnostic.Create(TypeNotSupported, location, new string[] { typeGenerationSpec.TypeRef }));
                            return;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }

                // Don't add a duplicate file, but instead raise a diagnostic to say the duplicate has been skipped.
                // Workaround https://github.com/dotnet/roslyn/issues/54185 by keeping track of the file names we've used.
                string propertyFileName = $"{_currentContext.ContextType.Name}.{typeGenerationSpec.TypeInfoPropertyName}.g.cs";
                if (_emittedPropertyFileNames.Add(propertyFileName))
                {
                    AddSource(propertyFileName, source);
                }
                else
                {
                    Location location = typeGenerationSpec.AttributeLocation ?? _currentContext.Location;
                    _sourceGenerationContext.ReportDiagnostic(Diagnostic.Create(DuplicateTypeName, location, new string[] { typeGenerationSpec.TypeInfoPropertyName }));
                }

                _generateGetConverterMethodForTypes |= typeGenerationSpec.HasTypeFactoryConverter;
                _generateGetConverterMethodForProperties |= typeGenerationSpec.HasPropertyFactoryConverters;
            }

            private static string GenerateForTypeWithKnownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $@"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.{typeFriendlyName}Converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForTypeWithUnknownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;

                // TODO (https://github.com/dotnet/runtime/issues/52218): consider moving this verification source to common helper.
                StringBuilder metadataInitSource = new(
                    $@"{JsonConverterTypeRef} converter = {typeMetadata.ConverterInstantiationLogic};
                {TypeTypeRef} typeToConvert = typeof({typeCompilableName});");

                metadataInitSource.Append($@"
                if (!converter.CanConvert(typeToConvert))
                {{
                    throw new {InvalidOperationExceptionTypeRef}(string.Format(""{ExceptionMessages.IncompatibleConverterType}"", converter.GetType(), typeToConvert));
                }}");

                metadataInitSource.Append($@"
                {JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)} ({OptionsLocalVariableName}, converter); ");

                return GenerateForType(typeMetadata, metadataInitSource.ToString());
            }

            private static string GenerateForNullable(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;

                TypeGenerationSpec? underlyingTypeMetadata = typeMetadata.NullableUnderlyingTypeMetadata;
                Debug.Assert(underlyingTypeMetadata != null);

                string underlyingTypeCompilableName = underlyingTypeMetadata.TypeRef;

                string metadataInitSource = @$"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}(
                    {OptionsLocalVariableName},
                    {JsonMetadataServicesTypeRef}.GetNullableConverter<{underlyingTypeCompilableName}>({OptionsLocalVariableName}));
";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForUnsupportedType(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;

                string metadataInitSource = $"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetUnsupportedTypeConverter<{typeCompilableName}>());";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForEnum(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;

                string metadataInitSource = $"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetEnumConverter<{typeCompilableName}>({OptionsLocalVariableName}));";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForCollection(TypeGenerationSpec typeGenerationSpec)
            {
                // Key metadata
                TypeGenerationSpec? collectionKeyTypeMetadata = typeGenerationSpec.CollectionKeyTypeMetadata;
                Debug.Assert(!(typeGenerationSpec.ClassType == ClassType.Dictionary && collectionKeyTypeMetadata == null));
                string? keyTypeCompilableName = collectionKeyTypeMetadata?.TypeRef;

                // Value metadata
                TypeGenerationSpec? collectionValueTypeMetadata = typeGenerationSpec.CollectionValueTypeMetadata;
                Debug.Assert(collectionValueTypeMetadata != null);
                string valueTypeCompilableName = collectionValueTypeMetadata.TypeRef;

                string numberHandlingArg = $"{GetNumberHandlingAsStr(typeGenerationSpec.NumberHandling)}";

                string serializeHandlerValue;

                string? serializeHandlerSource;
                if (!typeGenerationSpec.GenerateSerializationLogic)
                {
                    serializeHandlerSource = null;
                    serializeHandlerValue = "null";
                }
                else
                {
                    serializeHandlerSource = typeGenerationSpec.ClassType == ClassType.Enumerable
                        ? GenerateFastPathFuncForEnumerable(typeGenerationSpec)
                        : GenerateFastPathFuncForDictionary(typeGenerationSpec);

                    serializeHandlerValue = $"{typeGenerationSpec.TypeInfoPropertyName}{SerializeHandlerPropName}";
                }

                CollectionType collectionType = typeGenerationSpec.CollectionType;

                string typeRef = typeGenerationSpec.TypeRef;

                string objectCreatorValue;
                if (typeGenerationSpec.RuntimeTypeRef != null)
                {
                    objectCreatorValue = $"() => new {typeGenerationSpec.RuntimeTypeRef}()";
                }
                else
                {
                    objectCreatorValue = typeGenerationSpec.ConstructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                        ? $"() => new {typeRef}()"
                        : "null";
                }

                string collectionInfoCreationPrefix = collectionType switch
                {
                    CollectionType.IListOfT => $"{JsonMetadataServicesTypeRef}.CreateIListInfo<",
                    CollectionType.ICollectionOfT => $"{JsonMetadataServicesTypeRef}.CreateICollectionInfo<",
                    CollectionType.StackOfT => $"{JsonMetadataServicesTypeRef}.CreateStackInfo<",
                    CollectionType.QueueOfT => $"{JsonMetadataServicesTypeRef}.CreateQueueInfo<",
                    CollectionType.Stack => $"{JsonMetadataServicesTypeRef}.CreateStackInfo<",
                    CollectionType.Queue => $"{JsonMetadataServicesTypeRef}.CreateQueueInfo<",
                    CollectionType.IEnumerableOfT => $"{JsonMetadataServicesTypeRef}.CreateIEnumerableInfo<",
                    CollectionType.IAsyncEnumerableOfT => $"{JsonMetadataServicesTypeRef}.CreateIAsyncEnumerableInfo<",
                    CollectionType.IDictionaryOfTKeyTValue => $"{JsonMetadataServicesTypeRef}.CreateIDictionaryInfo<",
                    _ => $"{JsonMetadataServicesTypeRef}.Create{collectionType}Info<"
                };

                string dictInfoCreationPrefix = $"{collectionInfoCreationPrefix}{typeRef}, {keyTypeCompilableName!}, {valueTypeCompilableName}>({OptionsLocalVariableName}, {InfoVarName}";
                string enumerableInfoCreationPrefix = $"{collectionInfoCreationPrefix}{typeRef}, {valueTypeCompilableName}>({OptionsLocalVariableName}, {InfoVarName}";
                string immutableCollectionCreationSuffix = $"createRangeFunc: {typeGenerationSpec.ImmutableCollectionBuilderName}";

                string collectionTypeInfoValue;

                switch (collectionType)
                {
                    case CollectionType.Array:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{valueTypeCompilableName}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                    case CollectionType.IEnumerable:
                    case CollectionType.IList:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                    case CollectionType.Stack:
                    case CollectionType.Queue:
                        string addMethod = collectionType == CollectionType.Stack ? "Push" : "Enqueue";
                        string addFuncNamedArg = $"addFunc: (collection, {ValueVarName}) => collection.{addMethod}({ValueVarName})";
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsLocalVariableName}, {InfoVarName}, {addFuncNamedArg})";
                        break;
                    case CollectionType.ImmutableEnumerable:
                        collectionTypeInfoValue = $"{enumerableInfoCreationPrefix}, {immutableCollectionCreationSuffix})";
                        break;
                    case CollectionType.IDictionary:
                        collectionTypeInfoValue = $"{collectionInfoCreationPrefix}{typeRef}>({OptionsLocalVariableName}, {InfoVarName})";
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

                string metadataInitSource = @$"{JsonCollectionInfoValuesTypeRef}<{typeRef}> {InfoVarName} = new {JsonCollectionInfoValuesTypeRef}<{typeRef}>()
        {{
            {ObjectCreatorPropName} = {objectCreatorValue},
            {NumberHandlingPropName} = {numberHandlingArg},
            {SerializeHandlerPropName} = {serializeHandlerValue}
        }};

        {JsonTypeInfoReturnValueLocalVariableName} = {collectionTypeInfoValue};
";

                return GenerateForType(typeGenerationSpec, metadataInitSource, serializeHandlerSource);
            }

            private string GenerateFastPathFuncForEnumerable(TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CollectionValueTypeMetadata != null);

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
                    ? GetSerializeLogicForNonPrimitiveType(valueTypeGenerationSpec, valueToWrite)
                    : $"{writerMethodToCall}Value({valueToWrite});";

                string serializationLogic = $@"{WriterVarName}.WriteStartArray();

    {iterationLogic}
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndArray();";

                return GenerateFastPathFuncForType(typeGenerationSpec, serializationLogic, emitNullCheck: typeGenerationSpec.CanBeNull);
            }

            private string GenerateFastPathFuncForDictionary(TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CollectionKeyTypeMetadata != null);
                Debug.Assert(typeGenerationSpec.CollectionValueTypeMetadata != null);

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
        {GetSerializeLogicForNonPrimitiveType(valueTypeGenerationSpec, valueToWrite)}";
                }

                string serializationLogic = $@"{WriterVarName}.WriteStartObject();

    foreach ({KeyValuePairTypeRef}<{keyTypeGenerationSpec.TypeRef}, {valueTypeGenerationSpec.TypeRef}> {pairVarName} in {ValueVarName})
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndObject();";

                return GenerateFastPathFuncForType(typeGenerationSpec, serializationLogic, emitNullCheck: typeGenerationSpec.CanBeNull);
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

                string propInitMethod = "null";
                string ctorParamMetadataInitMethodName = "null";
                string serializeMethodName = "null";

                if (typeMetadata.GenerateMetadata)
                {
                    propMetadataInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata);
                    propInitMethod = $"_ => {typeFriendlyName}{PropInitMethodNameSuffix}({OptionsLocalVariableName})";

                    if (constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        ctorParamMetadataInitFuncSource = GenerateCtorParamMetadataInitFunc(typeMetadata);
                        ctorParamMetadataInitMethodName = $"{typeFriendlyName}{CtorParamInitMethodNameSuffix}";
                    }
                }

                if (typeMetadata.GenerateSerializationLogic)
                {
                    serializeFuncSource = GenerateFastPathFuncForObject(typeMetadata);
                    serializeMethodName = $"{typeFriendlyName}{SerializeHandlerPropName}";
                }

                const string ObjectInfoVarName = "objectInfo";
                string genericArg = typeMetadata.TypeRef;

                string objectInfoInitSource = $@"{JsonObjectInfoValuesTypeRef}<{genericArg}> {ObjectInfoVarName} = new {JsonObjectInfoValuesTypeRef}<{genericArg}>()
        {{
            {ObjectCreatorPropName} = {creatorInvocation},
            ObjectWithParameterizedConstructorCreator = {parameterizedCreatorInvocation},
            PropertyMetadataInitializer = {propInitMethod},
            ConstructorParameterMetadataInitializer = {ctorParamMetadataInitMethodName},
            {NumberHandlingPropName} = {GetNumberHandlingAsStr(typeMetadata.NumberHandling)},
            {SerializeHandlerPropName} = {serializeMethodName}
        }};

        {JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.CreateObjectInfo<{typeMetadata.TypeRef}>({OptionsLocalVariableName}, {ObjectInfoVarName});";

                if (typeMetadata.UnmappedMemberHandling != null)
                {
                    objectInfoInitSource += $"""

        {JsonTypeInfoReturnValueLocalVariableName}.{UnmappedMemberHandlingPropName} = {GetUnmappedMemberHandlingAsStr(typeMetadata.UnmappedMemberHandling.Value)};
""";
                }

                string additionalSource = @$"{propMetadataInitFuncSource}{serializeFuncSource}{ctorParamMetadataInitFuncSource}";

                return GenerateForType(typeMetadata, objectInfoInitSource, additionalSource);
            }

            private static string GeneratePropMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string PropVarName = "properties";

                List<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecList!;

                int propCount = properties.Count;

                string propertyArrayInstantiationValue = propCount == 0
                    ? $"{ArrayTypeRef}.Empty<{JsonPropertyInfoTypeRef}>()"
                    : $"new {JsonPropertyInfoTypeRef}[{propCount}]";

                string propInitMethodName = $"{typeGenerationSpec.TypeInfoPropertyName}{PropInitMethodNameSuffix}";

                StringBuilder sb = new();

                sb.Append($@"
private static {JsonPropertyInfoTypeRef}[] {propInitMethodName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})
{{
    {JsonPropertyInfoTypeRef}[] {PropVarName} = {propertyArrayInstantiationValue};
");

                for (int i = 0; i < propCount; i++)
                {
                    PropertyGenerationSpec memberMetadata = properties[i];

                    TypeGenerationSpec memberTypeMetadata = memberMetadata.TypeGenerationSpec;

                    string nameSpecifiedInSourceCode = memberMetadata.NameSpecifiedInSourceCode;

                    string declaringTypeCompilableName = memberMetadata.DeclaringTypeRef;

                    string jsonPropertyNameValue = memberMetadata.JsonPropertyName != null
                        ? @$"""{memberMetadata.JsonPropertyName}"""
                        : "null";

                    string getterValue = memberMetadata switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseGetter: true } => $"static (obj) => (({declaringTypeCompilableName})obj).{nameSpecifiedInSourceCode}{(memberMetadata.TypeGenerationSpec.CanContainNullableReferenceAnnotations ? "!" : "")}",
                        { CanUseGetter: false, HasJsonInclude: true }
                            => @$"static (obj) => throw new {InvalidOperationExceptionTypeRef}(""{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.Type.Name, nameSpecifiedInSourceCode)}"")",
                        _ => "null"
                    };

                    string setterValue = memberMetadata switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseSetter: true, IsInitOnlySetter: true }
                            => @$"static (obj, value) => throw new {InvalidOperationExceptionTypeRef}(""{ExceptionMessages.InitOnlyPropertySetterNotSupported}"")",
                        { CanUseSetter: true } when typeGenerationSpec.IsValueType
                            => $@"static (obj, value) => {UnsafeTypeRef}.Unbox<{declaringTypeCompilableName}>(obj).{nameSpecifiedInSourceCode} = value!",
                        { CanUseSetter: true }
                            => @$"static (obj, value) => (({declaringTypeCompilableName})obj).{nameSpecifiedInSourceCode} = value!",
                        { CanUseSetter: false, HasJsonInclude: true }
                            => @$"static (obj, value) => throw new {InvalidOperationExceptionTypeRef}(""{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.Type.Name, memberMetadata.ClrName)}"")",
                        _ => "null",
                    };

                    JsonIgnoreCondition? ignoreCondition = memberMetadata.DefaultIgnoreCondition;
                    string ignoreConditionNamedArg = ignoreCondition.HasValue
                        ? $"{JsonIgnoreConditionTypeRef}.{ignoreCondition.Value}"
                        : "null";

                    string converterValue = memberMetadata.ConverterInstantiationLogic == null
                        ? "null"
                        : $"{memberMetadata.ConverterInstantiationLogic}";

                    string memberTypeCompilableName = memberTypeMetadata.TypeRef;

                    string infoVarName = $"{InfoVarName}{i}";
                    string propertyInfoVarName = $"{PropertyInfoVarName}{i}";

                    sb.Append($@"
    {JsonPropertyInfoValuesTypeRef}<{memberTypeCompilableName}> {infoVarName} = new {JsonPropertyInfoValuesTypeRef}<{memberTypeCompilableName}>()
    {{
        IsProperty = {FormatBool(memberMetadata.IsProperty)},
        IsPublic = {FormatBool(memberMetadata.IsPublic)},
        IsVirtual = {FormatBool(memberMetadata.IsVirtual)},
        DeclaringType = typeof({memberMetadata.DeclaringTypeRef}),
        Converter = {converterValue},
        Getter = {getterValue},
        Setter = {setterValue},
        IgnoreCondition = {ignoreConditionNamedArg},
        HasJsonInclude = {FormatBool(memberMetadata.HasJsonInclude)},
        IsExtensionData = {FormatBool(memberMetadata.IsExtensionData)},
        NumberHandling = {GetNumberHandlingAsStr(memberMetadata.NumberHandling)},
        PropertyName = ""{memberMetadata.ClrName}"",
        JsonPropertyName = {jsonPropertyNameValue}
    }};

    {JsonPropertyInfoTypeRef} {propertyInfoVarName} = {JsonMetadataServicesTypeRef}.CreatePropertyInfo<{memberTypeCompilableName}>({OptionsLocalVariableName}, {infoVarName});");

                    if (memberMetadata.HasJsonRequiredAttribute ||
                        (memberMetadata.IsRequired && !typeGenerationSpec.ConstructorSetsRequiredParameters))
                    {
                        sb.Append($@"
    {propertyInfoVarName}.IsRequired = true;");
                    }

                    sb.Append($@"
    {PropVarName}[{i}] = {propertyInfoVarName};
");
                }

                sb.Append(@$"
    return {PropVarName};
}}");

                return sb.ToString();
            }

            private
#if !DEBUG
                static
#endif
                string GenerateCtorParamMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string parametersVarName = "parameters";

                Debug.Assert(typeGenerationSpec.CtorParamGenSpecArray != null);

                ParameterGenerationSpec[] parameters = typeGenerationSpec.CtorParamGenSpecArray;
                List<PropertyInitializerGenerationSpec>? propertyInitializers = typeGenerationSpec.PropertyInitializerSpecList;
                int paramCount = parameters.Length + (propertyInitializers?.Count(propInit => !propInit.MatchesConstructorParameter) ?? 0);
                Debug.Assert(paramCount > 0);

                StringBuilder sb = new($@"

private static {JsonParameterInfoValuesTypeRef}[] {typeGenerationSpec.TypeInfoPropertyName}{CtorParamInitMethodNameSuffix}()
{{
    {JsonParameterInfoValuesTypeRef}[] {parametersVarName} = new {JsonParameterInfoValuesTypeRef}[{paramCount}];
    {JsonParameterInfoValuesTypeRef} info;
");
                foreach (ParameterGenerationSpec spec in parameters)
                {
                    ParameterInfo reflectionInfo = spec.ParameterInfo;
                    Type parameterType = reflectionInfo.ParameterType;
                    string parameterTypeRef = parameterType.GetCompilableName();

                    object? defaultValue = reflectionInfo.GetDefaultValue();
                    string defaultValueAsStr = GetParamDefaultValueAsString(defaultValue, parameterType, parameterTypeRef);

                    sb.Append(@$"
    {InfoVarName} = new()
    {{
        Name = ""{reflectionInfo.Name!}"",
        ParameterType = typeof({parameterTypeRef}),
        Position = {reflectionInfo.Position},
        HasDefaultValue = {FormatBool(reflectionInfo.HasDefaultValue)},
        DefaultValue = {defaultValueAsStr}
    }};
    {parametersVarName}[{spec.ParameterIndex}] = {InfoVarName};
");
                }

                if (propertyInitializers != null)
                {
                    Debug.Assert(propertyInitializers.Count > 0);

                    foreach (PropertyInitializerGenerationSpec spec in propertyInitializers)
                    {
                        if (spec.MatchesConstructorParameter)
                            continue;

                        sb.Append(@$"
    {InfoVarName} = new()
    {{
        Name = ""{spec.Property.ClrName}"",
        ParameterType = typeof({spec.Property.TypeGenerationSpec.TypeRef}),
        Position = {spec.ParameterIndex},
        HasDefaultValue = false,
        DefaultValue = default({spec.Property.TypeGenerationSpec.TypeRef}),
    }};
    {parametersVarName}[{spec.ParameterIndex}] = {InfoVarName};
");
                    }
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

                if (!typeGenSpec.TryFilterSerializableProps(
                    options,
                    out Dictionary<string, PropertyGenerationSpec>? serializableProperties,
                    out bool castingRequiredForProps))
                {
                    string exceptionMessage = string.Format(ExceptionMessages.InvalidSerializablePropertyConfiguration, typeRef);

                    return GenerateFastPathFuncForType(typeGenSpec,
                        $@"throw new {InvalidOperationExceptionTypeRef}(""{exceptionMessage}"");",
                        emitNullCheck: false); // Skip null check since we want to throw an exception straightaway.
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
                    string propVarName = propertyGenSpec.PropertyNameVarName;

                    // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                    Debug.Assert(!_currentContext.RuntimePropertyNames.TryGetValue(runtimePropName, out string? existingName) || existingName == propVarName);
                    _currentContext.RuntimePropertyNames.TryAdd(runtimePropName, propVarName);

                    Type propertyType = propertyTypeSpec.Type;
                    string? objectRef = castingRequiredForProps ? $"(({propertyGenSpec.DeclaringTypeRef}){ValueVarName})" : ValueVarName;
                    string propValue = $"{objectRef}.{propertyGenSpec.NameSpecifiedInSourceCode}";
                    string methodArgs = $"{propVarName}, {propValue}";

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
    {WriterVarName}.WritePropertyName({propVarName});
    {GetSerializeLogicForNonPrimitiveType(propertyTypeSpec, propValue)}";
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

                return GenerateFastPathFuncForType(typeGenSpec, sb.ToString(), emitNullCheck: typeGenSpec.CanBeNull);
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
                Debug.Assert(typeGenerationSpec.CtorParamGenSpecArray != null);
                ParameterGenerationSpec[] parameters = typeGenerationSpec.CtorParamGenSpecArray;
                List<PropertyInitializerGenerationSpec>? propertyInitializers = typeGenerationSpec.PropertyInitializerSpecList;

                const string ArgsVarName = "args";

                StringBuilder sb = new($"static ({ArgsVarName}) => new {typeGenerationSpec.TypeRef}(");

                if (parameters.Length > 0)
                {
                    foreach (ParameterGenerationSpec param in parameters)
                    {
                        int index = param.ParameterIndex;
                        sb.Append($"{GetParamUnboxing(param.ParameterInfo.ParameterType, index)}, ");
                    }

                    sb.Length -= 2; // delete the last ", " token
                }

                sb.Append(')');

                if (propertyInitializers != null)
                {
                    Debug.Assert(propertyInitializers.Count > 0);
                    sb.Append("{ ");
                    foreach (PropertyInitializerGenerationSpec property in propertyInitializers)
                    {
                        sb.Append($"{property.Property.ClrName} = {GetParamUnboxing(property.Property.TypeGenerationSpec.Type, property.ParameterIndex)}, ");
                    }

                    sb.Length -= 2; // delete the last ", " token
                    sb.Append(" }");
                }

                return sb.ToString();

                static string GetParamUnboxing(Type type, int index)
                    => $"({type.GetCompilableName()}){ArgsVarName}[{index}]";
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

            private static string GenerateFastPathFuncForType(TypeGenerationSpec typeGenSpec, string serializeMethodBody, bool emitNullCheck)
            {
                Debug.Assert(!emitNullCheck || typeGenSpec.CanBeNull);

                string serializeMethodName = $"{typeGenSpec.TypeInfoPropertyName}{SerializeHandlerPropName}";
                // fast path serializers for reference types always support null inputs.
                string valueTypeRef = $"{typeGenSpec.TypeRef}{(typeGenSpec.IsValueType ? "" : "?")}";

                return $@"

// Intentionally not a static method because we create a delegate to it. Invoking delegates to instance
// methods is almost as fast as virtual calls. Static methods need to go through a shuffle thunk.
private void {serializeMethodName}({Utf8JsonWriterTypeRef} {WriterVarName}, {valueTypeRef} {ValueVarName})
{{
    {GetEarlyNullCheckSource(emitNullCheck)}
    {serializeMethodBody}
}}";
            }

            private static string? GetEarlyNullCheckSource(bool canBeNull)
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

            private string GetSerializeLogicForNonPrimitiveType(TypeGenerationSpec typeGenerationSpec, string valueExpr)
            {
                string valueExprSuffix = typeGenerationSpec.CanContainNullableReferenceAnnotations ? "!" : "";

                if (typeGenerationSpec.GenerateSerializationLogic)
                {
                    return $"{typeGenerationSpec.TypeInfoPropertyName}{SerializeHandlerPropName}({WriterVarName}, {valueExpr}{valueExprSuffix});";
                }

                string typeInfoRef = $"{_currentContext.ContextTypeRef}.Default.{typeGenerationSpec.TypeInfoPropertyName}!";
                return $"{JsonSerializerTypeRef}.Serialize({WriterVarName}, {valueExpr}{valueExprSuffix}, {typeInfoRef});";
            }

            private enum DefaultCheckType
            {
                None,
                Null,
                Default,
            }

            private static string WrapSerializationLogicInDefaultCheckIfRequired(string serializationLogic, string propValue, string propTypeRef, DefaultCheckType defaultCheckType)
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

            private static string GenerateForType(TypeGenerationSpec typeMetadata, string metadataInitSource, string? additionalSource = null)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                string typeInfoPropertyTypeRef = $"{JsonTypeInfoTypeRef}<{typeCompilableName}>";

                return @$"private {typeInfoPropertyTypeRef}? _{typeFriendlyName};

/// <summary>
/// Defines the source generated JSON serialization contract metadata for a given type.
/// </summary>
public {typeInfoPropertyTypeRef} {typeFriendlyName}
{{
    get => _{typeFriendlyName} ??= ({typeInfoPropertyTypeRef}){OptionsInstanceVariableName}.GetTypeInfo(typeof({typeCompilableName}));
}}

private {typeInfoPropertyTypeRef} {typeMetadata.CreateTypeInfoMethodName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})
{{
    {typeInfoPropertyTypeRef}? {JsonTypeInfoReturnValueLocalVariableName} = null;
    {WrapWithCheckForCustomConverter(metadataInitSource, typeCompilableName)}

    { /* NB OriginatingResolver should be the last property set by the source generator. */ ""}
    {JsonTypeInfoReturnValueLocalVariableName}.{OriginatingResolverPropertyName} = this;

    return {JsonTypeInfoReturnValueLocalVariableName};
}}
{additionalSource}";
            }

            private static string WrapWithCheckForCustomConverter(string source, string typeCompilableName)
                => @$"{JsonConverterTypeRef}? customConverter;
    if ({OptionsLocalVariableName}.Converters.Count > 0 && (customConverter = {RuntimeCustomConverterFetchingMethodName}({OptionsLocalVariableName}, typeof({typeCompilableName}))) != null)
    {{
        {JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, customConverter);
    }}
    else
    {{
        {source}
    }}";

            private string GetRootJsonContextImplementation()
            {
                string contextTypeRef = _currentContext.ContextTypeRef;
                string contextTypeName = _currentContext.ContextType.Name;

                int backTickIndex = contextTypeName.IndexOf('`');
                if (backTickIndex != -1)
                {
                    contextTypeName = contextTypeName.Substring(0, backTickIndex);
                }

                StringBuilder sb = new();

                sb.Append(@$"{GetLogicForDefaultSerializerOptionsInit()}

private static {contextTypeRef}? {DefaultContextBackingStaticVarName};

/// <summary>
/// The default <see cref=""{JsonSerializerContextTypeRef}""/> associated with a default <see cref=""{JsonSerializerOptionsTypeRef}""/> instance.
/// </summary>
public static {contextTypeRef} Default => {DefaultContextBackingStaticVarName} ??= new {contextTypeRef}(new {JsonSerializerOptionsTypeRef}({DefaultOptionsStaticVarName}));

/// <summary>
/// The source-generated options associated with this context.
/// </summary>
protected override {JsonSerializerOptionsTypeRef}? GeneratedSerializerOptions {{ get; }} = {DefaultOptionsStaticVarName};

/// <inheritdoc/>
public {contextTypeName}() : base(null)
{{
}}

/// <inheritdoc/>
public {contextTypeName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName}) : base({OptionsLocalVariableName})
{{
}}

{GetFetchLogicForRuntimeSpecifiedCustomConverter()}");

                if (_generateGetConverterMethodForProperties)
                {
                    sb.Append(GetFetchLogicForGetCustomConverter_PropertiesWithFactories());
                }

                if (_generateGetConverterMethodForProperties || _generateGetConverterMethodForTypes)
                {
                    sb.Append(GetFetchLogicForGetCustomConverter_TypesWithFactories());
                }

                return sb.ToString();
            }

            private string GetLogicForDefaultSerializerOptionsInit()
            {
                JsonSourceGenerationOptionsAttribute options = _currentContext.GenerationOptions;

                string? namingPolicyName = options.PropertyNamingPolicy switch
                {
                    JsonKnownNamingPolicy.CamelCase => nameof(JsonNamingPolicy.CamelCase),
                    JsonKnownNamingPolicy.SnakeCaseLower => nameof(JsonNamingPolicy.SnakeCaseLower),
                    JsonKnownNamingPolicy.SnakeCaseUpper => nameof(JsonNamingPolicy.SnakeCaseUpper),
                    JsonKnownNamingPolicy.KebabCaseLower => nameof(JsonNamingPolicy.KebabCaseLower),
                    JsonKnownNamingPolicy.KebabCaseUpper => nameof(JsonNamingPolicy.KebabCaseUpper),
                    _ => null,
                };

                string? namingPolicyInit = namingPolicyName != null
                    ? $@"
            PropertyNamingPolicy = {JsonNamingPolicyTypeRef}.{namingPolicyName}"
                    : null;

                return $@"
private static {JsonSerializerOptionsTypeRef} {DefaultOptionsStaticVarName} {{ get; }} = new {JsonSerializerOptionsTypeRef}()
{{
    DefaultIgnoreCondition = {JsonIgnoreConditionTypeRef}.{options.DefaultIgnoreCondition},
    IgnoreReadOnlyFields = {FormatBool(options.IgnoreReadOnlyFields)},
    IgnoreReadOnlyProperties = {FormatBool(options.IgnoreReadOnlyProperties)},
    IncludeFields = {FormatBool(options.IncludeFields)},
    WriteIndented = {FormatBool(options.WriteIndented)},{namingPolicyInit}
}};";
            }

            private static string GetFetchLogicForRuntimeSpecifiedCustomConverter()
            {
                // TODO (https://github.com/dotnet/runtime/issues/52218): use a dictionary if count > ~15.
                return @$"private static {JsonConverterTypeRef}? {RuntimeCustomConverterFetchingMethodName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName}, {TypeTypeRef} type)
{{
    {IListTypeRef}<{JsonConverterTypeRef}> converters = {OptionsLocalVariableName}.Converters;

    for (int i = 0; i < converters.Count; i++)
    {{
        {JsonConverterTypeRef}? converter = converters[i];

        if (converter.CanConvert(type))
        {{
            if (converter is {JsonConverterFactoryTypeRef} factory)
            {{
                converter = factory.CreateConverter(type, {OptionsLocalVariableName});
                if (converter == null || converter is {JsonConverterFactoryTypeRef})
                {{
                    throw new {InvalidOperationExceptionTypeRef}(string.Format(""{ExceptionMessages.InvalidJsonConverterFactoryOutput}"", factory.GetType()));
                }}
            }}

            return converter;
        }}
    }}

    return null;
}}";
            }

            private static string GetFetchLogicForGetCustomConverter_PropertiesWithFactories()
            {
                return @$"

private static {JsonConverterTypeRef}<T> {GetConverterFromFactoryMethodName}<T>({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName}, {JsonConverterFactoryTypeRef} factory)
{{
    return ({JsonConverterTypeRef}<T>) {GetConverterFromFactoryMethodName}({OptionsLocalVariableName}, typeof(T), factory);
}}";
            }

            private static string GetFetchLogicForGetCustomConverter_TypesWithFactories()
            {
                return @$"

private static {JsonConverterTypeRef} {GetConverterFromFactoryMethodName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName}, {TypeTypeRef} type, {JsonConverterFactoryTypeRef} factory)
{{
    {JsonConverterTypeRef}? converter = factory.CreateConverter(type, {OptionsLocalVariableName});
    if (converter == null || converter is {JsonConverterFactoryTypeRef})
    {{
        throw new {InvalidOperationExceptionTypeRef}(string.Format(""{ExceptionMessages.InvalidJsonConverterFactoryOutput}"", factory.GetType()));
    }}

    return converter;
}}";
            }

            private string GetGetTypeInfoImplementation()
            {
                StringBuilder sb = new();

                // JsonSerializerContext.GetTypeInfo override -- returns cached metadata via JsonSerializerOptions
                sb.Append(
@$"/// <inheritdoc/>
public override {JsonTypeInfoTypeRef}? GetTypeInfo({TypeTypeRef} type)
{{
    {OptionsInstanceVariableName}.TryGetTypeInfo(type, out {JsonTypeInfoTypeRef}? typeInfo);
    return typeInfo;
}}
");
                // Explicit IJsonTypeInfoResolver implementation -- the source of truth for metadata resolution
                sb.AppendLine();
                sb.Append(@$"{JsonTypeInfoTypeRef}? {JsonTypeInfoResolverTypeRef}.GetTypeInfo({TypeTypeRef} type, {JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})
{{");
                foreach (TypeGenerationSpec metadata in _currentContext.TypesWithMetadataGenerated)
                {
                    if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                    {
                        sb.Append($@"
    if (type == typeof({metadata.TypeRef}))
    {{
        return {metadata.CreateTypeInfoMethodName}({OptionsLocalVariableName});
    }}
");
                    }
                }

                sb.Append($@"
    return null;
}}
");

                return sb.ToString();
            }

            private string GetPropertyNameInitialization()
            {
                // Ensure metadata for types has already occurred.
                Debug.Assert(!(
                    _currentContext.TypesWithMetadataGenerated.Count == 0
                    && _currentContext.RuntimePropertyNames.Count > 0));

                StringBuilder sb = new();

                foreach (KeyValuePair<string, string> name_varName_pair in _currentContext.RuntimePropertyNames)
                {
                    sb.Append($@"
private static readonly {JsonEncodedTextTypeRef} {name_varName_pair.Value} = {JsonEncodedTextTypeRef}.Encode(""{name_varName_pair.Key}"");");
                }

                return sb.ToString();
            }

            private static string IndentSource(string source, int numIndentations)
            {
                if (numIndentations == 0)
                {
                    return source;
                }

                string indentation = new string(' ', 4 * numIndentations); // 4 spaces per indentation.
                return indentation + source.Replace(Environment.NewLine, Environment.NewLine + indentation);
            }

            private static string GetNumberHandlingAsStr(JsonNumberHandling? numberHandling) =>
                 numberHandling.HasValue
                    ? $"({JsonNumberHandlingTypeRef}){(int)numberHandling.Value}"
                    : "default";

            private static string GetUnmappedMemberHandlingAsStr(JsonUnmappedMemberHandling unmappedMemberHandling) =>
                $"({JsonUnmappedMemberHandlingTypeRef}){(int)unmappedMemberHandling}";

            private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";

            private static string FormatBool(bool value) => value ? "true" : "false";

            private
#if !DEBUG
                static
#endif
                string GetParamDefaultValueAsString(object? value, Type type, string typeRef)
            {
                if (value == null)
                {
                    return $"default({typeRef})";
                }

                if (type.IsEnum)
                {
                    // Roslyn gives us an instance of the underlying type, which is numerical.
#if DEBUG
                    Type? runtimeType = _generationSpec.MetadataLoadContext.Resolve(value.GetType()!);
                    Debug.Assert(runtimeType != null);
                    Debug.Assert(_generationSpec.IsNumberType(runtimeType));
#endif

                    // Return the numeric value.
                    return FormatNumber();
                }

                switch (value)
                {
                    case string @string:
                        return SymbolDisplay.FormatLiteral(@string, quote: true); ;
                    case char @char:
                        return SymbolDisplay.FormatLiteral(@char, quote: true);
                    case double.NegativeInfinity:
                        return "double.NegativeInfinity";
                    case double.PositiveInfinity:
                        return "double.PositiveInfinity";
                    case double.NaN:
                        return "double.NaN";
                    case double @double:
                        return $"({typeRef})({@double.ToString(JsonConstants.DoubleFormatString, CultureInfo.InvariantCulture)})";
                    case float.NegativeInfinity:
                        return "float.NegativeInfinity";
                    case float.PositiveInfinity:
                        return "float.PositiveInfinity";
                    case float.NaN:
                        return "float.NaN";
                    case float @float:
                        return $"({typeRef})({@float.ToString(JsonConstants.SingleFormatString, CultureInfo.InvariantCulture)})";
                    case decimal.MaxValue:
                        return "decimal.MaxValue";
                    case decimal.MinValue:
                        return "decimal.MinValue";
                    case decimal @decimal:
                        return @decimal.ToString(CultureInfo.InvariantCulture);
                    case bool @bool:
                        return FormatBool(@bool);
                    default:
                        // Assume this is a number.
                        return FormatNumber();
                }

                string FormatNumber() => $"({typeRef})({Convert.ToString(value, CultureInfo.InvariantCulture)})";
            }
        }
    }
}
