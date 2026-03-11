// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        private const string RegexName = "System.Text.RegularExpressions.Regex";
        private const string GeneratedRegexAttributeName = "System.Text.RegularExpressions.GeneratedRegexAttribute";

        /// <summary>
        /// Parses all collected attribute contexts into an equatable generation model.
        /// This runs in a <c>Select</c> step of the incremental pipeline after <c>Collect</c>.
        /// </summary>
        private static (RegexGenerationSpec? Spec, ImmutableArray<Diagnostic> Diagnostics) Parse(
            ImmutableArray<GeneratorAttributeSyntaxContext> contexts, CancellationToken cancellationToken)
        {
            ImmutableArray<Diagnostic>.Builder? diagnostics = null;
            HashSet<RegexMethodSpec>? methods = null;

            foreach (GeneratorAttributeSyntaxContext context in contexts)
            {
                RegexMethodSpec? spec = ParseMethod(context, ref diagnostics, cancellationToken);
                if (spec is not null)
                {
                    (methods ??= new HashSet<RegexMethodSpec>()).Add(spec);
                }
            }

            if (methods is null)
            {
                return (null, diagnostics?.ToImmutable() ?? ImmutableArray<Diagnostic>.Empty);
            }

            var generationSpec = new RegexGenerationSpec
            {
                RegexMethods = ImmutableEquatableSet<RegexMethodSpec>.UnsafeCreateFromHashSet(methods),
            };

            return (generationSpec, diagnostics?.ToImmutable() ?? ImmutableArray<Diagnostic>.Empty);
        }

        /// <summary>
        /// Parses a single <see cref="GeneratorAttributeSyntaxContext"/> into a <see cref="RegexMethodSpec"/>.
        /// Returns <see langword="null"/> if the member should be skipped. Appends any diagnostics
        /// to the provided builder.
        /// </summary>
        private static RegexMethodSpec? ParseMethod(
            GeneratorAttributeSyntaxContext context,
            ref ImmutableArray<Diagnostic>.Builder? diagnostics,
            CancellationToken cancellationToken)
        {
            if (context.TargetNode is IndexerDeclarationSyntax or AccessorDeclarationSyntax)
            {
                // We allow these to be used as a target node for the sole purpose
                // of being able to flag invalid use when [GeneratedRegex] is applied incorrectly.
                // Otherwise, if the ForAttributeWithMetadataName call excluded these, [GeneratedRegex]
                // could be applied to them and we wouldn't be able to issue a diagnostic.
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, context.TargetNode.GetLocation()));
                return null;
            }

            var memberSyntax = (MemberDeclarationSyntax)context.TargetNode;
            SemanticModel sm = context.SemanticModel;
            cancellationToken.ThrowIfCancellationRequested();

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
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, memberSyntax.GetLocation()));
                return null;
            }
            AttributeData generatedRegexAttr = boundAttributes[0];

            if (generatedRegexAttr.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
            {
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberSyntax.GetLocation()));
                return null;
            }

            ImmutableArray<TypedConstant> items = generatedRegexAttr.ConstructorArguments;
            if (items.Length is 0 or > 4)
            {
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberSyntax.GetLocation()));
                return null;
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
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "(null)"));
                return null;
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
                    AddDiagnostic(ref diagnostics,
                        Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberSyntax.GetLocation()));
                    return null;
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
                    AddDiagnostic(ref diagnostics,
                        Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberSyntax.GetLocation()));
                    return null;
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
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), e.Message));
                return null;
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(cultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    AddDiagnostic(ref diagnostics,
                        Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "cultureName"));
                    return null;
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    AddDiagnostic(ref diagnostics,
                        Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "cultureName"));
                    return null;
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
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "options"));
                return null;
            }

            // Validate the timeout
            if (matchTimeout is 0 or < -1)
            {
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "matchTimeout"));
                return null;
            }

            // Determine the namespace the class is declared in, if any
            string? ns = regexMemberSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Build the declaring type hierarchy from outside in.
            RegexTypeSpec? parentSpec = null;
            TypeDeclarationSyntax? parentDec = typeDec.Parent as TypeDeclarationSyntax;
            if (parentDec is not null)
            {
                // Collect parent types from inner to outer.
                var parents = new System.Collections.Generic.List<TypeDeclarationSyntax>();
                while (parentDec is not null && IsAllowedKind(parentDec.Kind()))
                {
                    parents.Add(parentDec);
                    parentDec = parentDec.Parent as TypeDeclarationSyntax;
                }

                // Build chain from outermost to innermost.
                for (int i = parents.Count - 1; i >= 0; i--)
                {
                    TypeDeclarationSyntax p = parents[i];
                    parentSpec = new RegexTypeSpec(
                        p is RecordDeclarationSyntax rds2 ? $"{p.Keyword.ValueText} {rds2.ClassOrStructKeyword}" : p.Keyword.ValueText,
                        ns ?? string.Empty,
                        $"{p.Identifier}{p.TypeParameterList}",
                        Parent: parentSpec);
                }
            }

            var regexTypeSpec = new RegexTypeSpec(
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}",
                Parent: parentSpec);

            var compilationData = compilation is CSharpCompilation { LanguageVersion: LanguageVersion langVersion, Options: CSharpCompilationOptions compilationOptions }
                ? new CompilationData(compilationOptions.AllowUnsafe, compilationOptions.CheckOverflow, langVersion)
                : default;

            // Parse the regex and build the equatable model.
            RegexTreeSpec? treeSpec;
            string? limitedSupportReason;
            try
            {
                RegexTree regexTree = RegexParser.Parse(pattern, regexOptions | RegexOptions.Compiled, culture);
                AnalysisResults analysis = RegexTreeAnalyzer.Analyze(regexTree);

                if (!SupportsCodeGeneration(regexTree.Root, compilationData.LanguageVersion, out limitedSupportReason))
                {
                    // Limited support — emit a boilerplate Regex wrapper, no tree needed
                    AddDiagnostic(ref diagnostics,
                        Diagnostic.Create(DiagnosticDescriptors.LimitedSourceGeneration, memberSyntax.GetLocation()));
                    treeSpec = null;
                }
                else
                {
                    treeSpec = CreateRegexTreeSpec(regexTree, analysis);
                    limitedSupportReason = null;
                }
            }
            catch (Exception e)
            {
                AddDiagnostic(ref diagnostics,
                    Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), e.Message));
                return null;
            }

            return new RegexMethodSpec
            {
                DeclaringType = regexTypeSpec,
                IsProperty = regexMemberSymbol is IPropertySymbol,
                MemberName = regexMemberSymbol.Name,
                Modifiers = memberSyntax.Modifiers.ToString(),
                NullableRegex = nullableRegex,
                Pattern = pattern,
                Options = regexOptions,
                MatchTimeout = matchTimeout,
                Tree = treeSpec,
                LimitedSupportReason = limitedSupportReason,
                CompilationData = compilationData,
            };

            static bool IsAllowedKind(SyntaxKind kind) => kind is
                SyntaxKind.ClassDeclaration or
                SyntaxKind.StructDeclaration or
                SyntaxKind.RecordDeclaration or
                SyntaxKind.RecordStructDeclaration or
                SyntaxKind.InterfaceDeclaration;
        }

        /// <summary>Determines whether the passed in node supports C# code generation.</summary>
        /// <remarks>
        // It also provides a human-readable string to explain the reason. It will be emitted by the source generator
        // as a comment into the C# code, hence there's no need to localize.
        /// </remarks>
        private static bool SupportsCodeGeneration(RegexNode root, LanguageVersion languageVersion, [NotNullWhen(false)] out string? reason)
        {
            if (languageVersion < LanguageVersion.CSharp11)
            {
                reason = "the language version must be C# 11 or higher.";
                return false;
            }

            if (!root.SupportsCompilation(out reason))
            {
                // If the pattern doesn't support Compilation, then code generation won't be supported either.
                return false;
            }

            if (HasCaseInsensitiveBackReferences(root))
            {
                // For case-insensitive patterns, we use our internal Regex case equivalence table when doing character comparisons.
                // Most of the use of this table is done at Regex construction time by substituting all characters that are involved in
                // case conversions into sets that contain all possible characters that could match. That said, there is still one case
                // where you may need to do case-insensitive comparisons at match time which is the case for backreferences. For that reason,
                // and given the Regex case equivalence table is internal and can't be called by the source generated emitted type, if
                // the pattern contains case-insensitive backreferences, we won't try to create a source generated Regex-derived type.
                reason = "the expression contains case-insensitive backreferences which are not supported by the source generator";
                return false;
            }

            // If Compilation is supported and pattern doesn't have case insensitive backreferences, then code generation is supported.
            reason = null;
            return true;

            static bool HasCaseInsensitiveBackReferences(RegexNode node)
            {
                if (node.Kind is RegexNodeKind.Backreference && (node.Options & RegexOptions.IgnoreCase) != 0)
                {
                    return true;
                }

                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    // This recursion shouldn't hit issues with stack depth since this gets checked after
                    // SupportCompilation has ensured that the max depth is not greater than 40.
                    if (HasCaseInsensitiveBackReferences(node.Child(i)))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Adds a diagnostic to the builder, lazily initializing it if necessary.</summary>
        private static void AddDiagnostic(ref ImmutableArray<Diagnostic>.Builder? diagnostics, Diagnostic diagnostic)
            => (diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>()).Add(diagnostic);

        /// <summary>Data about a regex, including a fully parsed RegexTree and subsequent analysis.</summary>
        internal sealed record RegexMethod(RegexType DeclaringType, bool IsProperty, string MemberName, string Modifiers, bool NullableRegex, string Pattern, RegexOptions Options, int? MatchTimeout, RegexTree Tree, AnalysisResults Analysis, CompilationData CompilationData)
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
