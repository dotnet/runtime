// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    [DebuggerDisplay("Type={Type}, ClassType={ClassType}")]
    internal sealed class TypeGenerationSpec
    {
        public TypeGenerationSpec(Type type)
        {
            Type = type;
            TypeRef = type.GetCompilableName();
            TypeInfoPropertyName = type.GetTypeInfoPropertyName();
            IsValueType = type.IsValueType;
        }

        /// <summary>
        /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
        /// </summary>
        public string TypeRef { get; private init; }

        /// <summary>
        /// If specified as a root type via <c>JsonSerializableAttribute</c>, specifies the location of the attribute application.
        /// </summary>
        public Location? AttributeLocation { get; set; }

        /// <summary>
        /// The name of the public <c>JsonTypeInfo&lt;T&gt;</c> property for this type on the generated context class.
        /// For example, if the context class is named MyJsonContext, and the value of this property is JsonMessage;
        /// then users will call MyJsonContext.JsonMessage to access generated metadata for the type.
        /// </summary>
        public string TypeInfoPropertyName { get; set; }

        /// <summary>
        /// Method used to generate JsonTypeInfo given options instance
        /// </summary>
        public string CreateTypeInfoMethodName => $"Create_{TypeInfoPropertyName}";

        public JsonSourceGenerationMode GenerationMode { get; set; }

        public bool GenerateMetadata => GenerationModeIsSpecified(JsonSourceGenerationMode.Metadata);

        public bool GenerateSerializationLogic => GenerationModeIsSpecified(JsonSourceGenerationMode.Serialization) && FastPathIsSupported();

        public Type Type { get; private init; }

        public ClassType ClassType { get; private set; }

        public bool ImplementsIJsonOnSerialized { get; private set; }
        public bool ImplementsIJsonOnSerializing { get; private set; }

        public bool IsPolymorphic { get; private set; }
        public bool IsValueType { get; private init; }

        public bool CanBeNull { get; private set; }

        public JsonNumberHandling? NumberHandling { get; private set; }
        public JsonUnmappedMemberHandling? UnmappedMemberHandling { get; private set; }
        public JsonObjectCreationHandling? PreferredPropertyObjectCreationHandling { get; private set; }

        public List<PropertyGenerationSpec>? PropertyGenSpecList { get; private set; }

        public ParameterGenerationSpec[]? CtorParamGenSpecArray { get; private set; }

        public List<PropertyInitializerGenerationSpec>? PropertyInitializerSpecList { get; private set; }
        public int PropertyInitializersWithoutMatchingConstructorParameters { get; private set; }

        public CollectionType CollectionType { get; private set; }

        public TypeGenerationSpec? CollectionKeyTypeMetadata { get; private set; }

        public TypeGenerationSpec? CollectionValueTypeMetadata { get; private set; }

        public ObjectConstructionStrategy ConstructionStrategy { get; private set; }

        public bool ConstructorSetsRequiredParameters { get; private set; }

        public TypeGenerationSpec? NullableUnderlyingTypeMetadata { get; private set; }

        /// <summary>
        /// Supports deserialization of extension data dictionaries typed as <c>I[ReadOnly]Dictionary&lt;string, object/JsonElement&gt;</c>.
        /// Specifies a concrete type to instanciate, which would be <c>Dictionary&lt;string, object/JsonElement&gt;</c>.
        /// </summary>
        public string? RuntimeTypeRef { get; private set; }

        public TypeGenerationSpec? ExtensionDataPropertyTypeSpec { get; private set; }

        public string? ConverterInstantiationLogic { get; private set; }

        // Only generate certain helper methods if necessary.
        public bool HasPropertyFactoryConverters { get; private set; }
        public bool HasTypeFactoryConverter { get; private set; }

        // The spec is derived from cached `System.Type` instances, which are generally annotation-agnostic.
        // Hence we can only record the potential for nullable annotations being possible for the runtime type.
        // TODO: consider deriving the generation spec from the Roslyn symbols directly.
        public bool CanContainNullableReferenceAnnotations { get; private set; }

        public string? ImmutableCollectionBuilderName
        {
            get
            {
                string? builderName;

                if (CollectionType == CollectionType.ImmutableDictionary)
                {
                    builderName = Type.GetImmutableDictionaryConstructingTypeName(sourceGenType: true);
                }
                else if (CollectionType == CollectionType.ImmutableEnumerable)
                {
                    builderName = Type.GetImmutableEnumerableConstructingTypeName(sourceGenType: true);
                }
                else
                {
                    return null;
                }

                Debug.Assert(builderName != null);
                return $"global::{builderName}.{ReflectionExtensions.CreateRangeMethodName}";
            }
        }

        public void Initialize(
            JsonSourceGenerationMode generationMode,
            ClassType classType,
            JsonNumberHandling? numberHandling,
            JsonUnmappedMemberHandling? unmappedMemberHandling,
            JsonObjectCreationHandling? preferredPropertyObjectCreationHandling,
            List<PropertyGenerationSpec>? propertyGenSpecList,
            ParameterGenerationSpec[]? ctorParamGenSpecArray,
            List<PropertyInitializerGenerationSpec>? propertyInitializerSpecList,
            CollectionType collectionType,
            TypeGenerationSpec? collectionKeyTypeMetadata,
            TypeGenerationSpec? collectionValueTypeMetadata,
            ObjectConstructionStrategy constructionStrategy,
            bool constructorSetsRequiredMembers,
            TypeGenerationSpec? nullableUnderlyingTypeMetadata,
            string? runtimeTypeRef,
            TypeGenerationSpec? extensionDataPropertyTypeSpec,
            string? converterInstantiationLogic,
            bool implementsIJsonOnSerialized,
            bool implementsIJsonOnSerializing,
            bool hasTypeFactoryConverter,
            bool canContainNullableReferenceAnnotations,
            bool hasPropertyFactoryConverters,
            bool isPolymorphic)
        {
            GenerationMode = generationMode;
            ClassType = classType;
            CanBeNull = !IsValueType || nullableUnderlyingTypeMetadata != null;
            IsPolymorphic = isPolymorphic;
            NumberHandling = numberHandling;
            UnmappedMemberHandling = unmappedMemberHandling;
            PreferredPropertyObjectCreationHandling = preferredPropertyObjectCreationHandling;
            PropertyGenSpecList = propertyGenSpecList;
            PropertyInitializerSpecList = propertyInitializerSpecList;
            CtorParamGenSpecArray = ctorParamGenSpecArray;
            CollectionType = collectionType;
            CollectionKeyTypeMetadata = collectionKeyTypeMetadata;
            CollectionValueTypeMetadata = collectionValueTypeMetadata;
            ConstructionStrategy = constructionStrategy;
            ConstructorSetsRequiredParameters = constructorSetsRequiredMembers;
            NullableUnderlyingTypeMetadata = nullableUnderlyingTypeMetadata;
            RuntimeTypeRef = runtimeTypeRef;
            ExtensionDataPropertyTypeSpec = extensionDataPropertyTypeSpec;
            ConverterInstantiationLogic = converterInstantiationLogic;
            ImplementsIJsonOnSerialized = implementsIJsonOnSerialized;
            ImplementsIJsonOnSerializing = implementsIJsonOnSerializing;
            CanContainNullableReferenceAnnotations = canContainNullableReferenceAnnotations;
            HasTypeFactoryConverter = hasTypeFactoryConverter;
            HasPropertyFactoryConverters = hasPropertyFactoryConverters;
        }

        public bool TryFilterSerializableProps(
                JsonSourceGenerationOptionsAttribute options,
                [NotNullWhen(true)] out Dictionary<string, PropertyGenerationSpec>? serializableProperties,
                out bool castingRequiredForProps)
        {
            Debug.Assert(PropertyGenSpecList != null);

            castingRequiredForProps = false;
            serializableProperties = new Dictionary<string, PropertyGenerationSpec>();
            HashSet<string>? ignoredMembers = null;

            for (int i = 0; i < PropertyGenSpecList.Count; i++)
            {
                PropertyGenerationSpec propGenSpec = PropertyGenSpecList[i];
                JsonIgnoreCondition? ignoreCondition = propGenSpec.DefaultIgnoreCondition;

                if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull && !propGenSpec.TypeGenerationSpec.CanBeNull)
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

                if (!propGenSpec.IsProperty && !propGenSpec.HasJsonInclude && !options.IncludeFields)
                {
                    continue;
                }

                // Using properties from an interface hierarchy -- require explicit casting when
                // getting properties in the fast path to account for possible diamond ambiguities.
                castingRequiredForProps |= Type.IsInterface && propGenSpec.DeclaringType != Type;

                string memberName = propGenSpec.ClrName!;

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

                        if (!Type.IsInterface)
                        {
                            ignoreCurrentProperty =
                                // Does the current property have `JsonIgnoreAttribute`?
                                propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always ||
                                // Is the current property hidden by the previously cached property
                                // (with `new` keyword, or by overriding)?
                                other.ClrName == memberName ||
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

            Debug.Assert(PropertyGenSpecList.Count >= serializableProperties.Count);
            castingRequiredForProps |= PropertyGenSpecList.Count > serializableProperties.Count;
            return true;

        ReturnFalse:
            serializableProperties = null;
            castingRequiredForProps = false;
            return false;
        }

        private bool FastPathIsSupported()
        {
            if (IsPolymorphic)
            {
                return false;
            }

            if (ClassType == ClassType.Object)
            {
                if (ExtensionDataPropertyTypeSpec != null)
                {
                    return false;
                }

                Debug.Assert(PropertyGenSpecList != null);

                foreach (PropertyGenerationSpec property in PropertyGenSpecList)
                {
                    if (property.TypeGenerationSpec.Type.IsObjectType() ||
                        property.NumberHandling == JsonNumberHandling.AllowNamedFloatingPointLiterals ||
                        property.NumberHandling == JsonNumberHandling.WriteAsString ||
                        property.ConverterInstantiationLogic is not null)
                    {
                        return false;
                    }
                }

                return true;
            }

            switch (CollectionType)
            {
                case CollectionType.NotApplicable:
                case CollectionType.IAsyncEnumerableOfT:
                    return false;
                case CollectionType.IDictionary:
                case CollectionType.Dictionary:
                case CollectionType.ImmutableDictionary:
                case CollectionType.IDictionaryOfTKeyTValue:
                case CollectionType.IReadOnlyDictionary:
                    return CollectionKeyTypeMetadata!.Type.IsStringType() && !CollectionValueTypeMetadata!.Type.IsObjectType();
                default:
                    // Non-dictionary collections
                    return !CollectionValueTypeMetadata!.Type.IsObjectType();
            }
        }

        private bool GenerationModeIsSpecified(JsonSourceGenerationMode mode) => GenerationMode == JsonSourceGenerationMode.Default || (mode & GenerationMode) != 0;
    }
}
