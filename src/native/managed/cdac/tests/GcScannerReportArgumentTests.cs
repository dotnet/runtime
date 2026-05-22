// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GcScannerReportArgumentTests
{
    private const ulong TransitionBlockBase = 0x10_0000;

    private static GcScanContext NewContext(TestPlaceholderTarget target)
    {
        GcScanContext ctx = new GcScanContext(target, resolveInteriorPointers: false);
        ctx.UpdateScanContext(new TargetPointer(0x1000), new TargetPointer(0x2000), new TargetPointer(0x3000));
        return ctx;
    }

    private static List<(TargetPointer Address, GcScanFlags Flags)> Run(
        ArgLayout arg,
        IRuntimeTypeSystem rts,
        MockTarget.Architecture arch)
    {
        // Stub reader: every read succeeds and returns zeros. The scanner reads the
        // object pointer from each reported stack slot; we don't care about the value,
        // we only verify the slot ADDRESSES the scanner reports.
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .UseReader((ulong _, Span<byte> buffer) =>
            {
                buffer.Clear();
                return 0;
            })
            .Build();
        GcScanContext ctx = NewContext(target);
        GcScanner.ReportArgument(arg, new TargetPointer(TransitionBlockBase), rts, target.PointerSize, ctx);
        return ctx.StackRefs.Select(r => (r.Address, r.Flags)).ToList();
    }

    private static IRuntimeTypeSystem EmptyRts() => Mock.Of<IRuntimeTypeSystem>();

    private static IRuntimeTypeSystem RtsWithSeries(params (uint Offset, uint Size)[] series)
    {
        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        mock.Setup(r => r.IsByRefLike(It.IsAny<TypeHandle>())).Returns(false);
        mock.Setup(r => r.GetGCDescSeries(It.IsAny<TypeHandle>(), 0u)).Returns(series);
        return mock.Object;
    }

    // ===== reference / interior slots =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void RefSlot_ReportedDirectly(MockTarget.Architecture arch)
    {
        ArgLayout arg = new(IsPassedByRef: false, Slots: new[] { new ArgSlot(0x20, CorElementType.Class) });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x20ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByrefSlot_ReportedAsInterior(MockTarget.Architecture arch)
    {
        ArgLayout arg = new(IsPassedByRef: false, Slots: new[] { new ArgSlot(0x40, CorElementType.Byref) });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x40ul, addr.Value);
        Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, flags);
    }

    // ===== by-implicit-reference value type =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValueTypeByRef_ReportedAsInterior(MockTarget.Architecture arch)
    {
        // Pass-by-implicit-reference structs (e.g. Win-x64 non-power-of-2 structs) carry an
        // interior pointer in their single slot; ValueTypeHandle is null per the Phase 1
        // ComputeValueTypeHandle rule.
        ArgLayout arg = new(IsPassedByRef: true, Slots: new[] { new ArgSlot(0x10, CorElementType.ValueType) });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x10ul, addr.Value);
        Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, flags);
    }

    // ===== decomposed value type (SysV split / HFA) =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void DecomposedValueType_ReportsPerSlotByElementType(MockTarget.Architecture arch)
    {
        // SysV-style split: eight-byte 0 is a ref, eight-byte 1 is a non-ref.
        // ValueTypeHandle is null (Phase 1 rule: any non-ValueType slot ElementType
        // disqualifies the CGCDesc walk because the iterator already did decomposition).
        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[]
            {
                new ArgSlot(0x10, CorElementType.Class),
                new ArgSlot(0x18, CorElementType.I8),
            });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x10ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UndecomposedValueTypeWithoutHandle_EmitsNothing(MockTarget.Architecture arch)
    {
        // A by-value value type that the iterator left undecomposed but for which the
        // Phase 1 producer could not resolve a TypeHandle (e.g. an unresolved TypeSpec
        // or a layout discontinuity). The scanner cannot safely walk an unknown layout
        // so it reports nothing.
        ArgLayout arg = new(IsPassedByRef: false, Slots: new[] { new ArgSlot(0x10, CorElementType.ValueType) });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        Assert.Empty(reports);
    }

    // ===== by-value value type with CGCDesc walk =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByValueValueTypeWithHandle_WalksGcDescAndReportsRefs(MockTarget.Architecture arch)
    {
        int ps = arch.Is64Bit ? 8 : 4;
        // Struct laid out (boxed) as { MT*; objref @ ps; objref @ 2*ps; nonref @ 3*ps }.
        // GetGCDescSeries returns one series: (Offset=ps, Size=2*ps) -- two adjacent refs.
        // Unboxed offsets are 0 and ps. With slots[0].Offset = 0x50, the expected report
        // addresses are TransitionBlockBase + 0x50 and TransitionBlockBase + 0x50 + ps.
        IRuntimeTypeSystem rts = RtsWithSeries(((uint)ps, (uint)(ps * 2)));
        TypeHandle th = new TypeHandle(new TargetPointer(0xAA_BB_CC_00));
        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x50, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        Assert.Equal(2, reports.Count);
        Assert.Equal(TransitionBlockBase + 0x50ul, reports[0].Address.Value);
        Assert.Equal(GcScanFlags.None, reports[0].Flags);
        Assert.Equal(TransitionBlockBase + 0x50ul + (ulong)ps, reports[1].Address.Value);
        Assert.Equal(GcScanFlags.None, reports[1].Flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByValueValueTypeWithHandle_MultiSlotStorage_UsesFirstSlotAsBase(MockTarget.Architecture arch)
    {
        // ARM64 16-byte non-HFA struct passed in X0+X1: two ValueType slots at
        // contiguous offsets. Phase 1 populates ValueTypeHandle because all slots have
        // ElementType=ValueType (the iterator did NOT decompose). The CGCDesc walk uses
        // slots[0].Offset as the base of the contiguous storage.
        int ps = arch.Is64Bit ? 8 : 4;
        IRuntimeTypeSystem rts = RtsWithSeries(((uint)ps, (uint)ps));
        TypeHandle th = new TypeHandle(new TargetPointer(0x12_34_56_78));
        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[]
            {
                new ArgSlot(0x80, CorElementType.ValueType),
                new ArgSlot(0x80 + ps, CorElementType.ValueType),
            },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        // unboxed offset = boxed offset - ps = ps - ps = 0, so the ref is at slots[0].
        Assert.Equal(TransitionBlockBase + 0x80ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByValueValueTypeWithHandle_NoRefs_EmitsNothing(MockTarget.Architecture arch)
    {
        // A struct with no managed refs (empty GCDesc series). The walk runs but yields
        // zero callbacks. Verifies the helper doesn't emit spurious reports.
        IRuntimeTypeSystem rts = RtsWithSeries(System.Array.Empty<(uint, uint)>());
        TypeHandle th = new TypeHandle(new TargetPointer(0xFF_FF_FF_FF));
        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x60, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);
        Assert.Empty(reports);
    }

    // ===== primitive / other =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void PrimitiveSlot_EmitsNothing(MockTarget.Architecture arch)
    {
        ArgLayout arg = new(IsPassedByRef: false, Slots: new[] { new ArgSlot(0x70, CorElementType.I4) });
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        Assert.Empty(reports);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void EmptySlots_EmitsNothing(MockTarget.Architecture arch)
    {
        ArgLayout arg = new(IsPassedByRef: false, Slots: System.Array.Empty<ArgSlot>());
        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, EmptyRts(), arch);
        Assert.Empty(reports);
    }

    // ===== priority: ValueTypeHandle wins over per-slot dispatch =====

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValueTypeHandleBranch_TakesPrecedenceOverPerSlotLoop(MockTarget.Architecture arch)
    {
        // Hypothetical: even if a slot's ElementType were Class, the presence of a
        // ValueTypeHandle indicates the producer wants the GCDesc walk to drive reports.
        // This documents the dispatch order so future producer/consumer changes preserve
        // the invariant.
        int ps = arch.Is64Bit ? 8 : 4;
        IRuntimeTypeSystem rts = RtsWithSeries(((uint)ps, (uint)ps));
        TypeHandle th = new TypeHandle(new TargetPointer(0xAB_CD));
        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x90, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);
        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x90ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    // ===== ByRefLike (Span<T>, ref structs): field-walk branch =====

    private static IRuntimeTypeSystem RtsForByRefLike(
        TypeHandle byRefLikeType,
        params (uint Offset, CorElementType Type, TypeHandle? Inner, bool InnerIsByRefLike, (uint Offset, uint Size)[]? InnerSeries)[] fields)
    {
        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        mock.Setup(r => r.IsByRefLike(byRefLikeType)).Returns(true);

        TargetPointer[] fdPtrs = Enumerable.Range(1, fields.Length)
            .Select(i => new TargetPointer((ulong)i)).ToArray();
        mock.Setup(r => r.EnumerateInstanceFieldDescs(byRefLikeType)).Returns(fdPtrs);

        for (int i = 0; i < fields.Length; i++)
        {
            TargetPointer fdPtr = fdPtrs[i];
            (uint offset, CorElementType et, TypeHandle? inner, bool innerByRefLike, (uint, uint)[]? innerSeries) = fields[i];

            mock.Setup(r => r.GetFieldDescOffset(fdPtr, It.IsAny<System.Reflection.Metadata.FieldDefinition>()))
                .Returns(offset);
            mock.Setup(r => r.GetFieldDescType(fdPtr)).Returns(et);

            if (et == CorElementType.ValueType)
            {
                mock.Setup(r => r.LookupApproxFieldTypeHandle(fdPtr))
                    .Returns(inner ?? default);
                if (inner is { } innerTh)
                {
                    mock.Setup(r => r.IsByRefLike(innerTh)).Returns(innerByRefLike);
                    if (!innerByRefLike && innerSeries is not null)
                    {
                        mock.Setup(r => r.GetGCDescSeries(innerTh, 0u)).Returns(innerSeries);
                    }
                    // For nested ByRefLike: the recursive walker will call
                    // EnumerateInstanceFieldDescs(innerTh) and resolve from there.
                    // Tests covering recursion add those setups explicitly.
                }
            }
        }

        return mock.Object;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_SpanLike_EmitsObjectRefAtOffsetZero(MockTarget.Architecture arch)
    {
        // Shape of Span<object>: { ref byref payload; int length }. The byref payload is
        // GC_CALL_INTERIOR; the length is a primitive (skipped).
        TypeHandle spanTh = new TypeHandle(new TargetPointer(0x1_0000));
        IRuntimeTypeSystem rts = RtsForByRefLike(spanTh,
            (Offset: 0, Type: CorElementType.Byref, Inner: null, InnerIsByRefLike: false, InnerSeries: null),
            (Offset: 8, Type: CorElementType.I4, Inner: null, InnerIsByRefLike: false, InnerSeries: null));

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x100, CorElementType.ValueType) },
            ValueTypeHandle: spanTh);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x100ul, addr.Value);
        Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_ObjectRefField_EmitsAsRoot(MockTarget.Architecture arch)
    {
        // ref struct { object x; } - object refs emit None (regular ref).
        TypeHandle th = new TypeHandle(new TargetPointer(0x2_0000));
        IRuntimeTypeSystem rts = RtsForByRefLike(th,
            (Offset: 16, Type: CorElementType.Class, Inner: null, InnerIsByRefLike: false, InnerSeries: null));

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x200, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x200ul + 16ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_MixedFields_EmitsExpectedRootsInOrder(MockTarget.Architecture arch)
    {
        // ref struct { object obj; ref int rr; int prim; }. Expect 2 roots:
        //   - obj at +0 with None
        //   - rr at +ps with GC_CALL_INTERIOR
        // prim is skipped. Order matches field-list order.
        int ps = arch.Is64Bit ? 8 : 4;
        TypeHandle th = new TypeHandle(new TargetPointer(0x3_0000));
        IRuntimeTypeSystem rts = RtsForByRefLike(th,
            (Offset: 0, Type: CorElementType.Class, Inner: null, InnerIsByRefLike: false, InnerSeries: null),
            (Offset: (uint)ps, Type: CorElementType.Byref, Inner: null, InnerIsByRefLike: false, InnerSeries: null),
            (Offset: (uint)(ps * 2), Type: CorElementType.I4, Inner: null, InnerIsByRefLike: false, InnerSeries: null));

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x300, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        Assert.Equal(2, reports.Count);
        Assert.Equal(TransitionBlockBase + 0x300ul, reports[0].Address.Value);
        Assert.Equal(GcScanFlags.None, reports[0].Flags);
        Assert.Equal(TransitionBlockBase + 0x300ul + (ulong)ps, reports[1].Address.Value);
        Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, reports[1].Flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_NestedOrdinaryStructField_DelegatesToCgcDescWalk(MockTarget.Architecture arch)
    {
        // ref struct { NormalStruct s; } where NormalStruct (non-ByRefLike) has one
        // object ref at boxed offset = ps -> unboxed offset = 0. Slot offset for the
        // nested field is 0x10 within the ByRefLike. Expect 1 ref report at
        // base + 0x10 + 0.
        int ps = arch.Is64Bit ? 8 : 4;
        TypeHandle outerTh = new TypeHandle(new TargetPointer(0x4_0000));
        TypeHandle innerTh = new TypeHandle(new TargetPointer(0x4_1000));
        IRuntimeTypeSystem rts = RtsForByRefLike(outerTh,
            (Offset: 0x10, Type: CorElementType.ValueType, Inner: innerTh, InnerIsByRefLike: false,
             InnerSeries: new (uint, uint)[] { ((uint)ps, (uint)ps) }));

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x400, CorElementType.ValueType) },
            ValueTypeHandle: outerTh);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x400ul + 0x10ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_NestedByRefLikeField_RecursesAndPreservesOffset(MockTarget.Architecture arch)
    {
        // Outer ref struct { InnerRefStruct s; } where InnerRefStruct has one byref at
        // offset 0. Outer places s at offset 0x20. Expect 1 interior-ref report at
        // base + 0x20.
        TypeHandle outerTh = new TypeHandle(new TargetPointer(0x5_0000));
        TypeHandle innerTh = new TypeHandle(new TargetPointer(0x5_1000));

        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        mock.Setup(r => r.IsByRefLike(outerTh)).Returns(true);
        mock.Setup(r => r.IsByRefLike(innerTh)).Returns(true);

        TargetPointer outerFd = new(1);
        TargetPointer innerFd = new(2);
        mock.Setup(r => r.EnumerateInstanceFieldDescs(outerTh)).Returns(new[] { outerFd });
        mock.Setup(r => r.EnumerateInstanceFieldDescs(innerTh)).Returns(new[] { innerFd });

        mock.Setup(r => r.GetFieldDescOffset(outerFd, It.IsAny<System.Reflection.Metadata.FieldDefinition>())).Returns(0x20u);
        mock.Setup(r => r.GetFieldDescType(outerFd)).Returns(CorElementType.ValueType);
        mock.Setup(r => r.LookupApproxFieldTypeHandle(outerFd)).Returns(innerTh);

        mock.Setup(r => r.GetFieldDescOffset(innerFd, It.IsAny<System.Reflection.Metadata.FieldDefinition>())).Returns(0u);
        mock.Setup(r => r.GetFieldDescType(innerFd)).Returns(CorElementType.Byref);

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x500, CorElementType.ValueType) },
            ValueTypeHandle: outerTh);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, mock.Object, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x500ul + 0x20ul, addr.Value);
        Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, flags);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_NoFields_EmitsNothing(MockTarget.Architecture arch)
    {
        TypeHandle th = new TypeHandle(new TargetPointer(0x6_0000));
        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        mock.Setup(r => r.IsByRefLike(th)).Returns(true);
        mock.Setup(r => r.EnumerateInstanceFieldDescs(th)).Returns(System.Array.Empty<TargetPointer>());

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x600, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, mock.Object, arch);
        Assert.Empty(reports);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ByRefLike_NestedValueTypeWithUnresolvedInner_SkipsThatField(MockTarget.Architecture arch)
    {
        // ref struct { object obj; ValueType missing; } where the second field's inner
        // TypeHandle can't be resolved (LookupApproxFieldTypeHandle returns default).
        // The walker should emit the first field's root and skip the second, matching
        // the native DAC's conservative behavior.
        TypeHandle th = new TypeHandle(new TargetPointer(0x7_0000));
        IRuntimeTypeSystem rts = RtsForByRefLike(th,
            (Offset: 0, Type: CorElementType.Class, Inner: null, InnerIsByRefLike: false, InnerSeries: null),
            (Offset: 8, Type: CorElementType.ValueType, Inner: default(TypeHandle), InnerIsByRefLike: false, InnerSeries: null));

        ArgLayout arg = new(
            IsPassedByRef: false,
            Slots: new[] { new ArgSlot(0x700, CorElementType.ValueType) },
            ValueTypeHandle: th);

        List<(TargetPointer Address, GcScanFlags Flags)> reports = Run(arg, rts, arch);

        (TargetPointer addr, GcScanFlags flags) = Assert.Single(reports);
        Assert.Equal(TransitionBlockBase + 0x700ul, addr.Value);
        Assert.Equal(GcScanFlags.None, flags);
    }
}
