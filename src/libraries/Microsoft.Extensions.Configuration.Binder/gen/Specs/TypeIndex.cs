// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed class TypeIndex(IEnumerable<TypeSpec> typeSpecs)
    {
        private readonly Dictionary<TypeRef, TypeSpec> _index = typeSpecs.ToDictionary(spec => spec.TypeRef);

        public bool CanBindTo(TypeRef typeRef) => GetEffectiveTypeSpec(typeRef) switch
        {
            SimpleTypeSpec => true,
            ComplexTypeSpec complexTypeSpec => CanInstantiate(complexTypeSpec) || HasBindableMembers(complexTypeSpec),
            _ => throw new InvalidOperationException(),
        };

        public bool CanInstantiate(ComplexTypeSpec typeSpec) => typeSpec switch
        {
            ObjectSpec objectType => objectType is { InstantiationStrategy: not ObjectInstantiationStrategy.None, InitExceptionMessage: null },
            DictionarySpec dictionaryType =>
                // Nullable not allowed; that would cause us to emit code that violates dictionary key notnull generic parameter constraint.
                GetTypeSpec(dictionaryType.KeyTypeRef) is ParsableFromStringSpec,
            CollectionSpec => true,
            _ => throw new InvalidOperationException(),
        };

        public bool HasBindableMembers(ComplexTypeSpec typeSpec) =>
            typeSpec switch
            {
                ObjectSpec objectSpec => objectSpec.Properties?.Any(ShouldBindTo) is true,
                DictionarySpec dictSpec => GetTypeSpec(dictSpec.KeyTypeRef) is ParsableFromStringSpec && CanBindTo(dictSpec.ElementTypeRef),
                CollectionSpec collectionSpec => CanBindTo(collectionSpec.ElementTypeRef),
                _ => throw new InvalidOperationException(),
            };

        public bool ShouldBindTo(PropertySpec property)
        {
            bool isAccessible = property.CanGet || property.CanSet;

            bool isCollectionAndCannotOverride = !property.CanSet &&
                GetEffectiveTypeSpec(property.TypeRef) is CollectionWithCtorInitSpec
                {
                    InstantiationStrategy: CollectionInstantiationStrategy.CopyConstructor or CollectionInstantiationStrategy.LinqToDictionary
                };

            return isAccessible && !isCollectionAndCannotOverride;
        }

        public TypeSpec GetEffectiveTypeSpec(TypeRef typeRef)
        {
            TypeSpec typeSpec = GetTypeSpec(typeRef);
            return GetEffectiveTypeSpec(typeSpec);
        }

        public TypeSpec GetEffectiveTypeSpec(TypeSpec typeSpec)
        {
            TypeRef effectiveRef = typeSpec.EffectiveTypeRef;
            TypeSpec effectiveSpec = effectiveRef == typeSpec.TypeRef ? typeSpec : _index[effectiveRef];
            return effectiveSpec;
        }

        public TypeSpec GetTypeSpec(TypeRef typeRef) => _index[typeRef];

        public string GetInstantiationTypeDisplayString(CollectionWithCtorInitSpec type)
        {
            CollectionInstantiationConcreteType concreteType = type.InstantiationConcreteType;
            return concreteType is CollectionInstantiationConcreteType.Self
                ? type.DisplayString
                : GetGenericTypeDisplayString(type, concreteType);
        }

        public string GetPopulationCastTypeDisplayString(CollectionWithCtorInitSpec type)
        {
            CollectionPopulationCastType castType = type.PopulationCastType;
            Debug.Assert(castType is not CollectionPopulationCastType.NotApplicable);
            return GetGenericTypeDisplayString(type, castType);
        }

        public string GetGenericTypeDisplayString(CollectionWithCtorInitSpec type, Enum genericProxyTypeName)
        {
            string proxyTypeNameStr = genericProxyTypeName.ToString();
            string elementTypeDisplayString = GetTypeSpec(type.ElementTypeRef).DisplayString;

            if (type is EnumerableSpec)
            {
                return $"{proxyTypeNameStr}<{elementTypeDisplayString}>";
            }

            string keyTypeDisplayString = GetTypeSpec(((DictionarySpec)type).KeyTypeRef).DisplayString;
            return $"{proxyTypeNameStr}<{keyTypeDisplayString}, {elementTypeDisplayString}>";
        }
    }
}
