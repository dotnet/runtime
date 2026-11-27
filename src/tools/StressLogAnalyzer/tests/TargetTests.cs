// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace StressLogAnalyzer.Tests;

public class TargetTests
{
    private const ulong MemoryBase = 0x10000000;
    private const ulong ModuleBase = 0x20000000;
    private const int ChunkBufferSize = 32768;
    private const uint ValidChunkSignature = 0xCFCFCFCF;
    private const int FormatOffset = 32;
    private const ulong Timestamp = 12345;
    private static readonly int s_headerSize = Marshal.SizeOf<StressLogHeader>();
    private static readonly int s_threadOffset = s_headerSize;
    private static readonly int s_chunkOffset = s_threadOffset + 80;
    private static readonly int s_messageOffset = s_chunkOffset + 16 + ChunkBufferSize - 24;
    private static readonly int s_fileSize = s_chunkOffset + 16 + ChunkBufferSize + 8 + 1;

    [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
    [InlineData(0x00010001u)]
    [InlineData(0x00010002u)]
    public void CreateTarget_DecodesMessage(uint headerVersion)
    {
        WithTarget(headerVersion, ValidChunkSignature, ValidChunkSignature, (target, logs) =>
        {
            IStressLog stressLog = target.Contracts.GetContract<IStressLog>();
            ThreadStressLogData thread = Assert.Single(stressLog.GetThreadStressLogs(logs));
            Assert.Equal(42UL, thread.ThreadId);

            StressMsgData message = Assert.Single(stressLog.GetStressMessages(thread.Address));
            Assert.Equal(1U, message.Facility);
            Assert.Equal(Timestamp, message.Timestamp);
            Assert.Equal(ModuleBase + FormatOffset, message.FormatString.Value);
            Assert.Equal(42UL, Assert.Single(message.Args).Value);

            var formatter = new PrintfStressMessageFormatter(target, new DefaultSpecialPointerFormatter());
            Assert.Equal("descriptor validation value=42", formatter.GetFormattedMessage(message));
        });
    }

    [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
    [InlineData(0x00010001u, true)]
    [InlineData(0x00010001u, false)]
    [InlineData(0x00010002u, true)]
    [InlineData(0x00010002u, false)]
    public void CreateTarget_SkipsInvalidChunk(uint headerVersion, bool invalidFirstSignature)
    {
        WithTarget(headerVersion,
            invalidFirstSignature ? 0 : ValidChunkSignature,
            invalidFirstSignature ? ValidChunkSignature : 0,
            (target, logs) => Assert.Empty(target.Contracts.GetContract<IStressLog>().GetThreadStressLogs(logs)));
    }

    private static void WithTarget(uint headerVersion, uint firstSignature, uint secondSignature, Action<Target, TargetPointer> validate)
    {
        using MemoryMappedFile file = MemoryMappedFile.CreateNew(null, s_fileSize);
        using MemoryMappedViewAccessor accessor = file.CreateViewAccessor();
        WriteLog(accessor, headerVersion, firstSignature, secondSignature);

        var (targetFactory, _, logs) = Program.CreateTarget(accessor.SafeMemoryMappedViewHandle);
        try
        {
            validate(targetFactory(), logs);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    private static void WriteLog(MemoryMappedViewAccessor accessor, uint headerVersion, uint firstSignature, uint secondSignature)
    {
        accessor.Write(HeaderOffset(nameof(StressLogHeader.headerSize)), (long)s_headerSize);
        accessor.WriteArray(HeaderOffset(nameof(StressLogHeader.magic)), "LRTS"u8.ToArray(), 0, 4);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.version)), headerVersion);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.memoryBase)), MemoryBase);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.memoryCur)), MemoryBase + (ulong)s_fileSize);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.memoryLimit)), MemoryBase + (ulong)s_fileSize);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.logs)), MemoryBase + (ulong)s_threadOffset);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.moduleTable)), ModuleBase);
        accessor.Write(HeaderOffset(nameof(StressLogHeader.moduleTable)) + 8, 512UL);
        byte[] format = "descriptor validation value=%d\0"u8.ToArray();
        accessor.WriteArray(HeaderOffset(nameof(StressLogHeader.moduleImageData)) + FormatOffset, format, 0, format.Length);

        accessor.Write(s_threadOffset + 8, 42UL);
        accessor.Write(s_threadOffset + 24, MemoryBase + (ulong)s_messageOffset);
        accessor.Write(s_threadOffset + 40, MemoryBase + (ulong)s_chunkOffset);
        accessor.Write(s_threadOffset + 48, MemoryBase + (ulong)s_chunkOffset);
        accessor.Write(s_threadOffset + 64, MemoryBase + (ulong)s_chunkOffset);

        accessor.Write(s_chunkOffset, MemoryBase + (ulong)s_chunkOffset);
        accessor.Write(s_chunkOffset + 8, MemoryBase + (ulong)s_chunkOffset);
        accessor.Write(s_chunkOffset + 16 + ChunkBufferSize, firstSignature);
        accessor.Write(s_chunkOffset + 20 + ChunkBufferSize, secondSignature);

        // One message with one argument, packed using each header version's bit layout.
        if (headerVersion == 0x00010001)
        {
            accessor.Write(s_messageOffset, (uint)((FormatOffset << 3) | 1));
            accessor.Write(s_messageOffset + 4, 1U);
            accessor.Write(s_messageOffset + 8, Timestamp);
        }
        else
        {
            accessor.Write(s_messageOffset, ((ulong)FormatOffset << 38) | (1UL << 32) | 1);
            accessor.Write(s_messageOffset + 8, Timestamp << 13);
        }
        accessor.Write(s_messageOffset + 16, 42UL);
    }

    private static long HeaderOffset(string fieldName)
        => Marshal.OffsetOf<StressLogHeader>(fieldName).ToInt64();
}
