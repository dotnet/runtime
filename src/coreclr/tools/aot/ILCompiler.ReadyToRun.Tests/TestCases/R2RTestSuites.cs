// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using ILCompiler.ReadyToRun.Tests.TestCasesRunner;
using ILCompiler.Reflection.ReadyToRun;
using Internal.ReadyToRunConstants;
using Internal.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests.TestCases;

/// <summary>
/// xUnit test suites for R2R cross-module resolution tests.
/// Each test method builds assemblies with Roslyn, crossgen2's them, and validates the R2R output.
/// </summary>
public class R2RTestSuites
{
    private static readonly KeyValuePair<string, string> RuntimeAsyncFeature = new("runtime-async", "on");
    private readonly ITestOutputHelper _output;

    public R2RTestSuites(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BasicCrossModuleInlining()
    {
        var inlineableLib = new CompiledAssembly
        {
            AssemblyName = "InlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLib.cs"],
        };
        var basicCrossModuleInlining = new CompiledAssembly
        {
            AssemblyName = "BasicCrossModuleInlining",
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [inlineableLib]
        };

        var cgInlineableLib = new CrossgenAssembly(inlineableLib){ Kind = Crossgen2InputKind.Reference, Options = [Crossgen2AssemblyOption.CrossModuleOptimization] };
        var cgBasicCrossModuleInlining = new CrossgenAssembly(basicCrossModuleInlining);

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(BasicCrossModuleInlining),
            [new CrossgenCompilation(basicCrossModuleInlining.AssemblyName, [cgInlineableLib, cgBasicCrossModuleInlining]) { Validate = Validate }])
        );

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InlineableLib", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetString", "GetString", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);
        }
    }

    [Fact]
    public void WasmWebcilModule()
    {
        var wasmWebcilModule = new CompiledAssembly
        {
            AssemblyName = nameof(WasmWebcilModule),
            SourceResourceNames = ["Webcil/WasmWebcilModule.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(WasmWebcilModule),
            [
                new(nameof(WasmWebcilModule), [new CrossgenAssembly(wasmWebcilModule)])
                {
                    OutputFileExtension = ".wasm",
                    AdditionalArgs =
                    {
                        "--targetarch",
                        "wasm",
                        "--targetos",
                        "browser",
                    },
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            var webcilReader = Assert.IsType<WebcilImageReader>(reader.CompositeReader);
            Assert.True(webcilReader.IsWasmWrapped);
            Assert.Equal(WasmMachine.Wasm32, reader.Machine);
            Assert.True(R2RAssert.GetAllMethods(reader).Exists(method =>
                method.SignatureString.Contains("AddIntegers", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void RuntimeFunctionsSectionSizeExcludesSentinel()
    {
        var lib = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeFunctionsSectionSizeExcludesSentinel),
            SourceResourceNames = ["ThumbBit/HotColdSplitting.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeFunctionsSectionSizeExcludesSentinel),
            [
                new(nameof(RuntimeFunctionsSectionSizeExcludesSentinel), [new CrossgenAssembly(lib)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            Assert.True(reader.ReadyToRunHeader.Sections.TryGetValue(
                ReadyToRunSectionType.RuntimeFunctions, out ReadyToRunSection section));

            // The header entry records the runtime-functions table size *excluding* the trailing
            // 0xffffffff sentinel word. Each entry is 12 bytes on x64 and 8 bytes on other targets.
            int entrySize = reader.Machine == Machine.Amd64 ? 12 : 8;
            Assert.True(section.Size > 0, "RuntimeFunctions section should not be empty");
            Assert.True(
                section.Size % entrySize == 0,
                $"RuntimeFunctions section size {section.Size} is not a multiple of entry size {entrySize} (machine: {reader.Machine}); remainder {section.Size % entrySize} suggests the trailing sentinel was included.");
        }
    }

    [Fact]
    public void ArmThumbBitRelocationTargets()
    {
        var inlineableLib = new CompiledAssembly
        {
            AssemblyName = "InlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLib.cs"],
        };
        var exceptionHandling = new CompiledAssembly
        {
            AssemblyName = nameof(ArmThumbBitRelocationTargets),
            SourceResourceNames = ["CrossModuleInlining/ExceptionHandling.cs"],
            References = [inlineableLib],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(ArmThumbBitRelocationTargets),
            [
                new(nameof(ArmThumbBitRelocationTargets),
                [
                    new CrossgenAssembly(exceptionHandling),
                    new CrossgenAssembly(inlineableLib) { Kind = Crossgen2InputKind.Reference },
                ])
                {
                    Options = [Crossgen2Option.TargetArchArm],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasExpectedArmThumbBitTargets(reader, out diag), diag);
        }
    }

    [Fact]
    public void ArmThumbBitHotColdRuntimeFunctions()
    {
        var hotColdSplitting = new CompiledAssembly
        {
            AssemblyName = nameof(ArmThumbBitHotColdRuntimeFunctions),
            SourceResourceNames = ["ThumbBit/HotColdSplitting.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(ArmThumbBitHotColdRuntimeFunctions),
            [
                new(nameof(ArmThumbBitHotColdRuntimeFunctions), [new CrossgenAssembly(hotColdSplitting)])
                {
                    Options =
                    [
                        Crossgen2Option.TargetArchArm,
                        Crossgen2Option.Optimize,
                        Crossgen2Option.HotColdSplitting,
                    ],
                    AdditionalArgs =
                    [
                        "--codegenopt",
                        "JitStressProcedureSplitting=1",
                    ],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasExpectedArmHotColdRuntimeFunctionTargets(reader, out diag), diag);
        }
    }

    [Fact]
    public void TransitiveReferences()
    {
        var externalLib = new CompiledAssembly()
        {
            AssemblyName = "ExternalLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/ExternalLib.cs"],
        };
        var inlineableLibTransitive = new CompiledAssembly()
        {
            AssemblyName = "InlineableLibTransitive",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLibTransitive.cs"],
            References = [externalLib]
        };
        var transitiveReferences = new CompiledAssembly()
        {
            AssemblyName = "TransitiveReferences",
            SourceResourceNames = ["CrossModuleInlining/TransitiveReferences.cs"],
            References = [inlineableLibTransitive, externalLib]
        };
        new R2RTestRunner(_output).Run(new R2RTestCase(nameof(TransitiveReferences),
            [
                new("TransitiveReferences", [
                        new CrossgenAssembly(transitiveReferences),
                        new CrossgenAssembly(externalLib) { Kind = Crossgen2InputKind.Reference },
                        new CrossgenAssembly(inlineableLibTransitive)
                        {
                            Kind = Crossgen2InputKind.Reference,
                            Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                        },
                ])
                {
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "InlineableLibTransitive", out diag), diag);
                        Assert.True(R2RAssert.HasManifestRef(reader, "ExternalLib", out diag), diag);
                        Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestTransitiveValue", "GetExternalValue", out diag), diag);
                    },
                },
            ]));
    }

    [Fact]
    public void AsyncCrossModuleInlining()
    {
        var asyncInlineableLib = new CompiledAssembly
        {
            AssemblyName = "AsyncInlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/AsyncInlineableLib.cs"],
        };
        var asyncCrossModuleInlining = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleInlining),
            SourceResourceNames = ["CrossModuleInlining/AsyncMethods.cs"],
            References = [asyncInlineableLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleInlining),
            [
                new(nameof(AsyncCrossModuleInlining),
                [
                    new CrossgenAssembly(asyncCrossModuleInlining),
                    new CrossgenAssembly(asyncInlineableLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncInlineableLib", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestAsyncInline", "GetValueAsync", out diag), diag);
        }
    }

    [Fact]
    public void CompositeBasic()
    {
        var compositeLib = new CompiledAssembly
        {
            AssemblyName = "CompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CompositeLib.cs"],
        };
        var compositeBasic = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeBasic),
            SourceResourceNames = ["CrossModuleInlining/CompositeBasic.cs"],
            References = [compositeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeBasic),
            [
                new(nameof(CompositeBasic),
                [
                    new CrossgenAssembly(compositeLib),
                    new CrossgenAssembly(compositeBasic),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "CompositeLib", out diag), diag);
        }
    }

    [Fact]
    public void CompositeManifestAssemblyMvidsAreAligned()
    {
        var compositeLib = new CompiledAssembly
        {
            AssemblyName = "MvidCompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CompositeLib.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeManifestAssemblyMvidsAreAligned),
            SourceResourceNames = ["CrossModuleInlining/CompositeBasic.cs"],
            References = [compositeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeManifestAssemblyMvidsAreAligned),
            [
                new(nameof(CompositeManifestAssemblyMvidsAreAligned),
                [
                    new CrossgenAssembly(compositeLib),
                    new CrossgenAssembly(compositeMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.ManifestAssemblyMvidsTableIsAligned(reader, out diag), diag);
        }
    }

    public static bool IsWindows => System.OperatingSystem.IsWindows();

    [ConditionalFact(nameof(IsWindows))]
    public void CompositeManifestAssemblyMvidsArePaddedWhenPdbPresent()
    {
        var compositeLib = new CompiledAssembly
        {
            AssemblyName = "MvidCompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CompositeLib.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeManifestAssemblyMvidsArePaddedWhenPdbPresent),
            SourceResourceNames = ["CrossModuleInlining/CompositeBasic.cs"],
            References = [compositeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeManifestAssemblyMvidsArePaddedWhenPdbPresent),
            [
                new(nameof(CompositeManifestAssemblyMvidsArePaddedWhenPdbPresent),
                [
                    new CrossgenAssembly(compositeLib),
                    new CrossgenAssembly(compositeMain),
                ])
                {
                    // --pdb creates an odd-sized debug directory section that exposes the MVID table
                    // misalignment bug. The odd size derives from the composite output name length, so
                    // renaming this test can shift the table back onto a 4-byte boundary and silently
                    // neutralize the regression coverage; verify it still misaligns without the fix if
                    // the name changes.
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    AdditionalArgs = ["--pdb"],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.ManifestAssemblyMvidsTableIsAligned(reader, out diag), diag);
        }
    }

    [Fact]
    public void RuntimeAsyncMethodEmission()
    {
        var runtimeAsyncMethodEmission = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncMethodEmission),
            SourceResourceNames =
            [
                "RuntimeAsync/BasicAsyncEmission.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncMethodEmission),
            [
                new(nameof(RuntimeAsyncMethodEmission), [new CrossgenAssembly(runtimeAsyncMethodEmission)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasAsyncVariant(reader, "SimpleAsyncMethod", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AsyncVoidReturn", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "ValueTaskMethod", out diag), diag);
        }
    }

    /// <summary>
    /// #129813 / PR #129884: crossgen2 --strip-il-bodies must preserve the IL of non-async
    /// Task/ValueTask-returning methods, which is needed to compile the runtime-async variant.
    /// It must also strip a non-async Task-returning method whose async variant has already been
    /// compiled, since the IL is no longer needed at runtime.
    /// </summary>
    [Fact]
    public void RuntimeAsyncStripILBodiesPreservesTaskReturningIL()
    {
        var stripILBodies = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncStripILBodiesPreservesTaskReturningIL),
            SourceResourceNames =
            [
                "RuntimeAsync/StripILBodies.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncStripILBodiesPreservesTaskReturningIL),
            [
                new(nameof(RuntimeAsyncStripILBodiesPreservesTaskReturningIL), [new CrossgenAssembly(stripILBodies)])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize, Crossgen2Option.StripILBodies],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;

            string componentFile = Path.Combine(
                Path.GetDirectoryName(reader.Filename)!,
                nameof(RuntimeAsyncStripILBodiesPreservesTaskReturningIL) + ".dll");

            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "StripILBodies", "SyncTaskOfTForwarder", out diag), diag);
            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "StripILBodies", "SyncValueTaskOfTForwarder", out diag), diag);
            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "StripILBodies", "SyncTaskForwarder", out diag), diag);
            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "StripILBodies", "SyncValueTaskForwarder", out diag), diag);

            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "StripILBodies", "GenericIdentity", out diag), diag);
            Assert.True(R2RAssert.MethodILIsPresent(componentFile, "GenericHolder`1", "MethodOnGenericType", out diag), diag);

            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "PlainStrippableMethod", out diag), diag);
            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "ComputeTag", out diag), diag);
            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "Root", out diag), diag);

            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "AsyncTaskMethod", out diag), diag);
            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "AsyncValueTaskMethod", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AsyncTaskMethod", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AsyncValueTaskMethod", out diag), diag);

            Assert.True(R2RAssert.HasAsyncVariant(reader, "SyncTaskWithCompiledAsyncVariant", out diag), diag);
            Assert.True(R2RAssert.MethodILIsStripped(componentFile, "StripILBodies", "SyncTaskWithCompiledAsyncVariant", out diag), diag);
        }
    }

    /// <summary>
    /// PR #123643: Async methods capturing GC refs across await points
    /// produce ContinuationLayout fixups encoding the GC ref map.
    /// PR #124203: Resumption stubs for methods with suspension points.
    /// </summary>
    [Fact]
    public void RuntimeAsyncContinuationLayout()
    {
        var runtimeAsyncContinuationLayout = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncContinuationLayout),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncWithContinuation.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncContinuationLayout),
            [
                new(nameof(RuntimeAsyncContinuationLayout), [new CrossgenAssembly(runtimeAsyncContinuationLayout)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureObjectAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureMultipleRefsAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasContinuationLayout(reader, "CaptureObjectAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasContinuationLayout(reader, "CaptureMultipleRefsAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStubFixup(reader, "CaptureObjectAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.AsyncMethodsWithResumptionStubsAreAdjacent(reader, out diag), diag);
        }
    }

    /// <summary>
    /// PR #125420: [ASYNC] variant generation for devirtualizable async call patterns
    /// (sealed class and interface dispatch through AsyncAwareVirtualMethodResolutionAlgorithm).
    /// </summary>
    [Fact]
    public void RuntimeAsyncDevirtualize()
    {
        var runtimeAsyncDevirtualize = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncDevirtualize),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncDevirtualize.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncDevirtualize),
            [
                new(nameof(RuntimeAsyncDevirtualize), [new CrossgenAssembly(runtimeAsyncDevirtualize)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasAsyncVariant(reader, "OpenImpl.GetValueAsync(", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "SealedImpl.GetValueAsync(", out diag), diag);
        }
    }

    /// <summary>
    /// PR #124203: Async methods without yield points may omit resumption stubs.
    /// Validates that no-yield async methods still produce [ASYNC] variants.
    /// </summary>
    [Fact]
    public void RuntimeAsyncNoYield()
    {
        var runtimeAsyncNoYield = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncNoYield),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncNoYield.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncNoYield),
            [
                new(nameof(RuntimeAsyncNoYield), [new CrossgenAssembly(runtimeAsyncNoYield)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AsyncButNoAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AsyncWithConditionalAwait", out diag), diag);
        }
    }

    /// <summary>
    /// Validates that ResumptionStubEntryPoint fixups are deduplicated for a method:
    /// even with multiple suspension points and forced compilation retries (via
    /// --determinism-stress), each compiled method should have exactly one
    /// ResumptionStubEntryPoint fixup.
    /// </summary>
    [Fact]
    public void RuntimeAsyncResumptionStubFixupDedup()
    {
        var asm = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncResumptionStubFixupDedup),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncMultipleSuspensionPoints.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncResumptionStubFixupDedup),
            [
                new(nameof(RuntimeAsyncResumptionStubFixupDedup), [new CrossgenAssembly(asm)])
                {
                    // Force each method to be compiled multiple times so that
                    // any non-deduplicated fixup additions become observable.
                    AdditionalArgs = { "--determinism-stress=2" },
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasAsyncVariant(reader, ".MultipleAwaits(", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, ".MultipleAwaitsWithRefs(", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStubFixup(reader, ".MultipleAwaits(", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStubFixup(reader, ".MultipleAwaitsWithRefs(", out diag), diag);
            Assert.True(R2RAssert.HasFixupKindCountOnMethod(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, ".MultipleAwaits(", 1, out diag), diag);
            Assert.True(R2RAssert.HasFixupKindCountOnMethod(reader, ReadyToRunFixupKind.ResumptionStubEntryPoint, ".MultipleAwaitsWithRefs(", 1, out diag), diag);
            Assert.True(R2RAssert.AsyncMethodsWithResumptionStubsAreAdjacent(reader, out diag), diag);
        }
    }

    /// <summary>
    /// PR #121679: MutableModule async references + cross-module inlining
    /// of runtime-async methods with cross-module dependency.
    /// </summary>
    [Fact]
    public void RuntimeAsyncCrossModule()
    {
        var asyncDepLib = new CompiledAssembly
        {
            AssemblyName = "AsyncDepLib",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AsyncDepLib.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var runtimeAsyncCrossModule = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncCrossModule),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncCrossModule.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncDepLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncCrossModule),
            [
                new(nameof(RuntimeAsyncCrossModule),
                [
                    new CrossgenAssembly(runtimeAsyncCrossModule),
                    new CrossgenAssembly(asyncDepLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncDepLib", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCrossModuleAsync", out diag), diag);
        }
    }

    // =====================================================================
    // Tier 1: Critical intersection tests
    // =====================================================================

    /// <summary>
    /// Composite mode with sync cross-module inlining.
    /// Validates that inlining info (CrossModuleInlineInfo or InliningInfo2) is
    /// properly populated (CompositeBasic only validates ManifestRef).
    /// </summary>
    [Fact]
    public void CompositeCrossModuleInlining()
    {
        var inlineableLib = new CompiledAssembly
        {
            AssemblyName = "InlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLib.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = "CompositeCrossModuleInlining",
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [inlineableLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeCrossModuleInlining),
            [
                new(nameof(CompositeCrossModuleInlining),
                [
                    new CrossgenAssembly(inlineableLib),
                    new CrossgenAssembly(compositeMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InlineableLib", out diag), diag);
            Assert.True(R2RAssert.HasInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);
        }
    }

    /// <summary>
    /// Negative test: a composite image whose only inputs are the inlinee and the inliner
    /// assemblies does NOT produce a CrossModuleInlineInfo section. CrossModuleInlineInfo only records
    /// inlining where the inlinee module is outside the compiled image's version bubble
    /// </summary>
    [Fact]
    public void CompositeDoesNotProduceCrossModuleInliningInfo()
    {
        var inlineableLib = new CompiledAssembly
        {
            AssemblyName = "InlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLib.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [inlineableLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
            [
                new(nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
                [
                    new CrossgenAssembly(inlineableLib),
                    new CrossgenAssembly(compositeMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            // Inlining still happens in composite mode — recorded in InliningInfo2 — confirming
            // the assertion below is meaningful (we are not just looking at a no-op compilation).
            Assert.True(R2RAssert.HasInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);

            // But no CrossModuleInlineInfo section/entries should be present in composite output.
            Assert.False(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);
        }
    }

    /// <summary>
    /// Positive complement to <see cref="CompositeDoesNotProduceCrossModuleInliningInfo"/>:
    /// composite mode produces a CrossModuleInlineInfo section when an inlineable method
    /// comes from an assembly outside the version bubble (passed as a Reference with
    /// --opt-cross-module).
    /// </summary>
    [Fact]
    public void CompositeProducesCrossModuleInliningInfoForExternalReference()
    {
        var inlineableLib = new CompiledAssembly
        {
            AssemblyName = "InlineableLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLib.cs"],
        };
        var compositeLib = new CompiledAssembly
        {
            AssemblyName = "CompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CompositeLib.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeProducesCrossModuleInliningInfoForExternalReference),
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [inlineableLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeProducesCrossModuleInliningInfoForExternalReference),
            [
                new(nameof(CompositeProducesCrossModuleInliningInfoForExternalReference),
                [
                    new CrossgenAssembly(inlineableLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                    new CrossgenAssembly(compositeLib),
                    new CrossgenAssembly(compositeMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InlineableLib", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);
        }
    }

    /// <summary>
    /// Composite mode with runtime-async methods in both assemblies.
    /// Validates async variants exist in composite output.
    /// </summary>
    [Fact]
    public void CompositeAsync()
    {
        var asyncCompositeLib = new CompiledAssembly
        {
            AssemblyName = "AsyncCompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/AsyncCompositeLib.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var compositeAsyncMain = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncMain",
            SourceResourceNames = ["CrossModuleInlining/CompositeAsync.cs"],
            Features = { RuntimeAsyncFeature },
            References = [asyncCompositeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsync),
            [
                new(nameof(CompositeAsync),
                [
                    new CrossgenAssembly(asyncCompositeLib),
                    new CrossgenAssembly(compositeAsyncMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncCompositeLib", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCompositeAsync", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GetValueAsync", out diag), diag);
        }
    }

    /// <summary>
    /// Composite + runtime-async + intra-bubble inlining matrix test.
    /// Verifies that, in composite mode, awaitless async candidates ARE inlined into
    /// their callers.
    /// </summary>
    [Fact]
    public void CompositeAsyncInliningMatrix()
    {
        var asyncInlineCandidatesLib = new CompiledAssembly
        {
            AssemblyName = "AsyncInlineCandidatesLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/AsyncInlineCandidatesLib.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var asyncInlineCallers = new CompiledAssembly
        {
            AssemblyName = "AsyncInlineCallers",
            SourceResourceNames = ["CrossModuleInlining/AsyncInlineCallers.cs"],
            Features = { RuntimeAsyncFeature },
            References = [asyncInlineCandidatesLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncInliningMatrix),
            [
                new(nameof(CompositeAsyncInliningMatrix),
                [
                    new CrossgenAssembly(asyncInlineCandidatesLib),
                    new CrossgenAssembly(asyncInlineCallers),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncInlineCandidatesLib", out diag), diag);

            // Awaitless async candidates: should be inlined into their callers.
            Assert.True(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskNoAwait", "ReturnTaskNoAwait", out diag), diag);
            Assert.True(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskPrimitiveNoAwait", "ReturnTaskPrimitiveNoAwait", out diag), diag);
            Assert.True(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskClassNoAwait", "ReturnTaskClassNoAwait", out diag), diag);

            // Async candidates that contain a real await: cannot be inlined by the JIT.
            Assert.False(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskWithAwait", "ReturnTaskWithAwait", out diag), diag);
            Assert.False(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskPrimitiveWithAwait", "ReturnTaskPrimitiveWithAwait", out diag), diag);
            Assert.False(R2RAssert.HasInlinedMethod(reader, "CallReturnTaskClassWithAwait", "ReturnTaskClassWithAwait", out diag), diag);
        }
    }

    /// <summary>
    /// Validate that async thunks with generic owning types are correctly emitted in composite mode.
    /// Async thunks (and all "faux" method IL stubs) strip the instantiation away when constructing a MethodWithToken.
    /// This is fine for the Method instantiation, but the Type instantiation needs to be tracked properly.
    /// https://github.com/dotnet/runtime/pull/126904 added support for ensuring the OwningType signature modifier is emitted
    /// for these methods.
    /// </summary>
    [Fact]
    public void CompositeAsyncGenericTypes()
    {
        var asyncGenericTypeLib = new CompiledAssembly
        {
            AssemblyName = "AsyncGenericTypeLib",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AsyncGenericTypeLib.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var compositeAsyncGenericTypesMain = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncGenericTypesMain",
            SourceResourceNames =
            [
                "RuntimeAsync/CompositeAsyncGenericTypesMain.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncGenericTypeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncGenericTypes),
            [
                new(nameof(CompositeAsyncGenericTypes),
                [
                    new CrossgenAssembly(asyncGenericTypeLib),
                    new CrossgenAssembly(compositeAsyncGenericTypesMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            // Async thunks for the consumer's instantiated callers.
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGenericContainerInt", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGenericContainerString", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGenericMethodOnGenericTypeIntLong", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGenericMethodOnGenericTypeStringObject", out diag), diag);

            // Async thunks for the library's generic-type methods, asserted with their
            // generic-arg instantiations to ensure we aren't matching only the open
            // (unspecialized) method signature. Reference-type instantiations are shared
            // through the canonical (__Canon) form, so the string consumer's calls also
            // produce the __Canon variant rather than a separate <String> entry.
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GenericContainer`1<int>.GetValueAsync", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GenericContainer`1<__Canon>.GetValueAsync", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GenericContainer`1<int>.CombineAsync<long>", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GenericContainer`1<__Canon>.CombineAsync<__Canon>", out diag), diag);
        }
    }

    /// <summary>
    /// Non-composite runtime-async + cross-module inlining where the inlinee
    /// captures GC refs across await points. Validates that ContinuationLayout
    /// fixups correctly reference cross-module types via MutableModule tokens.
    /// </summary>
    [Fact]
    public void AsyncCrossModuleContinuation()
    {
        var asyncDepLibCont = new CompiledAssembly
        {
            AssemblyName = "AsyncDepLibContinuation",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AsyncDepLibContinuation.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var asyncCrossModuleCont = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleContinuation),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncCrossModuleContinuation.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncDepLibCont]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleContinuation),
            [
                new(nameof(AsyncCrossModuleContinuation),
                [
                    new CrossgenAssembly(asyncCrossModuleCont),
                    new CrossgenAssembly(asyncDepLibCont)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncDepLibContinuation", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureRef", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureArray", out diag), diag);
        }
    }

    /// <summary>
    /// Two-step compilation: composite A+B, then non-composite C referencing A.
    /// Exercises the multi-compilation model.
    /// </summary>
    [Fact]
    public void MultiStepCompositeAndNonComposite()
    {
        var libA = new CompiledAssembly
        {
            AssemblyName = "MultiStepLibA",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/MultiStepLibA.cs"],
        };
        var libB = new CompiledAssembly
        {
            AssemblyName = "MultiStepLibB",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/MultiStepLibB.cs"],
            References = [libA]
        };
        var consumer = new CompiledAssembly
        {
            AssemblyName = "MultiStepConsumer",
            SourceResourceNames = ["CrossModuleInlining/MultiStepConsumer.cs"],
            References = [libA]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(MultiStepCompositeAndNonComposite),
            [
                new("CompositeStep",
                [
                    new CrossgenAssembly(libA),
                    new CrossgenAssembly(libB),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "MultiStepLibA", out diag), diag);
                    },
                },
                new("NonCompositeStep",
                [
                    new CrossgenAssembly(consumer),
                    new CrossgenAssembly(libA)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "MultiStepLibA", out diag), diag);
                        Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "GetValueFromLibA", "GetValue", out diag), diag);
                    },
                },
            ]));
    }

    // =====================================================================
    // Tier 2: Depth coverage
    // =====================================================================

    /// <summary>
    /// Composite + runtime-async + cross-module devirtualization.
    /// Interface defined in AsyncInterfaceLib, call sites in CompositeAsyncDevirtMain.
    /// </summary>
    [Fact]
    public void CompositeAsyncDevirtualize()
    {
        var asyncInterfaceLib = new CompiledAssembly
        {
            AssemblyName = "AsyncInterfaceLib",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AsyncInterfaceLib.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var compositeDevirtMain = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncDevirtMain",
            SourceResourceNames =
            [
                "RuntimeAsync/CompositeAsyncDevirtMain.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncInterfaceLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncDevirtualize),
            [
                new(nameof(CompositeAsyncDevirtualize),
                [
                    new CrossgenAssembly(asyncInterfaceLib),
                    new CrossgenAssembly(compositeDevirtMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncInterfaceLib", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallOnSealed", out diag), diag);
        }
    }

    /// <summary>
    /// Composite + runtime-async caller awaiting a NON-runtime-async virtual callee that the JIT
    /// devirtualizes to a sealed receiver. Resolving the callee's async-variant thunk must unwrap it
    /// to the underlying EcmaMethod.
    /// </summary>
    [Fact]
    public void CompositeAsyncDevirtNonAsyncCallee()
    {
        // Compiled WITHOUT runtime-async so the awaited virtuals get synthesized async-variant thunks.
        var nonAsyncCalleeLib = new CompiledAssembly
        {
            AssemblyName = "AsyncDevirtNonAsyncCalleeLib",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AsyncDevirtNonAsyncCalleeLib.cs"],
        };
        var main = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncDevirtNonAsyncCalleeMain",
            SourceResourceNames = ["RuntimeAsync/CompositeAsyncDevirtNonAsyncCalleeMain.cs"],
            Features = { RuntimeAsyncFeature },
            References = [nonAsyncCalleeLib],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncDevirtNonAsyncCallee),
            [
                new(nameof(CompositeAsyncDevirtNonAsyncCallee),
                [
                    new CrossgenAssembly(nonAsyncCalleeLib),
                    new CrossgenAssembly(main),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncDevirtNonAsyncCalleeLib", out diag), diag);

            Assert.True(R2RAssert.HasAsyncVariant(reader, "WriterBase.CompleteValueTaskAsync(", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "WriterBase.CompleteTaskAsync(", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AwaitInheritedValueTask(", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "AwaitInheritedTask(", out diag), diag);
        }
    }

    /// <summary>
    /// Composite with 3 assemblies in A→B→C transitive chain.
    /// Validates manifest refs for all three and transitive inlining.
    /// </summary>
    [Fact]
    public void CompositeTransitive()
    {
        var externalLib = new CompiledAssembly
        {
            AssemblyName = "ExternalLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/ExternalLib.cs"],
        };
        var inlineableLibTransitive = new CompiledAssembly
        {
            AssemblyName = "InlineableLibTransitive",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/InlineableLibTransitive.cs"],
            References = [externalLib]
        };
        var compositeTransitiveMain = new CompiledAssembly
        {
            AssemblyName = "CompositeTransitive",
            SourceResourceNames = ["CrossModuleInlining/TransitiveReferences.cs"],
            References = [inlineableLibTransitive, externalLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeTransitive),
            [
                new(nameof(CompositeTransitive),
                [
                    new CrossgenAssembly(externalLib),
                    new CrossgenAssembly(inlineableLibTransitive),
                    new CrossgenAssembly(compositeTransitiveMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InlineableLibTransitive", out diag), diag);
            Assert.True(R2RAssert.HasManifestRef(reader, "ExternalLib", out diag), diag);
        }
    }

    /// <summary>
    /// Non-composite runtime-async + transitive cross-module inlining.
    /// Chain: AsyncTransitiveMain → AsyncTransitiveLib → AsyncExternalLib.
    /// </summary>
    [Fact]
    public void AsyncCrossModuleTransitive()
    {
        var asyncExternalLib = new CompiledAssembly
        {
            AssemblyName = "AsyncExternalLib",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AsyncExternalLib.cs"],
        };
        var asyncTransitiveLib = new CompiledAssembly
        {
            AssemblyName = "AsyncTransitiveLib",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AsyncTransitiveLib.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncExternalLib]
        };
        var asyncTransitiveMain = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleTransitive),
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncTransitiveMain.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncTransitiveLib, asyncExternalLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleTransitive),
            [
                new(nameof(AsyncCrossModuleTransitive),
                [
                    new CrossgenAssembly(asyncTransitiveMain),
                    new CrossgenAssembly(asyncExternalLib) { Kind = Crossgen2InputKind.Reference },
                    new CrossgenAssembly(asyncTransitiveLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncTransitiveLib", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallTransitiveValueAsync", out diag), diag);
        }
    }

    /// <summary>
    /// Composite + runtime-async + transitive (3 assemblies).
    /// Full combination of composite, async, and transitive references.
    /// </summary>
    [Fact]
    public void CompositeAsyncTransitive()
    {
        var asyncExternalLib = new CompiledAssembly
        {
            AssemblyName = "AsyncExternalLib",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AsyncExternalLib.cs"],
        };
        var asyncTransitiveLib = new CompiledAssembly
        {
            AssemblyName = "AsyncTransitiveLib",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AsyncTransitiveLib.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncExternalLib]
        };
        var compositeAsyncTransitiveMain = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncTransitive",
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncTransitiveMain.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncTransitiveLib, asyncExternalLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncTransitive),
            [
                new(nameof(CompositeAsyncTransitive),
                [
                    new CrossgenAssembly(asyncExternalLib),
                    new CrossgenAssembly(asyncTransitiveLib),
                    new CrossgenAssembly(compositeAsyncTransitiveMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "AsyncTransitiveLib", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallTransitiveValueAsync", out diag), diag);
        }
    }

    /// <summary>
    /// Multi-step compilation with runtime-async in all assemblies.
    /// Step 1: Composite of async libs. Step 2: Non-composite consumer
    /// with cross-module inlining of async methods.
    /// </summary>
    [Fact]
    public void MultiStepCompositeAndNonCompositeAsync()
    {
        var asyncDepLib = new CompiledAssembly
        {
            AssemblyName = "AsyncDepLibContinuation",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AsyncDepLibContinuation.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var asyncCompositeLib = new CompiledAssembly
        {
            AssemblyName = "AsyncCompositeLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/AsyncCompositeLib.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var asyncConsumer = new CompiledAssembly
        {
            AssemblyName = "MultiStepAsyncConsumer",
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncCrossModuleContinuation.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncDepLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(MultiStepCompositeAndNonCompositeAsync),
            [
                new("CompositeAsyncStep",
                [
                    new CrossgenAssembly(asyncDepLib),
                    new CrossgenAssembly(asyncCompositeLib),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "AsyncDepLibContinuation", out diag), diag);
                        Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureRefAcrossAwait", out diag), diag);
                    },
                },
                new("NonCompositeAsyncStep",
                [
                    new CrossgenAssembly(asyncConsumer),
                    new CrossgenAssembly(asyncDepLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "AsyncDepLibContinuation", out diag), diag);
                        Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureRef", out diag), diag);
                    },
                },
            ]));
    }

    /// <summary>
    /// Tests cross-module generic compilation where multiple generic instantiations
    /// from an --opt-cross-module library each inline the same utility method.
    /// This produces multiple cross-module inliners for the same inlinee in the
    /// CrossModuleInlineInfo section, exercising the absolute-index encoding
    /// (not delta-encoded) for cross-module inliner entries.
    /// </summary>
    [Fact]
    public void CrossModuleGenericMultiInliner()
    {
        var crossModuleGenericLib = new CompiledAssembly
        {
            AssemblyName = "CrossModuleGenericLib",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CrossModuleGenericLib.cs"],
        };
        var consumer = new CompiledAssembly
        {
            AssemblyName = "MultiInlinerConsumer",
            SourceResourceNames = ["CrossModuleInlining/MultiInlinerConsumer.cs"],
            References = [crossModuleGenericLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CrossModuleGenericMultiInliner),
            [
                new(consumer.AssemblyName,
                [
                    new CrossgenAssembly(crossModuleGenericLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                    new CrossgenAssembly(consumer),
                ])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "CrossModuleGenericLib", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);

            // Verify that GetValue has cross-module inliners from both GenericWrapperA and GenericWrapperB.
            // This exercises the cross-module inliner parsing path where indices
            // must be read as absolute values, not delta-accumulated, and validates
            // that the resolved method names match the expected inliners.
            Assert.True(R2RAssert.HasCrossModuleInliners(reader, "GetValue", ["GenericWrapperA", "GenericWrapperB"], out diag), diag);
        }
    }

    [Fact]
    public void VirtualMethodGenericsNonGVM()
    {
        var nonGvmLib = new CompiledAssembly
        {
            AssemblyName = nameof(VirtualMethodGenericsNonGVM),
            SourceResourceNames = ["VirtualMethodGenerics/NonGVM.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(VirtualMethodGenericsNonGVM),
            [
                new(nameof(VirtualMethodGenericsNonGVM), [new CrossgenAssembly(nonGvmLib)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;

            // Test1: Interface impl on generic base type
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test1A`1<int>", "Test1Method", out diag), diag);

            // Test2: Virtual override on generic intermediate type
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test2C`1<int>", "Test2Method", out diag), diag);

            // Test3: Explicit DIM
            Assert.True(R2RAssert.HasCompiledMethod(reader, "ITest3WithDim`1<int>", "ITest3Base.Test3Method", out diag), diag);

            // Test4: Explicit interface impl on generic base
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test4A`1<int>", "ITest4<T>.Test4Method", out diag), diag);

            // Test5: Interface dispatch resolves to override on intermediate type
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test5B`1<int>", "Test5Method", out diag), diag);

            // Test6: Interface reimplementation with new slot
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test6B`1<int>", "Test6Method", out diag), diag);

            // Test7: Non-final DIM
            Assert.True(R2RAssert.HasCompiledMethod(reader, "ITest7`1<int>", "Test7Method", out diag), diag);
        }
    }

    [Fact]
    public void VirtualMethodGenericsGVM()
    {
        var gvmLib = new CompiledAssembly
        {
            AssemblyName = nameof(VirtualMethodGenericsGVM),
            SourceResourceNames = ["VirtualMethodGenerics/GVM.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(VirtualMethodGenericsGVM),
            [
                new(nameof(VirtualMethodGenericsGVM), [new CrossgenAssembly(gvmLib)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;

            // Test1: Interface GVM on base type
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test1A", "Test1Method", out diag, ["int"]), diag);

            // Test2: Interface GVM override on intermediate type
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test2B", "Test2Method", out diag, ["int"]), diag);

            // Test3: Explicit interface GVM impl on generic base
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test3A`1<int>", "ITest3<T>.Test3Method", out diag, ["int"]), diag);

            // Test4: Interface GVM reimplementation with new slot
            Assert.True(R2RAssert.HasCompiledMethod(reader, "Test4B", "Test4Method", out diag, ["int"]), diag);

            // Test5: Non-final default interface GVM
            Assert.True(R2RAssert.HasCompiledMethod(reader, "ITest5", "Test5Method", out diag, ["int"]), diag);

            // Test6: Explicit DIM with generic method
            Assert.True(R2RAssert.HasCompiledMethod(reader, "ITest6WithDim`1<int>", "ITest6Base.Test6Method", out diag, ["int"]), diag);

            // Test7: Static virtual generic method
            Assert.True(R2RAssert.HasCompiledMethod(reader, "ITest7`1<int>", "ITest7Base.Test7Method", out diag, ["int"]), diag);
        }
    }

    [Fact]
    public void VirtualMethodGenericsGenericLookup()
    {
        var genericLookupLib = new CompiledAssembly
        {
            AssemblyName = nameof(VirtualMethodGenericsGenericLookup),
            SourceResourceNames = ["VirtualMethodGenerics/GenericLookup.cs"],
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(VirtualMethodGenericsGenericLookup),
            [
                new(nameof(VirtualMethodGenericsGenericLookup), [new CrossgenAssembly(genericLookupLib)])
                {
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;

            // The generic type instantiation is reached only through a GenericLookupSignature
            // fixup, so its virtual method must still be discovered and compiled.
            Assert.True(R2RAssert.HasCompiledMethod(reader, "TestA`2<__Canon,int>", "TestMethod", out diag), diag);
        }
    }
}
