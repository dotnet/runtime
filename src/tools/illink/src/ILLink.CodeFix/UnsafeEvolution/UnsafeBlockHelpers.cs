// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix.UnsafeEvolution
{
    /// <summary>
    /// Helpers shared by the unsafe-evolution analyzer and code fixers.
    /// </summary>
    internal static class UnsafeBlockHelpers
    {
        /// <summary>
        /// True if any pointer-typed or function-pointer-typed syntax appears anywhere
        /// within <paramref name="type"/> (including nested generics, arrays, etc.).
        /// </summary>
        internal static bool ContainsPointerType(TypeSyntax? type)
        {
            if (type is null)
                return false;

            return type is PointerTypeSyntax or FunctionPointerTypeSyntax
                || type.DescendantNodes().Any(static n => n is PointerTypeSyntax or FunctionPointerTypeSyntax);
        }

        /// <summary>
        /// True if any parameter or return type of the given declaration mentions a pointer
        /// or function-pointer type. Used as the heuristic gate for IL5006.
        /// </summary>
        internal static bool SignatureContainsPointer(SyntaxNode declaration) => declaration switch
        {
            MethodDeclarationSyntax m => SignatureHasPointer(m.ReturnType, m.ParameterList),
            LocalFunctionStatementSyntax lf => SignatureHasPointer(lf.ReturnType, lf.ParameterList),
            OperatorDeclarationSyntax op => SignatureHasPointer(op.ReturnType, op.ParameterList),
            ConversionOperatorDeclarationSyntax co => SignatureHasPointer(co.Type, co.ParameterList),
            DelegateDeclarationSyntax d => SignatureHasPointer(d.ReturnType, d.ParameterList),
            ConstructorDeclarationSyntax c => SignatureHasPointer(returnType: null, c.ParameterList),
            IndexerDeclarationSyntax idx => SignatureHasPointer(idx.Type, idx.ParameterList),
            BasePropertyDeclarationSyntax bp => ContainsPointerType(bp.Type),    // PropertyDeclaration, EventDeclaration
            BaseFieldDeclarationSyntax bf => ContainsPointerType(bf.Declaration.Type), // FieldDeclaration, EventFieldDeclaration
            AccessorDeclarationSyntax acc => acc.Parent?.Parent is { } owner && SignatureContainsPointer(owner),
            _ => false,
        };

        private static bool SignatureHasPointer(TypeSyntax? returnType, BaseParameterListSyntax parameterList)
            => ContainsPointerType(returnType)
                || parameterList.Parameters.Any(static p => ContainsPointerType(p.Type));

        /// <summary>
        /// The first <c>unsafe</c> keyword in <paramref name="modifiers"/>, or <c>default</c> if absent.
        /// </summary>
        internal static SyntaxToken FindUnsafeModifier(SyntaxTokenList modifiers)
            => modifiers.FirstOrDefault(static t => t.IsKind(SyntaxKind.UnsafeKeyword));

        /// <summary>
        /// True when <paramref name="node"/> carries preprocessor directive trivia STRICTLY BETWEEN
        /// its first and last tokens. Directives that sit in the leading trivia of the first token
        /// or in the trailing trivia of the last token (i.e. an enclosing <c>#if/#endif</c>) are
        /// excluded because preserving them across a rewrite is straightforward.
        /// </summary>
        internal static bool ContainsInternalDirectiveTrivia(SyntaxNode node)
        {
            int internalStart = node.GetFirstToken().Span.End;
            int internalEnd = node.GetLastToken().Span.Start;
            if (internalEnd <= internalStart)
                return false;

            foreach (var trivia in node.DescendantTrivia(descendIntoTrivia: true))
            {
                if (trivia.IsDirective
                    && trivia.SpanStart >= internalStart
                    && trivia.Span.End <= internalEnd)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Heuristic. Returns true if a member's body has enough unsafe diagnostics that
        /// wrapping the whole body in one <c>unsafe { }</c> is preferable to wrapping each
        /// statement individually.
        /// </summary>
        /// <param name="unsafeDiagnosticCount">Number of CS9360/CS9361/CS9362/CS0214 diagnostics in the body.</param>
        /// <param name="totalStatementCount">Total number of statements in the body.</param>
        internal static bool ShouldWrapEntireBody(int unsafeDiagnosticCount, int totalStatementCount)
        {
            if (unsafeDiagnosticCount <= 0)
                return false;
            // Three or more unsafe operations and at least every 4th statement is unsafe.
            return unsafeDiagnosticCount >= 3 && unsafeDiagnosticCount * 4 >= totalStatementCount;
        }
    }
}
#endif
