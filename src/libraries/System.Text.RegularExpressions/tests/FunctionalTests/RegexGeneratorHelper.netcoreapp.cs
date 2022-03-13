// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions.Generator;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static class RegexGeneratorHelper
    {
        private static readonly CSharpParseOptions s_previewParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        private static readonly EmitOptions s_emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
        private static readonly CSharpGeneratorDriver s_generatorDriver = CSharpGeneratorDriver.Create(new[] { new RegexGenerator().AsSourceGenerator() }, parseOptions: s_previewParseOptions);
        private static Compilation? s_compilation;

        internal static MetadataReference[] References { get; } = CreateReferences();

        private static MetadataReference[] CreateReferences()
        {
            if (PlatformDetection.IsBrowser)
            {
                // These tests that use Roslyn don't work well on browser wasm today
                return new MetadataReference[0];
            }

            // Typically we'd want to use the right reference assemblies, but as we're not persisting any
            // assets and only using this for testing purposes, referencing implementation assemblies is sufficient.
            string corelibPath = typeof(object).Assembly.Location;
            return new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(corelibPath), "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Unsafe).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
            };
        }

        internal static byte[] CreateAssemblyImage(string source, string assemblyName)
        {
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)) },
                References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ms = new MemoryStream();
            if (compilation.Emit(ms).Success)
            {
                return ms.ToArray();
            }

            throw new InvalidOperationException();
        }

        internal static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
            string code, bool compile = false, LanguageVersion langVersion = LanguageVersion.Preview, MetadataReference[]? additionalRefs = null, bool allowUnsafe = false, CancellationToken cancellationToken = default)
        {
            var proj = new AdhocWorkspace()
                .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                .AddProject("RegexGeneratorTest", "RegexGeneratorTest.dll", "C#")
                .WithMetadataReferences(additionalRefs is not null ? References.Concat(additionalRefs) : References)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe)
                .WithNullableContextOptions(NullableContextOptions.Enable))
                .WithParseOptions(new CSharpParseOptions(langVersion))
                .AddDocument("RegexGenerator.g.cs", SourceText.From(code, Encoding.UTF8)).Project;

            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

            Compilation? comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            Debug.Assert(comp is not null);

            var generator = new RegexGenerator();
            CSharpGeneratorDriver cgd = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(langVersion));
            GeneratorDriver gd = cgd.RunGenerators(comp!, cancellationToken);
            GeneratorDriverRunResult generatorResults = gd.GetRunResult();
            if (!compile)
            {
                return generatorResults.Diagnostics;
            }

            comp = comp.AddSyntaxTrees(generatorResults.GeneratedTrees.ToArray());
            EmitResult results = comp.Emit(Stream.Null, cancellationToken: cancellationToken);
            ImmutableArray<Diagnostic> generatorDiagnostics = generatorResults.Diagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Hidden);
            ImmutableArray<Diagnostic> resultsDiagnostics = results.Diagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Hidden);
            if (!results.Success || resultsDiagnostics.Length != 0 || generatorDiagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, resultsDiagnostics.Concat(generatorDiagnostics)) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => t.ToString())));
            }

            return generatorResults.Diagnostics.Concat(results.Diagnostics).Where(d => d.Severity != DiagnosticSeverity.Hidden).ToArray();
        }

        internal static async Task<Regex> SourceGenRegexAsync(
            string pattern, RegexOptions? options = null, TimeSpan? matchTimeout = null, CancellationToken cancellationToken = default)
        {
            Regex[] results = await SourceGenRegexAsync(new[] { (pattern, options, matchTimeout) }, cancellationToken).ConfigureAwait(false);
            return results[0];
        }

        internal static async Task<Regex[]> SourceGenRegexAsync(
            (string pattern, RegexOptions? options, TimeSpan? matchTimeout)[] regexes, CancellationToken cancellationToken = default)
        {
            // Un-ifdef to compile each regex individually, which can be useful if one regex among thousands is causing a failure.
            // We compile them all en mass for test efficiency, but it can make it harder to debug a compilation failure in one of them.
#if false
            if (regexes.Length > 1)
            {
                var r = new List<Regex>();
                foreach (var input in regexes)
                {
                    r.AddRange(await SourceGenRegexAsync(new[] { input }, cancellationToken));
                }
                return r.ToArray();
            }
#endif

            Debug.Assert(regexes.Length > 0);

            var code = new StringBuilder();
            code.AppendLine("using System.Text.RegularExpressions;");
            code.AppendLine("public partial class C {");

            // Build up the code for all of the regexes
            int count = 0;
            foreach (var regex in regexes)
            {
                Assert.True(regex.options is not null || regex.matchTimeout is null);
                code.Append($"    [RegexGenerator({SymbolDisplay.FormatLiteral(regex.pattern, quote: true)}");
                if (regex.options is not null)
                {
                    code.Append($", {string.Join(" | ", regex.options.ToString().Split(',').Select(o => $"RegexOptions.{o.Trim()}"))}");
                    if (regex.matchTimeout is not null)
                    {
                        code.Append(string.Create(CultureInfo.InvariantCulture, $", {(int)regex.matchTimeout.Value.TotalMilliseconds}"));
                    }
                }
                code.AppendLine($")] public static partial Regex Get{count}();");

                count++;
            }

            code.AppendLine("}");

            // Use a cached compilation to save a little time.  Rather than creating an entirely new workspace
            // for each test, just create a single compilation, cache it, and then replace its syntax tree
            // on each test.
            if (s_compilation is not Compilation comp)
            {
                // Create the project containing the source.
                var proj = new AdhocWorkspace()
                    .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                    .AddProject("Test", "test.dll", "C#")
                    .WithMetadataReferences(References)
                    .WithCompilationOptions(
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary,
                            warningLevel: 9999, // docs recommend using "9999" to catch all warnings now and in the future
                            specificDiagnosticOptions: ImmutableDictionary<string, ReportDiagnostic>.Empty.Add("SYSLIB1045", ReportDiagnostic.Hidden)) // regex with limited support
                            .WithNullableContextOptions(NullableContextOptions.Enable))
                            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.Diagnose))
                    .AddDocument("RegexGenerator.g.cs", SourceText.From("// Empty", Encoding.UTF8)).Project;
                Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

                s_compilation = comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                Debug.Assert(comp is not null);
            }

            comp = comp.ReplaceSyntaxTree(comp.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(SourceText.From(code.ToString(), Encoding.UTF8), s_previewParseOptions));

            // Run the generator
            GeneratorDriverRunResult generatorResults = s_generatorDriver.RunGenerators(comp!, cancellationToken).GetRunResult();
            ImmutableArray<Diagnostic> generatorDiagnostics = generatorResults.Diagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Hidden);
            if (generatorDiagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => NumberLines(t.ToString()))) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorDiagnostics));
            }

            // Compile the assembly to a stream
            var dll = new MemoryStream();
            comp = comp.AddSyntaxTrees(generatorResults.GeneratedTrees.ToArray());
            EmitResult results = comp.Emit(dll, options: s_emitOptions, cancellationToken: cancellationToken);
            ImmutableArray<Diagnostic> resultsDiagnostics = results.Diagnostics.RemoveAll(d => d.Severity <= DiagnosticSeverity.Hidden);
            if (!results.Success || resultsDiagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => NumberLines(t.ToString()))) + Environment.NewLine +
                    string.Join(Environment.NewLine, resultsDiagnostics.Concat(generatorDiagnostics)));
            }
            dll.Position = 0;

            // Load the assembly into its own AssemblyLoadContext.
            var alc = new RegexLoadContext(Environment.CurrentDirectory);
            Assembly a = alc.LoadFromStream(dll);

            // Instantiate each regex using the newly created static Get method that was source generated.
            var instances = new Regex[count];
            Type c = a.GetType("C")!;
            for (int i = 0; i < instances.Length; i++)
            {
                instances[i] = (Regex)c.GetMethod($"Get{i}")!.Invoke(null, null)!;
            }

            // Issue an unload on the ALC, so it'll be collected once the Regex instance is collected
            alc.Unload();

            return instances;
        }

        /// <summary>Number the lines in the source file.</summary>
        private static string NumberLines(string source) =>
            string.Join(Environment.NewLine, source.Split(Environment.NewLine).Select((line, lineNumber) => $"{lineNumber,6}: {line}"));

        /// <summary>Simple AssemblyLoadContext used to load source generated regex assemblies so they can be unloaded.</summary>
        private sealed class RegexLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public RegexLoadContext(string pluginPath) : base(isCollectible: true)
            {
                _resolver = new AssemblyDependencyResolver(pluginPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }

                return IntPtr.Zero;
            }
        }
    }
}
