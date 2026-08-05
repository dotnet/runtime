// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;
extern alias wasmlowering;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;
using Xunit.Abstractions;

using Crossgen2Context = crossgen2::ILCompiler.ReadyToRunCompilerContext;
using Crossgen2Lowering = crossgen2::Internal.JitInterface.WasmLowering;
using Crossgen2LoweringFlags = crossgen2::Internal.JitInterface.WasmLowering.LoweringFlags;
using WasmResolverContext = wasmlowering::ILCompiler.Wasm.WasmTypeSystemContext;
using WasmResolverLowering = wasmlowering::Internal.JitInterface.WasmLowering;
using WasmResolverLoweringFlags = wasmlowering::Internal.JitInterface.WasmLowering.LoweringFlags;

namespace ILCompiler.ReadyToRun.Tests;

/// <summary>
/// Asserts that <c>ILCompiler.Wasm.Lowering</c> — the standalone resolver WasmAppBuilder shells out
/// to — computes exactly the signatures crossgen2 computes.
/// </summary>
/// <remarks>
/// The two share their lowering sources, but not their type system context: crossgen2 uses
/// <c>ReadyToRunCompilerContext</c> with a compilation module group, the resolver uses a plain
/// <c>MetadataTypeSystemContext</c>. Struct sizes come out of the field layout algorithm each
/// context installs, so nothing but a test keeps those two configurations from drifting apart —
/// and a drift would mean the generated interpreter call helpers disagree with compiled code about
/// how arguments are passed.
/// </remarks>
public class WasmLoweringParityTests
{
    private readonly ITestOutputHelper _output;

    public WasmLoweringParityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// The struct sizes WasmAppBuilder used to hard-code, back when it had no way to compute them.
    /// They are spelled out here so the resolver is pinned to the values that shipped.
    /// </summary>
    public static TheoryData<string, string, int> PreviouslyHardCodedStructSizes() => new()
    {
        { "System.Runtime.CompilerServices", "QCallModule", 8 },
        { "System.Runtime.CompilerServices", "QCallAssembly", 8 },
        { "System.Runtime.CompilerServices", "QCallTypeHandle", 8 },
        { "System", "GC/GCHeapHardLimitInfo", 64 },
    };

    [Theory]
    [MemberData(nameof(PreviouslyHardCodedStructSizes))]
    public void ResolverReproducesFormerlyHardCodedStructSizes(string @namespace, string name, int expectedSize)
    {
        WasmResolverContext resolverContext = CreateResolverContext();
        TypeDesc type = FindType(resolverContext.SystemModule, @namespace, name);

        Assert.Equal($"S{expectedSize}", wasmlowering::ILCompiler.Wasm.WasmAbiTypeResolver.GetAbiToken(type));
    }

    /// <summary>
    /// Walks every non-generic value type in CoreLib through both stacks. A single disagreement is
    /// a real ABI bug, so the test reports all of them rather than stopping at the first.
    /// </summary>
    [Fact]
    public void ResolverAgreesWithCrossgen2ForEveryCoreLibValueType()
    {
        Crossgen2Context crossgen2Context = CreateCrossgen2Context();
        WasmResolverContext resolverContext = CreateResolverContext();

        var crossgen2CoreLib = (EcmaModule)crossgen2Context.SystemModule;
        var resolverCoreLib = (EcmaModule)resolverContext.SystemModule;

        List<string> mismatches = new();
        int compared = 0;

        foreach (TypeDefinitionHandle handle in crossgen2CoreLib.MetadataReader.TypeDefinitions)
        {
            int token = MetadataTokens.GetToken(handle);

            if (!TryGetComparableValueType(crossgen2CoreLib, handle, out MetadataType crossgen2Type))
                continue;

            MetadataType resolverType;
            try
            {
                resolverType = (MetadataType)resolverCoreLib.GetType(MetadataTokens.EntityHandle(token));
            }
            catch (TypeSystemException)
            {
                // The resolver context refuses types crossgen2's context happens to tolerate only for
                // types the generator can never ask about; treat that as out of scope rather than
                // as a signature mismatch.
                continue;
            }

            string crossgen2Signature = GetCrossgen2Signature(crossgen2Context, crossgen2Type);
            string resolverSignature = GetResolverSignature(resolverContext, resolverType);
            compared++;

            if (crossgen2Signature != resolverSignature)
                mismatches.Add($"{crossgen2Type}: crossgen2 '{crossgen2Signature}' vs resolver '{resolverSignature}'");
        }

        _output.WriteLine($"Compared {compared} CoreLib value types.");

        Assert.True(compared > 100, $"Expected to compare a meaningful number of value types, but only saw {compared}.");
        Assert.Empty(mismatches);
    }

    private static bool TryGetComparableValueType(EcmaModule module, TypeDefinitionHandle handle, out MetadataType type)
    {
        type = null!;

        try
        {
            if (module.GetType(handle) is not MetadataType candidate)
                return false;

            // Generic types have no single layout, and the resolver identifies types by token, so it
            // cannot name an instantiation in the first place.
            if (!candidate.IsValueType || candidate.HasInstantiation || candidate.IsVoid)
                return false;

            // Touch the layout up front: types CoreLib cannot lay out (unsupported intrinsics on this
            // target, for instance) throw here on both stacks, and agreeing to throw is not the
            // parity this test is about.
            _ = candidate.GetElementSize();

            type = candidate;
            return true;
        }
        catch (TypeSystemException)
        {
            return false;
        }
    }

