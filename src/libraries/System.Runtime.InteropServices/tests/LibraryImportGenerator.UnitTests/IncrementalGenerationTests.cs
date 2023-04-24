// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Interop.UnitTests;
using Xunit;
using static Microsoft.Interop.LibraryImportGenerator;

namespace LibraryImportGenerator.UnitTests
{
    public class IncrementalGenerationTests
    {
        private static readonly GeneratorDriverOptions EnableIncrementalTrackingDriverOptions = new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

        public const string RequiresIncrementalSyntaxTreeModifySupport = "The GeneratorDriver treats all SyntaxTree replace operations on a Compilation as an Add/Remove operation instead of a Modify operation"
            + ", so all cached results based on that input are thrown out. As a result, we cannot validate that unrelated changes within the same SyntaxTree do not cause regeneration.";

        [Fact]
        public async Task AddingNewUnrelatedType_DoesNotRegenerateSource()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            Compilation comp1 = await TestUtils.CreateCompilation(source);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new IIncrementalGenerator[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct Foo {}", new CSharpParseOptions(LanguageVersion.Preview)));
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.CalculateStubInformation],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public async Task AppendingUnrelatedSource_DoesNotRegenerateSource()
        {
            string source = $$"""
                namespace NS
                {
                    {{CodeSnippets.BasicParametersAndModifiers<int>()}}
                }
                """;

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = await TestUtils.CreateCompilation(new[] { syntaxTree });

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            SyntaxTree newTree = syntaxTree.WithRootAndOptions(syntaxTree.GetCompilationUnitRoot().AddMembers(SyntaxFactory.ParseMemberDeclaration("struct Foo {}")!), syntaxTree.Options);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), newTree);
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.GenerateSingleStub],
                step =>
                {
                    // The calculated stub information will differ since we have a new syntax tree for where to report diagnostics.
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public async Task AddingFileWithNewLibraryImport_DoesNotRegenerateOriginalMethod()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            Compilation comp1 = await TestUtils.CreateCompilation(source);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1), new CSharpParseOptions(LanguageVersion.Preview)));

            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.CalculateStubInformation],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                },
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });
        }

        [Fact]
        public async Task ReplacingFileWithNewLibraryImport_DoesNotRegenerateStubsInOtherFiles()
        {
            Compilation comp1 = await TestUtils.CreateCompilation(new string[] { CodeSnippets.BasicParametersAndModifiers<int>(), CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1) });

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(CodeSnippets.BasicParametersAndModifiers<ulong>(), new CSharpParseOptions(LanguageVersion.Preview)));
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.CalculateStubInformation],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                },
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public async Task ChangingMarshallingStrategy_RegeneratesStub()
        {
            string stubSource = CodeSnippets.BasicParametersAndModifiers("CustomType", CodeSnippets.DisableRuntimeMarshalling);

            string customTypeImpl1 = "struct CustomType { System.IntPtr handle; }";

            string customTypeImpl2 = "class CustomType : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid { public CustomType():base(true){} protected override bool ReleaseHandle(){return true;} }";


            Compilation comp1 = await TestUtils.CreateCompilation(stubSource);

            SyntaxTree customTypeImpl1Tree = CSharpSyntaxTree.ParseText(customTypeImpl1, new CSharpParseOptions(LanguageVersion.Preview));
            comp1 = comp1.AddSyntaxTrees(customTypeImpl1Tree);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.ReplaceSyntaxTree(customTypeImpl1Tree, CSharpSyntaxTree.ParseText(customTypeImpl2, new CSharpParseOptions(LanguageVersion.Preview)));
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.CalculateStubInformation],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });

            Assert.Collection(runResult.TrackedSteps[StepNames.GenerateSingleStub],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });
        }

        [Fact]
        public async Task ChangingMarshallingAttributes_SameStrategy_DoesNotRegenerate()
        {
            string source = CodeSnippets.BasicParametersAndModifiers<int>();

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = await TestUtils.CreateCompilation(new[] { syntaxTree });

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            SyntaxTree newTree = syntaxTree.WithRootAndOptions(
                SyntaxFactory.ParseCompilationUnit(
                    CodeSnippets.MarshalAsParametersAndModifiers<int>(System.Runtime.InteropServices.UnmanagedType.I4)),
                syntaxTree.Options);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), newTree);

            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps[StepNames.CalculateStubInformation],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });

            Assert.Collection(runResult.TrackedSteps[StepNames.GenerateSingleStub],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        public static IEnumerable<object[]> CompilationObjectLivenessSources()
        {
            // Basic stub
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<int>() };
            // Stub with custom string marshaller
            yield return new[] { CodeSnippets.CustomStringMarshallingParametersAndModifiers<string>() };
        }

        // This test requires precise GC to ensure that we're accurately testing that we aren't
        // keeping the Compilation alive.
        [MemberData(nameof(CompilationObjectLivenessSources))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public async Task GeneratorRun_WithNewCompilation_DoesNotKeepOldCompilationAlive(string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = await TestUtils.CreateCompilation(new[] { syntaxTree });

            var (reference, driver) = RunTwoGeneratorOnTwoIterativeCompilationsAndReturnFirst(comp1);

            GC.Collect();

            Assert.False(reference.IsAlive);
            GC.KeepAlive(driver);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static (WeakReference reference, GeneratorDriver driver) RunTwoGeneratorOnTwoIterativeCompilationsAndReturnFirst(Compilation startingCompilation)
            {
                Compilation comp2 = startingCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct NewType {}", new CSharpParseOptions(LanguageVersion.Preview)));

                Microsoft.Interop.LibraryImportGenerator generator = new();
                GeneratorDriver driver = TestUtils.CreateDriver(comp2, null, new[] { generator }, EnableIncrementalTrackingDriverOptions);

                driver = driver.RunGenerators(comp2);

                Compilation comp3 = comp2.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct NewType2 {}", new CSharpParseOptions(LanguageVersion.Preview)));

                GeneratorDriver driver2 = driver.RunGenerators(comp3);

                // Assert here that we did use the last result and didn't regenerate.
                Assert.Collection(driver2.GetRunResult().Results,
                    result =>
                    {
                        Assert.Collection(result.TrackedSteps[StepNames.CalculateStubInformation],
                            step =>
                            {
                                Assert.Collection(step.Outputs,
                                    output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                            });
                    });

                // Return a weak reference to the first edited compilation and the driver from the most recent run.
                // The most recent run with comp3 shouldn't keep anything from comp2 alive.
                return (new WeakReference(comp2), driver2);
            }
        }
    }
}
