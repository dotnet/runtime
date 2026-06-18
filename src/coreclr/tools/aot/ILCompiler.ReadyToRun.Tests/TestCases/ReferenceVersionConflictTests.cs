// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.ReadyToRun.Tests.TestCasesRunner;
using Xunit;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests.TestCases;

/// <summary>
/// Tests that crossgen2 warns when the same assembly simple name is passed as a reference
/// more than once with different versions/identity. crossgen2 silently binds to the first
/// such reference, which can produce R2R code that throws MissingMethodException at runtime.
/// </summary>
public class ReferenceVersionConflictTests
{
    private const string LibAssemblyName = "ConflictingLib";

    private readonly ITestOutputHelper _output;

    public ReferenceVersionConflictTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void WarnsWhenReferencesHaveDifferentVersions()
    {
        // Two builds of the same assembly name with different versions (and thus different MVIDs),
        // emitted to separate subdirectories so they coexist on disk.
        var libV1 = new CompiledAssembly
        {
            AssemblyName = LibAssemblyName,
            SourceResourceNames = ["ReferenceConflict/ConflictingLibV1.cs"],
            OutputSubdirectory = "v1",
        };
        var libV2 = new CompiledAssembly
        {
            AssemblyName = LibAssemblyName,
            SourceResourceNames = ["ReferenceConflict/ConflictingLibV2.cs"],
            OutputSubdirectory = "v2",
        };
        var app = new CompiledAssembly
        {
            AssemblyName = "ConflictingApp",
            SourceResourceNames = ["ReferenceConflict/ConflictingApp.cs"],
            References = [libV1],
        };

        // Pass v1 first so it is the binding winner (matching what the app was built against),
        // and v2 second as the dropped, conflicting reference.
        var cgApp = new CrossgenAssembly(app);
        var cgLibV1 = new CrossgenAssembly(libV1) { Kind = Crossgen2InputKind.Reference };
        var cgLibV2 = new CrossgenAssembly(libV2) { Kind = Crossgen2InputKind.Reference };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(WarnsWhenReferencesHaveDifferentVersions),
            [new CrossgenCompilation(app.AssemblyName, [cgApp, cgLibV1, cgLibV2]) { ValidateResult = ValidateWarns }]));

        static void ValidateWarns(R2RCompilationResult result)
        {
            string output = result.StandardOutput + result.StandardError;
            // The conflict is a warning, not an error: crossgen2 still produces an image.
            Assert.True(result.Success, $"crossgen2 should succeed with a warning:\n{output}");
            // The "warning:" prefix is what the SDK's RunReadyToRunCompiler task scans for
            // (case-insensitively) to surface R2R compiler warnings, so assert it explicitly.
            Assert.Contains("warning:", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(LibAssemblyName, output);
            Assert.Contains("1.0.0.0", output);
            Assert.Contains("2.0.0.0", output);
        }
    }

    [Fact]
    public void DoesNotWarnWhenReferencesAreIdentical()
    {
        // Two builds of the same source. Deterministic compilation yields identical MVID and
        // version, so passing both as references (from different paths) must not warn.
        var libA = new CompiledAssembly
        {
            AssemblyName = LibAssemblyName,
            SourceResourceNames = ["ReferenceConflict/ConflictingLibV1.cs"],
            OutputSubdirectory = "a",
        };
        var libB = new CompiledAssembly
        {
            AssemblyName = LibAssemblyName,
            SourceResourceNames = ["ReferenceConflict/ConflictingLibV1.cs"],
            OutputSubdirectory = "b",
        };
        var app = new CompiledAssembly
        {
            AssemblyName = "ConflictingApp",
            SourceResourceNames = ["ReferenceConflict/ConflictingApp.cs"],
            References = [libA],
        };

        var cgApp = new CrossgenAssembly(app);
        var cgLibA = new CrossgenAssembly(libA) { Kind = Crossgen2InputKind.Reference };
        var cgLibB = new CrossgenAssembly(libB) { Kind = Crossgen2InputKind.Reference };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(DoesNotWarnWhenReferencesAreIdentical),
            [new CrossgenCompilation(app.AssemblyName, [cgApp, cgLibA, cgLibB]) { ValidateResult = ValidateNoWarning }]));

        static void ValidateNoWarning(R2RCompilationResult result)
        {
            string output = result.StandardOutput + result.StandardError;
            Assert.True(result.Success, $"crossgen2 should succeed:\n{output}");
            Assert.DoesNotContain("warning:", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
