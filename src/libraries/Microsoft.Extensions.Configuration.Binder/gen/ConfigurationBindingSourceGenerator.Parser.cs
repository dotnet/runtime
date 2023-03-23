// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private sealed class Parser
        {
            private const string GlobalNameSpaceString = "<global namespace>";

            private readonly SourceProductionContext _context;
            private readonly KnownTypeData _typeData;

            private readonly HashSet<TypeSpec> _typesForBindMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForGetMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForConfigureMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForBindCoreMethodGen = new();

            private readonly HashSet<ITypeSymbol> _unsupportedTypes = new(SymbolEqualityComparer.Default);
            private readonly Dictionary<ITypeSymbol, TypeSpec?> _createdSpecs = new(SymbolEqualityComparer.Default);

            private readonly HashSet<string> _namespaces = new()
            {
                "System",
                "System.Linq",
                "Microsoft.Extensions.Configuration"
            };

            public Parser(SourceProductionContext context, KnownTypeData typeData)
            {
                _context = context;
                _typeData = typeData;
            }

            public SourceGenerationSpec? GetSourceGenerationSpec(ImmutableArray<BinderInvocationOperation> operations)
            {
                if (_typeData.SymbolForIConfiguration is null || _typeData.SymbolForIServiceCollection is null)
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

                Dictionary<MethodSpecifier, HashSet<TypeSpec>> methods = new()
                {
                    [MethodSpecifier.Bind] = _typesForBindMethodGen,
                    [MethodSpecifier.Get] = _typesForGetMethodGen,
                    [MethodSpecifier.Configure] = _typesForConfigureMethodGen,
                    [MethodSpecifier.BindCore] = _typesForBindCoreMethodGen,
                };

                return new SourceGenerationSpec(methods, _namespaces);
            }

            private void ProcessBindCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;

                // We're looking for IConfiguration.Bind(object).
                if (operation is IInvocationOperation { Arguments: { Length: 2 } arguments } &&
                    operation.TargetMethod.IsExtensionMethod &&
                    TypesAreEqual(_typeData.SymbolForIConfiguration, arguments[0].Parameter.Type) &&
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

                    AddTargetConfigType(_typesForBindMethodGen, namedType, binderOperation.Location);

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
                    TypesAreEqual(_typeData.SymbolForIConfiguration, invocationOperation.TargetMethod.Parameters[0].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object ||
                        namedType.SpecialType == SpecialType.System_Void)
                    {
                        return;
                    }

                    AddTargetConfigType(_typesForGetMethodGen, namedType, binderOperation.Location);
                }
            }

            private void ProcessConfigureCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;

                // We're looking for IServiceCollection.Configure<T>(IConfiguration).
                if (operation is IInvocationOperation { Arguments.Length: 2 } invocationOperation &&
                    invocationOperation.TargetMethod.IsExtensionMethod &&
                    invocationOperation.TargetMethod.IsGenericMethod &&
                    TypesAreEqual(_typeData.SymbolForIServiceCollection, invocationOperation.TargetMethod.Parameters[0].Type) &&
                    TypesAreEqual(_typeData.SymbolForIConfiguration, invocationOperation.TargetMethod.Parameters[1].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object)
                    {
                        return;
                    }

                    AddTargetConfigType(_typesForConfigureMethodGen, namedType, binderOperation.Location);
                }
            }

            private TypeSpec? AddTargetConfigType(HashSet<TypeSpec> specs, ITypeSymbol type, Location? location)
            {
                if (type is not INamedTypeSymbol namedType || ContainsGenericParameters(namedType))
                {
                    return null;
                }

                TypeSpec? spec = GetOrCreateTypeSpec(namedType, location);
                if (spec != null &&
                    !specs.Contains(spec))
                {
                    specs.Add(spec);
                }

                return spec;
            }

            private TypeSpec? GetOrCreateTypeSpec(ITypeSymbol type, Location? location = null)
            {
                if (_createdSpecs.TryGetValue(type, out TypeSpec? spec))
                {
                    return spec;
                }

                if (type.SpecialType == SpecialType.System_Object)
                {
                    return CacheSpec(new TypeSpec(type) { Location = location, SpecKind = TypeSpecKind.System_Object });
                }
                else if (type is INamedTypeSymbol { IsGenericType: true } genericType &&
                    genericType.ConstructUnboundGenericType() is INamedTypeSymbol { } unboundGeneric &&
                    unboundGeneric.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return TryGetTypeSpec(genericType.TypeArguments[0], NotSupportedReason.NullableUnderlyingTypeNotSupported, out TypeSpec? underlyingType)
                        ? CacheSpec(new NullableSpec(type) { Location = location, UnderlyingType = underlyingType })
                        : null;
                }
                else if (type.SpecialType != SpecialType.None)
                {
                    return CacheSpec(new TypeSpec(type) { Location = location });
                }
                else if (IsEnum(type))
                {
                    return CacheSpec(new TypeSpec(type) { Location = location, SpecKind = TypeSpecKind.Enum });
                }
                else if (type is IArrayTypeSymbol { } arrayType)
                {
                    spec = CreateArraySpec(arrayType, location);
                    if (spec is null)
                    {
                        return null;
                    }

                    if (spec.SpecKind != TypeSpecKind.ByteArray)
                    {
                        Debug.Assert(spec.SpecKind is TypeSpecKind.Array);
                        _typesForBindCoreMethodGen.Add(spec);
                    }

                    return CacheSpec(spec);
                }
                else if (TypesAreEqual(type, _typeData.SymbolForIConfigurationSection))
                {
                    return CacheSpec(new TypeSpec(type) { Location = location, SpecKind = TypeSpecKind.IConfigurationSection });
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    spec = IsCollection(namedType)
                        ? CreateCollectionSpec(namedType, location)
                        : CreateObjectSpec(namedType, location);

                    if (spec is null)
                    {
                        return null;
                    }

                    _typesForBindCoreMethodGen.Add(spec);
                    return CacheSpec(spec);
                }

                ReportUnsupportedType(type, NotSupportedReason.TypeNotSupported, location);
                return null;

                T CacheSpec<T>(T? s) where T : TypeSpec
                {
                    string @namespace = s.Namespace;
                    if (@namespace != null && @namespace != GlobalNameSpaceString)
                    {
                        _namespaces.Add(@namespace);
                    }

                    _createdSpecs[type] = s;
                    return s;
                }
            }

            private bool TryGetTypeSpec(ITypeSymbol type, string unsupportedReason, out TypeSpec? spec)
            {
                spec = GetOrCreateTypeSpec(type);

                if (spec == null)
                {
                    ReportUnsupportedType(type, unsupportedReason);
                    return false;
                }

                return true;
            }

            private EnumerableSpec? CreateArraySpec(IArrayTypeSymbol arrayType, Location? location)
            {
                if (arrayType.Rank > 1)
                {
                    ReportUnsupportedType(arrayType, NotSupportedReason.MultiDimArraysNotSupported, location);
                    return null;
                }

                if (!TryGetTypeSpec(arrayType.ElementType, NotSupportedReason.ElementTypeNotSupported, out TypeSpec? elementSpec))
                {
                    return null;
                }

                EnumerableSpec spec;
                if (elementSpec.SpecialType is SpecialType.System_Byte)
                {
                    spec = new EnumerableSpec(arrayType) { Location = location, SpecKind = TypeSpecKind.ByteArray, ElementType = elementSpec };
                }
                else
                {
                    // We want a Bind method for List<TElement> as a temp holder for the array values.
                    EnumerableSpec? listSpec = ConstructAndCacheGenericTypeForBind(_typeData.SymbolForList, arrayType.ElementType) as EnumerableSpec;
                    // We know the element type is supported.
                    Debug.Assert(listSpec != null);

                    spec = new EnumerableSpec(arrayType)
                    {
                        Location = location,
                        SpecKind = TypeSpecKind.Array,
                        ElementType = elementSpec,
                        ConcreteType = listSpec,
                    };
                }

                return spec;
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
                if (!TryGetTypeSpec(keyType, NotSupportedReason.DictionaryKeyNotSupported, out TypeSpec keySpec) ||
                    !TryGetTypeSpec(elementType, NotSupportedReason.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                if (keySpec.SpecKind != TypeSpecKind.StringBasedParse)
                {
                    ReportUnsupportedType(type, NotSupportedReason.DictionaryKeyNotSupported, location);
                    return null;
                }

                DictionarySpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeData.SymbolForGenericIDictionary) || IsInterfaceMatch(type, _typeData.SymbolForIDictionary))
                {
                    // We know the key and element types are supported.
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeData.SymbolForDictionary, keyType, elementType) as DictionarySpec;
                    Debug.Assert(concreteType != null);
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType, keyType))
                {
                    ReportUnsupportedType(type, NotSupportedReason.CollectionNotSupported, location);
                    return null;
                }

                return new DictionarySpec(type)
                {
                    Location = location,
                    KeyType = keySpec,
                    ElementType = elementSpec,
                    ConstructionStrategy = ConstructionStrategy.ParameterlessConstructor,
                    ConcreteType = concreteType
                };
            }

            private TypeSpec? ConstructAndCacheGenericTypeForBind(INamedTypeSymbol type, params ITypeSymbol[] parameters)
            {
                Debug.Assert(type.IsGenericType);
                return AddTargetConfigType(_typesForBindMethodGen, type.Construct(parameters), location: null);
            }

            private EnumerableSpec? CreateEnumerableSpec(INamedTypeSymbol type, Location? location, ITypeSymbol elementType)
            {
                if (!TryGetTypeSpec(elementType, NotSupportedReason.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                EnumerableSpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeData.SymbolForISet))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeData.SymbolForHashSet, elementType) as EnumerableSpec;
                }
                else if (IsInterfaceMatch(type, _typeData.SymbolForICollection) ||
                    IsInterfaceMatch(type, _typeData.SymbolForGenericIList))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_typeData.SymbolForList, elementType) as EnumerableSpec;
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType))
                {
                    ReportUnsupportedType(type, NotSupportedReason.CollectionNotSupported, location);
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
                while (current != null)
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
                                    AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => TypesAreEqual(a.AttributeClass, _typeData.SymbolForConfigurationKeyNameAttribute));
                                    string? configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                                    PropertySpec spec = new PropertySpec(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };
                                    if (spec.CanGet || spec.CanSet)
                                    {
                                        objectSpec.Properties.Add(spec);
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
                INamedTypeSymbol? @interface = GetInterface(type, _typeData.SymbolForICollection);

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
                INamedTypeSymbol? @interface = GetInterface(type, _typeData.SymbolForGenericIDictionary);
                if (@interface is not null)
                {
                    keyType = @interface.TypeArguments[0];
                    elementType = @interface.TypeArguments[1];
                    return true;
                }

                if (IsInterfaceMatch(type, _typeData.SymbolForIDictionary))
                {
                    keyType = _typeData.SymbolForString;
                    elementType = _typeData.SymbolForString;
                    return true;
                }

                keyType = null;
                elementType = null;
                return false;
            }

            private bool IsCollection(INamedTypeSymbol type) =>
                GetInterface(type, _typeData.SymbolForIEnumerable) is not null;

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
                    ReportUnsupportedType(type, NotSupportedReason.AbstractOrInterfaceNotSupported, location);
                    return false;
                }
                else if (!HasPublicParameterlessCtor(type))
                {
                    ReportUnsupportedType(type, NotSupportedReason.NeedPublicParameterlessConstructor, location);
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

            private void ReportUnsupportedType(ITypeSymbol type, string reason, Location? location = null)
            {
                if (!_unsupportedTypes.Contains(type))
                {
                    _context.ReportDiagnostic(
                        Diagnostic.Create(TypeNotSupported, location, new string[] { type.ToDisplayString(), reason }));
                    _unsupportedTypes.Add(type);
                }
            }
        }
    }
}
