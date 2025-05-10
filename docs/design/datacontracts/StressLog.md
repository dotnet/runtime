# Contract StressLog

This contract is for reading the stress log of the process.

## APIs of the contract

```csharp
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
```

```csharp
bool HasStressLog();
StressLogData GetStressLogData();
StressLogData GetStressLogData(TargetPointer stressLogPointer);
IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer logs);
IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog);
bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer);
```

## Versions 1 and 2

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| StressLog | LoggedFacilities | Bitmask of facilities that are logged |
| StressLog | Level | Level of logging |
| StressLog | MaxSizePerThread | Maximum size of the log per thread |
| StressLog | MaxSizeTotal | Maximum size of the log |
| StressLog | TotalChunks | Total number of chunks across all thread-specific logs |
| StressLog | TickFrequency | Number of ticks per second for stresslog timestamps |
| StressLog | StartTimestamp | Timestamp when the stress log was started |
| StressLog | ModuleOffset | Offset of the module in the stress log |
| StressLog | Logs | Pointer to the thread-specific logs |
| StressLogModuleDesc | BaseAddress | Base address of the module |
| StressLogModuleDesc | Size | Size of the module |
| ThreadStressLog | Next | Pointer to the next thread-specific log |
| ThreadStressLog | ThreadId | ID of the thread |
| ThreadStressLog | WriteHasWrapped | Whether the write pointer is writing to previously used chunks |
| ThreadStressLog | CurrentPtr | Pointer to the most recently written message |
| ThreadStressLog | ChunkListHead | Pointer to the head of the chunk list |
| ThreadStressLog | ChunkListTail | Pointer to the tail of the chunk list |
| ThreadStressLog | CurrentWriteChunk | Pointer to the chunk currently being written to |
| StressLogChunk | Prev | Pointer to the previous chunk |
| StressLogChunk | Next | Pointer to the next chunk |
| StressLogChunk | Buf | The data stored in the chunk |
| StressLogChunk | Sig1 | First byte of the chunk signature (to ensure validity) |
| StressLogChunk | Sig2 | Second byte of the chunk signature (to ensure validity) |
| StressMsgHeader | Opaque structure | Header of a stress message. Meaning of bits is version-dependent. |
| StressMsg | Header | The message header |
| StressMsg | Args | The arguments of the message (number of arguments specified in the header) |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| StressLogEnabled | byte | Whether the stress log is enabled |
| StressLog | pointer | Pointer to the stress log |
| StressLogChunkSize | uint | Size of a stress log chunk |
| StressLogMaxMessageSize | ulong | Maximum size of a stress log message |
| StressLogHasModuleTable | byte | Whether the stress log module table is present |
| StressLogModuleTable | pointer | Pointer to the stress log's module table (if StressLogHasModuleTable is `1`) |

```csharp
bool HasStressLog()
{
    return Target.ReadGlobal<byte>("StressLogEnabled") != 0;
}

StressLogData GetStressLogData()
{
    if (!HasStressLog())
    {
        return default;
    }

    StressLog stressLog = new StressLog(Target, Target.ReadGlobalPointer(Constants.Globals.StressLog));
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

StressLogData GetStressLogData(TargetPointer stressLogPointer)
{
    StressLog stressLog = new StressLog(Target, stressLogPointer);
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

IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer logs)
{
    TargetPointer currentPointer = logs;
    while (currentPointer != TargetPointer.Null)
    {
        ThreadStressLog threadStressLog = new(Target, currentPointer);

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

        StressLogChunk currentChunkData = new(Target, threadStressLog.CurrentWriteChunk);
        if (currentChunkData.Sig1 != 0xCFCFCFCF || currentChunkData.Sig2 != 0xCFCFCFCF)
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

// Return messages going in reverse chronological order, newest first.
IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog)
{
    // 1. Get the current message pointer from the log and the info about the current chunk the runtime is writing into.
    //    Record our current read pointer as the current message pointer.

    // 2. The last written log, if it wrapped around may have partially overwritten a previous record.
    //    Update our current message pointer to reflect the last safe beginning of a record (StressLogMaxMessageSize before our current message pointer)
    //    without going before the start of the current chunk's buffer. Do not update the current read pointer in this way.

    // 3. If the current read pointer is at the end of the chunk (this will never happen on the first iteration), check if current read pointer is at the end of the chunk list.
    //    Otherwise, skip to step 8.

    // 4. If current chunk is at the end of the chunk list and this thread never wrapped around while writing,
    //    DONE.

    // 5. Otherwise, get the next chunk in the list.
    //    The tail will wrap around to the head if the current chunk at the end of the list. Record if we have wrapped around.

    // 6. StressLog writes variable-sized payloads starting from the end of a chunk.
    //    Chunks are zero-initialized, so look in the first StressLogMaxMessageSize bytes, for any non-0 bytes.
    //    If we find any, that's the start of the first message of the chunk.
    //    Set the current read pointer to that location.

    // 7. If we didn't find a message before we read a whole message size, there's no message in this chunk (it was freshly allocated),
    //    DONE.

    // 8. If we have wrapped around while reading, we are reading in the thread's current write chunk, and our current read pointer is ahead of the current message pointer,
    //    DONE.

    // 9. Read the messsage at the current read pointer.

    // 10. Advance the current read pointer to the next message (advance by "stress message header size + pointer size * number of arguments").

    // 11. Go to step 3.
}

bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer)
{
    // For all chunks in all thread stress logs, check if
    // any pointer-aligned offset in the chunk's data has the value of 'pointer'.
    // If found, return true.
}

// This method is a helper for the various specific versions.
protected TargetPointer GetFormatPointer(ulong formatOffset)
{
    if (Target.ReadGlobal<byte>(Constants.Globals.StressLogHasModuleTable) == 0)
    {
        StressLog stressLog = new(Target, target.ReadGlobalPointer(Constants.Globals.StressLog));
        return new TargetPointer(stressLog.ModuleOffset + formatOffset);
    }

    TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
    uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
    uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
    for (uint i = 0; i < maxModules; ++i)
    {
        StressLogModuleDesc module = new(Target, moduleTable + i * moduleEntrySize);
        ulong relativeOffset = formatOffset - cumulativeOffset;
        if (relativeOffset < module.Size.Value)
        {
            return new TargetPointer((ulong)module.BaseAddress + relativeOffset);
        }
        cumulativeOffset += module.Size.Value;
    }

    return TargetPointer.Null;
}
```

