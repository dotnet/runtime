/**
 * \file
 */

#include <config.h>

#ifdef HOST_WIN32
/* For select */
#include <winsock2.h>
#endif

#include "debugger-poll.h"
#include <errno.h>
#include <mono/utils/mono-errno.h>

#ifdef DISABLE_SOCKETS
#include <glib.h>

int
mono_poll_can_add (mono_pollfd *ufds, unsigned int nfds, int fd)
{
	return 1;
}

int
mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout)
{
	g_assert_not_reached ();
	return -1;
}

#else

#if defined(HAVE_POLL)

int
mono_poll_can_add (mono_pollfd *ufds, unsigned int nfds, int fd)
{
	return 1;
}

int
mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout)
{
	return poll (ufds, nfds, timeout);
}
#else

int
mono_poll_can_add (mono_pollfd *ufds, unsigned int nfds, int fd)
{
	if (fd < 0)
		return 1;
#ifdef HOST_WIN32
	return (nfds < FD_SETSIZE);
#else
	return (fd < FD_SETSIZE);
#endif
}

int
mono_poll (mono_pollfd *ufds, unsigned int nfds, int timeout)
{
	struct timeval tv, *tvptr;
	int fd, events, affected, count;
	fd_set rfds, wfds, efds;
#ifdef HOST_WIN32
	int nexc = 0;
#endif
	int maxfd = 0;

	if (timeout < 0) {
		tvptr = NULL;
	} else {
		tv.tv_sec = timeout / 1000;
		tv.tv_usec = (timeout % 1000) * 1000;
		tvptr = &tv;
	}

	FD_ZERO (&rfds);
	FD_ZERO (&wfds);
	FD_ZERO (&efds);

	for (unsigned int i = 0; i < nfds; i++) {
		ufds [i].revents = 0;
		fd = ufds [i].fd;
		if (fd < 0)
			continue;

#ifdef HOST_WIN32
		if (nexc >= FD_SETSIZE) {
			ufds [i].revents = MONO_POLLNVAL;
			return 1;
		}
#else
		if (fd >= FD_SETSIZE) {
			ufds [i].revents = MONO_POLLNVAL;
			return 1;
		}
#endif

		events = ufds [i].events;
		if ((events & MONO_POLLIN) != 0)
			FD_SET (fd, &rfds);

		if ((events & MONO_POLLOUT) != 0)
			FD_SET (fd, &wfds);

		FD_SET (fd, &efds);
#ifdef HOST_WIN32
		nexc++;
#endif
		if (fd > maxfd)
			maxfd = fd;

	}

	affected = select (maxfd + 1, &rfds, &wfds, &efds, tvptr);
	if (affected == -1) {
#ifdef HOST_WIN32
		int error = WSAGetLastError ();
		switch (error) {
		case WSAEFAULT: mono_set_errno (EFAULT); break;
		case WSAEINVAL: mono_set_errno (EINVAL); break;
		case WSAEINTR: mono_set_errno (EINTR); break;
		/* case WSAEINPROGRESS: mono_set_errno (EINPROGRESS); break; */
		case WSAEINPROGRESS: mono_set_errno (EINTR); break;
		case WSAENOTSOCK: mono_set_errno (EBADF); break;
#ifdef ENOSR
		case WSAENETDOWN: mono_set_errno (ENOSR); break;
#endif
		default: mono_set_errno (0);
		}
#endif

		return -1;
	}

	count = 0;
	for (unsigned int i = 0; i < nfds && affected > 0; i++) {
		fd = ufds [i].fd;
		if (fd < 0)
			continue;

		events = ufds [i].events;
		if ((events & MONO_POLLIN) != 0 && FD_ISSET (fd, &rfds)) {
			ufds [i].revents |= MONO_POLLIN;
			affected--;
		}

		if ((events & MONO_POLLOUT) != 0 && FD_ISSET (fd, &wfds)) {
			ufds [i].revents |= MONO_POLLOUT;
			affected--;
		}

		if (FD_ISSET (fd, &efds)) {
			ufds [i].revents |= MONO_POLLERR;
			affected--;
		}

		if (ufds [i].revents != 0)
			count++;
	}

	return count;
}

#endif

#endif /* #ifndef DISABLE_SOCKETS */
