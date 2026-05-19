// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS2008 // Not a shipping analyzer

namespace Microsoft.DotNet.Analyzers.PlatformDoc
{
    /// <summary>
    /// Ensures platform-specific libraries with UseCompilerGeneratedDocXmlFile=true
    /// place their triple-slash documentation on the primary source file so that
    /// docs are consistent across platforms.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PlatformDocAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdMissingPrimaryFile = "PLATDOC001";
        public const string DiagnosticIdBadPartialFileName = "PLATDOC002";
        public const string DiagnosticIdDocsOnNonPrimaryFile = "PLATDOC003";

        public static readonly DiagnosticDescriptor MissingPrimaryFileRule = new(
            DiagnosticIdMissingPrimaryFile,
            "Public type missing primary source file",
            "Public type '{0}' has no source file named '{0}.cs'",
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor BadPartialFileNameRule = new(
            DiagnosticIdBadPartialFileName,
            "Partial source file doesn't follow naming convention",
            "Source file '{0}' contains a partial definition of type '{1}' but doesn't follow the '{1}.*.cs' naming convention",
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DocsOnNonPrimaryFileRule = new(
            DiagnosticIdDocsOnNonPrimaryFile,
            "XML documentation on non-primary partial file",
            "Public member '{0}' in file '{1}' has XML documentation that should be moved to '{2}.cs'",
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingPrimaryFileRule, BadPartialFileNameRule, DocsOnNonPrimaryFileRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

            if (!options.TryGetValue("build_property.TargetFramework", out string? tfm) ||
                string.IsNullOrEmpty(tfm) ||
                !IsPlatformSpecificTfm(tfm))
            {
                return;
            }

            if (!options.TryGetValue("build_property.UseCompilerGeneratedDocXmlFile", out string? useCompilerDoc) ||
                !string.Equals(useCompilerDoc, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        private static bool IsPlatformSpecificTfm(string tfm)
        {
            // Platform-specific TFMs have a platform suffix: net10.0-windows, net9.0-linux, etc.
            int dashIndex = tfm.IndexOf('-');
            return dashIndex > 0;
        }

        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var namedType = (INamedTypeSymbol)context.Symbol;

            if (namedType.DeclaredAccessibility != Accessibility.Public)
                return;

            // Only check top-level types, not nested types.
            if (namedType.ContainingType is not null)
                return;

            ImmutableArray<SyntaxReference> syntaxRefs = namedType.DeclaringSyntaxReferences;
            if (syntaxRefs.IsEmpty)
                return;

            string typeName = namedType.Name;
            string primaryFileName = typeName + ".cs";

            bool hasPrimaryFile = false;
            var nonPrimaryRefs = new List<(SyntaxReference SyntaxRef, string FileName)>();

            foreach (SyntaxReference syntaxRef in syntaxRefs)
            {
                string filePath = syntaxRef.SyntaxTree.FilePath;
                string fileName = Path.GetFileName(filePath);

                if (string.Equals(fileName, primaryFileName, StringComparison.OrdinalIgnoreCase))
                {
                    hasPrimaryFile = true;
                }
                else
                {
                    nonPrimaryRefs.Add((syntaxRef, fileName));
                }
            }

            // PLATDOC001: Public type must have a primary source file named TypeName.cs
            if (!hasPrimaryFile)
            {
                foreach (SyntaxReference syntaxRef in syntaxRefs)
                {
                    SyntaxNode node = syntaxRef.GetSyntax(context.CancellationToken);
                    if (node is TypeDeclarationSyntax typeDecl)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            MissingPrimaryFileRule,
                            typeDecl.Identifier.GetLocation(),
                            typeName));
                    }
                }
            }

            foreach ((SyntaxReference syntaxRef, string fileName) in nonPrimaryRefs)
            {
                SyntaxNode node = syntaxRef.GetSyntax(context.CancellationToken);
                if (node is not TypeDeclarationSyntax typeDecl)
                    continue;

                // PLATDOC002: Non-primary files must follow TypeName.Something.cs convention
                string prefix = typeName + ".";
                if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Length <= prefix.Length + ".cs".Length - 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        BadPartialFileNameRule,
                        typeDecl.Identifier.GetLocation(),
                        fileName,
                        typeName));
                }

