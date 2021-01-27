// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "eventpipemetadatagenerator.h"
#include "eventpipe.h"

#ifdef FEATURE_PERFTRACING

bool EventPipeMetadataGenerator::HasV2ParamTypes(
        EventPipeParameterDesc *pParams,
        UINT32 paramCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(paramCount == 0 || pParams != NULL);
    }
    CONTRACTL_END;

    for (UINT32 i = 0; i < paramCount; ++i)
    {
        if (pParams[i].Type == EventPipeParameterType::Array)
        {
            return true;
        }
    }

    return false;
}

void EventPipeMetadataGenerator::GetEventMetadataLength(
    UINT32 eventID,
    LPCWSTR pEventName,
    INT64 keywords,
    UINT32 version,
    EventPipeEventLevel level,
    UINT8 opcode,
    EventPipeParameterDesc *pParams,
    UINT32 paramCount,
    size_t *totalLength,
    size_t *v2Length)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pEventName != NULL);
        PRECONDITION(paramCount == 0 || pParams != NULL);
    }
    CONTRACTL_END;

    bool hasV2Types = HasV2ParamTypes(pParams, paramCount);
    *v2Length = 0;

    // eventID          : 4 bytes
    // eventName        : (eventName.Length + 1) * 2 bytes
    // keywords         : 8 bytes
    // eventVersion     : 4 bytes
    // level            : 4 bytes
    // parameterCount   : 4 bytes
    size_t eventNameLength = wcslen(pEventName);
    *totalLength = 24 + ((eventNameLength + 1) * sizeof(WCHAR));

    if (opcode != 0)
    {
        // Size of the opcode tag
        *totalLength += 6;
    }

    if (hasV2Types)
    {
        // need 4 bytes for the length of the tag
        // 1 byte for the tag identifier
        // and 4 bytes for the count of params
        *totalLength += 9;
        // The metadata tag length does not include the required
        // length and tag fields
        *v2Length = 4;

        // Each parameter has an optional array identifier and then a 4 byte
        // TypeCode + the field name (parameterName.Length + 1) * 2 bytes.
        for(UINT32 i = 0; i < paramCount; ++i)
        {
            _ASSERTE(pParams[i].Name != NULL);
            // For v2 metadata, fields start with a length (4 bytes) and then the field name
            size_t paramSize = 4 + ((wcslen(pParams[i].Name) + 1) * sizeof(WCHAR));

            if (pParams[i].Type == EventPipeParameterType::Array)
            {
                // If it's an array type we write the array descriptor (4 bytes)
                paramSize += 4;
            }

            // Then the typecode
            paramSize += 4;

            *totalLength += paramSize;
            *v2Length += paramSize;
        }
    }
    else
    {
        // Each parameter has an 4 byte TypeCode + the field name (parameterName.Length + 1) * 2 bytes
        for(UINT32 i = 0; i < paramCount; i++)
        {
            _ASSERTE(pParams[i].Name != NULL);
            *totalLength += (4 + ((wcslen(pParams[i].Name) + 1) * sizeof(WCHAR)));
        }
    }

}

void EventPipeMetadataGenerator::WriteToBuffer(BYTE *pBuffer, size_t bufferLength, size_t *pOffset, LPCWSTR str, size_t strLen)
{
    _ASSERTE(bufferLength >= (*pOffset + strLen + 1));

    wcsncpy((WCHAR *)(pBuffer + *pOffset), str, strLen);
    *pOffset += (strLen * sizeof(WCHAR));

    // Null terminate the string
    *((WCHAR *)(pBuffer + *pOffset)) = W('\0');
    *pOffset += sizeof(WCHAR);
}

