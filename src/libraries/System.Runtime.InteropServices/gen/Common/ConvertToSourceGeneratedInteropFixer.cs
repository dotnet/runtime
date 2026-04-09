// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

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

        protected virtual IEnumerable<ConvertToSourceGeneratedInteropFix> CreateAllFixesForDiagnosticOptions(SyntaxNode node, ImmutableDictionary<string, Option> options)
        {
            // By default, we only have one fix for the options specified from the diagnostic.
            yield return new ConvertToSourceGeneratedInteropFix(CreateFixForSelectedOptions(node, options), options);
        }

        protected abstract Func<DocumentEditor, CancellationToken, Task> CreateFixForSelectedOptions(SyntaxNode node, ImmutableDictionary<string, Option> selectedOptions);

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
                        options.Add(optionKeyAndValue[0], new Bool(bool.Parse(value)));
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

        protected abstract ImmutableDictionary<string, Option> CombineOptions(ImmutableDictionary<string, Option> fixAllOptions, ImmutableDictionary<string, Option> diagnosticOptions);

        private ImmutableDictionary<string, Option> GetOptionsForIndividualFix(ImmutableDictionary<string, Option> fixAllOptions, Diagnostic diagnostic)
        {
            return CombineOptions(fixAllOptions, ParseOptionsFromDiagnostic(diagnostic));
        }

        private static async Task<Solution> ApplyActionAndEnableUnsafe(Solution solution, DocumentId documentId, Func<DocumentEditor, CancellationToken, Task> documentBasedFix, CancellationToken ct)
        {
            var editor = new SolutionEditor(solution);
            var docEditor = await editor.GetDocumentEditorAsync(documentId, ct).ConfigureAwait(false);
            await documentBasedFix(docEditor, ct).ConfigureAwait(false);

            var docProjectId = documentId.ProjectId;
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
                foreach (var fix in CreateAllFixesForDiagnosticOptions(node, options))
                {
                    if (enableUnsafe)
                    {
                        var selectedOptions = fix.SelectedOptions.Add(Option.AllowUnsafe, new Option.Bool(true));

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                GetDiagnosticTitle(selectedOptions),
                                ct => ApplyActionAndEnableUnsafe(doc.Project.Solution, doc.Id, fix.ApplyFix, ct),
                                Option.CreateEquivalenceKeyFromOptions(BaseEquivalenceKey, selectedOptions)),
                            diagnostic);
                    }
                    else
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                GetDiagnosticTitle(fix.SelectedOptions),
                                async ct =>
                                {
                                    DocumentEditor editor = await DocumentEditor.CreateAsync(doc, ct).ConfigureAwait(false);

                                    await fix.ApplyFix(editor, ct).ConfigureAwait(false);

                                    return editor.GetChangedDocument();
                                },
                                Option.CreateEquivalenceKeyFromOptions(BaseEquivalenceKey, fix.SelectedOptions)),
                            diagnostic);
                    }
                }
            }
        }

        protected record struct ConvertToSourceGeneratedInteropFix(Func<DocumentEditor, CancellationToken, Task> ApplyFix, ImmutableDictionary<string, Option> SelectedOptions);

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
                            bool mayRequireAdditionalWork = diagnostic.Properties.TryGetValue(Option.MayRequireAdditionalWork, out string mayRequireAdditionalWorkString)
                                && bool.TryParse(mayRequireAdditionalWorkString, out bool mayRequireAdditionalWorkValue)
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

                            var documentBasedFix = codeFixProvider.CreateFixForSelectedOptions(node, codeFixProvider.GetOptionsForIndividualFix(options, diagnostic));

                            await documentBasedFix(editor, ct).ConfigureAwait(false);

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

        protected static SyntaxNode AddHResultStructAsErrorMarshalling(SyntaxGenerator generator, IMethodSymbol methodSymbol, SyntaxNode generatedDeclaration)
        {
            if (methodSymbol.ReturnType is { TypeKind: TypeKind.Struct }
                && IsHResultLikeType(methodSymbol.ReturnType)
                && !methodSymbol.GetReturnTypeAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_InteropServices_MarshalAsAttribute))
            {
                generatedDeclaration = generator.AddReturnAttributes(generatedDeclaration,
                    GeneratedMarshalAsUnmanagedTypeErrorAttribute(generator));
            }

            return generatedDeclaration;


            static bool IsHResultLikeType(ITypeSymbol type)
            {
                string typeName = type.Name;
                return typeName.Equals("hr", StringComparison.OrdinalIgnoreCase)
                    || typeName.Equals("hresult", StringComparison.OrdinalIgnoreCase);
            }

            // MarshalAs(UnmanagedType.Error)
            static SyntaxNode GeneratedMarshalAsUnmanagedTypeErrorAttribute(SyntaxGenerator generator)
                 => generator.Attribute(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute,
                     generator.AttributeArgument(
                         generator.MemberAccessExpression(
                             generator.DottedName(TypeNames.System_Runtime_InteropServices_UnmanagedType),
                             generator.IdentifierName(nameof(UnmanagedType.Error)))));
        }
    }
}
