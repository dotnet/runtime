// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record BindingHelperInfo
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

        public sealed class Builder
        {
            private MethodsToGen_CoreBindingHelper _methodsToGen;
            private bool _emitConfigurationKeyCaches;

            private readonly Dictionary<MethodsToGen_CoreBindingHelper, HashSet<TypeSpec>> _typesForGen = new();

            public SortedSet<string> Namespaces { get; } = new()
            {
                "System",
                "System.CodeDom.Compiler",
                "System.Globalization",
                "System.Runtime.CompilerServices",
                "Microsoft.Extensions.Configuration",
            };

            public bool TryRegisterTypeForBindCoreMainGen(ComplexTypeSpec type)
            {
                if (type.HasBindableMembers)
                {
                    bool registeredForBindCoreGen = TryRegisterTypeForBindCoreGen(type);
                    Debug.Assert(registeredForBindCoreGen);

                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCoreMain, type);
                    Register_AsConfigWithChildren_HelperForGen_IfRequired(type);
                    return true;
                }

                return false;
            }

            public bool TryRegisterTypeForBindCoreGen(ComplexTypeSpec type)
            {
                if (type.HasBindableMembers)
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCore, type);

                    if (type is ObjectSpec)
                    {
                        _emitConfigurationKeyCaches = true;
                    }

                    return true;
                }

                return false;
            }

            public void RegisterTypeForGetCoreGen(TypeSpec type)
            {
                RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetCore, type);
                Register_AsConfigWithChildren_HelperForGen_IfRequired(type);
            }

            public void RegisterStringParsableType(ParsableFromStringSpec type)
            {
                if (type.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue)
                {
                    _methodsToGen |= MethodsToGen_CoreBindingHelper.ParsePrimitive;
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.ParsePrimitive, type);
                }
            }

            public void RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper method, TypeSpec type)
            {
                if (!_typesForGen.TryGetValue(method, out HashSet<TypeSpec>? types))
                {
                    _typesForGen[method] = types = new HashSet<TypeSpec>();
                }

                types.Add(type);
                _methodsToGen |= method;
            }

            public void Register_AsConfigWithChildren_HelperForGen_IfRequired(TypeSpec type)
            {
                if (type is ComplexTypeSpec)
                {
                    _methodsToGen |= MethodsToGen_CoreBindingHelper.AsConfigWithChildren;
                }
            }

            public BindingHelperInfo ToIncrementalValue()
            {
                return new BindingHelperInfo
                {
                    Namespaces = Namespaces.ToImmutableEquatableArray(),
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
                            types.OrderBy(t => t.AssemblyQualifiedName).ToImmutableEquatableArray();
            }
        }
    }
}