BYTE* EventPipeMetadataGenerator::GenerateEventMetadata(
    UINT32 eventID,
    LPCWSTR pEventName,
    INT64 keywords,
    UINT32 version,
    EventPipeEventLevel level,
    UINT8 opcode,
    EventPipeParameterDesc *pParams,
    UINT32 paramCount,
    size_t *pMetadataLength)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pEventName != NULL);
        PRECONDITION(paramCount == 0 || pParams != NULL);
        PRECONDITION(pMetadataLength != NULL);
    }
    CONTRACTL_END;

    size_t totalMetadataLength = 0;
    size_t v2MetadataLength = 0;
    GetEventMetadataLength(eventID,
                           pEventName,
                           keywords,
                           version,
                           level,
                           opcode,
                           pParams,
                           paramCount,
                           &totalMetadataLength,
                           &v2MetadataLength);

    bool hasV2Types = v2MetadataLength > 0;
    *pMetadataLength = totalMetadataLength;

    // Allocate a metadata blob.
    BYTE *pMetadata = new BYTE[*pMetadataLength];
    size_t offset = 0;

    // Write the event ID.
    WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, eventID);

    // Write the event name.
    size_t eventNameLength = wcslen(pEventName);
    WriteToBuffer(pMetadata, *pMetadataLength, &offset, pEventName, eventNameLength);

    // Write the keywords.
    WriteToBuffer<INT64>(pMetadata, *pMetadataLength, &offset, keywords);

    // Write the version.
    WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, version);

    // Write the level.
    WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)level);

    if (hasV2Types)
    {
        // If we have V2 metadata types, we need to have 0 params for V1
        WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, 0);
    }
    else
    {
        _ASSERTE(!hasV2Types);

        // Write the parameter count.
        WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, paramCount);
        // Now write the descriptors
        for(UINT32 i = 0; i < paramCount; ++i)
        {
            EventPipeParameterDesc *pParam = &pParams[i];
            WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)pParam->Type);

            size_t parameterNameLength = wcslen(pParam->Name);
            WriteToBuffer(pMetadata, *pMetadataLength, &offset, pParam->Name, parameterNameLength);
        }
    }

    // Now we write optional V2 metadata, if there is any

    if (opcode != 0)
    {
        // Size of opcode
        WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, 1);
        // opcode tag
        WriteToBuffer<BYTE>(pMetadata, *pMetadataLength, &offset, (BYTE)EventPipeMetadataTag::Opcode);
        // opcode value
        WriteToBuffer<BYTE>(pMetadata, *pMetadataLength, &offset, opcode);
    }

    if (hasV2Types)
    {
        // size of V2 metadata payload
        WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)v2MetadataLength);
        // v2 param tag
        WriteToBuffer<BYTE>(pMetadata, *pMetadataLength, &offset, (BYTE)EventPipeMetadataTag::ParameterPayload);
        // Write the parameter count.
        WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, paramCount);
        // Write the parameter descriptions.
        for(UINT32 i = 0; i < paramCount; ++i)
        {
            EventPipeParameterDesc *pParam = &pParams[i];
            size_t parameterNameLength = wcslen(pParam->Name);
            size_t parameterNameBytes = ((parameterNameLength + 1) * sizeof(WCHAR));
            if (pParam->Type == EventPipeParameterType::Array)
            {
                // For an array type, length is 12 (4 bytes length field, 4 bytes array descriptor, 4 bytes typecode)
                // + name length
                WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)(12 + parameterNameBytes));
                // Now write the event name
                WriteToBuffer(pMetadata, *pMetadataLength, &offset, pParam->Name, parameterNameLength);
                // And there is the array descriptor
                WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)EventPipeParameterType::Array);
                // Now write the underlying type
                WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)pParam->ElementType);
            }
            else
            {
                // For a non array type, length is 8 (4 bytes length field, 4 bytes typecode)
                // + name length
                WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)(8 + parameterNameBytes));
                // Now write the event name
                WriteToBuffer(pMetadata, *pMetadataLength, &offset, pParam->Name, parameterNameLength);
                // And then the type
                WriteToBuffer<UINT32>(pMetadata, *pMetadataLength, &offset, (UINT32)pParam->Type);
            }
        }
    }

    _ASSERTE(*pMetadataLength == offset);

    return pMetadata;
}

#endif // FEATURE_PERFTRACING
