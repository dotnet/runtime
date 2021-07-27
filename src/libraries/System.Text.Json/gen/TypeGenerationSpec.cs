// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration
{
    [DebuggerDisplay("Type={Type}, ClassType={ClassType}")]
    internal class TypeGenerationSpec
    {
        /// <summary>
        /// Fully qualified assembly name, prefixed with "global::", e.g. global::System.Numerics.BigInteger.
        /// </summary>
        public string TypeRef { get; private set; }

        /// <summary>
        /// The name of the public JsonTypeInfo<T> property for this type on the generated context class.
        /// For example, if the context class is named MyJsonContext, and the value of this property is JsonMessage;
        /// then users will call MyJsonContext.JsonMessage to access generated metadata for the type.
        /// </summary>
        public string TypeInfoPropertyName { get; set; }

        public JsonSourceGenerationMode GenerationMode { get; set; }

        public bool GenerateMetadata => GenerationModeIsSpecified(JsonSourceGenerationMode.Metadata);

        public bool GenerateSerializationLogic => GenerationModeIsSpecified(JsonSourceGenerationMode.Serialization) && FastPathIsSupported();

        public Type Type { get; private set; }

        public ClassType ClassType { get; private set; }

        public bool ImplementsIJsonOnSerialized { get; private set; }
        public bool ImplementsIJsonOnSerializing { get; private set; }

        public bool IsValueType { get; private set; }

        public bool CanBeNull { get; private set; }

        public JsonNumberHandling? NumberHandling { get; private set; }

        public List<PropertyGenerationSpec>? PropertyGenSpecList { get; private set; }

        public CollectionType CollectionType { get; private set; }

        public TypeGenerationSpec? CollectionKeyTypeMetadata { get; private set; }

        public TypeGenerationSpec? CollectionValueTypeMetadata { get; private set; }

        public ObjectConstructionStrategy ConstructionStrategy { get; private set; }

        public TypeGenerationSpec? NullableUnderlyingTypeMetadata { get; private set; }

        public string? ConverterInstantiationLogic { get; private set; }

        public string FastPathSerializeMethodName
        {
            get
            {
                Debug.Assert(GenerateSerializationLogic);
                return $"{TypeInfoPropertyName}Serialize";
            }
        }

        public string? ImmutableCollectionBuilderName
        {
            get
            {
                string builderName;

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
            Type type,
            ClassType classType,
            JsonNumberHandling? numberHandling,
            List<PropertyGenerationSpec>? propertyGenSpecList,
            CollectionType collectionType,
            TypeGenerationSpec? collectionKeyTypeMetadata,
            TypeGenerationSpec? collectionValueTypeMetadata,
            ObjectConstructionStrategy constructionStrategy,
            TypeGenerationSpec? nullableUnderlyingTypeMetadata,
            string? converterInstantiationLogic,
            bool implementsIJsonOnSerialized,
            bool implementsIJsonOnSerializing)
        {
            GenerationMode = generationMode;
            TypeRef = type.GetCompilableName();
            TypeInfoPropertyName = type.GetTypeInfoPropertyName();
            Type = type;
            ClassType = classType;
            IsValueType = type.IsValueType;
            CanBeNull = !IsValueType || nullableUnderlyingTypeMetadata != null;
            NumberHandling = numberHandling;
            PropertyGenSpecList = propertyGenSpecList;
            CollectionType = collectionType;
            CollectionKeyTypeMetadata = collectionKeyTypeMetadata;
            CollectionValueTypeMetadata = collectionValueTypeMetadata;
            ConstructionStrategy = constructionStrategy;
            NullableUnderlyingTypeMetadata = nullableUnderlyingTypeMetadata;
            ConverterInstantiationLogic = converterInstantiationLogic;
            ImplementsIJsonOnSerialized = implementsIJsonOnSerialized;
            ImplementsIJsonOnSerializing = implementsIJsonOnSerializing;
        }

        public bool TryFilterSerializableProps(
                JsonSourceGenerationOptionsAttribute options,
                [NotNullWhen(true)] out Dictionary<string, PropertyGenerationSpec>? serializableProperties,
                out bool castingRequiredForProps)
        {
            serializableProperties = new Dictionary<string, PropertyGenerationSpec>();
            Dictionary<string, PropertyGenerationSpec>? ignoredMembers = null;

            for (int i = 0; i < PropertyGenSpecList.Count; i++)
            {
                PropertyGenerationSpec propGenSpec = PropertyGenSpecList[i];
                bool hasJsonInclude = propGenSpec.HasJsonInclude;
                JsonIgnoreCondition? ignoreCondition = propGenSpec.DefaultIgnoreCondition;

                if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull && !propGenSpec.TypeGenerationSpec.CanBeNull)
                {
                    goto ReturnFalse;
                }

                if (!propGenSpec.IsPublic)
                {
                    if (hasJsonInclude)
                    {
                        goto ReturnFalse;
                    }

                    continue;
                }

                if (!propGenSpec.IsProperty && !hasJsonInclude && !options.IncludeFields)
                {
                    continue;
                }

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
                    else if (
                        // Does the current property have `JsonIgnoreAttribute`?
                        propGenSpec.DefaultIgnoreCondition != JsonIgnoreCondition.Always &&
                        // Is the current property hidden by the previously cached property
                        // (with `new` keyword, or by overriding)?
                        other.ClrName != memberName &&
                        // Was a property with the same CLR name was ignored? That property hid the current property,
                        // thus, if it was ignored, the current property should be ignored too.
                        ignoredMembers?.ContainsKey(memberName) != true)
                    {
                        // We throw if we have two public properties that have the same JSON property name, and neither have been ignored.
                        serializableProperties = null;
                        castingRequiredForProps = false;
                        return false;
                    }
                    // Ignore the current property.
                }

                if (propGenSpec.DefaultIgnoreCondition == JsonIgnoreCondition.Always)
                {
                    (ignoredMembers ??= new Dictionary<string, PropertyGenerationSpec>()).Add(memberName, propGenSpec);
                }
            }

            Debug.Assert(PropertyGenSpecList.Count >= serializableProperties.Count);
            castingRequiredForProps = PropertyGenSpecList.Count > serializableProperties.Count;
            return true;

ReturnFalse:
            serializableProperties = null;
            castingRequiredForProps = false;
            return false;
        }

        private bool FastPathIsSupported()
        {
            if (ClassType == ClassType.Object)
            {
                foreach (PropertyGenerationSpec property in PropertyGenSpecList)
                {
                    if (property.TypeGenerationSpec.Type.IsObjectType())
                    {
                        return false;
                    }
                }

                return true;
            }

            switch (CollectionType)
            {
                case CollectionType.NotApplicable:
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
