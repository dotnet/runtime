// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
    public static class JsonSourceGeneratorIncrementalTests
    {
        [Theory]
        [MemberData(nameof(GetCompilationHelperFactories))]
        public static void CompilingTheSameSourceResultsInEqualModels(Func<Compilation> factory)
        {
            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(factory());
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(factory());

            Assert.Equal(result1.ContextGenerationSpecs.Length, result2.ContextGenerationSpecs.Length);

            for (int i = 0; i < result1.ContextGenerationSpecs.Length; i++)
            {
                ContextGenerationSpec ctx1 = result1.ContextGenerationSpecs[i];
                ContextGenerationSpec ctx2 = result2.ContextGenerationSpecs[i];

                Assert.NotSame(ctx1, ctx2);
                AssertStructurallyEqual(ctx1, ctx2);

                Assert.Equal(ctx1, ctx2);
                Assert.Equal(ctx1.GetHashCode(), ctx2.GetHashCode());
            }
        }

        [Fact]
        public static void CompilingEquivalentSourcesResultsInEqualModels()
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

            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source1));
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source2));
            Assert.Empty(result1.Diagnostics);
            Assert.Empty(result2.Diagnostics);

            Assert.Equal(1, result1.ContextGenerationSpecs.Length);
            Assert.Equal(1, result2.ContextGenerationSpecs.Length);

            ContextGenerationSpec ctx1 = result1.ContextGenerationSpecs[0];
            ContextGenerationSpec ctx2 = result2.ContextGenerationSpecs[0];

            Assert.NotSame(ctx1, ctx2);
            AssertStructurallyEqual(ctx1, ctx2);

            Assert.Equal(ctx1, ctx2);
            Assert.Equal(ctx1.GetHashCode(), ctx2.GetHashCode());
        }

        [Fact]
        public static void CompilingDifferentSourcesResultsInUnequalModels()
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

            JsonSourceGeneratorResult result1 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source1));
            JsonSourceGeneratorResult result2 = CompilationHelper.RunJsonSourceGenerator(CompilationHelper.CreateCompilation(source2));
            Assert.Empty(result1.Diagnostics);
            Assert.Empty(result2.Diagnostics);

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
            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(factory());
            WalkObjectGraph(result.ContextGenerationSpecs);

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
            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver();
            Compilation compilation = factory();

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

            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver();
            Compilation compilation = CompilationHelper.CreateCompilation(source1);

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

            GeneratorDriver driver = CompilationHelper.CreateJsonSourceGeneratorDriver();
            Compilation compilation = CompilationHelper.CreateCompilation(source1);

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

        /// <summary>
        /// Asserts for structural equality, returning a path to the mismatching data when not equal.
        /// </summary>
        private static void AssertStructurallyEqual<T>(T expected, T actual)
        {
            CheckAreEqualCore(expected, actual, new());
            static void CheckAreEqualCore(object expected, object actual, Stack<string> path)
            {
                if (expected is null || actual is null)
                {
                    if (expected is not null || actual is not null)
                    {
                        FailNotEqual();
                    }

                    return;
                }

                Type type = expected.GetType();
                if (type != actual.GetType())
                {
                    FailNotEqual();
                    return;
                }

                if (expected is IEnumerable leftCollection)
                {
                    if (actual is not IEnumerable rightCollection)
                    {
                        FailNotEqual();
                        return;
                    }

                    object?[] expectedValues = leftCollection.Cast<object?>().ToArray();
                    object?[] actualValues = rightCollection.Cast<object?>().ToArray();

                    for (int i = 0; i < Math.Max(expectedValues.Length, actualValues.Length); i++)
                    {
                        object? expectedElement = i < expectedValues.Length ? expectedValues[i] : "<end of collection>";
                        object? actualElement = i < actualValues.Length ? actualValues[i] : "<end of collection>";

                        path.Push($"[{i}]");
                        CheckAreEqualCore(expectedElement, actualElement, path);
                        path.Pop();
                    }
                }

                if (type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic, null, returnType: typeof(Type), types: Array.Empty<Type>(), null) != null)
                {
                    // Type is a C# record, run pointwise equality comparison.
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        path.Push("." + property.Name);
                        CheckAreEqualCore(property.GetValue(expected), property.GetValue(actual), path);
                        path.Pop();
                    }

                    return;
                }

                if (!expected.Equals(actual))
                {
                    FailNotEqual();
                }

                void FailNotEqual() => Assert.Fail($"Value not equal in ${string.Join("", path.Reverse())}: expected {expected}, but was {actual}.");
            }
        }
    }
}
