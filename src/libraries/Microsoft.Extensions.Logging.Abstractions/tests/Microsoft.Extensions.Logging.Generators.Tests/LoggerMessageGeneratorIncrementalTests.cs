// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN4_8_OR_GREATER

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    public class LoggerMessageGeneratorIncrementalTests
    {
        private static readonly GeneratorDriverOptions EnableIncrementalTrackingDriverOptions =
            new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

        [Fact]
        public void AddingNewUnrelatedType_DoesNotRegenerateSource()
        {
            string source = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Hello")]
                    static partial void M(ILogger logger);
                }
                """;

            Compilation comp1 = CompilationHelper.CreateCompilation(source);

            LoggerMessageGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                driverOptions: EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            // Add an unrelated type - use consistent parse options from existing tree
            SyntaxTree existingTree = comp1.SyntaxTrees.First();
            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText("struct Foo {}", (CSharpParseOptions)existingTree.Options));
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            // Verify the tracked steps show the generator didn't re-run
            var trackedSteps = runResult.TrackedSteps;
            Assert.True(trackedSteps.ContainsKey(LoggerMessageGenerator.StepNames.LoggerMessageTransform));
            Assert.Collection(trackedSteps[LoggerMessageGenerator.StepNames.LoggerMessageTransform],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public void AppendingUnrelatedSource_DoesNotRegenerateSource()
        {
            string source = """
                using Microsoft.Extensions.Logging;
                namespace NS
                {
                    partial class C
                    {
                        [LoggerMessage(0, LogLevel.Information, "Hello")]
                        static partial void M(ILogger logger);
                    }
                }
                """;

            Compilation comp1 = CompilationHelper.CreateCompilation(source);
            SyntaxTree originalTree = comp1.SyntaxTrees.First(); // Get the REAL tree in the compilation

            LoggerMessageGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                driverOptions: EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            // Append an unrelated struct to the same file
            SyntaxTree newTree = originalTree.WithRootAndOptions(
                originalTree.GetCompilationUnitRoot().AddMembers(SyntaxFactory.ParseMemberDeclaration("struct Foo {}")!),
                originalTree.Options);

            Compilation comp2 = comp1.ReplaceSyntaxTree(originalTree, newTree);
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            // Verify the generator didn't re-run
            var trackedSteps = runResult.TrackedSteps;
            Assert.True(trackedSteps.ContainsKey(LoggerMessageGenerator.StepNames.LoggerMessageTransform));
            Assert.Collection(trackedSteps[LoggerMessageGenerator.StepNames.LoggerMessageTransform],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public void AddingNewLoggerMessageMethod_DoesNotRegenerateExistingMethod()
        {
            string source1 = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Hello")]
                    static partial void M1(ILogger logger);
                }
                """;

            Compilation comp1 = CompilationHelper.CreateCompilation(source1);

            LoggerMessageGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                driverOptions: EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            // Add a new file with a new LoggerMessage method
            string source2 = """
                using Microsoft.Extensions.Logging;
                partial class D
                {
                    [LoggerMessage(1, LogLevel.Warning, "World")]
                    static partial void M2(ILogger logger);
                }
                """;

            SyntaxTree existingTree = comp1.SyntaxTrees.First();
            Compilation comp2 = comp1.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source2, (CSharpParseOptions)existingTree.Options));
            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            // Verify the original method wasn't regenerated
            var trackedSteps = runResult.TrackedSteps;
            Assert.True(trackedSteps.ContainsKey(LoggerMessageGenerator.StepNames.LoggerMessageTransform));
            Assert.Collection(trackedSteps[LoggerMessageGenerator.StepNames.LoggerMessageTransform],
                step =>
                {
                    // First method should be unchanged
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                },
                step =>
                {
                    // Second method is new
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });
        }

        [Fact]
        public void ChangingLoggerMessageAttribute_RegeneratesMethod()
        {
            string source1 = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Hello")]
                    static partial void M(ILogger logger);
                }
                """;

            Compilation comp1 = CompilationHelper.CreateCompilation(source1);
            SyntaxTree originalTree = comp1.SyntaxTrees.First(); // Get the REAL tree in the compilation

            LoggerMessageGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                driverOptions: EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            // Change the message in the attribute
            string source2 = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Goodbye")]
                    static partial void M(ILogger logger);
                }
                """;

            SyntaxTree newTree = CSharpSyntaxTree.ParseText(source2, (CSharpParseOptions?)originalTree.Options);
            Compilation comp2 = comp1.ReplaceSyntaxTree(originalTree, newTree);

            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            // Verify the method was regenerated
            var trackedSteps = runResult.TrackedSteps;
            Assert.True(trackedSteps.ContainsKey(LoggerMessageGenerator.StepNames.LoggerMessageTransform));
            Assert.Collection(trackedSteps[LoggerMessageGenerator.StepNames.LoggerMessageTransform],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });
        }

        [Fact]
        public void ChangingUnrelatedMethodBody_DoesNotRegenerateLoggerMessage()
        {
            string source1 = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Hello")]
                    static partial void M1(ILogger logger);

                    void UnrelatedMethod() { var x = 1; }
                }
                """;

            Compilation comp1 = CompilationHelper.CreateCompilation(source1);
            SyntaxTree originalTree = comp1.SyntaxTrees.First(); // Get the REAL tree in the compilation

            LoggerMessageGenerator generator = new();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: new[] { generator.AsSourceGenerator() },
                driverOptions: EnableIncrementalTrackingDriverOptions);

            driver = driver.RunGenerators(comp1);

            // Change the unrelated method body
            string source2 = """
                using Microsoft.Extensions.Logging;
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Information, "Hello")]
                    static partial void M1(ILogger logger);

                    void UnrelatedMethod() { var x = 2; }
                }
                """;

            SyntaxTree newTree = CSharpSyntaxTree.ParseText(source2, (CSharpParseOptions?)originalTree.Options);
            Compilation comp2 = comp1.ReplaceSyntaxTree(originalTree, newTree);

            GeneratorDriver driver2 = driver.RunGenerators(comp2);
            GeneratorRunResult runResult = driver2.GetRunResult().Results[0];

            // Verify the logger message wasn't regenerated
            var trackedSteps = runResult.TrackedSteps;
            Assert.True(trackedSteps.ContainsKey(LoggerMessageGenerator.StepNames.LoggerMessageTransform));
            Assert.Collection(trackedSteps[LoggerMessageGenerator.StepNames.LoggerMessageTransform],
                step =>
                {
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }
    }
}

#endif
