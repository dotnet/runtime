// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator : IIncrementalGenerator
    {
        internal sealed partial class Parser(CompilationData compilationData)
        {
            private readonly KnownTypeSymbols _typeSymbols = compilationData.TypeSymbols!;
            private readonly bool _langVersionIsSupported = compilationData.LanguageVersionIsSupported;

            private readonly List<TypeParseInfo> _invocationTypeParseInfo = new();
            private readonly Queue<TypeParseInfo> _typesToParse = new();
            private readonly Dictionary<ITypeSymbol, TypeSpec> _createdTypeSpecs = new(SymbolEqualityComparer.Default);

            private readonly InterceptorInfo.Builder _interceptorInfoBuilder = new();
            private BindingHelperInfo.Builder? _helperInfoBuilder; // Init'ed with type index when registering interceptors, after creating type specs.
            private bool _emitEnumParseMethod;
            private bool _emitGenericParseEnum;

            public List<DiagnosticInfo>? Diagnostics { get; private set; }

            public SourceGenerationSpec? GetSourceGenerationSpec(ImmutableArray<BinderInvocation?> invocations, CancellationToken cancellationToken)
            {
                if (!_langVersionIsSupported)
                {
                    RecordDiagnostic(DiagnosticDescriptors.LanguageVersionNotSupported, trimmedLocation: Location.None);
                    return null;
                }

                if (_typeSymbols is not { IConfiguration: { }, ConfigurationBinder: { } })
                {
                    return null;
                }

                ParseInvocations(invocations);
                CreateTypeSpecs(cancellationToken);
                RegisterInterceptors();
                CheckIfToEmitParseEnumMethod();

                return new SourceGenerationSpec
                {
                    InterceptorInfo = _interceptorInfoBuilder.ToIncrementalValue(),
                    BindingHelperInfo = _helperInfoBuilder!.ToIncrementalValue(),
                    ConfigTypes = _createdTypeSpecs.Values.OrderBy(s => s.TypeRef.FullyQualifiedName).ToImmutableEquatableArray(),
                    EmitEnumParseMethod = _emitEnumParseMethod,
                    EmitGenericParseEnum = _emitGenericParseEnum,
                    EmitThrowIfNullMethod = IsThrowIfNullMethodToBeEmitted()
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

            private void ParseInvocations(ImmutableArray<BinderInvocation?> invocations)
            {
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
            }

            private void CreateTypeSpecs(CancellationToken cancellationToken)
            {
                while (_typesToParse.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    TypeParseInfo typeParseInfo = _typesToParse.Dequeue();
                    ITypeSymbol typeSymbol = typeParseInfo.TypeSymbol;

                    if (!_createdTypeSpecs.ContainsKey(typeSymbol))
                    {
                        _createdTypeSpecs.Add(typeSymbol, CreateTypeSpec(typeParseInfo));
                    }
                }
            }

            private void RegisterInterceptors()
            {
                TypeIndex typeIndex = new(_createdTypeSpecs.Values);
                _helperInfoBuilder = new(typeIndex);

                foreach (TypeParseInfo typeParseInfo in _invocationTypeParseInfo)
                {
                    TypeSpec typeSpec = _createdTypeSpecs[typeParseInfo.TypeSymbol];
                    MethodsToGen overload = typeParseInfo.BindingOverload;

                    if ((MethodsToGen.ConfigBinder_Any & overload) is not 0)
                    {
                        RegisterInterceptor_ConfigurationBinder(typeParseInfo, typeSpec);
                    }
                    else if ((MethodsToGen.OptionsBuilderExt_Any & overload) is not 0)
                    {
                        RegisterInterceptor_OptionsBuilderExt(typeParseInfo, typeSpec);
                    }
                    else
                    {
                        Debug.Assert((MethodsToGen.ServiceCollectionExt_Any & overload) is not 0);
                        RegisterInterceptor_ServiceCollectionExt(typeParseInfo, typeSpec);
                    }
                }
            }

            private void EnqueueTargetTypeForRootInvocation(ITypeSymbol? typeSymbol, MethodsToGen overload, BinderInvocation invocation)
            {
                if (!IsValidRootConfigType(typeSymbol))
                {
                    RecordDiagnostic(DiagnosticDescriptors.CouldNotDetermineTypeInfo, invocation.Location);
                }
                else
                {
                    TypeParseInfo typeParseInfo = TypeParseInfo.Create(typeSymbol, overload, invocation, containingTypeDiagInfo: null);
                    _typesToParse.Enqueue(typeParseInfo);
                    _invocationTypeParseInfo.Add(typeParseInfo);
                }
            }

            private TypeRef EnqueueTransitiveType(TypeParseInfo containingTypeParseInfo, ITypeSymbol memberTypeSymbol, DiagnosticDescriptor diagDescriptor, string? memberName = null)
            {
                TypeParseInfo memberTypeParseInfo = containingTypeParseInfo.ToTransitiveTypeParseInfo(memberTypeSymbol, diagDescriptor, memberName);

                if (_createdTypeSpecs.TryGetValue(memberTypeSymbol, out TypeSpec? memberTypeSpec))
                {
                    RecordTypeDiagnosticIfRequired(memberTypeParseInfo, memberTypeSpec);
                    return memberTypeSpec.TypeRef;
                }

                _typesToParse.Enqueue(memberTypeParseInfo);
                return new TypeRef(memberTypeSymbol);
            }

            private TypeSpec CreateTypeSpec(TypeParseInfo typeParseInfo)
            {
                ITypeSymbol type = typeParseInfo.TypeSymbol;
                TypeSpec spec;

                if (IsNullable(type, out ITypeSymbol? underlyingType))
                {
                    TypeRef underlyingTypeRef = EnqueueTransitiveType(
                        typeParseInfo,
                        underlyingType,
                        DiagnosticDescriptors.NullableUnderlyingTypeNotSupported);

                    spec = new NullableSpec(type, underlyingTypeRef);
                }
                else if (IsParsableFromString(type, out StringParsableTypeKind specialTypeKind))
                {
                    ParsableFromStringSpec stringParsableSpec = new(type) { StringParsableTypeKind = specialTypeKind };
                    spec = stringParsableSpec;
                }
                else if (type.TypeKind is TypeKind.Array)
                {
                    spec = CreateArraySpec(typeParseInfo);
                    Debug.Assert(spec is ArraySpec or UnsupportedTypeSpec);
                }
                else if (IsCollection(type))
                {
                    spec = CreateCollectionSpec(typeParseInfo);
                }
                else if (SymbolEqualityComparer.Default.Equals(type, _typeSymbols.IConfigurationSection))
                {
                    spec = new ConfigurationSectionSpec(type);
                }
                else if (type is INamedTypeSymbol)
                {
                    spec = CreateObjectSpec(typeParseInfo);
                }
                else
                {
                    spec = CreateUnsupportedTypeSpec(typeParseInfo, NotSupportedReason.UnknownType);
                }

                RecordTypeDiagnosticIfRequired(typeParseInfo, spec);

                return spec;
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

            private TypeSpec CreateArraySpec(TypeParseInfo typeParseInfo)
            {
                IArrayTypeSymbol typeSymbol = (IArrayTypeSymbol)typeParseInfo.TypeSymbol;

                if (typeSymbol.Rank > 1)
                {
                    return CreateUnsupportedTypeSpec(typeParseInfo, NotSupportedReason.MultiDimArraysNotSupported);
                }

                if (IsUnsupportedType(typeSymbol.ElementType))
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                TypeRef elementTypeRef = EnqueueTransitiveType(
                    typeParseInfo,
                    typeSymbol.ElementType,
                    DiagnosticDescriptors.ElementTypeNotSupported);

                return new ArraySpec(typeSymbol)
                {
                    ElementTypeRef = elementTypeRef,
                };
            }

            private TypeSpec CreateCollectionSpec(TypeParseInfo typeParseInfo)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)typeParseInfo.TypeSymbol;

                TypeSpec spec;
                if (IsCandidateDictionary(type, out ITypeSymbol? keyType, out ITypeSymbol? elementType))
                {
                    spec = CreateDictionarySpec(typeParseInfo, keyType, elementType);
                    Debug.Assert(spec is DictionarySpec or UnsupportedTypeSpec);
                }
                else
                {
                    spec = CreateEnumerableSpec(typeParseInfo);
                    Debug.Assert(spec is EnumerableSpec or UnsupportedTypeSpec);
                }

                return spec;
            }

            private TypeSpec CreateDictionarySpec(TypeParseInfo typeParseInfo, ITypeSymbol keyTypeSymbol, ITypeSymbol elementTypeSymbol)
            {
                if (IsUnsupportedType(keyTypeSymbol) || IsUnsupportedType(elementTypeSymbol))
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                INamedTypeSymbol type = (INamedTypeSymbol)typeParseInfo.TypeSymbol;

                CollectionInstantiationStrategy instantiationStrategy;
                CollectionInstantiationConcreteType instantiationConcreteType;
                CollectionPopulationCastType populationCastType;

                if (HasPublicParameterLessCtor(type))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.ParameterlessConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.Self;

                    if (HasAddMethod(type, keyTypeSymbol, elementTypeSymbol))
                    {
                        populationCastType = CollectionPopulationCastType.NotApplicable;
                    }
                    else if (_typeSymbols.GenericIDictionary is not null && GetInterface(type, _typeSymbols.GenericIDictionary_Unbound) is not null)
                    {
                        populationCastType = CollectionPopulationCastType.IDictionary;
                    }
                    else
                    {
                        return CreateUnsupportedCollectionSpec(typeParseInfo);
                    }
                }
                else if (_typeSymbols.Dictionary is not null &&
                    (IsInterfaceMatch(type, _typeSymbols.GenericIDictionary_Unbound) || IsInterfaceMatch(type, _typeSymbols.IDictionary)))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.ParameterlessConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.Dictionary;
                    populationCastType = CollectionPopulationCastType.NotApplicable;
                }
                else if (_typeSymbols.Dictionary is not null && IsInterfaceMatch(type, _typeSymbols.IReadOnlyDictionary_Unbound))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.LinqToDictionary;
                    instantiationConcreteType = CollectionInstantiationConcreteType.Dictionary;
                    populationCastType = CollectionPopulationCastType.IDictionary;
                }
                else
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                TypeRef keyTypeRef = EnqueueTransitiveType(typeParseInfo, keyTypeSymbol, DiagnosticDescriptors.DictionaryKeyNotSupported);
                TypeRef elementTypeRef = EnqueueTransitiveType(typeParseInfo, elementTypeSymbol, DiagnosticDescriptors.ElementTypeNotSupported);

                return new DictionarySpec(type)
                {
                    KeyTypeRef = keyTypeRef,
                    ElementTypeRef = elementTypeRef,
                    InstantiationStrategy = instantiationStrategy,
                    InstantiationConcreteType = instantiationConcreteType,
                    PopulationCastType = populationCastType,
                };
            }

            private TypeSpec CreateEnumerableSpec(TypeParseInfo typeParseInfo)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)typeParseInfo.TypeSymbol;

                if (!TryGetElementType(type, out ITypeSymbol? elementType))
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                if (IsUnsupportedType(elementType))
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                CollectionInstantiationStrategy instantiationStrategy;
                CollectionInstantiationConcreteType instantiationConcreteType;
                CollectionPopulationCastType populationCastType;

                if (HasPublicParameterLessCtor(type))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.ParameterlessConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.Self;

                    if (HasAddMethod(type, elementType))
                    {
                        populationCastType = CollectionPopulationCastType.NotApplicable;
                    }
                    else if (_typeSymbols.GenericICollection is not null && GetInterface(type, _typeSymbols.GenericICollection_Unbound) is not null)
                    {
                        populationCastType = CollectionPopulationCastType.ICollection;
                    }
                    else
                    {
                        return CreateUnsupportedCollectionSpec(typeParseInfo);
                    }
                }
                else if ((IsInterfaceMatch(type, _typeSymbols.GenericICollection_Unbound) || IsInterfaceMatch(type, _typeSymbols.GenericIList_Unbound)))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.ParameterlessConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.List;
                    populationCastType = CollectionPopulationCastType.NotApplicable;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.GenericIEnumerable_Unbound))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.CopyConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.List;
                    populationCastType = CollectionPopulationCastType.ICollection;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.ISet_Unbound))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.ParameterlessConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.HashSet;
                    populationCastType = CollectionPopulationCastType.NotApplicable;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlySet_Unbound))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.CopyConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.HashSet;
                    populationCastType = CollectionPopulationCastType.ISet;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.IReadOnlyList_Unbound) || IsInterfaceMatch(type, _typeSymbols.IReadOnlyCollection_Unbound))
                {
                    instantiationStrategy = CollectionInstantiationStrategy.CopyConstructor;
                    instantiationConcreteType = CollectionInstantiationConcreteType.List;
                    populationCastType = CollectionPopulationCastType.ICollection;
                }
                else
                {
                    return CreateUnsupportedCollectionSpec(typeParseInfo);
                }

                TypeRef elementTypeRef = EnqueueTransitiveType(typeParseInfo, elementType, DiagnosticDescriptors.ElementTypeNotSupported);

                return new EnumerableSpec(type)
                {
                    ElementTypeRef = elementTypeRef,
                    InstantiationStrategy = instantiationStrategy,
                    InstantiationConcreteType = instantiationConcreteType,
                    PopulationCastType = populationCastType,
                };
            }

            private bool IsAssignableTo(ITypeSymbol source, ITypeSymbol dest)
            {
                Conversion conversion = _typeSymbols.Compilation.ClassifyConversion(source, dest);
                return conversion.IsReference && conversion.IsImplicit;
            }

            private bool IsUnsupportedType(ITypeSymbol type)
            {
                if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    type = ((INamedTypeSymbol)type).TypeArguments[0]; // extract the T from a Nullable<T>
                }

                if (SymbolEqualityComparer.Default.Equals(_typeSymbols.IntPtr, type)  ||
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.UIntPtr, type) ||
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.SerializationInfo, type) ||
                    SymbolEqualityComparer.Default.Equals(_typeSymbols.ParameterInfo, type) ||
                    IsAssignableTo(type, _typeSymbols.MemberInfo) ||
                    IsAssignableTo(type, _typeSymbols.Delegate))
                {
                    return true;
                }

                if (type is IArrayTypeSymbol arrayTypeSymbol)
                {
                    return arrayTypeSymbol.Rank > 1 || IsUnsupportedType(arrayTypeSymbol.ElementType);
                }

                if (IsCollection(type))
                {
                    INamedTypeSymbol collectionType = (INamedTypeSymbol)type;

                    if (IsCandidateDictionary(collectionType, out ITypeSymbol? keyType, out ITypeSymbol? elementType))
                    {
                        return IsUnsupportedType(keyType) || IsUnsupportedType(elementType);
                    }
                    else if (TryGetElementType(collectionType, out elementType))
                    {
                        return IsUnsupportedType(elementType);
                    }
                }

                return false;
            }

            private bool ConstructorParametersContainUnsupportedType(IMethodSymbol ctor)
            {
                foreach (IParameterSymbol parameter in ctor.Parameters)
                {
                    if (IsUnsupportedType(parameter.Type))
                    {
                        return true;
                    }
                }

                return false;
            }

            private ObjectSpec CreateObjectSpec(TypeParseInfo typeParseInfo)
            {
                INamedTypeSymbol typeSymbol = (INamedTypeSymbol)typeParseInfo.TypeSymbol;

                ObjectInstantiationStrategy initializationStrategy = ObjectInstantiationStrategy.None;
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
                        else if (!ConstructorParametersContainUnsupportedType(candidate))
                        {
                            if (parameterizedCtor is not null)
                            {
                                hasMultipleParameterizedCtors = true;
                            }
                            else
                            {
                                parameterizedCtor = candidate;
                            }
                        }
                    }

                    bool hasPublicParameterlessCtor = typeSymbol.IsValueType || parameterlessCtor is not null;
                    if (!hasPublicParameterlessCtor && hasMultipleParameterizedCtors)
                    {
                        initDiagDescriptor = DiagnosticDescriptors.MultipleParameterizedConstructors;
                        initExceptionMessage = string.Format(Emitter.ExceptionMessages.MultipleParameterizedConstructors, typeSymbol.GetFullName());
                    }

                    ctor = typeSymbol.IsValueType
                        // Roslyn ctor fetching APIs include parameterless ctors for structs, unlike System.Reflection.
                        ? parameterizedCtor ?? parameterlessCtor
                        : parameterlessCtor ?? parameterizedCtor;
                }

                if (ctor is null)
                {
                    initDiagDescriptor = DiagnosticDescriptors.MissingPublicInstanceConstructor;
                    initExceptionMessage = string.Format(Emitter.ExceptionMessages.MissingPublicInstanceConstructor, typeSymbol.GetFullName());
                }
                else
                {
                    initializationStrategy = ctor.Parameters.Length is 0 ? ObjectInstantiationStrategy.ParameterlessConstructor : ObjectInstantiationStrategy.ParameterizedConstructor;
                }

                if (initDiagDescriptor is not null)
                {
                    Debug.Assert(initExceptionMessage is not null);
                    RecordTypeDiagnostic(typeParseInfo, initDiagDescriptor);
                }

                Dictionary<string, PropertySpec>? properties = null;

                INamedTypeSymbol? current = typeSymbol;
                while (current is not null)
                {
                    ImmutableArray<ISymbol> members = current.GetMembers();
                    foreach (ISymbol member in members)
                    {
                        if (member is IPropertySymbol { IsIndexer: false, IsImplicitlyDeclared: false } property && !IsUnsupportedType(property.Type))
                        {
                            string propertyName = property.Name;
                            TypeRef propertyTypeRef = EnqueueTransitiveType(typeParseInfo, property.Type, DiagnosticDescriptors.PropertyNotSupported, propertyName);

                            AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _typeSymbols.ConfigurationKeyNameAttribute));
                            string configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                            PropertySpec spec = new(property, propertyTypeRef)
                            {
                                ConfigurationKeyName = configKeyName
                            };

                            (properties ??= new(StringComparer.OrdinalIgnoreCase))[propertyName] = spec;
                        }
                    }
                    current = current.BaseType;
                }

                List<ParameterSpec>? ctorParams = null;

                if (initializationStrategy is ObjectInstantiationStrategy.ParameterizedConstructor)
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
                            ParameterSpec paramSpec = new ParameterSpec(parameter, propertySpec.TypeRef)
                            {
                                ConfigurationKeyName = propertySpec.ConfigurationKeyName,
                            };

                            propertySpec.MatchingCtorParam = paramSpec;
                            (ctorParams ??= new()).Add(paramSpec);
                        }
                    }

                    if (invalidParameters?.Count > 0)
                    {
                        initExceptionMessage = string.Format(Emitter.ExceptionMessages.CannotBindToConstructorParameter, typeSymbol.GetFullName(), FormatParams(invalidParameters));
                    }
                    else if (missingParameters?.Count > 0)
                    {
                        if (typeSymbol.IsValueType)
                        {
                            initializationStrategy = ObjectInstantiationStrategy.ParameterlessConstructor;
                        }
                        else
                        {
                            initExceptionMessage = string.Format(Emitter.ExceptionMessages.ConstructorParametersDoNotMatchProperties, typeSymbol.GetFullName(), FormatParams(missingParameters));
                        }
                    }

                    static string FormatParams(List<string> names) => string.Join(",", names);
                }

                return new ObjectSpec(
                    typeSymbol,
                    initializationStrategy,
                    properties: properties?.Values.ToImmutableEquatableArray(),
                    constructorParameters: ctorParams?.ToImmutableEquatableArray(),
                    initExceptionMessage);
            }

            private static UnsupportedTypeSpec CreateUnsupportedCollectionSpec(TypeParseInfo typeParseInfo)
                => CreateUnsupportedTypeSpec(typeParseInfo, NotSupportedReason.CollectionNotSupported);

            private static UnsupportedTypeSpec CreateUnsupportedTypeSpec(TypeParseInfo typeParseInfo, NotSupportedReason reason) =>
                new(typeParseInfo.TypeSymbol) { NotSupportedReason = reason };

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

            private void RecordTypeDiagnosticIfRequired(TypeParseInfo typeParseInfo, TypeSpec typeSpec)
            {
                ContainingTypeDiagnosticInfo? containingTypeDiagInfo = typeParseInfo.ContainingTypeDiagnosticInfo;

                if (typeSpec is UnsupportedTypeSpec unsupportedTypeSpec)
                {
                    DiagnosticDescriptor descriptor = DiagnosticDescriptors.GetNotSupportedDescriptor(unsupportedTypeSpec.NotSupportedReason);
                    RecordTypeDiagnostic(typeParseInfo, descriptor);
                }
                else if (containingTypeDiagInfo?.Descriptor == DiagnosticDescriptors.DictionaryKeyNotSupported &&
                    typeSpec is not ParsableFromStringSpec)
                {
                    ReportContainingTypeDiagnosticIfRequired(typeParseInfo);
                }
            }

            private void RecordTypeDiagnostic(TypeParseInfo typeParseInfo, DiagnosticDescriptor descriptor)
            {
                RecordDiagnostic(descriptor, typeParseInfo.BinderInvocation?.Location, [typeParseInfo.FullName]);
                ReportContainingTypeDiagnosticIfRequired(typeParseInfo);
            }

            private void ReportContainingTypeDiagnosticIfRequired(TypeParseInfo typeParseInfo)
            {
                ContainingTypeDiagnosticInfo? containingTypeDiagInfo = typeParseInfo.ContainingTypeDiagnosticInfo;

                while (containingTypeDiagInfo is not null)
                {
                    string containingTypeName = containingTypeDiagInfo.FullName;

                    object[] messageArgs = containingTypeDiagInfo.MemberName is string memberName
                        ? new[] { memberName, containingTypeName }
                        : new[] { containingTypeName };

                    RecordDiagnostic(containingTypeDiagInfo.Descriptor, typeParseInfo.BinderInvocation?.Location, messageArgs);

                    containingTypeDiagInfo = containingTypeDiagInfo.ContainingTypeInfo;
                }
            }

            private void RecordDiagnostic(DiagnosticDescriptor descriptor, Location trimmedLocation, params object?[]? messageArgs)
            {
                Diagnostics ??= new List<DiagnosticInfo>();
                Diagnostics.Add(DiagnosticInfo.Create(descriptor, trimmedLocation, messageArgs));
            }

            private void CheckIfToEmitParseEnumMethod()
            {
                foreach (var typeSymbol in _createdTypeSpecs.Keys)
                {
                    if (IsEnum(typeSymbol))
                    {
                        _emitEnumParseMethod = true;
                        _emitGenericParseEnum = _typeSymbols.Enum.GetMembers("Parse").Any(m => m is IMethodSymbol methodSymbol && methodSymbol.IsGenericMethod);
                        return;
                    }
                }
            }

            private bool IsThrowIfNullMethodToBeEmitted()
            {
                if (_typeSymbols.ArgumentNullException is not null)
                {
                    var throwIfNullMethods = _typeSymbols.ArgumentNullException.GetMembers("ThrowIfNull");

                    foreach (var throwIfNullMethod in throwIfNullMethods)
                    {
                        if (throwIfNullMethod is IMethodSymbol throwIfNullMethodSymbol && throwIfNullMethodSymbol.IsStatic && throwIfNullMethodSymbol.Parameters.Length == 2)
                        {
                            var parameters = throwIfNullMethodSymbol.Parameters;
                            var firstParam = parameters[0];
                            var secondParam = parameters[1];

                            if (firstParam.Name == "argument" && firstParam.Type.SpecialType == SpecialType.System_Object
                                && secondParam.Name == "paramName" && secondParam.Type.Equals(_typeSymbols.String, SymbolEqualityComparer.Default))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }
    }
}
