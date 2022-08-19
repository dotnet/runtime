// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        private const string RegexName = "System.Text.RegularExpressions.Regex";
        private const string GeneratedRegexAttributeName = "System.Text.RegularExpressions.GeneratedRegexAttribute";

        // Returns null if nothing to do, Diagnostic if there's an error to report, or RegexType if the type was analyzed successfully.
        private static object? GetSemanticTargetForGeneration(
            GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
            SemanticModel sm = context.SemanticModel;

            Compilation compilation = sm.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetBestTypeByMetadataName(RegexName);
            INamedTypeSymbol? generatedRegexAttributeSymbol = compilation.GetBestTypeByMetadataName(GeneratedRegexAttributeName);

            if (regexSymbol is null || generatedRegexAttributeSymbol is null)
            {
                // Required types aren't available
                return null;
            }

            TypeDeclarationSyntax? typeDec = methodSyntax.Parent as TypeDeclarationSyntax;
            if (typeDec is null)
            {
                return null;
            }

            IMethodSymbol regexMethodSymbol = context.TargetSymbol as IMethodSymbol;
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
            string? cultureName = string.Empty;
            foreach (AttributeData attributeData in boundAttributes)
            {
                if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, generatedRegexAttributeSymbol))
                {
                    continue;
                }

                if (attributeData.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, methodSyntax.GetLocation());
                }

                if (pattern is not null)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, methodSyntax.GetLocation());
                }

                ImmutableArray<TypedConstant> items = attributeData.ConstructorArguments;
                if (items.Length == 0 || items.Length > 4)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, methodSyntax.GetLocation());
                }

                attributeFound = true;
                pattern = items[0].Value as string;
                if (items.Length >= 2)
                {
                    options = items[1].Value as int?;
                    if (items.Length == 4)
                    {
                        matchTimeout = items[2].Value as int?;
                        cultureName = items[3].Value as string;
                    }
                    // If there are 3 parameters, we need to check if the third argument is
                    // int matchTimeoutMilliseconds, or string cultureName.
                    else if (items.Length == 3)
                    {
                        if (items[2].Type.SpecialType == SpecialType.System_Int32)
                        {
                            matchTimeout = items[2].Value as int?;
                        }
                        else
                        {
                            cultureName = items[2].Value as string;
                        }
                    }
                }
            }

            if (!attributeFound)
            {
                return null;
            }

            if (pattern is null || cultureName is null)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "(null)");
            }

            if (!regexMethodSymbol.IsPartialDefinition ||
                regexMethodSymbol.IsAbstract ||
                regexMethodSymbol.Parameters.Length != 0 ||
                regexMethodSymbol.Arity != 0 ||
                !SymbolEqualityComparer.Default.Equals(regexMethodSymbol.ReturnType, regexSymbol))
            {
                return Diagnostic.Create(DiagnosticDescriptors.RegexMethodMustHaveValidSignature, methodSyntax.GetLocation());
            }

            RegexOptions regexOptions = options is not null ? (RegexOptions)options : RegexOptions.None;

            // If  RegexOptions.IgnoreCase was specified or the inline ignore case option `(?i)` is present in the pattern, then we will (in priority order):
            // - If a culture name was passed in:
            //   - If RegexOptions.CultureInvariant was also passed in, then we emit a diagnostic due to the explicit conflict.
            //   - We try to initialize a culture using the passed in culture name to be used for case-sensitive comparisons. If
            //     the culture name is invalid, we'll emit a diagnostic.
            // - Default to use Invariant Culture if no culture name was passed in.
            CultureInfo culture = CultureInfo.InvariantCulture;
            RegexOptions regexOptionsWithPatternOptions;
            try
            {
                regexOptionsWithPatternOptions = regexOptions | RegexParser.ParseOptionsInPattern(pattern, regexOptions);
            }
            catch (Exception e)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), e.Message);
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(cultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "cultureName");
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), "cultureName");
                }
            }

            // Validate the options
            const RegexOptions SupportedOptions =
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant |
                RegexOptions.ECMAScript |
                RegexOptions.ExplicitCapture |
                RegexOptions.IgnoreCase |
                RegexOptions.IgnorePatternWhitespace |
                RegexOptions.Multiline |
                RegexOptions.NonBacktracking |
                RegexOptions.RightToLeft |
                RegexOptions.Singleline;
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
            RegexTree regexTree;
            AnalysisResults analysis;
            try
            {
                regexTree = RegexParser.Parse(pattern, regexOptions | RegexOptions.Compiled, culture); // make sure Compiled is included to get all optimizations applied to it
                analysis = RegexTreeAnalyzer.Analyze(regexTree);
            }
            catch (Exception e)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, methodSyntax.GetLocation(), e.Message);
            }

            // Determine the namespace the class is declared in, if any
            string? ns = regexMethodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            var regexType = new RegexType(
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}");

            var regexMethod = new RegexMethod(
                regexType,
                methodSyntax,
                regexMethodSymbol.Name,
                methodSyntax.Modifiers.ToString(),
                pattern,
                regexOptions,
                matchTimeout,
                regexTree,
                analysis);

            RegexType current = regexType;
            var parent = typeDec.Parent as TypeDeclarationSyntax;

            while (parent is not null && IsAllowedKind(parent.Kind()))
            {
                current.Parent = new RegexType(
                    parent is RecordDeclarationSyntax rds2 ? $"{parent.Keyword.ValueText} {rds2.ClassOrStructKeyword}" : parent.Keyword.ValueText,
                    ns ?? string.Empty,
                    $"{parent.Identifier}{parent.TypeParameterList}");

                current = current.Parent;
                parent = parent.Parent as TypeDeclarationSyntax;
            }

            return regexMethod;

            static bool IsAllowedKind(SyntaxKind kind) =>
                kind == SyntaxKind.ClassDeclaration ||
                kind == SyntaxKind.StructDeclaration ||
                kind == SyntaxKind.RecordDeclaration ||
                kind == SyntaxKind.RecordStructDeclaration ||
                kind == SyntaxKind.InterfaceDeclaration;
        }

        /// <summary>A regex method.</summary>
        internal sealed record RegexMethod(RegexType DeclaringType, MethodDeclarationSyntax MethodSyntax, string MethodName, string Modifiers, string Pattern, RegexOptions Options, int? MatchTimeout, RegexTree Tree, AnalysisResults Analysis)
        {
            public string? GeneratedName { get; set; }
            public bool IsDuplicate { get; set; }
        }

        /// <summary>A type holding a regex method.</summary>
        internal sealed record RegexType(string Keyword, string Namespace, string Name)
        {
            public RegexType? Parent { get; set; }
        }
    }
}
