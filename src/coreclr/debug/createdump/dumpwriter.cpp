// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

DumpWriter::DumpWriter(CrashInfo& crashInfo) :
    m_fd(-1),
    m_crashInfo(crashInfo)
{
    m_crashInfo.AddRef();
}

DumpWriter::~DumpWriter()
{
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
    m_crashInfo.Release();
}

bool
DumpWriter::OpenDump(const char* dumpFileName)
{
    m_fd = open(dumpFileName, O_WRONLY|O_CREAT|O_TRUNC, 0664);
    if (m_fd == -1)
    {
        fprintf(stderr, "Could not open output %s: %d %s\n", dumpFileName, errno, strerror(errno));
        return false;
    }
    return true;
}

// Write all of the given buffer, handling short writes and EINTR. Return true iff successful.
bool
DumpWriter::WriteData(const void* buffer, size_t length)
{
    const uint8_t* data = (const uint8_t*)buffer;

    size_t done = 0;
    while (done < length) {
        ssize_t written;
        do {
            written = write(m_fd, data + done, length - done);
        } while (written == -1 && errno == EINTR);

        if (written < 1) {
            fprintf(stderr, "WriteData FAILED %d %s\n", errno, strerror(errno));
            return false;
        }
        done += written;
    }
    return true;
}
