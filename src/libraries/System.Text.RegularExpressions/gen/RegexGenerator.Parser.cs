// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
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

        /// <summary>
        /// Returns null if nothing to do, <see cref="DiagnosticData"/> if there's an error to report,
        /// or <see cref="RegexPatternAndSyntax"/> if the type was analyzed successfully.
        /// </summary>
        private static object? GetRegexMethodDataOrFailureDiagnostic(
            GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.TargetNode is IndexerDeclarationSyntax or AccessorDeclarationSyntax)
            {
                // We allow these to be used as a target node for the sole purpose
                // of being able to flag invalid use when [GeneratedRegex] is applied incorrectly.
                // Otherwise, if the ForAttributeWithMetadataName call excluded these, [GeneratedRegex]
                // could be applied to them and we wouldn't be able to issue a diagnostic.
                return new DiagnosticData(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, GetComparableLocation(context.TargetNode));
            }

            var memberSyntax = (MemberDeclarationSyntax)context.TargetNode;
            SemanticModel sm = context.SemanticModel;

            Compilation compilation = sm.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetBestTypeByMetadataName(RegexName);

            if (regexSymbol is null)
            {
                // Required types aren't available
                return null;
            }

            TypeDeclarationSyntax? typeDec = memberSyntax.Parent as TypeDeclarationSyntax;
            if (typeDec is null)
            {
                return null;
            }

            ISymbol? regexMemberSymbol = context.TargetSymbol is IMethodSymbol or IPropertySymbol ? context.TargetSymbol : null;
            if (regexMemberSymbol is null)
            {
                return null;
            }

            ImmutableArray<AttributeData> boundAttributes = context.Attributes;
            if (boundAttributes.Length != 1)
            {
                return new DiagnosticData(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, GetComparableLocation(memberSyntax));
            }
            AttributeData generatedRegexAttr = boundAttributes[0];

            if (generatedRegexAttr.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
            {
                return new DiagnosticData(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, GetComparableLocation(memberSyntax));
            }

            ImmutableArray<TypedConstant> items = generatedRegexAttr.ConstructorArguments;
            if (items.Length is 0 or > 4)
            {
                return new DiagnosticData(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, GetComparableLocation(memberSyntax));
            }

            string? pattern = items[0].Value as string;
            int? options = null;
            int? matchTimeout = null;
            string? cultureName = string.Empty;
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
                    if (items[2].Type?.SpecialType == SpecialType.System_Int32)
                    {
                        matchTimeout = items[2].Value as int?;
                    }
                    else
                    {
                        cultureName = items[2].Value as string;
                    }
                }
            }

            if (pattern is null || cultureName is null)
            {
                return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), "(null)");
            }

            bool nullableRegex;
            if (regexMemberSymbol is IMethodSymbol regexMethodSymbol)
            {
                if (!regexMethodSymbol.IsPartialDefinition ||
                    regexMethodSymbol.IsAbstract ||
                    regexMethodSymbol.Parameters.Length != 0 ||
                    regexMethodSymbol.Arity != 0 ||
                    !SymbolEqualityComparer.Default.Equals(regexMethodSymbol.ReturnType, regexSymbol))
                {
                    return new DiagnosticData(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, GetComparableLocation(memberSyntax));
                }

                nullableRegex = regexMethodSymbol.ReturnNullableAnnotation == NullableAnnotation.Annotated;
            }
            else
            {
                Debug.Assert(regexMemberSymbol is IPropertySymbol);
                IPropertySymbol regexPropertySymbol = (IPropertySymbol)regexMemberSymbol;
                if (!memberSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) || // TODO: Switch to using regexPropertySymbol.IsPartialDefinition when available
                    regexPropertySymbol.IsAbstract ||
                    regexPropertySymbol.SetMethod is not null ||
                    !SymbolEqualityComparer.Default.Equals(regexPropertySymbol.Type, regexSymbol))
                {
                    return new DiagnosticData(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, GetComparableLocation(memberSyntax));
                }

                nullableRegex = regexPropertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
            }

            RegexOptions regexOptions = options is not null ? (RegexOptions)options : RegexOptions.None;

            // If RegexOptions.IgnoreCase was specified or the inline ignore case option `(?i)` is present in the pattern, then we will (in priority order):
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
                return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), e.Message);
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(cultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), "cultureName");
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), "cultureName");
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
                RegexOptions.Singleline |
                RegexOptions.AnyNewLine;
            if ((regexOptions & ~SupportedOptions) != 0)
            {
                return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), "options");
            }

            // Validate the timeout
            if (matchTimeout is 0 or < -1)
            {
                return new DiagnosticData(DiagnosticDescriptors.InvalidRegexArguments, GetComparableLocation(memberSyntax), "matchTimeout");
            }

            // Determine the namespace the class is declared in, if any
            string? ns = regexMemberSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            var regexType = new RegexType(
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}");

            var compilationData = compilation is CSharpCompilation { LanguageVersion: LanguageVersion langVersion, Options: CSharpCompilationOptions compilationOptions }
                ? new CompilationData(compilationOptions.AllowUnsafe, compilationOptions.CheckOverflow, langVersion)
                : default;

            var result = new RegexPatternAndSyntax(
                regexType,
                IsProperty: regexMemberSymbol is IPropertySymbol,
                GetComparableLocation(memberSyntax),
                regexMemberSymbol.Name,
                memberSyntax.Modifiers.ToString(),
                nullableRegex,
                pattern,
                regexOptions,
                matchTimeout,
                culture,
                compilationData);

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

            return result;

            static bool IsAllowedKind(SyntaxKind kind) => kind is
                SyntaxKind.ClassDeclaration or
                SyntaxKind.StructDeclaration or
                SyntaxKind.RecordDeclaration or
                SyntaxKind.RecordStructDeclaration or
                SyntaxKind.InterfaceDeclaration;

            // Get a Location object that doesn't store a reference to the compilation.
            // That allows it to compare equally across compilations.
            static Location GetComparableLocation(SyntaxNode syntax)
            {
                var location = syntax.GetLocation();
                return Location.Create(location.SourceTree?.FilePath ?? string.Empty, location.SourceSpan, location.GetLineSpan().Span);
            }
        }

        /// <summary>Data about a regex directly from the GeneratedRegex attribute.</summary>
        internal sealed record RegexPatternAndSyntax(RegexType DeclaringType, bool IsProperty, Location DiagnosticLocation, string MemberName, string Modifiers, bool NullableRegex, string Pattern, RegexOptions Options, int? MatchTimeout, CultureInfo Culture, CompilationData CompilationData);

        /// <summary>Data about a regex, including a fully parsed RegexTree and subsequent analysis.</summary>
        internal sealed record RegexMethod(RegexType DeclaringType, bool IsProperty, Location DiagnosticLocation, string MemberName, string Modifiers, bool NullableRegex, string Pattern, RegexOptions Options, int? MatchTimeout, RegexTree Tree, AnalysisResults Analysis, CompilationData CompilationData)
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
