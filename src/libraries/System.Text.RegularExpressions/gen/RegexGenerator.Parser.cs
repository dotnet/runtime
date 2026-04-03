// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics.Hashing;
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
            List<RegexMethodSpec>? methods = null;
            List<Diagnostic>? diagnostics = null;

            foreach (GeneratorAttributeSyntaxContext context in contexts)
            {
                RegexMethod? regexMethod = ParseMethod(context, ref diagnostics, cancellationToken);
                if (regexMethod is not null)
                {
                    (methods ??= []).Add(new(regexMethod));
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
        /// Parses a single <see cref="GeneratorAttributeSyntaxContext"/> into a <see cref="RegexMethod"/>.
        /// Returns <see langword="null"/> if the member should be skipped. Appends any diagnostics
        /// to the provided builder.
        /// </summary>
        private static RegexMethod? ParseMethod(
            GeneratorAttributeSyntaxContext context,
            ref List<Diagnostic>? diagnostics,
            CancellationToken cancellationToken)
        {
            if (context.TargetNode is IndexerDeclarationSyntax or AccessorDeclarationSyntax)
            {
                // We allow these to be used as a target node for the sole purpose
                // of being able to flag invalid use when [GeneratedRegex] is applied incorrectly.
                // Otherwise, if the ForAttributeWithMetadataName call excluded these, [GeneratedRegex]
                // could be applied to them and we wouldn't be able to issue a diagnostic.
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, context.TargetNode.GetLocation()));
                return null;
            }

            MemberDeclarationSyntax memberSyntax = (MemberDeclarationSyntax)context.TargetNode;
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
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, memberSyntax.GetLocation()));
                return null;
            }
            AttributeData generatedRegexAttr = boundAttributes[0];

            if (generatedRegexAttr.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberSyntax.GetLocation()));
                return null;
            }

            ImmutableArray<TypedConstant> items = generatedRegexAttr.ConstructorArguments;
            if (items.Length is 0 or > 4)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberSyntax.GetLocation()));
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
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "(null)"));
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
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberSyntax.GetLocation()));
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
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberSyntax.GetLocation()));
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
            string? effectiveCultureName = null;
            RegexOptions regexOptionsWithPatternOptions;
            try
            {
                regexOptionsWithPatternOptions = regexOptions | RegexParser.ParseOptionsInPattern(pattern, regexOptions);
            }
            catch (Exception e)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), e.Message));
                return null;
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(cultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "cultureName"));
                    return null;
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(cultureName);
                    effectiveCultureName = cultureName;
                }
                catch (CultureNotFoundException)
                {
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "cultureName"));
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
                RegexOptions.Singleline |
                RegexOptions.AnyNewLine;
            if ((regexOptions & ~SupportedOptions) != 0)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "options"));
                return null;
            }

            // Validate the timeout
            if (matchTimeout is 0 or < -1)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), "matchTimeout"));
                return null;
            }

            // Determine the namespace the class is declared in, if any
            string? ns = regexMemberSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));

            // Build the declaring type hierarchy from outside in.
            RegexType? parentType = null;
            TypeDeclarationSyntax? parentDec = typeDec.Parent as TypeDeclarationSyntax;
            if (parentDec is not null)
            {
                // Collect parent types from inner to outer.
                List<TypeDeclarationSyntax> parents = new();
                while (parentDec is not null && IsAllowedKind(parentDec.Kind()))
                {
                    parents.Add(parentDec);
                    parentDec = parentDec.Parent as TypeDeclarationSyntax;
                }

                // Build chain from outermost to innermost.
                for (int i = parents.Count - 1; i >= 0; i--)
                {
                    TypeDeclarationSyntax p = parents[i];
                    parentType = new RegexType(
                        p is RecordDeclarationSyntax rds2 ? $"{p.Keyword.ValueText} {rds2.ClassOrStructKeyword}" : p.Keyword.ValueText,
                        ns ?? string.Empty,
                        $"{p.Identifier}{p.TypeParameterList}",
                        parentType);
                }
            }

            RegexType regexType = new(
                typeDec is RecordDeclarationSyntax rds ? $"{typeDec.Keyword.ValueText} {rds.ClassOrStructKeyword}" : typeDec.Keyword.ValueText,
                ns ?? string.Empty,
                $"{typeDec.Identifier}{typeDec.TypeParameterList}",
                parentType);

            CompilationData compilationData = compilation is CSharpCompilation { LanguageVersion: LanguageVersion langVersion, Options: CSharpCompilationOptions compilationOptions }
                ? new CompilationData(compilationOptions.AllowUnsafe, compilationOptions.CheckOverflow, langVersion)
                : default;

            string? limitedSupportReason;
            RegexTree regexTree;
            AnalysisResults analysis;
            try
            {
                regexTree = RegexParser.Parse(pattern, regexOptions | RegexOptions.Compiled, culture);
                analysis = RegexTreeAnalyzer.Analyze(regexTree);

                if (!SupportsCodeGeneration(regexTree.Root, compilationData.LanguageVersion, out limitedSupportReason))
                {
                    // Limited support — emit a boilerplate Regex wrapper, but still preserve the parsed data
                    // for the emitter rather than mirroring the tree into a second object model.
                    (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.LimitedSourceGeneration, memberSyntax.GetLocation()));
                }
            }
            catch (Exception e)
            {
                (diagnostics ??= []).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberSyntax.GetLocation(), e.Message));
                return null;
            }

            return new RegexMethod(
                regexType,
                regexMemberSymbol is IPropertySymbol,
                regexMemberSymbol.Name,
                memberSyntax.Modifiers.ToString(),
                nullableRegex,
                pattern,
                regexOptions,
                matchTimeout,
                effectiveCultureName,
                regexTree,
                analysis,
                limitedSupportReason,
                compilationData);

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
        /// <summary>Data about a regex, including its parsed tree and analysis results used by the emitter.</summary>
        internal sealed class RegexMethod(
            RegexType declaringType,
            bool isProperty,
            string memberName,
            string modifiers,
            bool nullableRegex,
            string pattern,
            RegexOptions options,
            int? matchTimeout,
            string? cultureName,
            RegexTree tree,
            AnalysisResults analysis,
            string? limitedSupportReason,
            CompilationData compilationData)
        {
            public RegexType DeclaringType { get; } = declaringType;
            public bool IsProperty { get; } = isProperty;
            public string MemberName { get; } = memberName;
            public string Modifiers { get; } = modifiers;
            public bool NullableRegex { get; } = nullableRegex;
            public string Pattern { get; } = pattern;
            public RegexOptions Options { get; } = options;
            public int? MatchTimeout { get; } = matchTimeout;
            public string? CultureName { get; } = cultureName;
            public RegexTree Tree { get; } = tree;
            public AnalysisResults Analysis { get; } = analysis;
            public string? LimitedSupportReason { get; } = limitedSupportReason;
            public CompilationData CompilationData { get; } = compilationData;
            public string? GeneratedName { get; set; }
            public bool IsDuplicate { get; set; }
        }

        /// <summary>A containing type for a regex member.</summary>
        internal sealed class RegexType(string keyword, string @namespace, string name, RegexType? parent)
        {
            private string? _fullName;

            public string Keyword { get; } = keyword;
            public string Namespace { get; } = @namespace;
            public string Name { get; } = name;
            public RegexType? Parent { get; } = parent;
            public string FullName => _fullName ??= Parent is null ? Name : $"{Parent.FullName}+{Name}";
        }

        internal sealed class RegexTreeComparer : IEqualityComparer<RegexTree>
        {
            private static readonly RegexNodeComparer s_nodeComparer = new();
            private static readonly RegexFindOptimizationsComparer s_findOptimizationsComparer = new();

            public bool Equals(RegexTree? x, RegexTree? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.Options == y.Options &&
                    x.CaptureCount == y.CaptureCount &&
                    StringComparer.Ordinal.Equals(x.Culture?.Name, y.Culture?.Name) &&
                    StringArrayEquals(x.CaptureNames, y.CaptureNames) &&
                    HashtableEquals(x.CaptureNameToNumberMapping, y.CaptureNameToNumberMapping) &&
                    HashtableEquals(x.CaptureNumberSparseMapping, y.CaptureNumberSparseMapping) &&
                    s_nodeComparer.Equals(x.Root, y.Root) &&
                    s_findOptimizationsComparer.Equals(x.FindOptimizations, y.FindOptimizations);
            }

            public int GetHashCode(RegexTree obj)
            {
                int hash = obj.Options.GetHashCode();
                hash = HashHelpers.Combine(hash, obj.CaptureCount);
                hash = HashHelpers.Combine(hash, obj.Culture?.Name is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Culture.Name));
                hash = HashHelpers.Combine(hash, GetStringArrayHashCode(obj.CaptureNames));
                hash = HashHelpers.Combine(hash, GetHashtableHashCode(obj.CaptureNameToNumberMapping));
                hash = HashHelpers.Combine(hash, GetHashtableHashCode(obj.CaptureNumberSparseMapping));
                hash = HashHelpers.Combine(hash, s_nodeComparer.GetHashCode(obj.Root));
                hash = HashHelpers.Combine(hash, s_findOptimizationsComparer.GetHashCode(obj.FindOptimizations));
                return hash;
            }
        }

        internal sealed class AnalysisResultsComparer : IEqualityComparer<AnalysisResults>
        {
            private static readonly RegexTreeComparer s_treeComparer = new();
            private static readonly RegexNodeComparer s_nodeComparer = new();

            public bool Equals(AnalysisResults? x, AnalysisResults? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                if (!s_treeComparer.Equals(x.RegexTree, y.RegexTree) ||
                    x.HasIgnoreCase != y.HasIgnoreCase ||
                    x.HasRightToLeft != y.HasRightToLeft)
                {
                    return false;
                }

                Stack<(RegexNode Left, RegexNode Right)> pending = new();
                pending.Push((x.RegexTree.Root, y.RegexTree.Root));

                while (pending.Count != 0)
                {
                    (RegexNode left, RegexNode right) = pending.Pop();

                    if (!s_nodeComparer.Equals(left, right) ||
                        x.IsAtomicByAncestor(left) != y.IsAtomicByAncestor(right) ||
                        x.MayContainCapture(left) != y.MayContainCapture(right) ||
                        x.MayBacktrack(left) != y.MayBacktrack(right) ||
                        x.IsInLoop(left) != y.IsInLoop(right))
                    {
                        return false;
                    }

                    for (int i = left.ChildCount() - 1; i >= 0; i--)
                    {
                        pending.Push((left.Child(i), right.Child(i)));
                    }
                }

                return true;
            }

            public int GetHashCode(AnalysisResults obj)
            {
                int hash = s_treeComparer.GetHashCode(obj.RegexTree);
                hash = HashHelpers.Combine(hash, obj.HasIgnoreCase.GetHashCode());
                hash = HashHelpers.Combine(hash, obj.HasRightToLeft.GetHashCode());

                Stack<RegexNode> pending = new();
                pending.Push(obj.RegexTree.Root);

                while (pending.Count != 0)
                {
                    RegexNode node = pending.Pop();
                    hash = HashHelpers.Combine(hash, obj.IsAtomicByAncestor(node).GetHashCode());
                    hash = HashHelpers.Combine(hash, obj.MayContainCapture(node).GetHashCode());
                    hash = HashHelpers.Combine(hash, obj.MayBacktrack(node).GetHashCode());
                    hash = HashHelpers.Combine(hash, obj.IsInLoop(node).GetHashCode());

                    for (int i = node.ChildCount() - 1; i >= 0; i--)
                    {
                        pending.Push(node.Child(i));
                    }
                }

                return hash;
            }
        }

        private sealed class RegexFindOptimizationsComparer : IEqualityComparer<RegexFindOptimizations>
        {
            private static readonly RegexNodeComparer s_nodeComparer = new();

            public bool Equals(RegexFindOptimizations? x, RegexFindOptimizations? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                if (x.FindMode != y.FindMode ||
                    x.LeadingAnchor != y.LeadingAnchor ||
                    x.TrailingAnchor != y.TrailingAnchor ||
                    x.MinRequiredLength != y.MinRequiredLength ||
                    x.MaxPossibleLength != y.MaxPossibleLength ||
                    !StringComparer.Ordinal.Equals(x.LeadingPrefix, y.LeadingPrefix) ||
                    !StringArrayEquals(x.LeadingPrefixes, y.LeadingPrefixes) ||
                    x.FixedDistanceLiteral.Char != y.FixedDistanceLiteral.Char ||
                    !StringComparer.Ordinal.Equals(x.FixedDistanceLiteral.String, y.FixedDistanceLiteral.String) ||
                    x.FixedDistanceLiteral.Distance != y.FixedDistanceLiteral.Distance ||
                    !FixedDistanceSetsEquals(x.FixedDistanceSets, y.FixedDistanceSets))
                {
                    return false;
                }

                if (x.LiteralAfterLoop is null || y.LiteralAfterLoop is null)
                {
                    return x.LiteralAfterLoop is null && y.LiteralAfterLoop is null;
                }

                (RegexNode xLoopNode, (char Char, string? String, StringComparison StringComparison, char[]? Chars) xLiteral) = x.LiteralAfterLoop.GetValueOrDefault();
                (RegexNode yLoopNode, (char Char, string? String, StringComparison StringComparison, char[]? Chars) yLiteral) = y.LiteralAfterLoop.GetValueOrDefault();

                return s_nodeComparer.Equals(xLoopNode, yLoopNode) &&
                    xLiteral.Char == yLiteral.Char &&
                    StringComparer.Ordinal.Equals(xLiteral.String, yLiteral.String) &&
                    xLiteral.StringComparison == yLiteral.StringComparison &&
                    CharArrayEquals(xLiteral.Chars, yLiteral.Chars);
            }

            public int GetHashCode(RegexFindOptimizations obj)
            {
                int hash = obj.FindMode.GetHashCode();
                hash = HashHelpers.Combine(hash, obj.LeadingAnchor.GetHashCode());
                hash = HashHelpers.Combine(hash, obj.TrailingAnchor.GetHashCode());
                hash = HashHelpers.Combine(hash, obj.MinRequiredLength);
                hash = HashHelpers.Combine(hash, obj.MaxPossibleLength.GetHashCode());
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.LeadingPrefix));
                hash = HashHelpers.Combine(hash, GetStringArrayHashCode(obj.LeadingPrefixes));
                hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.Char.GetHashCode());
                hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.String is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.FixedDistanceLiteral.String));
                hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.Distance);
                hash = HashHelpers.Combine(hash, GetFixedDistanceSetsHashCode(obj.FixedDistanceSets));

                if (obj.LiteralAfterLoop is { } literalAfterLoop)
                {
                    hash = HashHelpers.Combine(hash, s_nodeComparer.GetHashCode(literalAfterLoop.LoopNode));
                    hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.Char.GetHashCode());
                    hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.String is null ? 0 : StringComparer.Ordinal.GetHashCode(literalAfterLoop.Literal.String));
                    hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.StringComparison.GetHashCode());
                    hash = HashHelpers.Combine(hash, GetCharArrayHashCode(literalAfterLoop.Literal.Chars));
                }

                return hash;
            }
        }

        private sealed class RegexNodeComparer : IEqualityComparer<RegexNode>
        {
            public bool Equals(RegexNode? x, RegexNode? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                Stack<(RegexNode Left, RegexNode Right)> pending = new();
                pending.Push((x, y));

                while (pending.Count != 0)
                {
                    (RegexNode left, RegexNode right) = pending.Pop();

                    if (left.Kind != right.Kind ||
                        !StringComparer.Ordinal.Equals(left.Str, right.Str) ||
                        left.Ch != right.Ch ||
                        left.M != right.M ||
                        left.N != right.N ||
                        left.Options != right.Options)
                    {
                        return false;
                    }

                    int childCount = left.ChildCount();
                    if (childCount != right.ChildCount())
                    {
                        return false;
                    }

                    for (int i = childCount - 1; i >= 0; i--)
                    {
                        pending.Push((left.Child(i), right.Child(i)));
                    }
                }

                return true;
            }

            public int GetHashCode(RegexNode obj)
            {
                int hash = 0;
                Stack<RegexNode> pending = new();
                pending.Push(obj);

                while (pending.Count != 0)
                {
                    RegexNode current = pending.Pop();
                    hash = HashHelpers.Combine(hash, current.Kind.GetHashCode());
                    hash = HashHelpers.Combine(hash, current.Str is null ? 0 : StringComparer.Ordinal.GetHashCode(current.Str));
                    hash = HashHelpers.Combine(hash, current.Ch.GetHashCode());
                    hash = HashHelpers.Combine(hash, current.M);
                    hash = HashHelpers.Combine(hash, current.N);
                    hash = HashHelpers.Combine(hash, current.Options.GetHashCode());
                    hash = HashHelpers.Combine(hash, current.ChildCount());

                    for (int i = current.ChildCount() - 1; i >= 0; i--)
                    {
                        pending.Push(current.Child(i));
                    }
                }

                return hash;
            }
        }

        private static bool StringArrayEquals(string[]? x, string[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (!StringComparer.Ordinal.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CharArrayEquals(char[]? x, char[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HashtableEquals(Hashtable? x, Hashtable? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Count != y.Count)
            {
                return false;
            }

            foreach (DictionaryEntry entry in x)
            {
                if (!y.ContainsKey(entry.Key) ||
                    !Equals(entry.Value, y[entry.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FixedDistanceSetsEquals(List<RegexFindOptimizations.FixedDistanceSet>? x, List<RegexFindOptimizations.FixedDistanceSet>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Count != y.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Count; i++)
            {
                RegexFindOptimizations.FixedDistanceSet left = x[i];
                RegexFindOptimizations.FixedDistanceSet right = y[i];

                if (!StringComparer.Ordinal.Equals(left.Set, right.Set) ||
                    left.Negated != right.Negated ||
                    !CharArrayEquals(left.Chars, right.Chars) ||
                    left.Distance != right.Distance ||
                    left.Range != right.Range)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetStringArrayHashCode(string[]? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Length;
            foreach (string value in values)
            {
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(value));
            }

            return hash;
        }

        private static int GetCharArrayHashCode(char[]? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Length;
            foreach (char value in values)
            {
                hash = HashHelpers.Combine(hash, value.GetHashCode());
            }

            return hash;
        }

        private static int GetHashtableHashCode(Hashtable? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Count;
            foreach (DictionaryEntry entry in values)
            {
                int entryHash = entry.Key?.GetHashCode() ?? 0;
                entryHash = HashHelpers.Combine(entryHash, entry.Value?.GetHashCode() ?? 0);
                hash ^= entryHash;
            }

            return hash;
        }

        private static int GetFixedDistanceSetsHashCode(List<RegexFindOptimizations.FixedDistanceSet>? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Count;
            foreach (RegexFindOptimizations.FixedDistanceSet value in values)
            {
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(value.Set));
                hash = HashHelpers.Combine(hash, value.Negated.GetHashCode());
                hash = HashHelpers.Combine(hash, GetCharArrayHashCode(value.Chars));
                hash = HashHelpers.Combine(hash, value.Distance);
                hash = HashHelpers.Combine(hash, value.Range.GetHashCode());
            }

            return hash;
        }

        internal sealed class RegexTypeComparer : IEqualityComparer<RegexType>
        {
            public bool Equals(RegexType? x, RegexType? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return StringComparer.Ordinal.Equals(x.Keyword, y.Keyword) &&
                    StringComparer.Ordinal.Equals(x.Namespace, y.Namespace) &&
                    StringComparer.Ordinal.Equals(x.Name, y.Name) &&
                    Equals(x.Parent, y.Parent);
            }

            public int GetHashCode(RegexType obj)
            {
                int hash = StringComparer.Ordinal.GetHashCode(obj.Keyword);
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.Namespace));
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.Name));
                hash = HashHelpers.Combine(hash, obj.Parent is null ? 0 : GetHashCode(obj.Parent));
                return hash;
            }
        }
    }
}
