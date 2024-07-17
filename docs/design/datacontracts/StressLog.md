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
IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer Logs);
IEnumerable<StressMsgData> GetStressMessages(ThreadStressLogData threadLog);
bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer);
```

## Versions 0 to 2

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
| StressLog | Logs | Pointer to the thread-specific logs |
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

IEnumerable<ThreadStressLogData> GetThreadStressLogs(TargetPointer logs)
{
    TargetPointer currentPointer = logs;
    while (currentPointer != TargetPointer.Null)
    {
        ThreadStressLog threadStressLog = new(Target, currentPointer);

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

        StressLogChunk currentChunkData = new(Target, threadStressLog.CurrentWriteChunk);
        if (currentChunkData.Sig1 != 0xCFCFCFCF || currentChunkData.Sig2 != 0xCFCFCFCF)
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

IEnumerable<StressMsgData> GetStressMessages(ThreadStressLog threadLog, uint formatVersion)
{
    uint stressMsgHeaderSize = Target.GetTypeInfo(DataType.StressMsgHeader).Size!.Value;
    uint pointerSize = Target.GetTypeInfo(DataType.pointer).Size!.Value;

    Data.StressLogChunk currentChunkData = new(Target, threadLog.CurrentWriteChunk);
    TargetPointer currentReadChunk = threadLog.CurrentWriteChunk;
    TargetPointer readPointer = threadLog.CurrentPointer;
    bool readHasWrapped = false;
    uint chunkSize = Target.ReadGlobal<uint>(Constants.Globals.StressLogChunkSize);

    TargetPointer currentPointer = threadLog.CurrentPointer;
    // the last written log, if it wrapped around may have partially overwritten
    // a previous record.  Update currentPointer to reflect the last safe beginning of a record,
    // but currentPointer shouldn't wrap around, otherwise it'll break our assumptions about stress
    // log
    currentPointer = new TargetPointer((ulong)currentPointer - Target.ReadGlobal<ulong>(Constants.Globals.StressLogMaxMessageSize));
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
                currentChunkData = new(Target, currentReadChunk);
            } while (currentChunkData.Sig1 != 0xCFCFCFCF || currentChunkData.Sig2 != 0xCFCFCFCF);

            TargetPointer p = currentChunkData.Buf;
            // StressLog writes variable-sized payloads starting from the end of a chunk.
            // Chunks are zero-initialized, so advance until we find any data,
            // ensuring we don't advance more than a full message.
            while (Target.ReadPointer(p) == TargetPointer.Null
                && p - currentChunkData.Buf < Target.ReadGlobal<ulong>(Constants.Globals.StressLogMaxMessageSize))
            {
                p = new TargetPointer((ulong)p + pointerSize);
            }

            if (Target.ReadPointer(p) == TargetPointer.Null)
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
        StressMsg message = new(Target, readPointer);
        StressMsgData parsedMessage = GetStressMsgData(message);
        yield return parsedMessage;

        // Advance the read pointer
        // We'll check if we passed the end of the chunk at the start of the loop.
        readPointer = new TargetPointer((ulong)readPointer + stressMsgHeaderSize + pointerSize * (uint)parsedMessage.Args.Count);
    }
}

bool IsPointerInStressLog(StressLogData stressLog, TargetPointer pointer)
{
    ulong chunckSize = target.GetTypeInfo(DataType.StressLogChunk).Size!.Value;
    foreach (ThreadStressLogData threadLog in GetThreadStressLogs(stressLog.Logs))
    {
        TargetPointer chunkPtr = threadLog.ChunkListHead;
        do
        {
            if (pointer.Value >= chunkPtr.Value && pointer.Value <= chunkPtr.Value + chunckSize)
            {
                return true;
            }

            Data.StressLogChunk chunk = target.ProcessedData.GetOrAdd<Data.StressLogChunk>(chunkPtr);
            chunkPtr = chunk.Next;
        } while (chunkPtr != TargetPointer.Null && chunkPtr != threadLog.ChunkListHead);
    }

    return false;
}
```

A StressLog message, represented by a `StressMsgData` struct, can be formatted as though the null-terminated UTF-8 string located at `FormatString` is a `printf`-style format string, with all arguments located at `Args`. Additionally, the following special format specifiers are supported:

| Format Specifier | Argument Type | Description |
| --- | --- | --- |
| `%pT` | pointer | A `MethodTableHandle` |
| `%pM` | pointer | A `MethodDesc` |
| `%pV` | pointer | A pointer to a virtual method table in the image. |
| `%pK` | pointer | A pointer to an offset from a symbol in the image, generally representing an IP in a stack trace. |

## Version 0

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| StressLog | ModuleOffset | Offset of the module in the stress log |

Version 0 stress logs are included in .NET runtime versions corresponding to an SOS breaking change version of 0, 1, or 2. This stress log has no memory mapped header and no module table.

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
    StressLog stressLog = new(Target, target.ReadGlobalPointer(Constants.Globals.StressLog));
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
        FormatString: new TargetPointer((ulong)stressLog.ModuleOffset + ((payload >> 3) & ((1 << 26) - 1))),
        Timestamp: Target.Read<ulong>((ulong)msg.Header + 8),
        Args: args);
}
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| StressLogModuleDesc | BaseAddress | Base address of the module |
| StressLogModuleDesc | Size | Size of the module |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| StressLogModuleTable | pointer | Pointer to the stress log's module table |

Version 1 stress logs are included in any .NET runtime version corresponding to an SOS breaking change version of 3 or a memory-mapped version of `0x00010001`. This stress log has a module table.

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

The format offset refers to the cummulative offset into a module referred to in the module table.

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

    ulong formatOffset = ((payload >> 3) & ((1 << 26) - 1));

    TargetPointer formatString = TargetPointer.Null;
    ulong cumulativeOffset = 0;

    TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
    uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
    uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
    for (uint i = 0; i < maxModules; ++i)
    {
        StressLogModuleDesc module = new(Target, moduleTable + i * moduleEntrySize);
        ulong relativeOffset = formatOffset - cumulativeOffset;
        if (relativeOffset < module.Size.Value)
        {
            formatString = new TargetPointer((ulong)module.BaseAddress + relativeOffset);
            break;
        }
        cumulativeOffset += module.Size.Value;
    }

    return new StressMsgData(
        Facility: Target.Read<uint>((ulong)msg.Header + 4),
        FormatString: formatString,
        Timestamp: Target.Read<ulong>((ulong)msg.Header + 8),
        Args: args);
}
```

## Version 2

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| StressLogModuleDesc | BaseAddress | Base address of the module |
| StressLogModuleDesc | Size | Size of the module |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| StressLogModuleTable | pointer | Pointer to the stress log's module table |

Version 2 stress logs are included in any .NET runtime version corresponding to an SOS breaking change version of 4 or a memory-mapped version of `0x00010002`. This stress log has a module table.

These functions implement additional logic required for the shared contract implementation above.

The message header data is stored in the following format:

```c++
struct StressMsg
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

    TargetPointer formatString = TargetPointer.Null;
    ulong cumulativeOffset = 0;

    TargetPointer moduleTable = target.ReadGlobalPointer(Constants.Globals.StressLogModuleTable);
    uint moduleEntrySize = target.GetTypeInfo(DataType.StressLogModuleDesc).Size!.Value;
    uint maxModules = target.ReadGlobal<uint>(Constants.Globals.StressLogMaxModules);
    for (uint i = 0; i < maxModules; ++i)
    {
        StressLogModuleDesc module = new(Target, moduleTable + i * moduleEntrySize);
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
```
