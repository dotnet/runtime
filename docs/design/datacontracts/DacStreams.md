# Contract DacStreams

This contract is for getting information from the streams embedded into a dump file as it crashes

## APIs of contract

``` csharp
// Return string corresponding to type system data structure if it exists, or null otherwise
string StringFromEEAddress(TargetPointer address);
```

## Version 1

Global variables used
| Global Name | Type | Purpose |
| --- | --- | --- |
| MiniMetaDataBuffAddress | TargetPointer | Identify where the mini metadata stream exists |
| MiniMetaDataBuffMaxSize | uint | Identify where the size of the mini metadata stream |

``` csharp
string StringFromEEAddress(TargetPointer address)
{
    TargetPointer miniMetaDataBuffAddress = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
    uint miniMetaDataBuffMaxSize = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));

    if (miniMetaDataBuffMaxSize < 20)
    {
        // buffer isn't long enough to hold required headers
        return null;
    }

    if (_target.Read<uint>(miniMetaDataBuffAddress) != 0x6d727473)
    {
        // Magic number is incorrect, data is either corrupt, or this is not a crash dump with embedded mini metadata streams
        return null;
    }

    uint totalSize = _target.Read<uint>(miniMetaDataBuffAddress + 0x4);
    if (totalSize > miniMetaDataBuffMaxSize)
    {
        // totalSize is inconsistent with miniMetaDataBuffMaxSize
        return stringToAddress;
    }
    uint countStreams = _target.Read<uint>(miniMetaDataBuffAddress + 0x8);
    if (countStreams != 1)
    {
        // This implementation is only aware of 1 possible stream type, so only 1 can exist
        return stringToAddress;
    }
    uint eeNameSig = _target.Read<uint>(miniMetaDataBuffAddress + 0xC);
    if (eeNameSig != 0x614e4545)
    {
        // name of first stream is not 0x614e4545 == "EENa"
        return stringToAddress;
    }
    uint countNames = _target.Read<uint>(miniMetaDataBuffAddress + 0x10);

    uint currentOffset = 20;

    for (int i = 0; i < countNames; i++)
    {
        if ((currentOffset + _target.PointerSize) > miniMetaDataBuffMaxSize)
            break;
        TargetPointer eeObjectPointer = _target.ReadPointer(miniMetaDataBuffAddress + currentOffset);
        currentOffset += (uint)_target.PointerSize;
        int stringLen = // Compute IndexOf null terminator starting at currentOffset, or -1 if it can't be found within miniMetaDataBuffMaxSize
        if (stringLen == -1)
            break;

        if (eeObjectPointer != address)
        {
            currentOffset += stringLen + 1;
            continue;
        }

        return Encoding.UTF8.GetString(miniMdBuffer.Slice((int)currentOffset, stringLen));
    }

    return null;
}
```
