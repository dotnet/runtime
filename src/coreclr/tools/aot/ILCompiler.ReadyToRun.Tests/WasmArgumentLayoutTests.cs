// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;

using crossgen2::ILCompiler;
using crossgen2::ILCompiler.DependencyAnalysis.ReadyToRun;
using crossgen2::ILCompiler.DependencyAnalysis.Wasm;
using crossgen2::Internal.CallingConvention;
using crossgen2::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;
using Xunit.Abstractions;

namespace ILCompiler.ReadyToRun.Tests;

/// <summary>
/// Unit tests for the wasm argument layout crossgen2 computes. These drive the compiler's type
/// system directly, so they need neither a wasm JIT nor a runtime to execute against.
/// </summary>
public class WasmArgumentLayoutTests
{
    private const string Vector128OfT = "Vector128`1";
    private const string VectorOfT = "Vector`1";
    private const string Vector64OfT = "Vector64`1";
    private const string Vector256OfT = "Vector256`1";
    private const string Vector512OfT = "Vector512`1";

    private readonly ITestOutputHelper _output;

    public WasmArgumentLayoutTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string, WellKnownType> V128Types()
    {
        TheoryData<string, WellKnownType> data = new();

        foreach (string vectorType in new[] { Vector128OfT, VectorOfT })
        {
            foreach (WellKnownType elementType in new[]
                     {
                         WellKnownType.Byte, WellKnownType.Int16, WellKnownType.Int32,
                         WellKnownType.Int64, WellKnownType.Single, WellKnownType.Double,
                     })
            {
                data.Add(vectorType, elementType);
            }
        }

        return data;
    }

    /// <summary>
    /// Every type the wasm ABI passes as a <c>v128</c> must be 16-byte aligned. Both this compiler
    /// and the runtime align an argument slot by the argument type's own alignment, so an
    /// under-aligned vector silently disagrees with the frame the runtime builds for the same method.
    /// </summary>
    [Theory]
    [MemberData(nameof(V128Types))]
    public void WasmV128TypesAre16ByteAligned(string vectorType, WellKnownType elementType)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType instantiated = InstantiateVector(context, vectorType, elementType);

