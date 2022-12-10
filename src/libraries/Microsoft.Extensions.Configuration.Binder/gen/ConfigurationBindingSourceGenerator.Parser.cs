// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingSourceGenerator
    {
        private sealed class Parser
        {
            private readonly Compilation _compilation;
            private readonly SourceProductionContext _context;

            private readonly INamedTypeSymbol _symbolForGenericIList;
            private readonly INamedTypeSymbol _symbolForICollection;
            private readonly INamedTypeSymbol _symbolForIEnumerable;
            private readonly INamedTypeSymbol _symbolForString;

            private readonly INamedTypeSymbol? _symbolForConfigurationKeyNameAttribute;
            private readonly INamedTypeSymbol? _symbolForDictionary;
            private readonly INamedTypeSymbol? _symbolForGenericIDictionary;
            private readonly INamedTypeSymbol? _symbolForHashSet;
            private readonly INamedTypeSymbol? _symbolForIConfiguration;
            private readonly INamedTypeSymbol? _symbolForIConfigurationSection;
            private readonly INamedTypeSymbol? _symbolForIDictionary;
            private readonly INamedTypeSymbol? _symbolForIServiceCollection;
            private readonly INamedTypeSymbol? _symbolForISet;
            private readonly INamedTypeSymbol? _symbolForList;

            private readonly HashSet<TypeSpec> _typesForBindMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForGetMethodGen = new();
            private readonly HashSet<TypeSpec> _typesForConfigureMethodGen = new();

#pragma warning disable RS1024
            private readonly HashSet<ITypeSymbol> _unsupportedTypes = new(SymbolEqualityComparer.Default);
            private readonly Dictionary<ITypeSymbol, TypeSpec?> _createdSpecs = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024

            public Parser(SourceProductionContext context, Compilation compilation)
            {
                _compilation = compilation;
                _context = context;

                _symbolForIEnumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                _symbolForConfigurationKeyNameAttribute = compilation.GetBestTypeByMetadataName(TypeFullName.ConfigurationKeyNameAttribute);
                _symbolForIConfiguration = compilation.GetBestTypeByMetadataName(TypeFullName.IConfiguration);
                _symbolForIConfigurationSection = compilation.GetBestTypeByMetadataName(TypeFullName.IConfigurationSection);
                _symbolForIServiceCollection = compilation.GetBestTypeByMetadataName(TypeFullName.IServiceCollection);
                _symbolForString = compilation.GetSpecialType(SpecialType.System_String);

                // Collections
                _symbolForIDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.IDictionary);

                // Use for type equivalency checks for unbounded generics
                _symbolForICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T).ConstructUnboundGenericType();
                _symbolForGenericIDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.GenericIDictionary)?.ConstructUnboundGenericType();
                _symbolForGenericIList = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IList_T).ConstructUnboundGenericType();
                _symbolForISet = compilation.GetBestTypeByMetadataName(TypeFullName.ISet)?.ConstructUnboundGenericType();

                // Used to construct concrete types at runtime; cannot also be constructed
                _symbolForDictionary = compilation.GetBestTypeByMetadataName(TypeFullName.Dictionary);
                _symbolForHashSet = compilation.GetBestTypeByMetadataName(TypeFullName.HashSet);
                _symbolForList = compilation.GetBestTypeByMetadataName(TypeFullName.List);
            }

            public SourceGenerationSpec? GetSourceGenerationSpec(
                IEnumerable<InvocationExpressionSyntax> invocations,
                CancellationToken cancellationToken)
            {
                if (_symbolForIConfiguration is null || _symbolForIServiceCollection is null)
                {
                    return null;
                }

                foreach (InvocationExpressionSyntax invocation in invocations)
                {
                    if (IsBindCall(invocation))
                    {
                        ProcessBindCall(invocation, cancellationToken);
                    }
                    else if (IsGetCall(invocation))
                    {
                        ProcessGetCall(invocation, cancellationToken);
                    }
                    else if (IsConfigureCall(invocation))
                    {
                        ProcessConfigureCall(invocation, cancellationToken);
                    }
                }

                return new SourceGenerationSpec(_typesForBindMethodGen, _typesForGetMethodGen, _typesForConfigureMethodGen);
            }

            public static bool IsInputCall(SyntaxNode node) =>
                node is not InvocationExpressionSyntax invocation
                ? false
                : IsBindCall(invocation) || IsConfigureCall(invocation) || IsGetCall(invocation);

            private void ProcessBindCall(InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
            {
                SemanticModel semanticModel = _compilation.GetSemanticModel(invocation.SyntaxTree);
                IInvocationOperation operation = semanticModel.GetOperation(invocation, cancellationToken) as IInvocationOperation;

                // We're looking for IConfiguration.Bind(object).
                if (operation is IInvocationOperation { Arguments: { Length: 2 } arguments } &&
                    operation.TargetMethod.IsExtensionMethod &&
                    TypesAreEqual(_symbolForIConfiguration, arguments[0].Parameter.Type) &&
                    arguments[1].Parameter.Type.SpecialType == SpecialType.System_Object)
                {
                    IConversionOperation argument = arguments[1].Value as IConversionOperation;
                    ITypeSymbol? type = ResolveType(argument)?.WithNullableAnnotation(NullableAnnotation.None);

                    // TODO: do we need diagnostic for System.Object?
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object ||
                        namedType.SpecialType == SpecialType.System_Void)
                    {
                        return;
                    }

                    AddTargetConfigType(_typesForBindMethodGen, namedType, invocation.GetLocation());

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

            private void ProcessGetCall(InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
            {
                SemanticModel semanticModel = _compilation.GetSemanticModel(invocation.SyntaxTree);
                IInvocationOperation? operation = semanticModel.GetOperation(invocation, cancellationToken) as IInvocationOperation;

                // We're looking for IConfiguration.Get<T>().
                if (operation is IInvocationOperation { Arguments.Length: 1 } invocationOperation &&
                    invocationOperation.TargetMethod.IsExtensionMethod &&
                    invocationOperation.TargetMethod.IsGenericMethod &&
                    TypesAreEqual(_symbolForIConfiguration, invocationOperation.TargetMethod.Parameters[0].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object ||
                        namedType.SpecialType == SpecialType.System_Void)
                    {
                        return;
                    }

                    AddTargetConfigType(_typesForGetMethodGen, namedType, invocation.GetLocation());
                }
            }

            private void ProcessConfigureCall(InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
            {
                SemanticModel semanticModel = _compilation.GetSemanticModel(invocation.SyntaxTree);
                IOperation? operation = semanticModel.GetOperation(invocation, cancellationToken);

                // We're looking for IServiceCollection.Configure<T>(IConfiguration).
                if (operation is IInvocationOperation { Arguments.Length: 2 } invocationOperation &&
                    invocationOperation.TargetMethod.IsExtensionMethod &&
                    invocationOperation.TargetMethod.IsGenericMethod &&
                    TypesAreEqual(_symbolForIServiceCollection, invocationOperation.TargetMethod.Parameters[0].Type) &&
                    TypesAreEqual(_symbolForIConfiguration, invocationOperation.TargetMethod.Parameters[1].Type))
                {
                    ITypeSymbol? type = invocationOperation.TargetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol { } namedType ||
                        namedType.SpecialType == SpecialType.System_Object)
                    {
                        return;
                    }

                    AddTargetConfigType(_typesForConfigureMethodGen, namedType, invocation.GetLocation());
                }
            }

            public static bool IsBindCall(SyntaxNode node) =>
                node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: IdentifierNameSyntax
                        {
                            Identifier.ValueText: "Bind"
                        }
                    },
                    ArgumentList.Arguments.Count: 1
                };

            public static bool IsConfigureCall(SyntaxNode node) =>
                node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            Identifier.ValueText: "Configure"
                        }
                    },
                    ArgumentList.Arguments.Count: 1
                };

            public static bool IsGetCall(SyntaxNode node) =>
                node is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            Identifier.ValueText: "Get"
                        }
                    },
                    ArgumentList.Arguments.Count: 0
                };

            private TypeSpec? AddTargetConfigType(HashSet<TypeSpec> specs, ITypeSymbol type, Location? location)
            {
                if (type is not INamedTypeSymbol namedType || ContainsGenericParameters(namedType))
                {
                    return null;
                }

                TypeSpec? spec = GetOrCreateTypeSpec(namedType, location);
                if (spec != null && !specs.Contains(spec))
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

                if (type.Name == "IDictionary" && type is INamedTypeSymbol { IsGenericType: false })
                {
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
                    return spec == null ? null : CacheSpec(spec);
                }
                else if (TypesAreEqual(type, _symbolForIConfigurationSection))
                {
                    return CacheSpec(new TypeSpec(type) { Location = location, SpecKind = TypeSpecKind.IConfigurationSection });
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    return IsCollection(namedType)
                        ? CacheSpec(CreateCollectionSpec(namedType, location))
                        : CacheSpec(CreateObjectSpec(namedType, location));
                }

                ReportUnsupportedType(type, NotSupportedReason.TypeNotSupported, location);
                return null;

                T CacheSpec<T>(T? s) where T : TypeSpec
                {
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
                    EnumerableSpec? listSpec = ConstructAndCacheGenericTypeForBind(_symbolForList, arrayType.ElementType) as EnumerableSpec;
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
                if (!TryGetTypeSpec(keyType, NotSupportedReason.KeyTypeNotSupported, out TypeSpec keySpec) ||
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
                if (IsInterfaceMatch(type, _symbolForGenericIDictionary) || IsInterfaceMatch(type, _symbolForIDictionary))
                {
                    // We know the key and element types are supported.
                    concreteType = ConstructAndCacheGenericTypeForBind(_symbolForDictionary, keyType, elementType) as DictionarySpec;
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
                if (IsInterfaceMatch(type, _symbolForISet))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_symbolForHashSet, elementType) as EnumerableSpec;
                }
                else if (IsInterfaceMatch(type, _symbolForICollection) ||
                    IsInterfaceMatch(type, _symbolForGenericIList))
                {
                    concreteType = ConstructAndCacheGenericTypeForBind(_symbolForList, elementType) as EnumerableSpec;
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
                if (!CanConstructObject(type, location))
                {
                    return null;
                }

                List<PropertySpec> properties = new();
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
                                    AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => TypesAreEqual(a.AttributeClass, _symbolForConfigurationKeyNameAttribute));
                                    string? configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                                    PropertySpec spec = new PropertySpec(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };
                                    if (spec.CanGet || spec.CanSet)
                                    {
                                        properties.Add(spec);
                                    }
                                }
                            }
                        }
                    }
                    current = current.BaseType;
                }

                return new ObjectSpec(type) { Location = location, Properties = properties, ConstructionStrategy = ConstructionStrategy.ParameterlessConstructor };
            }

            private bool IsCandidateEnumerable(INamedTypeSymbol type, out ITypeSymbol? elementType)
            {
                INamedTypeSymbol? @interface = GetInterface(type, _symbolForICollection);

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
                INamedTypeSymbol? @interface = GetInterface(type, _symbolForGenericIDictionary);
                if (@interface is not null)
                {
                    keyType = @interface.TypeArguments[0];
                    elementType = @interface.TypeArguments[1];
                    return true;
                }

                if (IsInterfaceMatch(type, _symbolForIDictionary))
                {
                    keyType = _symbolForString;
                    elementType = _symbolForString;
                    return true;
                }

                keyType = null;
                elementType = null;
                return false;
            }

            private bool IsCollection(INamedTypeSymbol type) =>
                GetInterface(type, _symbolForIEnumerable) is not null;

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
                    if (current.GetMembers(Literal.Add).Any(member =>
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
                    if (current.GetMembers(Literal.Add).Any(member =>
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
