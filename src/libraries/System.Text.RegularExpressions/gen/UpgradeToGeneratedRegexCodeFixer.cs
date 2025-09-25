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
        private const string DefaultRegexPropertyName = "MyRegex";

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

            // Check if the nodeToFix is within a field or property declaration
            FieldDeclarationSyntax? fieldDeclaration = nodeToFix.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            PropertyDeclarationSyntax? propertyDeclaration = nodeToFix.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

            if (fieldDeclaration is not null)
            {
                // For fields, offer to convert to partial property
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Convert to partial property with [GeneratedRegex]",
                        cancellationToken => ConvertFieldToPartialProperty(context.Document, root, nodeToFix, fieldDeclaration, cancellationToken),
                        equivalenceKey: "ConvertFieldToPartialProperty"),
                    context.Diagnostics);
            }
            else if (propertyDeclaration is not null)
            {
                // For properties with initializers, offer to convert to partial property
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Convert to partial property with [GeneratedRegex]",
                        cancellationToken => ConvertPropertyToPartialProperty(context.Document, root, nodeToFix, propertyDeclaration, cancellationToken),
                        equivalenceKey: "ConvertPropertyToPartialProperty"),
                    context.Diagnostics);
            }
            else
            {
                // For other cases (method calls, etc.), offer both method and property options
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Generate partial method with [GeneratedRegex]",
                        cancellationToken => ConvertToSourceGenerator(context.Document, root, nodeToFix, cancellationToken, generateMethod: true),
                        equivalenceKey: "ConvertToMethod"),
                    context.Diagnostics);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Generate partial property with [GeneratedRegex]",
                        cancellationToken => ConvertToSourceGenerator(context.Document, root, nodeToFix, cancellationToken, generateMethod: false),
                        equivalenceKey: "ConvertToProperty"),
                    context.Diagnostics);
            }
        }

        /// <summary>
        /// Takes a <see cref="Document"/> and a <see cref="Diagnostic"/> and returns a new <see cref="Document"/> with the replaced
        /// nodes in order to apply the code fix to the diagnostic.
        /// </summary>
        /// <param name="document">The original document.</param>
        /// <param name="root">The root of the syntax tree.</param>
        /// <param name="nodeToFix">The node to fix. This is where the diagnostic was produced.</param>
        /// <param name="cancellationToken">The cancellation token for the async operation.</param>
        /// <param name="generateMethod">True to generate a method, false to generate a property.</param>
        /// <returns>The new document with the replaced nodes after applying the code fix.</returns>
        private static async Task<Document> ConvertToSourceGenerator(Document document, SyntaxNode root, SyntaxNode nodeToFix, CancellationToken cancellationToken, bool generateMethod = true)
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

            // Calculate what name should be used for the generated static partial method or property
            string memberName = generateMethod ? DefaultRegexMethodName : DefaultRegexPropertyName;

            INamedTypeSymbol? typeSymbol = typeDeclarationOrCompilationUnit is TypeDeclarationSyntax typeDeclaration ?
                semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) :
                semanticModel.GetDeclaredSymbol((CompilationUnitSyntax)typeDeclarationOrCompilationUnit, cancellationToken)?.ContainingType;

            if (typeSymbol is not null)
            {
                IEnumerable<ISymbol> members = GetAllMembers(typeSymbol);
                int memberCount = 1;
                string baseName = generateMethod ? DefaultRegexMethodName : DefaultRegexPropertyName;
                while (members.Any(m => m.Name == memberName))
                {
                    memberName = $"{baseName}{memberCount++}";
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
            SyntaxNode replacement = generateMethod ?
                generator.InvocationExpression(generator.IdentifierName(memberName)) :
                generator.IdentifierName(memberName);
            ImmutableArray<IArgumentOperation> operationArguments;
            if (operation is IInvocationOperation invocationOperation) // When using a Regex static method
            {
                operationArguments = invocationOperation.Arguments;
                IEnumerable<SyntaxNode> arguments = operationArguments
                    .Where(arg => arg.Parameter?.Name is not (UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName or UpgradeToGeneratedRegexAnalyzer.PatternArgumentName))
                    .Select(arg => arg.Syntax);

                if (generateMethod)
                {
                    replacement = generator.InvocationExpression(generator.MemberAccessExpression(replacement, invocationOperation.TargetMethod.Name), arguments);
                }
                else
                {
                    replacement = generator.InvocationExpression(generator.MemberAccessExpression(replacement, invocationOperation.TargetMethod.Name), arguments);
                }
            }
            else
            {
                operationArguments = ((IObjectCreationOperation)operation).Arguments;
            }

            newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit.ReplaceNode(nodeToFix, WithTrivia(replacement, nodeToFix));

            // Initialize the inputs for the GeneratedRegex attribute.
            SyntaxNode? patternValue = GetNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            SyntaxNode? regexOptionsValue = GetNode(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            // Generate the new static partial method or property
            SyntaxNode newMember;
            if (generateMethod)
            {
                MethodDeclarationSyntax newMethod = (MethodDeclarationSyntax)generator.MethodDeclaration(
                    name: memberName,
                    returnType: generator.TypeExpression(regexSymbol),
                    modifiers: DeclarationModifiers.Static | DeclarationModifiers.Partial,
                    accessibility: Accessibility.Private);

                // Allow user to pick a different name for the method.
                newMember = newMethod.ReplaceToken(newMethod.Identifier, SyntaxFactory.Identifier(memberName).WithAdditionalAnnotations(RenameAnnotation.Create()));
            }
            else
            {
                // Create the partial property declaration manually since SyntaxGenerator doesn't support partial properties correctly
                List<SyntaxToken> modifiers = new List<SyntaxToken>
                {
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                };

                // Create accessor list with just a getter
                AccessorListSyntax accessorList = SyntaxFactory.AccessorList(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));

                // Create the property declaration
                PropertyDeclarationSyntax newProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.IdentifierName("Regex"),
                    SyntaxFactory.Identifier(memberName).WithAdditionalAnnotations(RenameAnnotation.Create()))
                    .WithModifiers(SyntaxFactory.TokenList(modifiers))
                    .WithAccessorList(accessorList);

                newMember = newProperty;
            }

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

            // Add the attribute to the generated member.
            newMember = generator.AddAttributes(newMember, attributes);

            // Add the member to the type.
            newTypeDeclarationOrCompilationUnit = newTypeDeclarationOrCompilationUnit is TypeDeclarationSyntax newTypeDeclaration ?
                newTypeDeclaration.AddMembers((MemberDeclarationSyntax)newMember) :
                ((CompilationUnitSyntax)newTypeDeclarationOrCompilationUnit).AddMembers((ClassDeclarationSyntax)generator.ClassDeclaration("Program", modifiers: DeclarationModifiers.Partial, members: new[] { newMember }));

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

                // Literals and class-level field references should be preserved as-is.
                if (argument.Value is ILiteralOperation ||
                    argument.Value is IFieldReferenceOperation { Member: IFieldSymbol { IsConst: true } })
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
                        else
                        {
                            // Default handling for all other patterns.
                            return generator.LiteralExpression(argument.Value.ConstantValue.Value);
                        }

                    default:
                        Debug.Fail($"Unknown parameter: {parameterName}");
                        return argument.Syntax;
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

        /// <summary>
        /// Converts a field declaration containing a Regex object creation to a partial property.
        /// </summary>
        private static async Task<Document> ConvertFieldToPartialProperty(Document document, SyntaxNode root, SyntaxNode nodeToFix, FieldDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
        {
            // Get the variable declarator (the part with the field name and initializer)
            VariableDeclaratorSyntax? variableDeclarator = fieldDeclaration.Declaration.Variables.FirstOrDefault();
            if (variableDeclarator is null || variableDeclarator.Initializer is null)
            {
                return document;
            }

            // Get the field name
            string fieldName = variableDeclarator.Identifier.ValueText;

            // Get the semantic model and symbols
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                return document;
            }

            Compilation compilation = semanticModel.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            INamedTypeSymbol? generatedRegexAttributeSymbol = compilation.GetTypeByMetadataName(GeneratedRegexTypeName);
            if (regexSymbol is null || generatedRegexAttributeSymbol is null)
            {
                return document;
            }

            // Get the operation for the initializer
            IOperation? operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
            if (operation is null)
            {
                return document;
            }

            // Extract arguments from the operation
            ImmutableArray<IArgumentOperation> operationArguments = operation is IObjectCreationOperation objectCreation
                ? objectCreation.Arguments
                : ((IInvocationOperation)operation).Arguments;

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Get the arguments for the GeneratedRegex attribute (reusing the existing helper methods)
            SyntaxNode? patternValue = GetNodeFromOperation(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            SyntaxNode? regexOptionsValue = GetNodeFromOperation(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            // Handle culture name using the same logic as the original method
            SyntaxNode? cultureNameValue = null;
            RegexOptions regexOptions = regexOptionsValue is not null ? GetRegexOptionsFromArgumentLocal(operationArguments) : RegexOptions.None;
            string? pattern = GetRegexPatternFromArgumentLocal(operationArguments);

            if (pattern is not null)
            {
                try
                {
                    regexOptions |= RegexParser.ParseOptionsInPattern(pattern, regexOptions);
                }
                catch (RegexParseException)
                {
                    // Pattern parsing failed, skip culture handling
                }

                // If the options include IgnoreCase and don't specify CultureInvariant then we will have to calculate the user's current culture
                if ((regexOptions & RegexOptions.IgnoreCase) != 0 && (regexOptions & RegexOptions.CultureInvariant) == 0)
                {
#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers.
                    // If CultureInvariant wasn't specified as options, we default to the current culture.
                    cultureNameValue = generator.LiteralExpression(CultureInfo.CurrentCulture.Name);
#pragma warning restore RS1035

                    // If options weren't passed in, then we need to define it as well in order to use the three parameter constructor.
                    regexOptionsValue ??= generator.MemberAccessExpression(SyntaxFactory.IdentifierName("RegexOptions"), "None");
                }
            }

            // Generate the GeneratedRegex attribute
            SyntaxNode attributes = generator.Attribute(generator.TypeExpression(generatedRegexAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, cultureNameValue) switch
            {
                ({ }, null, null) => [patternValue],
                ({ }, { }, null) => [patternValue, regexOptionsValue],
                ({ }, { }, { }) => [patternValue, regexOptionsValue, cultureNameValue],
                _ => Array.Empty<SyntaxNode>(),
            });

            // Create the partial property declaration manually since SyntaxGenerator doesn't support partial properties correctly
            List<SyntaxToken> modifiers = new List<SyntaxToken>();

            // Add accessibility modifiers
            foreach (SyntaxToken modifier in fieldDeclaration.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.PrivateKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                        modifiers.Add(modifier);
                        break;
                    case SyntaxKind.StaticKeyword:
                        modifiers.Add(modifier);
                        break;
                    // Skip readonly and const as they don't apply to properties
                }
            }

            // Add partial modifier
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            // Create accessor list with just a getter
            AccessorListSyntax accessorList = SyntaxFactory.AccessorList(
                SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))));

            // Create the property declaration
            PropertyDeclarationSyntax partialProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.IdentifierName("Regex"),
                SyntaxFactory.Identifier(fieldName).WithAdditionalAnnotations(RenameAnnotation.Create()))
                .WithModifiers(SyntaxFactory.TokenList(modifiers))
                .WithAccessorList(accessorList);

            // Add the GeneratedRegex attribute to the property
            partialProperty = (PropertyDeclarationSyntax)generator.AddAttributes(partialProperty, attributes);

            // Find the containing type and ensure it's partial
            TypeDeclarationSyntax? containingType = fieldDeclaration.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (containingType is null)
            {
                return document;
            }

            // Make the containing type partial if it isn't already
            SyntaxNode updatedRoot = root;
            if (!containingType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                TypeDeclarationSyntax partialType = containingType.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                updatedRoot = root.ReplaceNode(containingType, partialType);
                // Find the field declaration in the updated tree
                fieldDeclaration = updatedRoot.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText == fieldName))
                    ?? fieldDeclaration;
            }

            // Replace the field with the partial property
            updatedRoot = updatedRoot.ReplaceNode(fieldDeclaration, partialProperty);

            return document.WithSyntaxRoot(updatedRoot);
        }

        /// <summary>
        /// Converts a property declaration with an initializer containing a Regex object creation to a partial property.
        /// </summary>
        private static async Task<Document> ConvertPropertyToPartialProperty(Document document, SyntaxNode root, SyntaxNode nodeToFix, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
        {
            // Get the property name
            string propertyName = propertyDeclaration.Identifier.ValueText;

            // Get the semantic model and symbols
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                return document;
            }

            Compilation compilation = semanticModel.Compilation;
            INamedTypeSymbol? regexSymbol = compilation.GetTypeByMetadataName(RegexTypeName);
            INamedTypeSymbol? generatedRegexAttributeSymbol = compilation.GetTypeByMetadataName(GeneratedRegexTypeName);
            if (regexSymbol is null || generatedRegexAttributeSymbol is null)
            {
                return document;
            }

            // Get the operation for the initializer
            IOperation? operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
            if (operation is null)
            {
                return document;
            }

            // Extract arguments from the operation
            ImmutableArray<IArgumentOperation> operationArguments = operation is IObjectCreationOperation objectCreation
                ? objectCreation.Arguments
                : ((IInvocationOperation)operation).Arguments;

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator generator = editor.Generator;

            // Get the arguments for the GeneratedRegex attribute
            SyntaxNode? patternValue = GetNodeFromOperation(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            SyntaxNode? regexOptionsValue = GetNodeFromOperation(operationArguments, generator, UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            // Handle culture name using the same logic as the original method
            SyntaxNode? cultureNameValue = null;
            RegexOptions regexOptions = regexOptionsValue is not null ? GetRegexOptionsFromArgumentLocal(operationArguments) : RegexOptions.None;
            string? pattern = GetRegexPatternFromArgumentLocal(operationArguments);

            if (pattern is not null)
            {
                try
                {
                    regexOptions |= RegexParser.ParseOptionsInPattern(pattern, regexOptions);
                }
                catch (RegexParseException)
                {
                    // Pattern parsing failed, skip culture handling
                }

                // If the options include IgnoreCase and don't specify CultureInvariant then we will have to calculate the user's current culture
                if ((regexOptions & RegexOptions.IgnoreCase) != 0 && (regexOptions & RegexOptions.CultureInvariant) == 0)
                {
#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers.
                    // If CultureInvariant wasn't specified as options, we default to the current culture.
                    cultureNameValue = generator.LiteralExpression(CultureInfo.CurrentCulture.Name);
#pragma warning restore RS1035

                    // If options weren't passed in, then we need to define it as well in order to use the three parameter constructor.
                    regexOptionsValue ??= generator.MemberAccessExpression(SyntaxFactory.IdentifierName("RegexOptions"), "None");
                }
            }

            // Generate the GeneratedRegex attribute
            SyntaxNode attributes = generator.Attribute(generator.TypeExpression(generatedRegexAttributeSymbol), attributeArguments: (patternValue, regexOptionsValue, cultureNameValue) switch
            {
                ({ }, null, null) => [patternValue],
                ({ }, { }, null) => [patternValue, regexOptionsValue],
                ({ }, { }, { }) => [patternValue, regexOptionsValue, cultureNameValue],
                _ => Array.Empty<SyntaxNode>(),
            });

            // Create the partial property declaration (removing initializer and adding partial modifier)
            PropertyDeclarationSyntax partialProperty = propertyDeclaration
                .WithInitializer(null)
                .WithSemicolonToken(default)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            // Add the GeneratedRegex attribute to the property
            partialProperty = (PropertyDeclarationSyntax)generator.AddAttributes(partialProperty, attributes);

            // Find the containing type and ensure it's partial
            TypeDeclarationSyntax? containingType = propertyDeclaration.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (containingType is null)
            {
                return document;
            }

            // Make the containing type partial if it isn't already
            SyntaxNode updatedRoot = root;
            if (!containingType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                TypeDeclarationSyntax partialType = containingType.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                updatedRoot = root.ReplaceNode(containingType, partialType);
                // Find the property declaration in the updated tree
                propertyDeclaration = updatedRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.ValueText == propertyName)
                    ?? propertyDeclaration;
            }

            // Replace the property with the partial property
            updatedRoot = updatedRoot.ReplaceNode(propertyDeclaration, partialProperty);

            return document.WithSyntaxRoot(updatedRoot);
        }

        /// <summary>
        /// Extracts modifiers from field modifiers, converting them to property-appropriate modifiers.
        /// </summary>
        private static DeclarationModifiers ExtractModifiersFromField(SyntaxTokenList fieldModifiers)
        {
            DeclarationModifiers modifiers = DeclarationModifiers.None;

            foreach (SyntaxToken modifier in fieldModifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.StaticKeyword:
                        modifiers |= DeclarationModifiers.Static;
                        break;
                    case SyntaxKind.ReadOnlyKeyword:
                        // readonly fields become get-only properties
                        break;
                    case SyntaxKind.ConstKeyword:
                        // const fields cannot be converted to properties in this context
                        break;
                }
            }

            return modifiers;
        }

        /// <summary>
        /// Gets the accessibility level from field modifiers.
        /// </summary>
        private static Accessibility GetAccessibilityFromModifiers(SyntaxTokenList modifiers)
        {
            foreach (SyntaxToken modifier in modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                        return Accessibility.Public;
                    case SyntaxKind.PrivateKeyword:
                        return Accessibility.Private;
                    case SyntaxKind.ProtectedKeyword:
                        return Accessibility.Protected;
                    case SyntaxKind.InternalKeyword:
                        return Accessibility.Internal;
                }
            }
            return Accessibility.Private; // Default accessibility for fields
        }

        /// <summary>
        /// Helper method to get the node for pattern or options argument from operation arguments.
        /// </summary>
        private static SyntaxNode? GetNodeFromOperation(ImmutableArray<IArgumentOperation> arguments, SyntaxGenerator generator, string parameterName)
        {
            IArgumentOperation? argument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == parameterName);
            if (argument is null)
            {
                return null;
            }

            // Literals and class-level field references should be preserved as-is.
            if (argument.Value is ILiteralOperation ||
                argument.Value is IFieldReferenceOperation { Member: IFieldSymbol { IsConst: true } })
            {
                return argument.Value.Syntax;
            }

            switch (parameterName)
            {
                case UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName:
                    string optionsLiteral = LiteralLocal(((RegexOptions)(int)argument.Value.ConstantValue.Value!).ToString());
                    return SyntaxFactory.ParseExpression(optionsLiteral);

                case UpgradeToGeneratedRegexAnalyzer.PatternArgumentName:
                    if (argument.Value.ConstantValue.Value is string str && str.Contains('\\'))
                    {
                        // Special handling for string patterns with escaped characters
                        string escapedVerbatimText = str.Replace("\"", "\"\"");
                        return SyntaxFactory.ParseExpression($"@\"{escapedVerbatimText}\"");
                    }
                    else
                    {
                        // Default handling for all other patterns.
                        return generator.LiteralExpression(argument.Value.ConstantValue.Value);
                    }

                default:
                    return argument.Syntax;
            }
        }

        private static string? GetRegexPatternFromArgumentLocal(ImmutableArray<IArgumentOperation> arguments)
        {
            IArgumentOperation? patternArgument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.PatternArgumentName);
            if (patternArgument is null)
            {
                return null;
            }

            return patternArgument.Value.ConstantValue.Value as string;
        }

        private static RegexOptions GetRegexOptionsFromArgumentLocal(ImmutableArray<IArgumentOperation> arguments)
        {
            IArgumentOperation? optionsArgument = arguments.SingleOrDefault(arg => arg.Parameter?.Name == UpgradeToGeneratedRegexAnalyzer.OptionsArgumentName);

            return optionsArgument is null || !optionsArgument.Value.ConstantValue.HasValue ?
                RegexOptions.None :
                (RegexOptions)(int)optionsArgument.Value.ConstantValue.Value!;
        }

        private static string LiteralLocal(string stringifiedRegexOptions)
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
    }
}
