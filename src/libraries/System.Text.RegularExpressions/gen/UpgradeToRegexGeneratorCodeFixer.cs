// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Roslyn code fixer that will listen to SysLIB1046 diagnostics and will provide a code fix which onboards a particular Regex into
    /// source generation.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UpgradeToRegexGeneratorCodeFixer : CodeFixProvider
    {
        private const string RegexTypeName = "System.Text.RegularExpressions.Regex";
        private const string RegexGeneratorTypeName = "System.Text.RegularExpressions.RegexGeneratorAttribute";
        private const string DefaultRegexMethodName = "MyRegex";

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.UseRegexSourceGeneration.Id);

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Fetch the node to fix, and register the codefix by invoking the ConvertToSourceGenerator method.
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return;
            }

            SyntaxNode nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    SR.UseRegexSourceGeneratorTitle,
                    cancellationToken => ConvertToSourceGenerator(context.Document, context.Diagnostics[0], cancellationToken),
                    equivalenceKey: SR.UseRegexSourceGeneratorTitle),
                context.Diagnostics);
        }

        /// <summary>
        /// Takes a <see cref="Document"/> and a <see cref="Diagnostic"/> and returns a new <see cref="Document"/> with the replaced
        /// nodes in order to apply the code fix to the diagnostic.
        /// </summary>
        /// <param name="document">The original document.</param>
        /// <param name="diagnostic">The diagnostic to fix.</param>
        /// <param name="cancellationToken">The cancellation token for the async operation.</param>
        /// <returns>The new document with the replaced nodes after applying the code fix.</returns>
        private static async Task<Document> ConvertToSourceGenerator(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            // We first get the compilation object from the document
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                return document;
            }
            Compilation compilation = semanticModel.Compilation;

            // We then get the symbols for the Regex and RegexGeneratorAttribute types.
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            INamedTypeSymbol? regexGeneratorAttributeSymbol = compilation.GetTypeByMetadataName(RegexGeneratorTypeName);
            if (regexSymbol is null || regexGeneratorAttributeSymbol is null)
            {
                return document;
            }

            // Find the node that corresponding to the diagnostic which we will then fix.
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return document;
            }

            SyntaxNode nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            // Save the operation object from the nodeToFix before it gets replaced by the new method invocation.
            // We will later use this operation to get the parameters out and pass them into the RegexGenerator attribute.
            IOperation? operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
            if (operation is null)
            {
                return document;
            }

            // Calculate what name should be used for the generated static partial method
            string methodName = DefaultRegexMethodName;
            int memberCount = 1;
            while (!semanticModel.LookupSymbols(nodeToFix.SpanStart, name: methodName).IsEmpty)
            {
                methodName = $"{DefaultRegexMethodName}{memberCount++}";
            }

            // We generate a new invocation node to call our new partial method, and use it to replace the nodeToFix.
            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
            ImmutableDictionary<string, string?> properties = diagnostic.Properties;

            var annotation = new SyntaxAnnotation();

            SyntaxNode replacement = generator.InvocationExpression(generator.IdentifierName(methodName));

            if (operation is IInvocationOperation invocationOperation) // When using a Regex static method
            {
                IEnumerable<SyntaxNode> arguments = invocationOperation.Arguments
                    .Where(arg => arg.Parameter.Name is not (UpgradeToRegexGeneratorAnalyzer.OptionsArgumentName or UpgradeToRegexGeneratorAnalyzer.PatternArgumentName))
                    .Select(arg => arg.Syntax);

                replacement = generator.InvocationExpression(generator.MemberAccessExpression(replacement, invocationOperation.TargetMethod.Name), arguments);
            }

            root = root.ReplaceNode(nodeToFix, replacement.WithAdditionalAnnotations(annotation));

            SyntaxNode invocationNode = root.GetAnnotatedNodes(annotation).Single();
            var hasTypeDeclaration = false;
            // Walk the type hirerarchy of the node to fix, and add the partial modifier to each ancestor (if it doesn't have it already)
            root = root.ReplaceNodes(
                invocationNode.Ancestors().OfType<TypeDeclarationSyntax>(),
                (_, typeDeclaration) =>
                {
                    hasTypeDeclaration = true;
                    if (!typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)).WithAdditionalAnnotations(Simplifier.Annotation);
                    }

                    return typeDeclaration;
                });

            // Add the method to the type.
            MethodDeclarationSyntax newMethod = GenerateMethodDeclaration(methodName, generator, regexSymbol, regexGeneratorAttributeSymbol, properties);
            if (hasTypeDeclaration)
            {
                TypeDeclarationSyntax typeDeclaration = root.GetAnnotatedNodes(annotation).Single().FirstAncestorOrSelf<TypeDeclarationSyntax>();
                return document.WithSyntaxRoot(root.ReplaceNode(typeDeclaration, typeDeclaration.AddMembers(newMethod)));
            }
            else
            {
                // If we didn't have a type declaration, then it's likely we're in a top-level statements program.
                var topLevelClassDeclaration = (ClassDeclarationSyntax)generator.ClassDeclaration("Program", modifiers: DeclarationModifiers.Partial, members: new[] { newMethod });
                return document.WithSyntaxRoot(((CompilationUnitSyntax)root).AddMembers(topLevelClassDeclaration));
            }
        }

        private static MethodDeclarationSyntax GenerateMethodDeclaration(string methodName, SyntaxGenerator generator, ITypeSymbol regexSymbol, ITypeSymbol regexGeneratorAttributeSymbol, ImmutableDictionary<string, string?> properties)
        {
            // Initialize the inputs for the RegexGenerator attribute.
            SyntaxNode? patternValue = GetNode(properties, UpgradeToRegexGeneratorAnalyzer.PatternKeyName, generator, useOptionsMemberExpression: false);
            SyntaxNode? regexOptionsValue = GetNode(properties, UpgradeToRegexGeneratorAnalyzer.RegexOptionsKeyName, generator, useOptionsMemberExpression: true);

            // Generate the new static partial method
            MethodDeclarationSyntax newMethod = (MethodDeclarationSyntax)generator.MethodDeclaration(
                name: methodName,
                returnType: generator.TypeExpression(regexSymbol),
                modifiers: DeclarationModifiers.Static | DeclarationModifiers.Partial,
                accessibility: Accessibility.Private);

            // Allow user to pick a different name for the method.
            newMethod = newMethod.ReplaceToken(newMethod.Identifier, SyntaxFactory.Identifier(methodName).WithAdditionalAnnotations(RenameAnnotation.Create()));

            // Generate the RegexGenerator attribute syntax node with the specified parameters.
            SyntaxNode attributes = generator.Attribute(generator.TypeExpression(regexGeneratorAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue) switch
            {
                ({ }, null) => new[] { patternValue },
                ({ }, { }) => new[] { patternValue, regexOptionsValue },
                _ => Array.Empty<SyntaxNode>(),
            });

            // Add the attribute to the generated method.
            return (MethodDeclarationSyntax)generator.AddAttributes(newMethod, attributes);

            // Helper method that looks int the properties bag for the index of the passed in propertyname, and then returns that index from the args parameter.
            static SyntaxNode? GetNode(ImmutableDictionary<string, string?> properties, string propertyName, SyntaxGenerator generator, bool useOptionsMemberExpression)
            {
                string? propertyValue = properties[propertyName];
                if (propertyValue is null)
                {
                    return null;
                }

                if (!useOptionsMemberExpression)
                {
                    return generator.LiteralExpression(propertyValue);
                }
                else
                {
                    return Literal(propertyValue, generator);
                }
            }

            static SyntaxNode? Literal(string options, SyntaxGenerator generator)
            {
                if (int.TryParse(options, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    // The options were formatted as an int, which means the runtime couldn't
                    // produce a textual representation.  So just output casting the value as an int.
                    Debug.Fail("This shouldn't happen, as we should only get to the point of emitting code if RegexOptions was valid.");
                    return null;
                }

                // Parse the runtime-generated "Option1, Option2" into each piece and then concat
                // them back together.
                string[] parts = options.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                SyntaxNode regexOptionsExpression = generator.IdentifierName("RegexOptions");
                SyntaxNode result = generator.MemberAccessExpression(regexOptionsExpression, parts[0].Trim());
                for (int i = 1; i < parts.Length; i++)
                {
                    result = generator.BitwiseOrExpression(result, generator.MemberAccessExpression(regexOptionsExpression, parts[i].Trim()));
                }

                return result;
            }
        }
    }
}
