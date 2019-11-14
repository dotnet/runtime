// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_METADATAGENERATOR_H__
#define __EVENTPIPE_METADATAGENERATOR_H__

#ifdef FEATURE_PERFTRACING

enum class EventPipeEventLevel;

// Represents the type of an event parameter.
// This enum is derived from the managed TypeCode type, though
// not all of these values are available in TypeCode.
// For example, Guid does not exist in TypeCode.
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
};

// Contains the metadata associated with an EventPipe event parameter.
struct EventPipeParameterDesc
{
    EventPipeParameterType Type;
    LPCWSTR Name;
};

// Generates metadata for an event emitted by the EventPipe.
class EventPipeMetadataGenerator
{
public:
    static BYTE* GenerateEventMetadata(
        unsigned int eventID,
        LPCWSTR pEventName,
        INT64 keywords,
        unsigned int version,
        EventPipeEventLevel level,
        EventPipeParameterDesc *pParams,
        unsigned int paramCount,
        size_t &metadataLength);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_METADATAGENERATOR_H__
