#ifndef MONO_POLL_H
#define MONO_POLL_H

#include <config.h>
#include <sys/time.h>
#include <sys/types.h>
#include <unistd.h>

#ifdef HAVE_POLL
#include <sys/poll.h>

#define MONO_POLLIN		POLLIN
#define MONO_POLLPRI		POLLPRI
#define MONO_POLLOUT		POLLOUT
#define MONO_POLLERR		POLLERR
#define MONO_POLLHUP		POLLHUP
#define MONO_POLLNVAL		POLLNVAL

typedef struct pollfd mono_pollfd;

#else

#ifdef PLATFORM_WIN32
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

int mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout);

#endif /* MONO_POLL_H */

