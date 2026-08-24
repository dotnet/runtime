// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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
        private static readonly Regex s_memberRegex = new(@"<member\s+name=""([^""]+)"">(.*?)</member>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex s_whitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        public const string DiagnosticIdMissingPrimaryFile = "PLATDOC001";
        public const string DiagnosticIdBadPartialFileName = "PLATDOC002";
        public const string DiagnosticIdDocsOnNonPrimaryFile = "PLATDOC003";
        public const string DiagnosticIdDocMismatch = "PLATDOC004";

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

        public static readonly DiagnosticDescriptor DocMismatchRule = new(
            DiagnosticIdDocMismatch,
            "Documentation differs from canonical platform-agnostic build",
            "Documentation for '{0}' differs from the canonical (platform-agnostic) build. Ensure docs are on shared source so they are consistent across platforms.",
            "Documentation",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingPrimaryFileRule, BadPartialFileNameRule, DocsOnNonPrimaryFileRule, DocMismatchRule);

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

            // Look for the canonical doc XML in AdditionalFiles.
            Dictionary<string, string>? canonicalDocs = TryLoadCanonicalDocs(context);
            if (canonicalDocs is not null)
            {
                context.RegisterSymbolAction(
                    ctx => AnalyzeDocConsistency(ctx, canonicalDocs),
                    SymbolKind.NamedType);
            }
        }

        private static Dictionary<string, string>? TryLoadCanonicalDocs(CompilationStartAnalysisContext context)
        {
            foreach (AdditionalText file in context.Options.AdditionalFiles)
            {
                AnalyzerConfigOptions fileOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(file);
                if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.PlatformDocCanonical", out string? value) ||
                    !string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SourceText? text = file.GetText(context.CancellationToken);
                if (text is null)
                    continue;

                return ParseDocXml(text.ToString());
            }

            return null;
        }

        private static Dictionary<string, string> ParseDocXml(string xml)
        {
            var docs = new Dictionary<string, string>(StringComparer.Ordinal);

            // Use regex to extract member elements to avoid XML parser normalization
            // that could cause false mismatches with GetDocumentationCommentXml() output.
            foreach (Match match in s_memberRegex.Matches(xml))
            {
                string name = match.Groups[1].Value;
                string innerXml = match.Groups[2].Value;
                docs[name] = NormalizeDocXml(innerXml);
            }

            return docs;
        }

        private static void AnalyzeDocConsistency(
            SymbolAnalysisContext context,
            Dictionary<string, string> canonicalDocs)
        {
            ISymbol symbol = context.Symbol;

            if (symbol is not INamedTypeSymbol namedType)
                return;

            if (namedType.DeclaredAccessibility != Accessibility.Public)
                return;

            // Check the type itself and all its public members.
            CheckSymbolDoc(context, namedType, canonicalDocs);

            foreach (ISymbol member in namedType.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public)
                    continue;

                // Skip accessors; they're covered by the property/event.
                if (member is IMethodSymbol { AssociatedSymbol: not null })
                    continue;

                // Skip nested types; they get their own SymbolKind.NamedType callback.
                if (member is INamedTypeSymbol)
                    continue;

                CheckSymbolDoc(context, member, canonicalDocs);
            }
        }

        private static void CheckSymbolDoc(
            SymbolAnalysisContext context,
            ISymbol symbol,
            Dictionary<string, string> canonicalDocs)
        {
            string? docId = symbol.GetDocumentationCommentId();
            if (docId is null)
                return;

            if (!canonicalDocs.TryGetValue(docId, out string? canonicalDoc))
                return;

            string currentDoc = NormalizeDocXml(
                symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: context.CancellationToken) ?? "");

            // Strip the outer <member> wrapper from GetDocumentationCommentXml() output.
            currentDoc = StripMemberWrapper(currentDoc);

            if (string.Equals(canonicalDoc, currentDoc, StringComparison.Ordinal))
                return;

            // Both empty → no mismatch.
            if (string.IsNullOrWhiteSpace(canonicalDoc) && string.IsNullOrWhiteSpace(currentDoc))
                return;

            Location location = symbol.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(
                DocMismatchRule,
                location,
                symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }

        private static string StripMemberWrapper(string xml)
        {
            // GetDocumentationCommentXml() returns:
            //   <member name="...">..inner..</member>
            // We need just the inner content to compare against the parsed doc XML.
            const string memberStart = "<member";
            const string memberEnd = "</member>";

            int startIdx = xml.IndexOf('>', xml.IndexOf(memberStart, StringComparison.Ordinal) + 1);
            int endIdx = xml.LastIndexOf(memberEnd, StringComparison.Ordinal);

            if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
                return NormalizeDocXml(xml);

            return NormalizeDocXml(xml.Substring(startIdx + 1, endIdx - startIdx - 1));
        }

        private static string NormalizeDocXml(string xml)
        {
            // Normalize whitespace: collapse runs of whitespace into single spaces, trim.
            return s_whitespaceRegex.Replace(xml, " ").Trim();
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

            // Only check types declared across multiple files.
            // A type in a single file doesn't have a doc placement problem.
            var distinctFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SyntaxReference syntaxRef in syntaxRefs)
            {
                distinctFiles.Add(syntaxRef.SyntaxTree.FilePath);
            }

            if (distinctFiles.Count <= 1)
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
                // Nested type declarations define their own doc scope; skip them.
                if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                    continue;

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
            // Interface members without explicit access modifiers are implicitly public.
            // C# 8+ allows private/protected/internal members in interfaces.
            if (containingType is InterfaceDeclarationSyntax)
            {
                if (member.Modifiers.Count == 0)
                    return true;

                return !member.Modifiers.Any(SyntaxKind.PrivateKeyword) &&
                       !member.Modifiers.Any(SyntaxKind.ProtectedKeyword) &&
                       !member.Modifiers.Any(SyntaxKind.InternalKeyword);
            }

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
                EnumMemberDeclarationSyntax e => e.Identifier.Text,
                _ => "<member>"
            };
        }
    }
}
