// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal readonly record struct InterceptorLocationInfo
    {
        public InterceptorLocationInfo(IInvocationOperation operation)
        {
            SyntaxNode operationSyntax = operation.Syntax;
            TextSpan operationSpan = operationSyntax.Span;
            SyntaxTree operationSyntaxTree = operationSyntax.SyntaxTree;

            FilePath = GetInterceptorFilePath(operationSyntaxTree, operation.SemanticModel?.Compilation.Options.SourceReferenceResolver);

            FileLinePositionSpan span = operationSyntaxTree.GetLineSpan(operationSpan);
            LineNumber = span.StartLinePosition.Line + 1;

            // Calculate the character offset to the end of the binding invocation detected.
            int invocationLength = ((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)operationSyntax).Expression).Expression.Span.Length;
            CharacterNumber = span.StartLinePosition.Character + invocationLength + 2;
        }

        public string FilePath { get; }
        public int LineNumber { get; }
        public int CharacterNumber { get; }

        // Utilize the same logic used by the interceptors API for resolving the source mapped value of a path.
        // https://github.com/dotnet/roslyn/blob/f290437fcc75dad50a38c09e0977cce13a64f5ba/src/Compilers/CSharp/Portable/Compilation/CSharpCompilation.cs#L1063-L1064
        private static string GetInterceptorFilePath(SyntaxTree tree, SourceReferenceResolver? resolver) =>
            resolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
    }
}
