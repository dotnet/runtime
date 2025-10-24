// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers, but this is a fixer.

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
        private const string DefaultRegexPropertyName = "MyRegex";

        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.UseRegexSourceGeneration.Id];

        private static readonly char[] s_comma = [','];

        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Fetch the node to fix, and register the codefix by invoking the ConvertToSourceGenerator method.
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not SyntaxNode root ||
                root.FindNode(context.Span, getInnermostNodeForTie: true) is not SyntaxNode nodeToFix)
            {
                return;
            }

            if (nodeToFix.Ancestors().FirstOrDefault(a => a is FieldDeclarationSyntax) is FieldDeclarationSyntax fieldDeclaration)
            {
                // For fields, offer to convert to partial property
                context.RegisterCodeFix(
                    CodeAction.Create(
                        SR.UseRegexSourceGeneratorTitle,
                        cancellationToken => ConvertFieldToGeneratedRegexProperty(context.Document, root, nodeToFix, fieldDeclaration, cancellationToken),
                        equivalenceKey: nameof(ConvertFieldToGeneratedRegexProperty)),
                    context.Diagnostics);
            }
            else if (nodeToFix.Ancestors().FirstOrDefault(a => a is PropertyDeclarationSyntax) is PropertyDeclarationSyntax propertyDeclaration)
            {
                // For properties with initializers, offer to convert to partial property
                context.RegisterCodeFix(
                    CodeAction.Create(
                        SR.UseRegexSourceGeneratorTitle,
                        cancellationToken => ConvertPropertyToGeneratedRegexProperty(context.Document, root, nodeToFix, propertyDeclaration, cancellationToken),
                        equivalenceKey: nameof(ConvertPropertyToGeneratedRegexProperty)),
                    context.Diagnostics);
            }
            else
            {
                // For other cases (method calls, etc.), offer to generate a property
                context.RegisterCodeFix(
                    CodeAction.Create(
                        SR.UseRegexSourceGeneratorTitle,
                        cancellationToken => CreateGeneratedRegexProperty(context.Document, root, nodeToFix, cancellationToken),
                        equivalenceKey: nameof(CreateGeneratedRegexProperty)),
                    context.Diagnostics);
            }
        }

        private static async Task<Document> CreateGeneratedRegexProperty(
            Document document, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not SemanticModel semanticModel ||
                semanticModel.Compilation is not Compilation compilation ||
                compilation.GetTypeByMetadataName(RegexTypeName) is not INamedTypeSymbol regexSymbol ||
                compilation.GetTypeByMetadataName(GeneratedRegexTypeName) is not INamedTypeSymbol generatedRegexAttributeSymbol ||
                semanticModel.GetOperation(nodeToFix, cancellationToken) is not IOperation operation ||
                operation is not (IInvocationOperation or IObjectCreationOperation))
            {
                return document;
            }

            // Get the parent type declaration so that we can inspect its methods as well as check if we need to add the partial keyword.
            SyntaxNode? typeDeclarationOrCompilationUnit =
                nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ??
                await nodeToFix.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Calculate what name should be used for the generated static partial property.
            string memberName = DefaultRegexPropertyName;
            INamedTypeSymbol? typeSymbol = typeDeclarationOrCompilationUnit is TypeDeclarationSyntax typeDeclaration ?
                semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) :
                semanticModel.GetDeclaredSymbol((CompilationUnitSyntax)typeDeclarationOrCompilationUnit, cancellationToken)?.ContainingType;
            if (typeSymbol is not null)
            {
                int memberCount = 1;
                while (GetAllMembers(typeSymbol).Any(m => m.Name == memberName))
                {
                    memberName = $"{DefaultRegexPropertyName}{memberCount++}";
                }
            }

            // Add partial to all ancestors.
            nodeToFix = TryPartialize(nodeToFix, ref typeDeclarationOrCompilationUnit, ref root)!;
            if (nodeToFix is null)
            {
                return document;
            }

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Generate the modified type declaration depending on whether the call site was a Regex constructor call
            // or a Regex static method invocation.
            SyntaxNode replacement = generator.IdentifierName(memberName);
            ImmutableArray<IArgumentOperation> operationArguments;
            if (operation is IInvocationOperation invocationOperation) // when using a Regex static method
            {
                operationArguments = invocationOperation.Arguments;
                replacement = generator.InvocationExpression(generator.MemberAccessExpression(replacement, invocationOperation.TargetMethod.Name),
                    from arg in operationArguments
                    where arg.Parameter?.Name is not (UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName or UpgradeToGeneratedRegexAnalyzer.PatternArgumentName)
                    select arg.Syntax);
            }
            else
            {
                operationArguments = ((IObjectCreationOperation)operation).Arguments;
            }

            // Replace the Regex ctor or static method call with the new replacement expression.
            SyntaxNode newTypeDeclarationOrCompilationUnit = typeDeclarationOrCompilationUnit.ReplaceNode(nodeToFix, WithTrivia(replacement, nodeToFix));

            // Generate the new static partial property.
            SyntaxNode newMember = SyntaxFactory.PropertyDeclaration(
                (TypeSyntax)generator.TypeExpression(regexSymbol),
                SyntaxFactory.Identifier(memberName))
                    .WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                    .WithAccessorList(SyntaxFactory.AccessorList(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
                    .WithAdditionalAnnotations(RenameAnnotation.Create());

            return TryAddNewMember(
                generator, document, root, generatedRegexAttributeSymbol,
                operationArguments, null, newMember,
                typeDeclarationOrCompilationUnit, newTypeDeclarationOrCompilationUnit);
        }

        private static async Task<Document> ConvertFieldToGeneratedRegexProperty(Document document, SyntaxNode root, SyntaxNode nodeToFix, FieldDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
        {
            if (fieldDeclaration.Declaration.Variables.FirstOrDefault() is not VariableDeclaratorSyntax variableDeclarator ||
                variableDeclarator.Initializer is null ||
                await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not SemanticModel semanticModel ||
                semanticModel.Compilation is not Compilation compilation ||
                compilation.GetTypeByMetadataName(RegexTypeName) is not INamedTypeSymbol regexSymbol ||
                compilation.GetTypeByMetadataName(GeneratedRegexTypeName) is not INamedTypeSymbol generatedRegexAttributeSymbol ||
                semanticModel.GetOperation(nodeToFix, cancellationToken) is not IOperation operation ||
                operation is not (IInvocationOperation or IObjectCreationOperation))
            {
                return document;
            }

            // Add partial to all ancestors.
            nodeToFix = TryPartialize(nodeToFix, ref fieldDeclaration, ref root)!;
            if (nodeToFix is null)
            {
                return document;
            }

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Generate the new static partial property.
            SyntaxNode newMember = SyntaxFactory.PropertyDeclaration(
                    (TypeSyntax)generator.TypeExpression(regexSymbol), SyntaxFactory.Identifier(variableDeclarator.Identifier.ValueText).WithAdditionalAnnotations(RenameAnnotation.Create()))
                .WithModifiers(SyntaxFactory.TokenList([
                        .. from modifier in fieldDeclaration.Modifiers
                           where modifier.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.PrivateKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword or SyntaxKind.StaticKeyword
                           select modifier,
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                    ]))
                .WithAccessorList(SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))));

            var typeDeclarationOrCompilationUnit = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ?? root;

            ImmutableArray<IArgumentOperation> operationArguments =
                operation is IObjectCreationOperation objectCreation ?
                    objectCreation.Arguments :
                    ((IInvocationOperation)operation).Arguments;

            return TryAddNewMember(
                generator, document, root, generatedRegexAttributeSymbol,
                operationArguments, fieldDeclaration, newMember,
                typeDeclarationOrCompilationUnit, typeDeclarationOrCompilationUnit);
        }

        private static async Task<Document> ConvertPropertyToGeneratedRegexProperty(
            Document document, SyntaxNode root, SyntaxNode nodeToFix, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not SemanticModel semanticModel ||
                semanticModel.Compilation is not Compilation compilation ||
                compilation.GetTypeByMetadataName(RegexTypeName) is not INamedTypeSymbol regexSymbol ||
                compilation.GetTypeByMetadataName(GeneratedRegexTypeName) is not INamedTypeSymbol generatedRegexAttributeSymbol ||
                semanticModel.GetOperation(nodeToFix, cancellationToken) is not IOperation operation ||
                operation is not (IInvocationOperation or IObjectCreationOperation))
            {
                return document;
            }

            // Add partial to all ancestors.
            nodeToFix = TryPartialize(nodeToFix, ref propertyDeclaration, ref root)!;
            if (nodeToFix is null)
            {
                return document;
            }

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Generate the new static partial property.
            SyntaxNode newMember = SyntaxFactory.PropertyDeclaration(
                (TypeSyntax)generator.TypeExpression(regexSymbol), SyntaxFactory.Identifier(propertyDeclaration.Identifier.ValueText))
                .WithModifiers(SyntaxFactory.TokenList([.. propertyDeclaration.Modifiers, SyntaxFactory.Token(SyntaxKind.PartialKeyword)]))
                .WithAccessorList(SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))));

            var typeDeclarationOrCompilationUnit = nodeToFix.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() ?? root;

            ImmutableArray<IArgumentOperation> operationArguments =
                operation is IObjectCreationOperation objectCreation ?
                    objectCreation.Arguments :
                    ((IInvocationOperation)operation).Arguments;

            return TryAddNewMember(
                generator, document, root, generatedRegexAttributeSymbol,
                operationArguments, propertyDeclaration, newMember,
                typeDeclarationOrCompilationUnit, typeDeclarationOrCompilationUnit);
        }

        private static Document TryAddNewMember(
            SyntaxGenerator generator, Document document,
            SyntaxNode root, INamedTypeSymbol generatedRegexSymbol,
            ImmutableArray<IArgumentOperation> operationArguments,
            SyntaxNode? oldMember, SyntaxNode newMember,
            SyntaxNode oldTypeDeclarationOrCompilationUnit, SyntaxNode newTypeDeclarationOrCompilationUnit)
        {
            // Initialize the inputs for the GeneratedRegex attribute.
            SyntaxNode? patternValue = GeneratePatternOrOptionsArgumentNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            SyntaxNode? regexOptionsValue = GeneratePatternOrOptionsArgumentNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            // Handle culture parameter for IgnoreCase scenarios
            RegexOptions regexOptions = regexOptionsValue is not null ? GetRegexOptionsFromArgument(operationArguments) : RegexOptions.None;
            string pattern = GetRegexPatternFromArgument(operationArguments)!;

            // We now need to check if we have to pass in the cultureName parameter. This parameter will be required in case the option
            // RegexOptions.IgnoreCase is set for this Regex. To determine that, we first get the passed in options (if any), and then,
            // we also need to parse the pattern in case there are options that were specified inside the pattern via the `(?i)` switch.
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
            SyntaxNode? cultureNameValue = null;
            if ((regexOptions & RegexOptions.IgnoreCase) != 0 && (regexOptions & RegexOptions.CultureInvariant) == 0)
            {
                // If CultureInvariant wasn't specified as options, we default to the current culture.
                cultureNameValue = generator.LiteralExpression(CultureInfo.CurrentCulture.Name);

                // If options weren't passed in, then we need to define it as well in order to use the three parameter constructor.
                regexOptionsValue ??= generator.MemberAccessExpression(SyntaxFactory.IdentifierName("RegexOptions"), "None");
            }

            // Add the attribute to the generated member.
            newMember = generator.AddAttributes(newMember, generator.Attribute(
                generator.TypeExpression(generatedRegexSymbol),
                attributeArguments: (patternValue, regexOptionsValue, cultureNameValue)
                switch
                {
                    ({ }, null, null) => [patternValue],
                    ({ }, { }, null) => [patternValue, regexOptionsValue],
                    ({ }, { }, { }) => [patternValue, regexOptionsValue, cultureNameValue],
                    _ => Array.Empty<SyntaxNode>(),
                }));

            // Add the member to the type.
            if (oldMember is null)
            {
                newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit is TypeDeclarationSyntax newTypeDeclaration ?
                    newTypeDeclaration.AddMembers((MemberDeclarationSyntax)newMember) :
                    ((CompilationUnitSyntax)newTypeDeclarationOrCompilationUnit).AddMembers((ClassDeclarationSyntax)generator.ClassDeclaration("Program", modifiers: DeclarationModifiers.Partial, members: new[] { newMember }));
            }
            else
            {
                newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit.ReplaceNode(oldMember, newMember);
            }

            // Replace the old type declaration with the new modified one, and return the document.
            return document.WithSyntaxRoot(root.ReplaceNode(oldTypeDeclarationOrCompilationUnit, newTypeDeclarationOrCompilationUnit));
        }

        /// <summary>Walk the type hierarchy of the node to fix, and add the partial modifier to each ancestor (if it doesn't have it already).</summary>
        private static SyntaxNode? TryPartialize<TParent>(SyntaxNode nodeToFix, [NotNullIfNotNull(nameof(parent))] ref TParent? parent, ref SyntaxNode root) where TParent : SyntaxNode
        {
            var trackedRoot = root.TrackNodes(parent is null ? [nodeToFix] : [nodeToFix, parent]);

            root = trackedRoot.ReplaceNodes(
                trackedRoot.GetCurrentNode(nodeToFix)!.Ancestors().OfType<TypeDeclarationSyntax>(),
                (_, typeDeclaration) =>
                    typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) ?
                        typeDeclaration :
                        typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

            if (parent is not null)
            {
                parent = root.GetCurrentNode(parent);
            }

            return root.GetCurrentNode(nodeToFix);
        }

        private static string? GetRegexPatternFromArgument(ImmutableArray<IArgumentOperation> arguments) =>
            arguments
                .SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.PatternArgumentName)
                ?.Value.ConstantValue.Value as string;

        private static RegexOptions GetRegexOptionsFromArgument(ImmutableArray<IArgumentOperation> arguments)
        {
            IArgumentOperation? optionsArgument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            return optionsArgument is null || !optionsArgument.Value.ConstantValue.HasValue ?
                RegexOptions.None :
                (RegexOptions)(int)optionsArgument.Value.ConstantValue.Value!;
        }

        /// <summary>
        /// Helper method that generates the node for pattern argument or options argument.
        /// </summary>
        private static SyntaxNode? GeneratePatternOrOptionsArgumentNode(ImmutableArray<IArgumentOperation> arguments, SyntaxGenerator generator, string parameterName)
        {
            if (arguments.SingleOrDefault(arg => arg.Parameter?.Name == parameterName) is IArgumentOperation argument)
            {
                // Literals and class-level field references should be preserved as-is.
                if (argument.Value is ILiteralOperation or IFieldReferenceOperation { Member: IFieldSymbol { IsConst: true } })
                {
                    return argument.Value.Syntax;
                }

                switch (parameterName)
                {
                    case UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName:
                        string optionsLiteral = Literal(((RegexOptions)(int)argument.Value.ConstantValue.Value!).ToString());
                        return SyntaxFactory.ParseExpression(optionsLiteral);

                    case UpgradeToGeneratedRegexAnalyzer.PatternArgumentName:
                        if (argument.Value.ConstantValue.Value is string str && str.Contains('\\'))
                        {
                            // Special handling for string patterns with escaped characters
                            string escapedVerbatimText = str.Replace("\"", "\"\"");
                            return SyntaxFactory.ParseExpression($"@\"{escapedVerbatimText}\"");
                        }

                        // Default handling for all other patterns.
                        return generator.LiteralExpression(argument.Value.ConstantValue.Value);

                    default:
                        Debug.Fail($"Unknown parameter: {parameterName}");
                        return argument.Syntax;
                }
            }

            return null;
        }

        private static string Literal(string stringifiedRegexOptions)
        {
            if (int.TryParse(stringifiedRegexOptions, NumberStyles.Integer, CultureInfo.InvariantCulture, out int options))
            {
                // The options were formatted as an int, which means the runtime couldn't
                // produce a textual representation.  So just output casting the value as an int.
                return $"({nameof(RegexOptions)})({options})";
            }

            // Parse the runtime-generated "Option1, Option2" into each piece and then concat
            // them back together.
            return string.Join(" | ",
                from part in stringifiedRegexOptions.Split(s_comma, StringSplitOptions.RemoveEmptyEntries)
                select $"{nameof(RegexOptions)}.{part.Trim()}");
        }

        /// <summary>
        /// Helper method to preserve trivia (whitespace/formatting) when replacing nodes.
        /// </summary>
        private static SyntaxNode WithTrivia(SyntaxNode newNode, SyntaxNode originalNode) =>
            newNode.WithLeadingTrivia(originalNode.GetLeadingTrivia()).WithTrailingTrivia(originalNode.GetTrailingTrivia());

        /// <summary>
        /// Helper method to get all members from a type including inherited members.
        /// </summary>
        private static IEnumerable<ISymbol> GetAllMembers(INamedTypeSymbol typeSymbol)
        {
            foreach (ISymbol member in typeSymbol.GetMembers())
            {
                yield return member;
            }

            if (typeSymbol.BaseType is not null)
            {
                foreach (ISymbol member in GetAllMembers(typeSymbol.BaseType))
                {
                    yield return member;
                }
            }
        }
    }
}
