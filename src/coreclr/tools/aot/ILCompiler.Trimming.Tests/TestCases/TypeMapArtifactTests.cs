// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Linker.Tests.TestCasesRunner;
using Xunit;

namespace Mono.Linker.Tests.TestCases;

public class TypeMapArtifactTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompilerProducesBuildArtifact(bool useScanner)
    {
        TrimmedTestCaseResult fixture = TypeMapOutputTests.CreateFixture(new TestRunner(new ObjectFactory()));
        string outputDirectory = Path.Combine(fixture.Sandbox.OutputDirectory.ToString(), "native output");
        Directory.CreateDirectory(outputDirectory);
        string objectPath = Path.Combine(outputDirectory, "test.o");
        string reportPath = Path.ChangeExtension(objectPath, ".typemaps.xml");

        await RunCompiler(generateArtifact: false, expectSuccess: true);
        Assert.Empty(Directory.EnumerateFiles(outputDirectory, "*typemap*.xml", SearchOption.AllDirectories));

        await RunCompiler(generateArtifact: true, expectSuccess: true);
        TypeMapOutputTests.CheckArtifact(reportPath, nativeAot: true);

        byte[] firstReport = File.ReadAllBytes(reportPath);
        File.WriteAllText(reportPath, "stale output");
        await RunCompiler(generateArtifact: true, expectSuccess: true);
        Assert.Equal(firstReport, File.ReadAllBytes(reportPath));

        File.Delete(reportPath);
        await RunCompiler(generateArtifact: true, expectSuccess: true);
        Assert.Equal(firstReport, File.ReadAllBytes(reportPath));

        TypeMapOutputTests.UseUnrepresentableKey(fixture);
        await RunCompiler(generateArtifact: true, expectSuccess: false);
        Assert.False(File.Exists(reportPath));

        async Task RunCompiler(bool generateArtifact, bool expectSuccess)
        {
            string runtimeDirectory = (string)AppContext.GetData("Mono.Linker.Tests.RuntimeBinDirectory")!;
            string compiler = Path.Combine(runtimeDirectory, "ilc", "ilc.dll");
            Assert.True(File.Exists(compiler), $"The built compiler was not found at '{compiler}'.");
            var arguments = new List<string>
            {
                fixture.InputAssemblyPath.ToString(),
                $"-r:{Path.Combine(runtimeDirectory, "aotsdk", "*.dll")}",
                $"-r:{Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")}",
                $"-r:{Path.Combine(fixture.Sandbox.InputDirectory.ToString(), "*.dll")}",
                "--initassembly:System.Private.CoreLib",
                "--initassembly:System.Private.StackTraceMetadata",
                "--initassembly:System.Private.TypeLoader",
                "--initassembly:System.Private.Reflection.Execution",
                "--feature:System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=false",
                "--feature:System.Resources.ResourceManager.AllowCustomResourceTypes=false",
                "--feature:System.Linq.Expressions.CanEmitObjectArrayDelegate=false",
                "--feature:System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported=false",
                "--feature:System.Diagnostics.Debugger.IsSupported=false",
                "--feature:System.Text.Encoding.EnableUnsafeUTF7Encoding=false",
                "--feature:System.Diagnostics.Tracing.EventSource.IsSupported=false",
                "--feature:System.Globalization.Invariant=true",
                "--feature:System.Resources.UseSystemResourceKeys=true",
                "--scanreflection",
                useScanner ? "-O" : "--noscan",
                $"-o:{objectPath}"
            };
            if (generateArtifact)
                arguments.Add("--output-typemaps");
            (int exitCode, string diagnostics) = await TypeMapOutputTests.RunTool(compiler, outputDirectory, arguments);
            if (expectSuccess)
            {
                Assert.True(exitCode == 0, $"Compiler failed with exit code {exitCode}.\n{diagnostics}");
                Assert.True(File.Exists(objectPath), "Compilation succeeded without producing its native object.");
            }
            else
            {
                Assert.True(exitCode != 0, "The compiler reported success despite failing to write the build artifact.");
                Assert.Contains(reportPath, diagnostics, StringComparison.Ordinal);
            }
        }
    }
}
