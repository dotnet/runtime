// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json.SourceGeneration.Reflection;

namespace System.Text.Json.SourceGeneration
{
    [DebuggerDisplay("Type={Type}, ClassType={ClassType}")]
    internal class TypeGenerationSpec
    {
        private bool _hasBeenInitialized;

        /// <summary>
        /// Fully qualified assembly name, prefaced with "global::", e.g. global::System.Numerics.BigInteger.
        /// </summary>
        public string TypeRef { get; private set; }

        /// <summary>
        /// The name of the public JsonTypeInfo<T> property for this type on the generated context class.
        /// For example, if the context class is named MyJsonContext, and the value of this property is JsonMessage;
        /// then users will call MyJsonContext.JsonMessage to access generated metadata for the type.
        /// </summary>
        public string TypeInfoPropertyName { get; set; }

        public bool GenerateMetadata { get; set; } = true;

        private bool? _generateSerializationLogic;
        public bool GenerateSerializationLogic
        {
            get => _generateSerializationLogic ??= FastPathIsSupported();
            set => _generateSerializationLogic = value;
        }

        public Type Type { get; private set; }

        public ClassType ClassType { get; private set; }

        public bool IsValueType { get; private set; }

        public bool CanBeNull { get; private set; }

        public JsonNumberHandling? NumberHandling { get; private set; }

        public List<PropertyGenerationSpec>? PropertiesMetadata { get; private set; }

        public CollectionType CollectionType { get; private set; }

        public TypeGenerationSpec? CollectionKeyTypeMetadata { get; private set; }

        public TypeGenerationSpec? CollectionValueTypeMetadata { get; private set; }

        public ObjectConstructionStrategy ConstructionStrategy { get; private set; }

        public TypeGenerationSpec? NullableUnderlyingTypeMetadata { get; private set; }

        public string? ConverterInstantiationLogic { get; private set; }

        public void Initialize(
            string typeRef,
            string typeInfoPropertyName,
            Type type,
            ClassType classType,
            bool isValueType,
            JsonNumberHandling? numberHandling,
            List<PropertyGenerationSpec>? propertiesMetadata,
            CollectionType collectionType,
            TypeGenerationSpec? collectionKeyTypeMetadata,
            TypeGenerationSpec? collectionValueTypeMetadata,
            ObjectConstructionStrategy constructionStrategy,
            TypeGenerationSpec? nullableUnderlyingTypeMetadata,
            string? converterInstantiationLogic)
        {
            if (_hasBeenInitialized)
            {
                throw new InvalidOperationException("Type metadata has already been initialized.");
            }

            _hasBeenInitialized = true;

            TypeRef = $"global::{typeRef}";
            TypeInfoPropertyName = typeInfoPropertyName;
            Type = type;
            ClassType = classType;
            IsValueType = isValueType;
            CanBeNull = !isValueType || nullableUnderlyingTypeMetadata != null;
            NumberHandling = numberHandling;
            PropertiesMetadata = propertiesMetadata;
            CollectionType = collectionType;
            CollectionKeyTypeMetadata = collectionKeyTypeMetadata;
            CollectionValueTypeMetadata = collectionValueTypeMetadata;
            ConstructionStrategy = constructionStrategy;
            NullableUnderlyingTypeMetadata = nullableUnderlyingTypeMetadata;
            ConverterInstantiationLogic = converterInstantiationLogic;
        }

        public bool FastPathIsSupported()
                => Type != TypeExtensions.ObjectArrayType && (ClassType == ClassType.Object ||
                CollectionType == CollectionType.Dictionary ||
                CollectionType == CollectionType.Array ||
                CollectionType == CollectionType.List);
    }
}
