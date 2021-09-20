using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.DllImportGenerator;

namespace DllImportGenerator.UnitTests
{
    public class IncrementalGenerationTests
    {
        public const string RequiresIncrementalSyntaxTreeModifySupport = "The GeneratorDriver treats all SyntaxTree replace operations on a Compilation as an Add/Remove operation instead of a Modify operation"
            + ", so all cached results based on that input are thrown out. As a result, we cannot validate that unrelated changes within the same SyntaxTree do not cause regeneration.";

        [Fact]
        public async Task AddingNewUnrelatedType_DoesNotRegenerateSource()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            Compilation comp1 = await TestUtils.CreateCompilation(source);

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new IIncrementalGenerator[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct Foo {}", new CSharpParseOptions(LanguageVersion.Preview)));
            driver.RunGenerators(comp2);

            Assert.Collection(generator.IncrementalTracker.ExecutedSteps,
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.CalculateStubInformation, step.Step);
                });
        }

        [Fact(Skip = RequiresIncrementalSyntaxTreeModifySupport)]
        public async Task AppendingUnrelatedSource_DoesNotRegenerateSource()
        {
            string source = $"namespace NS{{{CodeSnippets.BasicParametersAndModifiers<int>()}}}";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = await TestUtils.CreateCompilation(new[] { syntaxTree });

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            SyntaxTree newTree = syntaxTree.WithRootAndOptions(syntaxTree.GetCompilationUnitRoot().AddMembers(SyntaxFactory.ParseMemberDeclaration("struct Foo {}")!), syntaxTree.Options);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), newTree);
            driver.RunGenerators(comp2);

            Assert.Collection(generator.IncrementalTracker.ExecutedSteps,
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.CalculateStubInformation, step.Step);
                });
        }

        [Fact]
        public async Task AddingFileWithNewGeneratedDllImport_DoesNotRegenerateOriginalMethod()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            Compilation comp1 = await TestUtils.CreateCompilation(source);

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(CodeSnippets.BasicParametersAndModifiers<bool>(), new CSharpParseOptions(LanguageVersion.Preview)));
            driver.RunGenerators(comp2);

            Assert.Equal(2, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.CalculateStubInformation));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.GenerateSingleStub));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.NormalizeWhitespace));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.ConcatenateStubs));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.OutputSourceFile));
        }

        [Fact]
        public async Task ReplacingFileWithNewGeneratedDllImport_DoesNotRegenerateStubsInOtherFiles()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            Compilation comp1 = await TestUtils.CreateCompilation(new string[] { CodeSnippets.BasicParametersAndModifiers<int>(), CodeSnippets.BasicParametersAndModifiers<bool>() });

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(CodeSnippets.BasicParametersAndModifiers<ulong>(), new CSharpParseOptions(LanguageVersion.Preview)));
            driver.RunGenerators(comp2);

            Assert.Equal(2, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.CalculateStubInformation));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.GenerateSingleStub));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.NormalizeWhitespace));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.ConcatenateStubs));
            Assert.Equal(1, generator.IncrementalTracker.ExecutedSteps.Count(s => s.Step == IncrementalityTracker.StepName.OutputSourceFile));
        }

        [Fact]
        public async Task ChangingMarshallingStrategy_RegeneratesStub()
        {
            string stubSource = CodeSnippets.BasicParametersAndModifiers("CustomType");

            string customTypeImpl1 = "struct CustomType { System.IntPtr handle; }";

            string customTypeImpl2 = "class CustomType : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid { public CustomType():base(true){} protected override bool ReleaseHandle(){return true;} }";


            Compilation comp1 = await TestUtils.CreateCompilation(stubSource);

            SyntaxTree customTypeImpl1Tree = CSharpSyntaxTree.ParseText(customTypeImpl1, new CSharpParseOptions(LanguageVersion.Preview));
            comp1 = comp1.AddSyntaxTrees(customTypeImpl1Tree);

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            Compilation comp2 = comp1.ReplaceSyntaxTree(customTypeImpl1Tree, CSharpSyntaxTree.ParseText(customTypeImpl2, new CSharpParseOptions(LanguageVersion.Preview)));
            driver.RunGenerators(comp2);

            Assert.Collection(generator.IncrementalTracker.ExecutedSteps,
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.CalculateStubInformation, step.Step);
                },
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.GenerateSingleStub, step.Step);
                },
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.NormalizeWhitespace, step.Step);
                },
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.ConcatenateStubs, step.Step);
                },
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.OutputSourceFile, step.Step);
                });
        }

        [Fact(Skip = RequiresIncrementalSyntaxTreeModifySupport)]
        public async Task ChangingMarshallingAttributes_SameStrategy_DoesNotRegenerate()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<bool>();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = await TestUtils.CreateCompilation(new[] { syntaxTree });

            Microsoft.Interop.DllImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator });

            driver = driver.RunGenerators(comp1);

            generator.IncrementalTracker = new IncrementalityTracker();

            SyntaxTree newTree = syntaxTree.WithRootAndOptions(
                syntaxTree.GetCompilationUnitRoot().AddMembers(
                    SyntaxFactory.ParseMemberDeclaration(
                        CodeSnippets.MarshalAsParametersAndModifiers<bool>(System.Runtime.InteropServices.UnmanagedType.Bool))!),
                syntaxTree.Options);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), newTree);
            driver.RunGenerators(comp2);

            Assert.Collection(generator.IncrementalTracker.ExecutedSteps,
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.CalculateStubInformation, step.Step);
                },
                step =>
                {
                    Assert.Equal(IncrementalityTracker.StepName.GenerateSingleStub, step.Step);
                });
        }
    }
}
