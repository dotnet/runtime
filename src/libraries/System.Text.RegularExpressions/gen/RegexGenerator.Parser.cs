// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        private const string RegexName = "System.Text.RegularExpressions.Regex";
        private const string RegexGeneratorAttributeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
            // We don't have a semantic model here, so the best we can do is say whether there are any attributes.
            node is MethodDeclarationSyntax { AttributeLists: { Count: > 0 } };

        private static MethodDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

            foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol &&
                        attributeSymbol.ContainingType.ToDisplayString() == RegexGeneratorAttributeName)
                    {
                        return methodDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        // Returns null if nothing to do, Diagnostic if there's an error to report, or RegexType if the type was analyzed successfully.
        private static object? GetRegexTypeToEmit(Compilation compilation, MethodDeclarationSyntax methodSyntax, CancellationToken cancellationToken)
        {
            INamedTypeSymbol? regexSymbol = compilation.GetBestTypeByMetadataName(RegexName);
            INamedTypeSymbol? regexGeneratorAttributeSymbol = compilation.GetBestTypeByMetadataName(RegexGeneratorAttributeName);
            if (regexSymbol is null || regexGeneratorAttributeSymbol is null)
            {
                // Required types aren't available
                return null;
            }

            TypeDeclarationSyntax? typeDec = methodSyntax.Parent as TypeDeclarationSyntax;
            if (typeDec is null)
            {
                return null;
            }

            SemanticModel sm = compilation.GetSemanticModel(methodSyntax.SyntaxTree);

            IMethodSymbol? regexMethodSymbol = sm.GetDeclaredSymbol(methodSyntax, cancellationToken) as IMethodSymbol;
            if (regexMethodSymbol is null)
            {
                return null;
            }

            ImmutableArray<AttributeData>? boundAttributes = regexMethodSymbol.GetAttributes();
            if (boundAttributes is null || boundAttributes.Value.Length == 0)
            {
                return null;
            }

            bool attributeFound = false;
            string? pattern = null;
            int? options = null;
            int? matchTimeout = null;
            foreach (AttributeData attributeData in boundAttributes)
            {
                if (attributeData.AttributeClass?.Equals(regexGeneratorAttributeSymbol) != true)
                {
                    continue;
                }

                if (attributeData.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexGeneratorAttribute, methodSyntax.GetLocation());
                }

                if (pattern is not null)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.MultipleRegexGeneratorAttributes, methodSyntax.GetLocation());
                }

                ImmutableArray<TypedConstant> items = attributeData.ConstructorArguments;
                if (items.Length == 0 || items.Length > 3)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexGeneratorAttribute, methodSyntax.GetLocation());
                }

                attributeFound = true;
                pattern = items[0].Value as string;
                if (items.Length >= 2)
                {
                    options = items[1].Value as int?;
                    if (items.Length == 3)
                    {
                        matchTimeout = items[2].Value as int?;
                    }
                }
            }

            if (!attributeFound)
            {
                return null;
            }

            if (pattern is null)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "(null)");
            }

            if (!regexMethodSymbol.IsPartialDefinition ||
                regexMethodSymbol.Parameters.Length != 0 ||
                regexMethodSymbol.Arity != 0 ||
                !regexMethodSymbol.ReturnType.Equals(regexSymbol))
            {
                return Diagnostic.Create(DiagnosticDescriptors.RegexMethodMustHaveValidSignature, methodSyntax.GetLocation());
            }

            if (typeDec.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: < LanguageVersion.CSharp10 })
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidLangVersion, methodSyntax.GetLocation());
            }

            RegexOptions regexOptions = RegexOptions.Compiled | (options is not null ? (RegexOptions)options : RegexOptions.None);

            // TODO: This is going to include the culture that's current at the time of compilation.
            // What should we do about that?  We could:
            // - say not specifying CultureInvariant is invalid if anything about options or the expression will look at culture
            // - fall back to not generating source if it's not specified
            // - just use whatever culture is present at build time
            // - devise a new way of not using the culture present at build time
            // - ...
            CultureInfo culture = (regexOptions & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

            // Validate the options
            const RegexOptions SupportedOptions =
                RegexOptions.IgnoreCase |
                RegexOptions.Multiline |
                RegexOptions.ExplicitCapture |
                RegexOptions.Compiled |
                RegexOptions.Singleline |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.RightToLeft |
#if DEBUG
                RegexOptions.Debug |
#endif
                RegexOptions.ECMAScript |
                RegexOptions.CultureInvariant |
                RegexOptions.NonBacktracking;
            if ((regexOptions & ~SupportedOptions) != 0)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "options");
            }

            // Validate the timeout
            if (matchTimeout is 0 or < -1)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "matchTimeout");
            }

            // Parse the input pattern
            RegexCode code;
            try
            {
                code = RegexWriter.Write(RegexParser.Parse(pattern, regexOptions, culture), culture);
            }
            catch (Exception e)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), e.Message);
            }

            // Determine the namespace the class is declared in, if any
            string? ns = regexMethodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            var regexMethod = new RegexMethod(
                regexMethodSymbol.Name,
                methodSyntax.Modifiers.ToString(),
                pattern,
                regexOptions,
                matchTimeout ?? Timeout.Infinite,
                code);

            var regexType = new RegexType(
                regexMethod,
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}",
                typeDec.ConstraintClauses.ToString());

            RegexType current = regexType;
            var parent = typeDec.Parent as TypeDeclarationSyntax;

            while (parent is not null && IsAllowedKind(parent.Kind()))
            {
                current.ParentClass = new RegexType(
                    null,
                    parent is RecordDeclarationSyntax rds2 ? $"{parent.Keyword.ValueText} {rds2.ClassOrStructKeyword}" : parent.Keyword.ValueText,
                    ns ?? string.Empty,
                    $"{parent.Identifier}{parent.TypeParameterList}",
                    parent.ConstraintClauses.ToString());

                current = current.ParentClass;
                parent = parent.Parent as TypeDeclarationSyntax;
            }

            return regexType;

            static bool IsAllowedKind(SyntaxKind kind) =>
                kind == SyntaxKind.ClassDeclaration ||
                kind == SyntaxKind.StructDeclaration ||
                kind == SyntaxKind.RecordDeclaration ||
                kind == SyntaxKind.RecordStructDeclaration ||
                kind == SyntaxKind.InterfaceDeclaration;
        }

        /// <summary>A regex method.</summary>
        internal sealed record RegexMethod(string MethodName, string Modifiers, string Pattern, RegexOptions Options, int MatchTimeout, RegexCode Code);

        /// <summary>A type holding a regex method.</summary>
        internal sealed record RegexType(RegexMethod? Method, string Keyword, string Namespace, string Name, string Constraints)
        {
            public RegexType? ParentClass { get; set; }
        }
    }
}
