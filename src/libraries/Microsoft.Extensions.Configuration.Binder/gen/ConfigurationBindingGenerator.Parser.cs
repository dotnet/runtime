// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        private sealed partial class Parser
        {
            private record struct InvocationDiagnosticInfo(DiagnosticDescriptor Descriptor, object[]? MessageArgs);

            private readonly SourceProductionContext _context;
            private readonly SourceGenerationSpec _sourceGenSpec = new();
            private readonly KnownTypeSymbols _typeSymbols;
            private readonly ImmutableArray<BinderInvocation> _invocations;

            private readonly Dictionary<ITypeSymbol, TypeSpec?> _createdSpecs = new(SymbolEqualityComparer.Default);
            private readonly HashSet<ITypeSymbol> _unsupportedTypes = new(SymbolEqualityComparer.Default);

            private readonly List<InvocationDiagnosticInfo> _invocationTargetTypeDiags = new();
            private readonly Dictionary<ITypeSymbol, HashSet<InvocationDiagnosticInfo>> _typeDiagnostics = new(SymbolEqualityComparer.Default);

            public Parser(SourceProductionContext context, KnownTypeSymbols typeSymbols, ImmutableArray<BinderInvocation> invocations)
            {
                _context = context;
                _typeSymbols = typeSymbols;
                _invocations = invocations;
            }

            public SourceGenerationSpec? GetSourceGenerationSpec()
            {
                if (_typeSymbols is not { IConfiguration: { }, ConfigurationBinder: { } })
                {
                    return null;
                }

                foreach (BinderInvocation invocation in _invocations)
                {
                    IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
                    INamedTypeSymbol? candidateBinderType = targetMethod.ContainingType;
                    Debug.Assert(targetMethod.IsExtensionMethod);

                    if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.ConfigurationBinder))
                    {
                        RegisterMethodInvocation_ConfigurationBinder(invocation);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.OptionsBuilderConfigurationExtensions))
                    {
                        RegisterMethodInvocation_OptionsBuilderExt(invocation);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.OptionsConfigurationServiceCollectionExtensions))
                    {
                        RegisterMethodInvocation_ServiceCollectionExt(invocation);
                    }
                }

                return _sourceGenSpec;
            }

            private static bool IsValidRootConfigType(ITypeSymbol? type)
            {
                if (type is null ||
                    type.SpecialType is SpecialType.System_Object or SpecialType.System_Void ||
                    type.TypeKind is TypeKind.TypeParameter or TypeKind.Pointer or TypeKind.Error ||
                    type.IsRefLikeType ||
                    ContainsGenericParameters(type))
                {
                    return false;
                }

                return true;
            }

            private TypeSpec? GetTargetTypeForRootInvocation(ITypeSymbol? type, Location? invocationLocation)
            {
                if (!IsValidRootConfigType(type))
                {
                    _context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CouldNotDetermineTypeInfo, invocationLocation));
                    return null;
                }

                return GetTargetTypeForRootInvocationCore(type, invocationLocation);
            }

            public TypeSpec? GetTargetTypeForRootInvocationCore(ITypeSymbol type, Location? invocationLocation)
            {
                TypeSpec? spec = GetOrCreateTypeSpec(type);

                foreach (InvocationDiagnosticInfo diag in _invocationTargetTypeDiags)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(diag.Descriptor, invocationLocation, diag.MessageArgs));
                }

                _invocationTargetTypeDiags.Clear();
                return spec;
            }

            private TypeSpec? GetOrCreateTypeSpec(ITypeSymbol type)
            {
                if (_createdSpecs.TryGetValue(type, out TypeSpec? spec))
                {
                    if (_typeDiagnostics.TryGetValue(type, out HashSet<InvocationDiagnosticInfo>? typeDiags))
                    {
                        _invocationTargetTypeDiags.AddRange(typeDiags);
                    }

                    return spec;
                }

                if (IsNullable(type, out ITypeSymbol? underlyingType))
                {
                    spec = TryGetTypeSpec(underlyingType, Diagnostics.NullableUnderlyingTypeNotSupported, out TypeSpec? underlyingTypeSpec)
                        ? new NullableSpec(type, underlyingTypeSpec)
                        : null;
                }
                else if (IsParsableFromString(type, out StringParsableTypeKind specialTypeKind))
                {
                    ParsableFromStringSpec stringParsableSpec = new(type) { StringParsableTypeKind = specialTypeKind };

                    if (stringParsableSpec.StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue)
                    {
                        _sourceGenSpec.PrimitivesForHelperGen.Add(stringParsableSpec);
                    }

                    spec = stringParsableSpec;
                }
                else if (IsSupportedArrayType(type))
                {
                    spec = CreateArraySpec((type as IArrayTypeSymbol));
                }
                else if (IsCollection(type))
                {
                    spec = CreateCollectionSpec((INamedTypeSymbol)type);
                }
                else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.IConfigurationSection))
                {
                    spec = new ConfigurationSectionSpec(type);
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    // List<string> is used in generated code as a temp holder for formatting
                    // an error for config properties that don't map to object properties.
                    _sourceGenSpec.Namespaces.Add("System.Collections.Generic");

                    spec = CreateObjectSpec(namedType);
                }
                else
                {
                    RegisterUnsupportedType(type, Diagnostics.TypeNotSupported);
                }

                foreach (InvocationDiagnosticInfo diag in _invocationTargetTypeDiags)
                {
                    RegisterTypeDiagnostic(type, diag);
                }

                if (spec is null)
                {
                    return null;
                }

                string @namespace = spec.Namespace;
                if (@namespace is not null and not "<global namespace>")
                {
                    _sourceGenSpec.Namespaces.Add(@namespace);
                }

                return _createdSpecs[type] = spec;
            }

            private void RegisterTypeForBindCoreMainGen(TypeSpec typeSpec)
            {
                if (typeSpec.NeedsMemberBinding)
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCoreMain, typeSpec);
                    RegisterTypeForBindCoreGen(typeSpec);
                    _sourceGenSpec.MethodsToGen_CoreBindingHelper |= MethodsToGen_CoreBindingHelper.AsConfigWithChildren;
                }
            }

            private void RegisterTypeForBindCoreGen(TypeSpec typeSpec)
            {
                if (typeSpec.NeedsMemberBinding)
                {
                    RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.BindCore, typeSpec);
                }
            }

            private void RegisterTypeForGetCoreGen(TypeSpec typeSpec)
            {
                RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.GetCore, typeSpec);
                _sourceGenSpec.MethodsToGen_CoreBindingHelper |= MethodsToGen_CoreBindingHelper.AsConfigWithChildren;
            }

            private void RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper method, TypeSpec type)
            {
                if (!_sourceGenSpec.TypesForGen_CoreBindingHelper_Methods.TryGetValue(method, out HashSet<TypeSpec>? types))
                {
                    _sourceGenSpec.TypesForGen_CoreBindingHelper_Methods[method] = types = new HashSet<TypeSpec>();
                }

                types.Add(type);
                _sourceGenSpec.MethodsToGen_CoreBindingHelper |= method;
            }

            /// <summary>
            /// Registers interceptors for root binding methods, except for ConfigurationBinder.Bind,
            /// which is handled by <see cref="RegisterAsInterceptor_ConfigBinder_BindMethod"/>
            /// </summary>
            private void RegisterAsInterceptor(Enum method, IInvocationOperation operation) =>
                _sourceGenSpec.InterceptionInfo.RegisterCacheEntry(method, new InterceptorLocationInfo(operation));

            private static bool IsNullable(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? underlyingType)
            {
                if (type is INamedTypeSymbol { IsGenericType: true } genericType &&
                    genericType.ConstructUnboundGenericType() is INamedTypeSymbol { } unboundGeneric &&
                    unboundGeneric.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    underlyingType = genericType.TypeArguments[0];
                    return true;
                }

                underlyingType = null;
                return false;
            }

            private bool IsParsableFromString(ITypeSymbol type, out StringParsableTypeKind typeKind)
            {
                if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
                {
                    typeKind = StringParsableTypeKind.ByteArray;
                    return true;
                }

                if (type is not INamedTypeSymbol namedType)
                {
                    typeKind = StringParsableTypeKind.None;
                    return false;
                }

                if (IsEnum(namedType))
                {
                    typeKind = StringParsableTypeKind.Enum;
                    return true;
                }

                SpecialType specialType = namedType.SpecialType;

                switch (specialType)
                {
                    case SpecialType.System_String:
                    case SpecialType.System_Object:
                        {
                            typeKind = StringParsableTypeKind.AssignFromSectionValue;
                            return true;
                        }
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                        {
                            typeKind = StringParsableTypeKind.Parse;
                            return true;
                        }
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        {
                            typeKind = StringParsableTypeKind.Float;
                            return true;
                        }
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_SByte:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        {
                            typeKind = StringParsableTypeKind.Integer;
                            return true;
                        }
                    case SpecialType.System_DateTime:
                        {
                            typeKind = StringParsableTypeKind.ParseInvariant;
                            return true;
                        }
                    case SpecialType.None:
                        {
                            if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.CultureInfo))
                            {
                                typeKind = StringParsableTypeKind.CultureInfo;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.DateTimeOffset) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.DateOnly) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.TimeOnly) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.TimeSpan))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.Int128) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.Half) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.UInt128))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.Uri))
                            {
                                typeKind = StringParsableTypeKind.Uri;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.Version) ||
                                SymbolEqualityComparer.Default.Equals(type, _typeSymbols.Guid))
                            {
                                typeKind = StringParsableTypeKind.Parse;
                            }
                            else
                            {
                                typeKind = StringParsableTypeKind.None;
                                return false;
                            }

                            return true;
                        }
                    default:
                        {
                            typeKind = StringParsableTypeKind.None;
                            return false;
                        }
                }
            }

            private bool TryGetTypeSpec(ITypeSymbol type, DiagnosticDescriptor descriptor, out TypeSpec? spec)
            {
                spec = GetOrCreateTypeSpec(type);

                if (spec is null)
                {
                    RegisterUnsupportedType(type, descriptor);
                    return false;
                }

                return true;
            }

            private EnumerableSpec? CreateArraySpec(IArrayTypeSymbol arrayType)
            {
                if (!TryGetTypeSpec(arrayType.ElementType, Diagnostics.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                // We want a BindCore method for List<TElement> as a temp holder for the array values. We know the element type is supported.
                EnumerableSpec listSpec = (GetOrCreateTypeSpec(_typeSymbols.List.Construct(arrayType.ElementType)) as EnumerableSpec)!;
                RegisterTypeForBindCoreGen(listSpec);

                EnumerableSpec spec = new EnumerableSpec(arrayType)
                {
                    ElementType = elementSpec,
                    ConcreteType = listSpec,
                    InitializationStrategy = InitializationStrategy.Array,
                    PopulationStrategy = CollectionPopulationStrategy.Cast_Then_Add, // Using the concrete list type as a temp holder.
                    ToEnumerableMethodCall = null,
                };

                Debug.Assert(spec.CanInitialize);
                RegisterTypeForBindCoreGen(spec);

                return spec;
            }

            private bool IsSupportedArrayType(ITypeSymbol type)
            {
                if (type is not IArrayTypeSymbol arrayType)
                {
                    return false;
                }

                if (arrayType.Rank > 1)
                {
                    RegisterUnsupportedType(arrayType, Diagnostics.MultiDimArraysNotSupported);
                    return false;
                }

                return true;
            }

            private CollectionSpec? CreateCollectionSpec(INamedTypeSymbol type)
            {
                CollectionSpec? spec;
                if (IsCandidateDictionary(type, out ITypeSymbol keyType, out ITypeSymbol elementType))
                {
                    spec = CreateDictionarySpec(type, keyType, elementType);
                    Debug.Assert(spec is null or DictionarySpec { KeyType: null or ParsableFromStringSpec });
                }
                else
                {
                    spec = CreateEnumerableSpec(type);
                }

                if (spec is not null)
                {
                    RegisterTypeForBindCoreGen(spec);
                    spec.InitExceptionMessage ??= spec.ElementType.InitExceptionMessage;
                }

                return spec;
            }

            private DictionarySpec CreateDictionarySpec(INamedTypeSymbol type, ITypeSymbol keyType, ITypeSymbol elementType)
            {
                if (!TryGetTypeSpec(keyType, Diagnostics.DictionaryKeyNotSupported, out TypeSpec keySpec) ||
                    !TryGetTypeSpec(elementType, Diagnostics.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                if (keySpec.SpecKind != TypeSpecKind.ParsableFromString)
                {
                    RegisterUnsupportedType(type, Diagnostics.DictionaryKeyNotSupported);
                    return null;
                }

                InitializationStrategy constructionStrategy;
                CollectionPopulationStrategy populationStrategy;
                INamedTypeSymbol? concreteType = null;
                INamedTypeSymbol? populationCastType = null;
                string? toEnumerableMethodCall = null;

                if (HasPublicParameterLessCtor(type))
                {
                    constructionStrategy = InitializationStrategy.ParameterlessConstructor;

                    if (HasAddMethod(type, keyType, elementType))
                    {
                        populationStrategy = CollectionPopulationStrategy.Add;
                    }
                    else if (GetInterface(type, _typeSymbols.GenericIDictionary_Unbound) is not null)
                    {
                        populationCastType = _typeSymbols.GenericIDictionary;
                        populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                    }
                    else
                    {
                        RegisterUnsupportedType(type, Diagnostics.CollectionNotSupported);
                        return null;
                    }
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericIDictionary_Unbound) || IsInterfaceMatch(type, _typeSymbols.IDictionary))
                {
                    concreteType = _typeSymbols.Dictionary;
                    constructionStrategy = InitializationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlyDictionary_Unbound))
                {
                    concreteType = _typeSymbols.Dictionary;
                    populationCastType = _typeSymbols.GenericIDictionary;
                    constructionStrategy = InitializationStrategy.ToEnumerableMethod;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                    toEnumerableMethodCall = "ToDictionary(pair => pair.Key, pair => pair.Value)";
                    _sourceGenSpec.Namespaces.Add("System.Linq");
                }
                else
                {
                    RegisterUnsupportedType(type, Diagnostics.CollectionNotSupported);
                    return null;
                }

                DictionarySpec spec = new(type)
                {
                    KeyType = (ParsableFromStringSpec)keySpec,
                    ElementType = elementSpec,
                    InitializationStrategy = constructionStrategy,
                    PopulationStrategy = populationStrategy,
                    ToEnumerableMethodCall = toEnumerableMethodCall,
                };

                Debug.Assert(!(populationStrategy is CollectionPopulationStrategy.Cast_Then_Add && populationCastType is null));
                spec.ConcreteType = ConstructGenericCollectionSpecIfRequired(concreteType, keyType, elementType);
                spec.PopulationCastType = ConstructGenericCollectionSpecIfRequired(populationCastType, keyType, elementType);

                return spec;
            }

            private EnumerableSpec? CreateEnumerableSpec(INamedTypeSymbol type)
            {
                if (!TryGetElementType(type, out ITypeSymbol? elementType) ||
                    !TryGetTypeSpec(elementType, Diagnostics.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                InitializationStrategy constructionStrategy;
                CollectionPopulationStrategy populationStrategy;
                INamedTypeSymbol? concreteType = null;
                INamedTypeSymbol? populationCastType = null;

                if (HasPublicParameterLessCtor(type))
                {
                    constructionStrategy = InitializationStrategy.ParameterlessConstructor;

                    if (HasAddMethod(type, elementType))
                    {
                        populationStrategy = CollectionPopulationStrategy.Add;
                    }
                    else if (GetInterface(type, _typeSymbols.GenericICollection_Unbound) is not null)
                    {
                        populationCastType = _typeSymbols.GenericICollection;
                        populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                    }
                    else
                    {
                        RegisterUnsupportedType(type, Diagnostics.CollectionNotSupported);
                        return null;
                    }
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericICollection_Unbound) ||
                    IsInterfaceMatch(type, _typeSymbols.GenericIList_Unbound))
                {
                    concreteType = _typeSymbols.List;
                    constructionStrategy = InitializationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericIEnumerable_Unbound))
                {
                    concreteType = _typeSymbols.List;
                    populationCastType = _typeSymbols.GenericICollection;
                    constructionStrategy = InitializationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.ISet_Unbound))
                {
                    concreteType = _typeSymbols.HashSet;
                    constructionStrategy = InitializationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlySet_Unbound))
                {
                    concreteType = _typeSymbols.HashSet;
                    populationCastType = _typeSymbols.ISet;
                    constructionStrategy = InitializationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlyList_Unbound) || IsInterfaceMatch(type, _typeSymbols.IReadOnlyCollection_Unbound))
                {
                    concreteType = _typeSymbols.List;
                    populationCastType = _typeSymbols.GenericICollection;
                    constructionStrategy = InitializationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else
                {
                    RegisterUnsupportedType(type, Diagnostics.CollectionNotSupported);
                    return null;
                }

                Register_AsConfigWithChildren_HelperForGen_IfRequired(elementSpec);

                EnumerableSpec spec = new(type)
                {
                    ElementType = elementSpec,
                    InitializationStrategy = constructionStrategy,
                    PopulationStrategy = populationStrategy,
                    ToEnumerableMethodCall = null,
                };

                Debug.Assert(!(populationStrategy is CollectionPopulationStrategy.Cast_Then_Add && populationCastType is null));
                spec.ConcreteType = ConstructGenericCollectionSpecIfRequired(concreteType, elementType);
                spec.PopulationCastType = ConstructGenericCollectionSpecIfRequired(populationCastType, elementType);

                return spec;
            }

            private ObjectSpec? CreateObjectSpec(INamedTypeSymbol type)
            {
                // Add spec to cache before traversing properties to avoid stack overflow.
                ObjectSpec objectSpec = new(type);
                _createdSpecs.Add(type, objectSpec);

                string typeName = objectSpec.Name;
                IMethodSymbol? ctor = null;
                DiagnosticDescriptor? diagnosticDescriptor = null;

                if (!(type.IsAbstract || type.TypeKind is TypeKind.Interface))
                {
                    IMethodSymbol? parameterlessCtor = null;
                    IMethodSymbol? parameterizedCtor = null;
                    bool hasMultipleParameterizedCtors = false;

                    foreach (IMethodSymbol candidate in type.InstanceConstructors)
                    {
                        if (candidate.DeclaredAccessibility is not Accessibility.Public)
                        {
                            continue;
                        }

                        if (candidate.Parameters.Length is 0)
                        {
                            parameterlessCtor = candidate;
                        }
                        else if (parameterizedCtor is not null)
                        {
                            hasMultipleParameterizedCtors = true;
                        }
                        else
                        {
                            parameterizedCtor = candidate;
                        }
                    }

                    bool hasPublicParameterlessCtor = type.IsValueType || parameterlessCtor is not null;
                    if (!hasPublicParameterlessCtor && hasMultipleParameterizedCtors)
                    {
                        diagnosticDescriptor = Diagnostics.MultipleParameterizedConstructors;
                        objectSpec.InitExceptionMessage = string.Format(Emitter.ExceptionMessages.MultipleParameterizedConstructors, typeName);
                    }

                    ctor = type.IsValueType
                        // Roslyn ctor fetching APIs include paramerterless ctors for structs, unlike System.Reflection.
                        ? parameterizedCtor ?? parameterlessCtor
                        : parameterlessCtor ?? parameterizedCtor;
                }

                objectSpec.InitializationStrategy = ctor?.Parameters.Length is 0 ? InitializationStrategy.ParameterlessConstructor : InitializationStrategy.ParameterizedConstructor;

                if (ctor is null)
                {
                    diagnosticDescriptor = Diagnostics.MissingPublicInstanceConstructor;
                    objectSpec.InitExceptionMessage = string.Format(Emitter.ExceptionMessages.MissingPublicInstanceConstructor, typeName);
                }

                if (diagnosticDescriptor is not null)
                {
                    Debug.Assert(objectSpec.InitExceptionMessage is not null);
                    RegisterUnsupportedType(type, diagnosticDescriptor);
                    return objectSpec;
                }

                INamedTypeSymbol current = type;
                while (current is not null)
                {
                    ImmutableArray<ISymbol> members = current.GetMembers();
                    foreach (ISymbol member in members)
                    {
                        if (member is IPropertySymbol { IsIndexer: false, IsImplicitlyDeclared: false } property)
                        {
                            string propertyName = property.Name;
                            TypeSpec? propertyTypeSpec = GetOrCreateTypeSpec(property.Type);

                            if (propertyTypeSpec?.CanInitialize is not true)
                            {
                                InvocationDiagnosticInfo propertyDiagnostic = new InvocationDiagnosticInfo(Diagnostics.PropertyNotSupported, new string[] { propertyName, type.ToDisplayString() });
                                RegisterTypeDiagnostic(causingType: type, propertyDiagnostic);
                                _invocationTargetTypeDiags.Add(propertyDiagnostic);
                            }

                            if (propertyTypeSpec is not null)
                            {
                                AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _typeSymbols.ConfigurationKeyNameAttribute));
                                string configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                                PropertySpec spec = new(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };
                                objectSpec.Properties[propertyName] = spec;
                                Register_AsConfigWithChildren_HelperForGen_IfRequired(propertyTypeSpec);
                            }
                        }
                    }
                    current = current.BaseType;
                }

                if (objectSpec.InitializationStrategy is InitializationStrategy.ParameterizedConstructor)
                {
                    List<string> missingParameters = new();
                    List<string> invalidParameters = new();

                    foreach (IParameterSymbol parameter in ctor.Parameters)
                    {
                        string parameterName = parameter.Name;

                        if (!objectSpec.Properties.TryGetValue(parameterName, out PropertySpec? propertySpec))
                        {
                            missingParameters.Add(parameterName);
                        }
                        else if (parameter.RefKind is not RefKind.None)
                        {
                            invalidParameters.Add(parameterName);
                        }
                        else
                        {
                            ParameterSpec paramSpec = new ParameterSpec(parameter)
                            {
                                Type = propertySpec.Type,
                                ConfigurationKeyName = propertySpec.ConfigurationKeyName,
                            };

                            propertySpec.MatchingCtorParam = paramSpec;
                            objectSpec.ConstructorParameters.Add(paramSpec);
                        }
                    }

                    if (invalidParameters.Count > 0)
                    {
                        objectSpec.InitExceptionMessage = string.Format(Emitter.ExceptionMessages.CannotBindToConstructorParameter, typeName, FormatParams(invalidParameters));
                    }
                    else if (missingParameters.Count > 0)
                    {
                        if (type.IsValueType)
                        {
                            objectSpec.InitializationStrategy = InitializationStrategy.ParameterlessConstructor;
                        }
                        else
                        {
                            objectSpec.InitExceptionMessage = string.Format(Emitter.ExceptionMessages.ConstructorParametersDoNotMatchProperties, typeName, FormatParams(missingParameters));
                        }
                    }

                    if (objectSpec.CanInitialize)
                    {
                        RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.Initialize, objectSpec);
                    }

                    static string FormatParams(List<string> names) => string.Join(",", names);
                }

                Debug.Assert((objectSpec.CanInitialize && objectSpec.InitExceptionMessage is null) ||
                    (!objectSpec.CanInitialize && objectSpec.InitExceptionMessage is not null));

                if (objectSpec.NeedsMemberBinding)
                {
                    RegisterTypeForBindCoreGen(objectSpec);
                }

                return objectSpec;
            }

            private void Register_AsConfigWithChildren_HelperForGen_IfRequired(TypeSpec type)
            {
                if (type.SpecKind is TypeSpecKind.Object or
                                        TypeSpecKind.Enumerable or
                                        TypeSpecKind.Dictionary)
                {

                    _sourceGenSpec.MethodsToGen_CoreBindingHelper |= MethodsToGen_CoreBindingHelper.AsConfigWithChildren;
                }
            }

            private bool TryGetElementType(INamedTypeSymbol type, out ITypeSymbol? elementType)
            {
                INamedTypeSymbol? candidate = GetInterface(type, _typeSymbols.GenericIEnumerable_Unbound);

                if (candidate is not null)
                {
                    elementType = candidate.TypeArguments[0];
                    return true;
                }

                elementType = null;
                return false;
            }

            private bool IsCandidateDictionary(INamedTypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? elementType)
            {
                INamedTypeSymbol? candidate = GetInterface(type, _typeSymbols.GenericIDictionary_Unbound) ?? GetInterface(type, _typeSymbols.IReadOnlyDictionary_Unbound);

                if (candidate is not null)
                {
                    keyType = candidate.TypeArguments[0];
                    elementType = candidate.TypeArguments[1];
                    return true;
                }

                if (IsInterfaceMatch(type, _typeSymbols.IDictionary))
                {
                    keyType = _typeSymbols.String;
                    elementType = _typeSymbols.String;
                    return true;
                }

                keyType = null;
                elementType = null;
                return false;
            }

            private bool IsCollection(ITypeSymbol type) =>
                type is INamedTypeSymbol namedType && GetInterface(namedType, _typeSymbols.IEnumerable) is not null;

            private static INamedTypeSymbol? GetInterface(INamedTypeSymbol type, INamedTypeSymbol @interface)
            {
                if (IsInterfaceMatch(type, @interface))
                {
                    return type;
                }

                if (@interface.IsGenericType)
                {
                    return type.AllInterfaces.FirstOrDefault(candidate =>
                        candidate.IsGenericType &&
                        candidate.ConstructUnboundGenericType() is INamedTypeSymbol unbound
                        && SymbolEqualityComparer.Default.Equals(unbound, @interface));
                }

                return type.AllInterfaces.FirstOrDefault(candidate => SymbolEqualityComparer.Default.Equals(candidate, @interface));
            }

            private static bool IsInterfaceMatch(INamedTypeSymbol type, INamedTypeSymbol @interface)
            {
                if (type.IsGenericType)
                {
                    INamedTypeSymbol unbound = type.ConstructUnboundGenericType();
                    return SymbolEqualityComparer.Default.Equals(unbound, @interface);
                }

                return SymbolEqualityComparer.Default.Equals(type, @interface);
            }

            private static bool ContainsGenericParameters(ITypeSymbol type)
            {
                if (type is not INamedTypeSymbol { IsGenericType: true } genericType)
                {
                    return false;
                }

                foreach (ITypeSymbol typeArg in genericType.TypeArguments)
                {
                    if (typeArg.TypeKind is TypeKind.TypeParameter or TypeKind.Error ||
                        ContainsGenericParameters(typeArg))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasPublicParameterLessCtor(INamedTypeSymbol type) =>
                type.InstanceConstructors.SingleOrDefault(ctor => ctor.DeclaredAccessibility is Accessibility.Public && ctor.Parameters.Length is 0) is not null;

            private static bool HasAddMethod(INamedTypeSymbol type, ITypeSymbol element)
            {
                INamedTypeSymbol current = type;
                while (current != null)
                {
                    if (current.GetMembers("Add").Any(member =>
                        member is IMethodSymbol { Parameters.Length: 1 } method &&
                        SymbolEqualityComparer.Default.Equals(element, method.Parameters[0].Type)))
                    {
                        return true;
                    }
                    current = current.BaseType;
                }
                return false;
            }

            private static bool HasAddMethod(INamedTypeSymbol type, ITypeSymbol key, ITypeSymbol element)
            {
                INamedTypeSymbol current = type;
                while (current != null)
                {
                    if (current.GetMembers("Add").Any(member =>
                        member is IMethodSymbol { Parameters.Length: 2 } method &&
                        SymbolEqualityComparer.Default.Equals(key, method.Parameters[0].Type) &&
                        SymbolEqualityComparer.Default.Equals(element, method.Parameters[1].Type)))
                    {
                        return true;
                    }
                    current = current.BaseType;
                }
                return false;
            }

            private static bool IsEnum(ITypeSymbol type) => type is INamedTypeSymbol { EnumUnderlyingType: INamedTypeSymbol { } };

            private CollectionSpec? ConstructGenericCollectionSpecIfRequired(INamedTypeSymbol? collectionType, params ITypeSymbol[] parameters) =>
                (collectionType is not null ? ConstructGenericCollectionSpec(collectionType, parameters) : null);

            private CollectionSpec? ConstructGenericCollectionSpec(INamedTypeSymbol type, params ITypeSymbol[] parameters)
            {
                Debug.Assert(type.IsGenericType);
                INamedTypeSymbol constructedType = type.Construct(parameters);
                return CreateCollectionSpec(constructedType);
            }

            private void RegisterUnsupportedType(ITypeSymbol type, DiagnosticDescriptor descriptor = null)
            {
                InvocationDiagnosticInfo diagInfo = new(descriptor, new string[] { type.ToDisplayString() });

                if (!_unsupportedTypes.Contains(type))
                {
                    RegisterTypeDiagnostic(type, diagInfo);
                    _unsupportedTypes.Add(type);
                }

                _invocationTargetTypeDiags.Add(diagInfo);
            }

            private void RegisterTypeDiagnostic(ITypeSymbol causingType, InvocationDiagnosticInfo info)
            {
                bool typeHadDiags = _typeDiagnostics.TryGetValue(causingType, out HashSet<InvocationDiagnosticInfo>? typeDiags);
                typeDiags ??= new HashSet<InvocationDiagnosticInfo>();
                typeDiags.Add(info);

                if (!typeHadDiags)
                {
                    _typeDiagnostics[causingType] = typeDiags;
                }
            }
        }
    }

    public static class ParserExtensions
    {
        public static void RegisterCacheEntry<TKey, TValue, TEntry>(this Dictionary<TKey, TValue> cache, TKey key, TEntry entry)
            where TKey : notnull
            where TValue : ICollection<TEntry>, new()
        {
            if (!cache.TryGetValue(key, out TValue? entryCollection))
            {
                cache[key] = entryCollection = new TValue();
            }

            entryCollection.Add(entry);
        }
    }
}
