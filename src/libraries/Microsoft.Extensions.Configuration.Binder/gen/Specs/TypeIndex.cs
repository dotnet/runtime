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

        public bool RequiresReflectionForTypeConverters(TypeSpec typeSpec, MethodsToGen overload)
        {
            PropertyBindingContext context = (overload & MethodsToGen.ConfigBinder_Get) is not 0
                ? PropertyBindingContext.CreateInstance
                : PropertyBindingContext.ExistingInstance;
            Dictionary<TypeRef, PropertyBindingContext> visited = new();
            return HasPropertyTypeConverterCore(typeSpec, context);

            bool HasPropertyTypeConverterCore(TypeSpec current, PropertyBindingContext currentContext)
            {
                visited.TryGetValue(current.TypeRef, out PropertyBindingContext visitedContexts);
                PropertyBindingContext contextsToVisit = currentContext & ~visitedContexts;
                if (contextsToVisit is 0)
                {
                    return false;
                }

                visited[current.TypeRef] = visitedContexts | contextsToVisit;

                return current switch
                {
                    NullableSpec nullableSpec => ContainsPropertyTypeConverter(nullableSpec.EffectiveTypeRef, contextsToVisit),
                    DictionarySpec dictionarySpec => ContainsPropertyTypeConverter(
                        dictionarySpec.ElementTypeRef,
                        (contextsToVisit & PropertyBindingContext.ExistingInstance) is not 0
                            ? PropertyBindingContext.ExistingInstance | PropertyBindingContext.CreateInstance
                            : PropertyBindingContext.CreateInstance),
                    CollectionSpec collectionSpec => ContainsPropertyTypeConverter(
                        collectionSpec.ElementTypeRef,
                        PropertyBindingContext.CreateInstance),
                    ObjectSpec objectSpec => ContainsObjectPropertyTypeConverter(objectSpec, contextsToVisit),
                    _ => false,
                };
            }

            bool ContainsObjectPropertyTypeConverter(ObjectSpec objectSpec, PropertyBindingContext currentContext)
            {
                bool bindExistingInstance = (currentContext & PropertyBindingContext.ExistingInstance) is not 0;
                bool createInstance = (currentContext & PropertyBindingContext.CreateInstance) is not 0 && CanInstantiate(objectSpec);

                if (createInstance && objectSpec.RequiresConstructorAccessor && objectSpec.ConstructorAccessor is null)
                {
                    return true;
                }

                if ((!bindExistingInstance && !createInstance) || objectSpec.Properties is not { } properties)
                {
                    return false;
                }

                foreach (PropertySpec property in properties)
                {
                    if (property.IsIgnored)
                    {
                        continue;
                    }

                    bool isConstructorBound = objectSpec.InstantiationStrategy is ObjectInstantiationStrategy.ParameterizedConstructor &&
                        property.MatchingCtorParam is not null;

                    if (property.CanGet && property.IsInitOnly && property.InitOnlySetter is null &&
                        (bindExistingInstance || createInstance))
                    {
                        return true;
                    }

                    if (bindExistingInstance && property.HasTypeConverterOnBindableProperty &&
                        (property.TypeConverter is null || property.HasUnresolvedTypeConverterFallback))
                    {
                        return true;
                    }

                    if (createInstance && !isConstructorBound && property.HasTypeConverterOnBindableProperty &&
                        (property.TypeConverter is null || property.HasUnresolvedTypeConverterFallback))
                    {
                        return true;
                    }

                    if (bindExistingInstance && ContainsPropertyTypeConverterForNormalBinding(property))
                    {
                        return true;
                    }

                    if (!createInstance)
                    {
                        continue;
                    }

                    if (isConstructorBound)
                    {
                        if ((property.AccessDeclaringType is null && property.HasTypeConverter &&
                                property.MatchingCtorParameterTypeMatches &&
                                (property.MatchingCtorParam!.TypeConverter is null || property.MatchingCtorParam.HasUnresolvedTypeConverterFallback)) ||
                            ContainsPropertyTypeConverter(property.MatchingCtorParam!.TypeRef, PropertyBindingContext.CreateInstance))
                        {
                            return true;
                        }
                    }
                    else if (ContainsPropertyTypeConverterForNormalBinding(
                        property,
                        duringInitialization: objectSpec.InstantiationStrategy is ObjectInstantiationStrategy.ParameterizedConstructor))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool ContainsPropertyTypeConverterForNormalBinding(PropertySpec property, bool duringInitialization = false)
            {
                if (!property.CanGet || !_index.TryGetValue(property.TypeRef, out TypeSpec? propertyType))
                {
                    return false;
                }

                PropertyBindingContext propertyContext = property.CanSet || (duringInitialization && property.SetOnInit)
                    ? PropertyBindingContext.ExistingInstance | PropertyBindingContext.CreateInstance
                    : PropertyBindingContext.ExistingInstance;
                return HasPropertyTypeConverterCore(propertyType, propertyContext);
            }

            bool ContainsPropertyTypeConverter(TypeRef typeRef, PropertyBindingContext currentContext) =>
                _index.TryGetValue(typeRef, out TypeSpec? referencedType) &&
                HasPropertyTypeConverterCore(referencedType, currentContext);
        }

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

            if (property.TypeConverter is not null)
            {
                return true;
            }

            if (!_index.TryGetValue(property.TypeRef, out TypeSpec? propertyTypeSpec))
            {
                return false;
            }

            TypeSpec propTypeSpec = GetEffectiveTypeSpec(propertyTypeSpec);
            return !IsCollectionAndCannotOverride() && !IsDictWithUnsupportedKey();

            bool IsAccessible() => property.CanGet;

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

        public bool TryGetTypeSpec(TypeRef typeRef, out TypeSpec? typeSpec) => _index.TryGetValue(typeRef, out typeSpec);

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

        [Flags]
        private enum PropertyBindingContext
        {
            ExistingInstance = 0x1,
            CreateInstance = 0x2,
        }
    }
}
