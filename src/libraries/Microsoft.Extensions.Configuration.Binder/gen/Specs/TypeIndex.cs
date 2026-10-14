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
            ObjectSpec objectSpec => objectSpec is { InstantiationStrategy: not ObjectInstantiationStrategy.None, InitExceptionMessage: null },
            DictionarySpec dictionarySpec => KeyIsSupported(dictionarySpec),
            CollectionSpec collectionSpec => CanBindTo(collectionSpec.ElementTypeRef),
            _ => throw new InvalidOperationException(),
        };

        /// <summary>
        /// Whether binding logic is generated for <paramref name="typeSpec"/>, i.e. whether a non-empty
        /// <c>BindCore</c> method exists for it. Emitters rely on this to decide between emitting a bind
        /// call and emitting nothing at all, so it must never report <see langword="true"/> for a type
        /// whose binding logic would be a no-op.
        /// </summary>
        public bool HasBindableMembers(ComplexTypeSpec typeSpec) =>
            typeSpec switch
            {
                ObjectSpec objectSpec => objectSpec.Properties?.Any(ShouldBindTo) is true,
                DictionarySpec dictSpec => KeyIsSupported(dictSpec) && CanBindTo(dictSpec.ElementTypeRef),
                CollectionSpec collectionSpec => CanConstructElementsOf(collectionSpec),
                _ => throw new InvalidOperationException(),
            };

        /// <summary>
        /// Whether elements can be created for <paramref name="typeSpec"/>. Every element of a non-dictionary
        /// collection is constructed and then appended; unlike a dictionary entry, there is no pre-existing
        /// element to populate in place. A complex element type that cannot be instantiated therefore yields
        /// no binding logic at all, leaving the collection with nothing to bind.
        /// </summary>
        private bool CanConstructElementsOf(CollectionSpec typeSpec)
        {
            Debug.Assert(typeSpec is not DictionarySpec, "Dictionary entries are bound in place, not constructed.");

            return GetEffectiveTypeSpec(typeSpec.ElementTypeRef) switch
            {
                SimpleTypeSpec => true,
                ComplexTypeSpec elementSpec => CanInstantiate(elementSpec),
                _ => throw new InvalidOperationException(),
            };
        }

        public bool ShouldBindTo(PropertySpec property)
        {
            if (property.IsIgnored || !IsAccessible())
            {
                return false;
            }

            TypeSpec propTypeSpec = GetEffectiveTypeSpec(property.TypeRef);
            return !IsCollectionAndCannotOverride() && !IsDictWithUnsupportedKey();

            bool IsAccessible() => property.CanGet || property.CanSet;

            bool IsDictWithUnsupportedKey() => propTypeSpec is DictionarySpec dictionarySpec && !KeyIsSupported(dictionarySpec);

            bool IsCollectionAndCannotOverride() => !property.CanSet &&
                propTypeSpec is CollectionWithCtorInitSpec
                {
                    InstantiationStrategy: CollectionInstantiationStrategy.CopyConstructor or CollectionInstantiationStrategy.LinqToDictionary
                };
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

        public static string GetInstantiationTypeDisplayString(CollectionWithCtorInitSpec type)
        {
            CollectionInstantiationConcreteType concreteType = type.InstantiationConcreteType;
            return concreteType is CollectionInstantiationConcreteType.Self
                ? type.TypeRef.FullyQualifiedName
                : GetGenericTypeDisplayString(type, concreteType);
        }

        public static string GetPopulationCastTypeDisplayString(CollectionWithCtorInitSpec type)
        {
            CollectionPopulationCastType castType = type.PopulationCastType;
            Debug.Assert(castType is not CollectionPopulationCastType.NotApplicable);
            return GetGenericTypeDisplayString(type, castType);
        }

        public static string GetGenericTypeDisplayString(CollectionWithCtorInitSpec type, Enum genericProxyTypeName)
        {
            string typeFNQ = type.TypeRef.FullyQualifiedName;
            int genericIndex = typeFNQ.IndexOf('<');

            // To get the namespace.
            int lastDotIndex = genericIndex > 0 ? typeFNQ.LastIndexOf('.', genericIndex) : -1;
            string proxyTypeNameStr = lastDotIndex >= 0 ? $"{typeFNQ.Substring(0, lastDotIndex + 1)}{genericProxyTypeName}" : genericProxyTypeName.ToString();
            string elementTypeFQN = type.ElementTypeRef.FullyQualifiedName;

            if (type is EnumerableSpec)
            {
                return $"{proxyTypeNameStr}<{elementTypeFQN}>";
            }

            string keyTypeDisplayString = ((DictionarySpec)type).KeyTypeRef.FullyQualifiedName;
            return $"{proxyTypeNameStr}<{keyTypeDisplayString}, {elementTypeFQN}>";
        }

        public bool KeyIsSupported(DictionarySpec typeSpec) =>
            // Only types that are parsable from string are supported.
            // Nullable keys not allowed; that would cause us to emit
            // code that violates dictionary key notnull constraint.
            GetTypeSpec(typeSpec.KeyTypeRef) is ParsableFromStringSpec;

        public static string GetConfigKeyCacheFieldName(ObjectSpec type) => $"s_configKeys_{type.IdentifierCompatibleSubstring}";

        public static string GetParseMethodName(ParsableFromStringSpec type)
        {
            Debug.Assert(type.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue);

            if (type.StringParsableTypeKind is StringParsableTypeKind.ByteArray)
            {
                return "ParseByteArray";
            }

            string displayString = type.TypeRef.FullyQualifiedName;

            const string GlobalPrefix = "global::";
            if (displayString.StartsWith(GlobalPrefix))
            {
                displayString = displayString.Substring(GlobalPrefix.Length);
            }

            Debug.Assert(displayString.Length > 0);
            if (char.IsLower(displayString[0]))
            {
                displayString = char.ToUpperInvariant(displayString[0]) + displayString.Substring(1);
            }

            if (displayString.Contains('.'))
            {
                displayString = displayString.Replace(".", "");
            }

            return "Parse" + displayString;
        }
    }
}
