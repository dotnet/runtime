// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public sealed class ConvertComImportToGeneratedComInterfaceCodeFixProvider : CodeFixProvider
    {
        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerDiagnostics.Ids.ConvertToGeneratedComInterface);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);
            if (node == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    SR.ConvertToGeneratedComInterfaceTitle,
                    ct => ConvertComImportToGeneratedComInterfaceAsync(
                        context.Document,
                        node,
                        mayRequireAdditionalWork: ParseOption(diagnostic, AnalyzerDiagnostics.Metadata.MayRequireAdditionalWork),
                        addStringMarshalling: ParseOption(diagnostic, AnalyzerDiagnostics.Metadata.AddStringMarshalling),
                        ct),
                    nameof(ConvertComImportToGeneratedComInterfaceAsync)),
                diagnostic);
        }

        private static bool ParseOption(Diagnostic diagnostic, string optionName)
        {
            var options = diagnostic.Properties;
            return options.TryGetValue(optionName, out string valueAsString) && bool.TryParse(valueAsString, out bool value) && value;
        }

        private static async Task<Document> ConvertComImportToGeneratedComInterfaceAsync(Document document, SyntaxNode node, bool mayRequireAdditionalWork, bool addStringMarshalling, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var gen = editor.Generator;
            var comp = editor.SemanticModel.Compilation;
            var declaringType = editor.SemanticModel.GetDeclaredSymbol(node, ct);

            var generatedComInterfaceAttribute = gen.Attribute(gen.TypeExpression(comp.GetTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute)).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation));

            if (addStringMarshalling)
            {
                generatedComInterfaceAttribute = gen.AddAttributeArguments(
                    generatedComInterfaceAttribute,
                    new[]
                    {
                       gen.AttributeArgument("StringMarshalling", gen.MemberAccessExpression(gen.DottedName(TypeNames.StringMarshalling), gen.IdentifierName(nameof(StringMarshalling.Custom)))),
                       gen.AttributeArgument("StringMarshallingCustomType", gen.TypeOfExpression(gen.TypeExpression(comp.GetTypeByMetadataName(TypeNames.BStrStringMarshaller))))
                    });
            }

            if (mayRequireAdditionalWork)
            {
                generatedComInterfaceAttribute = generatedComInterfaceAttribute.WithAdditionalAnnotations(
                    WarningAnnotation.Create(""));
            }

            var comImportAttributeType = comp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_ComImportAttribute);

            var comImportAttribute = await declaringType.GetAttributes().First(attr => attr.AttributeClass.Equals(comImportAttributeType, SymbolEqualityComparer.Default)).ApplicationSyntaxReference.GetSyntaxAsync(ct).ConfigureAwait(false);

            editor.ReplaceNode(comImportAttribute, generatedComInterfaceAttribute);

            foreach (var member in gen.GetMembers(node))
            {
                if (gen.GetDeclarationKind(member) != DeclarationKind.Method)
                {
                    continue;
                }

                var declarationModifiers = gen.GetModifiers(member);

                if (declarationModifiers.IsStatic)
                {
                    continue;
                }

                if (declarationModifiers.IsNew)
                {
                    // If this is a shadowing method, then we remove it.
                    // TODO: Do we want to be smarter here and try to match the number of methods to a base interface, etc.?
                    editor.RemoveNode(member);
                    continue;
                }

                IMethodSymbol method = (IMethodSymbol)editor.SemanticModel.GetDeclaredSymbol(member, ct);
                var generatedDeclaration = member;

                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    if (parameter.Type.SpecialType == SpecialType.System_Boolean
                        && !parameter.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                    {
                        var parameters = gen.GetParameters(member);
                        var parameterSyntax = parameters[parameter.Ordinal];
                        generatedDeclaration = gen.ReplaceNode(
                            member,
                            parameterSyntax,
                            gen.AddAttributes(
                                parameterSyntax,
                                GenerateMarshalAsUnmanagedTypeVariantBoolAttribute(gen)));
                    }
                }

                if (method.ReturnType.SpecialType == SpecialType.System_Boolean
                    && !method.GetReturnTypeAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                {
                    generatedDeclaration = gen.AddReturnAttributes(
                        generatedDeclaration,
                        GenerateMarshalAsUnmanagedTypeVariantBoolAttribute(gen));
                }
                editor.ReplaceNode(member, generatedDeclaration);
            }

            return editor.GetChangedDocument();
        }

        private static SyntaxNode GenerateMarshalAsUnmanagedTypeVariantBoolAttribute(SyntaxGenerator generator)
         => generator.Attribute(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute,
             generator.AttributeArgument(
                 generator.MemberAccessExpression(
                     generator.DottedName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                     generator.IdentifierName("VariantBool"))));

    }
}
