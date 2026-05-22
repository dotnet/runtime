// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class GcRefEnumerationTests
{
    private static IRuntimeTypeSystem MockRtsWithSeries(params (uint Offset, uint Size)[] series)
    {
        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        mock.Setup(r => r.GetGCDescSeries(It.IsAny<TypeHandle>(), 0u))
            .Returns(series);
        return mock.Object;
    }

    private static TypeHandle MakeHandle(ulong address)
        => new TypeHandle(new TargetPointer(address));

    // ===== empty input =====

    [Fact]
    public void EmptySeries_YieldsNoRefs()
    {
        IRuntimeTypeSystem rts = MockRtsWithSeries();
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8).ToArray();
        Assert.Empty(refs);
    }

    // ===== single series, one ref =====

    [Fact]
    public void SingleSeriesOneRef_x64_EmitsOneAddressWithBoxedToUnboxedAdjustment()
    {
        // A struct laid out as { /* MT* at 0..7 */ ; objref at boxed offset 8 } has
        // a single GCDesc series of (Offset=8, Size=8) on x64. Unboxed the same field
        // sits at offset 0, so the emitted address is exactly baseAddress.
        IRuntimeTypeSystem rts = MockRtsWithSeries((Offset: 8, Size: 8));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8).ToArray();
        Assert.Single(refs);
        Assert.Equal(0x2000ul, refs[0].Value);
    }

    [Fact]
    public void SingleSeriesOneRef_x86_EmitsOneAddressWithBoxedToUnboxedAdjustment()
    {
        // Same shape on a 32-bit target: pointerSize=4 means MT* is 4 bytes,
        // boxed offset 4 -> unboxed offset 0.
        IRuntimeTypeSystem rts = MockRtsWithSeries((Offset: 4, Size: 4));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 4).ToArray();
        Assert.Single(refs);
        Assert.Equal(0x2000ul, refs[0].Value);
    }

    // ===== single series, multiple adjacent refs =====

    [Fact]
    public void SingleSeriesTwoAdjacentRefs_x64_EmitsTwoConsecutiveAddresses()
    {
        // A struct with two adjacent objref fields at boxed offsets 8 and 16 produces
        // a single series (Offset=8, Size=16). Unboxed offsets are 0 and 8.
        IRuntimeTypeSystem rts = MockRtsWithSeries((Offset: 8, Size: 16));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8).ToArray();
        Assert.Equal(2, refs.Length);
        Assert.Equal(0x2000ul, refs[0].Value);
        Assert.Equal(0x2008ul, refs[1].Value);
    }

    [Fact]
    public void SingleSeriesFourAdjacentRefs_x86_EmitsFourConsecutiveAddresses()
    {
        IRuntimeTypeSystem rts = MockRtsWithSeries((Offset: 4, Size: 16));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 4).ToArray();
        Assert.Equal(4, refs.Length);
        Assert.Equal(0x2000ul, refs[0].Value);
        Assert.Equal(0x2004ul, refs[1].Value);
        Assert.Equal(0x2008ul, refs[2].Value);
        Assert.Equal(0x200Cul, refs[3].Value);
    }

    // ===== multiple disjoint series =====

    [Fact]
    public void MultipleDisjointSeries_x64_EmitsAllInSeriesOrder()
    {
        // Layout: { objref; long; objref } -> two single-ref series at boxed
        // offsets 8 and 24 (gap of one 8-byte non-ref field between them).
        IRuntimeTypeSystem rts = MockRtsWithSeries(
            (Offset: 8, Size: 8),
            (Offset: 24, Size: 8));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8).ToArray();
        Assert.Equal(2, refs.Length);
        Assert.Equal(0x2000ul, refs[0].Value); // boxed 8 -> unboxed 0
        Assert.Equal(0x2010ul, refs[1].Value); // boxed 24 -> unboxed 16
    }

    [Fact]
    public void MixedRunsAndGaps_x64_EmitsCorrectAddresses()
    {
        // Two series: a 2-ref run at boxed offset 8, then a 1-ref run at boxed offset 40.
        IRuntimeTypeSystem rts = MockRtsWithSeries(
            (Offset: 8, Size: 16),
            (Offset: 40, Size: 8));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8).ToArray();
        Assert.Equal(3, refs.Length);
        Assert.Equal(0x2000ul, refs[0].Value); // boxed 8 -> unboxed 0
        Assert.Equal(0x2008ul, refs[1].Value); // boxed 16 -> unboxed 8
        Assert.Equal(0x2020ul, refs[2].Value); // boxed 40 -> unboxed 32
    }

    // ===== base address arithmetic =====

    [Fact]
    public void BaseAddressIsOffsetCorrectly_NonZeroBase()
    {
        IRuntimeTypeSystem rts = MockRtsWithSeries((Offset: 8, Size: 16));
        TargetPointer[] refs = GcRefEnumeration.EnumerateValueTypeRefs(
            rts, MakeHandle(0x1000), new TargetPointer(0xFFFF_FFFF_FFFF_0000ul), pointerSize: 8).ToArray();
        Assert.Equal(2, refs.Length);
        Assert.Equal(0xFFFF_FFFF_FFFF_0000ul, refs[0].Value);
        Assert.Equal(0xFFFF_FFFF_FFFF_0008ul, refs[1].Value);
    }

    // ===== lazy enumeration =====

    [Fact]
    public void DoesNotCallGetGCDescSeriesUntilEnumerated()
    {
        Mock<IRuntimeTypeSystem> mock = new(MockBehavior.Strict);
        IEnumerable<TargetPointer> result = GcRefEnumeration.EnumerateValueTypeRefs(
            mock.Object, MakeHandle(0x1000), new TargetPointer(0x2000), pointerSize: 8);
        // No setup on the mock -> if the helper ate the iterator eagerly the strict mock
        // would throw. Materializing now must not throw because we set the call up below.
        mock.Setup(r => r.GetGCDescSeries(It.IsAny<TypeHandle>(), 0u))
            .Returns(System.Array.Empty<(uint, uint)>());
        Assert.Empty(result.ToArray());
    }
}
