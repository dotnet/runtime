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

            private readonly Dictionary<MethodSpecifier, HashSet<TypeSpec>> _rootConfigTypes = new();

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
                    IInvocationOperation invocationOperation = operation.InvocationOperation!;
                    if (!invocationOperation.TargetMethod.IsExtensionMethod)
                    {
                        continue;
                    }

                    switch (operation.Kind)
                    {
                        case BinderMethodKind.Bind:
                            {
                                ProcessBindCall(operation);
                            }
                            break;
                        case BinderMethodKind.Get:
                            {
                                ProcessGetCall(operation);
                            }
                            break;
                        case BinderMethodKind.GetValue:
                            {
                                ProcessGetValueCall(operation);
                            }
                            break;
                        case BinderMethodKind.Configure:
                            {
                                ProcessConfigureCall(operation);
                            }
                            break;
                        default:
                            break;
                    }
                }

                return new SourceGenerationSpec(
                    _rootConfigTypes,
                    _methodsToGen,
                    _primitivesForHelperGen,
                    _namespaces);
            }

            private void ProcessBindCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;
                ImmutableArray<IParameterSymbol> @params = operation.TargetMethod.Parameters;
                int paramLength = @params.Length;

                if (!Helpers.TypesAreEqual(_typeSymbols.IConfiguration, @params[0].Type))
                {
                    return;
                }

                MethodSpecifier binderMethod = MethodSpecifier.None;

                if (paramLength is 2)
                {
                    binderMethod = MethodSpecifier.Bind_instance;
                }
                else if (paramLength is 3)
                {
                    if (@params[1].Type.SpecialType is SpecialType.System_String)
                    {
                        binderMethod = MethodSpecifier.Bind_key_instance;
                    }
                    else if (Helpers.TypesAreEqual(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        binderMethod = MethodSpecifier.Bind_instance_BinderOptions;
                    }
                }

                if (binderMethod is MethodSpecifier.None)
                {
                    return;
                }

                int objectIndex = binderMethod switch
                {
                    MethodSpecifier.Bind_instance => 1,
                    MethodSpecifier.Bind_instance_BinderOptions => 1,
                    MethodSpecifier.Bind_key_instance => 2,
                    _ => throw new InvalidOperationException()
                };

                IArgumentOperation objectArg = operation.Arguments[objectIndex];
                if (objectArg.Parameter.Type.SpecialType != SpecialType.System_Object)
                {
                    return;
                }

                ITypeSymbol? type = ResolveType(objectArg.Value)?.WithNullableAnnotation(NullableAnnotation.None);
                INamedTypeSymbol? namedType;

                if ((namedType = type as INamedTypeSymbol) is null ||
                    namedType.SpecialType == SpecialType.System_Object ||
                    namedType.SpecialType == SpecialType.System_Void ||
                    // Binding to root-level struct is a no-op.
                    namedType.IsValueType)
                {
                    return;
                }

                AddRootConfigType(MethodSpecifier.BindMethods, binderMethod, namedType, binderOperation.Location);

                static ITypeSymbol? ResolveType(IOperation conversionOperation) =>
                    conversionOperation switch
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

            private void ProcessGetCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramLength = @params.Length;

                if (!Helpers.TypesAreEqual(_typeSymbols.IConfiguration, @params[0].Type))
                {
                    return;
                }

                MethodSpecifier binderMethod = MethodSpecifier.None;
                INamedTypeSymbol? namedType;

                if (targetMethod.IsGenericMethod)
                {
                    if (paramLength > 2)
                    {
                        return;
                    }

                    namedType = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None) as INamedTypeSymbol;

                    if (paramLength is 1)
                    {
                        binderMethod = MethodSpecifier.Get_T;
                    }
                    else if (paramLength is 2 && Helpers.TypesAreEqual(@params[1].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        binderMethod = MethodSpecifier.Get_T_BinderOptions;
                    }
                }
                else if (paramLength > 3)
                {
                    return;
                }
                else
                {
                    ITypeOfOperation? typeOfOperation = operation.Arguments[1].ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    namedType = typeOfOperation?.TypeOperand as INamedTypeSymbol;

                    if (paramLength is 2)
                    {
                        binderMethod = MethodSpecifier.Get_TypeOf;
                    }
                    else if (paramLength is 3 && Helpers.TypesAreEqual(@params[2].Type, _typeSymbols.ActionOfBinderOptions))
                    {
                        binderMethod = MethodSpecifier.Get_TypeOf_BinderOptions;
                    }
                }

                if (binderMethod is MethodSpecifier.None ||
                    namedType is null ||
                    namedType.SpecialType == SpecialType.System_Object ||
                    namedType.SpecialType == SpecialType.System_Void)
                {
                    return;
                }

                AddRootConfigType(MethodSpecifier.GetMethods, binderMethod, namedType, binderOperation.Location);
            }

            private void ProcessGetValueCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;
                int paramLength = @params.Length;

                MethodSpecifier binderMethod = MethodSpecifier.None;
                ITypeSymbol? type;

                if (targetMethod.IsGenericMethod)
                {
                    if (paramLength > 3 || @params[1].Type.SpecialType is not SpecialType.System_String)
                    {
                        return;
                    }

                    type = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);

                    if (paramLength is 2)
                    {
                        binderMethod = MethodSpecifier.GetValue_T_key;
                    }
                    else if (paramLength is 3 && Helpers.TypesAreEqual(@params[2].Type, type))
                    {
                        binderMethod = MethodSpecifier.GetValue_T_key_defaultValue;
                    }
                }
                else if (paramLength > 4)
                {
                    return;
                }
                else
                {
                    if (@params[2].Type.SpecialType is not SpecialType.System_String)
                    {
                        return;
                    }

                    ITypeOfOperation? typeOfOperation = operation.Arguments[1].ChildOperations.FirstOrDefault() as ITypeOfOperation;
                    type = typeOfOperation?.TypeOperand;

                    if (paramLength is 3)
                    {
                        binderMethod = MethodSpecifier.GetValue_TypeOf_key;
                    }
                    else if (paramLength is 4 && @params[3].Type.SpecialType is SpecialType.System_Object)
                    {
                        binderMethod = MethodSpecifier.GetValue_TypeOf_key_defaultValue;
                    }
                }

                if (binderMethod is MethodSpecifier.None ||
                    type is null ||
                    type.SpecialType == SpecialType.System_Object ||
                    type.SpecialType == SpecialType.System_Void)
                {
                    return;
                }

                ITypeSymbol effectiveType = IsNullable(type, out ITypeSymbol? underlyingType) ? underlyingType : type;
                if (IsParsableFromString(effectiveType, out _))
                {
                    AddRootConfigType(MethodSpecifier.GetValueMethods, binderMethod, type, binderOperation.Location);
                }
            }

            private void ProcessConfigureCall(BinderInvocationOperation binderOperation)
            {
                IInvocationOperation operation = binderOperation.InvocationOperation!;
                IMethodSymbol targetMethod = operation.TargetMethod;
                ImmutableArray<IParameterSymbol> @params = targetMethod.Parameters;

                // We're looking for IServiceCollection.Configure<T>(IConfiguration).
                if (operation is IInvocationOperation { Arguments.Length: 2 } invocationOperation &&
                    targetMethod.IsGenericMethod &&
                    Helpers.TypesAreEqual(_typeSymbols.IServiceCollection, @params[0].Type) &&
                    Helpers.TypesAreEqual(_typeSymbols.IConfiguration, @params[1].Type))
                {
                    ITypeSymbol? type = targetMethod.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.None);
                    if (type is not INamedTypeSymbol namedType ||
                        namedType.SpecialType == SpecialType.System_Object)
                    {
                        return;
                    }

                    AddRootConfigType(MethodSpecifier.Configure, MethodSpecifier.Configure, namedType, binderOperation.Location);
                }
            }

            private TypeSpec? AddRootConfigType(MethodSpecifier methodGroup, MethodSpecifier method, ITypeSymbol type, Location? location)
            {
                if (type is INamedTypeSymbol namedType && ContainsGenericParameters(namedType))
                {
                    return null;
                }

                TypeSpec? spec = GetOrCreateTypeSpec(type, location);
                if (spec != null)
                {
                    GetRootConfigTypeCache(method).Add(spec);
                    GetRootConfigTypeCache(methodGroup).Add(spec);

                    _methodsToGen |= method;
                }

                return spec;
            }

            private TypeSpec? GetOrCreateTypeSpec(ITypeSymbol type, Location? location = null)
            {
                if (_createdSpecs.TryGetValue(type, out TypeSpec? spec))
                {
                    return spec;
                }

                if (IsNullable(type, out ITypeSymbol? underlyingType))
                {
                    spec = TryGetTypeSpec(underlyingType, Helpers.NullableUnderlyingTypeNotSupported, out TypeSpec? underlyingTypeSpec)
                        ? new NullableSpec(type) { Location = location, UnderlyingType = underlyingTypeSpec }
                        : null;
                }
                else if (IsParsableFromString(type, out StringParsableTypeKind specialTypeKind))
                {
                    ParsableFromStringTypeSpec stringParsableSpec = new(type)
                    {
                        Location = location,
                        StringParsableTypeKind = specialTypeKind
                    };

                    if (stringParsableSpec.StringParsableTypeKind is not StringParsableTypeKind.ConfigValue)
                    {
                        _primitivesForHelperGen.Add(stringParsableSpec);
                    }

                    spec = stringParsableSpec;
                }
                else if (IsSupportedArrayType(type, location))
                {
                    spec = CreateArraySpec((type as IArrayTypeSymbol)!, location);
                    RegisterBindCoreGenType(spec);
                }
                else if (IsCollection(type))
                {
                    spec = CreateCollectionSpec((INamedTypeSymbol)type, location);
                    RegisterBindCoreGenType(spec);
                }
                else if (Helpers.TypesAreEqual(type, _typeSymbols.IConfigurationSection))
                {
                    spec = new ConfigurationSectionTypeSpec(type) { Location = location };
                }
                else if (type is INamedTypeSymbol namedType)
                {
                    spec = CreateObjectSpec(namedType, location);
                    RegisterBindCoreGenType(spec);
                }

                if (spec is null)
                {
                    ReportUnsupportedType(type, Helpers.TypeNotSupported, location);
                    return null;
                }

                string @namespace = spec.Namespace;
                if (@namespace is not null and not "<global namespace>")
                {
                    _namespaces.Add(@namespace);
                }

                _createdSpecs[type] = spec;
                return spec;

                void RegisterBindCoreGenType(TypeSpec? spec)
                {
                    if (spec is not null)
                    {
                        GetRootConfigTypeCache(MethodSpecifier.BindCore).Add(spec);
                        _methodsToGen |= MethodSpecifier.BindCore;
                    }
                }
            }

            private HashSet<TypeSpec> GetRootConfigTypeCache(MethodSpecifier method)
            {
                if (!_rootConfigTypes.TryGetValue(method, out HashSet<TypeSpec> types))
                {
                    _rootConfigTypes[method] = types = new HashSet<TypeSpec>();
                }

                return types;
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
                            if (Helpers.TypesAreEqual(type, _typeSymbols.CultureInfo))
                            {
                                typeKind = StringParsableTypeKind.CultureInfo;
                            }
                            else if (Helpers.TypesAreEqual(type, _typeSymbols.DateTimeOffset) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.DateOnly) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.TimeOnly) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.TimeSpan))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (Helpers.TypesAreEqual(type, _typeSymbols.Int128) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.Half) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.UInt128))
                            {
                                typeKind = StringParsableTypeKind.ParseInvariant;
                            }
                            else if (Helpers.TypesAreEqual(type, _typeSymbols.Uri))
                            {
                                typeKind = StringParsableTypeKind.Uri;
                            }
                            else if (Helpers.TypesAreEqual(type, _typeSymbols.Version) ||
                                Helpers.TypesAreEqual(type, _typeSymbols.Guid))
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
                if (!TryGetTypeSpec(arrayType.ElementType, Helpers.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                // We want a BindCore method for List<TElement> as a temp holder for the array values.
                EnumerableSpec? listSpec = ConstructAndCacheGenericTypeForBindCore(_typeSymbols.List, arrayType.ElementType) as EnumerableSpec;
                // We know the element type is supported.
                Debug.Assert(listSpec != null);

                return new ArraySpec(arrayType)
                {
                    Location = location,
                    ElementType = elementSpec,
                    ConcreteType = listSpec,
                };
            }

            private bool IsSupportedArrayType(ITypeSymbol type, Location? location)
            {
                if (type is not IArrayTypeSymbol arrayType)
                {
                    return false;
                }

                if (arrayType.Rank > 1)
                {
                    ReportUnsupportedType(arrayType, Helpers.MultiDimArraysNotSupported, location);
                    return false;
                }

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
                if (!TryGetTypeSpec(keyType, Helpers.DictionaryKeyNotSupported, out TypeSpec keySpec) ||
                    !TryGetTypeSpec(elementType, Helpers.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }

                if (keySpec.SpecKind != TypeSpecKind.ParsableFromString)
                {
                    ReportUnsupportedType(type, Helpers.DictionaryKeyNotSupported, location);
                    return null;
                }

                DictionarySpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeSymbols.GenericIDictionary) || IsInterfaceMatch(type, _typeSymbols.IDictionary))
                {
                    // We know the key and element types are supported.
                    concreteType = ConstructAndCacheGenericTypeForBindCore(_typeSymbols.Dictionary, keyType, elementType) as DictionarySpec;
                    Debug.Assert(concreteType != null);
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType, keyType))
                {
                    ReportUnsupportedType(type, Helpers.CollectionNotSupported, location);
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

            private TypeSpec? ConstructAndCacheGenericTypeForBindCore(INamedTypeSymbol type, params ITypeSymbol[] parameters)
            {
                Debug.Assert(type.IsGenericType);
                TypeSpec spec = GetOrCreateTypeSpec(type.Construct(parameters));
                GetRootConfigTypeCache(MethodSpecifier.BindCore).Add(spec);
                return spec;
            }

            private EnumerableSpec? CreateEnumerableSpec(INamedTypeSymbol type, Location? location, ITypeSymbol elementType)
            {
                if (!TryGetTypeSpec(elementType, Helpers.ElementTypeNotSupported, out TypeSpec elementSpec))
                {
                    return null;
                }
                EnumerableSpec? concreteType = null;
                if (IsInterfaceMatch(type, _typeSymbols.ISet))
                {
                    concreteType = ConstructAndCacheGenericTypeForBindCore(_typeSymbols.HashSet, elementType) as EnumerableSpec;
                }
                else if (IsInterfaceMatch(type, _typeSymbols.ICollection) ||
                    IsInterfaceMatch(type, _typeSymbols.GenericIList))
                {
                    concreteType = ConstructAndCacheGenericTypeForBindCore(_typeSymbols.List, elementType) as EnumerableSpec;
                }
                else if (!CanConstructObject(type, location) || !HasAddMethod(type, elementType))
                {
                    ReportUnsupportedType(type, Helpers.CollectionNotSupported, location);
                    return null;
                }

                RegisterHasChildrenHelperForGenIfRequired(elementSpec);

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
                                    _context.ReportDiagnostic(Diagnostic.Create(Helpers.PropertyNotSupported, location, new string[] { propertyName, type.ToDisplayString() }));
                                }
                                else
                                {
                                    AttributeData? attributeData = property.GetAttributes().FirstOrDefault(a => Helpers.TypesAreEqual(a.AttributeClass, _typeSymbols.ConfigurationKeyNameAttribute));
                                    string configKeyName = attributeData?.ConstructorArguments.FirstOrDefault().Value as string ?? propertyName;

                                    PropertySpec spec = new PropertySpec(property) { Type = propertyTypeSpec, ConfigurationKeyName = configKeyName };
                                    if (spec.CanGet || spec.CanSet)
                                    {
                                        objectSpec.Properties[configKeyName] = (spec);
                                    }

                                    RegisterHasChildrenHelperForGenIfRequired(propertyTypeSpec);
                                }
                            }
                        }
                    }
                    current = current.BaseType;
                }

                return objectSpec;
            }

            private void RegisterHasChildrenHelperForGenIfRequired(TypeSpec type)
            {
                if (type.SpecKind is TypeSpecKind.Object or
                                        TypeSpecKind.Array or
                                        TypeSpecKind.Enumerable or
                                        TypeSpecKind.Dictionary)
                {
                    _methodsToGen |= MethodSpecifier.HasChildren;
                }
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
                        && Helpers.TypesAreEqual(unbound, @interface));
                }

                return type.AllInterfaces.FirstOrDefault(candidate => Helpers.TypesAreEqual(candidate, @interface));
            }

            private static bool IsInterfaceMatch(INamedTypeSymbol type, INamedTypeSymbol @interface)
            {
                if (type.IsGenericType)
                {
                    INamedTypeSymbol unbound = type.ConstructUnboundGenericType();
                    return Helpers.TypesAreEqual(unbound, @interface);
                }

                return Helpers.TypesAreEqual(type, @interface);
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
                    ReportUnsupportedType(type, Helpers.AbstractOrInterfaceNotSupported, location);
                    return false;
                }
                else if (!HasPublicParameterlessCtor(type))
                {
                    ReportUnsupportedType(type, Helpers.NeedPublicParameterlessConstructor, location);
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
                    if (current.GetMembers("Add").Any(member =>
                        member is IMethodSymbol { Parameters.Length: 1 } method &&
                        Helpers.TypesAreEqual(element, method.Parameters[0].Type)))
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
                    if (current.GetMembers("Add").Any(member =>
                        member is IMethodSymbol { Parameters.Length: 2 } method &&
                        Helpers.TypesAreEqual(key, method.Parameters[0].Type) &&
                        Helpers.TypesAreEqual(element, method.Parameters[1].Type)))
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
