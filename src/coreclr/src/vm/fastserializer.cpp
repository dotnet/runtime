// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"

#ifdef FEATURE_PERFTRACING

FastSerializer::FastSerializer(SString &outputFilePath, FastSerializableObject &object)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_writeErrorEncountered = false;
    m_pEntryObject = &object;
    m_currentPos = 0;
    m_nextForwardReference = 0;
    m_pFileStream = new CFileStream();
    if(FAILED(m_pFileStream->OpenForWrite(outputFilePath)))
    {
        delete(m_pFileStream);
        m_pFileStream = NULL;
        return;
    }

    // Write the file header.
    WriteFileHeader();

    // Write the entry object.
    WriteEntryObject();
}

FastSerializer::~FastSerializer()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Write the end of the entry object.
    WriteTag(FastSerializerTags::EndObject);

    // Write forward reference table.
    StreamLabel forwardReferenceLabel = WriteForwardReferenceTable();

    // Write trailer.
    WriteTrailer(forwardReferenceLabel);

    if(m_pFileStream != NULL)
    {
        delete(m_pFileStream);
        m_pFileStream = NULL;
    }
}

StreamLabel FastSerializer::GetStreamLabel() const
{
    LIMITED_METHOD_CONTRACT;

    return (StreamLabel)m_currentPos;
}

void FastSerializer::WriteObject(FastSerializableObject *pObject)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pObject != NULL);
    }
    CONTRACTL_END;

    // Write a BeginObject tag.
    WriteTag(FastSerializerTags::BeginObject);

    // Write object begin tag.
    WriteSerializationType(pObject);

    // Ask the object to serialize itself using the current serializer.
    pObject->FastSerialize(this);

    // Write object end tag.
    WriteTag(FastSerializerTags::EndObject);
}

void FastSerializer::WriteBuffer(BYTE *pBuffer, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pBuffer != NULL);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

    if(m_writeErrorEncountered || m_pFileStream == NULL)
    {
        return;
    }

    EX_TRY
    {
        ULONG outCount;
        m_pFileStream->Write(pBuffer, length, &outCount);

#ifdef _DEBUG
        size_t prevPos = m_currentPos;
#endif
        m_currentPos += outCount;
#ifdef _DEBUG
        _ASSERTE(prevPos < m_currentPos);
#endif

        if (length != outCount)
        {
            // This will cause us to stop writing to the file.
            // The file will still remain open until shutdown so that we don't have to take a lock at this level when we touch the file stream.
            m_writeErrorEncountered = true;
        }
    }
    EX_CATCH
    {
        m_writeErrorEncountered = true;
    } 
    EX_END_CATCH(SwallowAllExceptions);
}

void FastSerializer::WriteEntryObject()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Write begin entry object tag.
    WriteTag(FastSerializerTags::BeginObject);

    // Write the type information for the entry object.
    WriteSerializationType(m_pEntryObject);

    // The object is now initialized.  Fields or other objects can now be written.
}

unsigned int FastSerializer::AllocateForwardReference()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(m_nextForwardReference < MaxForwardReferences);
    }
    CONTRACTL_END;

    // TODO: Handle failure.

    // Save the index.
    int index = m_nextForwardReference;

    // Allocate the forward reference and zero-fill it so that the reader
    // will know if it was not properly defined.
    m_forwardReferences[m_nextForwardReference++] = 0;

    return index;
}

void FastSerializer::DefineForwardReference(unsigned int index, StreamLabel value)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(index < MaxForwardReferences-1);
    }
    CONTRACTL_END;

    m_forwardReferences[index] = value;
}

void FastSerializer::WriteForwardReference(unsigned int index)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(index < MaxForwardReferences-1);
    }
    CONTRACTL_END;

    WriteBuffer((BYTE*)&index, sizeof(index));
}

void FastSerializer::WriteSerializationType(FastSerializableObject *pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pObject != NULL);
    }
    CONTRACTL_END;

    // Write the BeginObject tag.
    WriteTag(FastSerializerTags::BeginObject);

    // Write a NullReferenceTag, which implies that the following fields belong to SerializationType.
    WriteTag(FastSerializerTags::NullReference);

    // Write the SerializationType version fields.
    int serializationType[2];
    serializationType[0] = 1; // Object Version.
    serializationType[1] = 0; // Minimum Reader Version.
    WriteBuffer((BYTE*) &serializationType, sizeof(serializationType));

    // Write the SerializationType TypeName field.
    const char *strTypeName = pObject->GetTypeName();
    unsigned int length = (unsigned int)strlen(strTypeName);
    WriteString(strTypeName, length);

    // Write the EndObject tag.
    WriteTag(FastSerializerTags::EndObject);
}


void FastSerializer::WriteTag(FastSerializerTags tag, BYTE *payload, unsigned int payloadLength)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    WriteBuffer((BYTE *)&tag, sizeof(tag));
    if(payload != NULL)
    {
        _ASSERTE(payloadLength > 0);
        WriteBuffer(payload, payloadLength);
    }
}


void FastSerializer::WriteFileHeader()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    const char *strSignature = "!FastSerialization.1";
    unsigned int length = (unsigned int)strlen(strSignature);
    WriteString(strSignature, length);
}

void FastSerializer::WriteString(const char *strContents, unsigned int length)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Write the string length .
    WriteBuffer((BYTE*) &length, sizeof(length));

    // Write the string contents.
    WriteBuffer((BYTE*) strContents, length);
}

StreamLabel FastSerializer::WriteForwardReferenceTable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Save the position of the start of the forward references table.
    StreamLabel current = GetStreamLabel();

    // Write the count of allocated references.
    WriteBuffer((BYTE*) &m_nextForwardReference, sizeof(m_nextForwardReference));

    // Write each of the allocated references.
    WriteBuffer((BYTE*) m_forwardReferences, sizeof(StreamLabel) * m_nextForwardReference);

    return current;
}

void FastSerializer::WriteTrailer(StreamLabel forwardReferencesTableStart)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Get the current location to mark the beginning of the trailer.
    StreamLabel current = GetStreamLabel();

    // Write the trailer, which contains the start of the forward references table.
    WriteBuffer((BYTE*) &forwardReferencesTableStart, sizeof(forwardReferencesTableStart));

    // Write the location of the trailer.  This is the final piece of data written to the file,
    // so that it can be easily found by a reader that can seek to the end of the file.
    WriteBuffer((BYTE*) &current, sizeof(current));
}

#endif // FEATURE_PERFTRACING
