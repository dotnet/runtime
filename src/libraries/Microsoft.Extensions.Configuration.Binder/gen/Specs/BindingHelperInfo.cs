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

            public bool TryRegisterComplexTypeForMethodGen(ComplexTypeSpec type)
            {
                if (!_typeIndex.CanBindTo(type.TypeRef))
                {
                    return false;
                }

                if (type is ObjectSpec { InstantiationStrategy: ObjectInstantiationStrategy.ParameterizedConstructor } objectType
                    && _typeIndex.CanInstantiate(objectType))
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.Initialize, type);
                }
                else if (type is DictionarySpec { InstantiationStrategy: CollectionInstantiationStrategy.LinqToDictionary })
                {
                    _namespaces.Add("System.Linq");
                }

                TryRegisterTypeForBindCoreGen(type);
                return true;
            }

            public bool TryRegisterTypeForBindCoreMainGen(ComplexTypeSpec type)
            {
                if (_typeIndex.HasBindableMembers(type))
                {
                    bool bindCoreRegistered = TryRegisterTypeForBindCoreGen(type);
                    Debug.Assert(bindCoreRegistered);
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCoreMain, type);
                    RegisterForGen_AsConfigWithChildrenHelper();
                    return true;
                }

                return false;
            }

            public bool TryRegisterTypeForBindCoreGen(ComplexTypeSpec type)
            {
                if (_typeIndex.HasBindableMembers(type))
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCore, type);

                    if (type is ObjectSpec)
                    {
                        _emitConfigurationKeyCaches = true;

                        // List<string> is used in generated code as a temp holder for formatting
                        // an error for config properties that don't map to object properties.
                        _namespaces.Add("System.Collections.Generic");
                    }

                    return true;
                }

                return false;
            }

            public bool TryRegisterTypeForGetCoreGen(TypeSpec type)
            {
                if (!_typeIndex.CanBindTo(type.TypeRef))
                {
                    return false;
                }

                RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetCore, type);
                RegisterForGen_AsConfigWithChildrenHelper();

                if (type is ComplexTypeSpec complexType)
                {
                    bool registered = TryRegisterComplexTypeForMethodGen(complexType);
                    Debug.Assert(registered);
                }

                return true;
            }

            public bool TryRegisterTypeForGetValueCoreGen(TypeSpec type)
            {
                ParsableFromStringSpec effectiveType = (ParsableFromStringSpec)_typeIndex.GetEffectiveTypeSpec(type);
                RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetValueCore, type);
                RegisterStringParsableType(effectiveType);
                return true;
            }

            public void RegisterNamespace(string @namespace) => _namespaces.Add(@namespace);

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
                            types.OrderBy(t => t.TypeRef.FullyQualifiedName).ToImmutableEquatableArray();
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

                    if (type is { Namespace: string @namespace })
                    {
                        _namespaces.Add(@namespace);
                    }

                    RegisterTransitiveTypesForMethodGen(type);
                }
            }

            private void RegisterTransitiveTypesForMethodGen(TypeSpec typeSpec)
            {
                switch (typeSpec)
                {
                    case NullableSpec nullableTypeSpec:
                        {
                            RegisterTransitiveTypesForMethodGen(_typeIndex.GetEffectiveTypeSpec(nullableTypeSpec));
                        }
                        break;
                    case ArraySpec:
                    case EnumerableSpec:
                        {
                            RegisterComplexTypeForMethodGen(((CollectionSpec)typeSpec).ElementTypeRef);
                        }
                        break;
                    case DictionarySpec dictionaryTypeSpec:
                        {
                            RegisterComplexTypeForMethodGen(dictionaryTypeSpec.KeyTypeRef);
                            RegisterComplexTypeForMethodGen(dictionaryTypeSpec.ElementTypeRef);
                        }
                        break;
                    case ObjectSpec objectType when _typeIndex.HasBindableMembers(objectType):
                        {
                            foreach (PropertySpec property in objectType.Properties!)
                            {
                                RegisterComplexTypeForMethodGen(property.TypeRef);
                            }
                        }
                        break;
                }

                void RegisterComplexTypeForMethodGen(TypeRef transitiveTypeRef)
                {
                    TypeSpec effectiveTypeSpec = _typeIndex.GetTypeSpec(transitiveTypeRef);

                    if (effectiveTypeSpec is ParsableFromStringSpec parsableFromStringSpec)
                    {
                        RegisterStringParsableType(parsableFromStringSpec);
                    }
                    else if (effectiveTypeSpec is ComplexTypeSpec complexEffectiveTypeSpec)
                    {
                        if (_typeIndex.HasBindableMembers(complexEffectiveTypeSpec))
                        {
                            RegisterForGen_AsConfigWithChildrenHelper();
                        }

                        TryRegisterComplexTypeForMethodGen(complexEffectiveTypeSpec);
                    }
                }
            }

            private void RegisterStringParsableType(ParsableFromStringSpec type)
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
