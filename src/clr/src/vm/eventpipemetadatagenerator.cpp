// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipemetadatagenerator.h"
#include "eventpipe.h"

#ifdef FEATURE_PERFTRACING

BYTE* EventPipeMetadataGenerator::GenerateEventMetadata(
    unsigned int eventID,
    LPCWSTR pEventName,
    INT64 keywords,
    unsigned int version,
    EventPipeEventLevel level,
    EventPipeParameterDesc *pParams,
    unsigned int paramCount,
    size_t &metadataLength)
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

    // eventID          : 4 bytes
    // eventName        : (eventName.Length + 1) * 2 bytes
    // keywords         : 8 bytes
    // eventVersion     : 4 bytes
    // level            : 4 bytes
    // parameterCount   : 4 bytes
    size_t eventNameLength = wcslen(pEventName);
    metadataLength = 24 + ((eventNameLength + 1) * sizeof(WCHAR));

    // Each parameter has a 4 byte TypeCode + (parameterName.Length + 1) * 2 bytes.
    for(unsigned int i=0; i<paramCount; i++)
    {
        _ASSERTE(pParams[i].Name != NULL);

        metadataLength += (4 + ((wcslen(pParams[i].Name) + 1) * sizeof(WCHAR)));
    }

    // Allocate a metadata blob.
    BYTE *pMetadata = new BYTE[metadataLength];
    BYTE *pCurrent = pMetadata;

    // Write the event ID.
    *((unsigned int *)pCurrent) = eventID;
    pCurrent += sizeof(unsigned int);

    // Write the event name.
    wcsncpy((WCHAR *)pCurrent, pEventName, eventNameLength);
    pCurrent += eventNameLength * sizeof(WCHAR);
    *((WCHAR *)pCurrent) = W('\0');
    pCurrent += sizeof(WCHAR);

    // Write the keywords.
    *((INT64 *)pCurrent) = keywords;
    pCurrent += sizeof(INT64);

    // Write the version.
    *((unsigned int *)pCurrent) = version;
    pCurrent += sizeof(unsigned int);

    // Write the level.
    *((unsigned int *)pCurrent) = (unsigned int)level;
    pCurrent += sizeof(unsigned int);

    // Write the parameter count.
    *((unsigned int *)pCurrent) = paramCount;
    pCurrent += sizeof(unsigned int);

    // Write the parameter descriptions.
    for(unsigned int i=0; i<paramCount; i++)
    {
        EventPipeParameterDesc *pParam = &pParams[i];
        *((unsigned int *)pCurrent) = (unsigned int)pParam->Type;
        pCurrent += sizeof(unsigned int);

        size_t parameterNameLength = wcslen(pParam->Name);
        wcsncpy((WCHAR *)pCurrent, pParam->Name, parameterNameLength);
        pCurrent += parameterNameLength * sizeof(WCHAR);
        *((WCHAR *)pCurrent) = W('\0');
        pCurrent += sizeof(WCHAR);
    }

    _ASSERTE(metadataLength == (pCurrent - pMetadata));

    return pMetadata;
}

#endif // FEATURE_PERFTRACING
