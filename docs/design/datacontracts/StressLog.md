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
string? GetFormattedMessage(StressMsgData stressMsg);
```

## Versions 0 to 2

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

string? GetFormattedMessage(StressMsgData stressMsg)
{
    if (stressMsg.FormatString == TargetPointer.Null)
    {
        return null;
    }

    StressMessageFormatter formatter = new StressMessageFormatter(Target);
    return formatter.GetFormattedMessage(stressMsg);
}

private sealed class StressMessageFormatter
{
    private record struct PaddingFormat(int Width, char FormatChar);

    private readonly Target _target;

    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _formatActions;
    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _alternateActions;

    public StressMessageFormatter(Target target)
    {
        _target = target;

        _formatActions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pM", FormatMethodDesc },
            { "pT", FormatMethodTable },
            { "pV", FormatVTable },
            { "pK", FormatStackTrace },
            { "s", FormatAsciiString },
            { "hs", FormatAsciiString },
            // "S" is omitted because it is the only specifier that only differs in case from another specifier that we support.
            // We'll normalize it to "ls" before we look up in the table.
            { "ls", FormatUtf16String },
            { "p", FormatHexWithPrefix },
            { "d", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "i", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "x", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "lld", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "lli", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "llu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "llx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "xd", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "xi", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "xu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "xx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "Ix", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64p", FormatHexWithPrefix },
        };

        _alternateActions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "X", FormatHexWithPrefix },
            { "x", FormatHexWithPrefix },
        };
    }

    private static void FormatMethodDesc(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append("(MethodDesc: ");
        FormatHexWithPrefix(ptr, paddingFormat, builder);
        builder.Append(")");
    }

    private static void FormatMethodTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append("(MethodTable: ");
        FormatHexWithPrefix(ptr, paddingFormat, builder);
        builder.Append(")");
    }

    private static void FormatVTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append("(VTable: ");
        FormatHexWithPrefix(ptr, paddingFormat, builder);
        builder.Append(")");
    }

    private static void FormatStackTrace(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append("(IP: ");
        FormatHexWithPrefix(ptr, paddingFormat, builder);
        builder.Append(")");
    }

    private void FormatAsciiString(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(ReadZeroTerminatedString<byte>(ptr, maxLength: 256));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr:x}#)");
        }
    }

    private void FormatUtf16String(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(ReadZeroTerminatedString<char>(ptr, maxLength: 256));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr:x}#)");
        }
    }

    private static void FormatHexWithPrefix(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        if (paddingFormat.FormatChar == '0')
        {
            // We need to subtract 2 from the width to account for the "0x" prefix.
            string format = $"x{Math.Max(paddingFormat.Width - 2, 0)}";
            builder.Append($"0x{ptr.Value.ToString(format)}");
        }
        else
        {
            builder.Append($"0x{ptr.Value:x}".PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
        }
    }

    private static string FormatInteger<T>(TargetPointer value, char format, PaddingFormat paddingFormat)
        where T : INumberBase<T>
    {
        if (paddingFormat.FormatChar == '0')
        {
            return T.CreateTruncating(value.Value).ToString($"{format}{paddingFormat.Width}", formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            return T.CreateTruncating(value.Value).ToString($"{format}", formatProvider: CultureInfo.InvariantCulture).PadLeft(paddingFormat.Width, paddingFormat.FormatChar);
        }
    }

    private string ReadZeroTerminatedString<T>(TargetPointer pointer, int maxLength)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        StringBuilder sb = new();
        for (T ch = _target.Read<T>(pointer);
            ch != T.Zero;
            ch = _target.Read<T>(pointer = new TargetPointer((ulong)pointer + 1)))
        {
            if (sb.Length > maxLength)
            {
                break;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    public string GetFormattedMessage(StressMsgData stressMsg)
    {
        Debug.Assert(stressMsg.FormatString != TargetPointer.Null);
        uint pointerSize = _target.GetTypeInfo(DataType.pointer).Size!.Value;
        TargetPointer nextCharPtr = stressMsg.FormatString;
        string formatString = ReadZeroTerminatedString<byte>(stressMsg.FormatString, maxLength: 256);
        // Normalize '%S' to '%ls' to allow us to use case-insensitive compare for all of the other formats
        // we support.
        formatString = formatString.Replace("%S", "%ls", StringComparison.Ordinal);
        int currentArg = 0;
        int startIndex = 0;
        StringBuilder sb = new();
        while (startIndex < formatString.Length)
        {
            int nextFormatter = formatString.IndexOf('%', startIndex);
            if (nextFormatter == -1)
            {
                sb.Append(formatString.AsSpan()[startIndex..]);
                break;
            }

            sb.Append(formatString.AsSpan()[startIndex..nextFormatter]);

            if (nextFormatter == formatString.Length - 1)
            {
                sb.Append('%');
            }
            else
            {
                startIndex = nextFormatter + 1;
                char operand = formatString[startIndex++];
                if (operand == '%')
                {
                    sb.Append('%');
                    continue;
                }

                var formatActions = _formatActions;

                if (operand == '#')
                {
                    formatActions = _alternateActions;
                    operand = formatString[startIndex++];
                }

                PaddingFormat paddingFormat = new PaddingFormat(0, ' ');

                if (operand == '0')
                {
                    paddingFormat = paddingFormat with { FormatChar = '0' };
                    operand = formatString[startIndex++];
                }

                while (operand > '0' && operand <= '9')
                {
                    paddingFormat = paddingFormat with { Width = paddingFormat.Width * 10 + (operand - '0') };
                    operand = formatString[startIndex++];
                }

                string specifier;

                // Check for width specifiers to form the format specifier we'll look up in the table.
                if (operand == 'l')
                {
                    if (formatString[startIndex++] != 'l')
                    {
                        throw new InvalidOperationException("Unsupported format width specifier 'l'");
                    }
                    else
                    {
                        specifier = "ll" + formatString[startIndex++];
                    }
                }
                else if (operand == 'z')
                {
                    specifier = "z" + formatString[startIndex++];
                }
                else if (operand == 'p')
                {
                    if (formatString[startIndex] is 'M' or 'T' or 'V' or 'K')
                    {
                        specifier = "p" + formatString[startIndex++];
                    }
                    else
                    {
                        specifier = "p";
                    }
                }
                else
                {
                    specifier = operand.ToString();
                }

                if (!formatActions.TryGetValue(specifier, out var action))
                {
                    throw new InvalidOperationException($"Unknown format specifier '{operand}'");
                }

                action(stressMsg.Args[currentArg++], paddingFormat, sb);
            }
        }

        return sb.ToString();
    }
}

```

## Version 0

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
    ulong formatOffset = (payload1 & ((1 << 26) - 1) | ((payload2 & ((1 << 13) - 1)) << 26));

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
