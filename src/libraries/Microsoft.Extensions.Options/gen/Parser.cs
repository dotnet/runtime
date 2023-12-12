// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Options.Generators
{
    /// <summary>
    /// Holds an internal parser class that extracts necessary information for generating IValidateOptions.
    /// </summary>
    internal sealed class Parser
    {
        private const int NumValidationMethodArgs = 2;

        private readonly CancellationToken _cancellationToken;
        private readonly Compilation _compilation;
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly SymbolHolder _symbolHolder;
        private readonly OptionsSourceGenContext _optionsSourceGenContext;
        private readonly Dictionary<ITypeSymbol, ValidatorType> _synthesizedValidators = new(SymbolEqualityComparer.Default);
        private readonly HashSet<ITypeSymbol> _visitedModelTypes = new(SymbolEqualityComparer.Default);

        public Parser(
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            SymbolHolder symbolHolder,
            OptionsSourceGenContext optionsSourceGenContext,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _cancellationToken = cancellationToken;
            _reportDiagnostic = reportDiagnostic;
            _symbolHolder = symbolHolder;
            _optionsSourceGenContext = optionsSourceGenContext;
        }

        public IReadOnlyList<ValidatorType> GetValidatorTypes(IEnumerable<(TypeDeclarationSyntax TypeSyntax, SemanticModel SemanticModel)> classes)
        {
            var results = new List<ValidatorType>();

            foreach (var group in classes.GroupBy(x => x.TypeSyntax.SyntaxTree))
            {
                SemanticModel? sm = null;
                foreach (var typeDec in group)
                {
                    TypeDeclarationSyntax syntax = typeDec.TypeSyntax;
                    _cancellationToken.ThrowIfCancellationRequested();
                    sm ??= typeDec.SemanticModel;

                    var validatorType = sm.GetDeclaredSymbol(syntax) as ITypeSymbol;
                    if (validatorType is not null)
                    {
                        if (validatorType.IsStatic)
                        {
                            Diag(DiagDescriptors.CantBeStaticClass, syntax.GetLocation());
                            continue;
                        }

                        _visitedModelTypes.Clear();

                        var modelTypes = GetModelTypes(validatorType);
                        if (modelTypes.Count == 0)
                        {
                            // validator doesn't implement IValidateOptions
                            Diag(DiagDescriptors.DoesntImplementIValidateOptions, syntax.GetLocation(), validatorType.Name);
                            continue;
                        }

                        var modelsValidatorTypeValidates = new List<ValidatedModel>(modelTypes.Count);

                        foreach (var modelType in modelTypes)
                        {
                            if (modelType.Kind == SymbolKind.ErrorType)
                            {
                                // the compiler will report this error for us
                                continue;
                            }
                            else
                            {
                                // keep track of the models we look at, to detect loops
                                _ = _visitedModelTypes.Add(modelType.WithNullableAnnotation(NullableAnnotation.None));
                            }

                            if (AlreadyImplementsValidateMethod(validatorType, modelType))
                            {
                                // this type already implements a validation function, we can't auto-generate a new one
                                Diag(DiagDescriptors.AlreadyImplementsValidateMethod, syntax.GetLocation(), validatorType.Name);
                                continue;
                            }

                            Location? modelTypeLocation = modelType.GetLocation();
                            Location lowerLocationInCompilation = modelTypeLocation is not null && modelTypeLocation.SourceTree is not null && _compilation.ContainsSyntaxTree(modelTypeLocation.SourceTree)
                                ? modelTypeLocation
                                : syntax.GetLocation();

                            var membersToValidate = GetMembersToValidate(modelType, true, lowerLocationInCompilation, validatorType);
                            bool selfValidate = ModelSelfValidates(modelType);
                            if (membersToValidate.Count == 0 && !selfValidate)
                            {
                                // this type lacks any eligible members
                                Diag(DiagDescriptors.NoEligibleMembersFromValidator, syntax.GetLocation(), modelType.ToString(), validatorType.ToString());
                                continue;
                            }

                            modelsValidatorTypeValidates.Add(new ValidatedModel(
                                GetFQN(modelType),
                                modelType.Name,
                                selfValidate,
                                membersToValidate));
                        }

                        string keyword = GetTypeKeyword(validatorType);

                        // following code establishes the containment hierarchy for the generated type in terms of nested types

                        var parents = new List<string>();
                        var parent = syntax.Parent as TypeDeclarationSyntax;

                        while (parent is not null && IsAllowedKind(parent.Kind()))
                        {
                            parents.Add($"partial {GetTypeKeyword(parent)} {parent.Identifier}{parent.TypeParameterList} {parent.ConstraintClauses}");
                            parent = parent.Parent as TypeDeclarationSyntax;
                        }

                        parents.Reverse();

                        results.Add(new ValidatorType(
                            validatorType.ContainingNamespace.IsGlobalNamespace ? string.Empty : validatorType.ContainingNamespace.ToString()!,
                            GetMinimalFQN(validatorType),
                            GetMinimalFQNWithoutGenerics(validatorType),
                            keyword,
                            parents,
                            false,
                            modelsValidatorTypeValidates));
                    }
                }
            }

            results.AddRange(_synthesizedValidators.Values);
            _synthesizedValidators.Clear();

            if (results.Count > 0 && _compilation is CSharpCompilation { LanguageVersion : LanguageVersion version and < LanguageVersion.CSharp8 })
            {
                // we only support C# 8.0 and above
                Diag(DiagDescriptors.OptionsUnsupportedLanguageVersion, null, version.ToDisplayString(), LanguageVersion.CSharp8.ToDisplayString());
                return new List<ValidatorType>();
            }

            return results;
        }

        private static bool IsAllowedKind(SyntaxKind kind) =>
            kind == SyntaxKind.ClassDeclaration ||
            kind == SyntaxKind.StructDeclaration ||
            kind == SyntaxKind.RecordStructDeclaration ||
            kind == SyntaxKind.RecordDeclaration;

        private static string GetTypeKeyword(ITypeSymbol type)
        {
            if (type.IsReferenceType)
            {
                return type.IsRecord ? "record class" : "class";
            }

            return type.IsRecord ? "record struct" : "struct";
        }

        private static string GetTypeKeyword(TypeDeclarationSyntax type) =>
            type.Kind() switch
            {
                SyntaxKind.ClassDeclaration => "class",
                SyntaxKind.RecordDeclaration => "record class",
                SyntaxKind.RecordStructDeclaration => "record struct",
                _ => type.Keyword.ValueText,
            };

        private static string GetFQN(ISymbol type)
            => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));

        private static string GetMinimalFQN(ISymbol type)
            => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters));

        private static string GetMinimalFQNWithoutGenerics(ISymbol type)
            => type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None));

        /// <summary>
        /// Checks whether the given validator already implement the IValidationOptions&gt;T&lt; interface.
        /// </summary>
        private static bool AlreadyImplementsValidateMethod(INamespaceOrTypeSymbol validatorType, ISymbol modelType)
            => validatorType
                .GetMembers("Validate")
                .Where(m => m.Kind == SymbolKind.Method)
                .Select(m => (IMethodSymbol)m)
                .Any(m => m.Parameters.Length == NumValidationMethodArgs
                    && m.Parameters[0].Type.SpecialType == SpecialType.System_String
                    && SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, modelType));

        /// <summary>
        /// Checks whether the given type contain any unbound generic type arguments.
        /// </summary>
        private static bool HasOpenGenerics(ITypeSymbol type, out string genericType)
        {
            if (type is INamedTypeSymbol mt)
            {
                if (mt.IsGenericType)
                {
                    foreach (var ta in mt.TypeArguments)
                    {
                        if (ta.TypeKind == TypeKind.TypeParameter)
                        {
                            genericType = ta.Name;
                            return true;
                        }
                    }
                }
            }
            else if (type is ITypeParameterSymbol)
            {
                genericType = type.Name;
                return true;
            }
            else if (type is IArrayTypeSymbol ats)
            {
                return HasOpenGenerics(ats.ElementType, out genericType);
            }

            genericType = string.Empty;
            return false;
        }

        private ITypeSymbol? GetEnumeratedType(ITypeSymbol type)
        {
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                // extract the T from a Nullable<T>
                type = ((INamedTypeSymbol)type).TypeArguments[0];
            }

            // Check first if the type is IEnumerable<T> interface
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, _symbolHolder.GenericIEnumerableSymbol))
            {
                return ((INamedTypeSymbol)type).TypeArguments[0];
            }

            // Check first if the type implement IEnumerable<T> interface
            foreach (var implementingInterface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementingInterface.OriginalDefinition, _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)))
                {
                    return implementingInterface.TypeArguments.First();
                }
            }

            return null;
        }

        private List<ValidatedMember> GetMembersToValidate(ITypeSymbol modelType, bool speculate, Location lowerLocationInCompilation, ITypeSymbol validatorType)
        {
            // make a list of the most derived members in the model type

            if (modelType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                // extract the T from a Nullable<T>
                modelType = ((INamedTypeSymbol)modelType).TypeArguments[0];
            }

            var members = modelType.GetMembers().ToList();
            var addedMembers = new HashSet<string>(members.Select(m => m.Name));
            var baseType = modelType.BaseType;
            while (baseType is not null && baseType.SpecialType != SpecialType.System_Object
                // We ascend the hierarchy only if the base type is a user-defined type, as validating properties of system types is unnecessary.
                // This approach prevents generating warnings for properties defined in system types.
                // For example, in the case of `MyModel : Dictionary<string, string>`, this avoids warnings for properties like Keys and Values,
                // where a missing ValidateEnumeratedItemsAttribute might be incorrectly inferred.
                && !baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.", StringComparison.Ordinal))
            {
                var baseMembers = baseType.GetMembers().Where(m => !addedMembers.Contains(m.Name));
                members.AddRange(baseMembers);
                addedMembers.UnionWith(baseMembers.Select(m => m.Name));
                baseType = baseType.BaseType;
            }

            var membersToValidate = new List<ValidatedMember>();
            foreach (var member in members)
            {
                Location? memberLocation = member.GetLocation();
                Location location = memberLocation is not null && memberLocation.SourceTree is not null && _compilation.ContainsSyntaxTree(memberLocation.SourceTree)
                    ? memberLocation
                    : lowerLocationInCompilation;

                var memberInfo = GetMemberInfo(member, speculate, location, modelType, validatorType);
                if (memberInfo is not null)
                {
                    if (member.DeclaredAccessibility != Accessibility.Public)
                    {
                        Diag(DiagDescriptors.MemberIsInaccessible, member.Locations.First(), member.Name);
                        continue;
                    }

                    membersToValidate.Add(memberInfo);
                }
            }

            return membersToValidate;
        }

        private ValidatedMember? GetMemberInfo(ISymbol member, bool speculate, Location location, ITypeSymbol modelType, ITypeSymbol validatorType)
        {
            ITypeSymbol memberType;
            switch (member)
            {
                case IPropertySymbol prop:
                    memberType = prop.Type;
                    break;

                /* The runtime doesn't support fields validation yet. If we allow that in the future, we need to add the following code back.
                case IFieldSymbol field:
                    if (field.AssociatedSymbol is not null)
                    {
                        // a backing field for a property, don't need those
                        return null;
                    }

                    memberType = field.Type;
                    break;
                */
                default:
                    // we only care about properties
                    return null;
            }

            var validationAttrs = new List<ValidationAttributeInfo>();
            string? transValidatorTypeName = null;
            string? enumerationValidatorTypeName = null;
            var enumeratedIsNullable = false;
            var enumeratedIsValueType = false;
            var enumeratedMayBeNull = false;
            var transValidatorIsSynthetic = false;
            var enumerationValidatorIsSynthetic = false;

            foreach (var attribute in member.GetAttributes().Where(a => a.AttributeClass is not null))
            {
                var attributeType = attribute.AttributeClass!;
                var attrLoc = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

                if (SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.ValidateObjectMembersAttributeSymbol))
                {
                    if (HasOpenGenerics(memberType, out var genericType))
                    {
                        Diag(DiagDescriptors.CantUseWithGenericTypes, attrLoc, genericType);
    #pragma warning disable S1226 // Method parameters, caught exceptions and foreach variables' initial values should not be ignored
                        speculate = false;
    #pragma warning restore S1226 // Method parameters, caught exceptions and foreach variables' initial values should not be ignored
                        continue;
                    }

                    if (attribute.ConstructorArguments.Length == 1)
                    {
                        var transValidatorType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                        if (transValidatorType is not null)
                        {
                            if (CanValidate(transValidatorType, memberType))
                            {
                                if (transValidatorType.Constructors.Where(c => !c.Parameters.Any()).Any())
                                {
                                    transValidatorTypeName = transValidatorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                }
                                else
                                {
                                    Diag(DiagDescriptors.ValidatorsNeedSimpleConstructor, attrLoc, transValidatorType.Name);
                                }
                            }
                            else
                            {
                                Diag(DiagDescriptors.DoesntImplementIValidateOptions, attrLoc, transValidatorType.Name, memberType.Name);
                            }
                        }
                        else
                        {
                            Diag(DiagDescriptors.NullValidatorType, attrLoc);
                        }
                    }
                    else if (!_visitedModelTypes.Add(memberType.WithNullableAnnotation(NullableAnnotation.None)))
                    {
                        Diag(DiagDescriptors.CircularTypeReferences, attrLoc, memberType.ToString());
                        speculate = false;
                        continue;
                    }

                    if (transValidatorTypeName == null)
                    {
                        transValidatorIsSynthetic = true;
                        transValidatorTypeName = AddSynthesizedValidator(memberType, member, location, validatorType);
                    }

                    // pop the stack
                    _ = _visitedModelTypes.Remove(memberType.WithNullableAnnotation(NullableAnnotation.None));
                }
                else if (SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.ValidateEnumeratedItemsAttributeSymbol))
                {
                    var enumeratedType = GetEnumeratedType(memberType);
                    if (enumeratedType == null)
                    {
                        Diag(DiagDescriptors.NotEnumerableType, attrLoc, memberType);
                        speculate = false;
                        continue;
                    }

                    enumeratedIsNullable = enumeratedType.IsReferenceType || enumeratedType.NullableAnnotation == NullableAnnotation.Annotated;
                    enumeratedIsValueType = enumeratedType.IsValueType;
                    enumeratedMayBeNull = enumeratedType.NullableAnnotation == NullableAnnotation.Annotated;

                    if (HasOpenGenerics(enumeratedType, out var genericType))
                    {
                        Diag(DiagDescriptors.CantUseWithGenericTypes, attrLoc, genericType);
                        speculate = false;
                        continue;
                    }

                    if (attribute.ConstructorArguments.Length == 1)
                    {
                        var enumerationValidatorType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                        if (enumerationValidatorType is not null)
                        {
                            if (CanValidate(enumerationValidatorType, enumeratedType))
                            {
                                if (enumerationValidatorType.Constructors.Where(c => c.Parameters.Length == 0).Any())
                                {
                                    enumerationValidatorTypeName = enumerationValidatorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                }
                                else
                                {
                                    Diag(DiagDescriptors.ValidatorsNeedSimpleConstructor, attrLoc, enumerationValidatorType.Name);
                                }
                            }
                            else
                            {
                                Diag(DiagDescriptors.DoesntImplementIValidateOptions, attrLoc, enumerationValidatorType.Name, enumeratedType.Name);
                            }
                        }
                        else
                        {
                            Diag(DiagDescriptors.NullValidatorType, attrLoc);
                        }
                    }
                    else if (!_visitedModelTypes.Add(enumeratedType.WithNullableAnnotation(NullableAnnotation.None)))
                    {
                        Diag(DiagDescriptors.CircularTypeReferences, attrLoc, enumeratedType.ToString());
                        speculate = false;
                        continue;
                    }

                    if (enumerationValidatorTypeName == null)
                    {
                        enumerationValidatorIsSynthetic = true;
                        enumerationValidatorTypeName = AddSynthesizedValidator(enumeratedType, member, location, validatorType);
                    }

                    // pop the stack
                    _ = _visitedModelTypes.Remove(enumeratedType.WithNullableAnnotation(NullableAnnotation.None));
                }
                else if (ConvertTo(attributeType, _symbolHolder.ValidationAttributeSymbol))
                {
                    if (!_compilation.IsSymbolAccessibleWithin(attributeType, validatorType))
                    {
                        Diag(DiagDescriptors.InaccessibleValidationAttribute, location, attributeType.Name, member.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), validatorType.Name);
                        continue;
                    }

                    string attributeFullQualifiedName = attributeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.MaxLengthAttributeSymbol) ||
                        SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.MinLengthAttributeSymbol) ||
                        (_symbolHolder.LengthAttributeSymbol is not null && SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.LengthAttributeSymbol)))
                    {
                        if (!LengthBasedAttributeIsTrackedForSubstitution(memberType, location, attributeType, ref attributeFullQualifiedName))
                        {
                            continue;
                        }
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.CompareAttributeSymbol))
                    {
                        TrackCompareAttributeForSubstitution(attribute, modelType, ref attributeFullQualifiedName);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(attributeType, _symbolHolder.RangeAttributeSymbol))
                    {
                        TrackRangeAttributeForSubstitution(attribute, memberType, ref attributeFullQualifiedName);
                    }

                    var validationAttr = new ValidationAttributeInfo(attributeFullQualifiedName);
                    validationAttrs.Add(validationAttr);

                    ImmutableArray<IParameterSymbol> parameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
                    bool lastParameterDeclaredWithParamsKeyword =  parameters.Length > 0 && parameters[parameters.Length - 1].IsParams;

                    ImmutableArray<TypedConstant> arguments = attribute.ConstructorArguments;

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        TypedConstant argument = arguments[i];
                        if (argument.Kind == TypedConstantKind.Array)
                        {
                            bool isParams = lastParameterDeclaredWithParamsKeyword && i == arguments.Length - 1;
                            validationAttr.ConstructorArguments.Add(GetArrayArgumentExpression(argument.Values, isParams));
                        }
                        else
                        {
                            validationAttr.ConstructorArguments.Add(GetArgumentExpression(argument.Type!, argument.Value));
                        }
                    }

                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        if (namedArgument.Value.Kind == TypedConstantKind.Array)
                        {
                            bool isParams = lastParameterDeclaredWithParamsKeyword && namedArgument.Key == parameters[parameters.Length - 1].Name;
                            validationAttr.Properties.Add(namedArgument.Key, GetArrayArgumentExpression(namedArgument.Value.Values, isParams));
                        }
                        else
                        {
                            validationAttr.Properties.Add(namedArgument.Key, GetArgumentExpression(namedArgument.Value.Type!, namedArgument.Value.Value));
                        }
                    }
                }
            }

            bool validationAttributeIsApplied = validationAttrs.Count > 0 || transValidatorTypeName is not null || enumerationValidatorTypeName is not null;

            if (member.IsStatic)
            {
                // generate a warning if the member is const/static and has a validation attribute applied
                if (validationAttributeIsApplied)
                {
                    Diag(DiagDescriptors.CantValidateStaticOrConstMember, location, member.Name);
                }

                // don't validate the member in any case
                return null;
            }

            // generate a warning if the field/property seems like it should be transitively validated
            if (transValidatorTypeName == null && speculate && memberType.SpecialType == SpecialType.None)
            {
                if (!HasOpenGenerics(memberType, out var genericType))
                {
                    var membersToValidate = GetMembersToValidate(memberType, false, location, validatorType);
                    if (membersToValidate.Count > 0)
                    {
                        Diag(DiagDescriptors.PotentiallyMissingTransitiveValidation, location, memberType.Name, member.Name);
                    }
                }
            }

            // generate a warning if the field/property seems like it should be enumerated
            if (enumerationValidatorTypeName == null && speculate && memberType.SpecialType != SpecialType.System_String)
            {
                var enumeratedType = GetEnumeratedType(memberType);
                if (enumeratedType is not null)
                {
                    if (!HasOpenGenerics(enumeratedType, out var genericType))
                    {
                        var membersToValidate = GetMembersToValidate(enumeratedType, false, location, validatorType);
                        if (membersToValidate.Count > 0)
                        {
                            Diag(DiagDescriptors.PotentiallyMissingEnumerableValidation, location, enumeratedType.Name, member.Name);
                        }
                    }
                }
            }

            if (validationAttributeIsApplied)
            {
                return new(
                    member.Name,
                    validationAttrs,
                    transValidatorTypeName,
                    transValidatorIsSynthetic,
                    enumerationValidatorTypeName,
                    enumerationValidatorIsSynthetic,
                    memberType.IsReferenceType || memberType.NullableAnnotation == NullableAnnotation.Annotated,
                    memberType.IsValueType,
                    enumeratedIsNullable,
                    enumeratedIsValueType,
                    enumeratedMayBeNull);
            }

            return null;
        }

        private bool LengthBasedAttributeIsTrackedForSubstitution(ITypeSymbol memberType, Location location, ITypeSymbol attributeType, ref string attributeFullQualifiedName)
        {
            if (memberType.SpecialType == SpecialType.System_String || ConvertTo(memberType, _symbolHolder.ICollectionSymbol))
            {
                _optionsSourceGenContext.EnsureTrackingAttribute(attributeType.Name, createValue: false, out _);
            }
            else if (ParserUtilities.TypeHasProperty(memberType, "Count", SpecialType.System_Int32))
            {
                _optionsSourceGenContext.EnsureTrackingAttribute(attributeType.Name, createValue: true, out HashSet<object>? trackedTypeList);
                trackedTypeList!.Add(memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
            else
            {
                Diag(DiagDescriptors.IncompatibleWithTypeForValidationAttribute, location, attributeType.Name, memberType.Name);
                return false;
            }

            attributeFullQualifiedName = $"{Emitter.StaticGeneratedValidationAttributesClassesNamespace}.{Emitter.StaticAttributeClassNamePrefix}{_optionsSourceGenContext.Suffix}_{attributeType.Name}";
            return true;
        }

        private void TrackCompareAttributeForSubstitution(AttributeData attribute, ITypeSymbol modelType, ref string attributeFullQualifiedName)
        {
            ImmutableArray<IParameterSymbol> constructorParameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            if (constructorParameters.Length == 1 && constructorParameters[0].Name == "otherProperty" && constructorParameters[0].Type.SpecialType == SpecialType.System_String)
            {
                _optionsSourceGenContext.EnsureTrackingAttribute(attribute.AttributeClass!.Name, createValue: true, out HashSet<object>? trackedTypeList);
                trackedTypeList!.Add((modelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), (string)attribute.ConstructorArguments[0].Value!));
                attributeFullQualifiedName = $"{Emitter.StaticGeneratedValidationAttributesClassesNamespace}.{Emitter.StaticAttributeClassNamePrefix}{_optionsSourceGenContext.Suffix}_{attribute.AttributeClass!.Name}";
            }
        }

        private void TrackRangeAttributeForSubstitution(AttributeData attribute, ITypeSymbol memberType, ref string attributeFullQualifiedName)
        {
            ImmutableArray<IParameterSymbol> constructorParameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            ITypeSymbol? argumentType = null;
            bool hasTimeSpanType = false;

            ITypeSymbol typeSymbol = memberType;
            if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                typeSymbol = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
            }

            if (constructorParameters.Length == 2)
            {
                if (OptionsSourceGenContext.IsConvertibleBasicType(typeSymbol))
                {
                    argumentType = constructorParameters[0].Type;
                }
            }
            else if (constructorParameters.Length == 3)
            {
                object? argumentValue = null;
                for (int i = 0; i < constructorParameters.Length; i++)
                {
                    if (constructorParameters[i].Name == "type")
                    {
                        argumentValue = attribute.ConstructorArguments[i].Value;
                        break;
                    }
                }

                if (argumentValue is INamedTypeSymbol namedTypeSymbol)
                {
                    // When type is provided as a parameter, it has to match the property type.
                    if (OptionsSourceGenContext.IsConvertibleBasicType(namedTypeSymbol) && typeSymbol.SpecialType == namedTypeSymbol.SpecialType)
                    {
                        argumentType = namedTypeSymbol;
                    }
                    else if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, _symbolHolder.TimeSpanSymbol) &&
                             (SymbolEqualityComparer.Default.Equals(typeSymbol, _symbolHolder.TimeSpanSymbol) || typeSymbol.SpecialType == SpecialType.System_String))
                    {
                        hasTimeSpanType = true;
                        argumentType = _symbolHolder.TimeSpanSymbol;
                    }
                }
            }

            if (argumentType is not null)
            {
                _optionsSourceGenContext.EnsureTrackingAttribute(attribute.AttributeClass!.Name, createValue: hasTimeSpanType, out _);
                attributeFullQualifiedName = $"{Emitter.StaticGeneratedValidationAttributesClassesNamespace}.{Emitter.StaticAttributeClassNamePrefix}{_optionsSourceGenContext.Suffix}_{attribute.AttributeClass!.Name}";
            }
        }

        private string? AddSynthesizedValidator(ITypeSymbol modelType, ISymbol member, Location location, ITypeSymbol validatorType)
        {
            var mt = modelType.WithNullableAnnotation(NullableAnnotation.None);
            if (mt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                // extract the T from a Nullable<T>
                mt = ((INamedTypeSymbol)mt).TypeArguments[0];
            }

            if (_synthesizedValidators.TryGetValue(mt, out var validator))
            {
                return "global::" + validator.Namespace + "." + validator.Name;
            }

            bool selfValidate = ModelSelfValidates(mt);
            var membersToValidate = GetMembersToValidate(mt, true, location, validatorType);
            if (membersToValidate.Count == 0 && !selfValidate)
            {
                // this type lacks any eligible members
                Diag(DiagDescriptors.NoEligibleMember, location, mt.ToString(), member.ToString());
                return null;
            }

            var model = new ValidatedModel(
                GetFQN(mt),
                mt.Name,
                selfValidate,
                membersToValidate);

            var validatorTypeName = "__" + mt.Name + "Validator__";

            var result = new ValidatorType(
                mt.ContainingNamespace.IsGlobalNamespace ? string.Empty : mt.ContainingNamespace.ToString()!,
                validatorTypeName,
                validatorTypeName,
                "class",
                new List<string>(),
                true,
                new[] { model });

            _synthesizedValidators[mt] = result;
            return "global::" + (result.Namespace.Length > 0 ? result.Namespace + "." + result.Name : result.Name);
        }

        private bool ConvertTo(ITypeSymbol source, ITypeSymbol dest)
        {
            var conversion = _compilation.ClassifyConversion(source, dest);
            return conversion.IsReference && conversion.IsImplicit;
        }

        private bool ModelSelfValidates(ITypeSymbol modelType)
        {
            foreach (var implementingInterface in modelType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementingInterface.OriginalDefinition, _symbolHolder.IValidatableObjectSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        private List<ITypeSymbol> GetModelTypes(ITypeSymbol validatorType)
        {
            var result = new List<ITypeSymbol>();
            foreach (var implementingInterface in validatorType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementingInterface.OriginalDefinition, _symbolHolder.ValidateOptionsSymbol))
                {
                    result.Add(implementingInterface.TypeArguments.First());
                }
            }

            return result;
        }

        private bool CanValidate(ITypeSymbol validatorType, ISymbol modelType)
        {
            foreach (var implementingInterface in validatorType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementingInterface.OriginalDefinition, _symbolHolder.ValidateOptionsSymbol))
                {
                    var t = implementingInterface.TypeArguments.First();
                    if (SymbolEqualityComparer.Default.Equals(modelType, t))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string GetArrayArgumentExpression(ImmutableArray<Microsoft.CodeAnalysis.TypedConstant> value, bool isParams)
        {
            var sb = new StringBuilder();
            if (!isParams)
            {
                sb.Append("new[] { ");
            }

            for (int i = 0; i < value.Length; i++)
            {
                sb.Append(GetArgumentExpression(value[i].Type!, value[i].Value));

                if (i < value.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            if (!isParams)
            {
                sb.Append(" }");
            }

            return sb.ToString();
        }

        private string GetArgumentExpression(ITypeSymbol type, object? value)
        {
            if (value == null)
            {
                return "null";
            }

            if (type.SpecialType == SpecialType.System_Boolean)
            {
                return (bool)value ? "true" : "false";
            }

            if (SymbolEqualityComparer.Default.Equals(type, _symbolHolder.TypeSymbol) &&
                value is INamedTypeSymbol sym)
            {
                return $"typeof({sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
            }

            if (type.SpecialType == SpecialType.System_String)
            {
                return $@"""{EscapeString(value.ToString()!)}""";
            }

            if (type.SpecialType == SpecialType.System_Char)
            {
                return $@"'{EscapeString(value.ToString()!)}'";
            }

            return $"({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){Convert.ToString(value, CultureInfo.InvariantCulture)}";
        }

        private static readonly char[] _specialChars = { '\n', '\r', '"', '\\' };

        private static string EscapeString(string s)
        {
            int index = s.IndexOfAny(_specialChars);
            if (index < 0)
            {
                return s;
            }

            var sb = new StringBuilder(s.Length);
            _ = sb.Append(s, 0, index);

            while (index < s.Length)
            {
                _ = s[index] switch
                {
                    '\n' => sb.Append("\\n"),
                    '\r' => sb.Append("\\r"),
                    '"' => sb.Append("\\\""),
                    '\\' => sb.Append("\\\\"),
                    var other => sb.Append(other),
                };

                index++;
            }

            return sb.ToString();
        }

        private void Diag(DiagnosticDescriptor desc, Location? location) =>
            _reportDiagnostic(Diagnostic.Create(desc, location, Array.Empty<object?>()));

        private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs) =>
            _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
    }
}
