// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Internal.CallingConvention;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

using ArgIterator = Internal.CallingConvention.ArgIterator;
using CallingConventions = Internal.CallingConvention.CallingConventions;
using CorElementType = Internal.CorConstants.CorElementType;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class CallingConventionTests
{
    private static CallingConvention_1 CreateContract(int pointerSize, Mock<IRuntimeTypeSystem> mockRts)
    {
        MockTarget.Architecture arch = new()
        {
            Is64Bit = pointerSize == 8,
            IsLittleEndian = true,
        };
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        targetBuilder.AddMockContract(mockRts);
        TestPlaceholderTarget target = targetBuilder.Build();
        return new CallingConvention_1(target);
    }

    #region EnumerateValueTypeGCRefs tests

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void EnumerateValueTypeGCRefs_NoGCPointers_ReturnsEmpty(int pointerSize)
    {
        TypeHandle typeHandle = new(new TargetPointer(0x1000));
        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(false);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset: 0x20).ToList();

        Assert.Empty(refs);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_SingleRef_64bit()
    {
        const int pointerSize = 8;
        const int argOffset = 0x30;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: (uint)pointerSize, Size: (uint)pointerSize)]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Single(refs);
        Assert.Equal(argOffset, refs[0].Offset);
        Assert.False(refs[0].IsInterior);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_SingleRef_32bit()
    {
        const int pointerSize = 4;
        const int argOffset = 0x18;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: (uint)pointerSize, Size: (uint)pointerSize)]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Single(refs);
        Assert.Equal(argOffset, refs[0].Offset);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_TwoConsecutiveRefs()
    {
        const int pointerSize = 8;
        const int argOffset = 0x40;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: (uint)pointerSize, Size: 2u * (uint)pointerSize)]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Equal(2, refs.Count);
        Assert.Equal(argOffset, refs[0].Offset);
        Assert.Equal(argOffset + pointerSize, refs[1].Offset);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_RefAfterNonRefField()
    {
        const int pointerSize = 8;
        const int argOffset = 0x20;
        const uint gcFieldOffset = 16; // MT* + 8 bytes of non-GC data
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: gcFieldOffset, Size: (uint)pointerSize)]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Single(refs);
        // Unboxed field offset = gcFieldOffset - pointerSize = 8
        Assert.Equal(argOffset + (int)gcFieldOffset - pointerSize, refs[0].Offset);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_MultipleSeries()
    {
        // Struct layout (unboxed): [ref0][int64][ref1]
        const int pointerSize = 8;
        const int argOffset = 0x50;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([
                (Offset: (uint)pointerSize, Size: (uint)pointerSize),       // ref0 at unboxed offset 0
                (Offset: 3u * (uint)pointerSize, Size: (uint)pointerSize),  // ref1 at unboxed offset 16
            ]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Equal(2, refs.Count);
        Assert.Equal(argOffset, refs[0].Offset);                  // ref0 at argOffset + 0
        Assert.Equal(argOffset + 2 * pointerSize, refs[1].Offset); // ref1 at argOffset + 16
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_AllRefsAreNonInterior()
    {
        const int pointerSize = 8;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: (uint)pointerSize, Size: 3u * (uint)pointerSize)]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset: 0).ToList();

        Assert.Equal(3, refs.Count);
        Assert.All(refs, r => Assert.False(r.IsInterior));
        Assert.All(refs, r => Assert.False(r.IsThis));
        Assert.All(refs, r => Assert.False(r.IsParamType));
        Assert.All(refs, r => Assert.False(r.IsPinned));
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_EmptySeries_ReturnsEmpty()
    {
        const int pointerSize = 8;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns(Enumerable.Empty<(uint, uint)>());

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset: 0x10).ToList();

        Assert.Empty(refs);
    }

    [Fact]
    public void EnumerateValueTypeGCRefs_LargeStructWithManyRefs()
    {
        const int pointerSize = 8;
        const int argOffset = 0x60;
        const int refCount = 8;
        TypeHandle typeHandle = new(new TargetPointer(0x1000));

        var mockRts = new Mock<IRuntimeTypeSystem>();
        mockRts.Setup(r => r.ContainsGCPointers(typeHandle)).Returns(true);
        mockRts.Setup(r => r.GetGCDescSeries(typeHandle, 0))
            .Returns([(Offset: (uint)pointerSize, Size: (uint)(refCount * pointerSize))]);

        CallingConvention_1 contract = CreateContract(pointerSize, mockRts);
        List<CallerStackGCRef> refs = contract.EnumerateValueTypeGCRefs(
            mockRts.Object, typeHandle, argOffset).ToList();

        Assert.Equal(refCount, refs.Count);
        for (int i = 0; i < refCount; i++)
        {
            Assert.Equal(argOffset + i * pointerSize, refs[i].Offset);
        }
    }

    #endregion
}
