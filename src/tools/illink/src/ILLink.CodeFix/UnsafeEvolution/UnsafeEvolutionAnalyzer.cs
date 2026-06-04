// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.CodeFix.UnsafeEvolution
{
    /// <summary>
    /// Reports IL5005 (meaningless <c>unsafe</c> modifier on a type / static ctor / destructor / delegate)
    /// and IL5006 (probably-unnecessary <c>unsafe</c> modifier on a member whose signature has no pointer types).
    /// </summary>
    /// <remarks>
    /// These supplement compiler diagnostics so that <c>dotnet format</c> can drive the
    /// <see cref="RemoveUnsafeModifierCodeFixProvider"/> on assemblies that have not yet opted into
    /// the updated memory-safety rules.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeEvolutionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier,
            UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier,
        ];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // IL5005 - meaningless unsafe on types and special members.
            context.RegisterSyntaxNodeAction(AnalyzeMeaninglessUnsafe,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DestructorDeclaration);

            // IL5006 - probably-unnecessary unsafe on a member whose signature has no pointer types.
            context.RegisterSyntaxNodeAction(AnalyzeUnnecessaryUnsafe,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.IndexerDeclaration,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.EventFieldDeclaration,
                SyntaxKind.EventDeclaration);
        }

        // ---- IL5005: meaningless 'unsafe' ----

        private static void AnalyzeMeaninglessUnsafe(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var (modifiers, kind, name) = node switch
            {
                ClassDeclarationSyntax c => (c.Modifiers, "class", c.Identifier.ValueText),
                StructDeclarationSyntax s => (s.Modifiers, "struct", s.Identifier.ValueText),
                InterfaceDeclarationSyntax i => (i.Modifiers, "interface", i.Identifier.ValueText),
                RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: (int)SyntaxKind.StructKeyword } r
                    => (r.Modifiers, "record struct", r.Identifier.ValueText),
                RecordDeclarationSyntax r => (r.Modifiers, "record", r.Identifier.ValueText),
                DelegateDeclarationSyntax d => (d.Modifiers, "delegate", d.Identifier.ValueText),
                ConstructorDeclarationSyntax c => (c.Modifiers, "static constructor", c.Identifier.ValueText),
                DestructorDeclarationSyntax d => (d.Modifiers, "destructor", d.Identifier.ValueText),
                _ => (default(SyntaxTokenList), "", ""),
            };

            // Instance constructors can legitimately be requires-unsafe; only static ones are meaningless.
            if (node is ConstructorDeclarationSyntax && !modifiers.Any(SyntaxKind.StaticKeyword))
                return;

            var unsafeToken = UnsafeBlockHelpers.FindUnsafeModifier(modifiers);
            if (unsafeToken == default)
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                UnsafeEvolutionDescriptors.MeaninglessUnsafeModifier,
                unsafeToken.GetLocation(),
                kind,
                name));
        }

        // ---- IL5006: probably-unnecessary 'unsafe' on a signature without pointers ----

        private static void AnalyzeUnnecessaryUnsafe(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var (modifiers, name) = node switch
            {
                MethodDeclarationSyntax m => (m.Modifiers, m.Identifier.ValueText),
                LocalFunctionStatementSyntax lf => (lf.Modifiers, lf.Identifier.ValueText),
                PropertyDeclarationSyntax p => (p.Modifiers, p.Identifier.ValueText),
                IndexerDeclarationSyntax => (((IndexerDeclarationSyntax)node).Modifiers, "this[]"),
                FieldDeclarationSyntax f => (f.Modifiers, FirstVariableName(f.Declaration)),
                EventFieldDeclarationSyntax ef => (ef.Modifiers, FirstVariableName(ef.Declaration)),
                EventDeclarationSyntax e => (e.Modifiers, e.Identifier.ValueText),
                _ => (default(SyntaxTokenList), ""),
            };

            if (!HasRemovableUnsafe(node, modifiers))
                return;

            if (UnsafeBlockHelpers.SignatureContainsPointer(node))
                return;

            var unsafeToken = UnsafeBlockHelpers.FindUnsafeModifier(modifiers);
            context.ReportDiagnostic(Diagnostic.Create(
                UnsafeEvolutionDescriptors.UnnecessaryUnsafeModifier,
                unsafeToken.GetLocation(),
                name));
        }

        private static string FirstVariableName(VariableDeclarationSyntax decl)
            => decl.Variables.Count > 0 ? decl.Variables[0].Identifier.ValueText : "?";

        // ---- Common 'should we suggest removing the unsafe modifier?' predicate ----

        private static bool HasRemovableUnsafe(SyntaxNode decl, SyntaxTokenList modifiers)
        {
            if (!modifiers.Any(SyntaxKind.UnsafeKeyword))
                return false;

            // 'extern' members must be explicitly marked unsafe or safe in the new rules; don't suggest removal.
            if (modifiers.Any(SyntaxKind.ExternKeyword))
                return false;

            // Partial members require both halves to agree on 'unsafe'; we can't fix one safely.
            if (modifiers.Any(SyntaxKind.PartialKeyword))
                return false;

            // Be conservative for members nested inside a type that also carries 'unsafe' - the
            // type-level IL5005 will fire on the containing type, which is the better fix.
            for (SyntaxNode? p = decl.Parent; p is not null; p = p.Parent)
            {
                if (p is TypeDeclarationSyntax td && td.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                    return false;
            }

            return true;
        }
    }
}
#endif
