// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class AddMarshalAsToElementFixer : CodeFixProvider
    {
        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(GeneratorDiagnostics.Ids.NotRecommendedGeneratedComInterfaceUsage);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root and semantic model
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            SyntaxNode node = root.FindNode(context.Span);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue(GeneratorDiagnosticProperties.AddMarshalAsAttribute, out string? addMarshalAsAttribute))
                {
                    continue;
                }

                foreach (var unmanagedType in addMarshalAsAttribute.Split(','))
                {
                    string unmanagedTypeName = unmanagedType.Trim();
                    context.RegisterCodeFix(
                            CodeAction.Create(
                                $"Add [MarshalAs(UnmanagedType.{unmanagedTypeName})]",
                                async ct =>
                                {
                                    DocumentEditor editor = await DocumentEditor.CreateAsync(doc, ct).ConfigureAwait(false);

                                    SyntaxGenerator gen = editor.Generator;

                                    SyntaxNode marshalAsAttribute = gen.Attribute(
                                                TypeNames.System_Runtime_InteropServices_MarshalAsAttribute,
                                                gen.AttributeArgument(
                                                    gen.MemberAccessExpression(
                                                        gen.DottedName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                                                        gen.IdentifierName(unmanagedTypeName.Trim()))));

                                    if (node.IsKind(SyntaxKind.MethodDeclaration))
                                    {
                                        editor.AddReturnAttribute(node, marshalAsAttribute);
                                    }
                                    else
                                    {
                                        editor.AddAttribute(node, marshalAsAttribute);
                                    }

                                    return editor.GetChangedDocument();
                                },
                                $"AddUnmanagedType.{unmanagedTypeName}"),
                            diagnostic);
                }
            }
        }
    }
}
