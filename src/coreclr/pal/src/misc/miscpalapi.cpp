// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    miscpalapi.c

Abstract:

    Implementation misc PAL APIs

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/process.h"
#include "pal/module.h"
#include "pal/malloc.hpp"
#include "pal/stackstring.hpp"

#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <time.h>
#include <pthread.h>
#include <dlfcn.h>

#include <pal_endian.h>

#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif // __APPLE__

SET_DEFAULT_DEBUG_CHANNEL(MISC);

static const char URANDOM_DEVICE_NAME[]="/dev/urandom";

VOID
PALAPI
PAL_Random(
        IN OUT LPVOID lpBuffer,
        IN DWORD dwLength)
{
    int rand_des = -1;
    DWORD i;
    long num = 0;
    static BOOL sMissingDevURandom;
    static BOOL sInitializedMRand;

    PERF_ENTRY(PAL_Random);
    ENTRY("PAL_Random(lpBuffer=%p, dwLength=%d)\n", lpBuffer, dwLength);

    if (!sMissingDevURandom)
    {
        do
        {
            rand_des = open("/dev/urandom", O_RDONLY | O_CLOEXEC);
        }
        while ((rand_des == -1) && (errno == EINTR));

        if (rand_des == -1)
        {
            if (errno == ENOENT)
            {
                sMissingDevURandom = TRUE;
            }
            else
            {
                ASSERT("PAL__open() failed, errno:%d (%s)\n", errno, strerror(errno));
            }

            // Back off and try mrand48.
        }
        else
        {
            DWORD offset = 0;
            do
            {
                ssize_t n = read(rand_des, (BYTE*)lpBuffer + offset , dwLength - offset);
                if (n == -1)
                {
                    if (errno == EINTR)
                    {
                        continue;
                    }
                    ASSERT("read() failed, errno:%d (%s)\n", errno, strerror(errno));

                    break;
                }

                offset += n;
            }
            while (offset != dwLength);

            _ASSERTE(offset == dwLength);

            close(rand_des);
        }
    }

    if (!sInitializedMRand)
    {
        srand48(time(NULL));
        sInitializedMRand = TRUE;
    }

    // always xor srand48 over the whole buffer to get some randomness
    // in case /dev/urandom is not really random

    for (i = 0; i < dwLength; i++)
    {
        if (i % sizeof(long) == 0) {
            num = mrand48();
        }

        *(((BYTE*)lpBuffer) + i) ^= num;
        num >>= 8;
    }

    LOGEXIT("PAL_Random\n");
    PERF_EXIT(PAL_Random);
}
