// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_METADATAGENERATOR_H__
#define __EVENTPIPE_METADATAGENERATOR_H__

#ifdef FEATURE_PERFTRACING

enum class EventPipeEventLevel;

// Represents the type of an event parameter.
// This enum is derived from the managed TypeCode type, though
// not all of these values are available in TypeCode.
// For example, Guid does not exist in TypeCode.
// Keep this in sync with COR_PRF_EVENTPIPE_PARAM_TYPE defined in
// corprof.idl
enum class EventPipeParameterType
{
    Empty = 0,          // Null reference
    Object = 1,         // Instance that isn't a value
    DBNull = 2,         // Database null value
    Boolean = 3,        // Boolean
    Char = 4,           // Unicode character
    SByte = 5,          // Signed 8-bit integer
    Byte = 6,           // Unsigned 8-bit integer
    Int16 = 7,          // Signed 16-bit integer
    UInt16 = 8,         // Unsigned 16-bit integer
    Int32 = 9,          // Signed 32-bit integer
    UInt32 = 10,        // Unsigned 32-bit integer
    Int64 = 11,         // Signed 64-bit integer
    UInt64 = 12,        // Unsigned 64-bit integer
    Single = 13,        // IEEE 32-bit float
    Double = 14,        // IEEE 64-bit double
    Decimal = 15,       // Decimal
    DateTime = 16,      // DateTime
    Guid = 17,          // Guid
    String = 18,        // Unicode character string
    Array = 19,         // Indicates the type is an arbitrary sized array
};

enum class EventPipeMetadataTag
{
    Opcode = 1,
    ParameterPayload = 2
};

// Contains the metadata associated with an EventPipe event parameter.
struct EventPipeParameterDesc
{
    EventPipeParameterType Type;
    // Only used for array types to indicate what type the array elements are
    EventPipeParameterType ElementType;
    LPCWSTR Name;
};

// Generates metadata for an event emitted by the EventPipe.
class EventPipeMetadataGenerator
{
private:
    // Array is not part of TypeCode, we decided to use 19 to represent it.
    // (18 is the last type code value, string)
    static const UINT32 EventPipeTypeCodeArray = 19;

    static bool HasV2ParamTypes(
        EventPipeParameterDesc *pParams,
        UINT32 paramCount);

    static void GetEventMetadataLength(
        UINT32 eventID,
        LPCWSTR pEventName,
        INT64 keywords,
        UINT32 version,
        EventPipeEventLevel level,
        UINT8 opcode,
        EventPipeParameterDesc *pParams,
        UINT32 paramCount,
        size_t *totalLength,
        size_t *v2Length);

    static void WriteToBuffer(BYTE *pBuffer, size_t bufferLength, size_t *pOffset, LPCWSTR str, size_t strLen);

    template<typename T>
    static void WriteToBuffer(BYTE *pBuffer, size_t bufferLength, size_t *pOffset, T value)
    {
        _ASSERTE(bufferLength >= (*pOffset + sizeof(T)));

        *(T*)(pBuffer + *pOffset) = value;
        *pOffset += sizeof(T);
    }

public:
    static BYTE* GenerateEventMetadata(
        UINT32 eventID,
        LPCWSTR pEventName,
        INT64 keywords,
        UINT32 version,
        EventPipeEventLevel level,
        UINT8 opcode,
        EventPipeParameterDesc *pParams,
        UINT32 paramCount,
        size_t *pMetadataLength);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_METADATAGENERATOR_H__
