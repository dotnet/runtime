// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace System.Diagnostics.Tracing.Tests.GenerateTest
{
    [ExcludeFromCodeCoverage]
    internal static class Compiler
    {
        private static readonly CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable);

        private static readonly MetadataReference[] metadataReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(Attribute).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ActivitySource).Assembly.Location)
        }.Concat(AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            try
            {
                return !string.IsNullOrEmpty(x.Location);
            }
            catch (Exception)
            {
                return false;
            }
        }).Select(x => MetadataReference.CreateFromFile(x.Location))).ToArray();

        public static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                metadataReferences,
                compilationOptions);

        public static ImmutableArray<Diagnostic> GetDiagnostics(string code)
        {
            var compiler = CreateCompilation(code);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new EventSourceEventGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(compiler, out _, out var diagnostics);
            if (diagnostics.Length==0)
            {
                return compiler.GetDiagnostics();
            }
            return diagnostics;
        }

        public static GeneratorDriverRunResult CheckGenerated(string code, int willGeneratedFile)
        {
            var compiler = CreateCompilation(code);

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new EventSourceEventGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(compiler, out var outputCompilation, out var diagnostics);

            Assert.True(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.True(outputCompilation.GetDiagnostics().IsEmpty, string.Join("\n", outputCompilation.GetDiagnostics().Select(x => x.ToString()))); // verify the compilation with the added source has no diagnostics

            GeneratorDriverRunResult runResult = driver.GetRunResult();

            Assert.Equal(willGeneratedFile, runResult.GeneratedTrees.Length);
            Assert.True(runResult.Diagnostics.IsEmpty);

            return runResult;
        }

        public static void CheckGeneratedSingle(string code, string baseLineFileName)
        {
            var result = CheckGenerated(code, 1);

            var syntaxTree = Baselines.GetBaselineTree(baseLineFileName);
            GeneratorRunResult generatorResult = result.Results[0];
            Assert.NotNull(syntaxTree);
            Assert.NotNull(generatorResult.GeneratedSources[0].SourceText);
            Assert.True(generatorResult.GeneratedSources[0].SyntaxTree.IsEquivalentTo(syntaxTree),$"Expect:[{syntaxTree}]\nActual:[{generatorResult.GeneratedSources[0].SyntaxTree}]");
        }

        public static void CheckGeneratedMore(string code, List<(Type sourceGenerateType, string hitName, string baseLineFileName)> equals)
        {
            var result = CheckGenerated(code, equals.Count);

            foreach (var item in equals)
            {
                var gen = result.Results.FirstOrDefault(x => x.Generator.GetGeneratorType() == item.sourceGenerateType);

                if (gen.Generator == null)
                {
                    Assert.Fail($"Can't find {item.sourceGenerateType} generated result");
                }

                var baseLine = Baselines.GetBaselineNode(item.baseLineFileName);

                var fi = gen.GeneratedSources.FirstOrDefault(x => x.HintName == item.hitName);

                if (fi.HintName != item.hitName)
                {
                    Assert.Fail($"Can't find {item.sourceGenerateType} hitName {item.hitName} file");
                }

                Assert.True(fi.SourceText.ContentEquals(baseLine));
            }
        }
    }
}