        Assert.Equal(16, instantiated.InstanceFieldSize.AsInt);
        Assert.Equal(16, instantiated.InstanceFieldAlignment.AsInt);
        Assert.Equal(WasmValueType.V128, WasmLowering.LowerType(instantiated));
    }

    /// <summary>
    /// A v128 argument must start on a 16-byte boundary. <see cref="System.Numerics.Vector{T}"/>
    /// previously kept the 8-byte alignment from its metadata layout and produced <c>[0, 8, 24]</c>.
    /// </summary>
    [Theory]
    [InlineData(Vector128OfT)]
    [InlineData(VectorOfT)]
    public void WasmV128ArgumentsStartOn16ByteBoundaries(string vectorType)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        TypeDesc int32 = context.GetWellKnownType(WellKnownType.Int32);

        MethodSignature signature = MakeStaticVoidSignature(
            context,
            context.GetWellKnownType(WellKnownType.Int64),
            InstantiateVector(context, vectorType, WellKnownType.Int32),
            int32.MakeByRefType());

        Assert.Equal(new[] { 0, 16, 32 }, GetArgumentOffsets(context, signature));
    }

    /// <summary>
    /// Only vectors that are exactly 16 bytes are a <c>v128</c>; the rest use the generic struct ABI.
    /// </summary>
    [Theory]
    [InlineData(Vector64OfT)]
    [InlineData(Vector256OfT)]
    [InlineData(Vector512OfT)]
    public void OtherSimdWidthsAreNotV128(string vectorType)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType instantiated = InstantiateVector(context, vectorType, WellKnownType.Int32);

        Assert.NotEqual(16, instantiated.InstanceFieldSize.AsInt);
        Assert.NotEqual(WasmValueType.V128, WasmLowering.LowerType(instantiated));
    }

    /// <summary>
    /// The 'V' encoding says nothing about which vector type produced it, so raising must resolve the
    /// same type regardless of what lowering saw first. The wasm R2R-to-interpreter thunk derives its
    /// whole frame layout from the raised signature.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(Vector128OfT)]
    [InlineData(VectorOfT)]
    public void RaisingV128SignatureIsIndependentOfLoweringOrder(string? loweredFirst)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();

        if (loweredFirst is not null)
        {
            WasmLowering.GetSignature(
                MakeStaticVoidSignature(context, InstantiateVector(context, loweredFirst, WellKnownType.Int32)),
                WasmLowering.LoweringFlags.None);
        }

        MethodSignature signature = MakeStaticVoidSignature(
            context,
            context.GetWellKnownType(WellKnownType.Int64),
            InstantiateVector(context, VectorOfT, WellKnownType.Single),
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        WasmSignature lowered = WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None);
        Assert.Equal("vlVip", lowered.SignatureString);

        MethodSignature raised = WasmLowering.RaiseSignature(lowered, context);
        _output.WriteLine($"lowered '{loweredFirst ?? "(nothing)"}' first; 'V' raised to {raised[1]}");

        Assert.Same(InstantiateVector(context, Vector128OfT, WellKnownType.Byte), raised[1]);
        Assert.Equal(GetArgumentOffsets(context, signature), GetArgumentOffsets(context, raised));
    }

    /// <summary>
    /// Configures a type system context the way crossgen2 does for
    /// <c>--targetarch wasm --targetos browser</c>.
    /// </summary>
    private ReadyToRunCompilerContext CreateWasmContext()
    {
        string coreLibPath = new TestPaths(_output).SystemPrivateCoreLibPath;
        Assert.True(File.Exists(coreLibPath), $"System.Private.CoreLib.dll not found at '{coreLibPath}'");

        InstructionSetSupport instructionSetSupport = new(default, default, TargetArchitecture.Wasm32);
        TargetDetails target = new(TargetArchitecture.Wasm32, TargetOS.Browser, TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

        // Wasm cannot generate code at runtime, matching what crossgen2's Program computes for this target.
        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes, bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: false, instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "System.Private.CoreLib", coreLibPath } },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };

        EcmaModule coreLib = (EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib");
        context.SetSystemModule(coreLib);

        // The R2R field layout algorithm reaches into the compilation group to decide whether base
        // offsets need aligning, so a context without one throws before computing any layout.
        context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
        {
            Context = context,
            IsInputBubble = true,
            CompilationModuleSet = new[] { coreLib },
            VersionBubbleModuleSet = new ModuleDesc[] { coreLib },
            CrossModuleInlineable = Array.Empty<ModuleDesc>(),
            InstructionSetSupport = instructionSetSupport,
        }));

        return context;
    }

    private static DefType InstantiateVector(ReadyToRunCompilerContext context, string vectorType, WellKnownType elementType)
    {
        MetadataType openType = vectorType switch
        {
            Vector128OfT => context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector128`1"u8),
            Vector64OfT => context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector64`1"u8),
            Vector256OfT => context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector256`1"u8),
            Vector512OfT => context.SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector512`1"u8),
            VectorOfT => context.SystemModule.GetType("System.Numerics"u8, "Vector`1"u8),
            _ => throw new ArgumentOutOfRangeException(nameof(vectorType), vectorType, null),
        };

        return openType.MakeInstantiatedType(context.GetWellKnownType(elementType));
    }

    private static MethodSignature MakeStaticVoidSignature(TypeSystemContext context, params TypeDesc[] parameters)
    {
        return new MethodSignature(
            MethodSignatureFlags.Static,
            genericParameterCount: 0,
            returnType: context.GetWellKnownType(WellKnownType.Void),
            parameters: parameters);
    }

    /// <summary>
    /// Runs the same ArgIterator the wasm R2R-to-interpreter thunk uses, returning each argument's
    /// offset relative to the start of the arguments area.
    /// </summary>
    private static List<int> GetArgumentOffsets(TypeSystemContext context, MethodSignature signature)
    {
        var (argIterator, transitionBlock) = GCRefMapBuilder.BuildArgIterator(signature, context);

        List<int> offsets = new();
        int argOffset;

        while ((argOffset = argIterator.GetNextOffset()) != TransitionBlock.InvalidOffset)
        {
            offsets.Add(argOffset - transitionBlock.OffsetOfArgs);
        }

        return offsets;
    }
}
