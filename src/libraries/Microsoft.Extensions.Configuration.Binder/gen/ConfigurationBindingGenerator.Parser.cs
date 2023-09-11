// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        internal sealed partial class Parser
        {
            private readonly struct InvocationDiagnosticInfo
            {
                public InvocationDiagnosticInfo(DiagnosticDescriptor descriptor, object[]? messageArgs) =>
                    (Descriptor, MessageArgs) = (descriptor, messageArgs);

                public DiagnosticDescriptor Descriptor { get; }
                public object[]? MessageArgs { get; }
            }

            private readonly KnownTypeSymbols _typeSymbols;
            private readonly bool _langVersionIsSupported;

            private readonly Dictionary<ITypeSymbol, TypeSpec?> _createdSpecs = new(SymbolEqualityComparer.Default);
            private readonly HashSet<ITypeSymbol> _unsupportedTypes = new(SymbolEqualityComparer.Default);

            // Data for incremental source generation spec.
            private readonly BindingHelperInfo.Builder _helperInfoBuilder = new();
            private readonly InterceptorInfo.Builder _interceptorInfoBuilder = new();

            private readonly Dictionary<ITypeSymbol, HashSet<InvocationDiagnosticInfo>> _typeDiagnostics = new(SymbolEqualityComparer.Default);
            private readonly List<InvocationDiagnosticInfo> _invocationTargetTypeDiags = new();

            public Parser(CompilationData compilationData)
            {
                _typeSymbols = compilationData.TypeSymbols!;
                _langVersionIsSupported = compilationData.LanguageVersionIsSupported;
            }

            public List<DiagnosticInfo>? Diagnostics { get; private set; }

            public SourceGenerationSpec? GetSourceGenerationSpec(ImmutableArray<BinderInvocation?> invocations)
            {
                if (!_langVersionIsSupported)
                {
                    ReportDiagnostic(DiagnosticDescriptors.LanguageVersionNotSupported, location: Location.None);
                    return null;
                }

                if (_typeSymbols is not { IConfiguration: { }, ConfigurationBinder: { } })
                {
                    return null;
                }

                foreach (BinderInvocation? invocation in invocations)
                {
                    Debug.Assert(invocation is not null);
                    IMethodSymbol targetMethod = invocation.Operation.TargetMethod;
                    INamedTypeSymbol? candidateBinderType = targetMethod.ContainingType;
                    Debug.Assert(targetMethod.IsExtensionMethod);

                    if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.ConfigurationBinder))
                    {
                        ParseInvocation_ConfigurationBinder(invocation);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.OptionsBuilderConfigurationExtensions))
                    {
                        ParseInvocation_OptionsBuilderExt(invocation);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(candidateBinderType, _typeSymbols.OptionsConfigurationServiceCollectionExtensions))
                    {
                        ParseInvocation_ServiceCollectionExt(invocation);
                    }
                }

                return new SourceGenerationSpec
                {
                    InterceptorInfo = _interceptorInfoBuilder.ToIncrementalValue(),
                    BindingHelperInfo = _helperInfoBuilder.ToIncrementalValue()
                };
            }

            private bool IsValidRootConfigType([NotNullWhen(true)] ITypeSymbol? type)
            {
                if (type is null ||
                    type.SpecialType is SpecialType.System_Object or SpecialType.System_Void ||
                    !_typeSymbols.Compilation.IsSymbolAccessibleWithin(type, _typeSymbols.Compilation.Assembly) ||
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
                    ReportDiagnostic(DiagnosticDescriptors.CouldNotDetermineTypeInfo, invocationLocation);
                    return null;
                }

                return GetTargetTypeForRootInvocationCore(type, invocationLocation);
            }

            public TypeSpec? GetTargetTypeForRootInvocationCore(ITypeSymbol type, Location? invocationLocation)
            {
                TypeSpec? spec = GetOrCreateTypeSpec(type);

                foreach (InvocationDiagnosticInfo diag in _invocationTargetTypeDiags)
                {
                    ReportDiagnostic(diag.Descriptor, invocationLocation, diag.MessageArgs);
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
                    spec = MemberTypeIsBindable(type, underlyingType, DiagnosticDescriptors.NullableUnderlyingTypeNotSupported, out TypeSpec? underlyingTypeSpec)
                        ? new NullableSpec(type, underlyingTypeSpec)
                        : null;
                }
                else if (IsParsableFromString(type, out StringParsableTypeKind specialTypeKind))
                {
                    ParsableFromStringSpec stringParsableSpec = new(type) { StringParsableTypeKind = specialTypeKind };
                    _helperInfoBuilder.RegisterStringParsableType(stringParsableSpec);
                    spec = stringParsableSpec;
                }
                else if (IsSupportedArrayType(type))
                {
                    spec = CreateArraySpec((IArrayTypeSymbol)type);
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
                    _helperInfoBuilder.Namespaces.Add("System.Collections.Generic");

                    spec = CreateObjectSpec(namedType);
                }
                else
                {
                    RegisterUnsupportedType(type, DiagnosticDescriptors.TypeNotSupported);
                }

                foreach (InvocationDiagnosticInfo diag in _invocationTargetTypeDiags)
                {
                    RecordTypeDiagnostic(type, diag);
                }

                if (spec is { Namespace: string @namespace } && @namespace is not "<global namespace>")
                {
                    _helperInfoBuilder.Namespaces.Add(@namespace);
                }

                return _createdSpecs[type] = spec;
            }

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

            private EnumerableSpec? CreateArraySpec(IArrayTypeSymbol arrayTypeSymbol)
            {
                if (_typeSymbols.List is not INamedTypeSymbol listTypeSymbol)
                {
                    return null;
                }

                ITypeSymbol elementTypeSymbol = arrayTypeSymbol.ElementType;

                if (!MemberTypeIsBindable(arrayTypeSymbol, elementTypeSymbol, DiagnosticDescriptors.ElementTypeNotSupported, out TypeSpec? elementTypeSpec))
                {
                    return null;
                }

                // We want a BindCore method for List<TElement> as a temp holder for the array values.
                // Since the element type is supported, we can certainly a list of elements.
                EnumerableSpec listTypeSpec = (EnumerableSpec)GetOrCreateTypeSpec(listTypeSymbol.Construct(elementTypeSymbol))!;

                EnumerableSpec spec = new EnumerableSpec(arrayTypeSymbol)
                {
                    ElementType = elementTypeSpec,
                    InstantiationStrategy = InstantiationStrategy.Array,
                    PopulationStrategy = CollectionPopulationStrategy.Cast_Then_Add, // Using the concrete list type as a temp holder.
                    TypeToInstantiate = listTypeSpec,
                    PopulationCastType = null,
                };

                bool registeredForBindCore = _helperInfoBuilder.TryRegisterTypeForBindCoreGen(listTypeSpec) &&
                    _helperInfoBuilder.TryRegisterTypeForBindCoreGen(spec);
                Debug.Assert(registeredForBindCore);

                return spec;
            }

            private CollectionSpec? CreateCollectionSpec(INamedTypeSymbol type)
            {
                CollectionSpec? spec;
                if (IsCandidateDictionary(type, out ITypeSymbol? keyType, out ITypeSymbol? elementType))
                {
                    spec = CreateDictionarySpec(type, keyType, elementType);
                    Debug.Assert(spec is null or DictionarySpec { KeyType: null or ParsableFromStringSpec });
                }
                else
                {
                    spec = CreateEnumerableSpec(type);
                }

                if (spec is null)
                {
                    return null;
                }

                bool registerForBindCoreGen = _helperInfoBuilder.TryRegisterTypeForBindCoreGen(spec);
                Debug.Assert(registerForBindCoreGen);
                return spec;
            }

            private DictionarySpec? CreateDictionarySpec(INamedTypeSymbol type, ITypeSymbol keyType, ITypeSymbol elementType)
            {
                if (!MemberTypeIsBindable(type, keyType, DiagnosticDescriptors.DictionaryKeyNotSupported, out TypeSpec? keySpec) ||
                    !MemberTypeIsBindable(type, elementType, DiagnosticDescriptors.ElementTypeNotSupported, out TypeSpec? elementSpec))
                {
                    return null;
                }

                if (keySpec.SpecKind is not TypeSpecKind.ParsableFromString)
                {
                    RegisterUnsupportedType(type, DiagnosticDescriptors.DictionaryKeyNotSupported);
                    return null;
                }

                InstantiationStrategy constructionStrategy;
                CollectionPopulationStrategy populationStrategy;
                INamedTypeSymbol? typeToInstantiate = null;
                INamedTypeSymbol? populationCastType = null;

                if (HasPublicParameterLessCtor(type))
                {
                    constructionStrategy = InstantiationStrategy.ParameterlessConstructor;

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
                        RegisterUnsupportedType(type, DiagnosticDescriptors.CollectionNotSupported);
                        return null;
                    }
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericIDictionary_Unbound) || IsInterfaceMatch(type, _typeSymbols.IDictionary))
                {
                    typeToInstantiate = _typeSymbols.Dictionary;
                    constructionStrategy = InstantiationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlyDictionary_Unbound))
                {
                    typeToInstantiate = _typeSymbols.Dictionary;
                    populationCastType = _typeSymbols.GenericIDictionary;
                    constructionStrategy = InstantiationStrategy.ToEnumerableMethod;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                    _helperInfoBuilder.Namespaces.Add("System.Linq");
                }
                else
                {
                    RegisterUnsupportedType(type, DiagnosticDescriptors.CollectionNotSupported);
                    return null;
                }

                Debug.Assert(!(populationStrategy is CollectionPopulationStrategy.Cast_Then_Add && populationCastType is null));

                DictionarySpec spec = new(type)
                {
                    KeyType = (ParsableFromStringSpec)keySpec,
                    ElementType = elementSpec,
                    InstantiationStrategy = constructionStrategy,
                    PopulationStrategy = populationStrategy,
                    TypeToInstantiate = ConstructGenericCollectionSpecIfRequired(typeToInstantiate, keyType, elementType) as DictionarySpec,
                    PopulationCastType = ConstructGenericCollectionSpecIfRequired(populationCastType, keyType, elementType) as DictionarySpec,
                };

                return spec;
            }

            private EnumerableSpec? CreateEnumerableSpec(INamedTypeSymbol type)
            {
                if (!TryGetElementType(type, out ITypeSymbol? elementType) ||
                    !MemberTypeIsBindable(type, elementType, DiagnosticDescriptors.ElementTypeNotSupported, out TypeSpec? elementSpec))
                {
                    return null;
                }

                InstantiationStrategy instantiationStrategy;
                CollectionPopulationStrategy populationStrategy;
                INamedTypeSymbol? typeToInstantiate = null;
                INamedTypeSymbol? populationCastType = null;

                if (HasPublicParameterLessCtor(type))
                {
                    instantiationStrategy = InstantiationStrategy.ParameterlessConstructor;

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
                        RegisterUnsupportedType(type, DiagnosticDescriptors.CollectionNotSupported);
                        return null;
                    }
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericICollection_Unbound) ||
                    IsInterfaceMatch(type, _typeSymbols.GenericIList_Unbound))
                {
                    typeToInstantiate = _typeSymbols.List;
                    instantiationStrategy = InstantiationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericIEnumerable_Unbound))
                {
                    typeToInstantiate = _typeSymbols.List;
                    populationCastType = _typeSymbols.GenericICollection;
                    instantiationStrategy = InstantiationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.ISet_Unbound))
                {
                    typeToInstantiate = _typeSymbols.HashSet;
                    instantiationStrategy = InstantiationStrategy.ParameterlessConstructor;
                    populationStrategy = CollectionPopulationStrategy.Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlySet_Unbound))
                {
                    typeToInstantiate = _typeSymbols.HashSet;
                    populationCastType = _typeSymbols.ISet;
                    instantiationStrategy = InstantiationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlyList_Unbound) || IsInterfaceMatch(type, _typeSymbols.IReadOnlyCollection_Unbound))
                {
                    typeToInstantiate = _typeSymbols.List;
                    populationCastType = _typeSymbols.GenericICollection;
                    instantiationStrategy = InstantiationStrategy.ParameterizedConstructor;
                    populationStrategy = CollectionPopulationStrategy.Cast_Then_Add;
                }
                else
                {
                    RegisterUnsupportedType(type, DiagnosticDescriptors.CollectionNotSupported);
                    return null;
                }

                Debug.Assert(!(populationStrategy is CollectionPopulationStrategy.Cast_Then_Add && populationCastType is null));

                EnumerableSpec spec = new(type)
                {
                    ElementType = elementSpec,
                    InstantiationStrategy = instantiationStrategy,
                    PopulationStrategy = populationStrategy,
                    TypeToInstantiate = ConstructGenericCollectionSpecIfRequired(typeToInstantiate, elementType) as EnumerableSpec,
                    PopulationCastType = ConstructGenericCollectionSpecIfRequired(populationCastType, elementType) as EnumerableSpec,
                };

                return spec;
            }

            private ObjectSpec? CreateObjectSpec(INamedTypeSymbol typeSymbol)
            {
                // Add spec to cache before traversing properties to avoid stack overflow.
                _createdSpecs.Add(typeSymbol, null);

                string typeName = typeSymbol.GetTypeName().Name;
                InstantiationStrategy initStrategy = InstantiationStrategy.None;
                DiagnosticDescriptor? initDiagDescriptor = null;
                string? initExceptionMessage = null;

                IMethodSymbol? ctor = null;

                if (!(typeSymbol.IsAbstract || typeSymbol.TypeKind is TypeKind.Interface))
                {
                    IMethodSymbol? parameterlessCtor = null;
                    IMethodSymbol? parameterizedCtor = null;
                    bool hasMultipleParameterizedCtors = false;

                    foreach (IMethodSymbol candidate in typeSymbol.InstanceConstructors)
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

                    bool hasPublicParameterlessCtor = typeSymbol.IsValueType || parameterlessCtor is not null;
                    if (!hasPublicParameterlessCtor && hasMultipleParameterizedCtors)
                    {
                        initDiagDescriptor = DiagnosticDescriptors.MultipleParameterizedConstructors;
                        initExceptionMessage = string.Format(Emitter.ExceptionMessages.MultipleParameterizedConstructors, typeName);
                    }

                    ctor = typeSymbol.IsValueType
                        // Roslyn ctor fetching APIs include paramerterless ctors for structs, unlike System.Reflection.
                        ? parameterizedCtor ?? parameterlessCtor
                        : parameterlessCtor ?? parameterizedCtor;
                }

                if (ctor is null)
                {
                    initDiagDescriptor = DiagnosticDescriptors.MissingPublicInstanceConstructor;
                    initExceptionMessage = string.Format(Emitter.ExceptionMessages.MissingPublicInstanceConstructor, typeName);
                }
                else
                {
                    initStrategy = ctor.Parameters.Length is 0 ? InstantiationStrategy.ParameterlessConstructor : InstantiationStrategy.ParameterizedConstructor;
                }

                if (initDiagDescriptor is not null)
                {
                    Debug.Assert(initExceptionMessage is not null);
                    RegisterUnsupportedType(typeSymbol, initDiagDescriptor);
                }

                Dictionary<string, PropertySpec>? properties = null;

                INamedTypeSymbol? current = typeSymbol;
                while (current is not null)
                {
                    ImmutableArray<ISymbol> members = current.GetMembers();
                    foreach (ISymbol member in members)
                    {
                        if (member is IPropertySymbol { IsIndexer: false, IsImplicitlyDeclared: false } property)
                        {
                            string propertyName = property.Name;
                            TypeSpec? propertyTypeSpec = GetOrCreateTypeSpec(property.Type);

                            if (propertyTypeSpec?.CanBindTo is not true)
                            {
                                InvocationDiagnosticInfo propertyDiagnostic = new InvocationDiagnosticInfo(DiagnosticDescriptors.PropertyNotSupported, new string[] { propertyName, typeSymbol.ToDisplayString() });
                                RecordTypeDiagnostic(causingType: typeSymbol, propertyDiagnostic);
                                _invocationTargetTypeDiags.Add(propertyDiagnostic);
                            }

                            if (propertyTypeSpec is not null)
                            {
                                AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _typeSymbols.ConfigurationKeyNameAttribute));
                                string configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;
                                PropertySpec spec = new(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };

                                (properties ??= new(StringComparer.OrdinalIgnoreCase))[propertyName] = spec;
                                _helperInfoBuilder.Register_AsConfigWithChildren_HelperForGen_IfRequired(propertyTypeSpec);
                            }
                        }
                    }
                    current = current.BaseType;
                }

                List<ParameterSpec>? ctorParams = null;

                if (initStrategy is InstantiationStrategy.ParameterizedConstructor)
                {
                    Debug.Assert(ctor is not null);
                    List<string>? missingParameters = null;
                    List<string>? invalidParameters = null;

                    foreach (IParameterSymbol parameter in ctor.Parameters)
                    {
                        string parameterName = parameter.Name;

                        if (properties?.TryGetValue(parameterName, out PropertySpec? propertySpec) is not true)
                        {
                            (missingParameters ??= new()).Add(parameterName);
                        }
                        else if (parameter.RefKind is not RefKind.None)
                        {
                            (invalidParameters ??= new()).Add(parameterName);
                        }
                        else
                        {
                            ParameterSpec paramSpec = new ParameterSpec(parameter)
                            {
                                Type = propertySpec.Type,
                                ConfigurationKeyName = propertySpec.ConfigurationKeyName,
                            };

                            propertySpec.MatchingCtorParam = paramSpec;
                            (ctorParams ??= new()).Add(paramSpec);
                        }
                    }

                    if (invalidParameters?.Count > 0)
                    {
                        initExceptionMessage = string.Format(Emitter.ExceptionMessages.CannotBindToConstructorParameter, typeName, FormatParams(invalidParameters));
                    }
                    else if (missingParameters?.Count > 0)
                    {
                        if (typeSymbol.IsValueType)
                        {
                            initStrategy = InstantiationStrategy.ParameterlessConstructor;
                        }
                        else
                        {
                            initExceptionMessage = string.Format(Emitter.ExceptionMessages.ConstructorParametersDoNotMatchProperties, typeName, FormatParams(missingParameters));
                        }
                    }

                    static string FormatParams(List<string> names) => string.Join(",", names);
                }

                ObjectSpec typeSpec = new(typeSymbol)
                {
                    InstantiationStrategy = initStrategy,
                    Properties = properties?.Values.OrderBy(p => p.Name).ToImmutableEquatableArray(),
                    ConstructorParameters = ctorParams?.ToImmutableEquatableArray(),
                    InitExceptionMessage = initExceptionMessage
                };

                if (typeSpec is { InstantiationStrategy: InstantiationStrategy.ParameterizedConstructor, CanInstantiate: true })
                {
                    _helperInfoBuilder.RegisterTypeForMethodGen(MethodsToGen_CoreBindingHelper.Initialize, typeSpec);
                }

                Debug.Assert((typeSpec.CanInstantiate && initExceptionMessage is null) ||
                    (!typeSpec.CanInstantiate && initExceptionMessage is not null) ||
                    (!typeSpec.CanInstantiate && (typeSymbol.IsAbstract || typeSymbol.TypeKind is TypeKind.Interface)));

                _helperInfoBuilder.TryRegisterTypeForBindCoreGen(typeSpec);
                return typeSpec;
            }

            private bool MemberTypeIsBindable(ITypeSymbol containingTypeSymbol, ITypeSymbol memberTypeSymbol, DiagnosticDescriptor containingTypeDiagDescriptor, [NotNullWhen(true)] out TypeSpec? memberTypeSpec)
            {
                if (GetOrCreateTypeSpec(memberTypeSymbol) is TypeSpec { CanBindTo: true } spec)
                {
                    memberTypeSpec = spec;
                    return true;
                }

                RegisterUnsupportedType(containingTypeSymbol, containingTypeDiagDescriptor);
                memberTypeSpec = null;
                return false;
            }

            private bool TryGetElementType(INamedTypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? elementType)
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

            private bool IsCandidateDictionary(INamedTypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? keyType, [NotNullWhen(true)] out ITypeSymbol? elementType)
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

            private bool IsSupportedArrayType(ITypeSymbol type)
            {
                if (type is not IArrayTypeSymbol arrayType)
                {
                    return false;
                }

                if (arrayType.Rank > 1)
                {
                    RegisterUnsupportedType(arrayType, DiagnosticDescriptors.MultiDimArraysNotSupported);
                    return false;
                }

                return true;
            }

            private static INamedTypeSymbol? GetInterface(INamedTypeSymbol type, INamedTypeSymbol? @interface)
            {
                if (@interface is null)
                {
                    return null;
                }

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

            private static bool IsInterfaceMatch(INamedTypeSymbol type, INamedTypeSymbol? @interface)
            {
                if (@interface is null)
                {
                    return false;
                }

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
                INamedTypeSymbol? current = type;
                while (current is not null)
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
                INamedTypeSymbol? current = type;
                while (current is not null)
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

            private void RegisterUnsupportedType(ITypeSymbol type, DiagnosticDescriptor descriptor)
            {
                InvocationDiagnosticInfo diagInfo = new(descriptor, new string[] { type.ToDisplayString() });

                if (!_unsupportedTypes.Contains(type))
                {
                    RecordTypeDiagnostic(type, diagInfo);
                    _unsupportedTypes.Add(type);
                }

                _invocationTargetTypeDiags.Add(diagInfo);
            }

            private void RecordTypeDiagnostic(ITypeSymbol causingType, InvocationDiagnosticInfo info)
            {
                bool typeHadDiags = _typeDiagnostics.TryGetValue(causingType, out HashSet<InvocationDiagnosticInfo>? typeDiags);
                typeDiags ??= new HashSet<InvocationDiagnosticInfo>();
                typeDiags.Add(info);

                if (!typeHadDiags)
                {
                    _typeDiagnostics[causingType] = typeDiags;
                }
            }

            private void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs) =>
                (Diagnostics ??= new()).Add(new DiagnosticInfo
                {
                    Descriptor = descriptor,
                    //Location = location?.GetTrimmedLocation(),
                    Location = location,
                    MessageArgs = messageArgs ?? Array.Empty<object?>(),
                });
        }
    }
}
