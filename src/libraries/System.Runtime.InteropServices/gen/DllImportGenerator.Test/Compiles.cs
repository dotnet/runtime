using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DllImportGenerator.Test
{
    public class Compiles
    {
        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new[] { CodeSnippets.TrivialClassDeclarations };
            yield return new[] { CodeSnippets.TrivialStructDeclarations };
            yield return new[] { CodeSnippets.MultipleAttributes };
            yield return new[] { CodeSnippets.NestedNamespace };
            yield return new[] { CodeSnippets.NestedTypes };
            yield return new[] { CodeSnippets.UserDefinedEntryPoint };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers };
            yield return new[] { CodeSnippets.DefaultParameters };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public void ValidateSnippets(string source)
        {
            Compilation comp = CreateCompilation(source);
            var compDiags = comp.GetDiagnostics();
            foreach (var diag in compDiags)
            {
                Assert.True(
                    "CS8795".Equals(diag.Id)        // Partial method impl missing
                    || "CS0234".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS0246".Equals(diag.Id)     // Missing type or namespace - GeneratedDllImportAttribute
                    || "CS8019".Equals(diag.Id));   // Unnecessary using
            }

            var newComp = RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(generatorDiags);

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }

        private static Compilation CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(outputKind));

        private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => new CSharpGeneratorDriver(c.SyntaxTrees.First().Options,
                ImmutableArray.Create(generators),
                null,
                ImmutableArray<AdditionalText>.Empty);

        private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunFullGeneration(c, out var d, out diagnostics);
            return d;
        }
    }
}