    /// <summary>
    /// Generic value types are skipped by the sweep above, because they are not addressable by
    /// metadata token and the resolver therefore never sees them by that route. They still have to
    /// agree: their layout decides the layout of every non-generic struct that embeds them, and
    /// <c>Vector&lt;T&gt;</c> in particular gets a wasm-specific 16-byte alignment that a
    /// general-purpose field layout algorithm does not apply.
    /// </summary>
    public static TheoryData<string, string, string, string> GenericValueTypes() => new()
    {
        { "System.Numerics", "Vector`1", "System", "Single" },
        { "System.Numerics", "Vector`1", "System", "Int32" },
        { "System.Runtime.Intrinsics", "Vector128`1", "System", "Byte" },
        { "System.Runtime.Intrinsics", "Vector64`1", "System", "Byte" },
        { "System", "Nullable`1", "System", "Int32" },
    };

    [Theory]
    [MemberData(nameof(GenericValueTypes))]
    public void ResolverAgreesWithCrossgen2ForGenericValueTypes(string @namespace, string name, string argNamespace, string argName)
    {
        Crossgen2Context crossgen2Context = CreateCrossgen2Context();
        WasmResolverContext resolverContext = CreateResolverContext();

        DefType crossgen2Type = Instantiate(crossgen2Context.SystemModule, @namespace, name, argNamespace, argName);
        DefType resolverType = Instantiate(resolverContext.SystemModule, @namespace, name, argNamespace, argName);

        // Compared explicitly because size and alignment are what a containing struct's layout is
        // computed from; two types can lower to the same signature and still lay out differently.
        Assert.Equal(crossgen2Type.InstanceFieldSize.AsInt, resolverType.InstanceFieldSize.AsInt);
        Assert.Equal(crossgen2Type.InstanceFieldAlignment.AsInt, resolverType.InstanceFieldAlignment.AsInt);
        Assert.Equal(crossgen2Type.InstanceByteCount.AsInt, resolverType.InstanceByteCount.AsInt);

        Assert.Equal(
            GetCrossgen2Signature(crossgen2Context, crossgen2Type),
            GetResolverSignature(resolverContext, resolverType));
    }

    private static DefType Instantiate(ModuleDesc module, string @namespace, string name, string argNamespace, string argName)
    {
        var definition = (MetadataType)FindType(module, @namespace, name);
        var argument = (TypeDesc)FindType(module, argNamespace, argName);

        return definition.MakeInstantiatedType(argument);
    }

    private static string GetCrossgen2Signature(Crossgen2Context context, TypeDesc parameterType)
    {
        return Crossgen2Lowering.GetSignature(
            MakeStaticVoidSignature(context, parameterType),
            Crossgen2LoweringFlags.IsUnmanagedCallersOnly).SignatureString;
    }

    private static string GetResolverSignature(WasmResolverContext context, TypeDesc parameterType)
    {
        return WasmResolverLowering.GetSignature(
            MakeStaticVoidSignature(context, parameterType),
            WasmResolverLoweringFlags.IsUnmanagedCallersOnly).SignatureString;
    }

    private static MethodSignature MakeStaticVoidSignature(TypeSystemContext context, TypeDesc parameterType)
    {
        return new MethodSignature(
            MethodSignatureFlags.Static,
            genericParameterCount: 0,
            context.GetWellKnownType(WellKnownType.Void),
            new[] { parameterType });
    }

    /// <summary>
    /// Resolves a type by namespace and name, where <paramref name="name"/> spells nested types as
    /// <c>Outer/Nested</c>.
    /// </summary>
    private static TypeDesc FindType(ModuleDesc module, string @namespace, string name)
    {
        string[] nameParts = name.Split('/');

        object outer = module.GetType(
            new Utf8Span(Encoding.UTF8.GetBytes(@namespace)),
            new Utf8Span(Encoding.UTF8.GetBytes(nameParts[0])),
            NotFoundBehavior.ReturnNull);
        Assert.NotNull(outer);

        var type = (MetadataType)outer;
        foreach (string nestedName in nameParts.AsSpan(1))
        {
            MetadataType nested = type.GetNestedType(new Utf8Span(Encoding.UTF8.GetBytes(nestedName)));
            Assert.NotNull(nested);
            type = nested;
        }

        return type;
    }

    /// <summary>
    /// Configures a type system context the way crossgen2 does for
    /// <c>--targetarch wasm --targetos browser</c>.
    /// </summary>
    private Crossgen2Context CreateCrossgen2Context()
    {
        string coreLibPath = CoreLibPath;

        crossgen2::ILCompiler.InstructionSetSupport instructionSetSupport = new(default, default, TargetArchitecture.Wasm32);
        TargetDetails target = new(TargetArchitecture.Wasm32, TargetOS.Browser, TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

        Crossgen2Context context = new(target, crossgen2::ILCompiler.SharedGenericsMode.CanonicalReferenceTypes, bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: false, instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "System.Private.CoreLib", coreLibPath } },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };

        EcmaModule coreLib = (EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib");
        context.SetSystemModule(coreLib);

        context.SetCompilationGroup(new crossgen2::ILCompiler.ReadyToRunSingleAssemblyCompilationModuleGroup(
            new crossgen2::ILCompiler.ReadyToRunCompilationModuleGroupConfig
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

    private WasmResolverContext CreateResolverContext()
    {
        WasmResolverContext context = new(TargetOS.Browser);
        context.AddAssemblyPath(CoreLibPath);
        context.SetSystemModule(context.GetModuleForSimpleName("System.Private.CoreLib"));

        return context;
    }

    private string CoreLibPath
    {
        get
        {
            string path = new TestPaths(_output).SystemPrivateCoreLibPath;
            Assert.True(File.Exists(path), $"System.Private.CoreLib.dll not found at '{path}'");

            return path;
        }
    }
}
