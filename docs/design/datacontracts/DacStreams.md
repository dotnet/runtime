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

Magic numbers
| Name | Value |
| --- | --- |
| MiniMetadataSignature | 0x6d727473 |
| EENameStreamSignature | 0x614e4545 |

The format of the MiniMetadataStream begins with a Streams header, which has 3 fields

| Field | Type | Offset | Meaning |
| --- | --- | --- | --- |
| MiniMetadataSignature| uint | 0 | Magic value used to identify that there are streams |
| TotalSize | uint | 4 | Total size of the entire set of MiniMetadata streams including this header |
| Count of Streams | uint | 8 | Number of streams in the MiniMetadata |

The concept is that each stream simply follows the previous stream in the buffer.
There is no padding, so the data is not expected to be aligned within the buffer.
NOTE: At the moment there is only 1 supported stream type, so Count of Streams can only be 1.

The `EENameStream` is structured as a header, plus a series of null-terminated utf8 strings, and pointers.

The EENameStream header
| Field | Type | Offset | Meaning |
| --- | --- | --- | --- |
| EENameStreamSignature | uint | 0 | Magic value used to identify that the bytes immediately following are an `EENameStream` |
| CountOfNames | uint | 4 | Number of names encoded |

EENameStream entry
| Field | Type | Offset | Meaning |
| --- | --- | --- | --- |
| Pointer | pointer | 0 | Pointer to type system structure |
| String | null-terminated UTF-8 sting | 4 or 8 based on target pointer size | Pointer to type system structure |

Following the EENameStream header, there are CountOfNames entries. Each entry begins with a target pointer sized block which identifies a particular type system data structure, followed by a utf8 encoded null-terminated string.

``` csharp
string StringFromEEAddress(TargetPointer address)
{
    TargetPointer miniMetaDataBuffAddress = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
    uint miniMetaDataBuffMaxSize = _target.Read<uint>(_target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));

    // Parse MiniMetadataStream according the the format described above to produce a dictionary from pointer to string from the EENameStream.
    // Then lookup in the dictionary, to produce a result if it was present in the table.
    // In general, since this api is intended for fallback scenarios, implementations of this api should attempt
    // to return null instead of producing errors.
    // Since in normal execution of the runtime no stream is constructed, it is normal when examining full dumps and live process state without a stream encoded.
}
```
