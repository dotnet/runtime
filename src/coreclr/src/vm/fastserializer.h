// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FASTSERIALIZER_H__
#define __FASTSERIALIZER_H__

#define ALIGNMENT_SIZE 4

#ifdef FEATURE_PERFTRACING

#include "fastserializableobject.h"
#include "fstream.h"

class FastSerializer;

typedef unsigned int StreamLabel;

// the enumeration has a specific set of values to keep it compatible with consumer library
// it's sibling is defined in https://github.com/Microsoft/perfview/blob/10d1f92b242c98073b3817ac5ee6d98cd595d39b/src/FastSerialization/FastSerialization.cs#L2295
enum class FastSerializerTags : BYTE 
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

class FastSerializer
{
public:

    FastSerializer(SString &outputFilePath);
    ~FastSerializer();

    StreamLabel GetStreamLabel() const;

    void WriteObject(FastSerializableObject *pObject);
    void WriteBuffer(BYTE *pBuffer, unsigned int length);
    void WriteTag(FastSerializerTags tag, BYTE *payload = NULL, unsigned int payloadLength = 0);
    void WriteString(const char *strContents, unsigned int length);

    size_t GetCurrentPosition() const
    {
        LIMITED_METHOD_CONTRACT;

        return m_currentPos;
    }

private:

    void WriteSerializationType(FastSerializableObject *pObject);
    void WriteFileHeader();

    CFileStream *m_pFileStream;
    bool m_writeErrorEncountered;
    size_t m_currentPos;
};

#endif // FEATURE_PERFTRACING

#endif // __FASTSERIALIZER_H__
