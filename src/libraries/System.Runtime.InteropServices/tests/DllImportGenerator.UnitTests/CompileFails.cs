using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    public class CompileFails
    {
        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new object[] { CodeSnippets.UserDefinedPrefixedAttributes, 0, 3 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<char>(CharSet.Auto), 5, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<char>(CharSet.None), 5, 0 };
            yield return new object[] { CodeSnippets.BasicParametersAndModifiersWithCharSet<char>(CharSet.Ansi), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I1), 5, 0 };
            yield return new object[] { CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U1), 5, 0 };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.DllImportGenerator());

            // Verify the compilation failed with errors.
            int generatorErrors = generatorDiags.Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedGeneratorErrors, generatorErrors);

            int compilerErrors = newComp.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);
            Assert.Equal(expectedCompilerErrors, compilerErrors);
        }
    }
}
