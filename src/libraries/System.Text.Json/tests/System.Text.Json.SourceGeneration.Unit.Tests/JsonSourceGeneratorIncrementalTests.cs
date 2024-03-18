// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using SourceGenerators.Tests;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/92467")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotX86Process))] // https://github.com/dotnet/runtime/issues/71962
    public static class JsonSourceGeneratorIncrementalTests
    {
        [Theory]
        [MemberData(nameof(GetCompilationHelperFactories))]
        public static void CompilingTheSameSourceResultsInEqualModels(Func<Compilation> factory)
        {
            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(factory(), disableDiagnosticValidation: true);
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(factory(), disableDiagnosticValidation: true);

            Assert.Equal(result1.ContextGenerationSpecs.Length, result2.ContextGenerationSpecs.Length);

            for (int i = 0; i < result1.ContextGenerationSpecs.Length; i++)
            {
                ContextGenerationSpec ctx1 = result1.ContextGenerationSpecs[i];
                ContextGenerationSpec ctx2 = result2.ContextGenerationSpecs[i];

                Assert.NotSame(ctx1, ctx2);
                GeneratorTestHelpers.AssertStructurallyEqual(ctx1, ctx2);

                Assert.Equal(ctx1, ctx2);
                Assert.Equal(ctx1.GetHashCode(), ctx2.GetHashCode());
            }
        }

        [Fact]
        public static void CompilingEquivalentSourcesResultsInEqualModels()
        {
            string source1 = """
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public int MyProperty { get; set; } = 42;
                    }
                }
                """;

            string source2 = """
                using System;
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    // Same as above but with different implementation
                    public class MyPoco
                    {
                        public int MyProperty
                        {
                            get => -1;
                            set => throw new NotSupportedException();
                        }
                    }

                    // Changing location should produce identical SG model when no diagnostics are emitted.
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                }
                """;

            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source1));
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source2));

            Assert.Equal(1, result1.ContextGenerationSpecs.Length);
            Assert.Equal(1, result2.ContextGenerationSpecs.Length);

            ContextGenerationSpec ctx1 = result1.ContextGenerationSpecs[0];
            ContextGenerationSpec ctx2 = result2.ContextGenerationSpecs[0];

            Assert.NotSame(ctx1, ctx2);
            GeneratorTestHelpers.AssertStructurallyEqual(ctx1, ctx2);

            Assert.Equal(ctx1, ctx2);
            Assert.Equal(ctx1.GetHashCode(), ctx2.GetHashCode());
        }

        [Fact]
        public static void CompilingDifferentSourcesResultsInUnequalModels()
        {
            string source1 = """
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public int MyProperty { get; set; } = 42;
                    }
                }
                """;

            string source2 = """
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public int MyProperty { get; } = 42; // same, but missing a getter
                    }
                }
                """;

            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source1));
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source2));

            Assert.Equal(1, result1.ContextGenerationSpecs.Length);
            Assert.Equal(1, result2.ContextGenerationSpecs.Length);

            ContextGenerationSpec ctx1 = result1.ContextGenerationSpecs[0];
            ContextGenerationSpec ctx2 = result2.ContextGenerationSpecs[0];
            Assert.NotEqual(ctx1, ctx2);
        }

        [Theory]
        [MemberData(nameof(GetCompilationHelperFactories))]
        public static void SourceGenModelDoesNotEncapsulateSymbolsOrCompilationData(Func<Compilation> factory)
        {
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(factory(), disableDiagnosticValidation: true);
            WalkObjectGraph(result.ContextGenerationSpecs);
            WalkObjectGraph(result.Diagnostics);

            static void WalkObjectGraph(object obj)
            {
                var visited = new HashSet<object>();
                Visit(obj);

                void Visit(object? node)
                {
                    if (node is null || !visited.Add(node))
                    {
                        return;
                    }

                    Assert.False(node is Compilation or ISymbol);

                    Type type = node.GetType();
                    if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                    {
                        return;
                    }

                    if (node is IEnumerable collection and not string)
                    {
                        foreach (object? element in collection)
                        {
                            Visit(element);
                        }

                        return;
                    }

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        object? fieldValue = field.GetValue(node);
                        Visit(fieldValue);
                    }
                }
            }
        }

