// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

extern StressLog::StressLogHeader* s_hdr;

// A version-aware reader for memory-mapped stress log messages.
struct StressMsgReader
{
private:
    struct StressMsgSmallOffset
    {
        uint32_t numberOfArgsLow  : 3;                   // at most 7 arguments here
        uint32_t formatOffset  : 26;                     // low bits offset of format string in modules
        uint32_t numberOfArgsHigh : 3;                   // extend number of args in a backward compat way
        uint32_t facility;                               // facility used to log the entry
        uint64_t timeStamp;                              // time when mssg was logged
        void* args[0];                                   // variable number of arguments
    };

    void* m_rawMsg;
public:
    StressMsgReader() = default;

    StressMsgReader(void* msg)
        :m_rawMsg(msg)
    {
    }

    uint64_t GetFormatOffset() const
    {
        if (s_hdr->version == 0x00010001)
        {
            return ((StressMsgSmallOffset*)m_rawMsg)->formatOffset;
        }
        return ((StressMsg*)m_rawMsg)->GetFormatOffset();
    }

    uint32_t GetNumberOfArgs() const
    {
        if (s_hdr->version == 0x00010001)
        {
            return ((StressMsgSmallOffset*)m_rawMsg)->numberOfArgsHigh << 3 | ((StressMsgSmallOffset*)m_rawMsg)->numberOfArgsLow;
        }
        return ((StressMsg*)m_rawMsg)->GetNumberOfArgs();
    }

    uint32_t GetFacility() const
    {
        if (s_hdr->version == 0x00010001)
        {
            return ((StressMsgSmallOffset*)m_rawMsg)->facility;
        }
        return ((StressMsg*)m_rawMsg)->GetFacility();
    }

    uint64_t GetTimeStamp() const
    {
        if (s_hdr->version == 0x00010001)
        {
            return ((StressMsgSmallOffset*)m_rawMsg)->timeStamp;
        }
        return ((StressMsg*)m_rawMsg)->GetTimeStamp();
    }

    void** GetArgs() const
    {
        if (s_hdr->version == 0x00010001)
        {
            return ((StressMsgSmallOffset*)m_rawMsg)->args;
        }
        return ((StressMsg*)m_rawMsg)->args;
    }

    bool operator==(std::nullptr_t) const
    {
        return m_rawMsg == nullptr;
    }
};