A StressLog message, represented by a `StressMsgData` struct, can be formatted as though the null-terminated UTF-8 string located at `FormatString` is a `printf`-style format string, with all arguments located at `Args`. Additionally, the following special format specifiers are supported:

| Format Specifier | Argument Type | Description |
| --- | --- | --- |
| `%pT` | pointer | A `TypeHandle`, accessible through the `GetTypeHandle` API in the [RuntimeTypeSystem contract](./RuntimeTypeSystem.md), possibly with bits of the `ObjectToMethodTableUnmask` data contract global variable set. |
| `%pM` | pointer | A `MethodDescHandle`, accessible through the `GetMethodDescHandle` API in the [RuntimeTypeSystem contract](./RuntimeTypeSystem.md) |
| `%pV` | pointer | A pointer to an unmanaged symbol in the image. |
| `%pK` | pointer | A pointer to an offset from a symbol in the image, generally representing an IP in a stack trace. |

## Version 1

Version 1 stress logs are included in any .NET runtime version corresponding to an SOS breaking change version of 0, 1, 2, or 3, or a memory-mapped version of `0x00010001`.
SOS breaking change versions of 0, 1, or 2 do not have a module table. SOS breaking change version 3 logs and memory mapped logs have a module table.

These functions implement additional logic required for the shared contract implementation above.

The message header data is stored in the following format:

```c++
struct
{
    uint32_t numberOfArgsLow  : 3;
    uint32_t formatOffset  : 26;
    uint32_t numberOfArgsHigh : 3;
    uint32_t facility;
    uint64_t timeStamp;
};
```

The format offset refers to the offset from the module offset on the stress log.

```csharp
StressMsgData GetStressMsgData(StressMsg msg)
{
    uint pointerSize = Target.GetTypeInfo(DataType.pointer).Size!.Value;
    uint payload = Target.Read<uint>(msg.Header);
    int numArgs = (int)((payload & 0x7) | ((payload >> 29) & 0x7));
    var args = new TargetPointer[numArgs];
    for (int i = 0; i < numArgs; i++)
    {
        args[i] = Target.ReadPointer((ulong)msg.Args + (ulong)(i * pointerSize));
    }

    return new StressMsgData(
        Facility: Target.Read<uint>((ulong)msg.Header + 4),
        FormatString: GetFormatPointer(((payload >> 3) & ((1 << 26) - 1))),
        Timestamp: Target.Read<ulong>((ulong)msg.Header + 8),
        Args: args);
}
```

## Version 2

Version 2 stress logs are included in any .NET runtime version corresponding to an SOS breaking change version of 4 or a memory-mapped version of `0x00010002`.
SOS breaking change version 4 stress logs and memory mapped stress logs will have a module table.

These functions implement additional logic required for the shared contract implementation above.

The message header data is stored in the following format:

```c++
struct
{
    static const size_t formatOffsetLowBits = 26;
    static const size_t formatOffsetHighBits = 13;

    uint64_t facility: 32;
    uint64_t numberOfArgs : 6;
    uint64_t formatOffsetLow: formatOffsetLowBits;
    uint64_t formatOffsetHigh: formatOffsetHighBits;
    uint64_t timeStamp: 51;
};
```

The format offset refers to the cummulative offset into a module referred to in the module table.

```csharp
StressMsgData GetStressMsgData(StressMsg msg)
{
    StressLog stressLog = new(Target, target.ReadGlobalPointer(Constants.Globals.StressLog));
    uint pointerSize = Target.GetTypeInfo(DataType.pointer).Size!.Value;

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
        FormatString: GetFormatPointer(formatOffset),
        Timestamp: payload2 >> 13,
        Args: args);
}
```
