// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal record struct StressLogData(
    uint LoggedFacilities,
    uint Level,
    uint MaxSizePerThread,
    uint MaxSizeTotal,
    int TotalChunks,
    ulong TickFrequency,
    ulong StartTimestamp,
    TargetPointer Logs);

internal record struct ThreadStressLogData(
    TargetPointer NextPointer,
    ulong ThreadId,
    bool WriteHasWrapped,
    TargetPointer CurrentPointer,
    TargetPointer ChunkListHead,
    TargetPointer ChunkListTail,
    TargetPointer CurrentWriteChunk);

internal record struct StressMsgData(
    uint Facility,
    TargetPointer FormatString,
    ulong Timestamp,
    IReadOnlyList<TargetPointer> Args);

internal interface IStressLog : IContract
{
    static string IContract.Name { get; } = nameof(StressLog);
    static IContract IContract.Create(Target target, int version)
    {
        return version switch
        {
            0 => new StressLog_0(target),
            1 => new StressLog_1(target),
            2 => new StressLog_2(target),
            _ => default(StressLog),
        };
    }

    public virtual bool HasStressLog() => throw new NotImplementedException();
    public virtual StressLogData GetStressLogData() => throw new NotImplementedException();
    public virtual IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer Logs) => throw new NotImplementedException();
    public virtual IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog) => throw new NotImplementedException();
}

file readonly struct StressLog : IStressLog
{
    // Everything throws NotImplementedException
}

#pragma warning disable CS9107 // Parameter 'Target target' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
// Shared portions of the contract for versions 0 through 2
file abstract class StressLog_0_2(Target target) : IStressLog
{
    private static bool StressLogChunkValid(Data.StressLogChunk chunk)
    {
        return chunk.Sig1 == 0xCFCFCFCF && chunk.Sig2 == 0xCFCFCFCF;
    }

    public bool HasStressLog() => target.ReadGlobal<byte>(Constants.Globals.StressLogEnabled) != 0;

    public StressLogData GetStressLogData()
    {
        if (!HasStressLog())
        {
            return default;
        }

        Data.StressLog stressLog = target.ProcessedData.GetOrAdd<Data.StressLog>(target.ReadGlobalPointer(Constants.Globals.StressLog));
        return new StressLogData(
            stressLog.LoggedFacilities,
            stressLog.Level,
            stressLog.MaxSizePerThread,
            stressLog.MaxSizeTotal,
            stressLog.TotalChunks,
            stressLog.TickFrequency,
            stressLog.StartTimestamp,
            stressLog.Logs);
    }

    public IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer Logs)
    {
        TargetPointer currentPointer = Logs;
        while (currentPointer != TargetPointer.Null)
        {
            Data.ThreadStressLog threadStressLog = target.ProcessedData.GetOrAdd<Data.ThreadStressLog>(currentPointer);

            if (threadStressLog.ChunkListHead == TargetPointer.Null)
            {
                // If the chunk list head is null, this thread log isn't valid.
                continue;
            }

            if (threadStressLog.CurrentWriteChunk == TargetPointer.Null)
            {
                // If the current write chunk is null, this thread log isn't valid.
                continue;
            }

            Data.StressLogChunk currentChunkData = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(threadStressLog.CurrentWriteChunk);
            if (!StressLogChunkValid(currentChunkData))
            {
                // If the current write chunk isn't valid, this thread log isn't valid.
                continue;
            }

            yield return new ThreadStressLogData(
                threadStressLog.Next,
                threadStressLog.ThreadId,
                threadStressLog.WriteHasWrapped,
                threadStressLog.CurrentPtr,
                threadStressLog.ChunkListHead,
                threadStressLog.ChunkListTail,
                threadStressLog.CurrentWriteChunk);

            currentPointer = threadStressLog.Next;
        }
    }

    protected abstract StressMsgData GetStressMsgData(Data.StressMsg msg);

    public IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog)
    {
        uint stressMsgHeaderSize = target.GetTypeInfo(DataType.StressMsgHeader).Size!.Value;
        uint pointerSize = target.GetTypeInfo(DataType.pointer).Size!.Value;

        Data.StressLogChunk currentChunkData = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(threadLog.CurrentWriteChunk);
        TargetPointer currentReadChunk = threadLog.CurrentWriteChunk;
        TargetPointer readPointer = threadLog.CurrentPointer;
        bool readHasWrapped = false;
        uint chunkSize = target.ReadGlobal<uint>(Constants.Globals.StressLogChunkSize);

        TargetPointer currentPointer = threadLog.CurrentPointer;
        // the last written log, if it wrapped around may have partially overwritten
        // a previous record.  Update currentPointer to reflect the last safe beginning of a record,
        // but currentPointer shouldn't wrap around, otherwise it'll break our assumptions about stress
        // log
        currentPointer = new TargetPointer((ulong)currentPointer - target.ReadGlobal<ulong>(Constants.Globals.StressLogMaxMessageSize));
        if (currentPointer.Value < currentChunkData.Buf.Value)
        {
            currentPointer = currentChunkData.Buf;
        }

        while (true)
        {
            if (readPointer.Value >= currentChunkData.Buf.Value + chunkSize)
            {
                if (currentReadChunk == threadLog.ChunkListTail)
                {
                    if (!threadLog.WriteHasWrapped)
                    {
                        // If we wrapped around and writing never wrapped,
                        // we've read the whole log.
                        break;
                    }
                    readHasWrapped = true;
                }

                do
                {
                    currentReadChunk = currentChunkData.Next;
                    currentChunkData = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(currentReadChunk);
                } while (!StressLogChunkValid(currentChunkData));

                TargetPointer p = currentChunkData.Buf;
                // StressLog writes variable-sized payloads starting from the end of a chunk.
                // Chunks are zero-initialized, so advance until we find any data,
                // ensuring we don't advance more than a full message.
                while (target.ReadPointer(p) == TargetPointer.Null
                    && p - currentChunkData.Buf < target.ReadGlobal<ulong>(Constants.Globals.StressLogMaxMessageSize))
                {
                    p = new TargetPointer((ulong)p + pointerSize);
                }

                if (target.ReadPointer(p) == TargetPointer.Null)
                {
                    // If we didn't find a message before we read a whole message size,
                    // we're done.
                    // This can occur when the chunk was allocated, but no messages were written before dumping the log.
                    break;
                }
            }

            // Check if we've read all messages in this thread log.
            if (readHasWrapped
                && currentReadChunk == threadLog.CurrentWriteChunk
                && readPointer > currentPointer)
            {
                // We've read all of the entries in the log,
                // wrapped to the start, of the chunk list,
                // and read up to the current write pointer.
                // So we've read all messages.
                break;
            }

            // Read the message and return it to the caller.
            Data.StressMsg message = target.ProcessedData.GetOrAdd<Data.StressMsg>(readPointer);
            StressMsgData parsedMessage = GetStressMsgData(message);
            yield return parsedMessage;

            // Advance the read pointer
            // We'll check if we passed the end of the chunk at the start of the loop.
            readPointer = new TargetPointer((ulong)readPointer + stressMsgHeaderSize + pointerSize * (uint)parsedMessage.Args.Count);
        }
    }
}

