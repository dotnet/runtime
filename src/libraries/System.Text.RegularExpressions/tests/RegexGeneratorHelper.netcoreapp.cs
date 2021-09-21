// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly MetadataReference[] s_refs = CreateReferences();
        private static readonly EmitOptions s_emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded);
        private static readonly CSharpGeneratorDriver s_generatorDriver = CSharpGeneratorDriver.Create(new[] { new RegexGenerator().AsSourceGenerator() }, parseOptions: s_previewParseOptions);
        private static Compilation? s_compilation;

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

        internal static async Task<Regex> SourceGenRegexAsync(
            string pattern, RegexOptions? options = null, TimeSpan? matchTimeout = null, CancellationToken cancellationToken = default)
        {
            Assert.True(options is not null || matchTimeout is null);
            string attr = $"[RegexGenerator({SymbolDisplay.FormatLiteral(pattern, quote: true)}";
            if (options is not null)
            {
                attr += $", {string.Join(" | ", options.ToString().Split(',').Select(o => $"RegexOptions.{o.Trim()}"))}";
                if (matchTimeout is not null)
                {
                    attr += string.Create(CultureInfo.InvariantCulture, $", {(int)matchTimeout.Value.TotalMilliseconds}");
                }
            }
            attr += ")]";

            // Create the source boilerplate for the pattern
            string code = $@"
                using System.Text.RegularExpressions;
                public partial class C
                {{
                    {attr}
                    public static partial Regex Get();
                }}";


            // Use a cached compilation to save a little time.  Rather than creating an entirely new workspace
            // for each test, just create a single compilation, cache it, and then replace its syntax tree
            // on each test.
            if (s_compilation is not Compilation comp)
            {
                // Create the project containing the source.
                var proj = new AdhocWorkspace()
                    .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                    .AddProject("Test", "test.dll", "C#")
                    .WithMetadataReferences(s_refs)
                    .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable))
                    .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
                    .AddDocument("RegexGenerator.g.cs", SourceText.From("// Empty", Encoding.UTF8)).Project;
                Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));

                s_compilation = comp = await proj!.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
                Debug.Assert(comp is not null);
            }

            comp = comp.ReplaceSyntaxTree(comp.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8), s_previewParseOptions));

            // Run the generator
            GeneratorDriverRunResult generatorResults = s_generatorDriver.RunGenerators(comp!, cancellationToken).GetRunResult();
            if (generatorResults.Diagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, generatorResults.Diagnostics) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => NumberLines(t.ToString()))));
            }

            // Compile the assembly to a stream
            var dll = new MemoryStream();
            comp = comp.AddSyntaxTrees(generatorResults.GeneratedTrees.ToArray());
            EmitResult results = comp.Emit(dll, options: s_emitOptions, cancellationToken: cancellationToken);
            if (!results.Success || results.Diagnostics.Length != 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, results.Diagnostics.Concat(generatorResults.Diagnostics)) + Environment.NewLine +
                    string.Join(Environment.NewLine, generatorResults.GeneratedTrees.Select(t => NumberLines(t.ToString()))));
            }
            dll.Position = 0;

            // Load the assembly into its own AssemblyLoadContext.
            var alc = new RegexLoadContext(Environment.CurrentDirectory);
            Assembly a = alc.LoadFromStream(dll);

            // Instantiate a regex using the newly created static Get method that was source generated.
            Regex r = (Regex)a.GetType("C")!.GetMethod("Get")!.Invoke(null, null)!;

            // Issue an unload on the ALC, so it'll be collected once the Regex instance is collected
            alc.Unload();

            return r;
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
