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

namespace System.Text.Json.SourceGeneration
{
    internal sealed partial class JsonSourceGeneratorHelper
    {
        private readonly Type _ienumerableType;
        private readonly Type _listOfTType;
        private readonly Type _dictionaryType;

        private readonly Type _booleanType;
        private readonly Type _byteArrayType;
        private readonly Type _charType;
        private readonly Type _dateTimeType;
        private readonly Type _dateTimeOffsetType;
        private readonly Type _guidType;
        private readonly Type _stringType;
        private readonly Type _uriType;
        private readonly Type _versionType;

        private readonly HashSet<Type> _numberTypes = new();

        private readonly HashSet<Type> _knownTypes = new();

        /// <summary>
        /// Type information for member types in input object graphs.
        /// </summary>
        private readonly Dictionary<Type, TypeMetadata> _typeMetadataCache = new();

        /// <summary>
        /// Types that were specified with System.Text.Json.Serialization.JsonSerializableAttribute.
        /// </summary>
        private Dictionary<string, TypeMetadata> _rootSerializableTypes;

        private readonly GeneratorExecutionContext _executionContext;

        private readonly MetadataLoadContextInternal _metadataLoadContext;

        private const string SystemTextJsonNamespace = "System.Text.Json";

        private const string JsonConverterAttributeFullName = "System.Text.Json.Serialization.JsonConverterAttribute";

        private const string JsonIgnoreAttributeFullName = "System.Text.Json.Serialization.JsonIgnoreAttribute";

        private const string JsonIgnoreConditionFullName = "System.Text.Json.Serialization.JsonIgnoreCondition";

        private const string JsonIncludeAttributeFullName = "System.Text.Json.Serialization.JsonIncludeAttribute";

        private const string JsonNumberHandlingAttributeFullName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";

        private const string JsonPropertyNameAttributeFullName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";

        public JsonSourceGeneratorHelper(
            GeneratorExecutionContext executionContext,
            MetadataLoadContextInternal metadataLoadContext)
        {
            _generationNamespace = $"{executionContext.Compilation.AssemblyName}.JsonSourceGeneration";
            _executionContext = executionContext;
            _metadataLoadContext = metadataLoadContext;

            _ienumerableType = metadataLoadContext.Resolve(typeof(IEnumerable));
            _listOfTType = metadataLoadContext.Resolve(typeof(List<>));
            _dictionaryType = metadataLoadContext.Resolve(typeof(Dictionary<,>));

            _booleanType = metadataLoadContext.Resolve(typeof(bool));
            _byteArrayType = metadataLoadContext.Resolve(typeof(byte[]));
            _charType = metadataLoadContext.Resolve(typeof(char));
            _dateTimeType = metadataLoadContext.Resolve(typeof(DateTime));
            _dateTimeOffsetType = metadataLoadContext.Resolve(typeof(DateTimeOffset));
            _guidType = metadataLoadContext.Resolve(typeof(Guid));
            _stringType = metadataLoadContext.Resolve(typeof(string));
            _uriType = metadataLoadContext.Resolve(typeof(Uri));
            _versionType = metadataLoadContext.Resolve(typeof(Version));

            PopulateKnownTypes(metadataLoadContext);
        }

        public void RegisterRootSerializableType(Type type, string? typeInfoPropertyName)
        {
            _rootSerializableTypes ??= new Dictionary<string, TypeMetadata>();
            _rootSerializableTypes[type.FullName] = GetOrAddTypeMetadata(type, typeInfoPropertyName);
        }

        public void GenerateSerializationMetadata()
        {
            if (_rootSerializableTypes == null || _rootSerializableTypes.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, TypeMetadata> pair in _rootSerializableTypes)
            {
                TypeMetadata typeMetadata = pair.Value;
                GenerateTypeMetadata(typeMetadata);
            }

            // Add base default instance source.
            _executionContext.AddSource("JsonContext.g.cs", SourceText.From(BaseJsonContextImplementation(), Encoding.UTF8));

            // Add GetJsonTypeInfo override implementation.
            _executionContext.AddSource("JsonContext.GetJsonTypeInfo.g.cs", SourceText.From(GetGetTypeInfoImplementation(), Encoding.UTF8));
        }

        private TypeMetadata GetOrAddTypeMetadata(Type type, string? typeInfoPropertyName = null)
        {
            if (_typeMetadataCache.TryGetValue(type, out TypeMetadata? typeMetadata))
            {
                return typeMetadata!;
            }

            // Add metadata to cache now to prevent stack overflow when the same type is found somewhere else in the object graph.
            typeMetadata = new();
            _typeMetadataCache[type] = typeMetadata;

            ClassType classType;
            Type? collectionKeyType = null;
            Type? collectionValueType = null;
            Type? nullableUnderlyingType = null;
            List<PropertyMetadata>? propertiesMetadata = null;
            CollectionType collectionType = CollectionType.NotApplicable;
            ObjectConstructionStrategy constructionStrategy = default;
            JsonNumberHandling? numberHandling = null;
            bool containsOnlyPrimitives = true;

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
            else if (type.IsNullableValueType(out nullableUnderlyingType))
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
                    // TODO: support parameterized ctors.
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
                        PropertyMetadata metadata = GetPropertyMetadata(propertyInfo);

                        // Ignore indexers.
                        if (propertyInfo.GetIndexParameters().Length > 0)
                        {
                            continue;
                        }

                        string key = metadata.JsonPropertyName ?? metadata.ClrName;

                        if (metadata.HasGetter || metadata.HasSetter)
                        {
                            (propertiesMetadata ??= new()).Add(metadata);
                        }

                        if (containsOnlyPrimitives && !IsPrimitive(propertyInfo.PropertyType))
                        {
                            containsOnlyPrimitives = false;
                        }
                    }

