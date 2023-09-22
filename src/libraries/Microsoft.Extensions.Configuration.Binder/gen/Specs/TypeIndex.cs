// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed class TypeIndex
    {
        private readonly Dictionary<TypeRef, TypeSpec> _index;

        public TypeIndex(IEnumerable<TypeSpec> typeSpecs) => _index = typeSpecs.ToDictionary(spec => spec.TypeRef);

        public bool CanBindTo(TypeRef typeRef) => CanBindTo(GetEffectiveTypeSpec(typeRef));

        public bool CanBindTo(TypeSpec typeSpec) => typeSpec switch
        {
            SimpleTypeSpec => true,
            ComplexTypeSpec complexTypeSpec => CanInstantiate(complexTypeSpec) || HasBindableMembers(complexTypeSpec),
            _ => throw new InvalidOperationException(),
        };

        public bool CanInstantiate(ComplexTypeSpec typeSpec)
        {
            return CanInstantiate(typeSpec.TypeRef);

            bool CanInstantiate(TypeRef typeRef) => GetEffectiveTypeSpec(typeRef) switch
            {
                ObjectSpec objectType => objectType is { InstantiationStrategy: not ObjectInstantiationStrategy.None, InitExceptionMessage: null },
                DictionarySpec dictionaryType => GetTypeSpec(dictionaryType.TypeRef) is ParsableFromStringSpec,
                CollectionSpec => true,
                _ => throw new InvalidOperationException(),
            };
        }

        public bool HasBindableMembers(ComplexTypeSpec typeSpec) =>
            typeSpec switch
            {
                ObjectSpec objectSpec => objectSpec.Properties?.Any(p => p.ShouldBindTo) is true,
                ArraySpec or EnumerableSpec => CanBindTo(((CollectionSpec)typeSpec).ElementTypeRef),
                DictionarySpec dictSpec => GetTypeSpec(dictSpec.KeyTypeRef) is ParsableFromStringSpec && CanBindTo(dictSpec.ElementTypeRef),
                _ => throw new InvalidOperationException(),
            };

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
            Debug.Assert(concreteType is not CollectionInstantiationConcreteType.None);

            if (concreteType is CollectionInstantiationConcreteType.Self)
            {
                return type.DisplayString;
            }

            return GetGenericTypeDisplayString(type, concreteType);
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
