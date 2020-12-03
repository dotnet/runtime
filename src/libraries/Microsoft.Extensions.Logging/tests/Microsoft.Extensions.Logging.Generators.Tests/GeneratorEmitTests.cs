// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class GeneratorEmitTests
    {
        [Fact]
        public async Task TestEmitter()
        {
            var testSourceCode = File.ReadAllText(@"..\..\..\Definitions.cs");

            var proj = RoslynTestUtils.CreateTestProject()
                .WithLoggingBoilerplate()
                .WithDocument("Definitions.cs", testSourceCode);

            await proj.CommitChanges("CS8795").ConfigureAwait(false);
            var comp = (await proj.GetCompilationAsync().ConfigureAwait(false))!;

            for (int i = 0; i < 2; i++)
            {
                var p = new Microsoft.Extensions.Logging.Generators.LoggingGenerator.Parser(comp, d => { }, CancellationToken.None);
                var e = new Microsoft.Extensions.Logging.Generators.LoggingGenerator.Emitter(i == 0);

                var allNodes = comp.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
                var allClasses = allNodes.Where(d => d.IsKind(SyntaxKind.ClassDeclaration)).OfType<ClassDeclarationSyntax>();
                var lc = p.GetLogClasses(allClasses);

                var generatedSource = e.Emit(lc, CancellationToken.None);
                Assert.True(!string.IsNullOrEmpty(generatedSource));

                generatedSource = e.Emit(lc, new CancellationToken(true));
                Assert.True(string.IsNullOrEmpty(generatedSource));
            }
        }
    }
}
