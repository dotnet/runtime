// © Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Test
{
    public class LoggerMessageGeneratorEmitterTests
    {
        [Fact]
        public async Task TestEmitter()
        {
            var testSourceCode = File.ReadAllText(@"..\..\..\Definitions.cs");

            var proj = RoslynTestUtils.CreateTestProject()
                .WithLoggingBoilerplate()
                .WithDocument("Definitions.cs", testSourceCode);

            await proj.CommitChanges("CS8795").ConfigureAwait(false);
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
            var comp = (await proj.GetCompilationAsync().ConfigureAwait(false))!;
#pragma warning restore SA1009 // Closing parenthesis should be spaced correctly

            // This tests exists strictly to calculate the code coverage
            // attained by processing Definitions.cs. The functionality of the
            // resulting code is tested via LoggerMessageGeneratedCodeTests.cs
            for (int i = 0; i < 2; i++)
            {
                var p = new Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator.Parser(comp, d => { }, CancellationToken.None);
                var e = new Microsoft.Extensions.Logging.Generators.LoggerMessageGenerator.Emitter(i == 0);

                var allNodes = comp.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
                var allClasses = allNodes.Where(d => d.IsKind(SyntaxKind.ClassDeclaration)).OfType<ClassDeclarationSyntax>();
                var lc = p.GetLogClasses(allClasses);

                var generatedSource = e.Emit(lc, CancellationToken.None);
                Assert.True(!string.IsNullOrEmpty(generatedSource));

                Assert.Throws<OperationCanceledException>(() => _ = e.Emit(lc, new CancellationToken(true)));
            }
        }
    }
}
