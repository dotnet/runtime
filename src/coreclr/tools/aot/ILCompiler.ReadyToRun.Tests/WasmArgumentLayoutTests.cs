// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    private const string Int128Type = "Int128";
    private const string UInt128Type = "UInt128";
    private const string Decimal128Type = "Decimal128";

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
    /// Types with no single wasm value type wide enough to hold them are passed by value across
    /// several wasm parameters, matching the wasm C ABI. The signature spells the slot character
    /// followed by the factor by which the type's alignment is elevated above the slot's own.
    /// </summary>
    [Theory]
    [InlineData(Int128Type, "vll2ip", WasmValueType.I64, 2, 16, new[] { 0, 16, 32 })]
    [InlineData(Vector256OfT, "vlV2ip", WasmValueType.V128, 2, 32, new[] { 0, 16, 48 })]
    [InlineData(Vector512OfT, "vlV4ip", WasmValueType.V128, 4, 64, new[] { 0, 16, 80 })]
    public void MultiSlotTypesAreSplitAcrossWasmParameters(
        string typeName, string expectedSignature, WasmValueType slotType, int slotCount, int alignment, int[] expectedOffsets)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType multiSlot = InstantiateMultiSlotType(context, typeName);

        Assert.Equal(alignment, multiSlot.InstanceFieldSize.AsInt);
        Assert.Equal(alignment, multiSlot.InstanceFieldAlignment.AsInt);

        MethodSignature signature = MakeStaticVoidSignature(
            context,
            context.GetWellKnownType(WellKnownType.Int64),
            multiSlot,
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        WasmSignature lowered = WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None);
        Assert.Equal(expectedSignature, lowered.SignatureString);

        // $sp, the i64, the slots, the byref and the portable entrypoint.
        ReadOnlySpan<WasmValueType> parameters = lowered.FuncType.Params.Types;
        Assert.Equal(4 + slotCount, parameters.Length);
        for (int slot = 0; slot < slotCount; slot++)
        {
            Assert.Equal(slotType, parameters[2 + slot]);
        }

        // The argument still occupies a single frame slot. ArgIterator clamps argument alignment to
        // [8, 16], so these land on 16 rather than on their own larger alignment.
        Assert.Equal(expectedOffsets, GetArgumentOffsets(context, signature));
    }

    /// <summary>
    /// The elevation digit says nothing about which type produced it, so raising must resolve one
    /// with the same size and alignment regardless of what lowering saw. The wasm R2R-to-interpreter
    /// thunk derives its whole frame layout from the raised signature.
    /// </summary>
    [Theory]
    [InlineData(Int128Type)]
    [InlineData(UInt128Type)]
    [InlineData(Decimal128Type)]
    [InlineData(Vector256OfT)]
    [InlineData(Vector512OfT)]
    public void RaisingMultiSlotSignaturePreservesArgumentLayout(string typeName)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType multiSlot = InstantiateMultiSlotType(context, typeName);

        // Prime the struct-size cache with an ordinary 8-byte aligned struct of the same size. If
        // the multi-slot classification ever regressed to 'S<N>', raising would resolve to this
        // competitor and the offsets would differ; without it the cache would hand back the
        // multi-slot type itself and the comparison would hold either way.
        MethodSignature competitor = MakeStaticVoidSignature(
            context, MakeAlignedEightBlob(context, multiSlot.InstanceFieldSize.AsInt));
        WasmLowering.GetSignature(competitor, WasmLowering.LoweringFlags.None);

        MethodSignature signature = MakeStaticVoidSignature(
            context,
            context.GetWellKnownType(WellKnownType.Int64),
            multiSlot,
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        WasmSignature lowered = WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None);
        MethodSignature raised = WasmLowering.RaiseSignature(lowered, context);

        Assert.Equal(GetArgumentOffsets(context, signature), GetArgumentOffsets(context, raised));
    }

    /// <summary>
    /// A struct that is merely the same size as a multi-slot type keeps the by-reference
    /// <c>S&lt;N&gt;</c> encoding and a smaller alignment, so sharing an encoding with one would
    /// give the same thunk two different frame layouts.
    /// </summary>
    [Theory]
    [InlineData(2, "vlS16ip", new[] { 0, 8, 24 })]
    [InlineData(4, "vlS32ip", new[] { 0, 8, 40 })]
    public void SameSizedOrdinaryStructsStayByReference(int fieldCount, string expectedSignature, int[] expectedOffsets)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        TypeDesc int64 = context.GetWellKnownType(WellKnownType.Int64);

        // A value tuple of longs is an ordinary multi-field struct of the right size, 8-byte aligned.
        MetadataType tupleType = fieldCount == 2
            ? context.SystemModule.GetType("System"u8, "ValueTuple`2"u8)
            : context.SystemModule.GetType("System"u8, "ValueTuple`4"u8);
        DefType blob = tupleType.MakeInstantiatedType(new Instantiation(Enumerable.Repeat(int64, fieldCount).ToArray()));

        Assert.Equal(fieldCount * 8, blob.InstanceFieldSize.AsInt);
        Assert.Equal(8, blob.InstanceFieldAlignment.AsInt);

        MethodSignature signature = MakeStaticVoidSignature(
            context, int64, blob, context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        Assert.Equal(expectedSignature, WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString);
        Assert.Equal(expectedOffsets, GetArgumentOffsets(context, signature));
    }

    /// <summary>
    /// Auto-layout structs use the runtime's effective aggregate alignment for argument placement,
    /// which can be smaller than the alignment crossgen uses while laying out their fields.
    /// </summary>
    [Fact]
    public void AutoLayoutStructUsesRuntimeAggregateAlignment()
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType alignedEight = MakeAlignedEightBlob(context, 32);
        DefType int128 = InstantiateMultiSlotType(context, Int128Type);
        DefType autoLayout = MakeValueTuple(context, int128, int128);

        Assert.Equal(32, alignedEight.InstanceFieldSize.AsInt);
        Assert.Equal(8, alignedEight.InstanceFieldAlignment.AsInt);
        Assert.Equal(32, autoLayout.InstanceFieldSize.AsInt);
        Assert.Equal(16, autoLayout.InstanceFieldAlignment.AsInt);
        Assert.Equal(8, CorInfoImpl.GetClassAlignmentRequirementStatic(autoLayout));

        MethodSignature autoLayoutSignature = MakeProbeSignature(context, autoLayout);
        MethodSignature alignedEightSignature = MakeProbeSignature(context, alignedEight);

        WasmSignature autoLayoutLowered =
            WasmLowering.GetSignature(autoLayoutSignature, WasmLowering.LoweringFlags.None);
        WasmSignature alignedEightLowered =
            WasmLowering.GetSignature(alignedEightSignature, WasmLowering.LoweringFlags.None);

        Assert.Equal("vlS32ip", autoLayoutLowered.SignatureString);
        Assert.Equal("vlS32ip", alignedEightLowered.SignatureString);
        Assert.Equal(new[] { 0, 8, 40 }, GetArgumentOffsets(context, autoLayoutSignature));
        Assert.Equal(
            GetArgumentOffsets(context, autoLayoutSignature),
            GetArgumentOffsets(context, WasmLowering.RaiseSignature(autoLayoutLowered, context)));
        Assert.Equal(
            GetArgumentOffsets(context, alignedEightSignature),
            GetArgumentOffsets(context, WasmLowering.RaiseSignature(alignedEightLowered, context)));
    }

    /// <summary>
    /// Narrow vectors are not multi-slot. <see cref="System.Runtime.Intrinsics.Vector64{T}"/> is a
    /// single <c>ulong</c> field, so it unwraps to a scalar <c>i64</c> rather than to any slot form.
    /// Note the multi-slot widths are named by BYTE SIZE, so 64 there means Vector512, not Vector64.
    /// </summary>
    [Theory]
    [InlineData(Vector64OfT, 8, "vllp")]
    [InlineData(Vector128OfT, 16, "vlVp")]
    public void NarrowVectorsAreNotMultiSlot(string vectorType, int expectedSize, string expectedSignature)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType vector = InstantiateVector(context, vectorType, WellKnownType.Int32);

        Assert.Equal(expectedSize, vector.InstanceFieldSize.AsInt);
        Assert.False(WasmLowering.TryGetMultiSegmentLayout(vector, out _, out _));

        MethodSignature signature = MakeStaticVoidSignature(
            context, context.GetWellKnownType(WellKnownType.Int64), vector);

        Assert.Equal(expectedSignature, WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString);
    }

    /// <summary>
    /// A multi-slot type is returned through a hidden buffer and so spells <c>S&lt;N&gt;</c>, but it
    /// must not be remembered as the type that encoding raises to: it re-lowers to its multi-slot
    /// form, so an ordinary same-sized struct parameter would raise with the wrong alignment.
    /// </summary>
    [Theory]
    [InlineData(Int128Type, 2)]
    [InlineData(Vector256OfT, 4)]
    public void MultiSlotReturnDoesNotPoisonTheStructSizeCache(string typeName, int fieldCount)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        TypeDesc int64 = context.GetWellKnownType(WellKnownType.Int64);

        // Lower a method whose return is the multi-slot type; this is what caches by size.
        DefType multiSlot = InstantiateMultiSlotType(context, typeName);
        WasmLowering.GetSignature(
            new MethodSignature(MethodSignatureFlags.Static, 0, multiSlot, Array.Empty<TypeDesc>()),
            WasmLowering.LoweringFlags.None);

        // Now an ordinary struct of the same size, which is only 8-byte aligned.
        MetadataType tupleType = fieldCount == 2
            ? context.SystemModule.GetType("System"u8, "ValueTuple`2"u8)
            : context.SystemModule.GetType("System"u8, "ValueTuple`4"u8);
        DefType blob = tupleType.MakeInstantiatedType(new Instantiation(Enumerable.Repeat(int64, fieldCount).ToArray()));
        Assert.Equal(multiSlot.InstanceFieldSize.AsInt, blob.InstanceFieldSize.AsInt);

        MethodSignature signature = MakeStaticVoidSignature(
            context, int64, blob, context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        WasmSignature lowered = WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None);
        MethodSignature raised = WasmLowering.RaiseSignature(lowered, context);

        Assert.Equal(GetArgumentOffsets(context, signature), GetArgumentOffsets(context, raised));
    }

    /// <summary>
    /// A vector over an unsupported base type is not ABI-classifiable, so it keeps the struct ABI.
    /// The shared <c>__Canon</c> form reaches this, and a structural walk that ignored the base type
    /// would descend into <c>Vector128</c>'s raw fields and report eight scalar slots for a 512-bit
    /// vector rather than four v128 ones.
    /// </summary>
    [Theory]
    [InlineData(Vector128OfT)]
    [InlineData(Vector256OfT)]
    [InlineData(Vector512OfT)]
    public void VectorsOverUnsupportedBaseTypesAreNotMultiSlot(string vectorType)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        MetadataType openType = (MetadataType)context.SystemModule.GetType(
            "System.Runtime.Intrinsics"u8,
            System.Text.Encoding.UTF8.GetBytes(vectorType));
        DefType overString = openType.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.String));

        Assert.False(WasmLowering.TryGetMultiSegmentLayout(overString, out _, out _));
        Assert.NotEqual(WasmValueType.V128, WasmLowering.LowerType(overString));
    }

    /// <summary>
    /// <see cref="System.Numerics.Decimal128"/> is one of the known CoreLib multi-slot types. It is
    /// 16 bytes and 16-byte aligned to match <c>__int128_t</c>, so it splits into two i64 slots
    /// exactly as <see cref="System.Int128"/> does. Its narrower siblings and the legacy 8-aligned
    /// <see cref="decimal"/> keep the ordinary struct ABI.
    /// </summary>
    [Theory]
    [InlineData("Decimal128", "vll2ip", new[] { 0, 16, 32 })]
    [InlineData("Decimal64", "vllip", new[] { 0, 8, 16 })]
    [InlineData("Decimal32", "vliip", new[] { 0, 8, 16 })]
    public void DecimalFloatingPointTypesFollowTheirAbiShape(string typeName, string expectedSignature, int[] expectedOffsets)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType type = (DefType)context.SystemModule.GetType(
            "System.Numerics"u8, System.Text.Encoding.UTF8.GetBytes(typeName));

        MethodSignature signature = MakeStaticVoidSignature(
            context, context.GetWellKnownType(WellKnownType.Int64), type,
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        Assert.Equal(expectedSignature, WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString);
        Assert.Equal(expectedOffsets, GetArgumentOffsets(context, signature));
    }

    /// <summary>
    /// The legacy <see cref="decimal"/> is also 16 bytes but only 8-byte aligned and not intrinsic,
    /// so it must keep the by-reference struct ABI rather than being split.
    /// </summary>
    [Fact]
    public void LegacyDecimalStaysByReference()
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType type = (DefType)context.SystemModule.GetType("System"u8, "Decimal"u8);

        Assert.Equal(16, type.InstanceFieldSize.AsInt);
        Assert.Equal(8, type.InstanceFieldAlignment.AsInt);
        Assert.False(WasmLowering.TryGetMultiSegmentLayout(type, out _, out _));

        MethodSignature signature = MakeStaticVoidSignature(
            context, context.GetWellKnownType(WellKnownType.Int64), type,
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

        Assert.Equal("vlS16ip", WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString);
    }

    /// <summary>
    /// An elevated encoding records layout, not identity, so raising resolves one stand-in for
    /// every type that spells it. That is sound only while the stand-in has the same size and
    /// alignment, since the thunk's frame layout is derived from the raised signature, and only
    /// while the stand-in lowers back to the encoding it came from -- otherwise the two sides of
    /// a thunk would key it differently.
    /// </summary>
    [Theory]
    [InlineData(Int128Type)]
    [InlineData(UInt128Type)]
    [InlineData(Decimal128Type)]
    [InlineData(Vector256OfT)]
    [InlineData(Vector512OfT)]
    public void RaisingResolvesALayoutEquivalentStandIn(string typeName)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType multiSlot = InstantiateMultiSlotType(context, typeName);

        WasmSignature lowered = WasmLowering.GetSignature(
            MakeStaticVoidSignature(context, multiSlot), WasmLowering.LoweringFlags.None);
        var raised = (DefType)WasmLowering.RaiseSignature(lowered, context)[0];

        Assert.Equal(multiSlot.InstanceFieldSize.AsInt, raised.InstanceFieldSize.AsInt);
        Assert.Equal(multiSlot.InstanceFieldAlignment.AsInt, raised.InstanceFieldAlignment.AsInt);
        Assert.Equal(
            lowered.SignatureString,
            WasmLowering.GetSignature(
                MakeStaticVoidSignature(context, raised), WasmLowering.LoweringFlags.None).SignatureString);
    }

    /// <summary>
    /// <see cref="System.Int128"/>, <see cref="System.UInt128"/> and
    /// <see cref="System.Numerics.Decimal128"/> are all 16 bytes, 16-byte aligned and two i64
    /// slots, so they share the <c>l2</c> encoding and a thunk built for one serves the others.
    /// Pinned here because that sharing is only sound while their frame layouts agree.
    /// </summary>
    [Fact]
    public void SixteenByteTwoSlotTypesShareAnEncodingAndALayout()
    {
        ReadyToRunCompilerContext context = CreateWasmContext();

        foreach (string typeName in new[] { Int128Type, UInt128Type, Decimal128Type })
        {
            MethodSignature signature = MakeStaticVoidSignature(
                context,
                context.GetWellKnownType(WellKnownType.Int64),
                InstantiateMultiSlotType(context, typeName),
                context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());

            Assert.Equal(
                "vll2ip",
                WasmLowering.GetSignature(signature, WasmLowering.LoweringFlags.None).SignatureString);
            Assert.Equal(new[] { 0, 16, 32 }, GetArgumentOffsets(context, signature));
        }
    }


    /// <summary>
    /// A single-field struct is passed as the type it wraps, so one holding a <c>v128</c> encodes
    /// as <c>V</c> and lays out exactly like the vector itself, and one holding a multi-slot type
    /// splits into the same slots. Adding a second field takes that away: the struct is then an
    /// ordinary aggregate and keeps the by-reference ABI at its own size.
    /// </summary>
    [Theory]
    [InlineData(Vector128OfT, "vlVip")]
    [InlineData(Vector256OfT, "vlV2ip")]
    [InlineData(Vector512OfT, "vlV4ip")]
    public void SingleFieldStructIsPassedAsTheTypeItWraps(string typeName, string expectedSignature)
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType wrapped = InstantiateVector(context, typeName, WellKnownType.Int32);
        DefType wrapper = MakeValueTuple(context, wrapped);

        Assert.Equal(wrapped.InstanceFieldSize.AsInt, wrapper.InstanceFieldSize.AsInt);
        Assert.Equal(expectedSignature, SignatureOf(context, wrapped));
        Assert.Equal(expectedSignature, SignatureOf(context, wrapper));
        Assert.Equal(OffsetsOf(context, wrapped), OffsetsOf(context, wrapper));

        // A second field makes it an ordinary aggregate, so it goes back to the struct ABI.
        DefType twoFields = MakeValueTuple(context, wrapped, wrapped);
        Assert.Equal($"vlS{twoFields.InstanceFieldSize.AsInt}ip", SignatureOf(context, twoFields));
    }

    /// <summary>
    /// The wrapper rule is the same for <see cref="System.Int128"/>, which is what lets a struct
    /// holding one keep the slot form rather than falling back to <c>S&lt;N&gt;</c>.
    /// </summary>
    [Fact]
    public void SingleFieldStructWrappingAMultiSlotScalarKeepsItsSlots()
    {
        ReadyToRunCompilerContext context = CreateWasmContext();
        DefType int128 = (DefType)context.SystemModule.GetType("System"u8, "Int128"u8);
        DefType wrapper = MakeValueTuple(context, int128);

        Assert.Equal("vll2ip", SignatureOf(context, wrapper));
        Assert.Equal(OffsetsOf(context, int128), OffsetsOf(context, wrapper));
    }

    private static DefType MakeValueTuple(ReadyToRunCompilerContext context, params TypeDesc[] fields) =>
        ((MetadataType)context.SystemModule.GetType(
            "System"u8, System.Text.Encoding.UTF8.GetBytes($"ValueTuple`{fields.Length}")))
                .MakeInstantiatedType(new Instantiation(fields));

    /// <summary>
    /// Lowers <c>static void M(long, T, ref int)</c>, the shape these tests use to show where an
    /// argument lands relative to neighbours that pin the alignment either side of it.
    /// </summary>
    private static string SignatureOf(ReadyToRunCompilerContext context, TypeDesc type) =>
        WasmLowering.GetSignature(MakeProbeSignature(context, type), WasmLowering.LoweringFlags.None).SignatureString;

    private static List<int> OffsetsOf(ReadyToRunCompilerContext context, TypeDesc type) =>
        GetArgumentOffsets(context, MakeProbeSignature(context, type));

    private static MethodSignature MakeProbeSignature(ReadyToRunCompilerContext context, TypeDesc type) =>
        MakeStaticVoidSignature(
            context,
            context.GetWellKnownType(WellKnownType.Int64),
            type,
            context.GetWellKnownType(WellKnownType.Int32).MakeByRefType());


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

    /// <summary>
    /// Builds an ordinary multi-field struct of the given size with only 8-byte alignment, as a
    /// stand-in for a type that must keep the by-reference struct ABI.
    /// </summary>
    private static DefType MakeAlignedEightBlob(ReadyToRunCompilerContext context, int size)
    {
        TypeDesc int64 = context.GetWellKnownType(WellKnownType.Int64);

        DefType Tuple(string arity, params TypeDesc[] args) =>
            ((MetadataType)context.SystemModule.GetType("System"u8, System.Text.Encoding.UTF8.GetBytes(arity)))
                .MakeInstantiatedType(new Instantiation(args));

        DefType blob32 = Tuple("ValueTuple`4", int64, int64, int64, int64);

        DefType result = size switch
        {
            16 => Tuple("ValueTuple`2", int64, int64),
            32 => blob32,
            64 => Tuple("ValueTuple`2", blob32, blob32),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
        };

        Assert.Equal(size, result.InstanceFieldSize.AsInt);
        Assert.Equal(8, result.InstanceFieldAlignment.AsInt);
        return result;
    }

    private static DefType InstantiateMultiSlotType(ReadyToRunCompilerContext context, string typeName) =>
        typeName switch
        {
            Int128Type or UInt128Type =>
                context.SystemModule.GetType("System"u8, System.Text.Encoding.UTF8.GetBytes(typeName)),
            Decimal128Type => context.SystemModule.GetType("System.Numerics"u8, "Decimal128"u8),
            _ => InstantiateVector(context, typeName, WellKnownType.Int32),
        };

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
