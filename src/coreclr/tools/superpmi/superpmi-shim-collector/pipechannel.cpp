#include <unistd.h>
#include <string.h>
#include <stdio.h>
#include <unistd.h>

#include "pipechannel.hpp"

PipeChannel PipeChannel::Create()
{
    int fds[2] = {0,0};
    if (pipe (fds) < 0) {
        perror ("pipe failed");
        abort();
    }
    return PipeChannel{fds[0], fds[1]};
}
