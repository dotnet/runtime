
#pragma once

#include <string>
#include <vector>
#include <memory>
#include <assert.h>
#include "../profiler.h"

enum class EventPipeTypeCode
{
    Object = 1,         // Self describing object
    Boolean = 3,        // Boolean
    Char = 4,           // Unicode character
    Sbyte = 5,          // Signed 8-bit integer
    Byte = 6,           // Unsigned 8-bit integer
    Int16 = 7,          // Signed 16-bit integer
    Uint16 = 8,         // Unsigned 16-bit integer
    Int32 = 9,          // Signed 32-bit integer
    Uint32 = 10,        // Unsigned 32-bit integer
    Int64 = 11,         // Signed 64-bit integer
    Uint64 = 12,        // Unsigned 64-bit integer
    Single = 13,        // IEEE 32-bit float
    Double = 14,        // IEEE 64-bit double
    Decimal = 15,       // Decimal
    Datetime = 16,      // DateTime
    Guid = 17,          // Guid
    String = 18,        // Unicode character string
    ArrayType = 19      // An arbitrary length array
};

enum class EventPipeV2Tag
{
    Opcode = 1,
    ParamsV2 = 2
};

struct EventPipeDataDescriptor
{
    String name;
    EventPipeTypeCode type;
    // Only used if the type is ArrayType, then this is the underlying type
    std::shared_ptr<EventPipeDataDescriptor> elementType;
    // Used for self describing object fields
    std::vector<EventPipeDataDescriptor> fields;
};

struct EventPipeMetadataInstance
{
    UINT32 id;
    String name;
    INT64 keywords;
    UINT32 level;
    UINT32 version;
    BYTE opcode;
    std::vector<EventPipeDataDescriptor> parameters;
};

template<typename T>
T ReadFromBuffer(LPCBYTE eventData, ULONG cbEventData, ULONG *offset)
{
    T data = *((T *)(eventData + *offset));
    *offset += sizeof(T);
    assert(*offset <= cbEventData);
    return data;
}

template<>
WCHAR *ReadFromBuffer(LPCBYTE eventData, ULONG cbEventData, ULONG *offset);

class EventPipeMetadataReader
{
private:
    EventPipeDataDescriptor ParseType(LPCBYTE pMetadata, ULONG cbMetadata, ULONG *offset, bool v2);
    EventPipeDataDescriptor ParseField(LPCBYTE pMetadata, ULONG cbMetadata, ULONG *offset, bool v2);

public:
    EventPipeMetadataReader();
    ~EventPipeMetadataReader() = default;

    EventPipeMetadataInstance Parse(LPCBYTE pMetadata, ULONG cbMetadata);
};
