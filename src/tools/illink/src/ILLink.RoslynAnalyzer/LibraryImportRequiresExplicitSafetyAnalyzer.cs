// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Reports <c>IL5007</c> for methods with <c>LibraryImportAttribute</c> that do not declare an explicit
    /// <c>unsafe</c> or <c>safe</c> contract.
    /// </summary>
    /// <remarks>
    /// The compiler only requires the modifier when the generated implementing part is <c>extern</c>, which
    /// depends on whether the signature needs marshalling. Requiring it for every shape keeps the contract
    /// stable and matches what the language asks of <c>extern</c> members.
    /// <para>
    /// This is a migration aid for a code base that has not opted into the updated rules yet, so that
    /// <c>[LibraryImport]</c> methods can be annotated ahead of the switch. Once the rules are on the generator
    /// reports <c>SYSLIB1064</c> for the same methods, so this analyzer stands down to avoid reporting twice.
    /// It is disabled by default while this migration tooling remains experimental.
    /// </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LibraryImportRequiresExplicitSafetyAnalyzer : DiagnosticAnalyzer
    {
        private const string LibraryImportAttributeNamespace = "System.Runtime.InteropServices";
        private const string LibraryImportAttributeName = "LibraryImportAttribute";

        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.LibraryImportRequiresExplicitSafety,
                isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Both parts of a partial member are analyzed as separate symbols, so report only from the part the
            // user authored.
            if (method.PartialDefinitionPart is not null)
                return;

            foreach (AttributeData attribute in method.GetAttributes())
            {
                if (attribute.AttributeClass is not { } attributeClass
                    || !attributeClass.IsTypeOf(LibraryImportAttributeNamespace, LibraryImportAttributeName))
                {
                    continue;
                }

                // The source generator never copies LibraryImportAttribute onto the implementing part, so the
                // attribute application always points at the declaration the user authored.
                if (attribute.ApplicationSyntaxReference is not { } attributeReference
                    || attributeReference.GetSyntax(context.CancellationToken).FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } declaration
                    // Once the assembly is on the updated rules the generator reports SYSLIB1064 for the same
                    // methods, and this analyzer has nothing left to add.
                    || UnsafeMigrationSyntaxHelpers.UsesUpdatedMemorySafetyRules(declaration.SyntaxTree)
                    || UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                    || UnsafeMigrationSyntaxHelpers.HasSafeModifier(declaration))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(s_rule, declaration.Identifier.GetLocation()));
                return;
            }
        }
    }
}
#endif
