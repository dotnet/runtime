// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Diagnostic IDs, audit markers and the unsafe-evolution compiler APIs. The fixer builds against a Roslyn that
    /// predates <c>unsafe(...)</c> and <c>MemorySafetyRulesVersion</c>, so both are reached at run time.
    /// </summary>
    internal static class UnsafeContextFacts
    {
        internal const string UnsafeOperationId = "CS9360";
        internal const string UninitializedStackallocId = "CS9361";
        internal const string UnsafeMemberOperationId = "CS9362";
        internal const string UnsafeMemberOperationCompatId = "CS9363";
        internal const string UnsafeConstructorConstraintId = "CS9376";

        internal static readonly ImmutableArray<string> FixableDiagnosticIds =
        [
            UnsafeOperationId,
            UninitializedStackallocId,
            UnsafeMemberOperationId,
            UnsafeMemberOperationCompatId,
            UnsafeConstructorConstraintId,
        ];

        /// <summary>The placeholder audit comment on the line above every introduced <c>unsafe { }</c> block.</summary>
        internal const string BlockMarker = "// SAFETY: To be audited";

        /// <summary>The placeholder audit comment directly before every introduced <c>unsafe(...)</c> expression.</summary>
        internal const string ExpressionMarker = "/* SAFETY: To be audited */";

        private const int MemorySafetyRulesVersion2 = 2;
        private const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        private static readonly SyntaxKind s_unsafeExpressionKind =
            Enum.TryParse("UnsafeExpression", out SyntaxKind kind) ? kind : SyntaxKind.None;

        private static readonly Func<IModuleSymbol, object?>? s_getMemorySafetyRulesVersion =
            typeof(IModuleSymbol).GetProperty("MemorySafetyRulesVersion") is { } property ? module => property.GetValue(module) : null;

        internal static bool IsFixableDiagnosticId(string id) => FixableDiagnosticIds.Contains(id);

        internal static bool IsUnsafeExpression(SyntaxNode node) =>
            s_unsafeExpressionKind != SyntaxKind.None && node.IsKind(s_unsafeExpressionKind);

        /// <summary>Returns the expression inside <c>unsafe(...)</c>.</summary>
        internal static ExpressionSyntax GetUnsafeExpressionOperand(SyntaxNode unsafeExpression) =>
            unsafeExpression.ChildNodes().OfType<ExpressionSyntax>().Single();

        /// <summary>
        /// Returns <c>/* SAFETY: To be audited */ unsafe(expression)</c> with the expression's outer trivia; without the
        /// marker when <paramref name="withMarker"/> is false, e.g. when the token before already carries it.
        /// </summary>
        internal static ExpressionSyntax CreateUnsafeExpression(ExpressionSyntax expression, CSharpParseOptions options, bool withMarker = true)
        {
            var template = SyntaxFactory.ParseExpression("unsafe(_)", options: options);
            var wrapper = template.ReplaceNode(GetUnsafeExpressionOperand(template), expression.WithoutTrivia());
            SyntaxTriviaList leading = expression.GetLeadingTrivia();
            return wrapper
                .WithLeadingTrivia(withMarker ? leading.AddRange([SyntaxFactory.Comment(ExpressionMarker), SyntaxFactory.Space]) : leading)
                .WithTrailingTrivia(expression.GetTrailingTrivia());
        }

        /// <summary>
        /// Whether the fixer may introduce contexts in a document: v2 memory-safety rules, unsafe code
        /// allowed, a language version with <c>unsafe(...)</c>, and no generated code.
        /// </summary>
        internal static bool IsEnabled(Document document, SyntaxNode root, Compilation compilation) =>
            document is not SourceGeneratedDocument
            && compilation.Options is CSharpCompilationOptions { AllowUnsafe: true }
            && root.SyntaxTree.Options is CSharpParseOptions parseOptions
            && UsesUpdatedMemorySafetyRules(compilation, parseOptions)
            && SupportsUnsafeExpressions(parseOptions)
            && !IsGeneratedCode(root);

        // Hosts that predate the public API, such as the SDK's dotnet-format, read the compiler's feature flag instead.
        private static bool UsesUpdatedMemorySafetyRules(Compilation compilation, CSharpParseOptions parseOptions) =>
            s_getMemorySafetyRulesVersion is null
                ? parseOptions.Features.ContainsKey(UpdatedMemorySafetyRulesFeature)
                : s_getMemorySafetyRulesVersion(compilation.SourceModule) is { } version && Convert.ToInt32(version) == MemorySafetyRulesVersion2;

        private static bool SupportsUnsafeExpressions(CSharpParseOptions options) =>
            SyntaxFactory.ParseExpression("unsafe(0)", options: options) is var expression
            && IsUnsafeExpression(expression)
            && !expression.ContainsDiagnostics;

        private static bool IsGeneratedCode(SyntaxNode root)
        {
            string fileName = Path.GetFileName(root.SyntaxTree.FilePath ?? string.Empty);
            string[] generatedSuffixes = [".designer.cs", ".generated.cs", ".g.cs", ".g.i.cs"];
            if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase)
                || generatedSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return root.GetLeadingTrivia().Any(static trivia =>
                (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                && trivia.ToString().IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
