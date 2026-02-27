// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.CSharp;
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
            private const string InstanceMemberBindingFlagsVariableName = "InstanceMemberBindingFlags";
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
            private const string ValueTypeSetterDelegateName = "ValueTypeSetter";
            private const string PreserveReferenceHandlerPropertyName = "Preserve";
            private const string IgnoreCyclesReferenceHandlerPropertyName = "IgnoreCycles";

            private static readonly AssemblyName s_assemblyName = typeof(Emitter).Assembly.GetName();

            // global::fully.qualified.name for referenced types
            private const string InvalidOperationExceptionTypeRef = "global::System.InvalidOperationException";
            private const string JsonExceptionTypeRef = "global::System.Text.Json.JsonException";
            private const string TypeTypeRef = "global::System.Type";
            private const string UnsafeTypeRef = "global::System.Runtime.CompilerServices.Unsafe";
            private const string EqualityComparerTypeRef = "global::System.Collections.Generic.EqualityComparer";
            private const string KeyValuePairTypeRef = "global::System.Collections.Generic.KeyValuePair";
            private const string UnsafeAccessorAttributeTypeRef = "global::System.Runtime.CompilerServices.UnsafeAccessorAttribute";
            private const string UnsafeAccessorKindTypeRef = "global::System.Runtime.CompilerServices.UnsafeAccessorKind";
            private const string BindingFlagsTypeRef = "global::System.Reflection.BindingFlags";
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
            private const string ReferenceHandlerTypeRef = "global::System.Text.Json.Serialization.ReferenceHandler";
            private const string EmptyTypeArray = "global::System.Array.Empty<global::System.Type>()";

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
            /// Indicates that a value type property setter uses the reflection fallback,
            /// requiring the <c>ValueTypeSetter</c> delegate type to be emitted.
            /// </summary>
            private bool _emitValueTypeSetterDelegate;

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
                AddSource($"{contextName}.g.cs", GetRootJsonContextImplementation(contextGenerationSpec, _emitGetConverterForNullablePropertyMethod, _emitValueTypeSetterDelegate));

                // Add GetJsonTypeInfo override implementation.
                AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation(contextGenerationSpec));

                // Add property name initialization.
                AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization(contextGenerationSpec));

                _emitGetConverterForNullablePropertyMethod = false;
                _emitValueTypeSetterDelegate = false;
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
                string? constructorInfoFactoryFunc = null;
                string? ctorParamMetadataInitMethodName = null;
                string? serializeMethodName = null;

                if (ShouldGenerateMetadata(typeMetadata))
                {
                    propInitMethodName = $"{typeFriendlyName}{PropInitMethodNameSuffix}";
                    propInitAdapterFunc = $"_ => {propInitMethodName}({OptionsLocalVariableName})";

                    if (constructionStrategy is ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        ctorParamMetadataInitMethodName = $"{typeFriendlyName}{CtorParamInitMethodNameSuffix}";
                    }

                    if (constructionStrategy is ObjectConstructionStrategy.ParameterlessConstructor
                                             or ObjectConstructionStrategy.ParameterizedConstructor)
                    {
                        string argTypes = typeMetadata.CtorParamGenSpecs.Count == 0
                            ? EmptyTypeArray
                            : $$"""new[] {{{string.Join(", ", typeMetadata.CtorParamGenSpecs.Select(p => $"typeof({p.ParameterType.FullyQualifiedName})"))}}}""";

                        constructorInfoFactoryFunc = $"static () => typeof({typeMetadata.TypeRef.FullyQualifiedName}).GetConstructor({InstanceMemberBindingFlagsVariableName}, binder: null, {argTypes}, modifiers: null)";
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
                        ConstructorAttributeProviderFactory = {{constructorInfoFactoryFunc ?? "null"}},
                        {{SerializeHandlerPropName}} = {{serializeMethodName ?? "null"}},
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

                // Generate UnsafeAccessor methods or reflection cache fields for property accessors.
                _emitValueTypeSetterDelegate |= GeneratePropertyAccessors(writer, typeMetadata);

                // Generate constructor accessor for inaccessible [JsonConstructor] constructors.
                GenerateConstructorAccessor(writer, typeMetadata);

                writer.Indentation--;
                writer.WriteLine('}');

                return CompleteSourceFileAndReturnText(writer);
            }

            private void GeneratePropMetadataInitFunc(SourceWriter writer, string propInitMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecs;
                HashSet<string> duplicateMemberNames = GetDuplicateMemberNames(properties);

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

                    // If the property is ignored and its type is not used anywhere else in the type graph,
                    // emit a JsonPropertyInfo of type 'object' to avoid unnecessarily referencing the type.
                    // STJ requires that all ignored properties be included so that it can perform
                    // necessary run-time validations using configuration not known at compile time
                    // such as the property naming policy and case sensitivity.
                    bool isIgnoredPropertyOfUnusedType =
                        property.DefaultIgnoreCondition is JsonIgnoreCondition.Always &&
                        !_typeIndex.ContainsKey(property.PropertyType);

                    string propertyTypeFQN = isIgnoredPropertyOfUnusedType ? "object" : property.PropertyType.FullyQualifiedName;

                    string getterValue = GetPropertyGetterValue(property, typeGenerationSpec, propertyName, declaringTypeFQN, i, duplicateMemberNames.Contains(property.MemberName));
                    string setterValue = GetPropertySetterValue(property, typeGenerationSpec, propertyName, declaringTypeFQN, i, duplicateMemberNames.Contains(property.MemberName));

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

                    string attributeProviderFactoryExpr = property switch
                    {
                        _ when isIgnoredPropertyOfUnusedType => "null",
                        { IsProperty: true } => $"typeof({property.DeclaringType.FullyQualifiedName}).GetProperty({FormatStringLiteral(property.MemberName)}, {InstanceMemberBindingFlagsVariableName}, null, typeof({propertyTypeFQN}), {EmptyTypeArray}, null)",
                        _ => $"typeof({property.DeclaringType.FullyQualifiedName}).GetField({FormatStringLiteral(property.MemberName)}, {InstanceMemberBindingFlagsVariableName})",
                    };

                    writer.WriteLine($$"""
                        var {{InfoVarName}}{{i}} = new {{JsonPropertyInfoValuesTypeRef}}<{{propertyTypeFQN}}>
                        {
                            IsProperty = {{FormatBoolLiteral(property.IsProperty)}},
                            IsPublic = {{FormatBoolLiteral(property.IsPublic)}},
                            IsVirtual = {{FormatBoolLiteral(property.IsVirtual)}},
                            DeclaringType = typeof({{property.DeclaringType.FullyQualifiedName}}),
                            Converter = {{converterInstantiationExpr ?? "null"}},
                            Getter = {{getterValue}},
                            Setter = {{setterValue}},
                            IgnoreCondition = {{ignoreConditionNamedArg}},
                            HasJsonInclude = {{FormatBoolLiteral(property.HasJsonInclude)}},
                            IsExtensionData = {{FormatBoolLiteral(property.IsExtensionData)}},
                            NumberHandling = {{FormatNumberHandling(property.NumberHandling)}},
                            PropertyName = {{FormatStringLiteral(property.MemberName)}},
                            JsonPropertyName = {{FormatStringLiteral(property.JsonPropertyName)}},
                            AttributeProviderFactory = static () => {{attributeProviderFactoryExpr}},
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

                    if (property.IsGetterNonNullableAnnotation)
                    {
                        writer.WriteLine($"properties[{i}].IsGetNullable = false;");
                    }
                    if (property.IsSetterNonNullableAnnotation)
                    {
                        writer.WriteLine($"properties[{i}].IsSetNullable = false;");
                    }

                    writer.WriteLine();
                }

                writer.WriteLine($"return properties;");
                writer.Indentation--;
                writer.WriteLine('}');
            }

            /// <summary>
            /// Returns true if the property requires an unsafe accessor or reflection fallback
            /// for its getter (i.e. it's inaccessible but has [JsonInclude]).
            /// </summary>
            private static bool NeedsAccessorForGetter(PropertyGenerationSpec property)
                => !property.CanUseGetter && property.HasJsonInclude && property.DefaultIgnoreCondition is not JsonIgnoreCondition.Always;

            /// <summary>
            /// Returns true if the property requires an unsafe accessor or reflection fallback
            /// for its setter (i.e. init-only properties, or inaccessible with [JsonInclude]).
            /// </summary>
            private static bool NeedsAccessorForSetter(PropertyGenerationSpec property)
            {
                if (property.DefaultIgnoreCondition is JsonIgnoreCondition.Always)
                {
                    return false;
                }

                // All init-only properties need an accessor.
                if (property is { CanUseSetter: true, IsInitOnlySetter: true })
                {
                    return true;
                }

                // Inaccessible [JsonInclude] properties need an accessor.
                if (!property.CanUseSetter && property.HasJsonInclude)
                {
                    return true;
                }

                return false;
            }

            private static string GetPropertyGetterValue(
                PropertyGenerationSpec property,
                TypeGenerationSpec typeGenerationSpec,
                string propertyName,
                string declaringTypeFQN,
                int propertyIndex,
                bool needsDisambiguation)
            {
                if (property.DefaultIgnoreCondition is JsonIgnoreCondition.Always)
                {
                    return "null";
                }

                if (property.CanUseGetter)
                {
                    return $"static obj => (({declaringTypeFQN})obj).{propertyName}";
                }

                if (NeedsAccessorForGetter(property))
                {
                    string typeFriendlyName = typeGenerationSpec.TypeInfoPropertyName;

                    if (property.CanUseUnsafeAccessors)
                    {
                        // UnsafeAccessor externs for value types take 'ref T'.
                        string castExpr = typeGenerationSpec.TypeRef.IsValueType
                            ? $"ref {UnsafeTypeRef}.Unbox<{declaringTypeFQN}>(obj)"
                            : $"({declaringTypeFQN})obj";

                        string accessorName = property.IsProperty
                            ? GetAccessorName(typeFriendlyName, "get", property.MemberName, propertyIndex, needsDisambiguation)
                            : GetAccessorName(typeFriendlyName, "field", property.MemberName, propertyIndex, needsDisambiguation);

                        return $"static obj => {accessorName}({castExpr})";
                    }

                    // Reflection fallback wrappers are strongly typed; cast in the delegate.
                    string getterName = GetAccessorName(typeFriendlyName, "get", property.MemberName, propertyIndex, needsDisambiguation);

                    return $"static obj => {getterName}(({declaringTypeFQN})obj)";
                }

                return "null";
            }

            private static string GetPropertySetterValue(
                PropertyGenerationSpec property,
                TypeGenerationSpec typeGenerationSpec,
                string propertyName,
                string declaringTypeFQN,
                int propertyIndex,
                bool needsDisambiguation)
            {
                if (property.DefaultIgnoreCondition is JsonIgnoreCondition.Always)
                {
                    return "null";
                }

                if (property is { CanUseSetter: true, IsInitOnlySetter: true })
                {
                    return GetAccessorBasedSetterDelegate(property, typeGenerationSpec, declaringTypeFQN, propertyIndex, needsDisambiguation);
                }

                if (property.CanUseSetter)
                {
                    return typeGenerationSpec.TypeRef.IsValueType
                        ? $"""static (obj, value) => {UnsafeTypeRef}.Unbox<{declaringTypeFQN}>(obj).{propertyName} = value!"""
                        : $"""static (obj, value) => (({declaringTypeFQN})obj).{propertyName} = value!""";
                }

                if (NeedsAccessorForSetter(property))
                {
                    return GetAccessorBasedSetterDelegate(property, typeGenerationSpec, declaringTypeFQN, propertyIndex, needsDisambiguation);
                }

                return "null";
            }

            /// <summary>
            /// Generates a setter delegate expression that calls the UnsafeAccessor extern directly
            /// or the strongly typed reflection wrapper.
            /// </summary>
            private static string GetAccessorBasedSetterDelegate(
                PropertyGenerationSpec property,
                TypeGenerationSpec typeGenerationSpec,
                string declaringTypeFQN,
                int propertyIndex,
                bool needsDisambiguation)
            {
                string typeFriendlyName = typeGenerationSpec.TypeInfoPropertyName;

                if (property.CanUseUnsafeAccessors)
                {
                    string castExpr = typeGenerationSpec.TypeRef.IsValueType
                        ? $"ref {UnsafeTypeRef}.Unbox<{declaringTypeFQN}>(obj)"
                        : $"({declaringTypeFQN})obj";

                    if (property.IsProperty)
                    {
                        string accessorName = GetAccessorName(typeFriendlyName, "set", property.MemberName, propertyIndex, needsDisambiguation);
                        return $"static (obj, value) => {accessorName}({castExpr}, value!)";
                    }

                    string fieldName = GetAccessorName(typeFriendlyName, "field", property.MemberName, propertyIndex, needsDisambiguation);
                    return $"static (obj, value) => {fieldName}({castExpr}) = value!";
                }

                // Reflection fallback wrapper is strongly typed; cast in the delegate like UnsafeAccessor.
                string setterName = GetAccessorName(typeFriendlyName, "set", property.MemberName, propertyIndex, needsDisambiguation);
                string setterCastExpr = typeGenerationSpec.TypeRef.IsValueType
                    ? $"ref {UnsafeTypeRef}.Unbox<{declaringTypeFQN}>(obj)"
                    : $"({declaringTypeFQN})obj";

                return $"static (obj, value) => {setterName}({setterCastExpr}, value!)";
            }

            private static bool GeneratePropertyAccessors(SourceWriter writer, TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<PropertyGenerationSpec> properties = typeGenerationSpec.PropertyGenSpecs;
                HashSet<string> duplicateMemberNames = GetDuplicateMemberNames(properties);
                bool needsAccessors = false;
                bool needsValueTypeSetterDelegate = false;

                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyGenerationSpec property = properties[i];
                    bool needsGetterAccessor = NeedsAccessorForGetter(property);
                    bool needsSetterAccessor = NeedsAccessorForSetter(property);

                    if (!needsGetterAccessor && !needsSetterAccessor)
                    {
                        continue;
                    }

                    if (!needsAccessors)
                    {
                        writer.WriteLine();
                        needsAccessors = true;
                    }

                    string typeFriendlyName = typeGenerationSpec.TypeInfoPropertyName;
                    string declaringTypeFQN = property.DeclaringType.FullyQualifiedName;
                    string propertyTypeFQN = property.PropertyType.FullyQualifiedName;
                    bool disambiguate = duplicateMemberNames.Contains(property.MemberName);

                    if (property.CanUseUnsafeAccessors)
                    {
                        string refPrefix = typeGenerationSpec.TypeRef.IsValueType ? "ref " : "";

                        if (property.IsProperty)
                        {
                            if (needsGetterAccessor)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "get", property.MemberName, i, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "get_{property.MemberName}")]""");
                                writer.WriteLine($"private static extern {propertyTypeFQN} {accessorName}({refPrefix}{declaringTypeFQN} obj);");
                            }

                            if (needsSetterAccessor)
                            {
                                string accessorName = GetAccessorName(typeFriendlyName, "set", property.MemberName, i, disambiguate);
                                writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Method, Name = "set_{property.MemberName}")]""");
                                writer.WriteLine($"private static extern void {accessorName}({refPrefix}{declaringTypeFQN} obj, {propertyTypeFQN} value);");
                            }
                        }
                        else
                        {
                            // Field: single UnsafeAccessor that returns ref T, used for both get and set.
                            string fieldAccessorName = GetAccessorName(typeFriendlyName, "field", property.MemberName, i, disambiguate);
                            writer.WriteLine($"""[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Field, Name = "{property.MemberName}")]""");
                            writer.WriteLine($"private static extern ref {propertyTypeFQN} {fieldAccessorName}({refPrefix}{declaringTypeFQN} obj);");
                        }
                    }
                    else if (property.IsProperty)
                    {
                        // Reflection fallback for properties: use Delegate.CreateDelegate on the MethodInfo for efficient invocation.
                        // Wrapper methods are strongly typed to match UnsafeAccessor signatures.
                        string propertyExpr = $"typeof({declaringTypeFQN}).GetProperty({FormatStringLiteral(property.MemberName)}, {BindingFlagsTypeRef}.Instance | {BindingFlagsTypeRef}.Public | {BindingFlagsTypeRef}.NonPublic)!";

                        if (needsGetterAccessor)
                        {
                            string cacheName = GetReflectionCacheName(typeFriendlyName, "get", property.MemberName, i, disambiguate);
                            string wrapperName = GetAccessorName(typeFriendlyName, "get", property.MemberName, i, disambiguate);
                            string delegateType = $"global::System.Func<{declaringTypeFQN}, {propertyTypeFQN}>";
                            writer.WriteLine($"private static {delegateType}? {cacheName};");
                            writer.WriteLine($"private static {propertyTypeFQN} {wrapperName}({declaringTypeFQN} obj) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetGetMethod(true)!))(obj);");
                        }

                        if (needsSetterAccessor)
                        {
                            string cacheName = GetReflectionCacheName(typeFriendlyName, "set", property.MemberName, i, disambiguate);
                            string wrapperName = GetAccessorName(typeFriendlyName, "set", property.MemberName, i, disambiguate);

                            if (typeGenerationSpec.TypeRef.IsValueType)
                            {
                                // For value types, use a ref-parameter delegate to mutate the unboxed value in-place.
                                needsValueTypeSetterDelegate = true;
                                string delegateType = $"{ValueTypeSetterDelegateName}<{declaringTypeFQN}, {propertyTypeFQN}>";
                                writer.WriteLine($"private static {delegateType}? {cacheName};");
                                writer.WriteLine($"private static void {wrapperName}(ref {declaringTypeFQN} obj, {propertyTypeFQN} value) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetSetMethod(true)!))(ref obj, value);");
                            }
                            else
                            {
                                string delegateType = $"global::System.Action<{declaringTypeFQN}, {propertyTypeFQN}>";
                                writer.WriteLine($"private static {delegateType}? {cacheName};");
                                writer.WriteLine($"private static void {wrapperName}({declaringTypeFQN} obj, {propertyTypeFQN} value) => ({cacheName} ??= ({delegateType})global::System.Delegate.CreateDelegate(typeof({delegateType}), {propertyExpr}.GetSetMethod(true)!))(obj, value);");
                            }
                        }
                    }
                    else
                    {
                        // Reflection fallback for fields: use FieldInfo.GetValue/SetValue (fields don't have MethodInfo).
                        string fieldExpr = $"typeof({declaringTypeFQN}).GetField({FormatStringLiteral(property.MemberName)}, {BindingFlagsTypeRef}.Instance | {BindingFlagsTypeRef}.Public | {BindingFlagsTypeRef}.NonPublic)!";

                        if (needsGetterAccessor)
                        {
                            string cacheName = GetReflectionCacheName(typeFriendlyName, "get", property.MemberName, i, disambiguate);
                            string wrapperName = GetAccessorName(typeFriendlyName, "get", property.MemberName, i, disambiguate);
                            writer.WriteLine($"private static global::System.Func<object?, object?>? {cacheName};");
                            writer.WriteLine($"private static {propertyTypeFQN} {wrapperName}(object obj) => ({propertyTypeFQN})({cacheName} ??= {fieldExpr}.GetValue)(obj)!;");
                        }

                        if (needsSetterAccessor)
                        {
                            string cacheName = GetReflectionCacheName(typeFriendlyName, "set", property.MemberName, i, disambiguate);
                            string wrapperName = GetAccessorName(typeFriendlyName, "set", property.MemberName, i, disambiguate);
                            writer.WriteLine($"private static global::System.Action<object?, object?>? {cacheName};");
                            writer.WriteLine($"private static void {wrapperName}(object obj, {propertyTypeFQN} value) => ({cacheName} ??= {fieldExpr}.SetValue)(obj, value);");
                        }
                    }
                }

                return needsValueTypeSetterDelegate;
            }

            /// <summary>
            /// Gets the accessor name for a property or field. For UnsafeAccessor this is the extern method name;
            /// for reflection fallback this is the strongly typed wrapper method name.
            /// Use kind "get"/"set" for property getters/setters, or "field" for field UnsafeAccessor externs.
            /// The property index suffix is only appended when needed to disambiguate shadowed members.
            /// </summary>
            private static string GetAccessorName(string typeFriendlyName, string accessorKind, string memberName, int propertyIndex, bool needsDisambiguation)
                => needsDisambiguation
                    ? $"__{accessorKind}_{typeFriendlyName}_{memberName}_{propertyIndex}"
                    : $"__{accessorKind}_{typeFriendlyName}_{memberName}";

            private static string GetReflectionCacheName(string typeFriendlyName, string accessorKind, string memberName, int propertyIndex, bool needsDisambiguation)
                => needsDisambiguation
                    ? $"s_{accessorKind}_{typeFriendlyName}_{memberName}_{propertyIndex}"
                    : $"s_{accessorKind}_{typeFriendlyName}_{memberName}";

            /// <summary>
            /// Returns the set of member names that appear more than once in the property list.
            /// This occurs when derived types shadow base members via the <c>new</c> keyword.
            /// </summary>
            private static HashSet<string> GetDuplicateMemberNames(ImmutableEquatableArray<PropertyGenerationSpec> properties)
            {
                HashSet<string> seen = new();
                HashSet<string> duplicates = new();
                foreach (PropertyGenerationSpec property in properties)
                {
                    if (!seen.Add(property.MemberName))
                    {
                        duplicates.Add(property.MemberName);
                    }
                }

                return duplicates;
            }

            /// <summary>
            /// Gets the unified constructor accessor name. The wrapper has the same
            /// signature for both UnsafeAccessor and reflection fallback:
            /// <c>static TypeName __ctor_TypeName(params)</c>
            /// </summary>
            private static string GetConstructorAccessorName(TypeGenerationSpec typeSpec)
                => $"__ctor_{typeSpec.TypeInfoPropertyName}";

            private static string GetConstructorReflectionCacheName(TypeGenerationSpec typeSpec)
                => $"s_ctor_{typeSpec.TypeInfoPropertyName}";

            /// <summary>
            /// Generates the constructor accessor for inaccessible constructors.
            /// For UnsafeAccessor: emits a [UnsafeAccessor(Constructor)] extern method.
            /// For reflection fallback: emits a cached ConstructorInfo and a wrapper method.
            /// </summary>
            private static void GenerateConstructorAccessor(SourceWriter writer, TypeGenerationSpec typeSpec)
            {
                if (!typeSpec.ConstructorIsInaccessible)
                {
                    return;
                }

                writer.WriteLine();

                string typeFQN = typeSpec.TypeRef.FullyQualifiedName;
                string wrapperName = GetConstructorAccessorName(typeSpec);
                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeSpec.CtorParamGenSpecs;

                // Build the parameter list for the wrapper method.
                var wrapperParams = new StringBuilder();
                var callArgs = new StringBuilder();

                foreach (ParameterGenerationSpec param in parameters)
                {
                    if (wrapperParams.Length > 0)
                    {
                        wrapperParams.Append(", ");
                        callArgs.Append(", ");
                    }

                    wrapperParams.Append($"{param.ParameterType.FullyQualifiedName} p{param.ParameterIndex}");
                    callArgs.Append($"p{param.ParameterIndex}");
                }

                if (typeSpec.CanUseUnsafeAccessorForConstructor)
                {
                    writer.WriteLine($"[{UnsafeAccessorAttributeTypeRef}({UnsafeAccessorKindTypeRef}.Constructor)]");
                    writer.WriteLine($"private static extern {typeFQN} {wrapperName}({wrapperParams});");
                }
                else
                {
                    // Reflection fallback: cached ConstructorInfo + Invoke.
                    // Note: ConstructorInfo cannot be wrapped in a delegate, so we cache the ConstructorInfo directly.
                    string cacheName = GetConstructorReflectionCacheName(typeSpec);

                    string argTypes = parameters.Count == 0
                        ? EmptyTypeArray
                        : $"new global::System.Type[] {{{string.Join(", ", parameters.Select(p => $"typeof({p.ParameterType.FullyQualifiedName})"))}}}";

                    writer.WriteLine($"private static global::System.Reflection.ConstructorInfo? {cacheName};");

                    string invokeArgs = parameters.Count == 0
                        ? "null"
                        : $"new object?[] {{{string.Join(", ", parameters.Select(p => $"p{p.ParameterIndex}"))}}}";

                    writer.WriteLine($"private static {typeFQN} {wrapperName}({wrapperParams}) => ({typeFQN})({cacheName} ??= typeof({typeFQN}).GetConstructor({InstanceMemberBindingFlagsVariableName}, binder: null, {argTypes}, modifiers: null)!).Invoke({invokeArgs});");
                }
            }

            /// <summary>
            /// Returns the expression for reading a property value in the fast-path serialization handler.
            /// For accessible properties, this is a direct member access. For inaccessible [JsonInclude]
            /// properties, this uses UnsafeAccessor or reflection.
            /// </summary>
            private static string GetFastPathPropertyValueExpr(
                PropertyGenerationSpec property,
                TypeGenerationSpec typeGenSpec,
                string objectExpr,
                int propertyIndex,
                bool needsDisambiguation)
            {
                if (property.CanUseGetter)
                {
                    return $"{objectExpr}.{property.NameSpecifiedInSourceCode}";
                }

                // Inaccessible [JsonInclude] property/field: call accessor directly.
                string typeFriendlyName = typeGenSpec.TypeInfoPropertyName;

                if (property.CanUseUnsafeAccessors)
                {
                    string accessorName = property.IsProperty
                        ? GetAccessorName(typeFriendlyName, "get", property.MemberName, propertyIndex, needsDisambiguation)
                        : GetAccessorName(typeFriendlyName, "field", property.MemberName, propertyIndex, needsDisambiguation);

                    // Value type externs take 'ref T'; use the raw parameter variable to avoid
                    // ref-of-cast issues. Reference type externs take the declaring type by value.
                    return typeGenSpec.TypeRef.IsValueType
                        ? $"{accessorName}(ref {ValueVarName})"
                        : $"{accessorName}({objectExpr})";
                }

                string getterName = GetAccessorName(typeFriendlyName, "get", property.MemberName, propertyIndex, needsDisambiguation);

                return $"{getterName}({objectExpr})";
            }

            private static void GenerateCtorParamMetadataInitFunc(SourceWriter writer, string ctorParamMetadataInitMethodName, TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec> propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;
                int paramCount = parameters.Count + propertyInitializers.Count(propInit => !propInit.MatchesConstructorParameter);
                Debug.Assert(paramCount > 0);

                writer.WriteLine($"private static {JsonParameterInfoValuesTypeRef}[] {ctorParamMetadataInitMethodName}() => new {JsonParameterInfoValuesTypeRef}[]");
                writer.WriteLine('{');
                writer.Indentation++;

                int i = 0;
                foreach (ParameterGenerationSpec spec in parameters)
                {
                    writer.WriteLine($$"""
                        new()
                        {
                            Name = {{FormatStringLiteral(spec.Name)}},
                            ParameterType = typeof({{spec.ParameterType.FullyQualifiedName}}),
                            Position = {{spec.ParameterIndex}},
                            HasDefaultValue = {{FormatBoolLiteral(spec.HasDefaultValue)}},
                            DefaultValue = {{(spec.HasDefaultValue ? CSharpSyntaxUtilities.FormatLiteral(spec.DefaultValue, spec.ParameterType) : "null")}},
                            IsNullable = {{FormatBoolLiteral(spec.IsNullable)}},
                        },
                        """);

                    if (++i < paramCount)
                    {
                        writer.WriteLine();
                    }
                }

                foreach (PropertyInitializerGenerationSpec spec in propertyInitializers)
                {
                    if (spec.MatchesConstructorParameter)
                    {
                        continue;
                    }

                    writer.WriteLine($$"""
                        new()
                        {
                            Name = {{FormatStringLiteral(spec.Name)}},
                            ParameterType = typeof({{spec.ParameterType.FullyQualifiedName}}),
                            Position = {{spec.ParameterIndex}},
                            IsNullable = {{FormatBoolLiteral(spec.IsNullable)}},
                            IsMemberInitializer = true,
                        },
                        """);

                    if (++i < paramCount)
                    {
                        writer.WriteLine();
                    }
                }

                writer.Indentation--;
                writer.WriteLine("};");
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

                HashSet<string> duplicateMemberNames = GetDuplicateMemberNames(typeGenSpec.PropertyGenSpecs);

                if (typeGenSpec.ImplementsIJsonOnSerializing)
                {
                    writer.WriteLine($"((global::{JsonConstants.IJsonOnSerializingFullName}){ValueVarName}).OnSerializing();");
                    writer.WriteLine();
                }

                writer.WriteLine($"{WriterVarName}.WriteStartObject();");
                writer.WriteLine();

                bool generateDisallowNullThrowHelper = false;

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

                    SerializedValueCheckType defaultCheckType = GetCheckType(contextSpec, propertyGenSpec);

                    // For properties whose declared type differs from that of the serialized type
                    // perform an explicit cast -- this is to account for hidden properties or diamond ambiguity.
                    string? objectExpr = propertyGenSpec.DeclaringType != typeGenSpec.TypeRef
                        ? $"(({propertyGenSpec.DeclaringType.FullyQualifiedName}){ValueVarName})"
                        : ValueVarName;

                    string propValueExpr;
                    // For inaccessible [JsonInclude] properties, use UnsafeAccessor or reflection.
                    string? rawValueExpr = GetFastPathPropertyValueExpr(propertyGenSpec, typeGenSpec, objectExpr, i, duplicateMemberNames.Contains(propertyGenSpec.MemberName));

                    if (defaultCheckType != SerializedValueCheckType.None)
                    {
                        // Use temporary variable to evaluate property value only once
                        string localVariableName =  $"__value_{propertyGenSpec.NameSpecifiedInSourceCode.TrimStart('@')}";
                        writer.WriteLine($"{propertyGenSpec.PropertyType.FullyQualifiedName} {localVariableName} = {rawValueExpr};");
                        propValueExpr = localVariableName;
                    }
                    else
                    {
                        propValueExpr = rawValueExpr;
                    }

                    switch (defaultCheckType)
                    {
                        case SerializedValueCheckType.Ignore:
                            break;

                        case SerializedValueCheckType.IgnoreWhenNull:
                            writer.WriteLine($"if ({propValueExpr} is not null)");
                            writer.WriteLine('{');
                            writer.Indentation++;

                            GenerateSerializePropertyStatement(writer, propertyTypeSpec, propertyNameFieldName, propValueExpr);

                            writer.Indentation--;
                            writer.WriteLine('}');
                            break;

                        case SerializedValueCheckType.IgnoreWhenDefault:
                            writer.WriteLine($"if (!{EqualityComparerTypeRef}<{propertyGenSpec.PropertyType.FullyQualifiedName}>.Default.Equals(default, {propValueExpr}))");
                            writer.WriteLine('{');
                            writer.Indentation++;

                            GenerateSerializePropertyStatement(writer, propertyTypeSpec, propertyNameFieldName, propValueExpr);

                            writer.Indentation--;
                            writer.WriteLine('}');
                            break;

                        case SerializedValueCheckType.DisallowNull:
                            writer.WriteLine($$"""
                                if ({{propValueExpr}} is null)
                                {
                                   ThrowPropertyNullException({{FormatStringLiteral(propertyGenSpec.EffectiveJsonPropertyName)}});
                                }

                                """);

                            GenerateSerializePropertyStatement(writer, propertyTypeSpec, propertyNameFieldName, propValueExpr);
                            generateDisallowNullThrowHelper = true;
                            break;

                        default:
                            GenerateSerializePropertyStatement(writer, propertyTypeSpec, propertyNameFieldName, propValueExpr);
                            break;
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

                if (generateDisallowNullThrowHelper)
                {
                    writer.WriteLine();
                    writer.WriteLine($$"""
                        static void ThrowPropertyNullException(string propertyName)
                        {
                            throw new {{JsonExceptionTypeRef}}(string.Format("{{ExceptionMessages.PropertyGetterDisallowNull}}", propertyName, {{FormatStringLiteral(typeGenSpec.TypeRef.Name)}}));
                        }
                        """);
                }

                writer.Indentation--;
                writer.WriteLine('}');
            }

            private static string GetParameterizedCtorInvocationFunc(TypeGenerationSpec typeGenerationSpec)
            {
                ImmutableEquatableArray<ParameterGenerationSpec> parameters = typeGenerationSpec.CtorParamGenSpecs;
                ImmutableEquatableArray<PropertyInitializerGenerationSpec> propertyInitializers = typeGenerationSpec.PropertyInitializerSpecs;

                const string ArgsVarName = "args";

                // Determine if any non-matching member initializers exist for an inaccessible constructor.
                // These need post-construction setter calls since object initializer syntax can't be used.
                bool needsPostCtorInitializers = typeGenerationSpec.ConstructorIsInaccessible
                    && propertyInitializers.Any(p => !p.MatchesConstructorParameter);

                if (needsPostCtorInitializers)
                {
                    return GetParameterizedCtorWithPostInitFunc(typeGenerationSpec, parameters, propertyInitializers);
                }

                StringBuilder sb;

                if (typeGenerationSpec.ConstructorIsInaccessible)
                {
                    // Inaccessible constructor: use the unified constructor accessor wrapper.
                    string accessorName = GetConstructorAccessorName(typeGenerationSpec);
                    sb = new($"static {ArgsVarName} => {accessorName}(");
                }
                else
                {
                    sb = new($"static {ArgsVarName} => new {typeGenerationSpec.TypeRef.FullyQualifiedName}(");
                }

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
                    Debug.Assert(!typeGenerationSpec.ConstructorIsInaccessible);
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

            /// <summary>
            /// Generates a statement-body lambda for inaccessible constructors that also have
            /// required property member initializers. Since object initializer syntax can't be used
            /// with accessor-invoked constructors, the required properties are set individually
            /// after construction using property setters or accessor methods.
            /// </summary>
            private static string GetParameterizedCtorWithPostInitFunc(
                TypeGenerationSpec typeGenerationSpec,
                ImmutableEquatableArray<ParameterGenerationSpec> parameters,
                ImmutableEquatableArray<PropertyInitializerGenerationSpec> propertyInitializers)
            {
                const string ArgsVarName = "args";
                const string ObjVarName = "obj";
                string accessorName = GetConstructorAccessorName(typeGenerationSpec);
                HashSet<string> duplicateMemberNames = GetDuplicateMemberNames(typeGenerationSpec.PropertyGenSpecs);

                StringBuilder sb = new();
                sb.AppendLine($"static {ArgsVarName} =>");
                sb.AppendLine("{");

                // Construct the object via accessor
                sb.Append($"    var {ObjVarName} = {accessorName}(");
                if (parameters.Count > 0)
                {
                    foreach (ParameterGenerationSpec param in parameters)
                    {
                        sb.Append($"({param.ParameterType.FullyQualifiedName}){ArgsVarName}[{param.ParameterIndex}], ");
                    }

                    sb.Length -= 2;
                }
                sb.AppendLine(");");

                // Set member initializer properties post-construction
                string typeFriendlyName = typeGenerationSpec.TypeInfoPropertyName;
                foreach (PropertyInitializerGenerationSpec propInit in propertyInitializers)
                {
                    if (propInit.MatchesConstructorParameter)
                    {
                        continue;
                    }

                    string value = $"({propInit.ParameterType.FullyQualifiedName}){ArgsVarName}[{propInit.ParameterIndex}]";

                    // Find the matching PropertyGenerationSpec to determine how to set it
                    PropertyGenerationSpec? matchingProp = null;
                    int matchingIndex = 0;
                    for (int i = 0; i < typeGenerationSpec.PropertyGenSpecs.Count; i++)
                    {
                        if (typeGenerationSpec.PropertyGenSpecs[i].NameSpecifiedInSourceCode == propInit.Name)
                        {
                            matchingProp = typeGenerationSpec.PropertyGenSpecs[i];
                            matchingIndex = i;
                            break;
                        }
                    }

                    if (matchingProp is not null && (matchingProp.IsInitOnlySetter || NeedsAccessorForSetter(matchingProp)))
                    {
                        // Use the accessor method for init-only or inaccessible setters.
                        bool disambiguate = duplicateMemberNames.Contains(matchingProp.MemberName);
                        string refPrefix = typeGenerationSpec.TypeRef.IsValueType ? "ref " : "";

                        if (matchingProp.IsProperty)
                        {
                            string setterName = GetAccessorName(typeFriendlyName, "set", matchingProp.MemberName, matchingIndex, disambiguate);
                            sb.AppendLine($"    {setterName}({refPrefix}{ObjVarName}, {value});");
                        }
                        else if (matchingProp.CanUseUnsafeAccessors)
                        {
                            // UnsafeAccessor field returns ref T: assignment syntax.
                            string fieldName = GetAccessorName(typeFriendlyName, "field", matchingProp.MemberName, matchingIndex, disambiguate);
                            sb.AppendLine($"    {fieldName}({refPrefix}{ObjVarName}) = {value};");
                        }
                        else
                        {
                            // Reflection fallback field setter uses "set" kind and takes (object, T).
                            string setterName = GetAccessorName(typeFriendlyName, "set", matchingProp.MemberName, matchingIndex, disambiguate);
                            sb.AppendLine($"    {setterName}({ObjVarName}, {value});");
                        }
                    }
                    else
                    {
                        // Direct property assignment for accessible setters
                        sb.AppendLine($"    {ObjVarName}.{propInit.Name} = {value};");
                    }
                }

                sb.Append($"    return {ObjVarName};");
                sb.AppendLine();
                sb.Append('}');

                return sb.ToString();
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
                        if ({{ValueVarName}} is null)
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

            private enum SerializedValueCheckType
            {
                None,
                IgnoreWhenNull,
                IgnoreWhenDefault,
                DisallowNull,
                Ignore
            }

            private static SerializedValueCheckType GetCheckType(ContextGenerationSpec contextSpec, PropertyGenerationSpec propertySpec)
            {
                return (propertySpec.DefaultIgnoreCondition ?? contextSpec.GeneratedOptionsSpec?.DefaultIgnoreCondition) switch
                {
                    JsonIgnoreCondition.WhenWriting => SerializedValueCheckType.Ignore,
                    JsonIgnoreCondition.WhenWritingNull => propertySpec.PropertyType.CanBeNull ? SerializedValueCheckType.IgnoreWhenNull : SerializedValueCheckType.None,
                    JsonIgnoreCondition.WhenWritingDefault => propertySpec.PropertyType.CanBeNull ? SerializedValueCheckType.IgnoreWhenNull : SerializedValueCheckType.IgnoreWhenDefault,
                    _ when propertySpec.IsGetterNonNullableAnnotation && contextSpec.GeneratedOptionsSpec?.RespectNullableAnnotations is true => SerializedValueCheckType.DisallowNull,
                    _ => SerializedValueCheckType.None,
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

            private static SourceText GetRootJsonContextImplementation(ContextGenerationSpec contextSpec, bool emitGetConverterForNullablePropertyMethod, bool emitValueTypeSetterDelegate)
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

                writer.WriteLine($"""

                    private const global::System.Reflection.BindingFlags {InstanceMemberBindingFlagsVariableName} =
                        global::System.Reflection.BindingFlags.Instance |
                        global::System.Reflection.BindingFlags.Public |
                        global::System.Reflection.BindingFlags.NonPublic;

                    """);

                if (emitValueTypeSetterDelegate)
                {
                    writer.WriteLine($"private delegate void {ValueTypeSetterDelegateName}<TDeclaringType, TValue>(ref TDeclaringType obj, TValue value);");
                    writer.WriteLine();
                }

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

                if (optionsSpec.AllowDuplicateProperties is bool allowDuplicateProperties)
                    writer.WriteLine($"AllowDuplicateProperties = {FormatBoolLiteral(allowDuplicateProperties)},");

                if (optionsSpec.AllowOutOfOrderMetadataProperties is bool allowOutOfOrderMetadataProperties)
                    writer.WriteLine($"AllowOutOfOrderMetadataProperties = {FormatBoolLiteral(allowOutOfOrderMetadataProperties)},");

                if (optionsSpec.AllowTrailingCommas is bool allowTrailingCommas)
                    writer.WriteLine($"AllowTrailingCommas = {FormatBoolLiteral(allowTrailingCommas)},");

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

                if (optionsSpec.RespectNullableAnnotations is bool respectNullableAnnotations)
                    writer.WriteLine($"RespectNullableAnnotations = {FormatBoolLiteral(respectNullableAnnotations)},");

                if (optionsSpec.RespectRequiredConstructorParameters is bool respectRequiredConstructorParameters)
                    writer.WriteLine($"RespectRequiredConstructorParameters = {FormatBoolLiteral(respectRequiredConstructorParameters)},");

                if (optionsSpec.IgnoreReadOnlyFields is bool ignoreReadOnlyFields)
                    writer.WriteLine($"IgnoreReadOnlyFields = {FormatBoolLiteral(ignoreReadOnlyFields)},");

                if (optionsSpec.IgnoreReadOnlyProperties is bool ignoreReadOnlyProperties)
                    writer.WriteLine($"IgnoreReadOnlyProperties = {FormatBoolLiteral(ignoreReadOnlyProperties)},");

                if (optionsSpec.IncludeFields is bool includeFields)
                    writer.WriteLine($"IncludeFields = {FormatBoolLiteral(includeFields)},");

                if (optionsSpec.MaxDepth is int maxDepth)
                    writer.WriteLine($"MaxDepth = {maxDepth},");

                if (optionsSpec.NewLine is string newLine)
                    writer.WriteLine($"NewLine = {FormatStringLiteral(newLine)},");

                if (optionsSpec.NumberHandling is JsonNumberHandling numberHandling)
                    writer.WriteLine($"NumberHandling = {FormatNumberHandling(numberHandling)},");

                if (optionsSpec.PreferredObjectCreationHandling is JsonObjectCreationHandling preferredObjectCreationHandling)
                    writer.WriteLine($"PreferredObjectCreationHandling = {FormatObjectCreationHandling(preferredObjectCreationHandling)},");

                if (optionsSpec.PropertyNameCaseInsensitive is bool propertyNameCaseInsensitive)
                    writer.WriteLine($"PropertyNameCaseInsensitive = {FormatBoolLiteral(propertyNameCaseInsensitive)},");

                if (optionsSpec.PropertyNamingPolicy is JsonKnownNamingPolicy propertyNamingPolicy)
                    writer.WriteLine($"PropertyNamingPolicy = {FormatNamingPolicy(propertyNamingPolicy)},");

                if (optionsSpec.ReadCommentHandling is JsonCommentHandling readCommentHandling)
                    writer.WriteLine($"ReadCommentHandling = {FormatCommentHandling(readCommentHandling)},");

                if (optionsSpec.ReferenceHandler is JsonKnownReferenceHandler referenceHandler)
                    writer.WriteLine($"ReferenceHandler = {FormatReferenceHandler(referenceHandler)},");

                if (optionsSpec.UnknownTypeHandling is JsonUnknownTypeHandling unknownTypeHandling)
                    writer.WriteLine($"UnknownTypeHandling = {FormatUnknownTypeHandling(unknownTypeHandling)},");

                if (optionsSpec.UnmappedMemberHandling is JsonUnmappedMemberHandling unmappedMemberHandling)
                    writer.WriteLine($"UnmappedMemberHandling = {FormatUnmappedMemberHandling(unmappedMemberHandling)},");

                if (optionsSpec.WriteIndented is bool writeIndented)
                    writer.WriteLine($"WriteIndented = {FormatBoolLiteral(writeIndented)},");

                if (optionsSpec.IndentCharacter is char indentCharacter)
                    writer.WriteLine($"IndentCharacter = {FormatCharLiteral(indentCharacter)},");

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

                static string FormatReferenceHandler(JsonKnownReferenceHandler referenceHandler)
                {
                    string? referenceHandlerName = referenceHandler switch
                    {
                        JsonKnownReferenceHandler.Preserve => PreserveReferenceHandlerPropertyName,
                        JsonKnownReferenceHandler.IgnoreCycles => IgnoreCyclesReferenceHandlerPropertyName,
                        _ => null,
                    };

                    return referenceHandlerName != null
                    ? $"{ReferenceHandlerTypeRef}.{referenceHandlerName}"
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
                    writer.WriteLine($$"""private static readonly {{JsonEncodedTextTypeRef}} {{name_varName_pair.Value}} = {{JsonEncodedTextTypeRef}}.Encode({{FormatStringLiteral(name_varName_pair.Key)}});""");
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

            private static string FormatBoolLiteral(bool value) => value ? "true" : "false";
            private static string FormatStringLiteral(string? value) => value is null ? "null" : SymbolDisplay.FormatLiteral(value, quote: true);
            private static string FormatCharLiteral(char value) => SymbolDisplay.FormatLiteral(value, quote: true);

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
                    { ConstructionStrategy: ObjectConstructionStrategy.ParameterlessConstructor, ConstructorIsInaccessible: false } => $"() => new {typeSpec.TypeRef.FullyQualifiedName}()",
                    { ConstructionStrategy: ObjectConstructionStrategy.ParameterlessConstructor, ConstructorIsInaccessible: true } =>
                        $"static () => {GetConstructorAccessorName(typeSpec)}()",
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
                    CollectionType.IReadOnlySetOfT => "CreateIReadOnlySetInfo",
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
