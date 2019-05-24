// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "fastserializer.h"
#include "diagnosticsipc.h"
#include <diagnosticsprotocol.h>
#include <eventpipeprotocolhelper.h>

#ifdef FEATURE_PERFTRACING

// Event Pipe has previously implemented a feature called "forward references"
// As a result of work on V3 of Event Pipe (https://github.com/Microsoft/perfview/pull/532) it got removed
// if you need it, please use git to restore it

IpcStreamWriter::IpcStreamWriter(uint64_t id, IpcStream *pStream) : _pStream(pStream)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(_pStream != nullptr);
    }
    CONTRACTL_END;

    if (_pStream == nullptr)
        return;

    DiagnosticsIpc::IpcMessage successResponse;
    if (successResponse.Initialize(DiagnosticsIpc::GenericSuccessHeader, id))
        successResponse.Send(pStream);
}

IpcStreamWriter::~IpcStreamWriter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete _pStream;
}

bool IpcStreamWriter::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
        PRECONDITION(nBytesToWrite > 0);
    }
    CONTRACTL_END;

    if (_pStream == nullptr)
        return false;
    if (lpBuffer == nullptr || nBytesToWrite == 0)
        return false;
    return _pStream->Write(lpBuffer, nBytesToWrite, nBytesWritten);
}

FileStreamWriter::FileStreamWriter(const SString &outputFilePath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
    m_pFileStream = new CFileStream();
    if (FAILED(m_pFileStream->OpenForWrite(outputFilePath)))
    {
        _ASSERTE(!"Unable to open file for write.");
        delete m_pFileStream;
        m_pFileStream = NULL;
        return;
    }
}

FileStreamWriter::~FileStreamWriter()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete m_pFileStream;
}

bool FileStreamWriter::Write(const void *lpBuffer, const uint32_t nBytesToWrite, uint32_t &nBytesWritten) const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(lpBuffer != nullptr);
        PRECONDITION(nBytesToWrite > 0);
    }
    CONTRACTL_END;

    if (m_pFileStream == nullptr)
        return false;

    ULONG outCount;
    HRESULT hResult = m_pFileStream->Write(lpBuffer, nBytesToWrite, &outCount);
    nBytesWritten = static_cast<uint32_t>(outCount);
    return hResult == S_OK;
}

FastSerializer::FastSerializer(StreamWriter *pStreamWriter) : m_pStreamWriter(pStreamWriter)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(m_pStreamWriter != NULL);
    }
    CONTRACTL_END;

    m_writeErrorEncountered = false;
    m_requiredPadding = 0;
    WriteFileHeader();
}

FastSerializer::~FastSerializer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    delete m_pStreamWriter;
}

void FastSerializer::WriteObject(FastSerializableObject *pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pObject != NULL);
    }
    CONTRACTL_END;

    WriteTag(FastSerializerTags::BeginObject);

    WriteSerializationType(pObject);

    // Ask the object to serialize itself using the current serializer.
    pObject->FastSerialize(this);

    WriteTag(FastSerializerTags::EndObject);
}

void FastSerializer::WriteBuffer(BYTE *pBuffer, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pBuffer != NULL);
        PRECONDITION(length > 0);
    }
    CONTRACTL_END;

    if (m_writeErrorEncountered || m_pStreamWriter == NULL)
        return;

    EX_TRY
    {
        uint32_t outCount;
        bool fSuccess = m_pStreamWriter->Write(pBuffer, length, outCount);

        m_requiredPadding = (ALIGNMENT_SIZE + m_requiredPadding - (outCount % ALIGNMENT_SIZE)) % ALIGNMENT_SIZE;

        // This will cause us to stop writing to the file.
        // The file will still remain open until shutdown so that we don't
        // have to take a lock at this level when we touch the file stream.
        m_writeErrorEncountered = (length != outCount) || !fSuccess;
    }
    EX_CATCH
    {
        m_writeErrorEncountered = true;
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void FastSerializer::WriteSerializationType(FastSerializableObject *pObject)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pObject != NULL);
    }
    CONTRACTL_END;

    // Write the BeginObject tag.
    WriteTag(FastSerializerTags::BeginObject);

    // Write a NullReferenceTag, which implies that the following fields belong to SerializationType.
    WriteTag(FastSerializerTags::NullReference);

    // Write the SerializationType version fields.
    int serializationType[2] = {
        pObject->GetObjectVersion(),
        pObject->GetMinReaderVersion()};
    WriteBuffer((BYTE *)&serializationType, sizeof(serializationType));

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
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    WriteBuffer((BYTE *)&tag, sizeof(tag));
    if (payload != NULL)
    {
        _ASSERTE(payloadLength > 0);
        WriteBuffer(payload, payloadLength);
    }
}

void FastSerializer::WriteFileHeader()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    const char *strSignature = "!FastSerialization.1"; // the consumer lib expects exactly the same string, it must not be changed
    unsigned int length = (unsigned int)strlen(strSignature);
    WriteString(strSignature, length);
}

void FastSerializer::WriteString(const char *strContents, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Write the string length .
    WriteBuffer((BYTE *)&length, sizeof(length));

    // Write the string contents.
    WriteBuffer((BYTE *)strContents, length);
}

#endif // FEATURE_PERFTRACING
