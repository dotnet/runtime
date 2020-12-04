// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// fakepoll.h
// poll using select
// Warning: a call to this poll() takes about 4K of stack space.

// Greg Parker     gparker@cs.stanford.edu     December 2000
// This code is in the public domain and may be copied or modified without
// permission.

// Located at <http://www.sealiesoftware.com/fakepoll.h>.




#ifndef _FAKE_POLL_H
#define _FAKE_POLL_H

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#ifdef FD_SETSIZE
#undef FD_SETSIZE
#endif
#define FD_SETSIZE OPEN_MAX

typedef struct pollfd {
    int fd;                         /* file desc to poll */
    short events;                   /* events of interest on fd */
    short revents;                  /* events that occurred on fd */
} pollfd_t;

// Typically defined in sys/stropts.h and used for an infinite timeout.
#ifndef _INFTIM
#define _INFTIM -1
#endif
#ifndef INFTIM
#define INFTIM _INFTIM
#endif

// poll flags
#define POLLIN  0x0001
#define POLLOUT 0x0004
#define POLLERR 0x0008

// synonyms
#define POLLNORM POLLIN
#define POLLPRI POLLIN
#define POLLRDNORM POLLIN
#define POLLRDBAND POLLIN
#define POLLWRNORM POLLOUT
#define POLLWRBAND POLLOUT

// ignored
#define POLLHUP 0x0010
#define POLLNVAL 0x0020

int poll(struct pollfd *pollSet, int pollCount, int pollTimeout);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif  /* _FAKE_POLL_H */
