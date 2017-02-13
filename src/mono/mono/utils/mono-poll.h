/**
 * \file
 */

#ifndef MONO_POLL_H
#define MONO_POLL_H

#include <mono/utils/mono-publib.h>

#include <config.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#include <sys/types.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HAVE_POLL
#ifdef HAVE_POLL_H
#include <poll.h>
#elif defined(HAVE_SYS_POLL_H)
#include <sys/poll.h>
#endif

#define MONO_POLLIN		POLLIN
#define MONO_POLLPRI		POLLPRI
#define MONO_POLLOUT		POLLOUT
#define MONO_POLLERR		POLLERR
#define MONO_POLLHUP		POLLHUP
#define MONO_POLLNVAL		POLLNVAL

typedef struct pollfd mono_pollfd;

#else

#ifdef HOST_WIN32
#include <windows.h>
#endif
#define MONO_POLLIN		1
#define MONO_POLLPRI		2
#define MONO_POLLOUT		4
#define MONO_POLLERR		8
#define MONO_POLLHUP		0x10
#define MONO_POLLNVAL		0x20

typedef struct {
	int fd;
	short events;
	short revents;
} mono_pollfd;

#endif

MONO_API int mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout);

#endif /* MONO_POLL_H */

