// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// fakepoll.h
// poll using select
// Warning: a call to this poll() takes about 4K of stack space.

// Greg Parker     gparker@cs.stanford.edu     December 2000
// This code is in the public domain and may be copied or modified without
// permission.

// Located at <http://www.sealiesoftware.com/fakepoll.h>.



#include "pal/palinternal.h"
#include "pal/fakepoll.h"
#include "pal/dbgmsg.h"
#include <errno.h>
#include <string.h>
#include <limits.h>
#include <sys/types.h>
#include <sys/time.h>
#include <unistd.h>

SET_DEFAULT_DEBUG_CHANNEL(POLL);

int poll(struct pollfd *pollSet, int pollCount, int pollTimeout)
{
    struct timeval tv;
    struct timeval *tvp;
    fd_set readFDs, writeFDs, exceptFDs;
    fd_set *readp, *writep, *exceptp;
    struct pollfd *pollEnd, *p;
    int selected;
    int result;
    int maxFD;

    if (!pollSet) {
        pollEnd = NULL;
        readp = NULL;
        writep = NULL;
        exceptp = NULL;
        maxFD = 0;
    }
    else {
        pollEnd = pollSet + pollCount;
        readp = &readFDs;
        writep = &writeFDs;
        exceptp = &exceptFDs;

        FD_ZERO(readp);
        FD_ZERO(writep);
        FD_ZERO(exceptp);

        // Find the biggest fd in the poll set
        maxFD = 0;
        for (p = pollSet; p < pollEnd; p++) {
            if (p->fd > maxFD) maxFD = p->fd;
        }

        if (maxFD >= FD_SETSIZE) {
            // At least one fd is too big
            errno = EINVAL;
            return -1;
        }

        // Transcribe flags from the poll set to the fd sets
        for (p = pollSet; p < pollEnd; p++) {
            if (p->fd < 0) {
                // Negative fd checks nothing and always reports zero
            } else {
                if (p->events & POLLIN)  FD_SET(p->fd, readp);
                if (p->events & POLLOUT) FD_SET(p->fd, writep);
                if (p->events != 0)      FD_SET(p->fd, exceptp);
                // POLLERR is never set coming in; poll() always reports errors.
                // But don't report if we're not listening to anything at all.
            }
        }
    }

    // poll timeout is in milliseconds. Convert to struct timeval.
    // poll timeout == -1 : wait forever : select timeout of NULL
    // poll timeout == 0  : return immediately : select timeout of zero
    if (pollTimeout >= 0) {
        tv.tv_sec = pollTimeout / 1000;
        tv.tv_usec = (pollTimeout % 1000) * 1000;
        tvp = &tv;
    } else {
        tvp = NULL;
    }

    selected = select(maxFD+1, readp, writep, exceptp, tvp);

    if (selected < 0) {
        // Error during select
        result = -1;
    }
    else if (selected > 0) {
        // Select found something
        // Transcribe result from fd sets to poll set.
        // Also count the number of selected fds. poll returns the
        // number of ready fds; select returns the number of bits set.
        int polled = 0;
        for (p = pollSet; p < pollEnd; p++) {
            p->revents = 0;
            if (p->fd > -1) {
                // Check p->events before setting p->revents. If we
                // have multiple pollfds with the same fd, we want to
                // set the appropriate revents value for each pollfd.
                if (FD_ISSET(p->fd, readp) && (p->events & POLLIN))
                    p->revents |= POLLIN;
                if (FD_ISSET(p->fd, writep) && (p->events & POLLOUT))
                    p->revents |= POLLOUT;
                if (FD_ISSET(p->fd, exceptp) && (p->events != 0))
                    p->revents |= POLLERR;
                if (p->revents) polled++;
            }
        }
        result = polled;
    }
    else {
        // selected == 0, select timed out before anything happened
        // Clear all result bits and return zero.
        for (p = pollSet; p < pollEnd; p++) {
            p->revents = 0;
        }
        result = 0;
    }

    return result;
}
