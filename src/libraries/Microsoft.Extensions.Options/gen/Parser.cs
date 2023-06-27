// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        private readonly Dictionary<ITypeSymbol, ValidatorType> _synthesizedValidators = new(SymbolEqualityComparer.Default);
        private readonly HashSet<ITypeSymbol> _visitedModelTypes = new(SymbolEqualityComparer.Default);

        public Parser(
            Compilation compilation,
            Action<Diagnostic> reportDiagnostic,
            SymbolHolder symbolHolder,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _cancellationToken = cancellationToken;
            _reportDiagnostic = reportDiagnostic;
            _symbolHolder = symbolHolder;
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

                            var membersToValidate = GetMembersToValidate(modelType, true);
                            if (membersToValidate.Count == 0)
                            {
                                // this type lacks any eligible members
                                Diag(DiagDescriptors.NoEligibleMembersFromValidator, syntax.GetLocation(), modelType.ToString(), validatorType.ToString());
                                continue;
                            }

                            modelsValidatorTypeValidates.Add(new ValidatedModel(
                                GetFQN(modelType),
                                modelType.Name,
                                ModelSelfValidates(modelType),
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
                            validatorType.ContainingNamespace.IsGlobalNamespace ? string.Empty : validatorType.ContainingNamespace.ToString(),
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

            foreach (var implementingInterface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implementingInterface.OriginalDefinition, _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)))
                {
                    return implementingInterface.TypeArguments.First();
                }
            }

            return null;
        }

        private List<ValidatedMember> GetMembersToValidate(ITypeSymbol modelType, bool speculate)
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
            while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
            {
                var baseMembers = baseType.GetMembers().Where(m => !addedMembers.Contains(m.Name));
                members.AddRange(baseMembers);
                addedMembers.UnionWith(baseMembers.Select(m => m.Name));
                baseType = baseType.BaseType;
            }

            var membersToValidate = new List<ValidatedMember>();
            foreach (var member in members)
            {
                var memberInfo = GetMemberInfo(member, speculate);
                if (memberInfo is not null)
                {
                    if (member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
                    {
                        Diag(DiagDescriptors.MemberIsInaccessible, member.Locations.First(), member.Name);
                        continue;
                    }

                    membersToValidate.Add(memberInfo);
                }
            }

            return membersToValidate;
        }

        private ValidatedMember? GetMemberInfo(ISymbol member, bool speculate)
        {
            ITypeSymbol memberType;
            switch (member)
            {
                case IPropertySymbol prop:
                    memberType = prop.Type;
                    break;
                case IFieldSymbol field:
                    if (field.AssociatedSymbol is not null)
                    {
                        // a backing field for a property, don't need those
                        return null;
                    }

                    memberType = field.Type;
                    break;
                default:
                    // we only care about properties and fields
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
                        transValidatorTypeName = AddSynthesizedValidator(memberType, member);
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
                        enumerationValidatorTypeName = AddSynthesizedValidator(enumeratedType, member);
                    }

                    // pop the stack
                    _ = _visitedModelTypes.Remove(enumeratedType.WithNullableAnnotation(NullableAnnotation.None));
                }
                else if (ConvertTo(attributeType, _symbolHolder.ValidationAttributeSymbol))
                {
                    var validationAttr = new ValidationAttributeInfo(attributeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    validationAttrs.Add(validationAttr);

                    foreach (var constructorArgument in attribute.ConstructorArguments)
                    {
                        validationAttr.ConstructorArguments.Add(GetArgumentExpression(constructorArgument.Type!, constructorArgument.Value));
                    }

                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        validationAttr.Properties.Add(namedArgument.Key, GetArgumentExpression(namedArgument.Value.Type!, namedArgument.Value.Value));
                    }
                }
            }

            // generate a warning if the field/property seems like it should be transitively validated
            if (transValidatorTypeName == null && speculate && memberType.SpecialType == SpecialType.None)
            {
                if (!HasOpenGenerics(memberType, out var genericType))
                {
                    var membersToValidate = GetMembersToValidate(memberType, false);
                    if (membersToValidate.Count > 0)
                    {
                        Diag(DiagDescriptors.PotentiallyMissingTransitiveValidation, member.GetLocation(), memberType.Name, member.Name);
                    }
                }
            }

            // generate a warning if the field/property seems like it should be enumerated
            if (enumerationValidatorTypeName == null && speculate)
            {
                var enumeratedType = GetEnumeratedType(memberType);
                if (enumeratedType is not null)
                {
                    if (!HasOpenGenerics(enumeratedType, out var genericType))
                    {
                        var membersToValidate = GetMembersToValidate(enumeratedType, false);
                        if (membersToValidate.Count > 0)
                        {
                            Diag(DiagDescriptors.PotentiallyMissingEnumerableValidation, member.GetLocation(), enumeratedType.Name, member.Name);
                        }
                    }
                }
            }

            if (validationAttrs.Count > 0 || transValidatorTypeName is not null || enumerationValidatorTypeName is not null)
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

        private string? AddSynthesizedValidator(ITypeSymbol modelType, ISymbol member)
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

            var membersToValidate = GetMembersToValidate(mt, true);
            if (membersToValidate.Count == 0)
            {
                // this type lacks any eligible members
                Diag(DiagDescriptors.NoEligibleMember, member.GetLocation(), mt.ToString(), member.ToString());
                return null;
            }

            var model = new ValidatedModel(
                GetFQN(mt),
                mt.Name,
                false,
                membersToValidate);

            var validatorTypeName = "__" + mt.Name + "Validator__";

            var result = new ValidatorType(
                mt.ContainingNamespace.IsGlobalNamespace ? string.Empty : mt.ContainingNamespace.ToString(),
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
                return $@"""{EscapeString(value.ToString())}""";
            }

            if (type.SpecialType == SpecialType.System_Char)
            {
                return $@"'{EscapeString(value.ToString())}'";
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

        private void Diag(DiagnosticDescriptor desc, Location? location)
        {
            _reportDiagnostic(Diagnostic.Create(desc, location, Array.Empty<object?>()));
        }

        private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
        {
            _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
        }
    }
}
