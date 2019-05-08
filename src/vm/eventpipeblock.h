// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_BLOCK_H__
#define __EVENTPIPE_BLOCK_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipeeventinstance.h"
#include "fastserializableobject.h"
#include "fastserializer.h"

class EventPipeBlock final : public FastSerializableObject
{
public:
    EventPipeBlock(unsigned int maxBlockSize);
    ~EventPipeBlock();

    // Write an event to the block.
    // Returns:
    //  - true: The write succeeded.
    //  - false: The write failed.  In this case, the block should be considered full.
    bool WriteEvent(EventPipeEventInstance &instance);

    void Clear();

    const char *GetTypeName() override
    {
        LIMITED_METHOD_CONTRACT;
        return "EventBlock";
    }

    void FastSerialize(FastSerializer *pSerializer) override
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_PREEMPTIVE;
            PRECONDITION(pSerializer != NULL);
        }
        CONTRACTL_END;

        if (m_pBlock == NULL)
            return;

        unsigned int eventsSize = (unsigned int)(m_pWritePointer - m_pBlock);
        pSerializer->WriteBuffer((BYTE *)&eventsSize, sizeof(eventsSize));

        if (eventsSize == 0)
            return;

        unsigned int requiredPadding = pSerializer->GetRequiredPadding();
        if (requiredPadding != 0)
        {
            BYTE maxPadding[ALIGNMENT_SIZE - 1] = {}; // it's longest possible padding, we are going to use only part of it
            pSerializer->WriteBuffer(maxPadding, requiredPadding); // we write zeros here, the reader is going to always read from the first aligned address of the serialized content

            _ASSERTE(pSerializer->HasWriteErrors() || (pSerializer->GetRequiredPadding() == 0));
        }

        pSerializer->WriteBuffer(m_pBlock, eventsSize);
    }

private:
    BYTE *m_pBlock;
    BYTE *m_pWritePointer;
    BYTE *m_pEndOfTheBuffer;

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBlock == nullptr ? 0 : (unsigned int)(m_pEndOfTheBuffer - m_pBlock);
    }
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BLOCK_H__
