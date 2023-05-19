// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
            private const string PreferredPropertyObjectCreationHandlingPropName = "PreferredPropertyObjectCreationHandling";
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
            private const string JsonObjectCreationHandlingTypeRef = "global::System.Text.Json.Serialization.JsonObjectCreationHandling";
            private const string JsonUnmappedMemberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonUnmappedMemberHandling";
            private const string JsonMetadataServicesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonMetadataServices";
            private const string JsonObjectInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues";
            private const string JsonParameterInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonParameterInfoValues";
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonPropertyInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";
            private const string JsonTypeInfoResolverTypeRef = "global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver";

            private readonly JsonSourceGenerationContext _sourceGenerationContext;

            private ContextGenerationSpec _currentContext = null!;

            private readonly SourceGenerationSpec _generationSpec;

            /// <summary>
            /// Contains an index from TypeRef to TypeGenerationSpec for the current ContextGenerationSpec.
            /// </summary>
            private readonly Dictionary<TypeRef, TypeGenerationSpec> _typeIndex = new();

            /// <summary>
            /// Cache of runtime property names (statically determined) found across the type graph of the JsonSerializerContext.
            /// The dictionary Key is the JSON property name, and the Value is the variable name which is the same as the property
            /// name except for cases where special characters are used with [JsonPropertyName].
            /// </summary>
            private readonly Dictionary<string, string> _runtimePropertyNames = new();

            private bool _generateGetConverterMethodForTypes;
            private bool _generateGetConverterMethodForProperties;

            public Emitter(in JsonSourceGenerationContext sourceGenerationContext, SourceGenerationSpec generationSpec)
            {
                _sourceGenerationContext = sourceGenerationContext;
                _generationSpec = generationSpec;
            }

            public void Emit()
            {
                foreach (Diagnostic diagnostic in _generationSpec.Diagnostics)
                {
                    // Emit any diagnostics produced by the parser ahead of formatting source code.
                    _sourceGenerationContext.ReportDiagnostic(diagnostic);
                }

                foreach (ContextGenerationSpec contextGenerationSpec in _generationSpec.ContextGenerationSpecs)
                {
                    _currentContext = contextGenerationSpec;
                    _generateGetConverterMethodForTypes = false;
                    _generateGetConverterMethodForProperties = false;

                    Debug.Assert(_typeIndex.Count == 0);

                    foreach (TypeGenerationSpec spec in _currentContext.GeneratedTypes)
                    {
                        _typeIndex.Add(spec.TypeRef, spec);
                    }

                    foreach (TypeGenerationSpec typeGenerationSpec in _currentContext.GeneratedTypes)
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
                    AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation(contextGenerationSpec), interfaceImplementation: JsonTypeInfoResolverTypeRef);

                    // Add property name initialization.
                    AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization());

                    _typeIndex.Clear();
                }
            }

            private void AddSource(string fileName, string source, bool isRootContextDef = false, string? interfaceImplementation = null)
            {
                string? generatedCodeAttributeSource = isRootContextDef ? s_generatedCodeAttributeSource : null;

                ImmutableEquatableArray<string> declarationList = _currentContext.ContextClassDeclarations;
                int declarationCount = declarationList.Count;
                Debug.Assert(declarationCount >= 1);

                string? @namespace = _currentContext.Namespace;
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

                string source;

                switch (typeGenerationSpec.ClassType)
                {
                    case ClassType.BuiltInSupportType:
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
                            Debug.Assert(typeGenerationSpec.NullableUnderlyingType != null);

                            source = GenerateForNullable(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Enum:
                        {
                            source = GenerateForEnum(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Enumerable:
                        {
                            Debug.Assert(typeGenerationSpec.CollectionValueType != null);

                            source = GenerateForCollection(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Dictionary:
                        {
                            source = GenerateForCollection(typeGenerationSpec);
                        }
                        break;
                    case ClassType.Object:
                        {
                            source = GenerateForObject(typeGenerationSpec);
                        }
                        break;
                    case ClassType.UnsupportedType:
                        {
                            source = GenerateForUnsupportedType(typeGenerationSpec);
                        }
                        break;
                    case ClassType.TypeUnsupportedBySourceGen:
                        return; // Do not emit a file for the type.
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }

                string propertyFileName = $"{_currentContext.ContextType.Name}.{typeGenerationSpec.TypeInfoPropertyName}.g.cs";
                AddSource(propertyFileName, source);

                _generateGetConverterMethodForTypes |= typeGenerationSpec.HasTypeFactoryConverter;
                _generateGetConverterMethodForProperties |= typeGenerationSpec.HasPropertyFactoryConverters;
            }

            private static string GenerateForTypeWithKnownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $@"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.{typeFriendlyName}Converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForTypeWithUnknownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;

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
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;

                TypeRef? underlyingTypeMetadata = typeMetadata.NullableUnderlyingType;
                Debug.Assert(underlyingTypeMetadata != null);

                string underlyingTypeCompilableName = underlyingTypeMetadata.FullyQualifiedName;

                string metadataInitSource = @$"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}(
                    {OptionsLocalVariableName},
                    {JsonMetadataServicesTypeRef}.GetNullableConverter<{underlyingTypeCompilableName}>({OptionsLocalVariableName}));
";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForUnsupportedType(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;

                string metadataInitSource = $"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetUnsupportedTypeConverter<{typeCompilableName}>());";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private static string GenerateForEnum(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;

                string metadataInitSource = $"{JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetEnumConverter<{typeCompilableName}>({OptionsLocalVariableName}));";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForCollection(TypeGenerationSpec typeGenerationSpec)
            {
                // Key metadata
                TypeRef? collectionKeyTypeMetadata = typeGenerationSpec.CollectionKeyType;
                Debug.Assert(!(typeGenerationSpec.ClassType == ClassType.Dictionary && collectionKeyTypeMetadata == null));
                string? keyTypeCompilableName = collectionKeyTypeMetadata?.FullyQualifiedName;

                // Value metadata
                TypeRef? collectionValueTypeMetadata = typeGenerationSpec.CollectionValueType;
                Debug.Assert(collectionValueTypeMetadata != null);
                string valueTypeCompilableName = collectionValueTypeMetadata.FullyQualifiedName;

                string numberHandlingArg = $"{GetNumberHandlingAsStr(typeGenerationSpec.NumberHandling)}";

                string serializeHandlerValue;

                string? serializeHandlerSource;
                if (!ShouldGenerateSerializationLogic(typeGenerationSpec))
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

                string typeRef = typeGenerationSpec.TypeRef.FullyQualifiedName;

                string objectCreatorValue;
                if (typeGenerationSpec.RuntimeTypeRef != null)
                {
                    objectCreatorValue = $"() => new {typeGenerationSpec.RuntimeTypeRef}()";
                }
                else if (typeGenerationSpec.IsValueTuple)
                {
                    objectCreatorValue = $"() => default({typeRef})";
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
                string immutableCollectionCreationSuffix = $"createRangeFunc: {typeGenerationSpec.ImmutableCollectionFactoryMethod}";

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
                Debug.Assert(typeGenerationSpec.CollectionValueType != null);

                TypeGenerationSpec valueTypeGenerationSpec = _typeIndex[typeGenerationSpec.CollectionValueType];
                string? writerMethodToCall = GetWriterMethod(valueTypeGenerationSpec);

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
                        iterationLogic = $"foreach ({valueTypeGenerationSpec.TypeRef.FullyQualifiedName} {elementVarName} in {ValueVarName})";
                        valueToWrite = elementVarName;
                        break;
                };

                if (valueTypeGenerationSpec.PrimitiveTypeKind is JsonPrimitiveTypeKind.Char)
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

                return GenerateFastPathFuncForType(typeGenerationSpec, serializationLogic, emitNullCheck: typeGenerationSpec.TypeRef.CanBeNull);
            }

            private string GenerateFastPathFuncForDictionary(TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CollectionKeyType != null);
                Debug.Assert(typeGenerationSpec.CollectionValueType != null);

                TypeRef keyType = typeGenerationSpec.CollectionKeyType;
                TypeGenerationSpec valueTypeGenerationSpec = _typeIndex[typeGenerationSpec.CollectionValueType];

                string? writerMethodToCall = GetWriterMethod(valueTypeGenerationSpec);
                string elementSerializationLogic;

                const string pairVarName = "pair";
                string keyToWrite = $"{pairVarName}.Key";
                string valueToWrite = $"{pairVarName}.Value";

                if (valueTypeGenerationSpec.PrimitiveTypeKind is JsonPrimitiveTypeKind.Char)
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

    foreach ({KeyValuePairTypeRef}<{keyType.FullyQualifiedName}, {valueTypeGenerationSpec.TypeRef.FullyQualifiedName}> {pairVarName} in {ValueVarName})
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndObject();";

                return GenerateFastPathFuncForType(typeGenerationSpec, serializationLogic, emitNullCheck: typeGenerationSpec.TypeRef.CanBeNull);
            }

            private string GenerateForObject(TypeGenerationSpec typeMetadata)
            {
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                ObjectConstructionStrategy constructionStrategy = typeMetadata.ConstructionStrategy;

                string creatorInvocation = (typeMetadata.IsValueTuple, constructionStrategy) switch
                {
                    (true, _) => $"static () => default({typeMetadata.TypeRef.FullyQualifiedName})",
                    (false, ObjectConstructionStrategy.ParameterlessConstructor) => $"static () => new {typeMetadata.TypeRef.FullyQualifiedName}()",
                    _ => "null",
                };


                string parameterizedCreatorInvocation = constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor
                    ? GetParameterizedCtorInvocationFunc(typeMetadata)
                    : "null";

                string? propMetadataInitFuncSource = null;
                string? ctorParamMetadataInitFuncSource = null;
                string? serializeFuncSource = null;

                string propInitMethod = "null";
                string ctorParamMetadataInitMethodName = "null";
                string serializeMethodName = "null";

                if (ShouldGenerateMetadata(typeMetadata))
                {
                    propMetadataInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata);
                    propInitMethod = $"_ => {typeFriendlyName}{PropInitMethodNameSuffix}({OptionsLocalVariableName})";

                    if (constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        ctorParamMetadataInitFuncSource = GenerateCtorParamMetadataInitFunc(typeMetadata);
                        ctorParamMetadataInitMethodName = $"{typeFriendlyName}{CtorParamInitMethodNameSuffix}";
                    }
                }

                if (ShouldGenerateSerializationLogic(typeMetadata))
                {
                    serializeFuncSource = GenerateFastPathFuncForObject(typeMetadata);
                    serializeMethodName = $"{typeFriendlyName}{SerializeHandlerPropName}";
                }

                const string ObjectInfoVarName = "objectInfo";
                string genericArg = typeMetadata.TypeRef.FullyQualifiedName;

                string objectInfoInitSource = $@"{JsonObjectInfoValuesTypeRef}<{genericArg}> {ObjectInfoVarName} = new {JsonObjectInfoValuesTypeRef}<{genericArg}>()
        {{
            {ObjectCreatorPropName} = {creatorInvocation},
            ObjectWithParameterizedConstructorCreator = {parameterizedCreatorInvocation},
            PropertyMetadataInitializer = {propInitMethod},
            ConstructorParameterMetadataInitializer = {ctorParamMetadataInitMethodName},
            {NumberHandlingPropName} = {GetNumberHandlingAsStr(typeMetadata.NumberHandling)},
            {SerializeHandlerPropName} = {serializeMethodName}
        }};

        {JsonTypeInfoReturnValueLocalVariableName} = {JsonMetadataServicesTypeRef}.CreateObjectInfo<{typeMetadata.TypeRef.FullyQualifiedName}>({OptionsLocalVariableName}, {ObjectInfoVarName});";

                if (typeMetadata.UnmappedMemberHandling != null)
                {
                    objectInfoInitSource += $"""

        {JsonTypeInfoReturnValueLocalVariableName}.{UnmappedMemberHandlingPropName} = {GetUnmappedMemberHandlingAsStr(typeMetadata.UnmappedMemberHandling.Value)};
""";
                }

                if (typeMetadata.PreferredPropertyObjectCreationHandling != null)
                {
                    objectInfoInitSource += $"""

        {JsonTypeInfoReturnValueLocalVariableName}.{PreferredPropertyObjectCreationHandlingPropName} = {GetObjectCreationHandlingAsStr(typeMetadata.PreferredPropertyObjectCreationHandling.Value)};
""";
                }

                string additionalSource = @$"{propMetadataInitFuncSource}{serializeFuncSource}{ctorParamMetadataInitFuncSource}";

                return GenerateForType(typeMetadata, objectInfoInitSource, additionalSource);
            }

            private static string GeneratePropMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string PropVarName = "properties";

                ImmutableEquatableArray<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecs!;

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
                    string nameSpecifiedInSourceCode = memberMetadata.NameSpecifiedInSourceCode;

                    string declaringTypeCompilableName = memberMetadata.DeclaringTypeRef;

                    string jsonPropertyNameValue = memberMetadata.JsonPropertyName != null
                        ? @$"""{memberMetadata.JsonPropertyName}"""
                        : "null";

                    string getterValue = memberMetadata switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseGetter: true } => $"static (obj) => (({declaringTypeCompilableName})obj).{nameSpecifiedInSourceCode}",
                        { CanUseGetter: false, HasJsonInclude: true }
                            => @$"static (obj) => throw new {InvalidOperationExceptionTypeRef}(""{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.TypeRef.Name, nameSpecifiedInSourceCode)}"")",
                        _ => "null"
                    };

                    string setterValue = memberMetadata switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseSetter: true, IsInitOnlySetter: true }
                            => @$"static (obj, value) => throw new {InvalidOperationExceptionTypeRef}(""{ExceptionMessages.InitOnlyPropertySetterNotSupported}"")",
                        { CanUseSetter: true } when typeGenerationSpec.TypeRef.IsValueType
                            => $@"static (obj, value) => {UnsafeTypeRef}.Unbox<{declaringTypeCompilableName}>(obj).{nameSpecifiedInSourceCode} = value!",
                        { CanUseSetter: true }
                            => @$"static (obj, value) => (({declaringTypeCompilableName})obj).{nameSpecifiedInSourceCode} = value!",
                        { CanUseSetter: false, HasJsonInclude: true }
                            => @$"static (obj, value) => throw new {InvalidOperationExceptionTypeRef}(""{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.TypeRef.Name, memberMetadata.MemberName)}"")",
                        _ => "null",
                    };

                    JsonIgnoreCondition? ignoreCondition = memberMetadata.DefaultIgnoreCondition;
                    string ignoreConditionNamedArg = ignoreCondition.HasValue
                        ? $"{JsonIgnoreConditionTypeRef}.{ignoreCondition.Value}"
                        : "null";

                    string converterValue = memberMetadata.ConverterInstantiationLogic == null
                        ? "null"
                        : $"{memberMetadata.ConverterInstantiationLogic}";

                    string memberTypeCompilableName = memberMetadata.PropertyType.FullyQualifiedName;

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
        PropertyName = ""{memberMetadata.MemberName}"",
        JsonPropertyName = {jsonPropertyNameValue}
    }};

    {JsonPropertyInfoTypeRef} {propertyInfoVarName} = {JsonMetadataServicesTypeRef}.CreatePropertyInfo<{memberTypeCompilableName}>({OptionsLocalVariableName}, {infoVarName});");

                    if (memberMetadata.HasJsonRequiredAttribute ||
                        (memberMetadata.IsRequired && !typeGenerationSpec.ConstructorSetsRequiredParameters))
                    {
                        sb.Append($@"
    {propertyInfoVarName}.IsRequired = true;");
                    }

                    if (memberMetadata.ObjectCreationHandling != null)
                    {
                        sb.Append($@"
    {propertyInfoVarName}.ObjectCreationHandling = {GetObjectCreationHandlingAsStr(memberMetadata.ObjectCreationHandling.Value)};");
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

            private static string GenerateCtorParamMetadataInitFunc(TypeGenerationSpec typeGenerationSpec)
            {
                const string parametersVarName = "parameters";

                Debug.Assert(typeGenerationSpec.CtorParamGenSpecs != null);

                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec>? propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;
                int paramCount = parameters.Count + (propertyInitializers?.Count(propInit => !propInit.MatchesConstructorParameter) ?? 0);
                Debug.Assert(paramCount > 0);

                StringBuilder sb = new($@"

private static {JsonParameterInfoValuesTypeRef}[] {typeGenerationSpec.TypeInfoPropertyName}{CtorParamInitMethodNameSuffix}()
{{
    {JsonParameterInfoValuesTypeRef}[] {parametersVarName} = new {JsonParameterInfoValuesTypeRef}[{paramCount}];
    {JsonParameterInfoValuesTypeRef} info;
");
                foreach (ParameterGenerationSpec spec in parameters)
                {
                    string parameterTypeRef = spec.ParameterType.FullyQualifiedName;

                    object? defaultValue = spec.DefaultValue;
                    string defaultValueAsStr = GetParamDefaultValueAsString(defaultValue, spec.ParameterType);

                    sb.Append(@$"
    {InfoVarName} = new()
    {{
        Name = ""{spec.Name}"",
        ParameterType = typeof({parameterTypeRef}),
        Position = {spec.ParameterIndex},
        HasDefaultValue = {FormatBool(spec.HasDefaultValue)},
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
        Name = ""{spec.Property.MemberName}"",
        ParameterType = typeof({spec.Property.PropertyType.FullyQualifiedName}),
        Position = {spec.ParameterIndex},
        HasDefaultValue = false,
        DefaultValue = default({spec.Property.PropertyType.FullyQualifiedName}),
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
                string typeRef = typeGenSpec.TypeRef.FullyQualifiedName;
                ContextGenerationSpec contextSpec = _currentContext;

                if (!TryFilterSerializableProps(
                    typeGenSpec,
                    contextSpec,
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
                foreach (PropertyGenerationSpec propertyGenSpec in serializableProperties.Values)
                {
                    TypeGenerationSpec propertyTypeSpec = _typeIndex[propertyGenSpec.PropertyType];

                    if (!ShouldIncludePropertyForFastPath(propertyGenSpec, propertyTypeSpec, contextSpec))
                    {
                        continue;
                    }


                    string runtimePropName = propertyGenSpec.RuntimePropertyName;
                    string propVarName = propertyGenSpec.PropertyNameVarName;

                    // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                    Debug.Assert(!_runtimePropertyNames.TryGetValue(runtimePropName, out string? existingName) || existingName == propVarName);
                    _runtimePropertyNames.TryAdd(runtimePropName, propVarName);

                    string? objectRef = castingRequiredForProps ? $"(({propertyGenSpec.DeclaringTypeRef}){ValueVarName})" : ValueVarName;
                    string propValue = $"{objectRef}.{propertyGenSpec.NameSpecifiedInSourceCode}";
                    string methodArgs = $"{propVarName}, {propValue}";

                    string? methodToCall = GetWriterMethod(propertyTypeSpec);

                    if (propertyTypeSpec.PrimitiveTypeKind is JsonPrimitiveTypeKind.Char)
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

                    JsonIgnoreCondition ignoreCondition = propertyGenSpec.DefaultIgnoreCondition ?? contextSpec.DefaultIgnoreCondition;
                    DefaultCheckType defaultCheckType;
                    bool typeCanBeNull = propertyTypeSpec.TypeRef.CanBeNull;

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

                    sb.Append(WrapSerializationLogicInDefaultCheckIfRequired(serializationLogic, propValue, propertyGenSpec.PropertyType.FullyQualifiedName, defaultCheckType));
                }

                // End method logic.
                sb.Append($@"

    {WriterVarName}.WriteEndObject();");

                if (typeGenSpec.ImplementsIJsonOnSerialized)
                {
                    sb.Append($@"{Environment.NewLine}    ");
                    sb.Append($@"((global::{JsonConstants.IJsonOnSerializedFullName}){ValueVarName}).OnSerialized();");
                };

                return GenerateFastPathFuncForType(typeGenSpec, sb.ToString(), emitNullCheck: typeGenSpec.TypeRef.CanBeNull);
            }

            private static bool ShouldIncludePropertyForFastPath(PropertyGenerationSpec propertyGenSpec, TypeGenerationSpec propertyTypeSpec, ContextGenerationSpec contextSpec)
            {
                if (propertyTypeSpec.ClassType == ClassType.TypeUnsupportedBySourceGen || !propertyGenSpec.CanUseGetter)
                {
                    return false;
                }

                if (!propertyGenSpec.IsProperty && !propertyGenSpec.HasJsonInclude && !contextSpec.IncludeFields)
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
                        if (contextSpec.IgnoreReadOnlyProperties)
                        {
                            return false;
                        }
                    }
                    else if (contextSpec.IgnoreReadOnlyFields)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static string GetParameterizedCtorInvocationFunc(TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CtorParamGenSpecs != null);
                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec>? propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;

                const string ArgsVarName = "args";

                StringBuilder sb = new($"static ({ArgsVarName}) => new {typeGenerationSpec.TypeRef.FullyQualifiedName}(");

                if (parameters.Count > 0)
                {
                    foreach (ParameterGenerationSpec param in parameters)
                    {
                        int index = param.ParameterIndex;
                        sb.Append($"{GetParamUnboxing(param.ParameterType, index)}, ");
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
                        sb.Append($"{property.Property.MemberName} = {GetParamUnboxing(property.Property.PropertyType, property.ParameterIndex)}, ");
                    }

                    sb.Length -= 2; // delete the last ", " token
                    sb.Append(" }");
                }

                return sb.ToString();

                static string GetParamUnboxing(TypeRef type, int index)
                    => $"({type.FullyQualifiedName}){ArgsVarName}[{index}]";
            }

            private static string? GetWriterMethod(TypeGenerationSpec type)
            {
                return type.PrimitiveTypeKind switch
                {
                    JsonPrimitiveTypeKind.Number => $"{WriterVarName}.WriteNumber",
                    JsonPrimitiveTypeKind.String or JsonPrimitiveTypeKind.Char => $"{WriterVarName}.WriteString",
                    JsonPrimitiveTypeKind.Boolean => $"{WriterVarName}.WriteBoolean",
                    JsonPrimitiveTypeKind.ByteArray => $"{WriterVarName}.WriteBase64String",
                    _ => null
                };
            }

            private static string GenerateFastPathFuncForType(TypeGenerationSpec typeGenSpec, string serializeMethodBody, bool emitNullCheck)
            {
                Debug.Assert(!emitNullCheck || typeGenSpec.TypeRef.CanBeNull);

                string serializeMethodName = $"{typeGenSpec.TypeInfoPropertyName}{SerializeHandlerPropName}";
                // fast path serializers for reference types always support null inputs.
                string valueTypeRef = $"{typeGenSpec.TypeRef.FullyQualifiedName}{(typeGenSpec.TypeRef.IsValueType ? "" : "?")}";

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
                if (ShouldGenerateSerializationLogic(typeGenerationSpec))
                {
                    return $"{typeGenerationSpec.TypeInfoPropertyName}{SerializeHandlerPropName}({WriterVarName}, {valueExpr});";
                }

                string typeInfoRef = $"{_currentContext.ContextType.FullyQualifiedName}.Default.{typeGenerationSpec.TypeInfoPropertyName}!";
                return $"{JsonSerializerTypeRef}.Serialize({WriterVarName}, {valueExpr}, {typeInfoRef});";
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
                string typeCompilableName = typeMetadata.TypeRef.FullyQualifiedName;
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

private {typeInfoPropertyTypeRef} {CreateTypeInfoMethodName(typeMetadata)}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})
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
                string contextTypeRef = _currentContext.ContextType.FullyQualifiedName;
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
                ContextGenerationSpec contextSpec = _currentContext;

                string? namingPolicyName = contextSpec.PropertyNamingPolicy switch
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
    DefaultIgnoreCondition = {JsonIgnoreConditionTypeRef}.{contextSpec.DefaultIgnoreCondition},
    IgnoreReadOnlyFields = {FormatBool(contextSpec.IgnoreReadOnlyFields)},
    IgnoreReadOnlyProperties = {FormatBool(contextSpec.IgnoreReadOnlyProperties)},
    IncludeFields = {FormatBool(contextSpec.IncludeFields)},
    WriteIndented = {FormatBool(contextSpec.WriteIndented)},{namingPolicyInit}
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

            private static string GetGetTypeInfoImplementation(ContextGenerationSpec contextGenerationSpec)
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
                foreach (TypeGenerationSpec metadata in contextGenerationSpec.GeneratedTypes)
                {
                    if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                    {
                        sb.Append($@"
    if (type == typeof({metadata.TypeRef.FullyQualifiedName}))
    {{
        return {CreateTypeInfoMethodName(metadata)}({OptionsLocalVariableName});
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

                StringBuilder sb = new();

                foreach (KeyValuePair<string, string> name_varName_pair in _runtimePropertyNames)
                {
                    sb.Append($@"
private static readonly {JsonEncodedTextTypeRef} {name_varName_pair.Value} = {JsonEncodedTextTypeRef}.Encode(""{name_varName_pair.Key}"");");
                }

                _runtimePropertyNames.Clear(); // Clear the cache for the next context.
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
                numberHandling switch
                {
                    null => "default",
                    >= 0 => $"({JsonNumberHandlingTypeRef}){(int)numberHandling.Value}",
                    < 0 => $"({JsonNumberHandlingTypeRef})({(int)numberHandling.Value})"
                };

            private static string GetObjectCreationHandlingAsStr(JsonObjectCreationHandling creationHandling) =>
                creationHandling >= 0
                ? $"({JsonObjectCreationHandlingTypeRef}){(int)creationHandling}"
                : $"({JsonObjectCreationHandlingTypeRef})({(int)creationHandling})";

            private static string GetUnmappedMemberHandlingAsStr(JsonUnmappedMemberHandling unmappedMemberHandling) =>
                unmappedMemberHandling >= 0
                ? $"({JsonUnmappedMemberHandlingTypeRef}){(int)unmappedMemberHandling}"
                : $"({JsonUnmappedMemberHandlingTypeRef})({(int)unmappedMemberHandling})";

            private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";

            private static string FormatBool(bool value) => value ? "true" : "false";

            /// <summary>
            /// Method used to generate JsonTypeInfo given options instance
            /// </summary>
            private static string CreateTypeInfoMethodName(TypeGenerationSpec typeSpec)
                => $"Create_{typeSpec.TypeInfoPropertyName}";

            private static string GetParamDefaultValueAsString(object? value, TypeRef type)
            {
                if (value == null)
                {
                    return $"default({type.FullyQualifiedName})";
                }

                if (type.TypeKind is TypeKind.Enum)
                {
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
                        return $"({type.FullyQualifiedName})({@double.ToString(JsonConstants.DoubleFormatString, CultureInfo.InvariantCulture)})";
                    case float.NegativeInfinity:
                        return "float.NegativeInfinity";
                    case float.PositiveInfinity:
                        return "float.PositiveInfinity";
                    case float.NaN:
                        return "float.NaN";
                    case float @float:
                        return $"({type.FullyQualifiedName})({@float.ToString(JsonConstants.SingleFormatString, CultureInfo.InvariantCulture)})";
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

                string FormatNumber() => $"({type.FullyQualifiedName})({Convert.ToString(value, CultureInfo.InvariantCulture)})";
            }

            private static bool ShouldGenerateMetadata(TypeGenerationSpec typeSpec)
                => IsGenerationModeSpecified(typeSpec, JsonSourceGenerationMode.Metadata);

            private static bool ShouldGenerateSerializationLogic(TypeGenerationSpec typeSpec)
                => IsGenerationModeSpecified(typeSpec, JsonSourceGenerationMode.Serialization) && IsFastPathSupported(typeSpec);

            private static bool IsGenerationModeSpecified(TypeGenerationSpec typeSpec, JsonSourceGenerationMode mode)
                => typeSpec.GenerationMode == JsonSourceGenerationMode.Default || (mode & typeSpec.GenerationMode) != 0;

            public static bool TryFilterSerializableProps(
                    TypeGenerationSpec typeSpec,
                    ContextGenerationSpec contextSpec,
                    [NotNullWhen(true)] out Dictionary<string, PropertyGenerationSpec>? serializableProperties,
                    out bool castingRequiredForProps)
            {
                Debug.Assert(typeSpec.PropertyGenSpecs != null);

                castingRequiredForProps = false;
                serializableProperties = new Dictionary<string, PropertyGenerationSpec>();
                HashSet<string>? ignoredMembers = null;

                for (int i = 0; i < typeSpec.PropertyGenSpecs.Count; i++)
                {
                    PropertyGenerationSpec propGenSpec = typeSpec.PropertyGenSpecs[i];
                    JsonIgnoreCondition? ignoreCondition = propGenSpec.DefaultIgnoreCondition;

                    if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull && !propGenSpec.PropertyType.CanBeNull)
                    {
                        goto ReturnFalse;
                    }

                    // In case of JsonInclude fail if either:
                    // 1. the getter is not accessible by the source generator or
                    // 2. neither getter or setter methods are public.
                    if (propGenSpec.HasJsonInclude && (!propGenSpec.CanUseGetter || !propGenSpec.IsPublic))
                    {
                        goto ReturnFalse;
                    }

                    // Discard any getters not accessible by the source generator.
                    if (!propGenSpec.CanUseGetter)
                    {
                        continue;
                    }

                    if (!propGenSpec.IsProperty && !propGenSpec.HasJsonInclude && !contextSpec.IncludeFields)
                    {
                        continue;
                    }

                    // Using properties from an interface hierarchy -- require explicit casting when
                    // getting properties in the fast path to account for possible diamond ambiguities.
                    castingRequiredForProps |= typeSpec.TypeRef.TypeKind is TypeKind.Interface && propGenSpec.PropertyType != typeSpec.TypeRef;

                    string memberName = propGenSpec.MemberName!;

                    // The JsonPropertyNameAttribute or naming policy resulted in a collision.
                    if (!serializableProperties.TryAdd(propGenSpec.RuntimePropertyName, propGenSpec))
                    {
                        PropertyGenerationSpec other = serializableProperties[propGenSpec.RuntimePropertyName]!;

                        if (other.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                        {
                            // Overwrite previously cached property since it has [JsonIgnore].
                            serializableProperties[propGenSpec.RuntimePropertyName] = propGenSpec;
                        }
                        else
                        {
                            bool ignoreCurrentProperty;

                            if (typeSpec.TypeRef.TypeKind is not TypeKind.Interface)
                            {
                                ignoreCurrentProperty =
                                    // Does the current property have `JsonIgnoreAttribute`?
                                    propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always ||
                                    // Is the current property hidden by the previously cached property
                                    // (with `new` keyword, or by overriding)?
                                    other.MemberName == memberName ||
                                    // Was a property with the same CLR name ignored? That property hid the current property,
                                    // thus, if it was ignored, the current property should be ignored too.
                                    ignoredMembers?.Contains(memberName) == true;
                            }
                            else
                            {
                                // Unlike classes, interface hierarchies reject all naming conflicts for non-ignored properties.
                                // Conflicts like this are possible in two cases:
                                // 1. Diamond ambiguity in property names, or
                                // 2. Linear interface hierarchies that use properties with DIMs.
                                //
                                // Diamond ambiguities are not supported. Assuming there is demand, we might consider
                                // adding support for DIMs in the future, however that would require adding more APIs
                                // for the case of source gen.

                                ignoreCurrentProperty = propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always;
                            }

                            if (!ignoreCurrentProperty)
                            {
                                // We have a conflict, emit a stub method that throws.
                                goto ReturnFalse;
                            }
                        }
                    }

                    if (propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                    {
                        (ignoredMembers ??= new()).Add(memberName);
                    }
                }

                Debug.Assert(typeSpec.PropertyGenSpecs.Count >= serializableProperties.Count);
                castingRequiredForProps |= typeSpec.PropertyGenSpecs.Count > serializableProperties.Count;
                return true;

            ReturnFalse:
                serializableProperties = null;
                castingRequiredForProps = false;
                return false;
            }

            private static bool IsFastPathSupported(TypeGenerationSpec typeSpec)
            {
                if (typeSpec.IsPolymorphic)
                {
                    return false;
                }

                if (typeSpec.ClassType == ClassType.Object)
                {
                    if (typeSpec.ExtensionDataPropertyType != null)
                    {
                        return false;
                    }

                    Debug.Assert(typeSpec.PropertyGenSpecs != null);

                    foreach (PropertyGenerationSpec property in typeSpec.PropertyGenSpecs)
                    {
                        if (property.PropertyType.SpecialType is SpecialType.System_Object ||
                            property.NumberHandling == JsonNumberHandling.AllowNamedFloatingPointLiterals ||
                            property.NumberHandling == JsonNumberHandling.WriteAsString ||
                            property.ConverterInstantiationLogic is not null)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                switch (typeSpec.CollectionType)
                {
                    case CollectionType.NotApplicable:
                    case CollectionType.IAsyncEnumerableOfT:
                        return false;
                    case CollectionType.IDictionary:
                    case CollectionType.Dictionary:
                    case CollectionType.ImmutableDictionary:
                    case CollectionType.IDictionaryOfTKeyTValue:
                    case CollectionType.IReadOnlyDictionary:
                        return typeSpec.CollectionKeyType!.SpecialType is SpecialType.System_String &&
                               typeSpec.CollectionValueType!.SpecialType is not SpecialType.System_Object;
                    default:
                        // Non-dictionary collections
                        return typeSpec.CollectionValueType!.SpecialType is not SpecialType.System_Object;
                }
            }
        }
    }
}
