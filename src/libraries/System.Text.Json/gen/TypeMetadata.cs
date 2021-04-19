// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration
{
    [DebuggerDisplay("Type={Type}, ClassType={ClassType}")]
    internal class TypeMetadata
    {
        private bool _hasBeenInitialized;

        public string CompilableName { get; private set; }

        public string FriendlyName { get; private set; }

        public Type Type { get; private set; }

        public ClassType ClassType { get; private set; }

        public bool IsValueType { get; private set; }

        public JsonNumberHandling? NumberHandling { get; private set; }

        public List<PropertyMetadata>? PropertiesMetadata { get; private set; }

        public CollectionType CollectionType { get; private set; }

        public TypeMetadata? CollectionKeyTypeMetadata { get; private set; }

        public TypeMetadata? CollectionValueTypeMetadata { get; private set; }

        public ObjectConstructionStrategy ConstructionStrategy { get; private set; }

        public TypeMetadata? NullableUnderlyingTypeMetadata { get; private set; }

        public string? ConverterInstantiationLogic { get; private set; }

        public bool ContainsOnlyPrimitives { get; private set; }

        public void Initialize(
            string compilableName,
            string friendlyName,
            Type type,
            ClassType classType,
            bool isValueType,
            JsonNumberHandling? numberHandling,
            List<PropertyMetadata>? propertiesMetadata,
            CollectionType collectionType,
            TypeMetadata? collectionKeyTypeMetadata,
            TypeMetadata? collectionValueTypeMetadata,
            ObjectConstructionStrategy constructionStrategy,
            TypeMetadata? nullableUnderlyingTypeMetadata,
            string? converterInstantiationLogic,
            bool containsOnlyPrimitives)
        {
            if (_hasBeenInitialized)
            {
                throw new InvalidOperationException("Type metadata has already been initialized.");
            }

            _hasBeenInitialized = true;

            CompilableName = compilableName;
            FriendlyName = friendlyName;
            Type = type;
            ClassType = classType;
            IsValueType = isValueType;
            NumberHandling = numberHandling;
            PropertiesMetadata = propertiesMetadata;
            CollectionType = collectionType;
            CollectionKeyTypeMetadata = collectionKeyTypeMetadata;
            CollectionValueTypeMetadata = collectionValueTypeMetadata;
            ConstructionStrategy = constructionStrategy;
            NullableUnderlyingTypeMetadata = nullableUnderlyingTypeMetadata;
            ConverterInstantiationLogic = converterInstantiationLogic;
            ContainsOnlyPrimitives = containsOnlyPrimitives;
        }
    }
}
