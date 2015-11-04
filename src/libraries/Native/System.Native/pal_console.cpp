// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <stdio.h>
#include <stdlib.h>

extern "C" int32_t GetWindowSize(WinSize* windowSize)
{
    assert(windowSize != nullptr);

#if HAVE_IOCTL && HAVE_TIOCGWINSZ
    int error = ioctl(STDOUT_FILENO, TIOCGWINSZ, windowSize);

    if (error != 0)
    {
        *windowSize = {}; // managed out param must be initialized
    }

    return error;
#else
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t IsATty(int fd)
{
    return isatty(fd);
}

extern "C" int32_t ReadStdinUnbuffered(void* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

#if HAVE_TCGETATTR && HAVE_TCSETATTR && HAVE_ECHO && HAVE_ICANON && HAVE_TCSANOW
    struct termios oldtermios = {};
    struct termios newtermios = {};

    if (tcgetattr(STDIN_FILENO, &oldtermios) < 0)
        return -1;

    newtermios = oldtermios;
    newtermios.c_lflag &= static_cast<uint32_t>(~(ECHO | ICANON));
    newtermios.c_cc[VMIN] = 1;
    newtermios.c_cc[VTIME] = 0;
    if (tcsetattr(STDIN_FILENO, TCSANOW, &newtermios) < 0)
        return -1;
    ssize_t count = read(STDIN_FILENO, buffer, UnsignedCast(bufferSize));
    tcsetattr(STDIN_FILENO, TCSANOW, &oldtermios);
    return static_cast<int32_t>(count);
#else
    errno = ENOTSUP;
    return -1;
#endif
}
