// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Immutable;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RequiresUnsafeAnalyzer : RequiresAnalyzerBase
    {
        private const string RequiresUnsafeAttribute = nameof(RequiresUnsafeAttribute);
        public const string FullyQualifiedRequiresUnsafeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnsafeAttribute;

        private static readonly DiagnosticDescriptor s_requiresUnsafeOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafeOnStaticConstructor);
        private static readonly DiagnosticDescriptor s_requiresUnsafeOnEntryPoint = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafeOnEntryPoint);
        private static readonly DiagnosticDescriptor s_requiresUnsafeRule = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafe);
        private static readonly DiagnosticDescriptor s_requiresUnsafeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.RequiresUnsafeAttributeMismatch);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(s_requiresUnsafeRule, s_requiresUnsafeAttributeMismatch, s_requiresUnsafeOnStaticCtor, s_requiresUnsafeOnEntryPoint);

        private protected override string RequiresAttributeName => RequiresUnsafeAttribute;

        internal override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnsafeAttribute;

        private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

        private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnsafeRule;

        private protected override DiagnosticId RequiresDiagnosticId => DiagnosticId.RequiresUnsafe;

        private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnsafeAttributeMismatch;

        private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresUnsafeOnStaticCtor;

        private protected override DiagnosticDescriptor RequiresOnEntryPoint => s_requiresUnsafeOnEntryPoint;

        internal override bool IsAnalyzerEnabled(AnalyzerOptions options) =>
            options.IsMSBuildPropertyValueTrue(MSBuildPropertyOptionNames.EnableUnsafeAnalyzer);

        private protected override bool IsRequiresCheck(IPropertySymbol propertySymbol, Compilation compilation)
        {
            // No feature check property for RequiresUnsafe
            return false;
        }

        protected override bool IsInRequiresScope(ISymbol containingSymbol, in DiagnosticContext context)
        {
            if (base.IsInRequiresScope(containingSymbol, context))
                return true;

            if (!context.Location.IsInSource)
                return false;

            // Check to see if we're in an unsafe block or unsafe member
            var syntaxTree = context.Location.SourceTree!;
            var root = syntaxTree.GetRoot();
            var node = root.FindNode(context.Location.SourceSpan);
            while (node != null && node != root)
            {
                if (node.IsKind(SyntaxKind.UnsafeStatement))
                    return true;

                // Check for unsafe modifier on the containing member or type
                if (node is MethodDeclarationSyntax method && method.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is LocalFunctionStatementSyntax localFunc && localFunc.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is PropertyDeclarationSyntax prop && prop.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is IndexerDeclarationSyntax indexer && indexer.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is OperatorDeclarationSyntax op && op.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is ConversionOperatorDeclarationSyntax conv && conv.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is ConstructorDeclarationSyntax ctor && ctor.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;
                if (node is TypeDeclarationSyntax type && type.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return true;

                // Break out of lambdas/anonymous methods - they create a new scope
                if (node.IsKind(SyntaxKind.AnonymousMethodExpression)
                    || node.IsKind(SyntaxKind.SimpleLambdaExpression)
                    || node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                    break;

                node = node.Parent;
            }

            return false;
        }

        protected override bool VerifyAttributeArguments(AttributeData attribute) => true;

        protected override string GetMessageFromAttribute(AttributeData? requiresAttribute) => "";
    }
}
#endif
