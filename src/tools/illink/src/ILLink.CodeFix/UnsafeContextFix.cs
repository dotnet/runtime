// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.CodeAnalysis;

namespace ILLink.CodeFix
{
    /// <summary>
    /// The shape of the <c>unsafe</c> context that is introduced at a use site.
    /// </summary>
    internal enum UnsafeContextKind
    {
        /// <summary>
        /// The statement is replaced by <c>unsafe { statement }</c>.
        /// </summary>
        Statement,

        /// <summary>
        /// A local declaration is split into <c>T local;</c> followed by <c>unsafe { local = initializer; }</c>,
        /// so that the local stays visible to the statements that follow it.
        /// </summary>
        SplitDeclaration,

        /// <summary>
        /// The expression is replaced by <c>unsafe(expression)</c>, for the positions where a block is not valid
        /// syntax or would shorten an existing scope.
        /// </summary>
        Expression,

        /// <summary>
        /// The whole method body becomes a single <c>unsafe</c> block.
        /// </summary>
        Body,
    }

    /// <summary>
    /// A planned edit that introduces an <c>unsafe</c> context around <paramref name="target"/>.
    /// </summary>
    internal readonly struct UnsafeContextFix(UnsafeContextKind kind, SyntaxNode target)
    {
        public UnsafeContextKind Kind { get; } = kind;

        public SyntaxNode Target { get; } = target;
    }
}
#endif
