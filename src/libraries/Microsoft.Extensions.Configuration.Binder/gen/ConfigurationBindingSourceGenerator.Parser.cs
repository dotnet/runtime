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
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private sealed class Parser
        {
            private readonly SourceProductionContext _context;
            private readonly KnownTypeSymbols _typeSymbols;

            private readonly HashSet<TypeSpec> _typesForBindMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForGetMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForConfigureMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForBindCoreMethodGen = new();

            private readonly HashSet<ITypeSymbol> _unsupportedTypes = new(SymbolEqualityComparer.Default);
            private readonly Dictionary<ITypeSymbol, TypeSpec?> _createdSpecs = new(SymbolEqualityComparer.Default);

            private readonly HashSet<ParsableFromStringTypeSpec> _primitivesForHelperGen = new();
            private readonly HashSet<string> _namespaces = new()
            {
                "System",
                "System.Globalization",
                "Microsoft.Extensions.Configuration"
            };

            private MethodSpecifier _methodsToGen;

            public Parser(SourceProductionContext context, KnownTypeSymbols typeSymbols)
            {
                _context = context;
                _typeSymbols = typeSymbols;
            }

            public SourceGenerationSpec? GetSourceGenerationSpec(ImmutableArray<BinderInvocationOperation> operations)
            {
                if (_typeSymbols.IConfiguration is null || _typeSymbols.IServiceCollection is null)
                {
                    return null;
                }

                foreach (BinderInvocationOperation operation in operations)
                {
                    switch (operation.BinderMethodKind)
                    {
                        case BinderMethodKind.Configure:
                            {
                                ProcessConfigureCall(operation);
                            }
                            break;
                        case BinderMethodKind.Get:
                            {
                                ProcessGetCall(operation);
                            }
                            break;
                        case BinderMethodKind.Bind:
                            {
                                ProcessBindCall(operation);
                            }
                            break;
                        default:
                            break;
                    }
                }

                Dictionary<MethodSpecifier, HashSet<TypeSpec>> rootConfigTypes = new()
                {
                    [MethodSpecifier.Bind] = _typesForBindMethodGen,
                    [MethodSpecifier.Get] = _typesForGetMethodGen,
                    [MethodSpecifier.Configure] = _typesForConfigureMethodGen,
                    [MethodSpecifier.BindCore] = _typesForBindCoreMethodGen,
                };

                return new SourceGenerationSpec(rootConfigTypes, _methodsToGen, _primitivesForHelperGen, _namespaces);
            }

            private void ProcessBindCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;

                // We're looking for IConfiguration.Bind(object).
                if (operation is IInvocationOperation { Arguments: { Length: 2 } arguments } &&
                    operation.TargetMethod.IsExtensionMethod &&
                    TypesAreEqual(_typeSymbols.IConfiguration, arguments[0].Parameter.Type) &&
                    arguments[1].Parameter.Type.SpecialType == SpecialType.System_Object)
                {
                    IConversionOperation argument = arguments[1].Value as IConversionOperation;
                    ITypeSymbol? type = ResolveType(argument)?.WithNullableAnnotation(NullableAnnotation.None);

                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object ||
                        namedType.SpecialType == SpecialType.System_Void ||
                        // Binding to root-level struct is a no-op.
                        namedType.IsValueType)
                    {
                        return;
                    }

                    AddRootConfigType(MethodSpecifier.Bind, namedType, binderOperation.Location);

                    static ITypeSymbol? ResolveType(IOperation argument) =>
                        argument switch
                        {
                            IConversionOperation c => ResolveType(c.Operand),
                            IInstanceReferenceOperation i => i.Type,
                            ILocalReferenceOperation l => l.Local.Type,
                            IFieldReferenceOperation f => f.Field.Type,
                            IMethodReferenceOperation m when m.Method.MethodKind == MethodKind.Constructor => m.Method.ContainingType,
                            IMethodReferenceOperation m => m.Method.ReturnType,
                            IAnonymousFunctionOperation f => f.Symbol.ReturnType,
                            _ => null
                        };
                }
            }

            private void ProcessGetCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;

                // We're looking for IConfiguration.Get<T>().
                if (operation is IInvocationOperation { Arguments.Length: 1 } invocationOperation &&
                    invocationOperation.TargetMethod.IsExtensionMethod &&
                    invocationOperation.TargetMethod.IsGenericMethod &&
                    TypesAreEqual(_typeSymbols.IConfiguration, invocationOperation.TargetMethod.Parameters[0].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object ||
                        namedType.SpecialType == SpecialType.System_Void)
                    {
                        return;
                    }

                    AddRootConfigType(MethodSpecifier.Get, namedType, binderOperation.Location);
                }
            }

            private void ProcessConfigureCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;

                // We're looking for IServiceCollection.Configure<T>(IConfiguration).
                if (operation is IInvocationOperation { Arguments.Length: 2 } invocationOperation &&
                    invocationOperation.TargetMethod.IsExtensionMethod &&
                    invocationOperation.TargetMethod.IsGenericMethod &&
                    TypesAreEqual(_typeSymbols.IServiceCollection, invocationOperation.TargetMethod.Parameters[0].Type) &&
                    TypesAreEqual(_typeSymbols.IConfiguration, invocationOperation.TargetMethod.Parameters[1].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object)
                    {
                        return;
                    }

                    AddRootConfigType(MethodSpecifier.Configure, namedType, binderOperation.Location);
                }
            }

            private TypeSpec? AddRootConfigType(MethodSpecifier method, ITypeSymbol type, Location? location)
            {
                if (type is not INamedTypeSymbol namedType || ContainsGenericParameters(namedType))
                {
                    return null;
                }

                TypeSpec? spec = GetOrCreateTypeSpec(namedType, location);
                HashSet<TypeSpec> types = method switch
                {
                    MethodSpecifier.Configure => _typesForConfigureMethodGen,
                    MethodSpecifier.Get => _typesForGetMethodGen,
                    MethodSpecifier.Bind => _typesForBindMethodGen,
                    MethodSpecifier.BindCore => _typesForBindCoreMethodGen,
                    _ => throw new InvalidOperationException($"Invalid method for config binding method generation: {method}")
                };

                if (spec != null)
                {
                    types.Add(spec);
                    _methodsToGen |= method;
                    if (method is not MethodSpecifier.Bind)
                    {
                        _methodsToGen |= MethodSpecifier.HasValueOrChildren;
                    }
                }

                return spec;
            }

            private TypeSpec? GetOrCreateTypeSpec(ITypeSymbol type, Location? location = null)
            {
                if (_createdSpecs.TryGetValue(type, out TypeSpec? spec))
                {
                    return spec;
                }

                if (type is INamedTypeSymbol { IsGenericType: true } genericType &&
                    genericType.ConstructUnboundGenericType() is INamedTypeSymbol { } unboundGeneric &&
                    unboundGeneric.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return TryGetTypeSpec(genericType.TypeArguments[0], NullableUnderlyingTypeNotSupported, out TypeSpec? underlyingType)
                        ? CacheSpec(new NullableSpec(type) { Location = location, UnderlyingType = underlyingType })
                        : null;
                }
                else if (IsSupportedArrayType(type, location, out ITypeSymbol? elementType))
                {
                    if (elementType.SpecialType is SpecialType.System_Byte)
                    {
                        return CacheSpec(new ParsableFromStringTypeSpec(type) { Location = location, StringParsableTypeKind = StringParsableTypeKind.ByteArray });
                    }

                    spec = CreateArraySpec((type as IArrayTypeSymbol)!, location);
                    if (spec is null)
                    {
                        return null;
                    }

                    RegisterTypeForBindCoreMethodGen(MethodSpecifier.BindCore, spec);
                    return CacheSpec(spec);
                }
                else if (IsParsableFromString(type, out StringParsableTypeKind specialTypeKind))
                {
                    return CacheSpec(
                        new ParsableFromStringTypeSpec(type)
                        {
                            Location = location,
                            StringParsableTypeKind = specialTypeKind
                        });
                }
                else if (IsCollection(type))
                {
                    spec = CreateCollectionSpec((INamedTypeSymbol)type, location);
                    if (spec is null)
                    {
                        return null;
                    }

                    RegisterTypeForBindCoreMethodGen(MethodSpecifier.BindCore, spec);
                    return CacheSpec(spec);
                }
                else if (TypesAreEqual(type, _typeSymbols.IConfigurationSection))
                {
                    return CacheSpec(new ConfigurationSectionTypeSpec(type) { Location = location });
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    spec = CreateObjectSpec(namedType, location);
                    if (spec is null)
                    {
                        return null;
                    }

                    RegisterTypeForBindCoreMethodGen(MethodSpecifier.BindCore, spec);
                    return CacheSpec(spec);
                }

                ReportUnsupportedType(type, TypeNotSupported, location);
                return null;

                T CacheSpec<T>(T? s) where T : TypeSpec
                {
                    TypeSpecKind typeKind = s.SpecKind;
                    Debug.Assert(typeKind is not TypeSpecKind.Unknown);

                    string @namespace = s.Namespace;
                    if (@namespace != null && @namespace != "<global namespace>")
                    {
                        _namespaces.Add(@namespace);
                    }

                    if (typeKind is TypeSpecKind.ParsableFromString)
                    {
                        ParsableFromStringTypeSpec type = ((ParsableFromStringTypeSpec)(object)s);
                        if (type.StringParsableTypeKind is not StringParsableTypeKind.ConfigValue)
                        {
                            _primitivesForHelperGen.Add(type);
                        }
                    }

                    _createdSpecs[type] = s;
                    return s;
                }
            }

            private void RegisterTypeForBindCoreMethodGen(MethodSpecifier method, TypeSpec spec)
            {
                _typesForBindCoreMethodGen.Add(spec);
                _methodsToGen |= method;
            }

            private bool IsParsableFromString(ITypeSymbol type, out StringParsableTypeKind typeKind)
            {
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
                            typeKind = StringParsableTypeKind.ConfigValue;
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
                            if (TypesAreEqual(type, _typeSymbols.CultureInfo))
                            {
                                typeKind = StringParsableTypeKind.CultureInfo;
                            }
                            else if (TypesAreEqual(type, _typeSymbols.DateTimeOffset) ||
                                TypesAreEqual(type, _typeSymbols.DateOnly) ||
                                TypesAreEqual(type, _typeSymbols.TimeOnly) ||
                                TypesAreEqual(type, _typeSymbols.TimeSpan))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (TypesAreEqual(type, _typeSymbols.Int128) ||
                                TypesAreEqual(type, _typeSymbols.Half) ||
                                TypesAreEqual(type, _typeSymbols.UInt128))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (TypesAreEqual(type, _typeSymbols.Uri))
                            {
                                typeKind = StringParsableTypeKind.Uri;
                            }
                            else if (TypesAreEqual(type, _typeSymbols.Version) ||
                                TypesAreEqual(type, _typeSymbols.Guid))
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

                if (spec == null)
                {
                    ReportUnsupportedType(type, descriptor);
                    return false;
                }

                return true;
            }

            private ArraySpec? CreateArraySpec(IArrayTypeSymbol arrayType, Location? location)
            {
                if (!TryGetTypeSpec(arrayType.ElementType, ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                // We want a Bind method for List<TElement> as a temp holder for the array values.
                EnumerableSpec? listSpec = ConstructAndCacheGenericTypeForBind(_typeSymbols.List, arrayType.ElementType) as EnumerableSpec;
                // We know the element type is supported.
                Debug.Assert(listSpec != null);

                return new ArraySpec(arrayType)
                {
                    Location = location,
                    ElementType = elementSpec,
                    ConcreteType = listSpec,
                };
            }

            private bool IsSupportedArrayType(ITypeSymbol type, Location? location, [NotNullWhen(true)] out ITypeSymbol? elementType)
            {
                if (type is not IArrayTypeSymbol arrayType)
                {
                    elementType = null;
                    return false;
                }

                if (arrayType.Rank > 1)
                {
                    ReportUnsupportedType(arrayType, MultiDimArraysNotSupported, location);
                    elementType = null;
                    return false;
                }

                elementType = arrayType.ElementType;
                return true;
            }

            private CollectionSpec? CreateCollectionSpec(INamedTypeSymbol type, Location? location)
            {
                if (IsCandidateDictionary(type, out ITypeSymbol keyType, out ITypeSymbol elementType))
                {
                    return CreateDictionarySpec(type, location, keyType, elementType);
                }
                else if (IsCandidateEnumerable(type, out elementType))
                {
                    return CreateEnumerableSpec(type, location, elementType);
                }

                return null;
            }

            private DictionarySpec CreateDictionarySpec(INamedTypeSymbol type, Location? location, ITypeSymbol keyType, ITypeSymbol elementType)
            {
                if (!TryGetTypeSpec(keyType, DictionaryKeyNotSupported, out TypeSpec keySpec) ||
                    !TryGetTypeSpec(elementType, ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                if (keySpec.SpecKind != TypeSpecKind.ParsableFromString)
                {
                    ReportUnsupportedType(type, DictionaryKeyNotSupported, location);
                    return null;
                }

                DictionarySpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeSymbols.GenericIDictionary) || IsInterfaceMatch(type, _typeSymbols.IDictionary))
                {
                    // We know the key and element types are supported.
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeSymbols.Dictionary, keyType, elementType) as DictionarySpec;
                    Debug.Assert(concreteType != null);
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType, keyType))
                {
                    ReportUnsupportedType(type, CollectionNotSupported, location);
                    return null;
                }

                return new DictionarySpec(type)
                {
                    Location = location,
                    KeyType = (ParsableFromStringTypeSpec)keySpec,
                    ElementType = elementSpec,
                    ConstructionStrategy = ConstructionStrategy.ParameterlessConstructor,
                    ConcreteType = concreteType
                };
            }

            private TypeSpec? ConstructAndCacheGenericTypeForBind(INamedTypeSymbol type, params ITypeSymbol[] parameters)
            {
                Debug.Assert(type.IsGenericType);
                return AddRootConfigType(MethodSpecifier.Bind, type.Construct(parameters), location: null);
            }

            private EnumerableSpec? CreateEnumerableSpec(INamedTypeSymbol type, Location? location, ITypeSymbol elementType)
            {
                if (!TryGetTypeSpec(elementType, ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                EnumerableSpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeSymbols.ISet))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeSymbols.HashSet, elementType) as EnumerableSpec;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.ICollection) ||
                    IsInterfaceMatch(type, _typeSymbols.GenericIList))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeSymbols.List, elementType) as EnumerableSpec;
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType))
                {
                    ReportUnsupportedType(type, CollectionNotSupported, location);
                    return null;
                }

                return new EnumerableSpec(type)
                {
                    Location = location,
                    ElementType = elementSpec,
                    ConstructionStrategy = ConstructionStrategy.ParameterlessConstructor,
                    ConcreteType = concreteType
                };
            }

            private ObjectSpec? CreateObjectSpec(INamedTypeSymbol type, Location? location)
            {
                Debug.Assert(!_createdSpecs.ContainsKey(type));

                // Add spec to cache before traversing properties to avoid stack overflow.

                if (!CanConstructObject(type, location))
                {
                    _createdSpecs.Add(type, null);
                    return null;
                }
                ObjectSpec objectSpec = new(type) { Location = location, ConstructionStrategy = ConstructionStrategy.ParameterlessConstructor };
                _createdSpecs.Add(type, objectSpec);

                INamedTypeSymbol current = type;
                while (current is not null)
                {
                    foreach (ISymbol member in current.GetMembers())
                    {
                        if (member is IPropertySymbol { IsIndexer: false } property)
                        {
                            if (property.Type is ITypeSymbol { } propertyType)
                            {
                                TypeSpec? propertyTypeSpec = GetOrCreateTypeSpec(propertyType);
                                string propertyName = property.Name;

                                if (propertyTypeSpec is null)
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(PropertyNotSupported, location, new string[] { propertyName, type.ToDisplayString() }));
                                }
                                else
                                {
                                    AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => TypesAreEqual(a.AttributeClass, _typeSymbols.ConfigurationKeyNameAttribute));
                                    string? configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                                    PropertySpec spec = new PropertySpec(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };
                                    if (spec.CanGet || spec.CanSet)
                                    {
                                        objectSpec.Properties.Add(spec);
                                    }

                                    if (propertyTypeSpec.SpecKind is TypeSpecKind.Object or
                                        TypeSpecKind.Array or
                                        TypeSpecKind.Enumerable or
                                        TypeSpecKind.Dictionary)
                                    {
                                        _methodsToGen |= MethodSpecifier.HasChildren;
                                    }
                                }
                            }
                        }
                    }
                    current = current.BaseType;
                }

                return objectSpec;
            }

            private bool IsCandidateEnumerable(INamedTypeSymbol type, out ITypeSymbol? elementType)
            {
                INamedTypeSymbol? @interface = GetInterface(type, _typeSymbols.ICollection);

                if (@interface is not null)
                {
                    elementType = @interface.TypeArguments[0];
                    return true;
                }

                elementType = null;
                return false;
            }

            private bool IsCandidateDictionary(INamedTypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? elementType)
            {
                INamedTypeSymbol? @interface = GetInterface(type, _typeSymbols.GenericIDictionary);
                if (@interface is not null)
                {
                    keyType = @interface.TypeArguments[0];
                    elementType = @interface.TypeArguments[1];
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
                        && TypesAreEqual(unbound, @interface));
                }

                return type.AllInterfaces.FirstOrDefault(candidate => TypesAreEqual(candidate, @interface));
            }

            private static bool IsInterfaceMatch(INamedTypeSymbol type, INamedTypeSymbol @interface)
            {
                if (type.IsGenericType)
                {
                    INamedTypeSymbol unbound = type.ConstructUnboundGenericType();
                    return TypesAreEqual(unbound, @interface);
                }

                return TypesAreEqual(type, @interface);
            }

            public static bool ContainsGenericParameters(INamedTypeSymbol type)
            {
                if (!type.IsGenericType)
                {
                    return false;
                }

                foreach (ITypeSymbol typeArg in type.TypeArguments)
                {
                    if (typeArg.TypeKind == TypeKind.TypeParameter)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool CanConstructObject(INamedTypeSymbol type, Location? location)
            {
                if (type.IsAbstract || type.TypeKind == TypeKind.Interface)
                {
                    ReportUnsupportedType(type, AbstractOrInterfaceNotSupported, location);
                    return false;
                }
                else if (!HasPublicParameterlessCtor(type))
                {
                    ReportUnsupportedType(type, NeedPublicParameterlessConstructor, location);
                    return false;
                }

                return true;
            }

            private static bool HasPublicParameterlessCtor(ITypeSymbol type)
            {
                if (type is not INamedTypeSymbol namedType)
                {
                    return false;
                }

                foreach (IMethodSymbol ctor in namedType.InstanceConstructors)
                {
                    if (ctor.DeclaredAccessibility == Accessibility.Public && ctor.Parameters.Length == 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool HasAddMethod(INamedTypeSymbol type, ITypeSymbol element)
            {
                INamedTypeSymbol current = type;
                while (current != null)
                {
                    if (current.GetMembers(Identifier.Add).Any(member =>
                        member is IMethodSymbol { Parameters.Length: 1 } method &&
                        TypesAreEqual(element, method.Parameters[0].Type)))
                    {
                        return true;
                    }
                    current = current.BaseType;
                }
                return false;
            }

            private static bool HasAddMethod(INamedTypeSymbol type, ITypeSymbol element, ITypeSymbol key)
            {
                INamedTypeSymbol current = type;
                while (current != null)
                {
                    if (current.GetMembers(Identifier.Add).Any(member =>
                        member is IMethodSymbol { Parameters.Length: 2 } method &&
                        TypesAreEqual(key, method.Parameters[0].Type) &&
                        TypesAreEqual(element, method.Parameters[1].Type)))
                    {
                        return true;
                    }
                    current = current.BaseType;
                }
                return false;
            }

            private static bool IsEnum(ITypeSymbol type) => type is INamedTypeSymbol { EnumUnderlyingType: INamedTypeSymbol { } };

            private void ReportUnsupportedType(ITypeSymbol type, DiagnosticDescriptor descriptor, Location? location = null)
            {
                if (!_unsupportedTypes.Contains(type))
                {
                    _context.ReportDiagnostic(
                        Diagnostic.Create(descriptor, location, new string[] { type.ToDisplayString() }));
                    _unsupportedTypes.Add(type);
                }
            }
        }
    }
}
