// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed partial class Emitter
        {
            // Literals in generated source
            private const string CreateValueInfoMethodName = "CreateValueInfo";
            private const string CtorParamInitMethodNameSuffix = "CtorParamInit";
            private const string DefaultOptionsStaticVarName = "s_defaultOptions";
            private const string OriginatingResolverPropertyName = "OriginatingResolver";
            private const string InfoVarName = "info";
            private const string NumberHandlingPropName = "NumberHandling";
            private const string UnmappedMemberHandlingPropName = "UnmappedMemberHandling";
            private const string PreferredPropertyObjectCreationHandlingPropName = "PreferredPropertyObjectCreationHandling";
            private const string ObjectCreatorPropName = "ObjectCreator";
            private const string OptionsInstanceVariableName = "Options";
            private const string JsonTypeInfoLocalVariableName = "jsonTypeInfo";
            private const string PropInitMethodNameSuffix = "PropInit";
            private const string TryGetTypeInfoForRuntimeCustomConverterMethodName = "TryGetTypeInfoForRuntimeCustomConverter";
            private const string ExpandConverterMethodName = "ExpandConverter";
            private const string GetConverterForNullablePropertyMethodName = "GetConverterForNullableProperty";
            private const string SerializeHandlerPropName = "SerializeHandler";
            private const string OptionsLocalVariableName = "options";
            private const string ValueVarName = "value";
            private const string WriterVarName = "writer";

            private static readonly AssemblyName s_assemblyName = typeof(Emitter).Assembly.GetName();

            // global::fully.qualified.name for referenced types
            private const string InvalidOperationExceptionTypeRef = "global::System.InvalidOperationException";
            private const string TypeTypeRef = "global::System.Type";
            private const string UnsafeTypeRef = "global::System.Runtime.CompilerServices.Unsafe";
            private const string EqualityComparerTypeRef = "global::System.Collections.Generic.EqualityComparer";
            private const string KeyValuePairTypeRef = "global::System.Collections.Generic.KeyValuePair";
            private const string JsonEncodedTextTypeRef = "global::System.Text.Json.JsonEncodedText";
            private const string JsonNamingPolicyTypeRef = "global::System.Text.Json.JsonNamingPolicy";
            private const string JsonSerializerTypeRef = "global::System.Text.Json.JsonSerializer";
            private const string JsonSerializerOptionsTypeRef = "global::System.Text.Json.JsonSerializerOptions";
            private const string JsonSerializerContextTypeRef = "global::System.Text.Json.Serialization.JsonSerializerContext";
            private const string Utf8JsonWriterTypeRef = "global::System.Text.Json.Utf8JsonWriter";
            private const string JsonCommentHandlingTypeRef = "global::System.Text.Json.JsonCommentHandling";
            private const string JsonConverterTypeRef = "global::System.Text.Json.Serialization.JsonConverter";
            private const string JsonConverterFactoryTypeRef = "global::System.Text.Json.Serialization.JsonConverterFactory";
            private const string JsonCollectionInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues";
            private const string JsonIgnoreConditionTypeRef = "global::System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonSerializerDefaultsTypeRef = "global::System.Text.Json.JsonSerializerDefaults";
            private const string JsonNumberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonNumberHandling";
            private const string JsonObjectCreationHandlingTypeRef = "global::System.Text.Json.Serialization.JsonObjectCreationHandling";
            private const string JsonUnmappedMemberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonUnmappedMemberHandling";
            private const string JsonUnknownTypeHandlingTypeRef = "global::System.Text.Json.Serialization.JsonUnknownTypeHandling";
            private const string JsonMetadataServicesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonMetadataServices";
            private const string JsonObjectInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonObjectInfoValues";
            private const string JsonParameterInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonParameterInfoValues";
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonPropertyInfoValuesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";
            private const string JsonTypeInfoResolverTypeRef = "global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver";

            /// <summary>
            /// Contains an index from TypeRef to TypeGenerationSpec for the current ContextGenerationSpec.
            /// </summary>
            private readonly Dictionary<TypeRef, TypeGenerationSpec> _typeIndex = new();

            /// <summary>
            /// Cache of property names (statically determined) found across the type graph of the JsonSerializerContext.
            /// The dictionary Key is the JSON property name, and the Value is the variable name which is the same as the property
            /// name except for cases where special characters are used with [JsonPropertyName].
            /// </summary>
            private readonly Dictionary<string, string> _propertyNames = new();

            /// <summary>
            /// Indicates that the type graph contains a nullable property with a design-time custom converter declaration.
            /// </summary>
            private bool _emitGetConverterForNullablePropertyMethod;

            /// <summary>
            /// The SourceText emit implementation filled by the individual Roslyn versions.
            /// </summary>
            private partial void AddSource(string hintName, SourceText sourceText);

            public void Emit(ContextGenerationSpec contextGenerationSpec)
            {
                Debug.Assert(_typeIndex.Count == 0);
                Debug.Assert(_propertyNames.Count == 0);
                Debug.Assert(!_emitGetConverterForNullablePropertyMethod);

                foreach (TypeGenerationSpec spec in contextGenerationSpec.GeneratedTypes)
                {
                    _typeIndex.Add(spec.TypeRef, spec);
                }

                foreach (TypeGenerationSpec typeGenerationSpec in contextGenerationSpec.GeneratedTypes)
                {
                    SourceText? sourceText = GenerateTypeInfo(contextGenerationSpec, typeGenerationSpec);
                    if (sourceText != null)
                    {
                        AddSource($"{contextGenerationSpec.ContextType.Name}.{typeGenerationSpec.TypeInfoPropertyName}.g.cs", sourceText);
                    }
                }

                string contextName = contextGenerationSpec.ContextType.Name;

                // Add root context implementation.
                AddSource($"{contextName}.g.cs", GetRootJsonContextImplementation(contextGenerationSpec, _emitGetConverterForNullablePropertyMethod));

                // Add GetJsonTypeInfo override implementation.
                AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation(contextGenerationSpec));

                // Add property name initialization.
                AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization(contextGenerationSpec));

                _emitGetConverterForNullablePropertyMethod = false;
                _propertyNames.Clear();
                _typeIndex.Clear();
            }

            private static SourceWriter CreateSourceWriterWithContextHeader(ContextGenerationSpec contextSpec, bool isPrimaryContextSourceFile = false, string? interfaceImplementation = null)
            {
                var writer = new SourceWriter();

                writer.WriteLine("""
                    // <auto-generated/>

                    #nullable enable annotations
                    #nullable disable warnings

                    // Suppress warnings about [Obsolete] member usage in generated code.
                    #pragma warning disable CS0612, CS0618

                    """);

                if (contextSpec.Namespace != null)
                {
                    writer.WriteLine($"namespace {contextSpec.Namespace}");
                    writer.WriteLine('{');
                    writer.Indentation++;
                }

                ImmutableEquatableArray<string> contextClasses = contextSpec.ContextClassDeclarations;
                Debug.Assert(contextClasses.Count > 0);

                // Emit any containing classes first.
                for (int i = contextClasses.Count - 1; i > 0; i--)
                {
                    writer.WriteLine(contextClasses[i]);
                    writer.WriteLine('{');
                    writer.Indentation++;
                }

                if (isPrimaryContextSourceFile)
                {
                    // Annotate context class with the GeneratedCodeAttribute
                    writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{s_assemblyName.Name}", "{s_assemblyName.Version}")]""");
                }

                // Emit the JsonSerializerContext class declaration
                writer.WriteLine($"{contextClasses[0]}{(interfaceImplementation is null ? "" : " : " + interfaceImplementation)}");
                writer.WriteLine('{');
                writer.Indentation++;

                return writer;
            }

            private static SourceText CompleteSourceFileAndReturnText(SourceWriter writer)
            {
                while (writer.Indentation > 0)
                {
                    writer.Indentation--;
                    writer.WriteLine('}');
                }

                return writer.ToSourceText();
            }

            private SourceText? GenerateTypeInfo(ContextGenerationSpec contextSpec, TypeGenerationSpec typeGenerationSpec)
            {
                switch (typeGenerationSpec.ClassType)
                {
                    case ClassType.BuiltInSupportType:
                        return GenerateForTypeWithBuiltInConverter(contextSpec, typeGenerationSpec);

                    case ClassType.TypeWithDesignTimeProvidedCustomConverter:
                        return GenerateForTypeWithCustomConverter(contextSpec, typeGenerationSpec);

                    case ClassType.Nullable:
                        return GenerateForNullable(contextSpec, typeGenerationSpec);

                    case ClassType.Enum:
                        return GenerateForEnum(contextSpec, typeGenerationSpec);

                    case ClassType.Enumerable:
                    case ClassType.Dictionary:
                        return GenerateForCollection(contextSpec, typeGenerationSpec);

                    case ClassType.Object:
                        return GenerateForObject(contextSpec, typeGenerationSpec);

                    case ClassType.UnsupportedType:
                        return GenerateForUnsupportedType(contextSpec, typeGenerationSpec);

                    case ClassType.TypeUnsupportedBySourceGen:
                        return null; // Do not emit a source file for the type.

                    default:
                        Debug.Fail($"Unexpected class type {typeGenerationSpec.ClassType}");
                        return null;
                }
            }

            private static SourceText GenerateForTypeWithBuiltInConverter(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;
                string typeInfoPropertyName = typeMetadata.TypeInfoPropertyName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);
                writer.WriteLine($"""
                    {JsonTypeInfoLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeFQN)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.{typeInfoPropertyName}Converter);
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                return CompleteSourceFileAndReturnText(writer);
            }

            private static SourceText GenerateForTypeWithCustomConverter(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                Debug.Assert(typeMetadata.ConverterType != null);

                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;
                string converterFQN = typeMetadata.ConverterType.FullyQualifiedName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);

                writer.WriteLine($"""
                    {JsonConverterTypeRef} converter = {ExpandConverterMethodName}(typeof({typeFQN}), new {converterFQN}(), {OptionsLocalVariableName});
                    {JsonTypeInfoLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeFQN)} ({OptionsLocalVariableName}, converter);
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                return CompleteSourceFileAndReturnText(writer);
            }

            private static SourceText GenerateForNullable(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                Debug.Assert(typeMetadata.NullableUnderlyingType != null);

                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;
                string underlyingTypeFQN = typeMetadata.NullableUnderlyingType.FullyQualifiedName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);

                writer.WriteLine($$"""
                    {{JsonConverterTypeRef}} converter = {{JsonMetadataServicesTypeRef}}.GetNullableConverter<{{underlyingTypeFQN}}>({{OptionsLocalVariableName}});
                    {{JsonTypeInfoLocalVariableName}} = {{JsonMetadataServicesTypeRef}}.{{GetCreateValueInfoMethodRef(typeFQN)}}({{OptionsLocalVariableName}}, converter);
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                return CompleteSourceFileAndReturnText(writer);
            }

            private static SourceText GenerateForUnsupportedType(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);
                writer.WriteLine($"""
                    {JsonTypeInfoLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeFQN)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetUnsupportedTypeConverter<{typeFQN}>());
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                return CompleteSourceFileAndReturnText(writer);
            }

            private static SourceText GenerateForEnum(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);
                writer.WriteLine($"""
                    {JsonTypeInfoLocalVariableName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeFQN)}({OptionsLocalVariableName}, {JsonMetadataServicesTypeRef}.GetEnumConverter<{typeFQN}>({OptionsLocalVariableName}));
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                return CompleteSourceFileAndReturnText(writer);
            }

            private SourceText GenerateForCollection(ContextGenerationSpec contextSpec, TypeGenerationSpec typeGenerationSpec)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                // Key metadata
                TypeRef? collectionKeyType = typeGenerationSpec.CollectionKeyType;
                Debug.Assert(!(typeGenerationSpec.ClassType == ClassType.Dictionary && collectionKeyType == null));
                string? keyTypeFQN = collectionKeyType?.FullyQualifiedName;

                // Value metadata
                TypeRef? collectionValueType = typeGenerationSpec.CollectionValueType;
                Debug.Assert(collectionValueType != null);
                string valueTypeFQN = collectionValueType.FullyQualifiedName;

                CollectionType collectionType = typeGenerationSpec.CollectionType;

                string? serializeMethodName = ShouldGenerateSerializationLogic(typeGenerationSpec)
                    ? $"{typeGenerationSpec.TypeInfoPropertyName}{SerializeHandlerPropName}"
                    : null;

                string typeFQN = typeGenerationSpec.TypeRef.FullyQualifiedName;
                string createCollectionInfoMethodName = GetCollectionInfoMethodName(collectionType);
                string createCollectionMethodExpr;

                switch (collectionType)
                {
                    case CollectionType.Array:
                    case CollectionType.MemoryOfT:
                    case CollectionType.ReadOnlyMemoryOfT:
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{valueTypeFQN}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                    case CollectionType.IEnumerable:
                    case CollectionType.IDictionary:
                    case CollectionType.IList:
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                    case CollectionType.Stack:
                    case CollectionType.Queue:
                        string addMethod = collectionType == CollectionType.Stack ? "Push" : "Enqueue";
                        string addFuncNamedArg = $"(collection, {ValueVarName}) => collection.{addMethod}({ValueVarName})";
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}>({OptionsLocalVariableName}, {InfoVarName}, addFunc: {addFuncNamedArg})";
                        break;
                    case CollectionType.ImmutableEnumerable:
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}, {valueTypeFQN}>({OptionsLocalVariableName}, {InfoVarName}, createRangeFunc: {typeGenerationSpec.ImmutableCollectionFactoryMethod})";
                        break;
                    case CollectionType.Dictionary:
                    case CollectionType.IDictionaryOfTKeyTValue:
                    case CollectionType.IReadOnlyDictionary:
                        Debug.Assert(keyTypeFQN != null);
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}, {keyTypeFQN!}, {valueTypeFQN}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                    case CollectionType.ImmutableDictionary:
                        Debug.Assert(keyTypeFQN != null);
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}, {keyTypeFQN!}, {valueTypeFQN}>({OptionsLocalVariableName}, {InfoVarName}, createRangeFunc: {typeGenerationSpec.ImmutableCollectionFactoryMethod})";
                        break;
                    default:
                        createCollectionMethodExpr = $"{createCollectionInfoMethodName}<{typeFQN}, {valueTypeFQN}>({OptionsLocalVariableName}, {InfoVarName})";
                        break;
                }

                GenerateTypeInfoFactoryHeader(writer, typeGenerationSpec);

                writer.WriteLine($$"""
                    var {{InfoVarName}} = new {{JsonCollectionInfoValuesTypeRef}}<{{typeFQN}}>
                    {
                        {{ObjectCreatorPropName}} = {{FormatDefaultConstructorExpr(typeGenerationSpec)}},
                        {{SerializeHandlerPropName}} = {{serializeMethodName ?? "null"}}
                    };

                    {{JsonTypeInfoLocalVariableName}} = {{JsonMetadataServicesTypeRef}}.{{createCollectionMethodExpr}};
                    {{JsonTypeInfoLocalVariableName}}.{{NumberHandlingPropName}} = {{FormatNumberHandling(typeGenerationSpec.NumberHandling)}};
                    """);

                GenerateTypeInfoFactoryFooter(writer);

                if (serializeMethodName != null)
                {
                    writer.WriteLine();

                    if (typeGenerationSpec.ClassType == ClassType.Enumerable)
                    {
                        GenerateFastPathFuncForEnumerable(writer, serializeMethodName, typeGenerationSpec);
                    }
                    else
                    {
                        GenerateFastPathFuncForDictionary(writer, serializeMethodName, typeGenerationSpec);
                    }
                }

                return CompleteSourceFileAndReturnText(writer);
            }

            private void GenerateFastPathFuncForEnumerable(SourceWriter writer, string serializeMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CollectionValueType != null);
                TypeGenerationSpec valueTypeGenerationSpec = _typeIndex[typeGenerationSpec.CollectionValueType];

                GenerateFastPathFuncHeader(writer, typeGenerationSpec, serializeMethodName);

                writer.WriteLine($"{WriterVarName}.WriteStartArray();");
                writer.WriteLine();

                string getCurrentElementExpr;
                const string elementVarName = "element";
                switch (typeGenerationSpec.CollectionType)
                {
                    case CollectionType.Array:
                        writer.WriteLine($"for (int i = 0; i < {ValueVarName}.Length; i++)");
                        getCurrentElementExpr = $"{ValueVarName}[i]";
                        break;

                    case CollectionType.MemoryOfT:
                    case CollectionType.ReadOnlyMemoryOfT:
                        writer.WriteLine($"foreach ({valueTypeGenerationSpec.TypeRef.FullyQualifiedName} {elementVarName} in {ValueVarName}.Span)");
                        getCurrentElementExpr = elementVarName;
                        break;

                    case CollectionType.IListOfT:
                    case CollectionType.List:
                    case CollectionType.IList:
                        writer.WriteLine($"for (int i = 0; i < {ValueVarName}.Count; i++)");
                        getCurrentElementExpr = $"{ValueVarName}[i]";
                        break;

                    default:
                        writer.WriteLine($"foreach ({valueTypeGenerationSpec.TypeRef.FullyQualifiedName} {elementVarName} in {ValueVarName})");
                        getCurrentElementExpr = elementVarName;
                        break;
                };

                writer.WriteLine('{');
                writer.Indentation++;

                GenerateSerializeValueStatement(writer, valueTypeGenerationSpec, getCurrentElementExpr);

                writer.Indentation--;
                writer.WriteLine('}');

                writer.WriteLine();
                writer.WriteLine($"{WriterVarName}.WriteEndArray();");

                writer.Indentation--;
                writer.WriteLine('}');
            }

            private void GenerateFastPathFuncForDictionary(SourceWriter writer, string serializeMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                Debug.Assert(typeGenerationSpec.CollectionKeyType != null);
                Debug.Assert(typeGenerationSpec.CollectionValueType != null);

                TypeRef keyType = typeGenerationSpec.CollectionKeyType;
                TypeGenerationSpec valueTypeGenerationSpec = _typeIndex[typeGenerationSpec.CollectionValueType];

                GenerateFastPathFuncHeader(writer, typeGenerationSpec, serializeMethodName);

                writer.WriteLine($"{WriterVarName}.WriteStartObject();");
                writer.WriteLine();

                writer.WriteLine($"foreach ({KeyValuePairTypeRef}<{keyType.FullyQualifiedName}, {valueTypeGenerationSpec.TypeRef.FullyQualifiedName}> entry in {ValueVarName})");
                writer.WriteLine('{');
                writer.Indentation++;

                GenerateSerializePropertyStatement(writer, valueTypeGenerationSpec, propertyNameExpr: "entry.Key", valueExpr: "entry.Value");

                writer.Indentation--;
                writer.WriteLine('}');

                writer.WriteLine();
                writer.WriteLine($"{WriterVarName}.WriteEndObject();");

                writer.Indentation--;
                writer.WriteLine('}');
            }

            private SourceText GenerateForObject(ContextGenerationSpec contextSpec, TypeGenerationSpec typeMetadata)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                ObjectConstructionStrategy constructionStrategy = typeMetadata.ConstructionStrategy;

                string creatorInvocation = FormatDefaultConstructorExpr(typeMetadata);
                string parameterizedCreatorInvocation = constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor
                    ? GetParameterizedCtorInvocationFunc(typeMetadata)
                    : "null";

                string? propInitMethodName = null;
                string? propInitAdapterFunc = null;
                string? ctorParamMetadataInitMethodName = null;
                string? serializeMethodName = null;

                if (ShouldGenerateMetadata(typeMetadata))
                {
                    propInitMethodName = $"{typeFriendlyName}{PropInitMethodNameSuffix}";
                    propInitAdapterFunc = $"_ => {propInitMethodName}({OptionsLocalVariableName})";

                    if (constructionStrategy == ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        ctorParamMetadataInitMethodName = $"{typeFriendlyName}{CtorParamInitMethodNameSuffix}";
                    }
                }

                if (ShouldGenerateSerializationLogic(typeMetadata))
                {
                    serializeMethodName = $"{typeFriendlyName}{SerializeHandlerPropName}";
                }

                const string ObjectInfoVarName = "objectInfo";
                string genericArg = typeMetadata.TypeRef.FullyQualifiedName;

                GenerateTypeInfoFactoryHeader(writer, typeMetadata);

                writer.WriteLine($$"""
                    var {{ObjectInfoVarName}} = new {{JsonObjectInfoValuesTypeRef}}<{{genericArg}}>
                    {
                        {{ObjectCreatorPropName}} = {{creatorInvocation}},
                        ObjectWithParameterizedConstructorCreator = {{parameterizedCreatorInvocation}},
                        PropertyMetadataInitializer = {{propInitAdapterFunc ?? "null"}},
                        ConstructorParameterMetadataInitializer = {{ctorParamMetadataInitMethodName ?? "null"}},
                        {{SerializeHandlerPropName}} = {{serializeMethodName ?? "null"}}
                    };

                    {{JsonTypeInfoLocalVariableName}} = {{JsonMetadataServicesTypeRef}}.CreateObjectInfo<{{typeMetadata.TypeRef.FullyQualifiedName}}>({{OptionsLocalVariableName}}, {{ObjectInfoVarName}});
                    {{JsonTypeInfoLocalVariableName}}.{{NumberHandlingPropName}} = {{FormatNumberHandling(typeMetadata.NumberHandling)}};
                    """);

                if (typeMetadata is { UnmappedMemberHandling: not null } or { PreferredPropertyObjectCreationHandling: not null })
                {
                    writer.WriteLine();

                    if (typeMetadata.UnmappedMemberHandling != null)
                    {
                        writer.WriteLine($"{JsonTypeInfoLocalVariableName}.{UnmappedMemberHandlingPropName} = {FormatUnmappedMemberHandling(typeMetadata.UnmappedMemberHandling.Value)};");
                    }

                    if (typeMetadata.PreferredPropertyObjectCreationHandling != null)
                    {
                        writer.WriteLine($"{JsonTypeInfoLocalVariableName}.{PreferredPropertyObjectCreationHandlingPropName} = {FormatObjectCreationHandling(typeMetadata.PreferredPropertyObjectCreationHandling.Value)};");
                    }
                }

                GenerateTypeInfoFactoryFooter(writer);

                if (propInitMethodName != null)
                {
                    writer.WriteLine();
                    GeneratePropMetadataInitFunc(writer, propInitMethodName, typeMetadata);
                }

                if (serializeMethodName != null)
                {
                    writer.WriteLine();
                    GenerateFastPathFuncForObject(writer, contextSpec, serializeMethodName, typeMetadata);
                }

                if (ctorParamMetadataInitMethodName != null)
                {
                    writer.WriteLine();
                    GenerateCtorParamMetadataInitFunc(writer, ctorParamMetadataInitMethodName, typeMetadata);
                }

                writer.Indentation--;
                writer.WriteLine('}');

                return CompleteSourceFileAndReturnText(writer);
            }

            private void GeneratePropMetadataInitFunc(SourceWriter writer, string propInitMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecs;

                writer.WriteLine($"private static {JsonPropertyInfoTypeRef}[] {propInitMethodName}({JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})");
                writer.WriteLine('{');
                writer.Indentation++;

                writer.WriteLine($"var properties = new {JsonPropertyInfoTypeRef}[{properties.Count}];");
                writer.WriteLine();

                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyGenerationSpec property = properties[i];
                    string propertyName = property.NameSpecifiedInSourceCode;
                    string declaringTypeFQN = property.DeclaringType.FullyQualifiedName;
                    string propertyTypeFQN = property.PropertyType.FullyQualifiedName;

                    string getterValue = property switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseGetter: true } => $"static obj => (({declaringTypeFQN})obj).{propertyName}",
                        { CanUseGetter: false, HasJsonInclude: true }
                            => $"""static _ => throw new {InvalidOperationExceptionTypeRef}("{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.TypeRef.Name, propertyName)}")""",
                        _ => "null"
                    };

                    string setterValue = property switch
                    {
                        { DefaultIgnoreCondition: JsonIgnoreCondition.Always } => "null",
                        { CanUseSetter: true, IsInitOnlySetter: true }
                            => $"""static (obj, value) => throw new {InvalidOperationExceptionTypeRef}("{ExceptionMessages.InitOnlyPropertySetterNotSupported}")""",
                        { CanUseSetter: true } when typeGenerationSpec.TypeRef.IsValueType
                            => $"""static (obj, value) => {UnsafeTypeRef}.Unbox<{declaringTypeFQN}>(obj).{propertyName} = value!""",
                        { CanUseSetter: true }
                            => $"""static (obj, value) => (({declaringTypeFQN})obj).{propertyName} = value!""",
                        { CanUseSetter: false, HasJsonInclude: true }
                            => $"""static (obj, value) => throw new {InvalidOperationExceptionTypeRef}("{string.Format(ExceptionMessages.InaccessibleJsonIncludePropertiesNotSupported, typeGenerationSpec.TypeRef.Name, property.MemberName)}")""",
                        _ => "null",
                    };

                    string ignoreConditionNamedArg = property.DefaultIgnoreCondition.HasValue
                        ? $"{JsonIgnoreConditionTypeRef}.{property.DefaultIgnoreCondition.Value}"
                        : "null";

                    string? converterInstantiationExpr = null;
                    if (property.ConverterType != null)
                    {
                        string converterFQN = property.ConverterType.FullyQualifiedName;
                        TypeRef? nullableUnderlyingType = _typeIndex[property.PropertyType].NullableUnderlyingType;
                        _emitGetConverterForNullablePropertyMethod |= nullableUnderlyingType != null;

                        converterInstantiationExpr = nullableUnderlyingType != null
                            ? $"{GetConverterForNullablePropertyMethodName}<{nullableUnderlyingType.FullyQualifiedName}>(new {converterFQN}(), {OptionsLocalVariableName})"
                            : $"({JsonConverterTypeRef}<{propertyTypeFQN}>){ExpandConverterMethodName}(typeof({propertyTypeFQN}), new {converterFQN}(), {OptionsLocalVariableName})";
                    }

                    writer.WriteLine($$"""
                        var {{InfoVarName}}{{i}} = new {{JsonPropertyInfoValuesTypeRef}}<{{propertyTypeFQN}}>
                        {
                            IsProperty = {{FormatBool(property.IsProperty)}},
                            IsPublic = {{FormatBool(property.IsPublic)}},
                            IsVirtual = {{FormatBool(property.IsVirtual)}},
                            DeclaringType = typeof({{property.DeclaringType.FullyQualifiedName}}),
                            Converter = {{converterInstantiationExpr ?? "null"}},
                            Getter = {{getterValue}},
                            Setter = {{setterValue}},
                            IgnoreCondition = {{ignoreConditionNamedArg}},
                            HasJsonInclude = {{FormatBool(property.HasJsonInclude)}},
                            IsExtensionData = {{FormatBool(property.IsExtensionData)}},
                            NumberHandling = {{FormatNumberHandling(property.NumberHandling)}},
                            PropertyName = {{FormatStringLiteral(property.MemberName)}},
                            JsonPropertyName = {{FormatStringLiteral(property.JsonPropertyName)}}
                        };

                        properties[{{i}}] = {{JsonMetadataServicesTypeRef}}.CreatePropertyInfo<{{propertyTypeFQN}}>({{OptionsLocalVariableName}}, {{InfoVarName}}{{i}});
                        """);

                    if (property.HasJsonRequiredAttribute ||
                        (property.IsRequired && !typeGenerationSpec.ConstructorSetsRequiredParameters))
                    {
                        writer.WriteLine($"properties[{i}].IsRequired = true;");
                    }

                    if (property.ObjectCreationHandling != null)
                    {
                        writer.WriteLine($"properties[{i}].ObjectCreationHandling = {FormatObjectCreationHandling(property.ObjectCreationHandling.Value)};");
                    }

                    if (property.Order != 0)
                    {
                        writer.WriteLine($"properties[{i}].Order = {property.Order};");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine($"return properties;");
                writer.Indentation--;
                writer.WriteLine('}');
            }

            private static void GenerateCtorParamMetadataInitFunc(SourceWriter writer, string ctorParamMetadataInitMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                const string parametersVarName = "parameters";

                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec> propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;
                int paramCount = parameters.Count + propertyInitializers.Count(propInit => !propInit.MatchesConstructorParameter);
                Debug.Assert(paramCount > 0);

                writer.WriteLine($"private static {JsonParameterInfoValuesTypeRef}[] {ctorParamMetadataInitMethodName}()");
                writer.WriteLine('{');
                writer.Indentation++;

                writer.WriteLine($"var {parametersVarName} = new {JsonParameterInfoValuesTypeRef}[{paramCount}];");
                writer.WriteLine();

                foreach (ParameterGenerationSpec spec in parameters)
                {
                    writer.WriteLine($$"""
                        {{parametersVarName}}[{{spec.ParameterIndex}}] = new()
                        {
                            Name = "{{spec.Name}}",
                            ParameterType = typeof({{spec.ParameterType.FullyQualifiedName}}),
                            Position = {{spec.ParameterIndex}},
                            HasDefaultValue = {{FormatBool(spec.HasDefaultValue)}},
                            DefaultValue = {{CSharpSyntaxUtilities.FormatLiteral(spec.DefaultValue, spec.ParameterType)}}
                        };

                        """);
                }

                foreach (PropertyInitializerGenerationSpec spec in propertyInitializers)
                {
                    if (spec.MatchesConstructorParameter)
                    {
                        continue;
                    }

                    writer.WriteLine($$"""
                        {{parametersVarName}}[{{spec.ParameterIndex}}] = new()
                        {
                            Name = "{{spec.Name}}",
                            ParameterType = typeof({{spec.ParameterType.FullyQualifiedName}}),
                            Position = {{spec.ParameterIndex}},
                        };

                        """);
                }

                writer.WriteLine($"return {parametersVarName};");

                writer.Indentation--;
                writer.WriteLine('}');
            }

            private void GenerateFastPathFuncForObject(SourceWriter writer, ContextGenerationSpec contextSpec, string serializeMethodName, TypeGenerationSpec typeGenSpec)
            {
                if (typeGenSpec.FastPathPropertyIndices is null)
                {
                    // Type uses configuration that doesn't support fast-path: emit a stub that just throws.
                    GenerateFastPathFuncHeader(writer, typeGenSpec, serializeMethodName, skipNullCheck: true);

                    string exceptionMessage = string.Format(ExceptionMessages.InvalidSerializablePropertyConfiguration, typeGenSpec.TypeRef.FullyQualifiedName);
                    writer.WriteLine($"""throw new {InvalidOperationExceptionTypeRef}("{exceptionMessage}");""");
                    writer.Indentation--;
                    writer.WriteLine('}');
                    return;
                }

                GenerateFastPathFuncHeader(writer, typeGenSpec, serializeMethodName);

                if (typeGenSpec.ImplementsIJsonOnSerializing)
                {
                    writer.WriteLine($"((global::{JsonConstants.IJsonOnSerializingFullName}){ValueVarName}).OnSerializing();");
                    writer.WriteLine();
                }

                writer.WriteLine($"{WriterVarName}.WriteStartObject();");
                writer.WriteLine();

                // Provide generation logic for each prop.
                foreach (int i in typeGenSpec.FastPathPropertyIndices)
                {
                    PropertyGenerationSpec propertyGenSpec = typeGenSpec.PropertyGenSpecs[i];

                    if (!propertyGenSpec.ShouldIncludePropertyForFastPath(contextSpec))
                    {
                        continue;
                    }

                    TypeGenerationSpec propertyTypeSpec = _typeIndex[propertyGenSpec.PropertyType];

                    if (propertyTypeSpec.ClassType is ClassType.TypeUnsupportedBySourceGen)
                    {
                        continue;
                    }

                    string effectiveJsonPropertyName = propertyGenSpec.EffectiveJsonPropertyName;
                    string propertyNameFieldName = propertyGenSpec.PropertyNameFieldName;

                    // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                    Debug.Assert(!_propertyNames.TryGetValue(effectiveJsonPropertyName, out string? existingName) || existingName == propertyNameFieldName);
                    _propertyNames.TryAdd(effectiveJsonPropertyName, propertyNameFieldName);

                    DefaultCheckType defaultCheckType = GetDefaultCheckType(contextSpec, propertyGenSpec);

                    // For properties whose declared type differs from that of the serialized type
                    // perform an explicit cast -- this is to account for hidden properties or diamond ambiguity.
                    string? objectExpr = propertyGenSpec.DeclaringType != typeGenSpec.TypeRef
                        ? $"(({propertyGenSpec.DeclaringType.FullyQualifiedName}){ValueVarName})"
                        : ValueVarName;

                    string propValueExpr;
                    if (defaultCheckType != DefaultCheckType.None)
                    {
                        // Use temporary variable to evaluate property value only once
                        string localVariableName =  $"__value_{propertyGenSpec.NameSpecifiedInSourceCode}";
                        writer.WriteLine($"{propertyGenSpec.PropertyType.FullyQualifiedName} {localVariableName} = {objectExpr}.{propertyGenSpec.NameSpecifiedInSourceCode};");
                        propValueExpr = localVariableName;
                    }
                    else
                    {
                        propValueExpr = $"{objectExpr}.{propertyGenSpec.NameSpecifiedInSourceCode}";
                    }

                    switch (defaultCheckType)
                    {
                        case DefaultCheckType.Null:
                            writer.WriteLine($"if ({propValueExpr} != null)");
                            writer.WriteLine('{');
                            writer.Indentation++;
                            break;

                        case DefaultCheckType.Default:
                            writer.WriteLine($"if (!{EqualityComparerTypeRef}<{propertyGenSpec.PropertyType.FullyQualifiedName}>.Default.Equals(default, {propValueExpr}))");
                            writer.WriteLine('{');
                            writer.Indentation++;
                            break;
                    }

                    GenerateSerializePropertyStatement(writer, propertyTypeSpec, propertyNameFieldName, propValueExpr);

                    if (defaultCheckType != DefaultCheckType.None)
                    {
                        writer.Indentation--;
                        writer.WriteLine('}');
                    }
                }

                // End method logic.
                writer.WriteLine();
                writer.WriteLine($"{WriterVarName}.WriteEndObject();");

                if (typeGenSpec.ImplementsIJsonOnSerialized)
                {
                    writer.WriteLine();
                    writer.WriteLine($"((global::{JsonConstants.IJsonOnSerializedFullName}){ValueVarName}).OnSerialized();");
                }

                writer.Indentation--;
                writer.WriteLine('}');
            }

            private static string GetParameterizedCtorInvocationFunc(TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec> propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;

                const string ArgsVarName = "args";

                StringBuilder sb = new($"static {ArgsVarName} => new {typeGenerationSpec.TypeRef.FullyQualifiedName}(");

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

                if (propertyInitializers.Count > 0)
                {
                    sb.Append("{ ");
                    foreach (PropertyInitializerGenerationSpec property in propertyInitializers)
                    {
                        sb.Append($"{property.Name} = {GetParamUnboxing(property.ParameterType, property.ParameterIndex)}, ");
                    }

                    sb.Length -= 2; // delete the last ", " token
                    sb.Append(" }");
                }

                return sb.ToString();

                static string GetParamUnboxing(TypeRef type, int index)
                    => $"({type.FullyQualifiedName}){ArgsVarName}[{index}]";
            }

            private static string? GetPrimitiveWriterMethod(TypeGenerationSpec type)
            {
                return type.PrimitiveTypeKind switch
                {
                    JsonPrimitiveTypeKind.Number => "WriteNumber",
                    JsonPrimitiveTypeKind.String or JsonPrimitiveTypeKind.Char => "WriteString",
                    JsonPrimitiveTypeKind.Boolean => "WriteBoolean",
                    JsonPrimitiveTypeKind.ByteArray => "WriteBase64String",
                    _ => null
                };
            }

            private static void GenerateFastPathFuncHeader(SourceWriter writer, TypeGenerationSpec typeGenSpec, string methodName, bool skipNullCheck = false)
            {
                // fast path serializers for reference types always support null inputs.
                string valueTypeRef = typeGenSpec.TypeRef.IsValueType
                    ? typeGenSpec.TypeRef.FullyQualifiedName
                    : typeGenSpec.TypeRef.FullyQualifiedName + "?";

                writer.WriteLine($$"""
                    // Intentionally not a static method because we create a delegate to it. Invoking delegates to instance
                    // methods is almost as fast as virtual calls. Static methods need to go through a shuffle thunk.
                    private void {{methodName}}({{Utf8JsonWriterTypeRef}} {{WriterVarName}}, {{valueTypeRef}} {{ValueVarName}})
                    {
                    """);

                writer.Indentation++;

                if (!skipNullCheck && typeGenSpec.TypeRef.CanBeNull)
                {
                    writer.WriteLine($$"""
                        if ({{ValueVarName}} == null)
                        {
                            {{WriterVarName}}.WriteNullValue();
                            return;
                        }

                        """);
                }
            }

            private static void GenerateSerializeValueStatement(SourceWriter writer, TypeGenerationSpec typeSpec, string valueExpr)
            {
                if (GetPrimitiveWriterMethod(typeSpec) is string primitiveWriterMethod)
                {
                    if (typeSpec.PrimitiveTypeKind is JsonPrimitiveTypeKind.Char)
                    {
                        writer.WriteLine($"{WriterVarName}.{primitiveWriterMethod}Value({valueExpr}.ToString());");
                    }
                    else
                    {
                        writer.WriteLine($"{WriterVarName}.{primitiveWriterMethod}Value({valueExpr});");
                    }
                }
                else
                {
                    if (ShouldGenerateSerializationLogic(typeSpec))
                    {
                        writer.WriteLine($"{typeSpec.TypeInfoPropertyName}{SerializeHandlerPropName}({WriterVarName}, {valueExpr});");
                    }
                    else
                    {
                        writer.WriteLine($"{JsonSerializerTypeRef}.Serialize({WriterVarName}, {valueExpr}, {typeSpec.TypeInfoPropertyName});");
                    }
                }
            }

            private static void GenerateSerializePropertyStatement(SourceWriter writer, TypeGenerationSpec typeSpec, string propertyNameExpr, string valueExpr)
            {
                if (GetPrimitiveWriterMethod(typeSpec) is string primitiveWriterMethod)
                {
                    if (typeSpec.PrimitiveTypeKind is JsonPrimitiveTypeKind.Char)
                    {
                        writer.WriteLine($"{WriterVarName}.{primitiveWriterMethod}({propertyNameExpr}, {valueExpr}.ToString());");
                    }
                    else
                    {
                        writer.WriteLine($"{WriterVarName}.{primitiveWriterMethod}({propertyNameExpr}, {valueExpr});");
                    }
                }
                else
                {
                    writer.WriteLine($"{WriterVarName}.WritePropertyName({propertyNameExpr});");

                    if (ShouldGenerateSerializationLogic(typeSpec))
                    {
                        writer.WriteLine($"{typeSpec.TypeInfoPropertyName}{SerializeHandlerPropName}({WriterVarName}, {valueExpr});");
                    }
                    else
                    {
                        writer.WriteLine($"{JsonSerializerTypeRef}.Serialize({WriterVarName}, {valueExpr}, {typeSpec.TypeInfoPropertyName});");
                    }
                }
            }

            private enum DefaultCheckType
            {
                None,
                Null,
                Default,
            }

            private static DefaultCheckType GetDefaultCheckType(ContextGenerationSpec contextSpec, PropertyGenerationSpec propertySpec)
            {
                return (propertySpec.DefaultIgnoreCondition ?? contextSpec.GeneratedOptionsSpec?.DefaultIgnoreCondition) switch
                {
                    JsonIgnoreCondition.WhenWritingNull => propertySpec.PropertyType.CanBeNull ? DefaultCheckType.Null : DefaultCheckType.None,
                    JsonIgnoreCondition.WhenWritingDefault => propertySpec.PropertyType.CanBeNull ? DefaultCheckType.Null : DefaultCheckType.Default,
                    _ => DefaultCheckType.None,
                };
            }

            private static void GenerateTypeInfoFactoryHeader(SourceWriter writer, TypeGenerationSpec typeMetadata)
            {
                string typeFQN = typeMetadata.TypeRef.FullyQualifiedName;
                string typeInfoPropertyName = typeMetadata.TypeInfoPropertyName;
                string typeInfoFQN = $"{JsonTypeInfoTypeRef}<{typeFQN}>";

                writer.WriteLine($$"""
                    private {{typeInfoFQN}}? _{{typeInfoPropertyName}};

                    /// <summary>
                    /// Defines the source generated JSON serialization contract metadata for a given type.
                    /// </summary>
                    #nullable disable annotations // Marking the property type as nullable-oblivious.
                    public {{typeInfoFQN}} {{typeInfoPropertyName}}
                    #nullable enable annotations
                    {
                        get => _{{typeInfoPropertyName}} ??= ({{typeInfoFQN}}){{OptionsInstanceVariableName}}.GetTypeInfo(typeof({{typeFQN}}));
                    }

                    private {{typeInfoFQN}} {{CreateTypeInfoMethodName(typeMetadata)}}({{JsonSerializerOptionsTypeRef}} {{OptionsLocalVariableName}})
                    {
                        if (!{{TryGetTypeInfoForRuntimeCustomConverterMethodName}}<{{typeFQN}}>({{OptionsLocalVariableName}}, out {{typeInfoFQN}} {{JsonTypeInfoLocalVariableName}}))
                        {
                    """);

                writer.Indentation += 2;
            }

            private static void GenerateTypeInfoFactoryFooter(SourceWriter writer)
            {
                writer.Indentation -= 2;

                // NB OriginatingResolver should be the last property set by the source generator.
                writer.WriteLine($$"""
                        }

                        {{JsonTypeInfoLocalVariableName}}.{{OriginatingResolverPropertyName}} = this;
                        return {{JsonTypeInfoLocalVariableName}};
                    }
                    """);
            }

            private static SourceText GetRootJsonContextImplementation(ContextGenerationSpec contextSpec, bool emitGetConverterForNullablePropertyMethod)
            {
                string contextTypeRef = contextSpec.ContextType.FullyQualifiedName;
                string contextTypeName = contextSpec.ContextType.Name;

                int backTickIndex = contextTypeName.IndexOf('`');
                if (backTickIndex != -1)
                {
                    contextTypeName = contextTypeName.Substring(0, backTickIndex);
                }

                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec, isPrimaryContextSourceFile: true);

                GetLogicForDefaultSerializerOptionsInit(contextSpec.GeneratedOptionsSpec, writer);

                writer.WriteLine();

                writer.WriteLine($$"""
                    /// <summary>
                    /// The default <see cref="{{JsonSerializerContextTypeRef}}"/> associated with a default <see cref="{{JsonSerializerOptionsTypeRef}}"/> instance.
                    /// </summary>
                    public static {{contextTypeRef}} Default { get; } = new {{contextTypeRef}}(new {{JsonSerializerOptionsTypeRef}}({{DefaultOptionsStaticVarName}}));

                    /// <summary>
                    /// The source-generated options associated with this context.
                    /// </summary>
                    protected override {{JsonSerializerOptionsTypeRef}}? GeneratedSerializerOptions { get; } = {{DefaultOptionsStaticVarName}};

                    /// <inheritdoc/>
                    public {{contextTypeName}}() : base(null)
                    {
                    }

                    /// <inheritdoc/>
                    public {{contextTypeName}}({{JsonSerializerOptionsTypeRef}} {{OptionsLocalVariableName}}) : base({{OptionsLocalVariableName}})
                    {
                    }
                    """);

                writer.WriteLine();

                GenerateConverterHelpers(writer, emitGetConverterForNullablePropertyMethod);

                return CompleteSourceFileAndReturnText(writer);
            }

            private static void GetLogicForDefaultSerializerOptionsInit(SourceGenerationOptionsSpec? optionsSpec, SourceWriter writer)
            {
                const string DefaultOptionsFieldDecl = $"private readonly static {JsonSerializerOptionsTypeRef} {DefaultOptionsStaticVarName}";

                if (optionsSpec is null)
                {
                    writer.WriteLine($"{DefaultOptionsFieldDecl} = new();");
                    return;
                }

                if (optionsSpec.Defaults is JsonSerializerDefaults defaults)
                {
                    writer.WriteLine($"{DefaultOptionsFieldDecl} = new({FormatJsonSerializerDefaults(defaults)})");
                }
                else
                {
                    writer.WriteLine($"{DefaultOptionsFieldDecl} = new()");
                }

                writer.WriteLine('{');
                writer.Indentation++;

                if (optionsSpec.AllowOutOfOrderMetadataProperties is bool allowOutOfOrderMetadataProperties)
                    writer.WriteLine($"AllowOutOfOrderMetadataProperties = {FormatBool(allowOutOfOrderMetadataProperties)},");

                if (optionsSpec.AllowTrailingCommas is bool allowTrailingCommas)
                    writer.WriteLine($"AllowTrailingCommas = {FormatBool(allowTrailingCommas)},");

                if (optionsSpec.Converters is { Count: > 0 } converters)
                {
                    writer.WriteLine("Converters =");
                    writer.WriteLine('{');
                    writer.Indentation++;

                    foreach (TypeRef converter in converters)
                    {
                        writer.WriteLine($"new {converter.FullyQualifiedName}(),");
                    }

                    writer.Indentation--;
                    writer.WriteLine("},");
                }

                if (optionsSpec.DefaultBufferSize is int defaultBufferSize)
                    writer.WriteLine($"DefaultBufferSize = {defaultBufferSize},");

                if (optionsSpec.DefaultIgnoreCondition is JsonIgnoreCondition defaultIgnoreCondition)
                    writer.WriteLine($"DefaultIgnoreCondition = {FormatIgnoreCondition(defaultIgnoreCondition)},");

                if (optionsSpec.DictionaryKeyPolicy is JsonKnownNamingPolicy dictionaryKeyPolicy)
                    writer.WriteLine($"DictionaryKeyPolicy = {FormatNamingPolicy(dictionaryKeyPolicy)},");

                if (optionsSpec.IgnoreReadOnlyFields is bool ignoreReadOnlyFields)
                    writer.WriteLine($"IgnoreReadOnlyFields = {FormatBool(ignoreReadOnlyFields)},");

                if (optionsSpec.IgnoreReadOnlyProperties is bool ignoreReadOnlyProperties)
                    writer.WriteLine($"IgnoreReadOnlyProperties = {FormatBool(ignoreReadOnlyProperties)},");

                if (optionsSpec.IncludeFields is bool includeFields)
                    writer.WriteLine($"IncludeFields = {FormatBool(includeFields)},");

                if (optionsSpec.MaxDepth is int maxDepth)
                    writer.WriteLine($"MaxDepth = {maxDepth},");

                if (optionsSpec.NumberHandling is JsonNumberHandling numberHandling)
                    writer.WriteLine($"NumberHandling = {FormatNumberHandling(numberHandling)},");

                if (optionsSpec.PreferredObjectCreationHandling is JsonObjectCreationHandling preferredObjectCreationHandling)
                    writer.WriteLine($"PreferredObjectCreationHandling = {FormatObjectCreationHandling(preferredObjectCreationHandling)},");

                if (optionsSpec.PropertyNameCaseInsensitive is bool propertyNameCaseInsensitive)
                    writer.WriteLine($"PropertyNameCaseInsensitive = {FormatBool(propertyNameCaseInsensitive)},");

                if (optionsSpec.PropertyNamingPolicy is JsonKnownNamingPolicy propertyNamingPolicy)
                    writer.WriteLine($"PropertyNamingPolicy = {FormatNamingPolicy(propertyNamingPolicy)},");

                if (optionsSpec.ReadCommentHandling is JsonCommentHandling readCommentHandling)
                    writer.WriteLine($"ReadCommentHandling = {FormatCommentHandling(readCommentHandling)},");

                if (optionsSpec.UnknownTypeHandling is JsonUnknownTypeHandling unknownTypeHandling)
                    writer.WriteLine($"UnknownTypeHandling = {FormatUnknownTypeHandling(unknownTypeHandling)},");

                if (optionsSpec.UnmappedMemberHandling is JsonUnmappedMemberHandling unmappedMemberHandling)
                    writer.WriteLine($"UnmappedMemberHandling = {FormatUnmappedMemberHandling(unmappedMemberHandling)},");

                if (optionsSpec.WriteIndented is bool writeIndented)
                    writer.WriteLine($"WriteIndented = {FormatBool(writeIndented)},");

                if (optionsSpec.IndentCharacter is char indentCharacter)
                    writer.WriteLine($"IndentCharacter = {FormatIndentChar(indentCharacter)},");

                if (optionsSpec.IndentSize is int indentSize)
                    writer.WriteLine($"IndentSize = {indentSize},");

                writer.Indentation--;
                writer.WriteLine("};");

                static string FormatNamingPolicy(JsonKnownNamingPolicy knownNamingPolicy)
                {
                    string? policyName = knownNamingPolicy switch
                    {
                        JsonKnownNamingPolicy.CamelCase => nameof(JsonNamingPolicy.CamelCase),
                        JsonKnownNamingPolicy.SnakeCaseLower => nameof(JsonNamingPolicy.SnakeCaseLower),
                        JsonKnownNamingPolicy.SnakeCaseUpper => nameof(JsonNamingPolicy.SnakeCaseUpper),
                        JsonKnownNamingPolicy.KebabCaseLower => nameof(JsonNamingPolicy.KebabCaseLower),
                        JsonKnownNamingPolicy.KebabCaseUpper => nameof(JsonNamingPolicy.KebabCaseUpper),
                        _ => null,
                    };

                    return policyName != null
                    ? $"{JsonNamingPolicyTypeRef}.{policyName}"
                    : "null";
                }
            }

            private static void GenerateConverterHelpers(SourceWriter writer, bool emitGetConverterForNullablePropertyMethod)
            {
                // The generic type parameter could capture type parameters from containing types,
                // so use a name that is unlikely to be used.
                const string TypeParameter = "TJsonMetadataType";

                writer.WriteLine($$"""
                    private static bool {{TryGetTypeInfoForRuntimeCustomConverterMethodName}}<{{TypeParameter}}>({{JsonSerializerOptionsTypeRef}} options, out {{JsonTypeInfoTypeRef}}<{{TypeParameter}}> jsonTypeInfo)
                    {
                        {{JsonConverterTypeRef}}? converter = GetRuntimeConverterForType(typeof({{TypeParameter}}), options);
                        if (converter != null)
                        {
                            jsonTypeInfo = {{JsonMetadataServicesTypeRef}}.{{CreateValueInfoMethodName}}<{{TypeParameter}}>(options, converter);
                            return true;
                        }

                        jsonTypeInfo = null;
                        return false;
                    }

                    private static {{JsonConverterTypeRef}}? GetRuntimeConverterForType({{TypeTypeRef}} type, {{JsonSerializerOptionsTypeRef}} options)
                    {
                        for (int i = 0; i < options.Converters.Count; i++)
                        {
                            {{JsonConverterTypeRef}}? converter = options.Converters[i];
                            if (converter?.CanConvert(type) == true)
                            {
                                return {{ExpandConverterMethodName}}(type, converter, options, validateCanConvert: false);
                            }
                        }

                        return null;
                    }

                    private static {{JsonConverterTypeRef}} {{ExpandConverterMethodName}}({{TypeTypeRef}} type, {{JsonConverterTypeRef}} converter, {{JsonSerializerOptionsTypeRef}} options, bool validateCanConvert = true)
                    {
                        if (validateCanConvert && !converter.CanConvert(type))
                        {
                            throw new {{InvalidOperationExceptionTypeRef}}(string.Format("{{ExceptionMessages.IncompatibleConverterType}}", converter.GetType(), type));
                        }
                    
                        if (converter is {{JsonConverterFactoryTypeRef}} factory)
                        {
                            converter = factory.CreateConverter(type, options);
                            if (converter is null || converter is {{JsonConverterFactoryTypeRef}})
                            {
                                throw new {{InvalidOperationExceptionTypeRef}}(string.Format("{{ExceptionMessages.InvalidJsonConverterFactoryOutput}}", factory.GetType()));
                            }
                        }
                    
                        return converter;
                    }
                    """);

                if (emitGetConverterForNullablePropertyMethod)
                {
                    writer.WriteLine($$"""

                        private static {{JsonConverterTypeRef}}<{{TypeParameter}}?> {{GetConverterForNullablePropertyMethodName}}<{{TypeParameter}}>({{JsonConverterTypeRef}} converter, {{JsonSerializerOptionsTypeRef}} options)
                            where {{TypeParameter}} : struct
                        {
                            if (converter.CanConvert(typeof({{TypeParameter}}?)))
                            {
                                return ({{JsonConverterTypeRef}}<{{TypeParameter}}?>){{ExpandConverterMethodName}}(typeof({{TypeParameter}}?), converter, options, validateCanConvert: false);
                            }
                    
                            converter = {{ExpandConverterMethodName}}(typeof({{TypeParameter}}), converter, options);
                            {{JsonTypeInfoTypeRef}}<{{TypeParameter}}> typeInfo = {{JsonMetadataServicesTypeRef}}.{{CreateValueInfoMethodName}}<{{TypeParameter}}>(options, converter);
                            return {{JsonMetadataServicesTypeRef}}.GetNullableConverter<{{TypeParameter}}>(typeInfo);
                        }
                        """);
                }
            }

            private static SourceText GetGetTypeInfoImplementation(ContextGenerationSpec contextSpec)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec, interfaceImplementation: JsonTypeInfoResolverTypeRef);

                // JsonSerializerContext.GetTypeInfo override -- returns cached metadata via JsonSerializerOptions
                writer.WriteLine($$"""
                    /// <inheritdoc/>
                    public override {{JsonTypeInfoTypeRef}}? GetTypeInfo({{TypeTypeRef}} type)
                    {
                        {{OptionsInstanceVariableName}}.TryGetTypeInfo(type, out {{JsonTypeInfoTypeRef}}? typeInfo);
                        return typeInfo;
                    }
                    """);

                writer.WriteLine();

                // Explicit IJsonTypeInfoResolver implementation -- the source of truth for metadata resolution
                writer.WriteLine($"{JsonTypeInfoTypeRef}? {JsonTypeInfoResolverTypeRef}.GetTypeInfo({TypeTypeRef} type, {JsonSerializerOptionsTypeRef} {OptionsLocalVariableName})");
                writer.WriteLine('{');
                writer.Indentation++;

                foreach (TypeGenerationSpec metadata in contextSpec.GeneratedTypes)
                {
                    if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                    {
                        writer.WriteLine($$"""
                            if (type == typeof({{metadata.TypeRef.FullyQualifiedName}}))
                            {
                                return {{CreateTypeInfoMethodName(metadata)}}({{OptionsLocalVariableName}});
                            }
                            """);
                    }
                }

                writer.WriteLine("return null;");

                writer.Indentation--;
                writer.WriteLine('}');

                return CompleteSourceFileAndReturnText(writer);
            }

            private SourceText GetPropertyNameInitialization(ContextGenerationSpec contextSpec)
            {
                SourceWriter writer = CreateSourceWriterWithContextHeader(contextSpec);

                foreach (KeyValuePair<string, string> name_varName_pair in _propertyNames)
                {
                    writer.WriteLine($$"""private static readonly {{JsonEncodedTextTypeRef}} {{name_varName_pair.Value}} = {{JsonEncodedTextTypeRef}}.Encode("{{name_varName_pair.Key}}");""");
                }

                return CompleteSourceFileAndReturnText(writer);
            }

            private static string FormatNumberHandling(JsonNumberHandling? numberHandling)
                => numberHandling.HasValue
                ? SourceGeneratorHelpers.FormatEnumLiteral(JsonNumberHandlingTypeRef, numberHandling.Value)
                : "null";

            private static string FormatObjectCreationHandling(JsonObjectCreationHandling creationHandling)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonObjectCreationHandlingTypeRef, creationHandling);

            private static string FormatUnmappedMemberHandling(JsonUnmappedMemberHandling unmappedMemberHandling)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonUnmappedMemberHandlingTypeRef, unmappedMemberHandling);

            private static string FormatCommentHandling(JsonCommentHandling commentHandling)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonCommentHandlingTypeRef, commentHandling);

            private static string FormatUnknownTypeHandling(JsonUnknownTypeHandling commentHandling)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonUnknownTypeHandlingTypeRef, commentHandling);

            private static string FormatIgnoreCondition(JsonIgnoreCondition ignoreCondition)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonIgnoreConditionTypeRef, ignoreCondition);

            private static string FormatJsonSerializerDefaults(JsonSerializerDefaults defaults)
                => SourceGeneratorHelpers.FormatEnumLiteral(JsonSerializerDefaultsTypeRef, defaults);

            private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";

            private static string FormatBool(bool value) => value ? "true" : "false";
            private static string FormatStringLiteral(string? value) => value is null ? "null" : $"\"{value}\"";
            private static string FormatIndentChar(char value) => value is '\t' ? "'\\t'" : $"'{value}'";

            /// <summary>
            /// Method used to generate JsonTypeInfo given options instance
            /// </summary>
            private static string CreateTypeInfoMethodName(TypeGenerationSpec typeSpec)
                => $"Create_{typeSpec.TypeInfoPropertyName}";


            private static string FormatDefaultConstructorExpr(TypeGenerationSpec typeSpec)
            {
                return typeSpec switch
                {
                    { RuntimeTypeRef: TypeRef runtimeType } => $"() => new {runtimeType.FullyQualifiedName}()",
                    { IsValueTuple: true } => $"() => default({typeSpec.TypeRef.FullyQualifiedName})",
                    { ConstructionStrategy: ObjectConstructionStrategy.ParameterlessConstructor } => $"() => new {typeSpec.TypeRef.FullyQualifiedName}()",
                    _ => "null",
                };
            }

            private static string GetCollectionInfoMethodName(CollectionType collectionType)
            {
                return collectionType switch
                {
                    CollectionType.Array => "CreateArrayInfo",
                    CollectionType.List => "CreateListInfo",
                    CollectionType.IListOfT or CollectionType.IList => "CreateIListInfo",
                    CollectionType.ICollectionOfT => "CreateICollectionInfo",
                    CollectionType.IEnumerableOfT or CollectionType.IEnumerable => "CreateIEnumerableInfo",
                    CollectionType.StackOfT or CollectionType.Stack => "CreateStackInfo",
                    CollectionType.QueueOfT or CollectionType.Queue => "CreateQueueInfo",
                    CollectionType.ConcurrentStack => "CreateConcurrentStackInfo",
                    CollectionType.ConcurrentQueue => "CreateConcurrentQueueInfo",
                    CollectionType.ImmutableEnumerable => "CreateImmutableEnumerableInfo",
                    CollectionType.IAsyncEnumerableOfT => "CreateIAsyncEnumerableInfo",
                    CollectionType.MemoryOfT => "CreateMemoryInfo",
                    CollectionType.ReadOnlyMemoryOfT => "CreateReadOnlyMemoryInfo",
                    CollectionType.ISet => "CreateISetInfo",

                    CollectionType.Dictionary => "CreateDictionaryInfo",
                    CollectionType.IDictionaryOfTKeyTValue or CollectionType.IDictionary => "CreateIDictionaryInfo",
                    CollectionType.IReadOnlyDictionary => "CreateIReadOnlyDictionaryInfo",
                    CollectionType.ImmutableDictionary => "CreateImmutableDictionaryInfo",

                    _ => throw new Exception(),
                };
            }

            private static bool ShouldGenerateMetadata(TypeGenerationSpec typeSpec)
                => IsGenerationModeSpecified(typeSpec, JsonSourceGenerationMode.Metadata);

            private static bool ShouldGenerateSerializationLogic(TypeGenerationSpec typeSpec)
                => IsGenerationModeSpecified(typeSpec, JsonSourceGenerationMode.Serialization) && typeSpec.IsFastPathSupported();

            private static bool IsGenerationModeSpecified(TypeGenerationSpec typeSpec, JsonSourceGenerationMode mode)
                => typeSpec.GenerationMode == JsonSourceGenerationMode.Default || (mode & typeSpec.GenerationMode) != 0;
        }
    }
}