#if ROSLYN4_4_OR_GREATER
        [Theory]
        [MemberData(nameof(GetCompilationHelperFactories))]
        public static void IncrementalGenerator_SameInput_DoesNotRegenerate(Func<Compilation> factory)
        {
            Compilation compilation = factory();
            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];

            IncrementalGeneratorRunStep[] runSteps = GetSourceGenRunStep(runResult);
            if (runSteps != null)
            {
                Assert.Collection(runSteps,
                    step =>
                    {
                        Assert.Collection(step.Inputs,
                            source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                        Assert.Collection(step.Outputs,
                            output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                    });
            }

            // run the same compilation through again, and confirm the output wasn't called
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            IncrementalGeneratorRunStep[] runSteps2 = GetSourceGenRunStep(runResult);

            if (runSteps != null)
            {
                Assert.Collection(runSteps2,
                    step =>
                    {
                        Assert.Collection(step.Inputs,
                            source => Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason));
                        Assert.Collection(step.Outputs,
                            output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
                    });
            }
            else
            {
                Assert.Null(runSteps2);
            }

            static IncrementalGeneratorRunStep[]? GetSourceGenRunStep(GeneratorRunResult runResult)
            {
                if (!runResult.TrackedSteps.TryGetValue(JsonSourceGenerator.SourceGenerationSpecTrackingName, out var runSteps))
                {
                    return null;
                }

                return runSteps.ToArray();
            }
        }

        [Fact]
        public static void IncrementalGenerator_EquivalentSources_DoesNotRegenerate()
        {
            string source1 = """
                using System;
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public string MyProperty { get; set; } = 42;
                    }
                }
                """;

            string source2 = """
                using System;
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    // Same as above but with different implementation
                    public class MyPoco
                    {
                        public string MyProperty
                        {
                            get => -1;
                            set => throw new NotSupportedException();
                        }
                    }

                    // Changing location should produce identical SG model when no diagnostics are emitted.
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source1);
            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps[JsonSourceGenerator.SourceGenerationSpecTrackingName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            // Update the syntax tree and re-run the compilation
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree: CompilationHelper.ParseSource(source2));
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps[JsonSourceGenerator.SourceGenerationSpecTrackingName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
                });
        }

        [Fact]
        public static void IncrementalGenerator_DifferentSources_Regenerates()
        {
            string source1 = """
                using System;
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public string MyProperty { get; set; } = 42;
                    }
                }
                """;

            string source2 = """
                using System;
                using System.Text.Json.Serialization;
                
                namespace Test
                {
                    [JsonSerializable(typeof(MyPoco))]
                    public partial class JsonContext : JsonSerializerContext { }
                
                    public class MyPoco
                    {
                        public string MyProperty { get; } = 42; // same, but missing a getter
                    }
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source1);
            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver(compilation);

            driver = driver.RunGenerators(compilation);
            GeneratorRunResult runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps[JsonSourceGenerator.SourceGenerationSpecTrackingName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.New, output.Reason));
                });

            // Update the syntax tree and re-run the compilation
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree: CompilationHelper.ParseSource(source2));
            driver = driver.RunGenerators(compilation);
            runResult = driver.GetRunResult().Results[0];
            Assert.Collection(runResult.TrackedSteps[JsonSourceGenerator.SourceGenerationSpecTrackingName],
                step =>
                {
                    Assert.Collection(step.Inputs,
                        source => Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason));
                    Assert.Collection(step.Outputs,
                        output => Assert.Equal(IncrementalStepRunReason.Modified, output.Reason));
                });
        }
#endif
        public static IEnumerable<object[]> GetCompilationHelperFactories()
        {
            return typeof(CompilationHelper).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.ReturnType == typeof(Compilation) && m.GetParameters().Length == 0)
                .Select(m => new object[] { Delegate.CreateDelegate(typeof(Func<Compilation>), m) });
        }
    }
}
