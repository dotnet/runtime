// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class ExceptionTests
{
    private const int STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE = 0x0001;

    private const ulong ExceptionObjectAddr = 0x0010_0000;
    private const ulong StackTraceObjectAddr = 0x0020_0000;
    private const ulong CombinedArrayAddr = 0x0030_0000;
    private const ulong StackTraceMTAddr = 0x0000_0100;
    private const ulong CombinedArrayMTAddr = 0x0000_0200;

    private static int PointerSize(MockTarget.Architecture arch) => arch.Is64Bit ? sizeof(ulong) : sizeof(uint);

    // Layout of the Exception object fields, ordered as the cDAC descriptor reads them.
    private static int ExStackTraceOffset(MockTarget.Architecture arch) => 2 * PointerSize(arch);
    private static int ExceptionSize(MockTarget.Architecture arch) => 6 * PointerSize(arch) + sizeof(int) + sizeof(int);

    // Array header in front of the I1Array byte payload (MT + NumComponents + alignment pad).
    private static int ArrayHeaderSize(MockTarget.Architecture arch)
    {
        int unaligned = PointerSize(arch) + sizeof(uint);
        int align = PointerSize(arch);
        return (unaligned + align - 1) & ~(align - 1);
    }
    private static int ArrayNumComponentsOffset(MockTarget.Architecture arch) => PointerSize(arch);

    // StackTraceArrayHeader { uint Size; uint m_keepAliveItemsCount; void* m_thread; }
    private static int StackTraceArrayHeaderSize(MockTarget.Architecture arch) => sizeof(uint) + sizeof(uint) + PointerSize(arch);

    // StackTraceElement { void* ip; void* sp; void* pFunc; int flags; + pad }
    private static int StackTraceElementSize(MockTarget.Architecture arch)
    {
        int unaligned = 3 * PointerSize(arch) + sizeof(int);
        int align = PointerSize(arch);
        return (unaligned + align - 1) & ~(align - 1);
    }
    private static int StackTraceElementIpOffset => 0;
    private static int StackTraceElementMethodDescOffset(MockTarget.Architecture arch) => 2 * PointerSize(arch);
    private static int StackTraceElementFlagsOffset(MockTarget.Architecture arch) => 3 * PointerSize(arch);

    private readonly record struct FrameInput(ulong Ip, ulong MethodDesc, int Flags);

    private static Dictionary<DataType, Target.TypeInfo> CreateTypes(MockTarget.Architecture arch)
    {
        int ptr = PointerSize(arch);

        Target.TypeInfo exception = new()
        {
            Size = (uint)ExceptionSize(arch),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["_message"] = new() { Offset = 0 * ptr },
                ["_innerException"] = new() { Offset = 1 * ptr },
                ["_stackTrace"] = new() { Offset = 2 * ptr },
                ["_watsonBuckets"] = new() { Offset = 3 * ptr },
                ["_stackTraceString"] = new() { Offset = 4 * ptr },
                ["_remoteStackTraceString"] = new() { Offset = 5 * ptr },
                ["_HResult"] = new() { Offset = 6 * ptr },
                ["_xcode"] = new() { Offset = 6 * ptr + sizeof(int) },
            },
        };

        Target.TypeInfo array = new()
        {
            Size = (uint)ArrayHeaderSize(arch),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                [Constants.FieldNames.Array.NumComponents] = new() { Offset = ArrayNumComponentsOffset(arch) },
            },
        };

        Target.TypeInfo stackTraceHeader = new()
        {
            Size = (uint)StackTraceArrayHeaderSize(arch),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["Size"] = new() { Offset = 0 },
            },
        };

        Target.TypeInfo stackTraceElement = new()
        {
            Size = (uint)StackTraceElementSize(arch),
            Fields = new Dictionary<string, Target.FieldInfo>
            {
                ["Ip"] = new() { Offset = StackTraceElementIpOffset },
                ["MethodDesc"] = new() { Offset = StackTraceElementMethodDescOffset(arch) },
                ["Flags"] = new() { Offset = StackTraceElementFlagsOffset(arch) },
            },
        };

        return new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.Exception] = exception,
            [DataType.Array] = array,
            [DataType.StackTraceArrayHeader] = stackTraceHeader,
            [DataType.StackTraceElement] = stackTraceElement,
        };
    }

    private static byte[] BuildI1ArrayBytes(MockTarget.Architecture arch, ulong mt, FrameInput[] frames)
    {
        TargetTestHelpers helpers = new(arch);
        int headerSize = ArrayHeaderSize(arch);
        int payloadHeader = StackTraceArrayHeaderSize(arch);
        int elementSize = StackTraceElementSize(arch);
        byte[] data = new byte[headerSize + payloadHeader + frames.Length * elementSize];

        helpers.WritePointer(data.AsSpan(0, PointerSize(arch)), mt);
        helpers.Write(data.AsSpan(ArrayNumComponentsOffset(arch), sizeof(uint)), (uint)(payloadHeader + frames.Length * elementSize));

        int payloadStart = headerSize;
        helpers.Write(data.AsSpan(payloadStart, sizeof(uint)), (uint)frames.Length);

        for (int i = 0; i < frames.Length; i++)
        {
            int elementOffset = payloadStart + payloadHeader + i * elementSize;
            helpers.WritePointer(data.AsSpan(elementOffset + StackTraceElementIpOffset, PointerSize(arch)), frames[i].Ip);
            helpers.WritePointer(data.AsSpan(elementOffset + StackTraceElementMethodDescOffset(arch), PointerSize(arch)), frames[i].MethodDesc);
            helpers.Write(data.AsSpan(elementOffset + StackTraceElementFlagsOffset(arch), sizeof(int)), frames[i].Flags);
        }

        return data;
    }

    private static byte[] BuildExceptionBytes(MockTarget.Architecture arch, ulong stackTrace)
    {
        TargetTestHelpers helpers = new(arch);
        byte[] data = new byte[ExceptionSize(arch)];
        helpers.WritePointer(data.AsSpan(ExStackTraceOffset(arch), PointerSize(arch)), stackTrace);
        return data;
    }

    public enum StackTraceShape
    {
        Null,
        BareI1Array,
        CombinedPtrArray,
    }

    private static IException CreateContract(MockTarget.Architecture arch, StackTraceShape shape, FrameInput[] frames)
    {
        TestPlaceholderTarget.Builder builder = new(arch);
        TargetTestHelpers helpers = new(arch);

        ulong stackTraceFieldValue = shape switch
        {
            StackTraceShape.Null => 0,
            StackTraceShape.BareI1Array => StackTraceObjectAddr,
            StackTraceShape.CombinedPtrArray => CombinedArrayAddr,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };

        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = ExceptionObjectAddr,
            Data = BuildExceptionBytes(arch, stackTraceFieldValue),
            Name = "ExceptionObject",
        });

        Mock<IObject> objectMock = new(MockBehavior.Strict);
        Mock<IRuntimeTypeSystem> rtsMock = new(MockBehavior.Strict);

        if (shape != StackTraceShape.Null)
        {
            // The I1Array byte payload is always present (it carries the header + elements).
            builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = StackTraceObjectAddr,
                Data = BuildI1ArrayBytes(arch, StackTraceMTAddr, frames),
                Name = "I1Array",
            });

            if (shape == StackTraceShape.CombinedPtrArray)
            {
                // Combined object[]: array header + slot 0 holding the I1Array pointer.
                int headerSize = ArrayHeaderSize(arch);
                int ptr = PointerSize(arch);
                byte[] combined = new byte[headerSize + ptr];
                helpers.WritePointer(combined.AsSpan(0, ptr), CombinedArrayMTAddr);
                helpers.Write(combined.AsSpan(ArrayNumComponentsOffset(arch), sizeof(uint)), 1u);
                helpers.WritePointer(combined.AsSpan(headerSize, ptr), StackTraceObjectAddr);
                builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
                {
                    Address = CombinedArrayAddr,
                    Data = combined,
                    Name = "CombinedPtrArray",
                });

                ITypeHandle combinedHandle = new(CombinedArrayMTAddr);
                objectMock.Setup(o => o.GetMethodTableAddress(CombinedArrayAddr)).Returns(CombinedArrayMTAddr);
                rtsMock.Setup(r => r.GetTypeHandle(CombinedArrayMTAddr)).Returns(combinedHandle);
                rtsMock.Setup(r => r.ContainsGCPointers(combinedHandle)).Returns(true);
            }
            else
            {
                ITypeHandle i1Handle = new(StackTraceMTAddr);
                objectMock.Setup(o => o.GetMethodTableAddress(StackTraceObjectAddr)).Returns(StackTraceMTAddr);
                rtsMock.Setup(r => r.GetTypeHandle(StackTraceMTAddr)).Returns(i1Handle);
                rtsMock.Setup(r => r.ContainsGCPointers(i1Handle)).Returns(false);
            }
        }

        TestPlaceholderTarget target = builder
            .AddTypes(CreateTypes(arch))
            .AddMockContract(objectMock)
            .AddMockContract(rtsMock)
            .AddContract<IException>(version: "c1")
            .Build();

        return target.Contracts.Exception;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetExceptionStackFrames_NullStackTrace_ReturnsEmpty(MockTarget.Architecture arch)
    {
        IException contract = CreateContract(arch, StackTraceShape.Null, []);
        Assert.Empty(contract.GetExceptionStackFrames(ExceptionObjectAddr));
    }

    public static IEnumerable<object[]> ShapeArchData =>
        from arch in new MockTarget.StdArch().Select(o => (MockTarget.Architecture)o[0])
        from shape in new[] { StackTraceShape.BareI1Array, StackTraceShape.CombinedPtrArray }
        select new object[] { arch, shape };

    [Theory]
    [MemberData(nameof(ShapeArchData))]
    public void GetExceptionStackFrames_ZeroFrames_ReturnsEmpty(MockTarget.Architecture arch, StackTraceShape shape)
    {
        IException contract = CreateContract(arch, shape, []);
        Assert.Empty(contract.GetExceptionStackFrames(ExceptionObjectAddr));
    }

    [Theory]
    [MemberData(nameof(ShapeArchData))]
    public void GetExceptionStackFrames_MultipleFrames_YieldsFieldValuesInOrder(MockTarget.Architecture arch, StackTraceShape shape)
    {
        FrameInput[] frames =
        [
            new(Ip: 0x1000, MethodDesc: 0xAAA0, Flags: 0),
            new(Ip: 0x2000, MethodDesc: 0xBBB0, Flags: STEF_LAST_FRAME_FROM_FOREIGN_STACK_TRACE),
            new(Ip: 0x3000, MethodDesc: 0xCCC0, Flags: 0x10 /* unrelated bit */),
        ];

        IException contract = CreateContract(arch, shape, frames);
        List<ExceptionStackFrameInfo> result = contract.GetExceptionStackFrames(ExceptionObjectAddr).ToList();

        Assert.Equal(3, result.Count);

        Assert.Equal(new TargetPointer(0x1000), result[0].Ip);
        Assert.Equal(new TargetPointer(0xAAA0), result[0].MethodDesc);
        Assert.False(result[0].IsLastForeignExceptionFrame);

        Assert.Equal(new TargetPointer(0x2000), result[1].Ip);
        Assert.Equal(new TargetPointer(0xBBB0), result[1].MethodDesc);
        Assert.True(result[1].IsLastForeignExceptionFrame);

        Assert.Equal(new TargetPointer(0x3000), result[2].Ip);
        Assert.Equal(new TargetPointer(0xCCC0), result[2].MethodDesc);
        Assert.False(result[2].IsLastForeignExceptionFrame);
    }
}
