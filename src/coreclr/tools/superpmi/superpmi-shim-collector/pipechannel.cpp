#ifndef HOST_WINDOWS

#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>
#include <sys/errno.h>
#include <unistd.h>

#include "pipechannel.hpp"

PipeChannel PipeChannel::Create()
{
    int fds[2] = {0,0};
    if (pipe (fds) < 0)
    {
        perror ("pipe failed");
        abort();
    }
    return PipeChannel{fds[0], fds[1]};
}

int PipeChannel::Reader::ReadSome(char *dest, int destSize) const
{
    int toRead = destSize;
    int haveRead = 0;
    do
    {
        int res = read (m_readFd, dest, toRead);
        if (res < 0)
        {
            if (errno == EINTR || errno == EAGAIN)
            {
                continue;
            }
            else
            {
                return res;
            }
        }
        else if (res == 0)
        {
            break;
        }
        toRead -= res;
        dest += res;
        haveRead += res;
    }
    while (toRead > 0);
    return haveRead;
}

int PipeChannel::Reader::ReadAll(char *dest, int destSize) const
{
    int res = ReadSome(dest, destSize);
    if (res < 0)
    {
        return res;
    }
    else if (res != destSize)
    {
        return 0; // partial reads are not ok
    }
    else
    {
        return res;
    }
}

int PipeChannel::Writer::SendAll(const char *buf, int bufSize) const
{
    const char *cur = buf;
    int toWrite = bufSize;
    int written = 0;
    do
    {
        int res = write(m_writeFd, cur, toWrite);
        if (res < 0)
        {
            if (errno == EINTR || errno == EAGAIN)
            {
                continue;
            }
            else
            {
                return res;
            }
        }
        written += res;
        cur += res;
        toWrite -= res;
    }
    while (toWrite > 0);
    return written;
}

#else /* HOST_WINDOWS*/
#endif
