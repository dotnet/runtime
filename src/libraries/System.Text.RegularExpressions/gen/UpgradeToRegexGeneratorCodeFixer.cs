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
using Microsoft.CodeAnalysis.Text;

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

            // Generate the modified type declaration depending on whether the callsite was a Regex constructor call
            // or a Regex static method invocation.
            if (operation is IInvocationOperation invocationOperation) // When using a Regex static method
            {
                ImmutableArray<IArgumentOperation> arguments = invocationOperation.Arguments;

                // Parse the idices for where to get the arguments from.
                int?[] indices = new[]
                {
                    TryParseInt32(properties, UpgradeToRegexGeneratorAnalyzer.PatternIndexName),
                    TryParseInt32(properties, UpgradeToRegexGeneratorAnalyzer.RegexOptionsIndexName)
                };

                foreach (int? index in indices.Where(value => value != null).OrderByDescending(value => value))
                {
                    arguments = arguments.RemoveAt(index.GetValueOrDefault());
                }

                SyntaxNode createRegexMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
                SyntaxNode method = generator.InvocationExpression(generator.MemberAccessExpression(createRegexMethod, invocationOperation.TargetMethod.Name), arguments.Select(arg => arg.Syntax).ToArray());

                root = root.ReplaceNode(nodeToFix, method.WithAdditionalAnnotations(annotation));
            }
            else // When using a Regex constructor
            {
                SyntaxNode invokeMethod = generator.InvocationExpression(generator.IdentifierName(methodName));
                root = root.ReplaceNode(nodeToFix, invokeMethod.WithAdditionalAnnotations(annotation));
            }

            // Initialize the inputs for the RegexGenerator attribute.
            SyntaxNode? patternValue = null;
            SyntaxNode? regexOptionsValue = null;

            // Try to get the pattern and RegexOptions values out from the diagnostic's property bag.
            if (operation is IObjectCreationOperation objectCreationOperation) // When using the Regex constructors
            {
                patternValue = GetNode((objectCreationOperation).Arguments, properties, UpgradeToRegexGeneratorAnalyzer.PatternIndexName, generator, useOptionsMemberExpression: false, compilation, cancellationToken);
                regexOptionsValue = GetNode((objectCreationOperation).Arguments, properties, UpgradeToRegexGeneratorAnalyzer.RegexOptionsIndexName, generator, useOptionsMemberExpression: true, compilation, cancellationToken);
            }
            else if (operation is IInvocationOperation invocation) // When using the Regex static methods.
            {
                patternValue = GetNode(invocation.Arguments, properties, UpgradeToRegexGeneratorAnalyzer.PatternIndexName, generator, useOptionsMemberExpression: false, compilation, cancellationToken);
                regexOptionsValue = GetNode(invocation.Arguments, properties, UpgradeToRegexGeneratorAnalyzer.RegexOptionsIndexName, generator, useOptionsMemberExpression: true, compilation, cancellationToken);
            }

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
            newMethod = (MethodDeclarationSyntax)generator.AddAttributes(newMethod, attributes);

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

            // Helper method that searches the passed in property bag for the property with the passed in name, and if found, it converts the
            // value to an int.
            static int? TryParseInt32(ImmutableDictionary<string, string?> properties, string name)
            {
                if (!properties.TryGetValue(name, out string? value))
                {
                    return null;
                }

                if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
                {
                    return null;
                }

                return result;
            }

            // Helper method that looks int the properties bag for the index of the passed in propertyname, and then returns that index from the args parameter.
            static SyntaxNode? GetNode(ImmutableArray<IArgumentOperation> args, ImmutableDictionary<string, string?> properties, string propertyName, SyntaxGenerator generator, bool useOptionsMemberExpression, Compilation compilation, CancellationToken cancellationToken)
            {
                int? index = TryParseInt32(properties, propertyName);
                if (index == null)
                {
                    return null;
                }

                if (!useOptionsMemberExpression)
                {
                    return generator.LiteralExpression(args[index.Value].Value.ConstantValue.Value);
                }
                else
                {
                    RegexOptions options = (RegexOptions)(int)args[index.Value].Value.ConstantValue.Value;
                    string optionsLiteral = Literal(options);
                    return SyntaxFactory.ParseExpression(optionsLiteral).SyntaxTree.GetRoot(cancellationToken);
                }
            }

            static string Literal(RegexOptions options)
            {
                string s = options.ToString();
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    // The options were formatted as an int, which means the runtime couldn't
                    // produce a textual representation.  So just output casting the value as an int.
                    Debug.Fail("This shouldn't happen, as we should only get to the point of emitting code if RegexOptions was valid.");
                    return $"(RegexOptions)({(int)options})";
                }

                // Parse the runtime-generated "Option1, Option2" into each piece and then concat
                // them back together.
                string[] parts = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = "RegexOptions." + parts[i].Trim();
                }
                return string.Join(" | ", parts);
            }
        }
    }
}
