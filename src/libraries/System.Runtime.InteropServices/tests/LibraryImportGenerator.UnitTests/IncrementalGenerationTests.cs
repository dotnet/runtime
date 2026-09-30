// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Interop.UnitTests;
using Xunit;
using static Microsoft.Interop.LibraryImportGenerator;

namespace LibraryImportGenerator.UnitTests
{
    public class IncrementalGenerationTests
    {
        private static readonly GeneratorDriverOptions EnableIncrementalTrackingDriverOptions = new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

        [Fact]
        public void AddingNewUnrelatedType_DoesNotRegenerateSource()
        {
            string source = RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>());

            Compilation comp1 = TestUtils.CreateCompilation(source);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

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
            AssertGeneratedSourceUnchanged(driver.GetRunResult().Results[0], runResult);
        }

        [Fact]
        public void AppendingUnrelatedSource_DoesNotRegenerateSource()
        {
            string source = $$"""
                namespace NS
                {
                    {{RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>())}}
                }
                """;

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = TestUtils.CreateCompilation(new[] { syntaxTree });

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

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
            AssertGeneratedSourceUnchanged(driver.GetRunResult().Results[0], runResult);
        }

        [Fact]
        public void AddingFileWithNewLibraryImport_DoesNotRegenerateOriginalMethod()
        {
            string source = RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>());

