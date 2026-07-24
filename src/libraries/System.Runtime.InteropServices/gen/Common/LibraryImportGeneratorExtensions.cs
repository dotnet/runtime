// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop
{
    internal static class LibraryImportGeneratorExtensions
    {
        private const string SafeModifier = "safe";
        private const string UpdatedMemorySafetyRulesFeature = "updated-memory-safety-rules";

        /// <summary>
        /// Checks if a method is invalid for generation and returns a diagnostic if so.
        /// </summary>
        /// <returns>A diagnostic if the method is invalid, null otherwise.</returns>
        internal static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(this MethodDeclarationSyntax methodSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is marked static and partial.
            if (methodSyntax.TypeParameterList is not null
                || methodSyntax.Body is not null
                || !methodSyntax.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !methodSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            if (UsesUpdatedMemorySafetyRules(methodSyntax.SyntaxTree)
                && !HasSafetyModifier(methodSyntax.Modifiers))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodMissingSafetyModifier, methodSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify that the types the method is declared in are marked partial.
            if (methodSyntax.Parent is TypeDeclarationSyntax typeDecl && !typeDecl.IsInPartialContext(out var nonPartialIdentifier))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers, methodSyntax.Identifier.GetLocation(), method.Name, nonPartialIdentifier);
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, methodSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }

        private static bool HasSafetyModifier(SyntaxTokenList modifiers)
        {
            foreach (SyntaxToken modifier in modifiers)
            {
                // LibraryImportGenerator supports compiler hosts whose public SyntaxKind API predates SafeKeyword.
                if (modifier.IsKind(SyntaxKind.UnsafeKeyword) || modifier.ValueText == SafeModifier)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool UsesUpdatedMemorySafetyRules(SyntaxTree syntaxTree)
            => syntaxTree.Options.Features.ContainsKey(UpdatedMemorySafetyRulesFeature);
    }
}
