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
using Microsoft.CodeAnalysis.Text;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Roslyn code fixer that will listen to SysLIB1046 diagnostics and will provide a code fix which onboards a particular Regex into
    /// source generation.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class UpgradeToGeneratedRegexCodeFixer : CodeFixProvider
    {
        private const string RegexTypeName = "System.Text.RegularExpressions.Regex";
        private const string GeneratedRegexTypeName = "System.Text.RegularExpressions.GeneratedRegexAttribute";
        private const string DefaultRegexMethodName = "MyRegex";

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.UseRegexSourceGeneration.Id);

        private static readonly char[] s_comma = [','];

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
                    cancellationToken => ConvertToSourceGenerator(context.Document, root, nodeToFix, cancellationToken),
                    equivalenceKey: SR.UseRegexSourceGeneratorTitle),
                context.Diagnostics);
        }

        /// <summary>
        /// Takes a <see cref="Document"/> and a <see cref="Diagnostic"/> and returns a new <see cref="Document"/> with the replaced
        /// nodes in order to apply the code fix to the diagnostic.
        /// </summary>
        /// <param name="document">The original document.</param>
        /// <param name="root">The root of the syntax tree.</param>
        /// <param name="nodeToFix">The node to fix. This is where the diagnostic was produced.</param>
        /// <param name="diagnostic">The diagnostic to fix.</param>
        /// <param name="cancellationToken">The cancellation token for the async operation.</param>
        /// <returns>The new document with the replaced nodes after applying the code fix.</returns>
        private static async Task<Document> ConvertToSourceGenerator(Document document, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            // We first get the compilation object from the document
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                return document;
            }
            Compilation compilation = semanticModel.Compilation;

            // We then get the symbols for the Regex and GeneratedRegexAttribute types.
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            INamedTypeSymbol? generatedRegexAttributeSymbol = compilation.GetTypeByMetadataName(GeneratedRegexTypeName);
            if (regexSymbol is null || generatedRegexAttributeSymbol is null)
            {
                return document;
            }

            // Save the operation object from the nodeToFix before it gets replaced by the new method invocation.
            // We will later use this operation to get the parameters out and pass them into the Regex attribute.
            IOperation? operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
            if (operation is null)
            {
                return document;
            }

            // Get the parent type declaration so that we can inspect its methods as well as check if we need to add the partial keyword.
            SyntaxNode? typeDeclarationOrCompilationUnit = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

            typeDeclarationOrCompilationUnit ??= await nodeToFix.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Calculate what name should be used for the generated static partial method
            string methodName = DefaultRegexMethodName;

            INamedTypeSymbol? typeSymbol = typeDeclarationOrCompilationUnit is TypeDeclarationSyntax typeDeclaration ?
                semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) :
                semanticModel.GetDeclaredSymbol((CompilationUnitSyntax)typeDeclarationOrCompilationUnit, cancellationToken)?.ContainingType;

            if (typeSymbol is not null)
            {
                IEnumerable<ISymbol> members = GetAllMembers(typeSymbol);
                int memberCount = 1;
                while (members.Any(m => m.Name == methodName))
                {
                    methodName = $"{DefaultRegexMethodName}{memberCount++}";
                }
            }

            // Walk the type hierarchy of the node to fix, and add the partial modifier to each ancestor (if it doesn't have it already)
            // We also keep a count of how many partial keywords we added so that we can later find the nodeToFix again on the new root using the text offset.
            int typesModified = 0;
            root = root.ReplaceNodes(
                nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>(),
                (_, typeDeclaration) =>
                {
                    if (!typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        typesModified++;
                        return typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                    }

                    return typeDeclaration;
                });

            // We find nodeToFix again by calculating the offset of how many partial keywords we had to add.
            nodeToFix = root.FindNode(new TextSpan(nodeToFix.Span.Start + (typesModified * "partial".Length), nodeToFix.Span.Length), getInnermostNodeForTie: true);
            if (nodeToFix is null)
            {
                return document;
            }

            // We need to find the typeDeclaration again, but now using the new root.
            typeDeclarationOrCompilationUnit = typeDeclarationOrCompilationUnit is TypeDeclarationSyntax ?
                nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() :
                await nodeToFix.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            Debug.Assert(typeDeclarationOrCompilationUnit is not null);
            SyntaxNode newTypeDeclarationOrCompilationUnit = typeDeclarationOrCompilationUnit;

            // We generate a new invocation node to call our new partial method, and use it to replace the nodeToFix.
            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Generate the modified type declaration depending on whether the callsite was a Regex constructor call
            // or a Regex static method invocation.
            SyntaxNode replacement = generator.InvocationExpression(generator.IdentifierName(methodName));
            ImmutableArray<IArgumentOperation> operationArguments;
            if (operation is IInvocationOperation invocationOperation) // When using a Regex static method
            {
                operationArguments = invocationOperation.Arguments;
                IEnumerable<SyntaxNode> arguments = operationArguments
                    .Where(arg => arg.Parameter?.Name is not (UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName or UpgradeToGeneratedRegexAnalyzer.PatternArgumentName))
                    .Select(arg => arg.Syntax);

                replacement = generator.InvocationExpression(generator.MemberAccessExpression(replacement, invocationOperation.TargetMethod.Name), arguments);
            }
            else
            {
                operationArguments = ((IObjectCreationOperation)operation).Arguments;
            }

            newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit.ReplaceNode(nodeToFix, WithTrivia(replacement, nodeToFix));

            // Initialize the inputs for the GeneratedRegex attribute.
            SyntaxNode? patternValue = GetNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            SyntaxNode? regexOptionsValue = GetNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            // Generate the new static partial method
            MethodDeclarationSyntax newMethod = (MethodDeclarationSyntax)generator.MethodDeclaration(
                name: methodName,
                returnType: generator.TypeExpression(regexSymbol),
                modifiers: DeclarationModifiers.Static | DeclarationModifiers.Partial,
                accessibility: Accessibility.Private);

            // Allow user to pick a different name for the method.
            newMethod = newMethod.ReplaceToken(newMethod.Identifier, SyntaxFactory.Identifier(methodName).WithAdditionalAnnotations(RenameAnnotation.Create()));

            // We now need to check if we have to pass in the cultureName parameter. This parameter will be required in case the option
            // RegexOptions.IgnoreCase is set for this Regex. To determine that, we first get the passed in options (if any), and then,
            // we also need to parse the pattern in case there are options that were specified inside the pattern via the `(?i)` switch.
            SyntaxNode? cultureNameValue = null;
            RegexOptions regexOptions = regexOptionsValue is not null ? GetRegexOptionsFromArgument(operationArguments) : RegexOptions.None;
            string pattern = GetRegexPatternFromArgument(operationArguments)!;

            try
            {
                regexOptions |= RegexParser.ParseOptionsInPattern(pattern, regexOptions);
            }
            catch (RegexParseException)
            {
                // We can't safely make the fix without knowing the options
                return document;
            }

            // If the options include IgnoreCase and don't specify CultureInvariant then we will have to calculate the user's current culture in order to pass
            // it in as a parameter. If the user specified IgnoreCase, but also selected CultureInvariant, then we skip as the default is to use Invariant culture.
            if ((regexOptions & RegexOptions.IgnoreCase) != 0 && (regexOptions & RegexOptions.CultureInvariant) == 0)
            {
#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers.
                // If CultureInvariant wasn't specified as options, we default to the current culture.
                cultureNameValue = generator.LiteralExpression(CultureInfo.CurrentCulture.Name);
#pragma warning restore RS1035

                // If options weren't passed in, then we need to define it as well in order to use the three parameter constructor.
                regexOptionsValue ??= generator.MemberAccessExpression(SyntaxFactory.IdentifierName("RegexOptions"), "None");
            }

            // Generate the GeneratedRegex attribute syntax node with the specified parameters.
            SyntaxNode attributes = generator.Attribute(generator.TypeExpression(generatedRegexAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, cultureNameValue) switch
            {
                ({ }, null, null) => [patternValue],
                ({ }, { }, null) => [patternValue, regexOptionsValue],
                ({ }, { }, { }) => [patternValue, regexOptionsValue, cultureNameValue],
                _ => Array.Empty<SyntaxNode>(),
            });

            // Add the attribute to the generated method.
            newMethod = (MethodDeclarationSyntax)generator.AddAttributes(newMethod, attributes);

            // Add the method to the type.
            newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit is TypeDeclarationSyntax newTypeDeclaration ?
                newTypeDeclaration.AddMembers(newMethod) :
                ((CompilationUnitSyntax)newTypeDeclarationOrCompilationUnit).AddMembers((ClassDeclarationSyntax)generator.ClassDeclaration("Program", modifiers: DeclarationModifiers.Partial, members: new[] { newMethod }));

            // Replace the old type declaration with the new modified one, and return the document.
            return document.WithSyntaxRoot(root.ReplaceNode(typeDeclarationOrCompilationUnit, newTypeDeclarationOrCompilationUnit));

            static IEnumerable<ISymbol> GetAllMembers(ITypeSymbol? symbol)
            {
                while (symbol != null)
                {
                    foreach (ISymbol member in symbol.GetMembers())
                    {
                        yield return member;
                    }

                    symbol = symbol.BaseType;
                }
            }

            static string? GetRegexPatternFromArgument(ImmutableArray<IArgumentOperation> arguments)
            {
                IArgumentOperation? patternArgument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
                if (patternArgument is null)
                {
                    return null;
                }

                return patternArgument.Value.ConstantValue.Value as string;
            }

            static RegexOptions GetRegexOptionsFromArgument(ImmutableArray<IArgumentOperation> arguments)
            {
                IArgumentOperation? optionsArgument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

                return optionsArgument is null || !optionsArgument.Value.ConstantValue.HasValue ?
                    RegexOptions.None :
                    (RegexOptions)(int)optionsArgument.Value.ConstantValue.Value!;
            }

            // Helper method that looks generates the node for pattern argument or options argument.
            static SyntaxNode? GetNode(ImmutableArray<IArgumentOperation> arguments, SyntaxGenerator generator, string parameterName)
            {
                IArgumentOperation? argument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == parameterName);
                if (argument is null)
                {
                    return null;
                }

                Debug.Assert(parameterName is UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName or UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
                if (parameterName == UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName)
                {
                    string optionsLiteral = Literal(((RegexOptions)(int)argument.Value.ConstantValue.Value!).ToString());
                    return SyntaxFactory.ParseExpression(optionsLiteral);
                }
                else if (argument.Value is ILiteralOperation literalOperation)
                {
                    return literalOperation.Syntax;
                }
                else if (argument.Value is IFieldReferenceOperation fieldReferenceOperation &&
                    fieldReferenceOperation.Member is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
                {
                    return generator.Argument(fieldReferenceOperation.Syntax);
                }
                else if (argument.Value.ConstantValue.Value is string str && str.Contains('\\'))
                {
                    return SyntaxFactory.ParseExpression($"@\"{str}\"");
                }
                else
                {
                    return generator.LiteralExpression(argument.Value.ConstantValue.Value);
                }
            }

            static string Literal(string stringifiedRegexOptions)
            {
                if (int.TryParse(stringifiedRegexOptions, NumberStyles.Integer, CultureInfo.InvariantCulture, out int options))
                {
                    // The options were formatted as an int, which means the runtime couldn't
                    // produce a textual representation.  So just output casting the value as an int.
                    return $"(RegexOptions)({options})";
                }

                // Parse the runtime-generated "Option1, Option2" into each piece and then concat
                // them back together.
                string[] parts = stringifiedRegexOptions.Split(s_comma, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = "RegexOptions." + parts[i].Trim();
                }
                return string.Join(" | ", parts);
            }

            static SyntaxNode WithTrivia(SyntaxNode method, SyntaxNode nodeToFix)
                => method.WithLeadingTrivia(nodeToFix.GetLeadingTrivia()).WithTrailingTrivia(nodeToFix.GetTrailingTrivia());
        }
    }
}
