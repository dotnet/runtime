// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Shared helpers for the "unsafe evolution" migration analyzers (IL5005/IL5006).
    /// </summary>
    internal static class UnsafeMigrationFacts
    {
        internal const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        /// <summary>Diagnostic ids the compiler reports when an operation needs an unsafe context.</summary>
        internal static readonly string[] MissingUnsafeContextDiagnosticIds =
        [
            "CS9360", // pointer indirection / element / member access / function-pointer invocation
            "CS9362", // call to a member marked 'unsafe'
            "CS9363", // call to a member that has pointers in its signature
        ];

        internal static bool UsesUpdatedMemorySafetyRules(SyntaxTree tree) =>
            tree.Options is CSharpParseOptions options &&
            options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);

        internal static SyntaxTokenList GetModifiers(this SyntaxNode node) => node switch
        {
            MemberDeclarationSyntax member => member.Modifiers,
            LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
            AccessorDeclarationSyntax accessor => accessor.Modifiers,
            _ => default,
        };

        /// <summary>
        /// True when the node is already inside a lexical unsafe context: an <c>unsafe</c> block, an
        /// <c>unsafe(...)</c> expression, or a declaration carrying the <c>unsafe</c> modifier.
        /// </summary>
        internal static bool IsInUnsafeContext(this SyntaxNode node) =>
            node.Ancestors().Any(static a =>
                a.IsKind(SyntaxKind.UnsafeStatement) ||
                (a is ExpressionSyntax && a.GetFirstToken().IsKind(SyntaxKind.UnsafeKeyword)) ||
                a.GetModifiers().Any(SyntaxKind.UnsafeKeyword));

        internal static bool IsSpanType(ITypeSymbol? type) =>
            type is INamedTypeSymbol { IsGenericType: true, Name: "Span" or "ReadOnlySpan", ContainingNamespace.Name: "System" };

        /// <summary>True when the symbol (or its containers/module) is annotated with <c>SkipLocalsInitAttribute</c>.</summary>
        internal static bool HasSkipLocalsInit(ISymbol? symbol, Compilation compilation)
        {
            for (var current = symbol; current is not null; current = current.ContainingSymbol)
            {
                if (current.GetAttributes().Any(IsSkipLocalsInit))
                    return true;
            }

            return compilation.SourceModule.GetAttributes().Any(IsSkipLocalsInit);

            static bool IsSkipLocalsInit(AttributeData attribute) => attribute.AttributeClass?.Name == "SkipLocalsInitAttribute";
        }
    }
}
#endif
