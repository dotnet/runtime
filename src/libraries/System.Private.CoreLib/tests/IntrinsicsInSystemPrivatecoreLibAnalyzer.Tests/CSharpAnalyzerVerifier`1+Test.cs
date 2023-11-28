using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace IntrinsicsInSystemPrivateCoreLib.Test
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public class Test : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
            public Test()
            {
                // Clear out the default reference assemblies. We explicitly add references from the live ref pack,
                // so we don't want the Roslyn test infrastructure to resolve/add any default reference assemblies
                ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
                TestState.AdditionalReferences.AddRange(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences());

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId).CompilationOptions;
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    return solution;
                });
            }
        }
    }
}
