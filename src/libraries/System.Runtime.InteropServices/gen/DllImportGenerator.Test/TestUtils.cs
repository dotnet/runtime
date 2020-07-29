using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DllImportGenerator.Test
{
    internal static class TestUtils
    {
        /// <summary>
        /// Assert the pre-srouce generator compilation has only
        /// the expected failure diagnostics.
        /// </summary>
        /// <param name="comp"></param>
        public static void AssertPreSourceGeneratorCompilation(Compilation comp)
        {
            var compDiags = comp.GetDiagnostics();
            foreach (var diag in compDiags)
            {
                Assert.True(
                    "CS8795".Equals(diag.Id)        // Partial method impl missing
                    || "CS0234".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS0246".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS8019".Equals(diag.Id));   // Unnecessary using
            }
        }

        /// <summary>
        /// Create a compilation given source
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="outputKind">Output type</param>
        /// <returns>The resulting compilation</returns>
        public static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(outputKind));

        /// <summary>
        /// Run the supplied generators on the compilation.
        /// </summary>
        /// <param name="comp">Compilation target</param>
        /// <param name="diagnostics">Resulting diagnostics</param>
        /// <param name="generators">Source generator instances</param>
        /// <returns>The resulting compilation</returns>
        public static Compilation RunGenerators(Compilation comp, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(comp, generators).RunFullGeneration(comp, out var d, out diagnostics);
            return d;
        }

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => new CSharpGeneratorDriver(c.SyntaxTrees.First().Options,
                ImmutableArray.Create(generators),
                null,
                ImmutableArray<AdditionalText>.Empty);
    }
}
