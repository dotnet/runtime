// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.ReadyToRun.Tests.TestCasesRunner;
using ILCompiler.Reflection.ReadyToRun;
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
        var syncInlinableMethods = new CompiledAssembly
        {
            AssemblyName = "SyncInlinableMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/BasicInlining.SyncInlinableMethods.cs"],
        };
        var basicCrossModuleInlining = new CompiledAssembly
        {
            AssemblyName = "BasicCrossModuleInlining",
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [syncInlinableMethods]
        };

        var cgSyncInlinableMethods = new CrossgenAssembly(syncInlinableMethods){ Kind = Crossgen2InputKind.Reference, Options = [Crossgen2AssemblyOption.CrossModuleOptimization] };
        var cgBasicCrossModuleInlining = new CrossgenAssembly(basicCrossModuleInlining);

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(BasicCrossModuleInlining),
            [new CrossgenCompilation(basicCrossModuleInlining.AssemblyName, [cgSyncInlinableMethods, cgBasicCrossModuleInlining]) { Validate = Validate }])
        );

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "SyncInlinableMethods", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetString", "GetString", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);
        }
    }

    [Fact]
    public void TransitiveReferences()
    {
        var syncLeafMethods = new CompiledAssembly()
        {
            AssemblyName = "SyncLeafMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/SyncLeafMethods.cs"],
        };
        var inlinableLeafCallers = new CompiledAssembly()
        {
            AssemblyName = "InlinableLeafCallers",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/TransitiveReferences.InlinableLeafCallers.cs"],
            References = [syncLeafMethods]
        };
        var transitiveReferences = new CompiledAssembly()
        {
            AssemblyName = "TransitiveReferences",
            SourceResourceNames = ["CrossModuleInlining/TransitiveReferences.cs"],
            References = [inlinableLeafCallers, syncLeafMethods]
        };
        new R2RTestRunner(_output).Run(new R2RTestCase(nameof(TransitiveReferences),
            [
                new("TransitiveReferences", [
                        new CrossgenAssembly(transitiveReferences),
                        new CrossgenAssembly(syncLeafMethods) { Kind = Crossgen2InputKind.Reference },
                        new CrossgenAssembly(inlinableLeafCallers)
                        {
                            Kind = Crossgen2InputKind.Reference,
                            Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                        },
                ])
                {
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "InlinableLeafCallers", out diag), diag);
                        Assert.True(R2RAssert.HasManifestRef(reader, "SyncLeafMethods", out diag), diag);
                        Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "TestTransitiveValue", "GetExternalValue", out diag), diag);
                    },
                },
            ]));
    }

    [Fact]
    public void AsyncCrossModuleInlining()
    {
        var inlinableAsyncMethods = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncMethods",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsInlinableAsync.InlinableAsyncMethods.cs"],
        };
        var awaitsInlinableAsync = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleInlining),
            SourceResourceNames = ["RuntimeAsync/AwaitsInlinableAsync.cs"],
            References = [inlinableAsyncMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleInlining),
            [
                new(nameof(AsyncCrossModuleInlining),
                [
                    new CrossgenAssembly(awaitsInlinableAsync),
                    new CrossgenAssembly(inlinableAsyncMethods)
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableAsyncMethods", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInlinedMethod(reader, "CallGetValueAsync", "GetValueAsync", out diag), diag);
        }
    }

    [Fact]
    public void CompositeBasic()
    {
        var compositeLib = new CompiledAssembly
        {
            AssemblyName = "SyncTypeAndMethod",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/CompositeBasic.SyncTypeAndMethod.cs"],
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
            Assert.True(R2RAssert.HasManifestRef(reader, "SyncTypeAndMethod", out diag), diag);
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
                "RuntimeAsync/Dependencies/AwaitsLocalsCapturedAcrossAwait.LocalsCapturedAcrossAwait.cs",
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
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureArrayAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasContinuationLayout(reader, "CaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasContinuationLayout(reader, "CaptureArrayAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStubFixup(reader, "CaptureRefAcrossAwait", out diag), diag);
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
                "RuntimeAsync/AwaitsThroughInterface.cs",
                "RuntimeAsync/Dependencies/AwaitsThroughInterface.InterfaceAndImpls.cs",
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
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GetValueAsync", out diag), diag);
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
                "RuntimeAsync/AsyncWithoutYield.cs",
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
    /// PR #121679: MutableModule async references + cross-module inlining
    /// of runtime-async methods with cross-module dependency.
    /// </summary>
    [Fact]
    public void RuntimeAsyncCrossModule()
    {
        var inlinableAsyncMethods = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncMethods",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsInlinableAsync.InlinableAsyncMethods.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var runtimeAsyncCrossModule = new CompiledAssembly
        {
            AssemblyName = nameof(RuntimeAsyncCrossModule),
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsInlinableAsync.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [inlinableAsyncMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(RuntimeAsyncCrossModule),
            [
                new(nameof(RuntimeAsyncCrossModule),
                [
                    new CrossgenAssembly(runtimeAsyncCrossModule),
                    new CrossgenAssembly(inlinableAsyncMethods)
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableAsyncMethods", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGetValueAsync", out diag), diag);
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
        var syncInlinableMethods = new CompiledAssembly
        {
            AssemblyName = "SyncInlinableMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/BasicInlining.SyncInlinableMethods.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = "CompositeCrossModuleInlining",
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [syncInlinableMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeCrossModuleInlining),
            [
                new(nameof(CompositeCrossModuleInlining),
                [
                    new CrossgenAssembly(syncInlinableMethods),
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
            Assert.True(R2RAssert.HasManifestRef(reader, "SyncInlinableMethods", out diag), diag);
            Assert.True(R2RAssert.HasInlinedMethod(reader, "TestGetValue", "GetValue", out diag), diag);
        }
    }

    /// <summary>
    /// Negative test: a composite image whose only inputs are the inlinee and the inliner
    /// does NOT produce a CrossModuleInlineInfo section. CrossModuleInlineInfo only records
    /// inlining where the inlinee module is OUTSIDE the compiled image's version bubble
    /// (typically added via <c>--opt-cross-module</c> on a reference assembly). Here both
    /// modules are composite inputs and therefore in the same version bubble, so any
    /// inlining between them is recorded in the per-module InliningInfo2 section instead.
    /// A different setup — composite output plus an external reference passed via
    /// <c>--opt-cross-module</c> — could still produce CrossModuleInlineInfo entries; this
    /// test only covers the "all inlinees are composite inputs" case.
    /// Compare with <see cref="BasicCrossModuleInlining"/>, which uses the same source modules
    /// in a non-composite layout and DOES produce CrossModuleInlineInfo entries.
    /// </summary>
    [Fact]
    public void CompositeDoesNotProduceCrossModuleInliningInfo()
    {
        var syncInlinableMethods = new CompiledAssembly
        {
            AssemblyName = "SyncInlinableMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/BasicInlining.SyncInlinableMethods.cs"],
        };
        var compositeMain = new CompiledAssembly
        {
            AssemblyName = nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
            SourceResourceNames = ["CrossModuleInlining/BasicInlining.cs"],
            References = [syncInlinableMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
            [
                new(nameof(CompositeDoesNotProduceCrossModuleInliningInfo),
                [
                    new CrossgenAssembly(syncInlinableMethods),
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
    /// Composite mode with runtime-async methods in both assemblies.
    /// Validates async variants exist in composite output.
    /// </summary>
    [Fact]
    public void CompositeAsync()
    {
        var inlinableAsyncMethods = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncMethods",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsInlinableAsync.InlinableAsyncMethods.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var awaitsInlinableAsync = new CompiledAssembly
        {
            AssemblyName = "AwaitsInlinableAsync",
            SourceResourceNames = ["RuntimeAsync/AwaitsInlinableAsync.cs"],
            Features = { RuntimeAsyncFeature },
            References = [inlinableAsyncMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsync),
            [
                new(nameof(CompositeAsync),
                [
                    new CrossgenAssembly(inlinableAsyncMethods),
                    new CrossgenAssembly(awaitsInlinableAsync),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableAsyncMethods", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallGetValueAsync", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "GetValueAsync", out diag), diag);
        }
    }

    /// <summary>
    /// The full intersection: composite + runtime-async + cross-module inlining.
    /// Async methods from InlinableAsyncMethods are inlined into AwaitsInlinableAsync
    /// within a composite image, exercising MutableModule token encoding for
    /// cross-module async continuation layouts.
    /// </summary>
    /// <summary>
    /// Composite + runtime-async + intra-bubble inlining matrix test.
    /// Verifies that, in composite mode, awaitless async candidates ARE inlined into
    /// their callers (recorded in InliningInfo2 — composite inputs share a version
    /// bubble, so true CrossModuleInlineInfo entries are not produced for them), while
    /// candidates whose body contains a real <c>await</c> are NOT inlined. The latter
    /// is a JIT-level limitation: <c>Compiler::impSetupAsyncCall</c> in
    /// importercalls.cpp issues a FATAL <c>CALLEE_AWAIT</c> observation as soon as it
    /// sees an async call inside an inlining candidate (see also <c>CALLEE_ASYNC_SUSPEND</c>).
    /// The matrix covers Task, Task&lt;primitive&gt;, and Task&lt;class&gt; return shapes.
    /// </summary>
    [Fact]
    public void CompositeAsyncInliningMatrix()
    {
        var asyncInlineCandidatesLib = new CompiledAssembly
        {
            AssemblyName = "InlineCandidateMatrix",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsInlineCandidateMatrix.InlineCandidateMatrix.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var asyncInlineCallers = new CompiledAssembly
        {
            AssemblyName = "AwaitsInlineCandidateMatrix",
            SourceResourceNames = ["RuntimeAsync/AwaitsInlineCandidateMatrix.cs"],
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlineCandidateMatrix", out diag), diag);

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
    /// Composite mode with runtime-async methods that capture GC refs across await
    /// points, exercising ContinuationLayout and RESUME stub emission in composite images.
    /// Validates that both the library and main assembly's async methods produce
    /// [ASYNC] variants, [RESUME] stubs, and ContinuationLayout fixups.
    /// </summary>
    [Fact]
    public void CompositeAsyncContinuationAndResume()
    {
        var locals = new CompiledAssembly
        {
            AssemblyName = "LocalsCapturedAcrossAwait",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsLocalsCapturedAcrossAwait.LocalsCapturedAcrossAwait.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var awaitsLocals = new CompiledAssembly
        {
            AssemblyName = "AwaitsLocalsCapturedAcrossAwait",
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsLocalsCapturedAcrossAwait.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [locals]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncContinuationAndResume),
            [
                new(nameof(CompositeAsyncContinuationAndResume),
                [
                    new CrossgenAssembly(locals),
                    new CrossgenAssembly(awaitsLocals),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            // Library methods produce async variants and resume stubs
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureArrayAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStub(reader, "CaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStub(reader, "CaptureArrayAcrossAwait", out diag), diag);

            // Main assembly methods produce async variants and resume stubs
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "LocalCaptureAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStub(reader, "CallCaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasResumptionStub(reader, "LocalCaptureAcrossAwait", out diag), diag);

            // ContinuationLayout fixups are present for methods with GC refs across awaits
            Assert.True(R2RAssert.HasContinuationLayout(reader, "CaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasContinuationLayout(reader, "LocalCaptureAcrossAwait", out diag), diag);
        }
    }

    /// <summary>
    /// Composite-mode regression coverage for async thunk emission of methods on
    /// generic types (and generic methods on generic types). The parent PR's
    /// description specifically calls these out as the case that originally
    /// broke <c>MethodWithToken..ctor()</c> owning-type computation when the
    /// async-thunk ILStub forced a strip-instantiation in
    /// <c>CorInfoImpl.HandleToModuleToken</c>. The follow-up "Get IL for the
    /// (possibly instantiated) method, not the definition" fix in
    /// <c>ReadyToRunCodegenCompilation.EnsureAsyncThunkTokensAreAvailable</c>
    /// is also exercised by this test (it only matters for instantiated
    /// methods/types). Both reference-type and value-type instantiations are
    /// covered because token resolution differs between the two.
    /// </summary>
    [Fact]
    public void CompositeAsyncGenericTypes()
    {
        var asyncGenericTypeLib = new CompiledAssembly
        {
            AssemblyName = "AwaitsAsyncMethodsOnGenericType.GenericContainer",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsAsyncMethodsOnGenericType.GenericContainer.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var compositeAsyncGenericTypesMain = new CompiledAssembly
        {
            AssemblyName = "AwaitsAsyncMethodsOnGenericType",
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsAsyncMethodsOnGenericType.cs",
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
        var locals = new CompiledAssembly
        {
            AssemblyName = "LocalsCapturedAcrossAwait",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsLocalsCapturedAcrossAwait.LocalsCapturedAcrossAwait.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
        };
        var awaitsLocals = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleContinuation),
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsLocalsCapturedAcrossAwait.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [locals]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleContinuation),
            [
                new(nameof(AsyncCrossModuleContinuation),
                [
                    new CrossgenAssembly(awaitsLocals),
                    new CrossgenAssembly(locals)
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
            Assert.True(R2RAssert.HasManifestRef(reader, "LocalsCapturedAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCaptureRefAcrossAwait", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCaptureArrayAcrossAwait", out diag), diag);
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
            AssemblyName = "MultiStepLeaf",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/MultiStepConsumer.Leaf.cs"],
        };
        var libB = new CompiledAssembly
        {
            AssemblyName = "MultiStepMid",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/MultiStepConsumer.Mid.cs"],
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
                        Assert.True(R2RAssert.HasManifestRef(reader, "MultiStepLeaf", out diag), diag);
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
                        Assert.True(R2RAssert.HasManifestRef(reader, "MultiStepLeaf", out diag), diag);
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
        var interfaceAndImpls = new CompiledAssembly
        {
            AssemblyName = "InterfaceAndImpls",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsThroughInterface.InterfaceAndImpls.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var awaitsThroughInterface = new CompiledAssembly
        {
            AssemblyName = "AwaitsThroughInterface",
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsThroughInterface.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [interfaceAndImpls]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncDevirtualize),
            [
                new(nameof(CompositeAsyncDevirtualize),
                [
                    new CrossgenAssembly(interfaceAndImpls),
                    new CrossgenAssembly(awaitsThroughInterface),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = Validate,
                },
            ]));

        static void Validate(ReadyToRunReader reader)
        {
            string diag;
            Assert.True(R2RAssert.HasManifestRef(reader, "InterfaceAndImpls", out diag), diag);
            Assert.True(R2RAssert.HasAsyncVariant(reader, "CallOnSealed", out diag), diag);
        }
    }

    /// <summary>
    /// Composite with 3 assemblies in A→B→C transitive chain.
    /// Validates manifest refs for all three and transitive inlining.
    /// </summary>
    [Fact]
    public void CompositeTransitive()
    {
        var syncLeafMethods = new CompiledAssembly
        {
            AssemblyName = "SyncLeafMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/SyncLeafMethods.cs"],
        };
        var inlinableLeafCallers = new CompiledAssembly
        {
            AssemblyName = "InlinableLeafCallers",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/TransitiveReferences.InlinableLeafCallers.cs"],
            References = [syncLeafMethods]
        };
        var compositeTransitiveMain = new CompiledAssembly
        {
            AssemblyName = "CompositeTransitive",
            SourceResourceNames = ["CrossModuleInlining/TransitiveReferences.cs"],
            References = [inlinableLeafCallers, syncLeafMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeTransitive),
            [
                new(nameof(CompositeTransitive),
                [
                    new CrossgenAssembly(syncLeafMethods),
                    new CrossgenAssembly(inlinableLeafCallers),
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableLeafCallers", out diag), diag);
            Assert.True(R2RAssert.HasManifestRef(reader, "SyncLeafMethods", out diag), diag);
        }
    }

    /// <summary>
    /// Non-composite runtime-async + transitive cross-module inlining.
    /// Chain: AsyncTransitiveMain → AsyncTransitiveLib → AsyncExternalLib.
    /// </summary>
    [Fact]
    public void AsyncCrossModuleTransitive()
    {
        var syncLeafMethods = new CompiledAssembly
        {
            AssemblyName = "SyncLeafMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/SyncLeafMethods.cs"],
        };
        var inlinableAsyncLeafCallers = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncLeafCallers",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsTransitiveAsync.InlinableAsyncLeafCallers.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [syncLeafMethods]
        };
        var awaitsTransitiveAsync = new CompiledAssembly
        {
            AssemblyName = nameof(AsyncCrossModuleTransitive),
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsTransitiveAsync.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [inlinableAsyncLeafCallers, syncLeafMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(AsyncCrossModuleTransitive),
            [
                new(nameof(AsyncCrossModuleTransitive),
                [
                    new CrossgenAssembly(awaitsTransitiveAsync),
                    new CrossgenAssembly(syncLeafMethods) { Kind = Crossgen2InputKind.Reference },
                    new CrossgenAssembly(inlinableAsyncLeafCallers)
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableAsyncLeafCallers", out diag), diag);
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
        var syncLeafMethods = new CompiledAssembly
        {
            AssemblyName = "SyncLeafMethods",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/SyncLeafMethods.cs"],
        };
        var inlinableAsyncLeafCallers = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncLeafCallers",
            SourceResourceNames =
            [
                "RuntimeAsync/Dependencies/AwaitsTransitiveAsync.InlinableAsyncLeafCallers.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [syncLeafMethods]
        };
        var compositeAsyncTransitiveMain = new CompiledAssembly
        {
            AssemblyName = "CompositeAsyncTransitive",
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsTransitiveAsync.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [inlinableAsyncLeafCallers, syncLeafMethods]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(CompositeAsyncTransitive),
            [
                new(nameof(CompositeAsyncTransitive),
                [
                    new CrossgenAssembly(syncLeafMethods),
                    new CrossgenAssembly(inlinableAsyncLeafCallers),
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
            Assert.True(R2RAssert.HasManifestRef(reader, "InlinableAsyncLeafCallers", out diag), diag);
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
        var locals = new CompiledAssembly
        {
            AssemblyName = "LocalsCapturedAcrossAwait",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsLocalsCapturedAcrossAwait.LocalsCapturedAcrossAwait.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var inlinableAsyncMethods = new CompiledAssembly
        {
            AssemblyName = "InlinableAsyncMethods",
            SourceResourceNames = ["RuntimeAsync/Dependencies/AwaitsInlinableAsync.InlinableAsyncMethods.cs"],
            Features = { RuntimeAsyncFeature },
        };
        var asyncConsumer = new CompiledAssembly
        {
            AssemblyName = "MultiStepAsyncConsumer",
            SourceResourceNames =
            [
                "RuntimeAsync/AwaitsLocalsCapturedAcrossAwait.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [locals]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(MultiStepCompositeAndNonCompositeAsync),
            [
                new("CompositeAsyncStep",
                [
                    new CrossgenAssembly(locals),
                    new CrossgenAssembly(inlinableAsyncMethods),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "LocalsCapturedAcrossAwait", out diag), diag);
                        Assert.True(R2RAssert.HasAsyncVariant(reader, "CaptureRefAcrossAwait", out diag), diag);
                    },
                },
                new("NonCompositeAsyncStep",
                [
                    new CrossgenAssembly(asyncConsumer),
                    new CrossgenAssembly(locals)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = reader =>
                    {
                        string diag;
                        Assert.True(R2RAssert.HasManifestRef(reader, "LocalsCapturedAcrossAwait", out diag), diag);
                        Assert.True(R2RAssert.HasAsyncVariant(reader, "CallCaptureRefAcrossAwait", out diag), diag);
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
            AssemblyName = "MultiInlinerConsumer.GenericWrappers",
            SourceResourceNames = ["CrossModuleInlining/Dependencies/MultiInlinerConsumer.GenericWrappers.cs"],
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
            Assert.True(R2RAssert.HasManifestRef(reader, "MultiInlinerConsumer.GenericWrappers", out diag), diag);
            Assert.True(R2RAssert.HasCrossModuleInliningInfo(reader, out diag), diag);

            // Verify that GetValue has cross-module inliners from both GenericWrapperA and GenericWrapperB.
            // This exercises the cross-module inliner parsing path where indices
            // must be read as absolute values, not delta-accumulated, and validates
            // that the resolved method names match the expected inliners.
            Assert.True(R2RAssert.HasCrossModuleInliners(reader, "GetValue", ["GenericWrapperA", "GenericWrapperB"], out diag), diag);
        }
    }
}
