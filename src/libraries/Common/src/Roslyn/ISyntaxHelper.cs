// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions
{
    internal interface ISyntaxHelper
    {
        bool IsCaseSensitive { get; }

        bool IsValidIdentifier(string name);

        bool IsAnyNamespaceBlock(SyntaxNode node);

        bool IsAttributeList(SyntaxNode node);
        SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node);

        void AddAttributeTargets(SyntaxNode node, ref ValueListBuilder<SyntaxNode> targets);

        bool IsAttribute(SyntaxNode node);
        SyntaxNode GetNameOfAttribute(SyntaxNode node);

        bool IsLambdaExpression(SyntaxNode node);

        SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode node);

        /// <summary>
        /// <paramref name="node"/> must be a compilation unit or namespace block.
        /// </summary>
        void AddAliases(SyntaxNode node, ref ValueListBuilder<(string aliasName, string symbolName)> aliases, bool global);
        void AddAliases(CompilationOptions options, ref ValueListBuilder<(string aliasName, string symbolName)> aliases);

        bool ContainsGlobalAliases(SyntaxNode root);
    }

    internal abstract class AbstractSyntaxHelper : ISyntaxHelper
    {
        public abstract bool IsCaseSensitive { get; }

        public abstract bool IsValidIdentifier(string name);

        public abstract SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode name);

        public abstract bool IsAnyNamespaceBlock(SyntaxNode node);

        public abstract bool IsAttribute(SyntaxNode node);
        public abstract SyntaxNode GetNameOfAttribute(SyntaxNode node);

        public abstract bool IsAttributeList(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node);
        public abstract void AddAttributeTargets(SyntaxNode node, ref ValueListBuilder<SyntaxNode> targets);

        public abstract bool IsLambdaExpression(SyntaxNode node);

        public abstract void AddAliases(SyntaxNode node, ref ValueListBuilder<(string aliasName, string symbolName)> aliases, bool global);
        public abstract void AddAliases(CompilationOptions options, ref ValueListBuilder<(string aliasName, string symbolName)> aliases);

        public abstract bool ContainsGlobalAliases(SyntaxNode root);
    }
}