file sealed class StressLog_0(Target target) : StressLog_0_2(target)
{
    protected override StressMsgData GetStressMsgData(Data.StressMsg msg)
    {
        Data.StressLog stressLog = target.ProcessedData.GetOrAdd<Data.StressLog>(target.ReadGlobalPointer(Constants.Globals.StressLog));
        uint pointerSize = target.GetTypeInfo(DataType.pointer).Size!.Value;
        uint payload = target.Read<uint>(msg.Header);
        int numArgs = (int)((payload & 0x7) | ((payload >> 29) & 0x7));
        var args = new TargetPointer[numArgs];
        for (int i = 0; i < numArgs; i++)
        {
            args[i] = target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
        }

        return new StressMsgData(
            Facility: target.Read<uint>((ulong)msg.Header + 4),
            FormatString: new TargetPointer(stressLog.ModuleOffset.Value + ((payload >> 3) & ((1 << 26) - 1))),
            Timestamp: target.Read<ulong>((ulong)msg.Header + 8),
            Args: args);
    }
}

file sealed class StressLog_1(Target target) : StressLog_0_2(target)
{
    protected override StressMsgData GetStressMsgData(Data.StressMsg msg)
    {
        uint pointerSize = target.GetTypeInfo(DataType.pointer).Size!.Value;
        uint payload = target.Read<uint>(msg.Header);
        int numArgs = (int)((payload & 0x7) | ((payload >> 29) & 0x7));
        var args = new TargetPointer[numArgs];
        for (int i = 0; i < numArgs; i++)
        {
            args[i] = target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
        }

        ulong formatOffset = ((payload >> 3) & ((1 << 26) - 1));

        TargetPointer formatString = TargetPointer.Null;
        ulong cumulativeOffset = 0;

        TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
        uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
        uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
        for (uint i = 0; i < maxModules; ++i)
        {
            Data.StressLogModuleDesc module = target.ProcessedData.GetOrAdd<Data.StressLogModuleDesc>(moduleTable + i * moduleEntrySize);
            ulong relativeOffset = formatOffset - cumulativeOffset;
            if (relativeOffset < module.Size.Value)
            {
                formatString = new TargetPointer((ulong)module.BaseAddress + relativeOffset);
                break;
            }
            cumulativeOffset += module.Size.Value;
        }

        return new StressMsgData(
            Facility: target.Read<uint>((ulong)msg.Header + 4),
            FormatString: formatString,
            Timestamp: target.Read<ulong>((ulong)msg.Header + 8),
            Args: args);
    }
}

file sealed class StressLog_2(Target target): StressLog_0_2(target)
{
    protected override StressMsgData GetStressMsgData(Data.StressMsg msg)
    {
        uint pointerSize = target.GetTypeInfo(DataType.pointer).Size!.Value;

        ulong payload1 = target.Read<ulong>(msg.Header);
        ulong payload2 = target.Read<ulong>((ulong)msg.Header + 8);
        int numArgs = (int)((payload1 >> 32) & ((1 << 6) - 1));
        var args = new TargetPointer[numArgs];
        for (int i = 0; i < numArgs; i++)
        {
            args[i] = target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
        }
        ulong formatOffset = (payload1 & ((1 << 26) - 1) | ((payload2 & ((1 << 13) - 1)) << 26));

        TargetPointer formatString = TargetPointer.Null;
        ulong cumulativeOffset = 0;

        TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
        uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
        uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
        for (uint i = 0; i < maxModules; ++i)
        {
            Data.StressLogModuleDesc module = target.ProcessedData.GetOrAdd<Data.StressLogModuleDesc>(moduleTable + i * moduleEntrySize);
            ulong relativeOffset = formatOffset - cumulativeOffset;
            if (relativeOffset < module.Size.Value)
            {
                formatString = new TargetPointer((ulong)module.BaseAddress + relativeOffset);
                break;
            }
            cumulativeOffset += module.Size.Value;
        }

        return new StressMsgData(
            Facility: (uint)payload1,
            FormatString: formatString,
            Timestamp: payload2 >> 13,
            Args: args);
    }
}
