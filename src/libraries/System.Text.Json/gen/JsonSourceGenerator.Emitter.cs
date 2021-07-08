// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
            private const string UnsafeTypeRef = "global::System.CompilerServices.Unsafe";
            private const string NullableTypeRef = "global::System.Nullable";
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
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";

            private static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1030",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor DuplicateTypeName { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1031",
                title: new LocalizableResourceString(nameof(SR.DuplicateTypeNameTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DuplicateTypeNameMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: SystemTextJsonSourceGenerationName,
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

                StringBuilder sb = new();

                sb.Append($@"// <auto-generated/>

namespace {_currentContext.ContextType.Namespace}
{{");

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

                // Match curly brace for namespace.
                sb.AppendLine("}");

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

                            if (typeGenerationSpec.PropertiesMetadata != null)
                            {
                                foreach (PropertyGenerationSpec metadata in typeGenerationSpec.PropertiesMetadata)
                                {
                                    GenerateTypeInfo(metadata.TypeGenerationSpec);
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
                            converter = {JsonMetadataServicesTypeRef}.GetNullableConverter<{typeCompilableName}>(({JsonConverterTypeRef}<{typeCompilableName}>)actualConverter);
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
                string typeCompilableName = typeGenerationSpec.TypeRef;
                string typeFriendlyName = typeGenerationSpec.TypeInfoPropertyName;

                // Key metadata
                TypeGenerationSpec? collectionKeyTypeMetadata = typeGenerationSpec.CollectionKeyTypeMetadata;
                Debug.Assert(!(typeGenerationSpec.CollectionType == CollectionType.Dictionary && collectionKeyTypeMetadata == null));
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

                string serializeMethodName = $"{typeFriendlyName}{SerializeMethodNameSuffix}";
                string serializeFuncNamedArg;

                CollectionType collectionType = typeGenerationSpec.CollectionType;

                string? serializeFuncSource;
                if (!typeGenerationSpec.GenerateSerializationLogic)
                {
                    serializeFuncSource = null;
                    serializeFuncNamedArg = "serializeFunc: null";
                }
                else
                {
                    bool canBeNull = typeGenerationSpec.CanBeNull;

                    switch (collectionType)
                    {
                        case CollectionType.Array:
                            serializeFuncSource = GenerateFastPathFuncForEnumerable(typeCompilableName, serializeMethodName, canBeNull, isArray: true, collectionValueTypeMetadata);
                            break;
                        case CollectionType.List:
                            serializeFuncSource = GenerateFastPathFuncForEnumerable(typeCompilableName, serializeMethodName, canBeNull, isArray: false, collectionValueTypeMetadata);
                            break;
                        case CollectionType.Dictionary:
                            serializeFuncSource = GenerateFastPathFuncForDictionary(typeCompilableName, serializeMethodName, canBeNull, collectionKeyTypeMetadata, collectionValueTypeMetadata);
                            break;
                        default:
                            serializeFuncSource = null;
                            break;
                    }

                    serializeFuncNamedArg = $"serializeFunc: {serializeMethodName}";
                }

                string collectionTypeInfoValue = collectionType switch
                {
                    CollectionType.Array => $"{JsonMetadataServicesTypeRef}.CreateArrayInfo<{valueTypeCompilableName}>({OptionsInstanceVariableName}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})",
                    CollectionType.List => $"{JsonMetadataServicesTypeRef}.CreateListInfo<{typeCompilableName}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new {ListTypeRef}<{valueTypeCompilableName}>(), {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})",
                    CollectionType.Dictionary => $"{JsonMetadataServicesTypeRef}.CreateDictionaryInfo<{typeCompilableName}, {keyTypeCompilableName!}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new {DictionaryTypeRef}<{keyTypeCompilableName}, {valueTypeCompilableName}>(), {keyTypeMetadataPropertyName!}, {valueTypeMetadataPropertyName}, {numberHandlingArg}, {serializeFuncNamedArg})",
                    _ => throw new NotSupportedException()
                };

                string metadataInitSource = @$"_{typeFriendlyName} = {collectionTypeInfoValue};";

                return GenerateForType(typeGenerationSpec, metadataInitSource, serializeFuncSource);
            }

            private string GenerateFastPathFuncForEnumerable(string typeInfoRef, string serializeMethodName, bool canBeNull, bool isArray, TypeGenerationSpec valueTypeGenerationSpec)
            {
                string? writerMethodToCall = GetWriterMethod(valueTypeGenerationSpec.Type);
                string valueToWrite = $"{ValueVarName}[i]";
                string lengthPropName = isArray ? "Length" : "Count";

                string elementSerializationLogic;
                if (writerMethodToCall != null)
                {
                    elementSerializationLogic = $"{writerMethodToCall}Value({valueToWrite});";
                }
                else
                {
                    elementSerializationLogic = GetSerializeLogicForNonPrimitiveType(valueTypeGenerationSpec.TypeInfoPropertyName, valueToWrite, valueTypeGenerationSpec.GenerateSerializationLogic);
                }

                string serializationLogic = $@"{WriterVarName}.WriteStartArray();

    for (int i = 0; i < {ValueVarName}.{lengthPropName}; i++)
    {{
        {elementSerializationLogic}
    }}

    {WriterVarName}.WriteEndArray();";

                return GenerateFastPathFuncForType(serializeMethodName, typeInfoRef, serializationLogic, canBeNull);
            }

            private string GenerateFastPathFuncForDictionary(
                string typeInfoRef,
                string serializeMethodName,
                bool canBeNull,
                TypeGenerationSpec keyTypeGenerationSpec,
                TypeGenerationSpec valueTypeGenerationSpec)
            {
                const string pairVarName = "pair";
                string keyToWrite = $"{pairVarName}.Key";
                string valueToWrite = $"{pairVarName}.Value";

                string? writerMethodToCall = GetWriterMethod(valueTypeGenerationSpec.Type);
                string elementSerializationLogic;

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

                return GenerateFastPathFuncForType(serializeMethodName, typeInfoRef, serializationLogic, canBeNull);
            }

            private string GenerateForObject(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string createObjectFuncTypeArg = typeMetadata.ConstructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                    ? $"createObjectFunc: static () => new {typeMetadata.TypeRef}()"
                    : "createObjectFunc: null";

                string propInitMethodName = $"{typeFriendlyName}{PropInitMethodNameSuffix}";
                string? propMetadataInitFuncSource = null;
                string propMetadataInitFuncNamedArg;

                string serializeMethodName = $"{typeFriendlyName}{SerializeMethodNameSuffix}";
                string? serializeFuncSource = null;
                string serializeFuncNamedArg;

                List<PropertyGenerationSpec>? properties = typeMetadata.PropertiesMetadata;

                if (typeMetadata.GenerateMetadata)
                {
                    propMetadataInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata.IsValueType, propInitMethodName, properties);
                    propMetadataInitFuncNamedArg = $@"propInitFunc: {propInitMethodName}";
                }
                else
                {
                    propMetadataInitFuncNamedArg = @"propInitFunc: null";
                }

                if (typeMetadata.GenerateSerializationLogic)
                {
                    serializeFuncSource = GenerateFastPathFuncForObject(
                        typeCompilableName,
                        serializeMethodName,
                        typeMetadata.CanBeNull,
                        typeMetadata.ImplementsIJsonOnSerialized,
                        typeMetadata.ImplementsIJsonOnSerializing,
                        properties);
                    serializeFuncNamedArg = $@"serializeFunc: {serializeMethodName}";
                }
                else
                {
                    serializeFuncNamedArg = @"serializeFunc: null";
                }

                string objectInfoInitSource = $@"{JsonTypeInfoTypeRef}<{typeCompilableName}> objectInfo = {JsonMetadataServicesTypeRef}.CreateObjectInfo<{typeCompilableName}>(
                    {OptionsInstanceVariableName},
                    {createObjectFuncTypeArg},
                    {propMetadataInitFuncNamedArg},
                    {GetNumberHandlingAsStr(typeMetadata.NumberHandling)},
                    {serializeFuncNamedArg});

                    _{typeFriendlyName} = objectInfo;";

                string additionalSource;
                if (propMetadataInitFuncSource == null || serializeFuncSource == null)
                {
                    additionalSource = propMetadataInitFuncSource ?? serializeFuncSource;
                }
                else
                {
                    additionalSource = @$"{propMetadataInitFuncSource}{serializeFuncSource}";
                }

                return GenerateForType(typeMetadata, objectInfoInitSource, additionalSource);
            }

            private string GeneratePropMetadataInitFunc(
                bool declaringTypeIsValueType,
                string propInitMethodName,
                List<PropertyGenerationSpec>? properties)
            {
                const string PropVarName = "properties";
                const string JsonContextVarName = "jsonContext";

                string propertyArrayInstantiationValue = properties == null
                    ? $"{ArrayTypeRef}.Empty<{JsonPropertyInfoTypeRef}>()"
                    : $"new {JsonPropertyInfoTypeRef}[{properties.Count}]";

                string contextTypeRef = _currentContext.ContextTypeRef;

                StringBuilder sb = new();

                sb.Append($@"

private static {JsonPropertyInfoTypeRef}[] {propInitMethodName}({JsonSerializerContextTypeRef} context)
{{
    {contextTypeRef} {JsonContextVarName} = ({contextTypeRef})context;
    {JsonSerializerOptionsTypeRef} options = context.Options;

    {JsonPropertyInfoTypeRef}[] {PropVarName} = {propertyArrayInstantiationValue};
");

                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
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
                            ? $"getter: static (obj) => {{ return (({declaringTypeCompilableName})obj).{clrPropertyName}; }}"
                            : "getter: null";

                        string setterNamedArg;
                        if (memberMetadata.CanUseSetter)
                        {
                            string propMutation = declaringTypeIsValueType
                                ? @$"{{ {UnsafeTypeRef}.Unbox<{declaringTypeCompilableName}>(obj).{clrPropertyName} = value; }}"
                                : $@"{{ (({declaringTypeCompilableName})obj).{clrPropertyName} = value; }}";

                            setterNamedArg = $"setter: static (obj, value) => {propMutation}";
                        }
                        else
                        {
                            setterNamedArg = "setter: null";
                        }

                        JsonIgnoreCondition? ignoreCondition = memberMetadata.DefaultIgnoreCondition;
                        string ignoreConditionNamedArg = ignoreCondition.HasValue
                            ? $"ignoreCondition: JsonIgnoreCondition.{ignoreCondition.Value}"
                            : "ignoreCondition: default";

                        string converterNamedArg = memberMetadata.ConverterInstantiationLogic == null
                            ? "converter: null"
                            : $"converter: {memberMetadata.ConverterInstantiationLogic}";

                        string memberTypeCompilableName = memberTypeMetadata.TypeRef;

                        sb.Append($@"
    {PropVarName}[{i}] = {JsonMetadataServicesTypeRef}.CreatePropertyInfo<{memberTypeCompilableName}>(
        options,
        isProperty: {memberMetadata.IsProperty.ToString().ToLowerInvariant()},
        declaringType: typeof({memberMetadata.DeclaringTypeRef}),
        {typeTypeInfoNamedArg},
        {converterNamedArg},
        {getterNamedArg},
        {setterNamedArg},
        {ignoreConditionNamedArg},
        numberHandling: {GetNumberHandlingAsStr(memberMetadata.NumberHandling)},
        propertyName: ""{clrPropertyName}"",
        {jsonPropertyNameNamedArg});
    ");
                    }
                }

                sb.Append(@$"
    return {PropVarName};
}}");

                return sb.ToString();
            }

            private string GenerateFastPathFuncForObject(
                string typeInfoTypeRef,
                string serializeMethodName,
                bool canBeNull,
                bool implementsIJsonOnSerialized,
                bool implementsIJsonOnSerializing,
                List<PropertyGenerationSpec>? properties)
            {
                JsonSourceGenerationOptionsAttribute options = _currentContext.GenerationOptions;

                // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                string[] runtimePropNames = GetRuntimePropNames(properties, options.PropertyNamingPolicy);
                _currentContext.RuntimePropertyNames.UnionWith(runtimePropNames);

                StringBuilder sb = new();

                // Begin method definition
                if (implementsIJsonOnSerializing)
                {
                    sb.Append($@"(({IJsonOnSerializingFullName}){ValueVarName}).OnSerializing();");
                    sb.Append($@"{Environment.NewLine}    ");
                }

                sb.Append($@"{WriterVarName}.WriteStartObject();");

                if (properties != null)
                {
                    // Provide generation logic for each prop.
                    for (int i = 0; i < properties.Count; i++)
                    {
                        PropertyGenerationSpec propertySpec = properties[i];
                        TypeGenerationSpec propertyTypeSpec = propertySpec.TypeGenerationSpec;

                        if (propertyTypeSpec.ClassType == ClassType.TypeUnsupportedBySourceGen)
                        {
                            continue;
                        }

                        if (propertySpec.IsReadOnly)
                        {
                            if (propertySpec.IsProperty)
                            {
                                if (options.IgnoreReadOnlyProperties)
                                {
                                    continue;
                                }
                            }
                            else if (options.IgnoreReadOnlyFields)
                            {
                                continue;
                            }
                        }

                        if (!propertySpec.IsProperty && !propertySpec.HasJsonInclude && !options.IncludeFields)
                        {
                            continue;
                        }

                        Type propertyType = propertyTypeSpec.Type;
                        string propName = $"{runtimePropNames[i]}PropName";
                        string propValue = $"{ValueVarName}.{propertySpec.ClrName}";
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

                        JsonIgnoreCondition ignoreCondition = propertySpec.DefaultIgnoreCondition ?? options.DefaultIgnoreCondition;
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

                        sb.Append(WrapSerializationLogicInDefaultCheckIfRequired(serializationLogic, propValue, defaultCheckType));
                    }
                }

                // End method definition
                sb.Append($@"

        {WriterVarName}.WriteEndObject();");

                if (implementsIJsonOnSerialized)
                {
                    sb.Append($@"{Environment.NewLine}    ");
                    sb.Append($@"(({IJsonOnSerializedFullName}){ValueVarName}).OnSerialized();");
                };

                return GenerateFastPathFuncForType(serializeMethodName, typeInfoTypeRef, sb.ToString(), canBeNull);
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

            private string WrapSerializationLogicInDefaultCheckIfRequired(string serializationLogic, string propValue, DefaultCheckType defaultCheckType)
            {
                if (defaultCheckType == DefaultCheckType.None)
                {
                    return serializationLogic;
                }

                string defaultLiteral = defaultCheckType == DefaultCheckType.Null ? "null" : "default";
                return $@"
        if ({propValue} != {defaultLiteral})
        {{{serializationLogic}
        }}";
            }

            private string[] GetRuntimePropNames(List<PropertyGenerationSpec>? properties, JsonKnownNamingPolicy namingPolicy)
            {
                if (properties == null)
                {
                    return Array.Empty<string>();
                }

                int propCount = properties.Count;
                string[] runtimePropNames = new string[propCount];

                // Compute JsonEncodedText values to represent each property name. This gives the best throughput performance
                for (int i = 0; i < propCount; i++)
                {
                    PropertyGenerationSpec propertySpec = properties[i];

                    string propName = DetermineRuntimePropName(propertySpec.ClrName, propertySpec.JsonPropertyName, namingPolicy);
                    Debug.Assert(propName != null);

                    runtimePropNames[i] = propName;
                }

                return runtimePropNames;
            }

            private string DetermineRuntimePropName(string clrPropName, string? jsonPropName, JsonKnownNamingPolicy namingPolicy)
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
    IgnoreReadOnlyFields = {options.IgnoreReadOnlyFields.ToString().ToLowerInvariant()},
    IgnoreReadOnlyProperties = {options.IgnoreReadOnlyProperties.ToString().ToLowerInvariant()},
    IncludeFields = {options.IncludeFields.ToString().ToLowerInvariant()},
    WriteIndented = {options.WriteIndented.ToString().ToLowerInvariant()},{namingPolicyInit}
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

                // TODO (https://github.com/dotnet/runtime/issues/52218): Make this Dictionary-lookup-based if root-serializable type count > 64.
                foreach (TypeGenerationSpec metadata in _currentContext.RootSerializableTypes)
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
    }
}