                    foreach (FieldInfo fieldInfo in currentType.GetFields(bindingFlags))
                    {
                        PropertyMetadata metadata = GetPropertyMetadata(fieldInfo);

                        if (metadata.HasGetter || metadata.HasSetter)
                        {
                            (propertiesMetadata ??= new()).Add(metadata);
                        }
                    }
                }
            }

            typeMetadata.Initialize(
                compilableName: type.GetUniqueCompilableTypeName(),
                friendlyName: typeInfoPropertyName ?? type.GetFriendlyTypeName(),
                type,
                classType,
                isValueType: type.IsValueType,
                numberHandling,
                propertiesMetadata,
                collectionType,
                collectionKeyTypeMetadata: collectionKeyType != null ? GetOrAddTypeMetadata(collectionKeyType) : null,
                collectionValueTypeMetadata: collectionValueType != null ? GetOrAddTypeMetadata(collectionValueType) : null,
                constructionStrategy,
                nullableUnderlyingTypeMetadata: nullableUnderlyingType != null ? GetOrAddTypeMetadata(nullableUnderlyingType) : null,
                converterInstatiationLogic,
                containsOnlyPrimitives);

            return typeMetadata;
        }

        private PropertyMetadata GetPropertyMetadata(MemberInfo memberInfo)
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
            bool hasGetter;
            bool hasSetter;
            bool getterIsVirtual = false;
            bool setterIsVirtual = false;

            switch (memberInfo)
            {
                case PropertyInfo propertyInfo:
                    {
                        MethodInfo setMethod = propertyInfo.SetMethod;

                        memberCLRType = propertyInfo.PropertyType;
                        hasGetter = PropertyAccessorCanBeReferenced(propertyInfo.GetMethod, hasJsonInclude);
                        hasSetter = PropertyAccessorCanBeReferenced(setMethod, hasJsonInclude) && !setMethod.IsInitOnly();
                        getterIsVirtual = propertyInfo.GetMethod?.IsVirtual == true;
                        setterIsVirtual = propertyInfo.SetMethod?.IsVirtual == true;
                    }
                    break;
                case FieldInfo fieldInfo:
                    {
                        Debug.Assert(fieldInfo.IsPublic);

                        memberCLRType = fieldInfo.FieldType;
                        hasGetter = true;
                        hasSetter = !fieldInfo.IsInitOnly;
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return new PropertyMetadata
            {
                ClrName = memberInfo.Name,
                IsProperty = memberInfo.MemberType == MemberTypes.Property,
                JsonPropertyName = jsonPropertyName,
                HasGetter = hasGetter,
                HasSetter = hasSetter,
                GetterIsVirtual = getterIsVirtual,
                SetterIsVirtual = setterIsVirtual,
                IgnoreCondition = ignoreCondition,
                NumberHandling = numberHandling,
                HasJsonInclude = hasJsonInclude,
                TypeMetadata = GetOrAddTypeMetadata(memberCLRType),
                DeclaringTypeCompilableName = memberInfo.DeclaringType.GetUniqueCompilableTypeName(),
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

            Type converterType = new TypeWrapper((ITypeSymbol)attributeData.ConstructorArguments[0].Value, _metadataLoadContext);

            if (converterType == null || converterType.GetConstructor(Type.EmptyTypes) == null || converterType.IsNestedPrivate)
            {
                return null;
            }

            return $"new {converterType.GetUniqueCompilableTypeName()}()";
        }

        private void PopulateNumberTypes(MetadataLoadContextInternal metadataLoadContext)
        {
            Debug.Assert(_numberTypes != null);
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(byte)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(decimal)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(double)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(short)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(sbyte)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(int)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(long)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(float)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(ushort)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(uint)));
            _numberTypes.Add(metadataLoadContext.Resolve(typeof(ulong)));
        }

        private void PopulateKnownTypes(MetadataLoadContextInternal metadataLoadContext)
        {
            PopulateNumberTypes(metadataLoadContext);

            Debug.Assert(_knownTypes != null);
            Debug.Assert(_numberTypes != null);

            _knownTypes.UnionWith(_numberTypes);
            _knownTypes.Add(_booleanType);
            _knownTypes.Add(_byteArrayType);
            _knownTypes.Add(_charType);
            _knownTypes.Add(_dateTimeType);
            _knownTypes.Add(_dateTimeOffsetType);
            _knownTypes.Add(_guidType);
            _knownTypes.Add(metadataLoadContext.Resolve(typeof(object)));
            _knownTypes.Add(_stringType);

            // System.Private.Uri may not be loaded in input compilation.
            if (_uriType != null)
            {
                _knownTypes.Add(_uriType);
            }

            _knownTypes.Add(metadataLoadContext.Resolve(typeof(Version)));
        }

        private bool IsPrimitive(Type type)
            => _knownTypes.Contains(type) && type != _uriType && type != _versionType;

        public Dictionary<string, Type>? GetSerializableTypes() => _rootSerializableTypes?.ToDictionary(p => p.Key, p => p.Value.Type);
    }
}
