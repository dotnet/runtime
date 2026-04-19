// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.ReadyToRun.Tests.TestCasesRunner;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.DotNet.XUnitExtensions;
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
            R2RAssert.HasManifestRef(reader, "InlineableLib");
            R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetValue", "GetValue");
            R2RAssert.HasCrossModuleInlinedMethod(reader, "TestGetString", "GetString");
            R2RAssert.HasCrossModuleInliningInfo(reader);
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
                        R2RAssert.HasManifestRef(reader, "InlineableLibTransitive");
                        R2RAssert.HasManifestRef(reader, "ExternalLib");
                        R2RAssert.HasCrossModuleInlinedMethod(reader, "TestTransitiveValue", "GetExternalValue");
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
            R2RAssert.HasManifestRef(reader, "AsyncInlineableLib");
            R2RAssert.HasCrossModuleInlinedMethod(reader, "TestAsyncInline", "GetValueAsync");
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
            R2RAssert.HasManifestRef(reader, "CompositeLib");
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
            R2RAssert.HasAsyncVariant(reader, "SimpleAsyncMethod");
            R2RAssert.HasAsyncVariant(reader, "AsyncVoidReturn");
            R2RAssert.HasAsyncVariant(reader, "ValueTaskMethod");
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
            R2RAssert.HasAsyncVariant(reader, "CaptureObjectAcrossAwait");
            R2RAssert.HasAsyncVariant(reader, "CaptureMultipleRefsAcrossAwait");
            R2RAssert.HasContinuationLayout(reader, "CaptureObjectAcrossAwait");
            R2RAssert.HasContinuationLayout(reader, "CaptureMultipleRefsAcrossAwait");
            R2RAssert.HasResumptionStubFixup(reader, "CaptureObjectAcrossAwait");
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
            R2RAssert.HasAsyncVariant(reader, "GetValueAsync");
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
            R2RAssert.HasAsyncVariant(reader, "AsyncButNoAwait");
            R2RAssert.HasAsyncVariant(reader, "AsyncWithConditionalAwait");
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
            R2RAssert.HasManifestRef(reader, "AsyncDepLib");
            R2RAssert.HasAsyncVariant(reader, "CallCrossModuleAsync");
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
            R2RAssert.HasManifestRef(reader, "InlineableLib");
            R2RAssert.HasInlinedMethod(reader, "TestGetValue", "GetValue");
        }
    }

    /// <summary>
    /// Composite mode with runtime-async methods in both assemblies.
    /// Validates async variants exist in composite output.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/125337")]
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
            R2RAssert.HasManifestRef(reader, "AsyncCompositeLib");
            R2RAssert.HasAsyncVariant(reader, "CallCompositeAsync");
            R2RAssert.HasAsyncVariant(reader, "GetValueAsync");
        }
    }

    /// <summary>
    /// The full intersection: composite + runtime-async + cross-module inlining.
    /// Async methods from AsyncCompositeLib are inlined into CompositeAsyncMain
    /// within a composite image, exercising MutableModule token encoding for
    /// cross-module async continuation layouts.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/125337")]
    [Fact]
    public void CompositeAsyncCrossModuleInlining()
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
            nameof(CompositeAsyncCrossModuleInlining),
            [
                new(nameof(CompositeAsyncCrossModuleInlining),
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
            R2RAssert.HasManifestRef(reader, "AsyncCompositeLib");
            R2RAssert.HasAsyncVariant(reader, "CallCompositeAsync");
            R2RAssert.HasInlinedMethod(reader, "CallCompositeAsync", "GetValueAsync");
            R2RAssert.HasContinuationLayout(reader, "CallCompositeAsync");
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
            R2RAssert.HasManifestRef(reader, "AsyncDepLibContinuation");
            R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureRef");
            R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureArray");
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
                        R2RAssert.HasManifestRef(reader, "MultiStepLibA");
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
                        R2RAssert.HasManifestRef(reader, "MultiStepLibA");
                        R2RAssert.HasCrossModuleInlinedMethod(reader, "GetValueFromLibA", "GetValue");
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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/125337")]
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
            R2RAssert.HasManifestRef(reader, "AsyncInterfaceLib");
            R2RAssert.HasAsyncVariant(reader, "CallOnSealed");
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
            R2RAssert.HasManifestRef(reader, "InlineableLibTransitive");
            R2RAssert.HasManifestRef(reader, "ExternalLib");
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
            R2RAssert.HasManifestRef(reader, "AsyncTransitiveLib");
            R2RAssert.HasAsyncVariant(reader, "CallTransitiveValueAsync");
        }
    }

    /// <summary>
    /// Composite + runtime-async + transitive (3 assemblies).
    /// Full combination of composite, async, and transitive references.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/125337")]
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
            R2RAssert.HasManifestRef(reader, "AsyncTransitiveLib");
            R2RAssert.HasAsyncVariant(reader, "CallTransitiveValueAsync");
        }
    }

    /// <summary>
    /// Multi-step compilation with runtime-async in all assemblies.
    /// Step 1: Composite of async libs. Step 2: Non-composite consumer
    /// with cross-module inlining of async methods.
    /// </summary>
    [ActiveIssue("https://github.com/dotnet/runtime/issues/125337")]
    [Fact]
    public void MultiStepCompositeAndNonCompositeAsync()
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
        var asyncConsumer = new CompiledAssembly
        {
            AssemblyName = "MultiStepAsyncConsumer",
            SourceResourceNames =
            [
                "RuntimeAsync/AsyncCrossModuleContinuation.cs",
                "RuntimeAsync/RuntimeAsyncMethodGenerationAttribute.cs",
            ],
            Features = { RuntimeAsyncFeature },
            References = [asyncCompositeLib]
        };

        new R2RTestRunner(_output).Run(new R2RTestCase(
            nameof(MultiStepCompositeAndNonCompositeAsync),
            [
                new("CompositeAsyncStep",
                [
                    new CrossgenAssembly(asyncCompositeLib),
                    new CrossgenAssembly(compositeAsyncMain),
                ])
                {
                    Options = [Crossgen2Option.Composite, Crossgen2Option.Optimize],
                    Validate = reader =>
                    {
                        R2RAssert.HasManifestRef(reader, "AsyncCompositeLib");
                        R2RAssert.HasAsyncVariant(reader, "CallCompositeAsync");
                    },
                },
                new("NonCompositeAsyncStep",
                [
                    new CrossgenAssembly(asyncConsumer),
                    new CrossgenAssembly(asyncCompositeLib)
                    {
                        Kind = Crossgen2InputKind.Reference,
                        Options = [Crossgen2AssemblyOption.CrossModuleOptimization],
                    },
                ])
                {
                    Validate = reader =>
                    {
                        R2RAssert.HasManifestRef(reader, "AsyncCompositeLib");
                        R2RAssert.HasAsyncVariant(reader, "CallCrossModuleCaptureRef");
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
            R2RAssert.HasManifestRef(reader, "CrossModuleGenericLib");
            R2RAssert.HasCrossModuleInliningInfo(reader);

            // Verify that GetValue has cross-module inliners from both GenericWrapperA and GenericWrapperB.
            // This exercises the cross-module inliner parsing path where indices
            // must be read as absolute values, not delta-accumulated, and validates
            // that the resolved method names match the expected inliners.
            R2RAssert.HasCrossModuleInliners(reader, "GetValue", "GenericWrapperA", "GenericWrapperB");
        }
    }
}
