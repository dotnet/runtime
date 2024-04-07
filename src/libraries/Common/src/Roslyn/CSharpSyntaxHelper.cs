// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions
{
    internal sealed class CSharpSyntaxHelper : AbstractSyntaxHelper
    {
        public static readonly ISyntaxHelper Instance = new CSharpSyntaxHelper();

        private CSharpSyntaxHelper()
        {
        }

        public override bool IsCaseSensitive
            => true;

        public override bool IsValidIdentifier(string name)
            => SyntaxFacts.IsValidIdentifier(name);

        public override bool IsAnyNamespaceBlock(SyntaxNode node)
            => node is BaseNamespaceDeclarationSyntax;

        public override bool IsAttribute(SyntaxNode node)
            => node is AttributeSyntax;

        public override SyntaxNode GetNameOfAttribute(SyntaxNode node)
            => ((AttributeSyntax)node).Name;

        public override bool IsAttributeList(SyntaxNode node)
            => node is AttributeListSyntax;

        public override void AddAttributeTargets(SyntaxNode node, ref ValueListBuilder<SyntaxNode> targets)
        {
            var attributeList = (AttributeListSyntax)node;
            var container = attributeList.Parent;
            Debug.Assert(container != null);

            // For fields/events, the attribute applies to all the variables declared.
            if (container is FieldDeclarationSyntax field)
            {
                foreach (var variable in field.Declaration.Variables)
                    targets.Append(variable);
            }
            else if (container is EventFieldDeclarationSyntax ev)
            {
                foreach (var variable in ev.Declaration.Variables)
                    targets.Append(variable);
            }
            else
            {
                targets.Append(container);
            }
        }

        public override SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node)
            => ((AttributeListSyntax)node).Attributes;

        public override bool IsLambdaExpression(SyntaxNode node)
            => node is LambdaExpressionSyntax;

        public override SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode node)
            => ((NameSyntax)node).GetUnqualifiedName().Identifier;

        public override void AddAliases(SyntaxNode node, ref ValueListBuilder<(string aliasName, string symbolName)> aliases, bool global)
        {
            if (node is CompilationUnitSyntax compilationUnit)
            {
                AddAliases(compilationUnit.Usings, ref aliases, global);
            }
            else if (node is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                AddAliases(namespaceDeclaration.Usings, ref aliases, global);
            }
            else
            {
                Debug.Fail("This should not be reachable.  Caller already checked we had a compilation unit or namespace.");
            }
        }

        private static void AddAliases(SyntaxList<UsingDirectiveSyntax> usings, ref ValueListBuilder<(string aliasName, string symbolName)> aliases, bool global)
        {
            foreach (var usingDirective in usings)
            {
                if (usingDirective.Alias is null)
                    continue;

                if (global != usingDirective.GlobalKeyword.Kind() is SyntaxKind.GlobalKeyword)
                    continue;

                // We only care about aliases from one name to another name.  e.g. `using X = A.B.C;`  That's because
                // the caller is only interested in finding a fully-qualified-metadata-name to an attribute.
                if (usingDirective.Name is null)
                    continue;

                var aliasName = usingDirective.Alias.Name.Identifier.ValueText;
                var symbolName = usingDirective.Name.GetUnqualifiedName().Identifier.ValueText;
                aliases.Append((aliasName, symbolName));
            }
        }

        public override void AddAliases(CompilationOptions compilation, ref ValueListBuilder<(string aliasName, string symbolName)> aliases)
        {
            // C# doesn't have global aliases at the compilation level.
            return;
        }

        public override bool ContainsGlobalAliases(SyntaxNode root)
        {
            // Global usings can only exist at the compilation-unit level, so no need to dive any deeper than that.
            var compilationUnit = (CompilationUnitSyntax)root;

            foreach (var directive in compilationUnit.Usings)
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) &&
                    directive.Alias != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
