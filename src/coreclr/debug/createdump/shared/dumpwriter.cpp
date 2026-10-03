// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef __APPLE__
#include "createdump.h"
#else
#include "createdumpcore.h"
#include "dumpwriter.h"
#endif

#ifdef __APPLE__
DumpWriter::DumpWriter(CrashInfo& crashInfo) :
    m_fd(-1),
    m_crashInfo(crashInfo)
{
    m_crashInfo.AddRef();
}
#endif

DumpWriter::~DumpWriter()
{
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
#ifdef __APPLE__
    m_crashInfo.Release();
#endif
}

bool
DumpWriter::OpenAndWriteDump(const char* dumpFileName)
{
    if (!OpenDump(dumpFileName))
    {
        return false;
    }
    if (!WriteDump())
    {
        printf_error("Writing dump FAILED\n");
        remove(dumpFileName);
        return false;
    }
    return true;
}

bool
DumpWriter::OpenDump(const char* dumpFileName)
{
    m_fd = open(dumpFileName, O_WRONLY|O_CREAT|O_TRUNC, S_IWUSR | S_IRUSR);
    if (m_fd == -1)
    {
        printf_error("Could not create output file '%s': %s (%d)\n", dumpFileName, strerror(errno), errno);
        return false;
    }
    return true;
}

bool
DumpWriter::WriteDiagInfo(size_t size)
{
    // Write the diagnostics info header
    SpecialDiagInfoHeader header = {
        {SPECIAL_DIAGINFO_SIGNATURE},
        SPECIAL_DIAGINFO_VERSION,
#ifdef __APPLE__
        m_crashInfo.ExceptionRecord(),
        m_crashInfo.RuntimeBaseAddress()
#else
        m_processInfo.ExceptionRecord(),
        m_processInfo.RuntimeBaseAddress()
#endif
    };
    if (!WriteData(&header, sizeof(header))) {
        return false;
    }
    size_t alignment = size - sizeof(header);
    assert(alignment < sizeof(m_tempBuffer));
    memset(m_tempBuffer, 0, alignment);
    if (!WriteData(m_tempBuffer, alignment)) {
        return false;
    }
    return true;
}

// Write all of the given buffer, handling short writes and EINTR. Return true iff successful.
bool
DumpWriter::WriteData(int fd, const void* buffer, size_t length)
{
    const uint8_t* data = (const uint8_t*)buffer;

    size_t done = 0;
    while (done < length) {
        ssize_t written;
        do {
            written = write(fd, data + done, length - done);
        } while (written == -1 && errno == EINTR);

        if (written < 1) {
            printf_error("Error writing data to dump file: %s (%d)\n", strerror(errno), errno);
            return false;
        }
        done += written;
    }
    return true;
}
