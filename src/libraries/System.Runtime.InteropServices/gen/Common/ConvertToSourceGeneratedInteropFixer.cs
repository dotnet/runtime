// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Interop.Analyzers
{
    public abstract class ConvertToSourceGeneratedInteropFixer : CodeFixProvider
    {
        public sealed override FixAllProvider GetFixAllProvider() => CustomFixAllProvider.Instance;

        protected abstract string BaseEquivalenceKey { get; }

        protected abstract IEnumerable<ConvertToSourceGeneratedInteropDocumentCodeAction> CreateAllCodeFixesForOptions(Document document, SyntaxNode node, ImmutableDictionary<string, Option> options);

        protected abstract ConvertToSourceGeneratedInteropDocumentCodeAction CreateFixForSelectedOptions(SyntaxNode node, Document document, ImmutableDictionary<string, Option> selectedOptions);

        protected abstract string GetDiagnosticTitle(ImmutableDictionary<string, Option> selectedOptions);

        /// <summary>
        /// A basic string-serializable option mechanism to enable the same fixer to be used for diagnostics with slightly different properties.
        /// </summary>
        public abstract record Option
        {
            private protected Option()
            {
            }

            public const string AllowUnsafe = nameof(AllowUnsafe);
            public const string MayRequireAdditionalWork = nameof(MayRequireAdditionalWork);

            public sealed record Bool(bool Value) : Option
            {
                public override string ToString() => $"b:{Value};";
            }

            public sealed record String(string Value) : Option
            {
                public override string ToString() => $"s:{Value};";
            }

            public static ImmutableDictionary<string, Option> ParseOptionsFromEquivalenceKey(string equivalenceKey)
            {
                ImmutableDictionary<string, Option>.Builder options = ImmutableDictionary.CreateBuilder<string, Option>();
                // The first ';' separates the base equivalence key from the options
                foreach (string option in equivalenceKey.Split(';').Skip(0))
                {
                    string[] optionKeyAndValue = option.Split('=');
                    if (optionKeyAndValue.Length != 2 || optionKeyAndValue[1].Length < 3)
                    {
                        continue;
                    }

                    string type = optionKeyAndValue[1].Substring(0, 2);
                    string value = optionKeyAndValue[1].Substring(2);
                    if (type == "b:")
                    {
                        options.Add(optionKeyAndValue[0], new Bool(bool.TryParse(value, out bool boolValue) && boolValue));
                    }
                    else if (type == "s:")
                    {
                        options.Add(optionKeyAndValue[0], new String(value));
                    }
                }

                return options.ToImmutable();
            }

            public static string CreateEquivalenceKeyFromOptions(string baseEquivalenceKey, ImmutableDictionary<string, Option> options)
            {
                StringBuilder equivalenceKeyBuilder = new(baseEquivalenceKey);
                foreach (var option in options.OrderBy(item => item.Key))
                {
                    equivalenceKeyBuilder.Append($";{option.Key}={option.Value}");
                }
                return equivalenceKeyBuilder.ToString();
            }
        }

        protected abstract ImmutableDictionary<string, Option> ParseOptionsFromDiagnostic(Diagnostic diagnostic);

        private static async Task<Solution> ApplyActionAndEnableUnsafe(Solution solution, ConvertToSourceGeneratedInteropDocumentCodeAction documentBasedFix, CancellationToken ct)
        {
            var editor = new SolutionEditor(solution);
            var docEditor = await editor.GetDocumentEditorAsync(documentBasedFix.Document.Id, ct).ConfigureAwait(false);
            await documentBasedFix.DocumentEditAction(docEditor, ct).ConfigureAwait(false);

            var docProjectId = documentBasedFix.Document.Project.Id;
            var updatedSolution = editor.GetChangedSolution();
            return AddUnsafe(updatedSolution, updatedSolution.GetProject(docProjectId));
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the syntax root and semantic model
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            SyntaxNode node = root.FindNode(context.Span);

            bool enableUnsafe = doc.Project.CompilationOptions is CSharpCompilationOptions { AllowUnsafe: false };
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                var options = ParseOptionsFromDiagnostic(diagnostic);
                foreach (var fix in CreateAllCodeFixesForOptions(doc, node, options))
                {
                    if (enableUnsafe)
                    {
                        context.RegisterCodeFix(
                            new ConvertToSourceGeneratedInteropSolutionCodeAction(
                                GetDiagnosticTitle(fix.Options),
                                fix.Options.Add(Option.AllowUnsafe, new Option.Bool(true)),
                                doc.Project.Solution,
                                (solution, ct) => ApplyActionAndEnableUnsafe(solution, fix, ct),
                                BaseEquivalenceKey),
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            fix,
                            diagnostic);
                    }
                }
            }
        }

        protected sealed class ConvertToSourceGeneratedInteropDocumentCodeAction : CodeAction
        {
            public ConvertToSourceGeneratedInteropDocumentCodeAction(string title, ImmutableDictionary<string, Option> options, Document document, Func<DocumentEditor, CancellationToken, Task> applyDocumentEdit, string baseEquivalenceKey)
            {
                Title = title;
                Options = options;
                Document = document;
                DocumentEditAction = applyDocumentEdit;
                EquivalenceKey = Option.CreateEquivalenceKeyFromOptions(baseEquivalenceKey, options);
            }

            public override string Title { get; }
            public ImmutableDictionary<string, Option> Options { get; }
            public Document Document { get; }
            public Func<DocumentEditor, CancellationToken, Task> DocumentEditAction { get; }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                DocumentEditor editor = await DocumentEditor.CreateAsync(Document, cancellationToken).ConfigureAwait(false);

                await DocumentEditAction(editor, cancellationToken).ConfigureAwait(false);

                return editor.GetChangedDocument();
            }

            public override string? EquivalenceKey { get; }
        }

        private sealed class ConvertToSourceGeneratedInteropSolutionCodeAction : CodeAction
        {
            public ConvertToSourceGeneratedInteropSolutionCodeAction(string title, ImmutableDictionary<string, Option> options, Solution solution, Func<Solution, CancellationToken, Task<Solution>> applySolutionEdit, string baseEquivalenceKey)
            {
                Title = title;
                Options = options;
                Solution = solution;
                SolutionEditAction = applySolutionEdit;
                EquivalenceKey = Option.CreateEquivalenceKeyFromOptions(baseEquivalenceKey, options);
            }

            public override string Title { get; }
            public ImmutableDictionary<string, Option> Options { get; }
            public Solution Solution { get; }
            public Func<Solution, CancellationToken, Task<Solution>> SolutionEditAction { get; }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return await SolutionEditAction(Solution, cancellationToken).ConfigureAwait(false);
            }

            public override string? EquivalenceKey { get; }
        }

        private sealed class CustomFixAllProvider : FixAllProvider
        {
            public static readonly CustomFixAllProvider Instance = new();
            public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var options = Option.ParseOptionsFromEquivalenceKey(fixAllContext.CodeActionEquivalenceKey);
                var codeFixProvider = (ConvertToSourceGeneratedInteropFixer)fixAllContext.CodeFixProvider;

                bool addUnsafe = options.TryGetValue(Option.AllowUnsafe, out Option allowUnsafeOption) && allowUnsafeOption is Option.Bool(true);

                bool includeFixesWithAdditionalWork = options.TryGetValue(Option.MayRequireAdditionalWork, out Option includeFixesWithAdditionalWorkOption) && includeFixesWithAdditionalWorkOption is Option.Bool(true);
                ImmutableArray<Diagnostic> diagnosticsInScope = await fixAllContext.GetDiagnosticsInScopeAsync().ConfigureAwait(false);


                return CodeAction.Create(codeFixProvider.GetDiagnosticTitle(options),
                    async ct =>
                    {
                        HashSet<Project> projectsToAddUnsafe = new();
                        SolutionEditor solutionEditor = new SolutionEditor(fixAllContext.Solution);

                        foreach (var diagnostic in diagnosticsInScope)
                        {
                            bool mayRequireAdditionalWork = bool.TryParse(diagnostic.Properties[Option.MayRequireAdditionalWork], out bool mayRequireAdditionalWorkValue)
                                ? mayRequireAdditionalWorkValue
                                : false;
                            if (mayRequireAdditionalWork && !includeFixesWithAdditionalWork)
                            {
                                // Don't fix any diagnostics that require additional work if the "fix all" command wasn't triggered from a location
                                // that was able to warn the user that additional work may be required.
                                continue;
                            }
                            DocumentId documentId = solutionEditor.OriginalSolution.GetDocumentId(diagnostic.Location.SourceTree)!;
                            DocumentEditor editor = await solutionEditor.GetDocumentEditorAsync(documentId, ct).ConfigureAwait(false);
                            SyntaxNode root = await diagnostic.Location.SourceTree.GetRootAsync(ct).ConfigureAwait(false);

                            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan);

                            var documentBasedFix = codeFixProvider.CreateFixForSelectedOptions(node, editor.OriginalDocument, options);

                            await documentBasedFix.DocumentEditAction(editor, ct).ConfigureAwait(false);

                            // Record this project as a project we need to allow unsafe blocks in.
                            projectsToAddUnsafe.Add(solutionEditor.OriginalSolution.GetDocument(documentId).Project);
                        }

                        Solution solutionWithUpdatedSources = solutionEditor.GetChangedSolution();

                        if (addUnsafe)
                        {
                            foreach (var project in projectsToAddUnsafe)
                            {
                                solutionWithUpdatedSources = AddUnsafe(solutionWithUpdatedSources, project);
                            }
                        }
                        return solutionWithUpdatedSources;
                    },
                    equivalenceKey: fixAllContext.CodeActionEquivalenceKey);
            }
        }

        private static Solution AddUnsafe(Solution solution, Project project)
        {
            return solution.WithProjectCompilationOptions(project.Id, ((CSharpCompilationOptions)project.CompilationOptions).WithAllowUnsafe(true));
        }

        protected static void MakeNodeParentsPartial(DocumentEditor editor, SyntaxNode syntax)
        {
            for (SyntaxNode? node = syntax.Parent; node is not null; node = node.Parent)
            {
                editor.ReplaceNode(node, (node, gen) => gen.WithModifiers(node, gen.GetModifiers(node).WithPartial(true)));
            }
        }

        protected static SyntaxNode AddExplicitDefaultBoolMarshalling(SyntaxGenerator generator, IMethodSymbol methodSymbol, SyntaxNode generatedDeclaration, string unmanagedTypeMemberIdentifier)
        {
            foreach (IParameterSymbol parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.SpecialType == SpecialType.System_Boolean
                    && !parameter.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
                {
                    SyntaxNode generatedParameterSyntax = generator.GetParameters(generatedDeclaration)[parameter.Ordinal];
                    generatedDeclaration = generator.ReplaceNode(generatedDeclaration, generatedParameterSyntax, generator.AddAttributes(generatedParameterSyntax,
                                    GenerateMarshalAsUnmanagedTypeBoolAttribute(generator, unmanagedTypeMemberIdentifier)));
                }
            }

            if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Boolean
                && !methodSymbol.GetReturnTypeAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
            {
                generatedDeclaration = generator.AddReturnAttributes(generatedDeclaration,
                    GenerateMarshalAsUnmanagedTypeBoolAttribute(generator, unmanagedTypeMemberIdentifier));
            }

            return generatedDeclaration;


            static SyntaxNode GenerateMarshalAsUnmanagedTypeBoolAttribute(SyntaxGenerator generator, string unmanagedTypeMemberIdentifier)
                 => generator.Attribute(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute,
                     generator.AttributeArgument(
                         generator.MemberAccessExpression(
                             generator.DottedName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                             generator.IdentifierName(unmanagedTypeMemberIdentifier))));
        }
    }
}
