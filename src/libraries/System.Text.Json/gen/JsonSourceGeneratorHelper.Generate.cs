// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json.SourceGeneration.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    internal sealed partial class JsonSourceGeneratorHelper
    {
        private readonly string _generationNamespace;

        // TODO: consider public option for this.
        // Converter-honoring logic generation can be simplified
        // if we don't plan to have a feature around this.
        private bool _honorRuntimeProvidedCustomConverters = true;

        private const string RuntimeCustomConverterFetchingMethodName = "GetRuntimeProvidedCustomConverter";

        private const string JsonContextDeclarationSource = "internal partial class JsonContext : JsonSerializerContext";

        private const string OptionsInstanceVariableName = "Options";

        private const string PropInitFuncVarName = "PropInitFunc";

        private const string JsonMetadataServicesClassName = "JsonMetadataServices";

        private const string CreateValueInfoMethodName = "CreateValueInfo";

        private static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
            "SYSLIB2021",
            "Did not generate serialization metadata for type",
            "Did not generate serialization metadata for type {0}",
            "JSON source generation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Error message: {2}");

        private static DiagnosticDescriptor DuplicateTypeName { get; } = new DiagnosticDescriptor(
            "SYSLIB2022",
            "Duplicate type name",
            "There are multiple types named {0}. Source was generated for the first one detected. Use 'JsonSerializableAttribute.TypeInfoPropertyName' to resolve this collision.",
            "JSON source generation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Error message: {2}");

        /// <summary>
        /// Types that we have initiated serialization metadata generation for. A type may be discoverable in the object graph,
        /// but not reachable for serialization (e.g. it is [JsonIgnore]'d); thus we maintain a separate cache.
        /// </summary>
        private readonly HashSet<TypeMetadata> _typesWithMetadataGenerated = new();

        private void GenerateTypeMetadata(TypeMetadata typeMetadata)
        {
            Debug.Assert(typeMetadata != null);

            if (_typesWithMetadataGenerated.Contains(typeMetadata))
            {
                return;
            }

            _typesWithMetadataGenerated.Add(typeMetadata);

            string source;

            switch (typeMetadata.ClassType)
            {
                case ClassType.KnownType:
                    {
                        source = GenerateForTypeWithKnownConverter(typeMetadata);
                    }
                    break;
                case ClassType.TypeWithDesignTimeProvidedCustomConverter:
                    {
                        source = GenerateForTypeWithUnknownConverter(typeMetadata);
                    }
                    break;
                case ClassType.Nullable:
                    {
                        source = GenerateForNullable(typeMetadata);

                        GenerateTypeMetadata(typeMetadata.NullableUnderlyingTypeMetadata);
                    }
                    break;
                case ClassType.Enum:
                    {
                        source = GenerateForEnum(typeMetadata);
                    }
                    break;
                case ClassType.Enumerable:
                    {
                        source = GenerateForCollection(typeMetadata);

                        GenerateTypeMetadata(typeMetadata.CollectionValueTypeMetadata);
                    }
                    break;
                case ClassType.Dictionary:
                    {
                        source = GenerateForCollection(typeMetadata);

                        GenerateTypeMetadata(typeMetadata.CollectionKeyTypeMetadata);
                        GenerateTypeMetadata(typeMetadata.CollectionValueTypeMetadata);
                    }
                    break;
                case ClassType.Object:
                    {
                        source = GenerateForObject(typeMetadata);

                        if (typeMetadata.PropertiesMetadata != null)
                        {
                            foreach (PropertyMetadata metadata in typeMetadata.PropertiesMetadata)
                            {
                                GenerateTypeMetadata(metadata.TypeMetadata);
                            }
                        }
                    }
                    break;
                case ClassType.TypeUnsupportedBySourceGen:
                    {
                        _executionContext.ReportDiagnostic(
                            Diagnostic.Create(TypeNotSupported, Location.None, new string[] { typeMetadata.CompilableName }));
                        return;
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }

            try
            {
                _executionContext.AddSource($"{typeMetadata.FriendlyName}.cs", SourceText.From(source, Encoding.UTF8));
            }
            catch (ArgumentException)
            {
                _executionContext.ReportDiagnostic(Diagnostic.Create(DuplicateTypeName, Location.None, new string[] { typeMetadata.FriendlyName }));
            }
        }

        private string GenerateForTypeWithKnownConverter(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            string metadataInitSource = $@"_{typeFriendlyName} = {JsonMetadataServicesClassName}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, {JsonMetadataServicesClassName}.{typeFriendlyName}Converter);";

            return GenerateForType(typeMetadata, metadataInitSource);
        }

        private string GenerateForTypeWithUnknownConverter(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            StringBuilder sb = new();

            string metadataInitSource = $@"JsonConverter converter = {typeMetadata.ConverterInstantiationLogic};
                    // TODO: consider moving this verification source to common helper.
                    Type typeToConvert = typeof({typeCompilableName});
                    if (!converter.CanConvert(typeToConvert))
                    {{
                        Type underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                        if (underlyingType != null && converter.CanConvert(underlyingType))
                        {{
                            JsonConverter actualConverter = converter;

                            if (converter is JsonConverterFactory converterFactory)
                            {{
                                actualConverter = converterFactory.CreateConverter(underlyingType, {OptionsInstanceVariableName});

                                if (actualConverter == null || actualConverter is JsonConverterFactory)
                                {{
                                    throw new InvalidOperationException($""JsonConverterFactory '{{converter}} cannot return a 'null' or 'JsonConverterFactory' value."");
                                }}
                            }}

                            // Allow nullable handling to forward to the underlying type's converter.
                            converter = {JsonMetadataServicesClassName}.GetNullableConverter<{typeCompilableName}>((JsonConverter<{typeCompilableName}>)actualConverter);
                        }}
                        else
                        {{
                            throw new InvalidOperationException($""The converter '{{converter.GetType()}}' is not compatible with the type '{{typeToConvert}}'."");
                        }}
                    }}

                    _{typeFriendlyName} = {JsonMetadataServicesClassName}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, converter);";

            return GenerateForType(typeMetadata, metadataInitSource);
        }

        private string GenerateForNullable(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            TypeMetadata? underlyingTypeMetadata = typeMetadata.NullableUnderlyingTypeMetadata;
            Debug.Assert(underlyingTypeMetadata != null);
            string underlyingTypeCompilableName = underlyingTypeMetadata.CompilableName;
            string underlyingTypeFriendlyName = underlyingTypeMetadata.FriendlyName;
            string underlyingTypeInfoNamedArg = underlyingTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                ? "underlyingTypeInfo: null"
                : $"underlyingTypeInfo: {underlyingTypeFriendlyName}";

            string metadataInitSource = @$"_{typeFriendlyName} = {JsonMetadataServicesClassName}.{GetCreateValueInfoMethodRef(typeCompilableName)}(
                        {OptionsInstanceVariableName},
                        {JsonMetadataServicesClassName}.GetNullableConverter<{underlyingTypeCompilableName}>({underlyingTypeInfoNamedArg}));
";

            return GenerateForType(typeMetadata, metadataInitSource);
        }

        private string GenerateForEnum(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            string metadataInitSource = $"_{typeFriendlyName} = {JsonMetadataServicesClassName}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, JsonMetadataServices.GetEnumConverter<{typeCompilableName}>({OptionsInstanceVariableName}));";

            return GenerateForType(typeMetadata, metadataInitSource);
        }

        private string GenerateForCollection(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            // Key metadata
            TypeMetadata? collectionKeyTypeMetadata = typeMetadata.CollectionKeyTypeMetadata;
            Debug.Assert(!(typeMetadata.CollectionType == CollectionType.Dictionary && collectionKeyTypeMetadata == null));
            string? keyTypeCompilableName = collectionKeyTypeMetadata?.CompilableName;
            string? keyTypeReadableName = collectionKeyTypeMetadata?.FriendlyName;

            string? keyTypeMetadataPropertyName;
            if (typeMetadata.ClassType != ClassType.Dictionary)
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
            TypeMetadata? collectionValueTypeMetadata = typeMetadata.CollectionValueTypeMetadata;
            Debug.Assert(collectionValueTypeMetadata != null);
            string valueTypeCompilableName = collectionValueTypeMetadata.CompilableName;
            string valueTypeReadableName = collectionValueTypeMetadata.FriendlyName;

            string valueTypeMetadataPropertyName = collectionValueTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                ? "null"
                : $"this.{valueTypeReadableName}";

            string numberHandlingArg = $"{GetNumberHandlingAsStr(typeMetadata.NumberHandling)}";

            CollectionType collectionType = typeMetadata.CollectionType;
            string collectionTypeInfoValue = collectionType switch
            {
                CollectionType.Array => $"{JsonMetadataServicesClassName}.CreateArrayInfo<{valueTypeCompilableName}>({OptionsInstanceVariableName}, {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                CollectionType.List => $"{JsonMetadataServicesClassName}.CreateListInfo<{typeCompilableName}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new System.Collections.Generic.List<{valueTypeCompilableName}>(), {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                CollectionType.Dictionary => $"{JsonMetadataServicesClassName}.CreateDictionaryInfo<{typeCompilableName}, {keyTypeCompilableName!}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new System.Collections.Generic.Dictionary<{keyTypeCompilableName}, {valueTypeCompilableName}>(), {keyTypeMetadataPropertyName!}, {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                _ => throw new NotSupportedException()
            };

            string metadataInitSource = @$"_{typeFriendlyName} = {collectionTypeInfoValue};";
            return GenerateForType(typeMetadata, metadataInitSource);
        }

        private string GenerateForObject(TypeMetadata typeMetadata)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            string createObjectFuncTypeArg = typeMetadata.ConstructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                ? $"createObjectFunc: static () => new {typeMetadata.CompilableName}()"
                : "createObjectFunc: null";

            List<PropertyMetadata>? properties = typeMetadata.PropertiesMetadata;

            StringBuilder sb = new();

            sb.Append($@"JsonTypeInfo<{typeCompilableName}> objectInfo = {JsonMetadataServicesClassName}.CreateObjectInfo<{typeCompilableName}>();
                    _{typeFriendlyName} = objectInfo;
");

            string propInitFuncVarName = $"{typeFriendlyName}{PropInitFuncVarName}";

            sb.Append($@"
                    {JsonMetadataServicesClassName}.InitializeObjectInfo(
                        objectInfo,
                        {OptionsInstanceVariableName},
                        {createObjectFuncTypeArg},
                        {propInitFuncVarName},
                        {GetNumberHandlingAsStr(typeMetadata.NumberHandling)});");

            string metadataInitSource = sb.ToString();
            string? propInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata.IsValueType, propInitFuncVarName, properties);

            return GenerateForType(typeMetadata, metadataInitSource, propInitFuncSource);
        }

        private string GeneratePropMetadataInitFunc(
            bool declaringTypeIsValueType,
            string propInitFuncVarName,
            List<PropertyMetadata>? properties)
        {
            const string PropVarName = "properties";
            const string JsonContextVarName = "jsonContext";
            const string JsonPropertyInfoTypeName = "JsonPropertyInfo";

            string propertyArrayInstantiationValue = properties == null
                ? $"System.Array.Empty<{JsonPropertyInfoTypeName}>()"
                : $"new {JsonPropertyInfoTypeName}[{properties.Count}]";

            StringBuilder sb = new();

            sb.Append($@"
        private static {JsonPropertyInfoTypeName}[] {propInitFuncVarName}(JsonSerializerContext context)
        {{
            JsonContext {JsonContextVarName} = (JsonContext)context;
            JsonSerializerOptions options = context.Options;

            {JsonPropertyInfoTypeName}[] {PropVarName} = {propertyArrayInstantiationValue};
");

            if (properties != null)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyMetadata memberMetadata = properties[i];

                    TypeMetadata memberTypeMetadata = memberMetadata.TypeMetadata;

                    string clrPropertyName = memberMetadata.ClrName;

                    string declaringTypeCompilableName = memberMetadata.DeclaringTypeCompilableName;

                    string memberTypeFriendlyName = memberTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                        ? "null"
                        : $"{JsonContextVarName}.{memberTypeMetadata.FriendlyName}";

                    string typeTypeInfoNamedArg = $"propertyTypeInfo: {memberTypeFriendlyName}";

                    string jsonPropertyNameNamedArg = memberMetadata.JsonPropertyName != null
                        ? @$"jsonPropertyName: ""{memberMetadata.JsonPropertyName}"""
                        : "jsonPropertyName: null";

                    string getterNamedArg = memberMetadata.HasGetter
                        ? $"getter: static (obj) => {{ return (({declaringTypeCompilableName})obj).{clrPropertyName}; }}"
                        : "getter: null";

                    string setterNamedArg;
                    if (memberMetadata.HasSetter)
                    {
                        string propMutation = declaringTypeIsValueType
                            ? @$"{{ Unsafe.Unbox<{declaringTypeCompilableName}>(obj).{clrPropertyName} = value; }}"
                            : $@"{{ (({declaringTypeCompilableName})obj).{clrPropertyName} = value; }}";

                        setterNamedArg = $"setter: static (obj, value) => {propMutation}";
                    }
                    else
                    {
                        setterNamedArg = "setter: null";
                    }

                    JsonIgnoreCondition? ignoreCondition = memberMetadata.IgnoreCondition;
                    string ignoreConditionNamedArg = ignoreCondition.HasValue
                        ? $"ignoreCondition: JsonIgnoreCondition.{ignoreCondition.Value}"
                        : "ignoreCondition: default";

                    string converterNamedArg = memberMetadata.ConverterInstantiationLogic == null
                        ? "converter: null"
                        : $"converter: {memberMetadata.ConverterInstantiationLogic}";

                    string memberTypeCompilableName = memberTypeMetadata.CompilableName;

                    sb.Append($@"
            {PropVarName}[{i}] = {JsonMetadataServicesClassName}.CreatePropertyInfo<{memberTypeCompilableName}>(
                options,
                isProperty: {memberMetadata.IsProperty.ToString().ToLowerInvariant()},
                declaringType: typeof({memberMetadata.DeclaringTypeCompilableName}),
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

        private string GenerateForType(TypeMetadata typeMetadata, string metadataInitSource, string? additionalSource = null)
        {
            string typeCompilableName = typeMetadata.CompilableName;
            string typeFriendlyName = typeMetadata.FriendlyName;

            return @$"{GetUsingStatementsString(typeMetadata)}

namespace {_generationNamespace}
{{
    {JsonContextDeclarationSource}
    {{
        private JsonTypeInfo<{typeCompilableName}> _{typeFriendlyName};
        public JsonTypeInfo<{typeCompilableName}> {typeFriendlyName}
        {{
            get
            {{
                if (_{typeFriendlyName} == null)
                {{
                    {WrapWithCheckForCustomConverterIfRequired(metadataInitSource, typeCompilableName, typeFriendlyName, GetNumberHandlingAsStr(typeMetadata.NumberHandling))}
                }}

                return _{typeFriendlyName};
            }}
        }}{additionalSource}
    }}
}}
";
        }

        private string WrapWithCheckForCustomConverterIfRequired(string source, string typeCompilableName, string typeFriendlyName, string numberHandlingNamedArg)
        {
            if (!_honorRuntimeProvidedCustomConverters)
            {
                return source;
            }

            return @$"JsonConverter customConverter;
                    if ({OptionsInstanceVariableName}.Converters.Count > 0 && (customConverter = {RuntimeCustomConverterFetchingMethodName}(typeof({typeCompilableName}))) != null)
                    {{
                        _{typeFriendlyName} = {JsonMetadataServicesClassName}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, customConverter);
                    }}
                    else
                    {{
                        {source.Replace(Environment.NewLine, $"{Environment.NewLine}    ")}
                    }}";
        }

        // Base source generation context partial class.
        private string BaseJsonContextImplementation()
        {
            StringBuilder sb = new();
            sb.Append(@$"using System.Text.Json;
using System.Text.Json.Serialization;

namespace {_generationNamespace}
{{
    {JsonContextDeclarationSource}
    {{
        private static JsonContext s_default;
        public static JsonContext Default => s_default ??= new JsonContext(new JsonSerializerOptions());

        public JsonContext() : base(null)
        {{
        }}

        public JsonContext(JsonSerializerOptions options) : base(options)
        {{
        }}

        {GetFetchLogicForRuntimeSpecifiedCustomConverter()}
    }}
}}
");

            return sb.ToString();
        }

        private string GetFetchLogicForRuntimeSpecifiedCustomConverter()
        {
            if (!_honorRuntimeProvidedCustomConverters)
            {
                return "";
            }

            return @$"private JsonConverter {RuntimeCustomConverterFetchingMethodName}(System.Type type)
        {{
            System.Collections.Generic.IList<JsonConverter> converters = {OptionsInstanceVariableName}.Converters;

            // TODO: use a dictionary if count > ~15.
            for (int i = 0; i < converters.Count; i++)
            {{
                JsonConverter converter = converters[i];

                if (converter.CanConvert(type))
                {{
                    if (converter is JsonConverterFactory factory)
                    {{
                        converter = factory.CreateConverter(type, {OptionsInstanceVariableName});
                        if (converter == null || converter is JsonConverterFactory)
                        {{
                            throw new System.InvalidOperationException($""The converter '{{factory.GetType()}}' cannot return null or a JsonConverterFactory instance."");
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

            HashSet<string> usingStatements = new();

            foreach (TypeMetadata metadata in _rootSerializableTypes.Values)
            {
                usingStatements.UnionWith(GetUsingStatements(metadata));
            }

            sb.Append(@$"{GetUsingStatementsString(usingStatements)}

namespace {_generationNamespace}
{{
    {JsonContextDeclarationSource}
    {{
        public override JsonTypeInfo GetTypeInfo(System.Type type)
        {{");

            // TODO: Make this Dictionary-lookup-based if _handledType.Count > 64.
            foreach (TypeMetadata metadata in _rootSerializableTypes.Values)
            {
                if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                {
                    sb.Append($@"
            if (type == typeof({metadata.Type.GetUniqueCompilableTypeName()}))
            {{
                return this.{metadata.FriendlyName};
            }}
");
                }
            }

            sb.Append(@"
            return null!;
        }
    }
}
");

            return sb.ToString();
        }

        private static string GetUsingStatementsString(TypeMetadata typeMetadata)
        {
            HashSet<string> usingStatements = GetUsingStatements(typeMetadata);
            return GetUsingStatementsString(usingStatements);
        }

        private static string GetUsingStatementsString(HashSet<string> usingStatements)
        {
            string[] usingsArr = usingStatements.ToArray();
            Array.Sort(usingsArr);
            return string.Join("\n", usingsArr);
        }

        private static HashSet<string> GetUsingStatements(TypeMetadata typeMetadata)
        {
            HashSet<string> usingStatements = new();

            // Add library usings.
            usingStatements.Add(FormatAsUsingStatement("System.Runtime.CompilerServices"));
            usingStatements.Add(FormatAsUsingStatement("System.Text.Json"));
            usingStatements.Add(FormatAsUsingStatement("System.Text.Json.Serialization"));
            usingStatements.Add(FormatAsUsingStatement("System.Text.Json.Serialization.Metadata"));

            // Add imports to root type.
            usingStatements.Add(FormatAsUsingStatement(typeMetadata.Type.Namespace));

            switch (typeMetadata.ClassType)
            {
                case ClassType.Nullable:
                    {
                        AddUsingStatementsForType(typeMetadata.NullableUnderlyingTypeMetadata!);
                    }
                    break;
                case ClassType.Enumerable:
                    {
                        AddUsingStatementsForType(typeMetadata.CollectionValueTypeMetadata);
                    }
                    break;
                case ClassType.Dictionary:
                    {
                        AddUsingStatementsForType(typeMetadata.CollectionKeyTypeMetadata);
                        AddUsingStatementsForType(typeMetadata.CollectionValueTypeMetadata);
                    }
                    break;
                case ClassType.Object:
                    {
                        if (typeMetadata.PropertiesMetadata != null)
                        {
                            foreach (PropertyMetadata metadata in typeMetadata.PropertiesMetadata)
                            {
                                AddUsingStatementsForType(metadata.TypeMetadata);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            void AddUsingStatementsForType(TypeMetadata typeMetadata)
            {
                usingStatements.Add(FormatAsUsingStatement(typeMetadata.Type.Namespace));

                if (typeMetadata.CollectionKeyTypeMetadata != null)
                {
                    Debug.Assert(typeMetadata.CollectionValueTypeMetadata != null);
                    usingStatements.Add(FormatAsUsingStatement(typeMetadata.CollectionKeyTypeMetadata.Type.Namespace));
                }

                if (typeMetadata.CollectionValueTypeMetadata != null)
                {
                    usingStatements.Add(FormatAsUsingStatement(typeMetadata.CollectionValueTypeMetadata.Type.Namespace));
                }
            }

            return usingStatements;
        }

        private static string FormatAsUsingStatement(string @namespace) => $"using {@namespace};";

        private static string GetNumberHandlingAsStr(JsonNumberHandling? numberHandling) =>
             numberHandling.HasValue
                ? $"(JsonNumberHandling){(int)numberHandling.Value}"
                : "default";

        private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";
    }
}
