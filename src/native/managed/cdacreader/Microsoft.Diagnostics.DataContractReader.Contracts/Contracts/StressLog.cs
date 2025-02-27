// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class StressLogFactory : IContractFactory<IStressLog>
{
    public IStressLog CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new StressLog_1(target),
            2 => new StressLog_2(target),
            _ => default(StressLog),
        };
    }
}

file interface IStressMessageReader
{
    StressMsgData GetStressMsgData(Data.StressMsg msg, Func<ulong, TargetPointer> getFormatPointerFromOffset);
}

file sealed class StressLogTraversal(Target target, IStressMessageReader messageReader)
{
    private bool StressLogChunkValid(Data.StressLogChunk chunk)
    {
        uint validSig = target.ReadGlobal<uint>(Constants.Globals.StressLogValidChunkSig);
        return chunk.Sig1 == validSig && chunk.Sig2 == validSig;
    }

    public bool HasStressLog() => target.ReadGlobal<byte>(Constants.Globals.StressLogEnabled) != 0;

    public StressLogData GetStressLogData()
    {
        if (!HasStressLog())
        {
            return default;
        }

        return GetStressLogData(target.ReadGlobalPointer(Constants.Globals.StressLog));
    }

    public StressLogData GetStressLogData(TargetPointer stressLogPointer)
    {
        Data.StressLog stressLog = target.ProcessedData.GetOrAdd<Data.StressLog>(stressLogPointer);
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
                currentPointer = threadStressLog.Next;
                continue;
            }

            if (threadStressLog.CurrentWriteChunk == TargetPointer.Null)
            {
                // If the current write chunk is null, this thread log isn't valid.
                currentPointer = threadStressLog.Next;
                continue;
            }

            Data.StressLogChunk currentChunkData = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(threadStressLog.CurrentWriteChunk);
            if (!StressLogChunkValid(currentChunkData))
            {
                // If the current write chunk isn't valid, this thread log isn't valid.
                currentPointer = threadStressLog.Next;
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

    private TargetPointer GetFormatPointer(ulong formatOffset)
    {
        if (target.ReadGlobal<byte>(Constants.Globals.StressLogHasModuleTable) == 0)
        {
            Data.StressLog stressLog = target.ProcessedData.GetOrAdd<Data.StressLog>(target.ReadGlobalPointer(Constants.Globals.StressLog));
            return new TargetPointer(stressLog.ModuleOffset.Value + formatOffset);
        }

        TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
        uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
        uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
        ulong cumulativeOffset = 0;
        for (uint i = 0; i < maxModules; ++i)
        {
            Data.StressLogModuleDesc module = target.ProcessedData.GetOrAdd<Data.StressLogModuleDesc>(moduleTable + i * moduleEntrySize);
            ulong relativeOffset = formatOffset - cumulativeOffset;
            if (relativeOffset < module.Size.Value)
            {
                return new TargetPointer((ulong)module.BaseAddress + relativeOffset);
            }
            cumulativeOffset += module.Size.Value;
        }

        return TargetPointer.Null;
    }

    public IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog)
    {
        uint stressMsgHeaderSize = target.GetTypeInfo(DataType.StressMsgHeader).Size!.Value;
        uint pointerSize = (uint)target.PointerSize;

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

                // If we found a non-null value, then that's the start of the first message of the chunk.
                readPointer = p;
            }

            // Check if we've read all messages in this thread log.
            if (readHasWrapped
                && currentReadChunk == threadLog.CurrentWriteChunk
                && readPointer > currentPointer)
            {
                // We've read all of the entries in the log,
                // wrapped to the start of the chunk list,
                // and read up to the current write pointer.
                // So we've read all messages.
                break;
            }

            // Read the message and return it to the caller.
            Data.StressMsg message = target.ProcessedData.GetOrAdd<Data.StressMsg>(readPointer);
            StressMsgData parsedMessage = messageReader.GetStressMsgData(message, GetFormatPointer);
            yield return parsedMessage;

            // Advance the read pointer
            // We'll check if we passed the end of the chunk at the start of the loop.
            readPointer = new TargetPointer((ulong)readPointer + stressMsgHeaderSize + pointerSize * (uint)parsedMessage.Args.Count);
        }
    }

    public bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer)
    {
        ulong chunkSize = target.GetTypeInfo(DataType.StressLogChunk).Size!.Value;
        StressLogMemory stressLogMemory = target.ProcessedData.GetOrAdd<StressLogMemory>(stressLog.Logs);
        foreach (TargetPointer chunk in stressLogMemory.Chunks)
        {
            if (pointer >= chunk && pointer < chunk + chunkSize)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class StressLogMemory(IReadOnlyList<TargetPointer> chunks) : Data.IData<StressLogMemory>
    {
        public static StressLogMemory Create(Target target, TargetPointer address)
        {
            List<TargetPointer> chunks = [];
            // Do a simple traversal of the thread stress log list.
            while (address != TargetPointer.Null)
            {
                Data.ThreadStressLog threadLog = target.ProcessedData.GetOrAdd<Data.ThreadStressLog>(address);
                TargetPointer chunkPtr = threadLog.ChunkListHead;

                if (chunkPtr == TargetPointer.Null)
                {
                    address = threadLog.Next;
                    continue;
                }

                do
                {
                    // Record each chunk in the stress log.
                    chunks.Add(chunkPtr);
                    Data.StressLogChunk chunk = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(chunkPtr);
                    chunkPtr = chunk.Next;
                } while (chunkPtr != TargetPointer.Null && chunkPtr != threadLog.ChunkListHead);

                address = threadLog.Next;
            }

            return new StressLogMemory(chunks);
        }

        public IReadOnlyList<TargetPointer> Chunks { get; } = chunks;
    }
}

file sealed class SmallStressMessageReader(Target target) : IStressMessageReader
{
    public StressMsgData GetStressMsgData(Data.StressMsg msg, Func<ulong, TargetPointer> getFormatPointerFromOffset)
    {
        // Message header layout:
        // struct
        // {
        //     uint32_t numberOfArgsLow  : 3;
        //     uint32_t formatOffset  : 26;
        //     uint32_t numberOfArgsHigh : 3;
        //     uint32_t facility;
        //     uint64_t timeStamp;
        // };
        uint pointerSize = (uint)target.PointerSize;
        uint payload = target.Read<uint>(msg.Header);
        int numArgs = (int)((payload & 0x7) | ((payload >> 29) & 0x7));
        var args = new TargetPointer[numArgs];
        for (int i = 0; i < numArgs; i++)
        {
            args[i] = target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
        }

        return new StressMsgData(
            Facility: target.Read<uint>((ulong)msg.Header + 4),
            FormatString: getFormatPointerFromOffset(((payload >> 3) & ((1 << 26) - 1))),
            Timestamp: target.Read<ulong>((ulong)msg.Header + 8),
            Args: args);
    }
}

file sealed class LargeStressMessageReader(Target target) : IStressMessageReader
{
    public StressMsgData GetStressMsgData(Data.StressMsg msg, Func<ulong, TargetPointer> getFormatPointerFromOffset)
    {
        // Message header layout:
        // struct
        // {
        //     static const size_t formatOffsetLowBits = 26;
        //     static const size_t formatOffsetHighBits = 13;
        //
        //     uint64_t facility: 32;
        //     uint64_t numberOfArgs : 6;
        //     uint64_t formatOffsetLow: formatOffsetLowBits;
        //     uint64_t formatOffsetHigh: formatOffsetHighBits;
        //     uint64_t timeStamp: 51;
        // };

        uint pointerSize = (uint)target.PointerSize;

        ulong payload1 = target.Read<ulong>(msg.Header);
        ulong payload2 = target.Read<ulong>((ulong)msg.Header + 8);
        int numArgs = (int)((payload1 >> 32) & ((1 << 6) - 1));
        var args = new TargetPointer[numArgs];
        for (int i = 0; i < numArgs; i++)
        {
            args[i] = target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
        }
        ulong formatOffset = ((payload1 >> 38) & ((1 << 26) - 1)) | ((payload2 & ((1ul << 13) - 1)) << 26);

        return new StressMsgData(
            Facility: (uint)payload1,
            FormatString: getFormatPointerFromOffset(formatOffset),
            Timestamp: payload2 >> 13,
            Args: args);
    }
}

file sealed class StressLog_1(Target target) : IStressLog
{
    private readonly StressLogTraversal traversal = new(target, new SmallStressMessageReader(target));

    public bool HasStressLog() => traversal.HasStressLog();
    public StressLogData GetStressLogData() => traversal.GetStressLogData();
    public StressLogData GetStressLogData(TargetPointer stressLog) => traversal.GetStressLogData(stressLog);
    public IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer Logs) => traversal.GetThreadStressLogs(Logs);
    public IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog) => traversal.GetStressMessages(threadLog);
    public bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer) => traversal.IsPointerInStressLog(stressLog, pointer);
}


file sealed class StressLog_2(Target target) : IStressLog
{
    private readonly StressLogTraversal traversal = new(target, new LargeStressMessageReader(target));

    public bool HasStressLog() => traversal.HasStressLog();
    public StressLogData GetStressLogData() => traversal.GetStressLogData();
    public StressLogData GetStressLogData(TargetPointer stressLog) => traversal.GetStressLogData(stressLog);
    public IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer Logs) => traversal.GetThreadStressLogs(Logs);
    public IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog) => traversal.GetStressMessages(threadLog);
    public bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer) => traversal.IsPointerInStressLog(stressLog, pointer);
}