            Compilation comp1 = TestUtils.CreateCompilation(source);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(RemoveTestMarkup(CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1)), new CSharpParseOptions(LanguageVersion.Preview)));

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
        public void ReplacingFileWithNewLibraryImport_DoesNotRegenerateStubsInOtherFiles()
        {
            Compilation comp1 = TestUtils.CreateCompilation([ RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>()), RemoveTestMarkup(CodeSnippets.MarshalAsParametersAndModifiers<bool>(UnmanagedType.I1)) ]);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            Compilation comp2 = comp1.ReplaceSyntaxTree(comp1.SyntaxTrees.First(), CSharpSyntaxTree.ParseText(RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<ulong>()), new CSharpParseOptions(LanguageVersion.Preview)));
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
        public void ChangingMarshallingStrategy_RegeneratesStub()
        {
            string stubSource = RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers("CustomType", CodeSnippets.DisableRuntimeMarshalling));

            string customTypeImpl1 = "struct CustomType { System.IntPtr handle; }";

            string customTypeImpl2 = "class CustomType : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid { public CustomType():base(true){} protected override bool ReleaseHandle(){return true;} }";


            Compilation comp1 = TestUtils.CreateCompilation(stubSource);

            SyntaxTree customTypeImpl1Tree = CSharpSyntaxTree.ParseText(customTypeImpl1, new CSharpParseOptions(LanguageVersion.Preview));
            comp1 = comp1.AddSyntaxTrees(customTypeImpl1Tree);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

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
            Assert.NotEqual(GetStubText(driver.GetRunResult().Results[0]), GetStubText(runResult));
            AssertStringOutputs(runResult);
        }

        [Fact]
        public void ChangingMarshallingAttributes_SameStrategy_DoesNotRegenerate()
        {
            string source = RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>());

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = TestUtils.CreateCompilation([syntaxTree]);

            Microsoft.Interop.LibraryImportGenerator generator = new();
            GeneratorDriver driver = TestUtils.CreateDriver(comp1, null, [generator], EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            SyntaxTree newTree = syntaxTree.WithRootAndOptions(
                SyntaxFactory.ParseCompilationUnit(
                    RemoveTestMarkup(CodeSnippets.MarshalAsParametersAndModifiers<int>(System.Runtime.InteropServices.UnmanagedType.I4))),
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
            AssertGeneratedSourceUnchanged(driver.GetRunResult().Results[0], runResult);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ChangingTrivia_DoesNotRegenerateSource(bool requiresMarshalling)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                namespace NS.Inner
                {
                    partial class C
                    {
                        [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                        public static partial void Method({{(requiresMarshalling ? "string" : "int")}} value);
                    }
                }
                """;
            Compilation compilation = TestUtils.CreateCompilation(source);
            GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.LibraryImportGenerator()], EnableIncrementalTrackingDriverOptions);
            driver = driver.RunGenerators(compilation);

            string editedSource = source
                .Replace("namespace NS.Inner", "namespace NS /* namespace */ . Inner")
                .Replace("public static partial", "public /* modifiers */ static\npartial");
            SyntaxTree editedTree = CSharpSyntaxTree.ParseText(editedSource, new CSharpParseOptions(LanguageVersion.Preview));
            Compilation editedCompilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), editedTree);
            GeneratorRunResult result = driver.RunGenerators(editedCompilation).GetRunResult().Results[0];

            Assert.Equal(IncrementalStepRunReason.Unchanged, Assert.Single(Assert.Single(result.TrackedSteps[StepNames.GenerateSingleStub]).Outputs).Reason);
            AssertGeneratedSourceUnchanged(driver.GetRunResult().Results[0], result);
        }

        [Theory]
        [InlineData("CallConvCdecl", "CallConvStdcall")]
        [InlineData("DllImportSearchPath.System32", "DllImportSearchPath.UserDirectories")]
        public void ChangingForwardedAttributeValues_RegeneratesSource(string originalValue, string newValue)
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.Utf16)]
                    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
                    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
                    public static partial void Method(string value);
                }
                """;
            Compilation compilation = TestUtils.CreateCompilation(source);
            GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.LibraryImportGenerator()], EnableIncrementalTrackingDriverOptions);
            driver = driver.RunGenerators(compilation);

            SyntaxTree editedTree = CSharpSyntaxTree.ParseText(source.Replace(originalValue, newValue), new CSharpParseOptions(LanguageVersion.Preview));
            Compilation editedCompilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), editedTree);
            GeneratorRunResult result = driver.RunGenerators(editedCompilation).GetRunResult().Results[0];

            Assert.Equal(IncrementalStepRunReason.Modified, Assert.Single(Assert.Single(result.TrackedSteps[StepNames.GenerateSingleStub]).Outputs).Reason);
            Assert.NotEqual(GetStubText(driver.GetRunResult().Results[0]), GetStubText(result));
            AssertStringOutputs(result);
        }

        [Fact]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task DownlevelAppendingUnrelatedSource_DoesNotRegenerateSource()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method(ref int value);
                }
                """;
            ImmutableArray<MetadataReference> references = await ReferenceAssemblies.NetStandard.NetStandard20.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            Compilation compilation = TestUtils.CreateCompilation(source).WithReferences(references);
            GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.DownlevelLibraryImportGenerator()], EnableIncrementalTrackingDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
            Assert.Empty(diagnostics);
            TestUtils.AssertPostSourceGeneratorCompilation(outputCompilation);

            SyntaxTree editedTree = CSharpSyntaxTree.ParseText(source + "\nstruct Unrelated { }", new CSharpParseOptions(LanguageVersion.Preview));
            Compilation editedCompilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.Single(), editedTree);
            GeneratorRunResult result = driver.RunGenerators(editedCompilation).GetRunResult().Results[0];

            Assert.Equal(IncrementalStepRunReason.Unchanged, Assert.Single(Assert.Single(result.TrackedSteps[StepNames.GenerateSingleStub]).Outputs).Reason);
            AssertGeneratedSourceUnchanged(driver.GetRunResult().Results[0], result);
        }

        [Fact]
        public void ForwarderOutputHasDeterministicFormatting()
        {
            string source = """
                using System.Runtime.InteropServices;
                namespace @namespace;
                partial class @class
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int @event(int @return);
                }
                """;
            Compilation compilation = TestUtils.CreateCompilation(source);
            GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.LibraryImportGenerator()], EnableIncrementalTrackingDriverOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
            Assert.Empty(diagnostics);
            TestUtils.AssertPostSourceGeneratorCompilation(outputCompilation);

            string expected = """
                // <auto-generated/>
                namespace @namespace
                {
                    partial class @class
                    {
                        [global::System.Runtime.InteropServices.DllImportAttribute("DoesNotExist", EntryPoint = "event", ExactSpelling = true)]
                        public static extern partial int @event(int @return);
                    }
                }
                """.ReplaceLineEndings("\r\n") + "\r\n";
            GeneratorRunResult result = driver.GetRunResult().Results[0];
            Assert.Equal(expected, GetStubText(result));
            AssertStringOutputs(result);
        }

        private static string GetStubText(GeneratorRunResult result)
            => Assert.Single(result.GeneratedSources.Where(source => source.HintName == "LibraryImports.g.cs")).SourceText.ToString();

        private static void AssertGeneratedSourceUnchanged(GeneratorRunResult previous, GeneratorRunResult current)
        {
            Assert.Equal(GetStubText(previous), GetStubText(current));
            AssertStringOutputs(current);
        }

        private static void AssertStringOutputs(GeneratorRunResult result)
        {
            Assert.All(result.TrackedSteps[StepNames.GenerateSingleStub], step =>
                Assert.All(step.Outputs, output => Assert.EndsWith("\r\n", Assert.IsType<string>(output.Value), StringComparison.Ordinal)));
        }

        public static IEnumerable<object[]> CompilationObjectLivenessSources()
        {
            // Basic stub
            yield return new[] { RemoveTestMarkup(CodeSnippets.BasicParametersAndModifiers<int>()) };
            // Stub with custom string marshaller
            yield return new[] { RemoveTestMarkup(CodeSnippets.CustomStringMarshallingParametersAndModifiers<string>()) };
        }

        // This test requires precise GC to ensure that we're accurately testing that we aren't
        // keeping the Compilation alive.
        [MemberData(nameof(CompilationObjectLivenessSources))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void GeneratorRun_WithNewCompilation_DoesNotKeepOldCompilationAlive(string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

            Compilation comp1 = TestUtils.CreateCompilation([syntaxTree]);

            var (reference, driver) = RunTwoGeneratorOnTwoIterativeCompilationsAndReturnFirst(comp1);

            GC.Collect();

            Assert.False(reference.IsAlive);
            GC.KeepAlive(driver);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static (WeakReference reference, GeneratorDriver driver) RunTwoGeneratorOnTwoIterativeCompilationsAndReturnFirst(Compilation startingCompilation)
            {
                Compilation comp2 = startingCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct NewType {}", new CSharpParseOptions(LanguageVersion.Preview)));

                Microsoft.Interop.LibraryImportGenerator generator = new();
                GeneratorDriver driver = TestUtils.CreateDriver(comp2, null, [generator], EnableIncrementalTrackingDriverOptions);

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

        private static string RemoveTestMarkup(string sourceWithMarkup)
        {
            TestFileMarkupParser.GetSpans(sourceWithMarkup, out string sourceWithoutMarkup, out ImmutableArray<TextSpan> _);
            return sourceWithoutMarkup;
        }
    }
}
