// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace SourceGenerators.Tests
{
    internal static class RoslynTestUtils
    {
        /// <summary>
        /// Creates a canonical Roslyn project for testing.
        /// </summary>
        /// <param name="references">Assembly references to include in the project.</param>
        /// <param name="includeBaseReferences">Whether to include references to the BCL assemblies.</param>
        public static Project CreateTestProject(IEnumerable<Assembly>? references, bool includeBaseReferences = true)
        {
            string corelib = Assembly.GetAssembly(typeof(object))!.Location;
            string runtimeDir = Path.GetDirectoryName(corelib)!;

            var refs = new List<MetadataReference>();
            if (includeBaseReferences)
            {
                refs.Add(MetadataReference.CreateFromFile(corelib));
                refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));
                refs.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
            }

            if (references != null)
            {
                foreach (var r in references)
                {
                    refs.Add(MetadataReference.CreateFromFile(r.Location));
                }
            }

            return new AdhocWorkspace()
                        .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                        .AddProject("Test", "test.dll", "C#")
                            .WithMetadataReferences(refs)
                            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
        }

        public static Task CommitChanges(this Project proj, params string[] ignorables)
        {
            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));
            return AssertNoDiagnostic(proj, ignorables);
        }

        public static async Task AssertNoDiagnostic(this Project proj, params string[] ignorables)
        {
            foreach (Document doc in proj.Documents)
            {
                SemanticModel? sm = await doc.GetSemanticModelAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.NotNull(sm);

                foreach (Diagnostic d in sm!.GetDiagnostics())
                {
                    bool ignore = ignorables.Any(ig => d.Id == ig);

                    Assert.True(ignore, d.ToString());
                }
            }
        }

        private static Project WithDocuments(this Project project, IEnumerable<string> sources, IEnumerable<string>? sourceNames = null)
        {
            int count = 0;
            Project result = project;
            if (sourceNames != null)
            {
                List<string> names = sourceNames.ToList();
                foreach (string s in sources)
                    result = result.WithDocument(names[count++], s);
            }
            else
            {
                foreach (string s in sources)
                    result = result.WithDocument($"src-{count++}.cs", s);
            }

            return result;
        }

        public static Project WithDocument(this Project proj, string name, string text)
        {
            return proj.AddDocument(name, text).Project;
        }

        public static Document FindDocument(this Project proj, string name)
        {
            foreach (Document doc in proj.Documents)
            {
                if (doc.Name == name)
                {
                    return doc;
                }
            }

            throw new FileNotFoundException(name);
        }

        /// <summary>
        /// Looks for /*N+*/ and /*-N*/ markers in a string and creates a TextSpan containing the enclosed text.
        /// </summary>
        public static TextSpan MakeSpan(string text, int spanNum)
        {
            int start = text.IndexOf($"/*{spanNum}+*/", StringComparison.Ordinal);
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spanNum));
            }

            start += 6;

            int end = text.IndexOf($"/*-{spanNum}*/", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spanNum));
            }

            end -= 1;

            return new TextSpan(start, end - start);
        }

        /// <summary>
        /// Runs a Roslyn generator over a set of source files.
        /// </summary>
        public static async Task<(ImmutableArray<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGenerator(
            ISourceGenerator generator,
            IEnumerable<Assembly>? references,
            IEnumerable<string> sources,
            AnalyzerConfigOptionsProvider? optionsProvider = null,
            bool includeBaseReferences = true,
            CancellationToken cancellationToken = default)
        {
            Project proj = CreateTestProject(references, includeBaseReferences);

            proj = proj.WithDocuments(sources);

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            Compilation? comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

            CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator }, optionsProvider: optionsProvider);
            GeneratorDriver gd = cgd.RunGenerators(comp!, cancellationToken);

            GeneratorDriverRunResult r = gd.GetRunResult();
            return (r.Results[0].Diagnostics, r.Results[0].GeneratedSources);
        }

        /// <summary>
        /// Runs a Roslyn analyzer over a set of source files.
        /// </summary>
        public static async Task<IList<Diagnostic>> RunAnalyzer(
            DiagnosticAnalyzer analyzer,
            IEnumerable<Assembly> references,
            IEnumerable<string> sources)
        {
            Project proj = CreateTestProject(references);

            proj = proj.WithDocuments(sources);

            await proj.CommitChanges().ConfigureAwait(false);

            ImmutableArray<DiagnosticAnalyzer> analyzers = ImmutableArray.Create(analyzer);

            Compilation? comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
            return await comp!.WithAnalyzers(analyzers).GetAllDiagnosticsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Runs a Roslyn analyzer and fixer.
        /// </summary>
        public static async Task<IList<string>> RunAnalyzerAndFixer(
            DiagnosticAnalyzer analyzer,
            CodeFixProvider fixer,
            IEnumerable<Assembly> references,
            IEnumerable<string> sources,
            IEnumerable<string>? sourceNames = null,
            string? defaultNamespace = null,
            string? extraFile = null)
        {
            Project proj = CreateTestProject(references);

            int count = sources.Count();
            proj = proj.WithDocuments(sources, sourceNames);

            if (defaultNamespace != null)
            {
                proj = proj.WithDefaultNamespace(defaultNamespace);
            }

            await proj.CommitChanges().ConfigureAwait(false);

            ImmutableArray<DiagnosticAnalyzer> analyzers = ImmutableArray.Create(analyzer);

            while (true)
            {
                Compilation? comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
                ImmutableArray<Diagnostic> diags = await comp!.WithAnalyzers(analyzers).GetAllDiagnosticsAsync().ConfigureAwait(false);
                if (diags.IsEmpty)
                {
                    // no more diagnostics reported by the analyzers
                    break;
                }

                var actions = new List<CodeAction>();
                foreach (Diagnostic d in diags)
                {
                    Document? doc = proj.GetDocument(d.Location.SourceTree);

                    CodeFixContext context = new CodeFixContext(doc!, d, (action, _) => actions.Add(action), CancellationToken.None);
                    await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                }

                if (actions.Count == 0)
                {
                    // nothing to fix
                    break;
                }

                ImmutableArray<CodeActionOperation> operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
                Solution solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                Project? changedProj = solution.GetProject(proj.Id);
                if (changedProj != proj)
                {
                    proj = await RecreateProjectDocumentsAsync(changedProj!).ConfigureAwait(false);
                }
            }

            var results = new List<string>();

            if (sourceNames != null)
            {
                List<string> l = sourceNames.ToList();
                for (int i = 0; i < count; i++)
                {
                    SourceText s = await proj.FindDocument(l[i]).GetTextAsync().ConfigureAwait(false);
                    results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    SourceText s = await proj.FindDocument($"src-{i}.cs").GetTextAsync().ConfigureAwait(false);
                    results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
                }
            }

            if (extraFile != null)
            {
                SourceText s = await proj.FindDocument(extraFile).GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }

            return results;
        }

        private static async Task<Project> RecreateProjectDocumentsAsync(Project project)
        {
            foreach (DocumentId documentId in project.DocumentIds)
            {
                Document? document = project.GetDocument(documentId);
                document = await RecreateDocumentAsync(document!).ConfigureAwait(false);
                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document)
        {
            SourceText newText = await document.GetTextAsync().ConfigureAwait(false);
            return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
        }
    }
}
