// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SourceGenerators;

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

        private static void AddHelper(ref Dictionary<string, HelperMethod>? helpers, string name, HelperMethod helper)
        {
            helpers ??= new Dictionary<string, HelperMethod>(StringComparer.Ordinal);
#if NET
            helpers.TryAdd(name, helper);
#else
            if (!helpers.ContainsKey(name))
            {
                helpers.Add(name, helper);
            }
#endif
        }

        /// <summary>
        /// Validates the attributed member and extracts the <see cref="RegexPatternAndSyntax"/> data.
        /// Returns <see langword="null"/> for silent skips (e.g., missing types), a <see cref="Diagnostic"/>
        /// for validation failures, or a <see cref="RegexPatternAndSyntax"/> on success.
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
                return Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, context.TargetNode.GetLocation());
            }

            var memberSyntax = (MemberDeclarationSyntax)context.TargetNode;
            Location memberLocation = memberSyntax.GetLocation();
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
                return Diagnostic.Create(DiagnosticDescriptors.MultipleGeneratedRegexAttributes, memberLocation);
            }
            AttributeData generatedRegexAttr = boundAttributes[0];

            if (generatedRegexAttr.ConstructorArguments.Any(ca => ca.Kind == TypedConstantKind.Error))
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberLocation);
            }

            ImmutableArray<TypedConstant> items = generatedRegexAttr.ConstructorArguments;
            if (items.Length is 0 or > 4)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidGeneratedRegexAttribute, memberLocation);
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
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, "(null)");
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
                    return Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberLocation);
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
                    return Diagnostic.Create(DiagnosticDescriptors.RegexMemberMustHaveValidSignature, memberLocation);
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
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, e.Message);
            }

            if ((regexOptionsWithPatternOptions & RegexOptions.IgnoreCase) != 0 && !string.IsNullOrEmpty(cultureName))
            {
                if ((regexOptions & RegexOptions.CultureInvariant) != 0)
                {
                    // User passed in both a culture name and set RegexOptions.CultureInvariant which causes an explicit conflict.
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, "cultureName");
                }

                try
                {
                    culture = CultureInfo.GetCultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, "cultureName");
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
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, "options");
            }

            // Validate the timeout
            if (matchTimeout is 0 or < -1)
            {
                return Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, memberLocation, "matchTimeout");
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
                memberLocation,
                IsProperty: regexMemberSymbol is IPropertySymbol,
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
        }

        /// <summary>
        /// Parses the regex, generates code, and extracts the result into deeply equatable types.
        /// Called after <see cref="GetRegexMethodDataOrFailureDiagnostic"/> has validated the attribute
        /// and built the <see cref="RegexPatternAndSyntax"/>. Diagnostics and helpers are added to
        /// the respective accumulators.
        /// </summary>
        private static RegexMethodEntry? ParseAndGenerateRegex(
            RegexPatternAndSyntax method,
            ref ImmutableArray<Diagnostic>.Builder? diagnostics, ref Dictionary<string, HelperMethod>? helpers)
        {
            RegexTree regexTree;
            AnalysisResults analysis;
            try
            {
                regexTree = RegexParser.Parse(method.Pattern, method.Options | RegexOptions.Compiled, method.Culture); // make sure Compiled is included to get all optimizations applied to it
                analysis = RegexTreeAnalyzer.Analyze(regexTree);
            }
            catch (Exception e)
            {
                (diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>()).Add(Diagnostic.Create(DiagnosticDescriptors.InvalidRegexArguments, method.MemberLocation, e.Message));
                return null;
            }

            var regexMethod = new RegexMethod(method.DeclaringType, method.IsProperty, method.MemberName, method.Modifiers, method.NullableRegex, method.Pattern, method.Options, method.MatchTimeout, regexTree, analysis, method.CompilationData);

            // Pre-compute the XML expression description from the tree while we still have access to it.
            string expressionDescription;
            using (var descSw = new StringWriter())
            {
                DescribeExpressionAsXmlComment(descSw, regexTree.Root.Child(0), regexMethod);
                expressionDescription = descSw.ToString();
            }

            // Extract capture metadata from the tree into equatable forms.
            ImmutableEquatableArray<(int Key, int Value)>? captureNumberSparseMapping = regexTree.CaptureNumberSparseMapping is { } cnsm
                ? cnsm.Cast<Collections.DictionaryEntry>().Select(de => (Key: (int)de.Key, Value: (int)de.Value!)).OrderBy(p => p.Key).ToImmutableEquatableArray()
                : null;
            ImmutableEquatableArray<(string Key, int Value)>? captureNameToNumberMapping = regexTree.CaptureNameToNumberMapping is { } cntnm
                ? cntnm.Cast<Collections.DictionaryEntry>().Select(de => (Key: (string)de.Key, Value: (int)de.Value!)).OrderBy(p => p.Key, StringComparer.Ordinal).ToImmutableEquatableArray()
                : null;
            ImmutableEquatableArray<string>? captureNames = regexTree.CaptureNames?.ToImmutableEquatableArray();
            int captureCount = regexTree.CaptureCount;

            // If we're unable to generate a full implementation for this regex, report a diagnostic.
            // We'll still output a limited implementation that just caches a new Regex(...).
            if (!SupportsCodeGeneration(regexMethod, method.CompilationData.LanguageVersion, out string? reason))
            {
                (diagnostics ??= ImmutableArray.CreateBuilder<Diagnostic>()).Add(Diagnostic.Create(DiagnosticDescriptors.LimitedSourceGeneration, method.MemberLocation));

                return new RegexMethodEntry(
                    method.DeclaringType, method.IsProperty, method.MemberName,
                    method.Modifiers, method.NullableRegex, method.Pattern,
                    method.Options, method.MatchTimeout, method.CompilationData,
                    GeneratedCode: null, LimitedSupportReason: reason,
                    expressionDescription, captureNumberSparseMapping, captureNameToNumberMapping,
                    captureNames, captureCount);
            }

            // Generate the core logic for the regex.
            Dictionary<string, string[]> requiredHelpers = new();
            var sw = new StringWriter();
            var writer = new IndentedTextWriter(sw);
            writer.Indent += 2;
            writer.WriteLine();
            EmitRegexDerivedTypeRunnerFactory(writer, regexMethod, requiredHelpers, method.CompilationData.CheckOverflow);
            writer.Indent -= 2;

            // Add required helpers to the shared accumulator.
            foreach (KeyValuePair<string, string[]> h in requiredHelpers)
            {
                AddHelper(ref helpers, h.Key, new HelperMethod(h.Key, h.Value.ToImmutableEquatableArray()));
            }

            return new RegexMethodEntry(
                method.DeclaringType, method.IsProperty, method.MemberName,
                method.Modifiers, method.NullableRegex, method.Pattern,
                method.Options, method.MatchTimeout, method.CompilationData,
                GeneratedCode: sw.ToString(), LimitedSupportReason: null,
                expressionDescription, captureNumberSparseMapping, captureNameToNumberMapping,
                captureNames, captureCount);
        }

        /// <summary>Data about a regex directly from the GeneratedRegex attribute.</summary>
        internal sealed record RegexPatternAndSyntax(RegexType DeclaringType, Location MemberLocation, bool IsProperty, string MemberName, string Modifiers, bool NullableRegex, string Pattern, RegexOptions Options, int? MatchTimeout, CultureInfo Culture, CompilationData CompilationData);

        /// <summary>Data about a regex, including a fully parsed RegexTree and subsequent analysis.</summary>
        internal sealed record RegexMethod(RegexType DeclaringType, bool IsProperty, string MemberName, string Modifiers, bool NullableRegex, string Pattern, RegexOptions Options, int? MatchTimeout, RegexTree Tree, AnalysisResults Analysis, CompilationData CompilationData);

        /// <summary>A type holding a regex method.</summary>
        internal sealed record RegexType(string Keyword, string Namespace, string Name)
        {
            public RegexType? Parent { get; set; }
        }

        /// <summary>
        /// Per-method data extracted from <see cref="RegexMethod"/> with all fields deeply equatable.
        /// This is the incremental model used for source generation — it contains no references to
        /// <see cref="RegexTree"/>, <see cref="AnalysisResults"/>, or Roslyn symbols.
        /// </summary>
        internal sealed record RegexMethodEntry(
            RegexType DeclaringType,
            bool IsProperty,
            string MemberName,
            string Modifiers,
            bool NullableRegex,
            string Pattern,
            RegexOptions Options,
            int? MatchTimeout,
            CompilationData CompilationData,
            string? GeneratedCode,
            string? LimitedSupportReason,
            string ExpressionDescription,
            ImmutableEquatableArray<(int Key, int Value)>? CaptureNumberSparseMapping,
            ImmutableEquatableArray<(string Key, int Value)>? CaptureNameToNumberMapping,
            ImmutableEquatableArray<string>? CaptureNames,
            int CaptureCount);

        /// <summary>A named helper method (e.g. IsWordChar, IsBoundary) shared across regex implementations.</summary>
        internal sealed record HelperMethod(string Name, ImmutableEquatableArray<string> Lines);

        /// <summary>
        /// The complete source generation model for all regex methods in a compilation.
        /// All fields use <see cref="ImmutableEquatableArray{T}"/> for deep value equality,
        /// enabling Roslyn's incremental pipeline to skip re-emission when the model is unchanged.
        /// </summary>
        internal sealed record RegexSourceGenerationResult(
            ImmutableEquatableArray<RegexMethodEntry> Methods,
            ImmutableEquatableArray<HelperMethod> Helpers);
    }
}
