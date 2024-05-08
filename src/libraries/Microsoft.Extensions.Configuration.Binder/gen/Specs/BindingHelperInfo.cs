// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record BindingHelperInfo
    {
        public required ImmutableEquatableArray<string> Namespaces { get; init; }
        public required bool EmitConfigurationKeyCaches { get; init; }

        public required MethodsToGen_CoreBindingHelper MethodsToGen { get; init; }
        public required ImmutableEquatableArray<ComplexTypeSpec>? TypesForGen_BindCoreMain { get; init; }
        public required ImmutableEquatableArray<TypeSpec>? TypesForGen_GetCore { get; init; }
        public required ImmutableEquatableArray<TypeSpec>? TypesForGen_GetValueCore { get; init; }
        public required ImmutableEquatableArray<ComplexTypeSpec>? TypesForGen_BindCore { get; init; }
        public required ImmutableEquatableArray<ObjectSpec>? TypesForGen_Initialize { get; init; }
        public required ImmutableEquatableArray<ParsableFromStringSpec>? TypesForGen_ParsePrimitive { get; init; }

        internal sealed class Builder(TypeIndex _typeIndex)
        {
            private readonly Dictionary<TypeRef, bool> _seenTransitiveTypes = new();

            private MethodsToGen_CoreBindingHelper _methodsToGen;
            private bool _emitConfigurationKeyCaches;

            private readonly Dictionary<MethodsToGen_CoreBindingHelper, HashSet<TypeSpec>> _typesForGen = new();

            private readonly SortedSet<string> _namespaces = new()
            {
                "System",
                "System.CodeDom.Compiler",
                "System.Globalization",
                "System.Runtime.CompilerServices",
                "Microsoft.Extensions.Configuration",
            };

            public BindingHelperInfo ToIncrementalValue()
            {
                return new BindingHelperInfo
                {
                    Namespaces = _namespaces.ToImmutableEquatableArray(),
                    EmitConfigurationKeyCaches = _emitConfigurationKeyCaches,

                    MethodsToGen = _methodsToGen,
                    TypesForGen_GetCore = GetTypesForGen_CoreBindingHelper<TypeSpec>(MethodsToGen_CoreBindingHelper.GetCore),
                    TypesForGen_BindCoreMain = GetTypesForGen_CoreBindingHelper<ComplexTypeSpec>(MethodsToGen_CoreBindingHelper.BindCoreMain),
                    TypesForGen_GetValueCore = GetTypesForGen_CoreBindingHelper<TypeSpec>(MethodsToGen_CoreBindingHelper.GetValueCore),
                    TypesForGen_BindCore = GetTypesForGen_CoreBindingHelper<ComplexTypeSpec>(MethodsToGen_CoreBindingHelper.BindCore),
                    TypesForGen_Initialize = GetTypesForGen_CoreBindingHelper<ObjectSpec>(MethodsToGen_CoreBindingHelper.Initialize),
                    TypesForGen_ParsePrimitive = GetTypesForGen_CoreBindingHelper<ParsableFromStringSpec>(MethodsToGen_CoreBindingHelper.ParsePrimitive)
                };

                ImmutableEquatableArray<TSpec>? GetTypesForGen_CoreBindingHelper<TSpec>(MethodsToGen_CoreBindingHelper overload)
                    where TSpec : TypeSpec, IEquatable<TSpec>
                {
                    _typesForGen.TryGetValue(overload, out HashSet<TypeSpec>? typesAsBase);

                    if (typesAsBase is null)
                    {
                        return null;
                    }

                    IEnumerable<TSpec> types = typeof(TSpec) == typeof(TypeSpec)
                        ? (HashSet<TSpec>)(object)typesAsBase
                        : typesAsBase.Select(t => (TSpec)t);

                    return GetTypesForGen(types);
                }

                static ImmutableEquatableArray<TSpec> GetTypesForGen<TSpec>(IEnumerable<TSpec> types)
                    where TSpec : TypeSpec, IEquatable<TSpec> =>
                            types.ToImmutableEquatableArray();
            }

            public bool TryRegisterTypeForGetGen(TypeSpec type)
            {
                if (TryRegisterTransitiveTypesForMethodGen(type.TypeRef))
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetCore, type);
                    RegisterForGen_AsConfigWithChildrenHelper();
                    return true;
                }

                return false;
            }

            public bool TryRegisterTypeForGetValueGen(TypeSpec typeSpec)
            {
                ParsableFromStringSpec effectiveType = (ParsableFromStringSpec)_typeIndex.GetEffectiveTypeSpec(typeSpec);
                RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetValueCore, typeSpec);
                RegisterStringParsableTypeIfApplicable(effectiveType);
                return true;
            }

            public bool TryRegisterTypeForBindCoreMainGen(ComplexTypeSpec type)
            {
                if (TryRegisterTransitiveTypesForMethodGen(type.TypeRef))
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCoreMain, type);
                    RegisterForGen_AsConfigWithChildrenHelper();
                    return true;
                }

                return false;
            }

            public bool TryRegisterTransitiveTypesForMethodGen(TypeRef typeRef)
            {
                return _seenTransitiveTypes.TryGetValue(typeRef, out bool isValid)
                    ? isValid
                    : (_seenTransitiveTypes[typeRef] = TryRegisterCore());

                bool TryRegisterCore()
                {
                    switch (_typeIndex.GetTypeSpec(typeRef))
                    {
                        case NullableSpec nullableSpec:
                            {
                                return TryRegisterTransitiveTypesForMethodGen(nullableSpec.EffectiveTypeRef);
                            }
                        case ParsableFromStringSpec stringParsableSpec:
                            {
                                RegisterStringParsableTypeIfApplicable(stringParsableSpec);
                                return true;
                            }
                        case DictionarySpec dictionarySpec:
                            {
                                bool shouldRegister = _typeIndex.CanBindTo(typeRef) &&
                                    TryRegisterTransitiveTypesForMethodGen(dictionarySpec.KeyTypeRef) &&
                                    TryRegisterTransitiveTypesForMethodGen(dictionarySpec.ElementTypeRef) &&
                                    TryRegisterTypeForBindCoreGen(dictionarySpec);

                                if (shouldRegister && dictionarySpec.InstantiationStrategy is CollectionInstantiationStrategy.LinqToDictionary)
                                {
                                    _namespaces.Add("System.Linq");
                                }

                                return shouldRegister;
                            }
                        case CollectionSpec collectionSpec:
                            {
                                if (_typeIndex.GetTypeSpec(collectionSpec.ElementTypeRef) is ComplexTypeSpec)
                                {
                                    _namespaces.Add("System.Linq");
                                }

                                return TryRegisterTransitiveTypesForMethodGen(collectionSpec.ElementTypeRef) &&
                                    TryRegisterTypeForBindCoreGen(collectionSpec);
                            }
                        case ObjectSpec objectSpec:
                            {
                                // Base case to avoid stack overflow for recursive object graphs.
                                // Register all object types for gen; we need to throw runtime exceptions in some cases.
                                bool shouldRegister = true;
                                _seenTransitiveTypes.Add(typeRef, shouldRegister);

                                // List<string> is used in generated code as a temp holder for formatting
                                // an error for config properties that don't map to object properties.
                                _namespaces.Add("System.Collections.Generic");

                                if (_typeIndex.HasBindableMembers(objectSpec))
                                {
                                    foreach (PropertySpec property in objectSpec.Properties!)
                                    {
                                        TryRegisterTransitiveTypesForMethodGen(property.TypeRef);

                                        if (_typeIndex.GetTypeSpec(property.TypeRef) is ComplexTypeSpec)
                                        {
                                            RegisterForGen_AsConfigWithChildrenHelper();
                                        }
                                    }

                                    bool registeredForBindCore = TryRegisterTypeForBindCoreGen(objectSpec);
                                    Debug.Assert(registeredForBindCore);

                                    if (objectSpec is { InstantiationStrategy: ObjectInstantiationStrategy.ParameterizedConstructor, InitExceptionMessage: null })
                                    {
                                        RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.Initialize, objectSpec);
                                    }
                                }

                                return true;
                            }
                        default:
                            {
                                return true;
                            }
                    }
                }
            }

            public void RegisterNamespace(string @namespace) => _namespaces.Add(@namespace);

            private bool TryRegisterTypeForBindCoreGen(ComplexTypeSpec type)
            {
                if (_typeIndex.HasBindableMembers(type))
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCore, type);
                    _emitConfigurationKeyCaches = true;
                    return true;
                }

                return false;
            }

            private void RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper method, TypeSpec type)
            {
                if (!_typesForGen.TryGetValue(method, out HashSet<TypeSpec>? types))
                {
                    _typesForGen[method] = types = new HashSet<TypeSpec>();
                }

                if (types.Add(type))
                {
                    _methodsToGen |= method;
                }
            }

            private void RegisterStringParsableTypeIfApplicable(ParsableFromStringSpec type)
            {
                if (type.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue)
                {
                    _methodsToGen |= MethodsToGen_CoreBindingHelper.ParsePrimitive;
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.ParsePrimitive, type);
                }
            }

            private void RegisterForGen_AsConfigWithChildrenHelper() => _methodsToGen |= MethodsToGen_CoreBindingHelper.AsConfigWithChildren;
        }
    }
}
