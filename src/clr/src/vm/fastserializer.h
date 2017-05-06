// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FASTSERIALIZER_H__
#define __FASTSERIALIZER_H__

#ifdef FEATURE_PERFTRACING

#include "fastserializableobject.h"
#include "fstream.h"

class FastSerializer;

typedef unsigned int StreamLabel;

enum class FastSerializerTags : BYTE 
{
    Error,              // To improve debugabilty, 0 is an illegal tag.  
    NullReference,      // Tag for a null object forwardReference. 
    ObjectReference,    // Followed by StreamLabel 
    ForwardReference,   // Followed by an index (32-bit integer) into the Forward forwardReference array and a Type object
    BeginObject,        // Followed by Type object, object data, tagged EndObject
    BeginPrivateObject, // Like beginObject, but not placed in interning table on deserialiation 
    EndObject,          // Placed after an object to mark its end. 
    ForwardDefinition,  // Followed by a forward forwardReference index and an object definition (BeginObject)
    Byte,
    Int16,
    Int32,
    Int64,
    SkipRegion,
    String,
    Limit,              // Just past the last valid tag, used for asserts.  
};

class FastSerializer
{
public:

    FastSerializer(SString &outputFilePath, FastSerializableObject &object);
    ~FastSerializer();

    StreamLabel GetStreamLabel() const;

    void WriteObject(FastSerializableObject *pObject);
    void WriteBuffer(BYTE *pBuffer, unsigned int length);
    void WriteTag(FastSerializerTags tag, BYTE *payload = NULL, unsigned int payloadLength = 0);
    void WriteString(const char *strContents, unsigned int length);

    unsigned int AllocateForwardReference();
    void DefineForwardReference(unsigned int index, StreamLabel value);
    void WriteForwardReference(unsigned int index);

private:

    void WriteEntryObject();
    void WriteSerializationType(FastSerializableObject *pObject);
    void WriteFileHeader();
    StreamLabel WriteForwardReferenceTable();
    void WriteTrailer(StreamLabel forwardReferencesTableStart);

    CFileStream *m_pFileStream;
    bool m_writeErrorEncountered;
    FastSerializableObject *m_pEntryObject;
    size_t m_currentPos;

    static const unsigned int MaxForwardReferences = 100;
    StreamLabel m_forwardReferences[MaxForwardReferences];
    unsigned int m_nextForwardReference;
};

#endif // FEATURE_PERFTRACING

#endif // __FASTSERIALIZER_H__
