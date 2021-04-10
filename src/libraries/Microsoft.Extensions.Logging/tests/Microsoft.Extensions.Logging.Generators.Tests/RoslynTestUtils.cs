// © Microsoft Corporation. All rights reserved.

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

namespace Microsoft.Extensions.Logging.Generators.Test
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
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
            var corelib = Assembly.GetAssembly(typeof(object))!.Location;
            var runtimeDir = Path.GetDirectoryName(corelib)!;
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly

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

#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AdhocWorkspace()
                        .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                        .AddProject("Test", "test.dll", "C#")
                            .WithMetadataReferences(refs)
                            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public static Task CommitChanges(this Project proj, params string[] ignorables)
        {
            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));
            return AssertNoDiagnostic(proj, ignorables);
        }

        public static async Task AssertNoDiagnostic(this Project proj, params string[] ignorables)
        {
            foreach (var doc in proj.Documents)
            {
                var sm = await doc.GetSemanticModelAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.NotNull(sm);

                foreach (var d in sm!.GetDiagnostics())
                {
                    bool ignore = ignorables.Any(ig => d.Id == ig);

                    Assert.True(ignore, d.ToString());
                }
            }
        }

        public static Project WithDocument(this Project proj, string name, string text)
        {
            return proj.AddDocument(name, text).Project;
        }

        public static Document FindDocument(this Project proj, string name)
        {
            foreach (var doc in proj.Documents)
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
            var proj = CreateTestProject(references, includeBaseReferences);

            var count = 0;
            foreach (var s in sources)
            {
                proj = proj.WithDocument($"src-{count++}.cs", s);
            }

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            var comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);

            var cgd = CSharpGeneratorDriver.Create(new[] { generator }, optionsProvider: optionsProvider);
            var gd = cgd.RunGenerators(comp!, cancellationToken);

            var r = gd.GetRunResult();
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
            var proj = CreateTestProject(references);

            var count = 0;
            foreach (var s in sources)
            {
                proj = proj.WithDocument($"src-{count++}.cs", s);
            }

            await proj.CommitChanges().ConfigureAwait(false);

            var analyzers = ImmutableArray.Create(analyzer);

            var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
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
            var proj = CreateTestProject(references);

            var count = 0;
            if (sourceNames != null)
            {
                var l = sourceNames.ToList();
                foreach (var s in sources)
                {
                    proj = proj.WithDocument(l[count++], s);
                }
            }
            else
            {
                foreach (var s in sources)
                {
                    proj = proj.WithDocument($"src-{count++}.cs", s);
                }
            }

            if (defaultNamespace != null)
            {
                proj = proj.WithDefaultNamespace(defaultNamespace);
            }

            await proj.CommitChanges().ConfigureAwait(false);

            var analyzers = ImmutableArray.Create(analyzer);

            while (true)
            {
                var comp = await proj!.GetCompilationAsync().ConfigureAwait(false);
                var diags = await comp!.WithAnalyzers(analyzers).GetAllDiagnosticsAsync().ConfigureAwait(false);
                if (diags.IsEmpty)
                {
                    // no more diagnostics reported by the analyzers
                    break;
                }

                var actions = new List<CodeAction>();
                foreach (var d in diags)
                {
                    var doc = proj.GetDocument(d.Location.SourceTree);

                    var context = new CodeFixContext(doc!, d, (action, _) => actions.Add(action), CancellationToken.None);
                    await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
                }

                if (actions.Count == 0)
                {
                    // nothing to fix
                    break;
                }

                var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
                var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
                var changedProj = solution.GetProject(proj.Id);
                if (changedProj != proj)
                {
                    proj = await RecreateProjectDocumentsAsync(changedProj!).ConfigureAwait(false);
                }
            }

            var results = new List<string>();

            if (sourceNames != null)
            {
                var l = sourceNames.ToList();
                for (int i = 0; i < count; i++)
                {
                    var s = await proj.FindDocument(l[i]).GetTextAsync().ConfigureAwait(false);
                    results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var s = await proj.FindDocument($"src-{i}.cs").GetTextAsync().ConfigureAwait(false);
                    results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
                }
            }

            if (extraFile != null)
            {
                var s = await proj.FindDocument(extraFile).GetTextAsync().ConfigureAwait(false);
                results.Add(s.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
            }

            return results;
        }

        private static async Task<Project> RecreateProjectDocumentsAsync(Project project)
        {
            foreach (var documentId in project.DocumentIds)
            {
                var document = project.GetDocument(documentId);
                document = await RecreateDocumentAsync(document!).ConfigureAwait(false);
                project = document.Project;
            }

            return project;
        }

        private static async Task<Document> RecreateDocumentAsync(Document document)
        {
            var newText = await document.GetTextAsync().ConfigureAwait(false);
            return document.WithText(SourceText.From(newText.ToString(), newText.Encoding, newText.ChecksumAlgorithm));
        }
    }
}
