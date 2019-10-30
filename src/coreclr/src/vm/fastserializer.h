// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FASTSERIALIZER_H__
#define __FASTSERIALIZER_H__

#define ALIGNMENT_SIZE 4

#ifdef FEATURE_PERFTRACING

#include "fastserializableobject.h"
#include "fstream.h"

class IpcStream;

// the enumeration has a specific set of values to keep it compatible with consumer library
// it's sibling is defined in https://github.com/Microsoft/perfview/blob/10d1f92b242c98073b3817ac5ee6d98cd595d39b/src/FastSerialization/FastSerialization.cs#L2295
enum class FastSerializerTags : uint8_t
{
    Error              = 0, // To improve debugabilty, 0 is an illegal tag.
    NullReference      = 1, // Tag for a null object forwardReference.
    ObjectReference    = 2, // Followed by StreamLabel
                            // 3 used to belong to ForwardReference, which got removed in V3
    BeginObject        = 4, // Followed by Type object, object data, tagged EndObject
    BeginPrivateObject = 5, // Like beginObject, but not placed in interning table on deserialiation
    EndObject          = 6, // Placed after an object to mark its end.
                            // 7 used to belong to ForwardDefinition, which got removed in V3
    Byte               = 8,
    Int16,
    Int32,
    Int64,
    SkipRegion,
    String,
    Blob,
    Limit                   // Just past the last valid tag, used for asserts.
};

//!
//! Provides a generic interface for writing a sequence of bytes to a stream.
//!
class StreamWriter
{
public:
    StreamWriter() = default;
    virtual ~StreamWriter() = default;
    virtual bool Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const = 0;
};

//!
//! Implements a StreamWriter for writing bytes to an IPC.
//!
class IpcStreamWriter final : public StreamWriter
{
public:
    IpcStreamWriter(uint64_t id, IpcStream *pStream);
    ~IpcStreamWriter();
    bool Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const;

private:
    IpcStream *_pStream;
};

//!
//! Implements a StreamWriter for writing bytes to a File.
//!
class FileStreamWriter final : public StreamWriter
{
public:
    FileStreamWriter(const SString &outputFilePath);
    ~FileStreamWriter();
    bool Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const;

private:
    CFileStream *m_pFileStream;
};

class FastSerializer
{
public:
    FastSerializer(StreamWriter *pStreamWriter);
    ~FastSerializer();

    void WriteObject(FastSerializableObject *pObject);
    void WriteBuffer(BYTE *pBuffer, unsigned int length);
    void WriteTag(FastSerializerTags tag, BYTE *payload = NULL, unsigned int payloadLength = 0);
    void WriteString(const char *strContents, unsigned int length);

    unsigned int GetRequiredPadding() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_requiredPadding;
    }

    bool HasWriteErrors() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_writeErrorEncountered;
    }

private:
    void WriteSerializationType(FastSerializableObject *pObject);
    void WriteFileHeader();

    StreamWriter *const m_pStreamWriter;
    bool m_writeErrorEncountered;
    unsigned int m_requiredPadding;
};

#endif // FEATURE_PERFTRACING

#endif // __FASTSERIALIZER_H__