                // PLATDOC003: Public members in non-primary files must not have XML docs
                CheckMembersForDocs(context, typeDecl, fileName, typeName);
            }
        }

        private static void CheckMembersForDocs(
            SymbolAnalysisContext context,
            TypeDeclarationSyntax typeDecl,
            string fileName,
            string typeName)
        {
            foreach (MemberDeclarationSyntax member in typeDecl.Members)
            {
                if (!IsEffectivelyPublic(member, typeDecl))
                    continue;

                if (HasXmlDocComment(member))
                {
                    string memberName = GetMemberName(member);
                    context.ReportDiagnostic(Diagnostic.Create(
                        DocsOnNonPrimaryFileRule,
                        GetMemberIdentifierLocation(member),
                        memberName,
                        fileName,
                        typeName));
                }
            }
        }

        private static bool IsEffectivelyPublic(MemberDeclarationSyntax member, TypeDeclarationSyntax containingType)
        {
            // Interface members are implicitly public
            if (containingType is InterfaceDeclarationSyntax)
                return true;

            // Enum members are implicitly public
            if (member is EnumMemberDeclarationSyntax)
                return true;

            return member.Modifiers.Any(SyntaxKind.PublicKeyword);
        }

        private static bool HasXmlDocComment(MemberDeclarationSyntax member)
        {
            foreach (SyntaxTrivia trivia in member.GetLeadingTrivia())
            {
                SyntaxKind kind = trivia.Kind();
                if (kind == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                    kind == SyntaxKind.MultiLineDocumentationCommentTrivia)
                {
                    return true;
                }
            }

            return false;
        }

        private static Location GetMemberIdentifierLocation(MemberDeclarationSyntax member)
        {
            return member switch
            {
                MethodDeclarationSyntax m => m.Identifier.GetLocation(),
                PropertyDeclarationSyntax p => p.Identifier.GetLocation(),
                EventDeclarationSyntax e => e.Identifier.GetLocation(),
                EventFieldDeclarationSyntax ef => ef.Declaration.Variables.FirstOrDefault()?.Identifier.GetLocation() ?? member.GetLocation(),
                FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.GetLocation() ?? member.GetLocation(),
                ConstructorDeclarationSyntax c => c.Identifier.GetLocation(),
                DestructorDeclarationSyntax d => d.Identifier.GetLocation(),
                IndexerDeclarationSyntax i => i.ThisKeyword.GetLocation(),
                OperatorDeclarationSyntax o => o.OperatorToken.GetLocation(),
                ConversionOperatorDeclarationSyntax c => c.Type.GetLocation(),
                TypeDeclarationSyntax t => t.Identifier.GetLocation(),
                DelegateDeclarationSyntax d => d.Identifier.GetLocation(),
                EnumMemberDeclarationSyntax e => e.Identifier.GetLocation(),
                _ => member.GetLocation()
            };
        }

        private static string GetMemberName(MemberDeclarationSyntax member)
        {
            return member switch
            {
                MethodDeclarationSyntax m => m.Identifier.Text,
                PropertyDeclarationSyntax p => p.Identifier.Text,
                EventDeclarationSyntax e => e.Identifier.Text,
                EventFieldDeclarationSyntax ef => ef.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "<event>",
                FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? "<field>",
                ConstructorDeclarationSyntax c => c.Identifier.Text,
                DestructorDeclarationSyntax d => "~" + d.Identifier.Text,
                IndexerDeclarationSyntax => "this[]",
                OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text,
                ConversionOperatorDeclarationSyntax c => c.ImplicitOrExplicitKeyword.Text + " operator " + c.Type,
                TypeDeclarationSyntax t => t.Identifier.Text,
                DelegateDeclarationSyntax d => d.Identifier.Text,
                EnumMemberDeclarationSyntax e => e.Identifier.Text,
                _ => "<member>"
            };
        }
    }
}
