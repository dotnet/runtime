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
        /// Extracts essential data from a <see cref="GeneratorAttributeSyntaxContext"/> into a
        /// lightweight <see cref="RegexMethodInput"/> that does not hold Roslyn symbols.
        /// Returns <see langword="null"/> for cases that should be silently skipped (no diagnostic needed).
        /// </summary>
        private static RegexMethodInput? ExtractRegexMethodData(
            GeneratorAttributeSyntaxContext context, CancellationToken _)
        {
            // Handle indexer/accessor: needs diagnostic, pass through with flag
            if (context.TargetNode is IndexerDeclarationSyntax or AccessorDeclarationSyntax)
            {
                return new RegexMethodInput { IsIndexerOrAccessor = true, DiagnosticLocation = context.TargetNode.GetLocation() };
            }

            MemberDeclarationSyntax memberSyntax = (MemberDeclarationSyntax)context.TargetNode;

            // Prefilter: required Regex type must be available
            Compilation compilation = context.SemanticModel.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetBestTypeByMetadataName(RegexName);
            if (regexSymbol is null)
            {
                return null;
            }

            // Prefilter: must be inside a type declaration
            if (memberSyntax.Parent is not TypeDeclarationSyntax typeDec)
            {
                return null;
            }

            // Prefilter: must be a method or property symbol
            if (context.TargetSymbol is not (IMethodSymbol or IPropertySymbol))
            {
                return null;
            }

            // Extract attribute data
            ImmutableArray<AttributeData> boundAttributes = context.Attributes;
            int attributeCount = boundAttributes.Length;
            bool hasAttributeError = false;
            int argCount = 0;
            string? pattern = null;
            int? options = null;
            int? matchTimeout = null;
            string? cultureName = string.Empty;

            if (attributeCount == 1)
            {
                AttributeData attr = boundAttributes[0];
                hasAttributeError = attr.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error);

                if (!hasAttributeError)
                {
                    ImmutableArray<TypedConstant> items = attr.ConstructorArguments;
                    argCount = items.Length;
                    if (argCount >= 1)
                    {
                        pattern = items[0].Value as string;
                    }

                    if (argCount >= 2)
                    {
                        options = items[1].Value as int?;
                        if (argCount == 4)
                        {
                            matchTimeout = items[2].Value as int?;
                            cultureName = items[3].Value as string;
                        }
                        else if (argCount == 3)
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
                }
            }

            // Validate member signature and extract nullability
            bool hasValidSignature;
            bool nullableRegex;
            bool isProperty;
            if (context.TargetSymbol is IMethodSymbol regexMethodSymbol)
            {
                isProperty = false;
                hasValidSignature =
                    regexMethodSymbol.IsPartialDefinition &&
                    !regexMethodSymbol.IsAbstract &&
                    regexMethodSymbol.Parameters.Length == 0 &&
                    regexMethodSymbol.Arity == 0 &&
                    SymbolEqualityComparer.Default.Equals(regexMethodSymbol.ReturnType, regexSymbol);
                nullableRegex = regexMethodSymbol.ReturnNullableAnnotation == NullableAnnotation.Annotated;
            }
            else
            {
                IPropertySymbol regexPropertySymbol = (IPropertySymbol)context.TargetSymbol;
                isProperty = true;
                hasValidSignature =
                    memberSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) && // TODO: Switch to using regexPropertySymbol.IsPartialDefinition when available
                    !regexPropertySymbol.IsAbstract &&
                    regexPropertySymbol.SetMethod is null &&
                    SymbolEqualityComparer.Default.Equals(regexPropertySymbol.Type, regexSymbol);
                nullableRegex = regexPropertySymbol.NullableAnnotation == NullableAnnotation.Annotated;
            }

            // Determine the namespace the class is declared in, if any
            string? ns = context.TargetSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Build the declaring type hierarchy from outside in.
            RegexTypeSpec? parentSpec = null;
            TypeDeclarationSyntax? parentDec = typeDec.Parent as TypeDeclarationSyntax;
            if (parentDec is not null)
            {
                List<TypeDeclarationSyntax> parents = [];
                while (parentDec is not null && IsAllowedKind(parentDec.Kind()))
                {
                    parents.Add(parentDec);
                    parentDec = parentDec.Parent as TypeDeclarationSyntax;
                }

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

            RegexTypeSpec declaringType = new(
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}",
                Parent: parentSpec);

            CompilationData compilationData = compilation is CSharpCompilation { LanguageVersion: LanguageVersion langVersion, Options: CSharpCompilationOptions compilationOptions }
                ? new CompilationData(compilationOptions.AllowUnsafe, compilationOptions.CheckOverflow, langVersion)
                : default;

            return new RegexMethodInput
            {
                DiagnosticLocation = memberSyntax.GetLocation(),
                MemberName = context.TargetSymbol.Name,
                Modifiers = memberSyntax.Modifiers.ToString(),
                IsProperty = isProperty,
                NullableRegex = nullableRegex,
                HasValidSignature = hasValidSignature,
                AttributeCount = attributeCount,
                HasAttributeError = hasAttributeError,
                ArgCount = argCount,
                Pattern = pattern,
                Options = options,
                MatchTimeout = matchTimeout,
                CultureName = cultureName,
                DeclaringType = declaringType,
                CompilationData = compilationData,
            };

            static bool IsAllowedKind(SyntaxKind kind) => kind is
                SyntaxKind.ClassDeclaration or
                SyntaxKind.StructDeclaration or
                SyntaxKind.RecordDeclaration or
                SyntaxKind.RecordStructDeclaration or
                SyntaxKind.InterfaceDeclaration;
        }

        /// <summary>
        /// Parses all collected method inputs into an equatable generation model.
        /// This runs in a <c>Select</c> step of the incremental pipeline after <c>Collect</c>.
        /// </summary>
        private static (RegexGenerationSpec? Spec, ImmutableArray<Diagnostic> Diagnostics) Parse(
            ImmutableArray<RegexMethodInput?> inputs, CancellationToken cancellationToken)
        {
            List<RegexMethodSpec>? methods = null;
            List<Diagnostic>? diagnostics = null;

            foreach (RegexMethodInput? input in inputs)
            {
                if (input is null)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                RegexMethodSpec? spec = ParseMethod(input, ref diagnostics);
                if (spec is not null)
                {
                    (methods ??= []).Add(spec);
                }
            }

            if (methods is null)
            {
                return (null, diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty);
            }

            RegexGenerationSpec generationSpec = new()
            {
                RegexMethods = methods.ToImmutableEquatableSet(),
            };

            return (generationSpec, diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty);
        }

        /// <summary>
        /// Parses a single <see cref="RegexMethodInput"/> into a <see cref="RegexMethodSpec"/>.
        /// Returns <see langword="null"/> if the member should be skipped. Appends any diagnostics
        /// to the provided list.
        /// </summary>
        private static RegexMethodSpec? ParseMethod(
            RegexMethodInput input,
            ref List<Diagnostic>? diagnostics)
        {
            if (input.IsIndexerOrAccessor)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, input.DiagnosticLocation));
                return null;
            }

            if (input.AttributeCount != 1)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, input.DiagnosticLocation));
                return null;
            }

            if (input.HasAttributeError || input.ArgCount is 0 or > 4)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, input.DiagnosticLocation));
                return null;
            }

            if (input.Pattern is null || input.CultureName is null)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, "(null)"));
                return null;
            }

            if (!input.HasValidSignature)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, input.DiagnosticLocation));
                return null;
            }

            RegexOptions regexOptions = input.Options is not null ? (RegexOptions)input.Options : RegexOptions.None;

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
                regexOptionsWithPatternOptions = regexOptions | RegexParser.ParseOptionsInPattern(input.Pattern, regexOptions);
            }
            catch (Exception e)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, e.Message));
                return null;
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(input.CultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, "cultureName"));
                    return null;
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(input.CultureName);
                }
                catch (CultureNotFoundException)
                {
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, "cultureName"));
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
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, "options"));
                return null;
            }

            // Validate the timeout
            if (input.MatchTimeout is 0 or < -1)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, "matchTimeout"));
                return null;
            }

            // Parse the regex and build the equatable model.
            RegexTreeSpec? treeSpec;
            string? limitedSupportReason;
            try
            {
                RegexTree regexTree = RegexParser.Parse(input.Pattern, regexOptions | RegexOptions.Compiled, culture);
                AnalysisResults analysis = RegexTreeAnalyzer.Analyze(regexTree);

                if (!SupportsCodeGeneration(regexTree.Root, input.CompilationData.LanguageVersion, out limitedSupportReason))
                {
                    // Limited support — emit a boilerplate Regex wrapper, no tree needed
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.LimitedSourceGeneration, input.DiagnosticLocation));
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
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, input.DiagnosticLocation, e.Message));
                return null;
            }

            return new RegexMethodSpec
            {
                DeclaringType = input.DeclaringType!,
                IsProperty = input.IsProperty,
                MemberName = input.MemberName!,
                Modifiers = input.Modifiers!,
                NullableRegex = input.NullableRegex,
                Pattern = input.Pattern,
                Options = regexOptions,
                MatchTimeout = input.MatchTimeout,
                Tree = treeSpec,
                LimitedSupportReason = limitedSupportReason,
                CompilationData = input.CompilationData,
            };
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

        /// <summary>Lightweight data extracted from <see cref="GeneratorAttributeSyntaxContext"/> by the transform delegate.
        /// Does not hold Roslyn symbol or compilation references.</summary>
        private sealed class RegexMethodInput
        {
            public bool IsIndexerOrAccessor { get; init; }
            public required Location DiagnosticLocation { get; init; }
            public string? MemberName { get; init; }
            public string? Modifiers { get; init; }
            public bool IsProperty { get; init; }
            public bool NullableRegex { get; init; }
            public bool HasValidSignature { get; init; }
            public int AttributeCount { get; init; }
            public bool HasAttributeError { get; init; }
            public int ArgCount { get; init; }
            public string? Pattern { get; init; }
            public int? Options { get; init; }
            public int? MatchTimeout { get; init; }
            public string? CultureName { get; init; } = string.Empty;
            public RegexTypeSpec? DeclaringType { get; init; }
            public CompilationData CompilationData { get; init; }
        }

        /// <summary>Data about a regex, including a fully parsed RegexTree and subsequent analysis.</summary>
        private sealed class RegexMethod(RegexType declaringType, bool isProperty, string memberName, string modifiers, bool nullableRegex, string pattern, RegexOptions options, int? matchTimeout, RegexTree tree, AnalysisResults analysis, CompilationData compilationData)
        {
            public RegexType DeclaringType { get; } = declaringType;
            public bool IsProperty { get; } = isProperty;
            public string MemberName { get; } = memberName;
            public string Modifiers { get; } = modifiers;
            public bool NullableRegex { get; } = nullableRegex;
            public string Pattern { get; } = pattern;
            public RegexOptions Options { get; } = options;
            public int? MatchTimeout { get; } = matchTimeout;
            public RegexTree Tree { get; } = tree;
            public AnalysisResults Analysis { get; } = analysis;
            public CompilationData CompilationData { get; } = compilationData;
            public string? GeneratedName { get; set; }
            public bool IsDuplicate { get; set; }
        }

        /// <summary>A type holding a regex method.</summary>
        private sealed class RegexType(string keyword, string @namespace, string name)
        {
            public string Keyword { get; } = keyword;
            public string Namespace { get; } = @namespace;
            public string Name { get; } = name;
            public RegexType? Parent { get; set; }
        }
    }
}
